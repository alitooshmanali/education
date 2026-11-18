[CmdletBinding()]
param(
    [string]$Version = "v33.003"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$webRoot = Join-Path $projectRoot "src/Lesson.Web/wwwroot"
$fontDirectory = Join-Path $webRoot "fonts"
$tempDirectory = Join-Path $env:TEMP ("asatir-vazirmatn-" + [Guid]::NewGuid().ToString("N"))
$archivePath = Join-Path $tempDirectory "vazirmatn.zip"
$downloadUrl = "https://github.com/rastikerdar/vazirmatn/releases/download/$Version/vazirmatn-$Version.zip"

try {
    New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $fontDirectory -Force | Out-Null

    Write-Host "Downloading official Vazirmatn $Version release..."
    Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath

    $extractDirectory = Join-Path $tempDirectory "extracted"
    Expand-Archive -Path $archivePath -DestinationPath $extractDirectory -Force

    $fontFile = Get-ChildItem -Path $extractDirectory -Recurse -File |
        Where-Object { $_.Name -ceq 'Vazirmatn[wght].woff2' } |
        Select-Object -First 1

    if (-not $fontFile) {
        throw "The official release did not contain fonts/webfonts/Vazirmatn[wght].woff2."
    }

    Copy-Item -LiteralPath $fontFile.FullName -Destination (Join-Path $fontDirectory "Vazirmatn-Variable.woff2") -Force

    $licenseFile = Get-ChildItem -Path $extractDirectory -Recurse -File |
        Where-Object { $_.Name -ceq 'OFL.txt' } |
        Select-Object -First 1

    if ($licenseFile) {
        Copy-Item -LiteralPath $licenseFile.FullName -Destination (Join-Path $fontDirectory "OFL-Vazirmatn.txt") -Force
    }

    Write-Host "Vazirmatn installed locally at: $fontDirectory"
    Write-Host "No font CDN is required at runtime."
}
finally {
    if (Test-Path $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}
