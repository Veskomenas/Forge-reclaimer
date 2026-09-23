[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageRoot = Join-Path $projectRoot 'package'
$projectFile = Join-Path $projectRoot 'ForgeReclaimer.csproj'
$manifestPath = Join-Path $packageRoot 'manifest.json'
$iconPath = Join-Path $packageRoot 'icon.png'
$readmePath = Join-Path $packageRoot 'README.md'
$changelogPath = Join-Path $packageRoot 'CHANGELOG.md'

foreach ($requiredPath in @($projectFile, $manifestPath, $iconPath, $readmePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required package file is missing: $requiredPath"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.name -notmatch '^[A-Za-z0-9_]+$') {
    throw 'manifest.json name may contain only letters, numbers, and underscores.'
}
if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
    throw 'manifest.json version_number must use Major.Minor.Patch format.'
}
if ($manifest.description.Length -gt 250) {
    throw 'manifest.json description must be 250 characters or fewer.'
}

$image = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($image.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid -or $image.Width -ne 256 -or $image.Height -ne 256) {
        throw 'package/icon.png must be a 256x256 PNG for Thunderstore.'
    }
}
finally {
    $image.Dispose()
}

if (-not $SkipBuild) {
    & dotnet build $projectFile -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}

$dllPath = Join-Path $projectRoot 'bin\Release\Helgi.ForgeReclaimer.dll'
if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "Built DLL is missing: $dllPath"
}

$distRoot = Join-Path $projectRoot 'dist'
$stageRoot = Join-Path $distRoot 'ForgeReclaimer'
$zipPath = Join-Path $distRoot ("ForgeReclaimer-{0}.zip" -f $manifest.version_number)

if (Test-Path -LiteralPath $stageRoot) { Remove-Item -LiteralPath $stageRoot -Recurse -Force }
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
New-Item -ItemType Directory -Path (Join-Path $stageRoot 'BepInEx\plugins\Helgi.ForgeReclaimer') -Force | Out-Null

Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $stageRoot 'manifest.json')
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $stageRoot 'icon.png')
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $stageRoot 'README.md')
if (Test-Path -LiteralPath $changelogPath -PathType Leaf) {
    Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $stageRoot 'CHANGELOG.md')
}
Copy-Item -LiteralPath $dllPath -Destination (Join-Path $stageRoot 'BepInEx\plugins\Helgi.ForgeReclaimer\Helgi.ForgeReclaimer.dll')

Compress-Archive -Path (Join-Path $stageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal

$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries.FullName)
    foreach ($requiredEntry in @('manifest.json', 'icon.png', 'README.md', 'BepInEx/plugins/Helgi.ForgeReclaimer/Helgi.ForgeReclaimer.dll')) {
        if ($entries -notcontains $requiredEntry) { throw "Package archive is missing: $requiredEntry" }
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Thunderstore package created: $zipPath"
