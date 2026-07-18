#!/usr/bin/env pwsh
# PreToolUse hook para Edit|Write.
# Bloquea cualquier edicion fuera del `scope:` declarado en el CR activo
# (change-request/.active -> change-request/{id}.md). Es el gate tecnico real
# detras de la regla "todo cambio de codigo pasa por un CR" de CLAUDE.md.
#
# Escrito en PowerShell (no bash+jq) siguiendo la convencion ya existente en
# este repo (el hook anterior, check-release-approval.ps1, ya era .ps1) y
# porque en esta maquina no hay jq ni python3 instalados/funcionales.
#
# Politica cuando NO hay CR activo (.active vacio o inexistente): solo se
# permite tocar rutas "meta" del propio pipeline (CLAUDE.md, change-request/**,
# .claude/**, specs/**). Cualquier otra ruta se bloquea - fail closed, no
# fail open, para que nunca se pueda editar codigo de negocio sin un CR.

$ErrorActionPreference = 'Stop'

$inputJson = [Console]::In.ReadToEnd()
try {
    $data = $inputJson | ConvertFrom-Json
} catch {
    # Si no podemos parsear el payload, fallamos cerrado: mejor bloquear una
    # edicion legitima por error de parseo que dejar pasar una fuera de scope.
    [Console]::Error.WriteLine("scope-guard: no se pudo parsear el payload del hook. Bloqueando por seguridad.")
    exit 2
}

$filePath = $data.tool_input.file_path
if (-not $filePath) {
    # MultiEdit u otra herramienta sin file_path directo: no es nuestro caso de uso, no bloqueamos.
    exit 0
}

$projectDir = $env:CLAUDE_PROJECT_DIR
if (-not $projectDir) {
    $projectDir = (Get-Location).Path
}

function ConvertTo-RelativePosixPath {
    param([string]$AbsoluteOrRelativePath, [string]$Root)

    $normalizedRoot = ($Root -replace '\\', '/').TrimEnd('/')
    $normalizedPath = ($AbsoluteOrRelativePath -replace '\\', '/')

    # Comparacion case-insensitive: en Windows la letra de unidad puede llegar
    # en distinta capitalizacion ("c:/..." vs "C:/...") segun quien construya
    # la ruta, y el filesystem de todas formas no distingue mayusculas/minusculas.
    if ($normalizedPath.ToLowerInvariant().StartsWith(($normalizedRoot + '/').ToLowerInvariant())) {
        return $normalizedPath.Substring($normalizedRoot.Length + 1)
    }

    # Fuera del directorio del proyecto por completo (ej. un archivo de notas
    # en una carpeta hermana). El concepto de "scope de CR" solo tiene sentido
    # para archivos DENTRO del propio repo - no gatees archivos ajenos al repo.
    return $null
}

$relativePath = ConvertTo-RelativePosixPath -AbsoluteOrRelativePath $filePath -Root $projectDir
if ($null -eq $relativePath) {
    exit 0
}

function Test-GlobMatch {
    param([string]$Path, [string]$Glob)

    $normalizedGlob = ($Glob -replace '\\', '/').Trim()

    # ** => cualquier cosa incluyendo '/'; * => cualquier cosa excepto '/'; ? => un caracter.
    $regex = [regex]::Escape($normalizedGlob)
    $regex = $regex -replace '\\\*\\\*', 'DOUBLESTARPLACEHOLDER'
    $regex = $regex -replace '\\\*', '[^/]*'
    $regex = $regex -replace '\\\?', '.'
    $regex = $regex -replace 'DOUBLESTARPLACEHOLDER', '.*'
    $regex = '^' + $regex + '$'

    return $Path -match $regex
}

$metaGlobs = @(
    'CLAUDE.md',
    'change-request/**',
    '.claude/**',
    'specs/**'
)

function Test-AnyGlobMatch {
    param([string]$Path, [string[]]$Globs)
    foreach ($g in $Globs) {
        if (Test-GlobMatch -Path $Path -Glob $g) { return $true }
    }
    return $false
}

