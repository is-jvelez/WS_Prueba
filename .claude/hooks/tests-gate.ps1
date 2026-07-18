#!/usr/bin/env pwsh
# PostToolUse hook para Bash.
# Detecta si el comando ejecutado fue de tests (dotnet test / vstest), lee el
# resultado real y lo escribe en .claude/artifacts/status-pipeline.json
# (campo tests_last_run de la fase activa). Si el test fallo, exit 2 con el
# resumen del fallo, para que el agente se detenga (politica HITL, punto 2).
#
# Escrito en PowerShell (no bash+jq) por la misma razon que scope-guard.ps1:
# no hay jq ni python3 disponibles en esta maquina, y ya existia convencion
# de hooks .ps1 en este repo.
#
# Nota sobre exit code real: el payload de PostToolUse para Bash en Claude
# Code NO expone un campo numerico de exit code (verificado empiricamente:
# tool_response trae solo { type: "text", text, isError }). isError refleja
# si el comando termino con exit code != 0, asi que lo usamos como la senal
# de pass/fail, reforzada con el parseo del resumen que dotnet test imprime
# en texto (lineas "Aprobado!"/"Failed!" y conteos Failed/Passed/Skipped/Total).

$ErrorActionPreference = 'Stop'

$inputJson = [Console]::In.ReadToEnd()
try {
    $data = $inputJson | ConvertFrom-Json
} catch {
    exit 0
}

$command = $data.tool_input.command
if (-not $command) {
    exit 0
}

if ($command -notmatch 'dotnet\s+test' -and $command -notmatch 'vstest') {
    exit 0
}

$responseText = ''
$isError = $false
if ($data.tool_response) {
    if ($data.tool_response.text) { $responseText = $data.tool_response.text }
    if ($data.tool_response.isError) { $isError = [bool]$data.tool_response.isError }
}

# dotnet test imprime un resumen tipo:
#   Failed!  - Failed:     2, Passed:    10, Skipped:     0, Total:    12, ...
#   Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, ...
$summaryMatch = [regex]::Match($responseText, '(?im)^(Passed|Failed)!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)')

$passed = $null
$summaryLine = $null
if ($summaryMatch.Success) {
    $failedCount = [int]$summaryMatch.Groups[2].Value
    $passed = ($failedCount -eq 0)
    $summaryLine = $summaryMatch.Value.Trim()
} else {
    # No se encontro el resumen esperado (ej. build fallo antes de llegar a
    # correr tests, o el formato de salida cambio) - nos quedamos con isError.
    $passed = -not $isError
    $summaryLine = if ($isError) { 'Comando de tests termino con error (sin resumen de dotnet test reconocible).' } else { 'Comando de tests termino sin error aparente (sin resumen de dotnet test reconocible).' }
}

$projectDir = $env:CLAUDE_PROJECT_DIR
if (-not $projectDir) { $projectDir = (Get-Location).Path }
$statusFile = Join-Path $projectDir '.claude/artifacts/status-pipeline.json'

if (Test-Path $statusFile) {
    try {
        $status = Get-Content $statusFile -Raw | ConvertFrom-Json
    } catch {
        $status = $null
    }

    if ($status) {
        $nowIso = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $currentPhaseId = $status.current_phase
        $phase = $status.phases | Where-Object { $_.id -eq $currentPhaseId } | Select-Object -First 1

        if ($phase) {
            $phase.tests_last_run = "$(if ($passed) { 'PASA' } else { 'FALLA' }) - $summaryLine ($nowIso)"
        }

        $status.last_updated = $nowIso
        $status | ConvertTo-Json -Depth 10 | Set-Content -Path $statusFile -Encoding UTF8
    }
}

if (-not $passed) {
    [Console]::Error.WriteLine("tests-gate: la corrida de tests fallo. $summaryLine")
    [Console]::Error.WriteLine("Detente y reporta el fallo (politica HITL) en vez de continuar a la siguiente fase.")
    exit 2
}

exit 0
