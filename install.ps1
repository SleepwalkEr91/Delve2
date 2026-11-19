# Delve Plugin Installer for ExileCore
# Downloads the latest release from GitHub and installs to your Plugins folder

$repo = "Matejko-poe/Delve2"
$pluginName = "Delve"

Write-Host "Delve Plugin Installer" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan

# Ask for plugins folder
$defaultPath = "C:\ExileCore\Plugins"
$pluginsPath = Read-Host "ExileCore Plugins folder path (Enter for $defaultPath)"
if ([string]::IsNullOrWhiteSpace($pluginsPath)) {
    $pluginsPath = $defaultPath
}

if (-not (Test-Path $pluginsPath)) {
    Write-Host "ERROR: Folder not found: $pluginsPath" -ForegroundColor Red
    exit 1
}

# Fetch latest release
Write-Host "Fetching latest release..." -ForegroundColor Yellow
try {
    $release = Invoke-RestMethod "https://api.github.com/repos/$repo/releases/latest"
} catch {
    Write-Host "ERROR: Could not fetch release info. Check your internet connection." -ForegroundColor Red
    exit 1
}

$zipAsset = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1
if (-not $zipAsset) {
    Write-Host "ERROR: No zip found in latest release." -ForegroundColor Red
    exit 1
}

Write-Host "Downloading $($zipAsset.name) ($([math]::Round($zipAsset.size/1KB)) KB)..." -ForegroundColor Yellow
$zipPath = Join-Path $env:TEMP "$pluginName-release.zip"
Invoke-WebRequest $zipAsset.browser_download_url -OutFile $zipPath

# Extract
$extractPath = Join-Path $env:TEMP "$pluginName-extract"
if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
Expand-Archive $zipPath $extractPath

# Find the Delve folder inside the zip
$sourceFolder = Get-ChildItem $extractPath -Directory | Where-Object { $_.Name -eq $pluginName } | Select-Object -First 1
if (-not $sourceFolder) {
    # zip might contain files directly (not in a subfolder)
    $sourceFolder = Get-Item $extractPath
}

$destFolder = Join-Path $pluginsPath $pluginName
Write-Host "Installing to $destFolder..." -ForegroundColor Yellow

if (Test-Path $destFolder) {
    Remove-Item $destFolder -Recurse -Force
}
Copy-Item $sourceFolder.FullName $destFolder -Recurse

# Cleanup
Remove-Item $zipPath -Force
Remove-Item $extractPath -Recurse -Force

Write-Host "Done! Delve $($release.tag_name) installed." -ForegroundColor Green
Write-Host "Restart ExileCore to load the plugin." -ForegroundColor Green