$activeFile = Join-Path $projectDir 'change-request/.active'
$activeCr = $null
if (Test-Path $activeFile) {
    $activeCr = (Get-Content $activeFile -Raw -ErrorAction SilentlyContinue)
    if ($activeCr) {
        # Quita BOM (U+FEFF) ademas de espacios en blanco normales - algunos
        # editores (Notepad, Out-File por defecto) lo agregan y .Trim() solo
        # no lo cuenta como whitespace, lo que dejaria $activeCr no-vacio
        # aunque el archivo "se vea" vacio.
        $activeCr = $activeCr.Trim([char]0xFEFF, "`r", "`n", ' ', "`t")
    }
}

if (-not $activeCr) {
    if (Test-AnyGlobMatch -Path $relativePath -Globs $metaGlobs) {
        exit 0
    }

    [Console]::Error.WriteLine(
        "Bloqueado: no hay ningun CR activo (change-request/.active esta vacio) y '$relativePath' " +
        "no es una ruta meta del pipeline (CLAUDE.md, change-request/**, .claude/**, specs/**). " +
        "No se puede editar codigo de negocio sin un CR aprobado y activo. " +
        "Crea/activa un CR en change-request/ antes de continuar."
    )
    exit 2
}

$crFile = Join-Path $projectDir "change-request/$activeCr.md"
if (-not (Test-Path $crFile)) {
    [Console]::Error.WriteLine(
        "Bloqueado: change-request/.active apunta a '$activeCr' pero '$crFile' no existe. " +
        "No se puede verificar el scope permitido. Corrige change-request/.active o crea el CR."
    )
    exit 2
}

$crContent = Get-Content $crFile -Raw

# Extrae el bloque de frontmatter YAML entre los dos primeros '---'.
$frontmatterMatch = [regex]::Match($crContent, '(?s)^---\s*\r?\n(.*?)\r?\n---')
if (-not $frontmatterMatch.Success) {
    [Console]::Error.WriteLine("Bloqueado: '$crFile' no tiene frontmatter YAML valido (falta '---'). No se puede leer 'scope:'.")
    exit 2
}
$frontmatter = $frontmatterMatch.Groups[1].Value

# Dentro del frontmatter, ubica "scope:" y lee las lineas "  - "..."" que le siguen.
$scopeSectionMatch = [regex]::Match($frontmatter, '(?m)^scope:\s*(?:#.*)?\r?\n((?:^\s*-\s*.+\r?\n?)*)')
if (-not $scopeSectionMatch.Success) {
    [Console]::Error.WriteLine("Bloqueado: '$crFile' no tiene un campo 'scope:' con al menos un glob. No se puede editar nada sin scope declarado.")
    exit 2
}

$scopeGlobs = @()
foreach ($line in ($scopeSectionMatch.Groups[1].Value -split "`r?`n")) {
    $m = [regex]::Match($line, '^\s*-\s*"?([^"#]+?)"?\s*(#.*)?$')
    if ($m.Success) {
        $glob = $m.Groups[1].Value.Trim()
        if ($glob) { $scopeGlobs += $glob }
    }
}

if ($scopeGlobs.Count -eq 0) {
    [Console]::Error.WriteLine("Bloqueado: 'scope:' en '$crFile' esta vacio. No se puede editar nada sin al menos un glob declarado.")
    exit 2
}

# Las rutas meta del pipeline siempre estan permitidas (los agentes necesitan
# escribir specs/CR-XXX/** y actualizar status-pipeline.json/blueprint.md
# aunque el scope del CR solo hable de codigo de negocio).
$alwaysAllowed = @('specs/**', '.claude/artifacts/**', 'change-request/.active', "change-request/$activeCr.md")

if ((Test-AnyGlobMatch -Path $relativePath -Globs $alwaysAllowed) -or
    (Test-AnyGlobMatch -Path $relativePath -Globs $scopeGlobs)) {
    exit 0
}

[Console]::Error.WriteLine(
    "Bloqueado: '$relativePath' esta fuera del scope declarado en '$crFile' (CR activo: $activeCr). " +
    "Scope permitido: $($scopeGlobs -join ', '). " +
    "Si este archivo realmente debe cambiar, actualiza el 'scope:' del CR con aprobacion humana explicita " +
    "antes de reintentar la edicion."
)
exit 2
