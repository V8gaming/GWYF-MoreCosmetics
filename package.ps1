param(
    [string]$GameDir = "C:\SteamLibrary\steamapps\common\Gamble With Your Friends",
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$TeamName = "MrMeeseeks"

function Get-VersionFromCsproj($path) {
    $content = Get-Content $path -Raw
    if ($content -match '<Version>(.+?)</Version>') {
        return $Matches[1]
    }
    throw "Could not find <Version> in $path"
}

$libVersion = Get-VersionFromCsproj (Join-Path $Root "src\GWYF-NewClothing.csproj")
$exVersion  = Get-VersionFromCsproj (Join-Path $Root "ExampleCosmetics\GWYF-ExampleCosmetics.csproj")

$libName = "MoreCosmetics"
$exName  = "ExampleCosmetics"

Write-Host "=== Packaging $libName v$libVersion + $exName v$exVersion ===" -ForegroundColor Cyan
Write-Host "Team: $TeamName" -ForegroundColor Gray

# Build
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "[1/4] Building $libName..." -ForegroundColor Yellow
    dotnet build -c Release "$Root/src" -p:DeployToGame=false
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }

    Write-Host "[2/4] Building $exName..." -ForegroundColor Yellow
    dotnet build -c Release "$Root/ExampleCosmetics" -p:GameDir=$GameDir -p:DeployToGame=false
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}
else {
    Write-Host ""
    Write-Host "[Skipping build]" -ForegroundColor Gray
}

# Setup
$releasesDir = Join-Path $Root "releases"
$libOut = Join-Path $Root "src\bin\Release\netstandard2.1"
$exOut  = Join-Path $Root "ExampleCosmetics\bin\Release\netstandard2.1"

New-Item -ItemType Directory -Path $releasesDir -Force | Out-Null

# ---- More Cosmetics ----
Write-Host ""
Write-Host "[3/4] Packaging $libName..." -ForegroundColor Yellow

$libTmp = Join-Path $releasesDir $libName
$libZip = Join-Path $releasesDir "$libName-$libVersion.zip"
if (Test-Path $libTmp) { Remove-Item $libTmp -Recurse -Force }
if (Test-Path $libZip) { Remove-Item $libZip -Force }

New-Item -ItemType Directory -Path (Join-Path $libTmp "BepInEx\plugins\More-Cosmetics") -Force | Out-Null

Copy-Item (Join-Path $Root "manifest.json")   (Join-Path $libTmp "manifest.json")
Copy-Item (Join-Path $Root "README.md")       (Join-Path $libTmp "README.md")

$icon = Join-Path $Root "project.png"
if (Test-Path $icon) {
    Copy-Item $icon (Join-Path $libTmp "icon.png")
} else {
    Write-Host "  WARNING: project.png not found, skipping icon" -ForegroundColor DarkYellow
}

Copy-Item (Join-Path $libOut "More-Cosmetics.dll") (Join-Path $libTmp "BepInEx\plugins\More-Cosmetics\More-Cosmetics.dll")

Compress-Archive -Path "$libTmp\*" -DestinationPath $libZip -Force
Remove-Item $libTmp -Recurse -Force

$libSize = [math]::Round((Get-Item $libZip).Length / 1KB, 0)
Write-Host "  ${libName}-${libVersion}.zip ($libSize KB)" -ForegroundColor Green

# ---- Example Cosmetics ----
Write-Host "[4/4] Packaging $exName..." -ForegroundColor Yellow

$exTmp = Join-Path $releasesDir $exName
$exZip = Join-Path $releasesDir "$exName-$exVersion.zip"
if (Test-Path $exTmp) { Remove-Item $exTmp -Recurse -Force }
if (Test-Path $exZip) { Remove-Item $exZip -Force }

New-Item -ItemType Directory -Path (Join-Path $exTmp "BepInEx\plugins\ExampleCosmetics\models") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $exTmp "BepInEx\plugins\ExampleCosmetics\textures") -Force | Out-Null

Copy-Item (Join-Path $Root "ExampleCosmetics\manifest.json") (Join-Path $exTmp "manifest.json")
Copy-Item (Join-Path $Root "ExampleCosmetics\README.md")     (Join-Path $exTmp "README.md")

if (Test-Path $icon) {
    Copy-Item $icon (Join-Path $exTmp "icon.png")
}

$exBase = "BepInEx\plugins\ExampleCosmetics"
Copy-Item (Join-Path $exOut "ExampleCosmetics.dll") (Join-Path $exTmp "$exBase\ExampleCosmetics.dll")
Copy-Item (Join-Path $Root "ExampleCosmetics\cosmetics.json") (Join-Path $exTmp "$exBase\cosmetics.json")
Copy-Item (Join-Path $Root "ExampleCosmetics\models\suit_1.obj") (Join-Path $exTmp "$exBase\models\suit_1.obj")
Copy-Item (Join-Path $Root "ExampleCosmetics\textures\custom_suit_1.png") (Join-Path $exTmp "$exBase\textures\custom_suit_1.png")

Compress-Archive -Path "$exTmp\*" -DestinationPath $exZip -Force
Remove-Item $exTmp -Recurse -Force

$exSize = [math]::Round((Get-Item $exZip).Length / 1KB, 0)
Write-Host "  ${exName}-${exVersion}.zip ($exSize KB)" -ForegroundColor Green

# Done
Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host "Upload these to Thunderstore:"
Write-Host "  releases/${libName}-${libVersion}.zip"
Write-Host "  releases/${exName}-${exVersion}.zip"
