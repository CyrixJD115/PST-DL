$ErrorActionPreference = "Stop"

$PSTM_REPO = "CyrixJD115/PST-Manager"
$PSTM_RAW_BASE = "https://raw.githubusercontent.com/$PSTM_REPO/main"
$PSTM_DIR = Join-Path $env:LOCALAPPDATA "pstm"
$PSTM_SCRIPT = Join-Path $PSTM_DIR "pstm.ps1"

Write-Host ""
Write-Host "Installing pstm..."
Write-Host ""

New-Item -ItemType Directory -Path $PSTM_DIR -Force | Out-Null

Write-Host "> Downloading pstm..."
try {
    Invoke-WebRequest -Uri "${PSTM_RAW_BASE}/.Windows/pstm.ps1" -OutFile $PSTM_SCRIPT -UseBasicParsing
} catch {
    Write-Host "x Error: Failed to download pstm. $($_.Exception.Message)"
    exit 1
}

Write-Host "* Downloaded to: $PSTM_SCRIPT"
Write-Host ""

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$PSTM_DIR*") {
    $newPath = if ($userPath) { "$userPath;$PSTM_DIR" } else { $PSTM_DIR }
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host "* Added $PSTM_DIR to user PATH"
} else {
    Write-Host "  $PSTM_DIR already in PATH"
}

$env:Path = [Environment]::GetEnvironmentVariable("Path", "User") + ";" + [Environment]::GetEnvironmentVariable("Path", "Machine")

Write-Host ""
Write-Host "pstm installed successfully!"
Write-Host ""
Write-Host "pstm is ready to use now."
Write-Host ""
Write-Host "  pstm -i    Install PalworldSaveTools"
Write-Host "  pstm -h    Show all commands"
Write-Host ""
