param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version = "0.1.0-alpha"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src/DeskNotes.App/DeskNotes.App.csproj"
$output = Join-Path $root "artifacts/publish"

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

$publishArgs = @(
    $project,
    "--configuration", "Release",
    "--runtime", "win-x64",
    "--self-contained", "false",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:Version=$Version",
    "--output", $output
)

& dotnet publish @publishArgs

if ($LASTEXITCODE -ne 0) {
    throw "La publicación de DeskNotes ha fallado (exit code $LASTEXITCODE)."
}

$exe = Join-Path $output "DeskNotes.App.exe"
if (-not (Test-Path $exe)) {
    throw "No se ha generado DeskNotes.App.exe en $output."
}

Write-Host "Publicación completada: $exe"
Write-Host "Versión: $Version"
Write-Host "Framework-dependent: requiere Microsoft .NET 10 Desktop Runtime x64."
