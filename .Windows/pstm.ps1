$ErrorActionPreference = "Stop"

$PSTM_VERSION = "1.1.0"
$PSTM_REPO = "CyrixJD115/PST-Manager"
$PST_REPO = "deafdudecomputers/PalworldSaveTools"
$PSTM_RAW_BASE = "https://raw.githubusercontent.com/$PSTM_REPO/main"
$PSTM_VERSION_URL = "$PSTM_RAW_BASE/version.yaml"

$PST_DATA_DIR = Join-Path $env:LOCALAPPDATA "palworldsavetools"
$PSTM_DIR = Join-Path $env:LOCALAPPDATA "pstm"
$PSTM_SCRIPT = Join-Path $PSTM_DIR "pstm.ps1"



function Show-Help {

    Write-Host "pstm v${PSTM_VERSION}" -NoNewline
    Write-Host " - PST Manager"
    Write-Host ""

    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  pstm [command]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host ""
    Write-Host "  -h" -NoNewline; Write-Host ", -help" -NoNewline; Write-Host "            Show this help message"
    Write-Host "  -v" -NoNewline; Write-Host ", -version" -NoNewline; Write-Host "         Show pstm and remote PST version"
    Write-Host "  -i" -NoNewline; Write-Host ", -install" -NoNewline; Write-Host "        Download and install the latest PalworldSaveTools"
    Write-Host "  run" -NoNewline; Write-Host "                Run PalworldSaveTools"
    Write-Host "  -u" -NoNewline; Write-Host ", -upgrade" -NoNewline; Write-Host "          Update PalworldSaveTools to the latest version"
    Write-Host "  -update-self" -NoNewline; Write-Host "          Update pstm to the latest version"
    Write-Host "  -g" -NoNewline; Write-Host ", -github" -NoNewline; Write-Host "          Open PalworldSaveTools GitHub page"
    Write-Host "  -uninstall" -NoNewline; Write-Host "            Uninstall PalworldSaveTools"
    Write-Host "  -uninstall-all" -NoNewline; Write-Host "        Uninstall pstm and PalworldSaveTools"
    Write-Host ""

    Write-Host ""
}

function Get-LatestPstTag {
    $apiUrl = "https://api.github.com/repos/$PST_REPO/releases/latest"
    try {
        $rel = Invoke-RestMethod -Uri $apiUrl -UseBasicParsing
        return $rel.tag_name
    } catch {}
    return ""
}

function Compare-Versions {
    param([string]$V1, [string]$V2)
    $a1 = $V1 -split '\.'
    $a2 = $V2 -split '\.'
    $len = [Math]::Max($a1.Count, $a2.Count)
    for ($i = 0; $i -lt $len; $i++) {
        $n1 = if ($i -lt $a1.Count) { [int]$a1[$i] } else { 0 }
        $n2 = if ($i -lt $a2.Count) { [int]$a2[$i] } else { 0 }
        if ($n1 -gt $n2) { return 1 }
        if ($n1 -lt $n2) { return -1 }
    }
    return 0
}

function Get-LatestPstmVersion {
    try {
        $yaml = Invoke-RestMethod -Uri $PSTM_VERSION_URL -UseBasicParsing
        $match = [regex]::Match($yaml, 'version:\s*"([^"]+)"')
        if (-not $match.Success) {
            $match = [regex]::Match($yaml, 'version:\s+([0-9][0-9.]*[0-9])')
        }
        if ($match.Success) {
            return $match.Groups[1].Value.TrimStart('v')
        }
    } catch {}
    return ""
}

