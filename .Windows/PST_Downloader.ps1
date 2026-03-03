# Filename: DownloadPST_Windows.ps1

$REPO_OWNER = "deafdudecomputers"
$REPO_NAME = "PalworldSaveTools"
$API_URL = "https://api.github.com/repos/$REPO_OWNER/$REPO_NAME/releases/latest"

Write-Host "--- Starting PalworldSaveTools Download (PowerShell) ---"

# 1. Fetch the latest release information from the GitHub API
Write-Host "1. Querying GitHub API for latest release..."

try {
    $response = Invoke-WebRequest -Uri $API_URL -UseBasicParsing
    $json = $response.Content | ConvertFrom-Json
    $tag_name = $json.tag_name
} catch {
    Write-Host ""
    Write-Host "❌ Error: Failed to fetch latest release information. $($_.Exception.Message)"
    Write-Host "Check your internet connection or the repository name."
    exit 1
}

if (-not $tag_name) {
    Write-Host ""
    Write-Host "❌ Error: Could not find the latest release tag name."
    exit 1
}

# Extract version number (e.g., 'v1.0.0' -> '1.0.0')
$version = $tag_name.TrimStart('v')

Write-Host "-> Latest version found: **$tag_name**"

# 2. Construct the Download URL
$DOWNLOAD_URL = "https://github.com/$REPO_OWNER/$REPO_NAME/releases/download/$tag_name/PST_standalone_v$version.7z"
$OUTPUT_FILENAME = "PST_standalone_v$version.7z"

Write-Host "2. Constructed Download URL: **$DOWNLOAD_URL**"
Write-Host "-> Output Filename: **$OUTPUT_FILENAME**"

# 3. Download the file
Write-Host "3. Downloading file to current directory..."

try {
    # Try using curl.exe if available (much faster than Invoke-WebRequest)
    if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
        & curl.exe -L -o $OUTPUT_FILENAME $DOWNLOAD_URL
        if ($LASTEXITCODE -ne 0) {
            throw "curl.exe failed with exit code $LASTEXITCODE"
        }
    } else {
        # Fallback to Invoke-WebRequest
        Invoke-WebRequest -Uri $DOWNLOAD_URL -OutFile $OUTPUT_FILENAME
    }
    Write-Host ""
    Write-Host "✅ **Download Complete!**"
    Write-Host "File saved as: **$OUTPUT_FILENAME**"
} catch {
    Write-Host "❌ Error: Failed to download the file. The asset might be missing. $($_.Exception.Message)"
    Write-Host "Attempted URL: $DOWNLOAD_URL"
    exit 1
}

# 4. Extract the 7z file
$EXTRACT_DIR = "PST_standalone_v$version"
Write-Host "4. Extracting $OUTPUT_FILENAME to folder '$EXTRACT_DIR'..."

try {
    if (Get-Command 7z -ErrorAction SilentlyContinue) {
        & 7z x "$OUTPUT_FILENAME" -o"$EXTRACT_DIR" -y
    } elseif (Test-Path "C:\Program Files\7-Zip\7z.exe") {
        & "C:\Program Files\7-Zip\7z.exe" x "$OUTPUT_FILENAME" -o"$EXTRACT_DIR" -y
    } elseif (Test-Path "C:\Program Files (x86)\7-Zip\7z.exe") {
        & "C:\Program Files (x86)\7-Zip\7z.exe" x "$OUTPUT_FILENAME" -o"$EXTRACT_DIR" -y
    } else {
        Write-Host "❌ Error: 7-Zip is not installed or not found in PATH. Please install 7-Zip to extract the file."
        exit 1
    }
    Write-Host ""
    Write-Host "✅ **Extraction Complete!**"
    Write-Host "Extracted to folder: **$EXTRACT_DIR**"
} catch {
    Write-Host "❌ Error: Failed to extract the file. $($_.Exception.Message)"
    exit 1
}

# 5. Delete the 7z file
Write-Host "5. Deleting the downloaded 7z file..."

try {
    Remove-Item $OUTPUT_FILENAME -Force
    Write-Host ""
    Write-Host "✅ **Cleanup Complete!**"
    Write-Host "Deleted file: **$OUTPUT_FILENAME**"
} catch {
    Write-Host "❌ Error: Failed to delete the file. $($_.Exception.Message)"
    exit 1
}

Write-Host "--- End of Script ---"
