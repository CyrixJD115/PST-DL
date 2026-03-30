# PST Manager

A cross-platform CLI version manager for [PalworldSaveTools](https://github.com/deafdudecomputers/PalworldSaveTools). Download, install, upgrade, and manage PalworldSaveTools from your terminal.

## Quick Install

**Linux / macOS:**
```bash
curl -LsSf https://raw.githubusercontent.com/CyrixJD115/PST-Manager/main/.Unix/install.sh | sh
```

**Windows (PowerShell):**
```powershell
irm https://raw.githubusercontent.com/CyrixJD115/PST-Manager/main/.Windows/install.ps1 | iex
```
## Commands

- `pstm` or `pstm -h` — Show help
- `pstm -i` or `pstm -install` — Download and install PalworldSaveTools
- `pstm -u` or `pstm -upgrade` — Update PalworldSaveTools to the latest version
- `pstm -v` or `pstm -version` — Show pstm and remote PST version
- `pstm -g` or `pstm-github` — Open PalworldSaveTools GitHub page
- `pstm -uninstall` — Uninstall PalworldSaveTools
- `pstm -uninstall-all` — Uninstall pstm and PalworldSaveTools
- `pstm -update-self` — Update pstm to the latest version

## Project Structure
```
PST-Manager/
├── .Unix/
│   ├── pstm              # CLI tool (bash) - Linux/macOS
│   └── install.sh        # Bootstrap installer (curl | sh)
├── .Windows/
│   ├── pstm.ps1          # CLI tool (PowerShell) - Windows
│   └── install.ps1       # Bootstrap installer (irm | iex)
└── README.md
```

## Install Locations

- **pstm binary**: `%LOCALAPPDATA%\pstm\pstm.ps1` (Windows) / `~/.local/bin/pstm` (Linux/macOS)
- **PST data**: `%LOCALAPPDATA%\palworldsavetools\` (Windows) / `~/.local/share/palworldsavetools/` (Linux/macOS)

### Directory Structure (both platforms)

```
<pst_data_dir>/
├── source/       # extracted source code
└── pst.ps1        # launcher script (`uv python install 3.13` + `uv run ./start.py`)

## How It Works (both platforms)
### Windows
- Downloads the source `.zip` from GitHub
- Extracts to `%LOCALAPPDATA%\palworldsavetools\source\`
- Installs [uv](https://github.com/astral-sh/uv) if not present
- Generates a `pst.ps1` launcher (`uv python install 3.13` + `uv run ./start.py`)

### Linux/macOS
- Downloads the source `.zip` from GitHub
- Extracts to `~/.local/share/palworldsavetools/source/`
- Auto-installs [uv](https://github.com/astral-sh/uv) if not present
- Generates a `pst` launcher (`uv python install 3.13` + `uv run ./start.py`)
## Requirements
### Windows
- PowerShell 5.1+ (no more 7-Zip!)

### Linux/macOS
- Bash, curl, unzip

## Auto-Update
pstm silently checks for updates on every run. If a newer version is found, it auto-updates in-place. You can also manually update with `pstm -update-self`.
## Credits
- Original PalworldSaveTools by [ deafdudecomputers](https://github.com/deafdudecomputers)
- PST Manager by [CyrixJD115](https://github.com/CyrixJD115)
