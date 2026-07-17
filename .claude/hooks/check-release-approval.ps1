#!/usr/bin/env pwsh
# PreToolUse hook para Bash.
# Bloquea 'git push' si no existe un validation-report.md con "Estado: APROBADO"
# en el CR mas reciente. Es el unico gate tecnico duro del pipeline - todo lo
# demas depende de que los agentes respeten el STOP.
#
# 'docker compose up/down' NO se gatea: es infraestructura local de SQL Server
# para dev/test (ver docker-compose.yml), siempre reversible, y la usan
# legitimamente behavior-capturer y validator ANTES de que exista un
# validation-report para ese CR. Gatearlo producia un deadlock: validator
# necesita levantar el entorno para poder generar el reporte que el hook
# exigia que ya existiera.

$inputJson = [Console]::In.ReadToEnd()
$data = $inputJson | ConvertFrom-Json
$command = $data.tool_input.command

if (-not $command) {
    exit 0
}

# Solo nos interesa el push a remoto - la accion irreversible/compartida real
if ($command -match 'git\s+push') {

    # Se ordena por el numero de CR extraido del nombre de carpeta (CR-XXX),
    # no por LastWriteTime: el timestamp de la carpeta es fragil (puede
    # actualizarse por acciones ajenas al CR activo, o no reflejar cual CR
    # es realmente "el actual" si hay mas de uno en vuelo). El numero de CR
    # es monotonicamente creciente por convencion del pipeline (ver
    # feature.md, paso 0), asi que el mas alto es siempre el mas reciente.
    $latestCr = Get-ChildItem -Path 'specs' -Directory -Filter 'CR-*' -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^CR-(\d+)$' } |
        Sort-Object { [int]([regex]::Match($_.Name, '^CR-(\d+)$').Groups[1].Value) } -Descending |
        Select-Object -First 1

    if (-not $latestCr) {
        [Console]::Error.WriteLine("Bloqueado: no se encontro ninguna carpeta specs/CR-*/. No se puede verificar aprobacion antes de hacer push.")
        exit 2
    }

    $report = Join-Path $latestCr.FullName 'validation-report.md'

    if (-not (Test-Path $report)) {
        [Console]::Error.WriteLine("Bloqueado: no existe '$report'. Corre primero el agente validator.")
        exit 2
    }

    $content = Get-Content $report -Raw

    # Match anclado a la linea completa "Estado: <valor>" (permite un prefijo
    # de encabezado markdown tipo '## '). Se exige que el valor sea
    # EXACTAMENTE 'APROBADO' tras recortar espacios - un texto tipo
    # 'APROBADO | RECHAZADO' (plantilla sin resolver) o cualquier otra
    # variante NO pasa. La version anterior solo buscaba la subcadena
    # 'Estado: APROBADO' en cualquier parte del archivo, lo cual matcheaba
    # incluso si la plantilla se dejaba sin resolver con ambas palabras.
    $estadoMatch = [regex]::Match($content, '(?im)^\s*#*\s*Estado:\s*(.+?)\s*$')

    if (-not $estadoMatch.Success) {
        [Console]::Error.WriteLine("Bloqueado: '$report' no tiene una linea 'Estado: ...' reconocible. No se puede hacer push.")
        exit 2
    }

    $estadoValor = $estadoMatch.Groups[1].Value.Trim()

    if ($estadoValor -ne 'APROBADO') {
        [Console]::Error.WriteLine("Bloqueado: '$report' declara Estado: '$estadoValor' (se requiere exactamente 'APROBADO', sin texto adicional). No se puede hacer push hasta que la validacion quede aprobada explicitamente.")
        exit 2
    }
}

exit 0