function Ensure-Uv {
    try {
        uv --version | Out-Null
        return
    } catch {}

    Write-Host "> uv not found. Installing uv..."
    powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"

    $env:Path = [Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [Environment]::GetEnvironmentVariable("Path", "User")

    try {
        uv --version | Out-Null
        Write-Host "* uv installed successfully!"
    } catch {
        Write-Host "x Error: Failed to install uv."
        throw "uv install failed"
    }
}

function New-PstLauncher {
    $pstPs1 = Join-Path $PST_DATA_DIR "pst.ps1"
    $sourceDir = Join-Path $PST_DATA_DIR "source"
    $icoPath = Join-Path $PST_DATA_DIR "pstm.ico"
    $shortcutPath = Join-Path $env:USERPROFILE "Desktop\PST.lnk"

    $content = @"
Set-Location "$sourceDir"
uv python install 3.13
uv run ./start.py `$args
"@
    Set-Content -Path $pstPs1 -Value $content -Force
    Write-Host "* Launcher generated: " -NoNewline
    Write-Host $pstPs1

    try {
        Write-Host "> Downloading icon..."
        Invoke-WebRequest -Uri "${PSTM_RAW_BASE}/pstm.ico" -OutFile $icoPath -UseBasicParsing
        Write-Host "* Icon downloaded: " -NoNewline
        Write-Host $icoPath
    } catch {
        Write-Host "! Warning: Failed to download icon."
    }

    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($shortcutPath)
    $Shortcut.TargetPath = "powershell.exe"
    $Shortcut.Arguments = "-ExecutionPolicy Bypass -File `"$pstPs1`""
    $Shortcut.WorkingDirectory = $sourceDir
    if (Test-Path $icoPath) {
        $Shortcut.IconLocation = $icoPath
    }
    $Shortcut.Save()
    Write-Host "* Desktop shortcut created: " -NoNewline
    Write-Host $shortcutPath
}

function Install-PstVersion {
    param([string]$TagName)

    $version = $TagName.TrimStart('v')
    $downloadUrl = "https://github.com/$PST_REPO/archive/refs/tags/$TagName.zip"
    $outputFilename = Join-Path $env:TEMP "PalworldSaveTools-$version.zip"
    $extractedDirName = "PalworldSaveTools-$version"

    Write-Host "* Latest version found:" -NoNewline
    Write-Host " $TagName"
    Write-Host ""

    Write-Host "> Step 1/4: Downloading source code..."
    Write-Host "  URL: " -NoNewline
    Write-Host $downloadUrl
    Write-Host ""

    try {
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $downloadUrl -OutFile $outputFilename -UseBasicParsing

        $fileSize = (Get-Item $outputFilename).Length / 1MB
        $fileSizeStr = "{0:N1} MB" -f $fileSize
        Write-Host "* Download Complete! " -NoNewline
        Write-Host "($fileSizeStr)"
    } catch {
        Write-Host ""
        Write-Host "x Error: Download failed."
        Write-Host "  $($_.Exception.Message)"
        exit 1
    } finally {
        $ProgressPreference = 'Continue'
    }
    Write-Host ""

    Write-Host "> Step 2/4: Extracting archive..."
    Write-Host "  Extracting to: " -NoNewline
    Write-Host "$PST_DATA_DIR\source"
    Write-Host ""

    if (Test-Path $PST_DATA_DIR) {
        Remove-Item $PST_DATA_DIR -Recurse -Force
    }
    New-Item -ItemType Directory -Path $PST_DATA_DIR -Force | Out-Null

    try {
        $extractTmp = Join-Path $env:TEMP "PST_extract"
        if (Test-Path $extractTmp) { Remove-Item $extractTmp -Recurse -Force }
        Expand-Archive -Path $outputFilename -DestinationPath $extractTmp -Force

        $extractedDir = Join-Path $extractTmp $extractedDirName
        if (Test-Path $extractedDir) {
            Move-Item -Path $extractedDir -Destination (Join-Path $PST_DATA_DIR "source") -Force
        } else {
            Write-Host "x Error: Failed to find extracted directory."
            exit 1
        }
        Remove-Item $extractTmp -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "* Extraction Complete!"
    } catch {
        Write-Host ""
        Write-Host "x Error: Extraction failed. $($_.Exception.Message)"
        exit 1
    }
    Write-Host ""

    Write-Host "> Step 3/4: Cleaning up..."
    Write-Host ""

    try {
        Remove-Item $outputFilename -Force -ErrorAction Stop
        Write-Host "* Cleanup Complete!"
    } catch {
        Write-Host ""
        Write-Host "x Error: Failed to delete file. $($_.Exception.Message)"
    }
    Write-Host ""

    Write-Host "> Step 4/4: Finalizing..."
    Write-Host ""
    Set-Content -Path (Join-Path $PST_DATA_DIR "version") -Value $TagName -NoNewline
    Ensure-Uv
    New-PstLauncher
    Write-Host ""
}

function Install-PST {


    Write-Host ""
    Write-Host "Installing PalworldSaveTools"
    Write-Host ""

    Write-Host "> Fetching latest release info..."
    Write-Host ""

    $tagName = Get-LatestPstTag
    if (-not $tagName) {
        Write-Host ""
        Write-Host "x Error: Failed to find the latest release."
        Write-Host "  Check your internet connection."
        exit 1
    }

    Install-PstVersion -TagName $tagName


    Write-Host ""
    Write-Host -NoNewline "Setup Complete!"
    Write-Host ""
    Write-Host ""
    Write-Host "How to run:"
    Write-Host ""
    Write-Host "  pstm run"
    Write-Host ""
}

function Upgrade-PST {


    Write-Host ""

    if (-not (Test-Path $PST_DATA_DIR)) {
        Write-Host "x Error: PalworldSaveTools is not installed."
        Write-Host "  Run " -NoNewline
        Write-Host "pstm -i" -NoNewline
        Write-Host " to install first."
        exit 1
    }

    Write-Host "Upgrading PalworldSaveTools"
    Write-Host ""

    Write-Host "> Fetching latest release info..."
    Write-Host ""

    $tagName = Get-LatestPstTag
    if (-not $tagName) {
        Write-Host ""
        Write-Host "x Error: Failed to find the latest release."
        exit 1
    }

    $versionFile = Join-Path $PST_DATA_DIR "version"
    $installedVer = ""
    if (Test-Path $versionFile) {
        $installedVer = (Get-Content $versionFile -Raw).Trim()
    }

    if ($installedVer -and ($installedVer -eq $tagName)) {
        Write-Host "* Already up to date ($tagName)."
        Write-Host ""
        return
    }

    Install-PstVersion -TagName $tagName


    Write-Host ""
    Write-Host -NoNewline "Upgrade Complete!"
    Write-Host " ($tagName)"
    Write-Host ""
}

function Uninstall-PST {
    if (-not (Test-Path $PST_DATA_DIR)) {
        Write-Host "x PalworldSaveTools is not installed."
        exit 1
    }

    Write-Host "> This will delete: " -NoNewline
    Write-Host $PST_DATA_DIR
    $confirm = Read-Host -Prompt "> Are you sure? [y/N]"

    if ($confirm -match '^[Yy]$') {
        $shortcutPath = Join-Path $env:USERPROFILE "Desktop\PST.lnk"
        if (Test-Path $shortcutPath) { Remove-Item $shortcutPath -Force }
        Remove-Item $PST_DATA_DIR -Recurse -Force
        Write-Host "* PalworldSaveTools uninstalled successfully."
    } else {
        Write-Host "Cancelled."
    }
}

function Uninstall-All {
    Write-Host "> This will delete:"
    Write-Host "  $PST_DATA_DIR"
    Write-Host "  $PSTM_DIR"
    Write-Host ""
    $confirm = Read-Host -Prompt "> Are you sure? [y/N]"

    if ($confirm -match '^[Yy]$') {
        $shortcutPath = Join-Path $env:USERPROFILE "Desktop\PST.lnk"
        if (Test-Path $shortcutPath) { Remove-Item $shortcutPath -Force }
        if (Test-Path $PST_DATA_DIR) { Remove-Item $PST_DATA_DIR -Recurse -Force }

        Remove-Item $PSTM_DIR -Recurse -Force

        $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
        if ($userPath -like "*pstm*") {
            $newPath = ($userPath -split ';' | Where-Object { $_ -notlike "*pstm*" }) -join ';'
            [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
            Write-Host "> Removed pstm from user PATH."
        }

        Write-Host ""
        Write-Host "* pstm and PalworldSaveTools fully uninstalled."
    } else {
        Write-Host "Cancelled."
    }
}

function Show-Version {

    Write-Host "pstm      v$PSTM_VERSION"
    Write-Host "pst remote" -NoNewline; Write-Host " querying..."

    $tagName = Get-LatestPstTag
    if ($tagName) {
        Write-Host "pst latest" -NoNewline; Write-Host " $tagName"
    } else {
        Write-Host "pst latest" -NoNewline; Write-Host " unavailable"
    }

    $installedVer = "not installed"
    $versionFile = Join-Path $PST_DATA_DIR "version"
    if (Test-Path $versionFile) {
        $installedVer = (Get-Content $versionFile -Raw).Trim()
    } elseif (Test-Path $PST_DATA_DIR) {
        $installedVer = "installed"
    }
    Write-Host "pst local " -NoNewline; Write-Host " $installedVer"
}

function Open-GitHub {
    $url = "https://github.com/$PST_REPO"
    Write-Host "> Opening: " -NoNewline
    Write-Host $url
    Start-Process $url
}

function Run-PST {
    $pstPs1 = Join-Path $PST_DATA_DIR "pst.ps1"

    if (Test-Path $pstPs1) {
        & $pstPs1 @args
    } else {
        Write-Host "x Error: PalworldSaveTools is not installed."
        Write-Host "  Run " -NoNewline
        Write-Host "pstm -i" -NoNewline
        Write-Host " to install first."
        exit 1
    }
}

function Update-Self {
    Write-Host "> Checking for pstm update..."
    $tmpFile = Join-Path $env:TEMP "pstm_update.ps1"

    try {
        Invoke-WebRequest -Uri "${PSTM_RAW_BASE}/.Windows/pstm.ps1" -OutFile $tmpFile -UseBasicParsing
        if ((Test-Path $tmpFile) -and ((Get-Item $tmpFile).Length -gt 0)) {
            Move-Item -Path $tmpFile -Destination $PSTM_SCRIPT -Force
            Write-Host "* pstm updated successfully!"
        } else {
            Write-Host "x Error: Downloaded file is empty."
            Remove-Item $tmpFile -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Host "x Error: Failed to download update. $($_.Exception.Message)"
        Remove-Item $tmpFile -Force -ErrorAction SilentlyContinue
    }
}

$remoteVer = Get-LatestPstmVersion
if ($remoteVer) {
    $cmp = Compare-Versions -V1 $remoteVer -V2 $PSTM_VERSION
    if ($cmp -eq 1) {
        Write-Host "> pstm update available: v$PSTM_VERSION -> v$remoteVer (auto-updating...)"
        Update-Self
    } elseif ($cmp -eq -1) {
        Write-Host "Warning: local version (v$PSTM_VERSION) is ahead of remote (v$remoteVer). Skipping update."
    }
}

$command = if ($args.Count -gt 0) { $args[0] } else { "" }

switch ($command) {
    { $_ -in "-h", "--help", "" } { Show-Help }
    { $_ -in "-i", "-install" } { Install-PST }
    { $_ -in "-u", "-upgrade" } { Upgrade-PST }
    { $_ -in "-v", "-version" } { Show-Version }
    { $_ -in "-g", "-github" } { Open-GitHub }
    "run" { Run-PST }
    "-uninstall" { Uninstall-PST }
    "-uninstall-all" { Uninstall-All }
    "-update-self" {
    
        Write-Host "> Checking for pstm update..."
        $remoteVer = Get-LatestPstmVersion
        if (-not $remoteVer) {
            Write-Host "x Error: Could not fetch remote version."
        } else {
            $cmp = Compare-Versions -V1 $remoteVer -V2 $PSTM_VERSION
            if ($cmp -eq 1) {
                Update-Self
            } elseif ($cmp -eq 0) {
                Write-Host "* pstm is already up to date (v$PSTM_VERSION)."
            } else {
                Write-Host "Warning: local version (v$PSTM_VERSION) is ahead of remote (v$remoteVer). Skipping update."
            }
        }
    }
    default {
        Write-Host "x Unknown command: $command"
        Write-Host "  Run " -NoNewline
        Write-Host "pstm -h" -NoNewline
        Write-Host " for help."
        exit 1
    }
}
