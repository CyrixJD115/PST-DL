# PalworldSaveTools Nexus Download Script

A cross-platform download script that automatically fetches the latest release of PalworldSaveTools from GitHub, extracts it to a folder, and cleans up the archive file.

## Features

- **Cross-platform support**: Works on Windows, Linux, and macOS
- **Automatic latest version detection**: Always downloads the newest release
- **Automatic extraction**: Extracts the downloaded 7z file to a versioned folder
- **Cleanup**: Removes the downloaded archive after successful extraction
- **Error handling**: Comprehensive error checking and user-friendly messages

## Project Structure

```
Nexus_PST_Download_Script/
├── .Windows/
│   ├── PST_Downloader.ps1          # PowerShell script for Windows
│   └── DownloadPST_Windows.bat     # Batch file wrapper for PowerShell script
├── .Python/
│   └── DownloadPST_Python.py       # Python script (cross-platform)
├── .Unix/
│   └── DownloadPST_Linux-Mac.sh    # Bash script for Linux/macOS
└── README.md                       # This file
```

## Requirements

### Windows
- PowerShell 5.1 or later (included with Windows)
- 7-Zip or NanaZip (for extraction)
- curl.exe (included in Windows 10 1803+ and Windows 11, fallback available)

### Linux/macOS
- Bash shell
- curl
- p7zip (7z/7za command)

### Python (Cross-platform)
- Python 3.6+
- requests library (`pip install requests`)
- 7-Zip/NanaZip or p7zip (or py7zr as fallback)

## Usage

### Windows

```powershell
.\.Windows\DownloadPST_Windows.bat
```

### Linux/macOS

**Option 1: Fast Method (Recommended)**
```bash
sh ./.Unix/DownloadPST_Linux-Mac.sh
```

**Option 2: Fallback Method**
```bash
chmod +x ./.Unix/DownloadPST_Linux-Mac.sh
./.Unix/DownloadPST_Linux-Mac.sh
```

### Python (Any Platform)
```bash
python ./.Python/DownloadPST_Python.py
```

## What It Does

1. **Queries GitHub API** - Fetches information about the latest release
2. **Downloads the file** - Downloads the latest PST_standalone_v{version}.7z file
3. **Extracts the archive** - Extracts contents to a folder named `PST_standalone_v{version}`
4. **Cleans up** - Deletes the downloaded .7z file

## Output Example

```
--- Starting PalworldSaveTools Download (PowerShell) ---

1. Querying GitHub API for latest release...
-> Latest version found: **v1.1.51**

2. Constructed Download URL: **https://github.com/deafdudecomputers/PalworldSaveTools/releases/download/v1.1.51/PST_standalone_v1.1.51.7z**
-> Output Filename: **PST_standalone_v1.1.51.7z**

3. Downloading file to current directory...
✅ Download Complete!
File saved as: **PST_standalone_v1.1.51.7z**

4. Extracting PST_standalone_v1.1.51.7z to folder 'PST_standalone_v1.1.51'...
✅ Extraction Complete!
Extracted to folder: **PST_standalone_v1.1.51**

5. Deleting the downloaded 7z file...
✅ Cleanup Complete!
Deleted file: **PST_standalone_v1.1.51.7z**

--- End of Script ---
```

## Troubleshooting

### Windows Issues

**"7-Zip is not installed"**
- Install 7-Zip from https://www.7-zip.org/
- Or install NanaZip from Microsoft Store (recommended for Windows 11)

**Slow downloads**
- The script automatically uses curl.exe if available (fast)
- Falls back to PowerShell's Invoke-WebRequest if curl is not found

### Linux/macOS Issues

**"7z command not found"**
```bash
# Ubuntu/Debian
sudo apt-get install p7zip-full

# CentOS/RHEL/Fedora
sudo yum install p7zip  # or dnf install p7zip

# macOS
brew install p7zip
```

### Python Issues

**"requests module not found"**
```bash
pip install requests
```

**Extraction fails**
- Install 7-Zip/p7zip, or
- Install py7zr: `pip install py7zr`

## Performance Notes

- **PowerShell**: Uses curl.exe for fast downloads when available
- **Python**: Uses streaming downloads for memory efficiency
- **Bash**: Uses optimized curl for maximum speed

## Security

- Only downloads from the official GitHub repository
- Validates HTTPS connections
- No elevated privileges required
- Scripts can be inspected before running

## License

This project follows the same license as PalworldSaveTools.

## Credits

- Original PalworldSaveTools by deafdudecomputers
- Download automation scripts for easy access
