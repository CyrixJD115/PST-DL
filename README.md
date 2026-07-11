# PST Manager

A CLI version manager for [PalworldSaveTools](https://github.com/deafdudecomputers/PalworldSaveTools). Download, install, upgrade, and manage PalworldSaveTools from your terminal.

> **Windows support coming soon.**

## Quick Install

```bash
curl -LsSf https://raw.githubusercontent.com/CyrixJD115/PST-Manager/main/.Unix/install.sh | sh
```

**Verbose mode:**
```bash
curl -LsSf https://raw.githubusercontent.com/CyrixJD115/PST-Manager/main/.Unix/install.sh | sh -s -- --verbose
```

## Commands

- `pstm -h` or `pstm -help` - Show help
- `pstm -v` or `pstm -version` - Show pstm and remote PST version
- `pstm -i` or `pstm -install` - Download and install the latest PalworldSaveTools
- `pstm run` - Run PalworldSaveTools
- `pstm -u` or `pstm -upgrade` - Update PalworldSaveTools to the latest version
- `pstm -update-self` - Update pstm to the latest version
- `pstm -g` or `pstm -github` - Open PalworldSaveTools GitHub page
- `pstm -uninstall` - Uninstall PalworldSaveTools
- `pstm -uninstall-all` - Uninstall pstm and PalworldSaveTools

## Project Structure

```
PST-Manager/
└── .Unix/
    ├── pstm              # CLI tool (bash)
    └── install.sh        # Bootstrap installer (curl | sh)
```

## Install Locations

| Component | Path |
|-----------|------|
| pstm binary | `~/.local/bin/pstm` |
| PST data | `~/.local/share/palworldsavetools/` |

### Directory Structure

```
<pst_data_dir>/
├── source/       # extracted source code from .zip
└── pst           # launcher script
```

## How It Works

- Download the source `.zip` from GitHub tags
- Extract to `<data>/source/`
- Auto-install [uv](https://github.com/astral-sh/uv) if not present
- Generate a launcher that runs `uv python install 3.13` then `uv run ./start.py`

## Requirements

- Bash, curl, unzip

## Auto-Update

pstm silently checks for updates on every run. If a newer version is found, it auto-updates in-place. You can also manually update with `pstm -update-self`.

## Credits

- Original PalworldSaveTools by [deafdudecomputers](https://github.com/deafdudecomputers)
- PST Manager by [CyrixJD115](https://github.com/CyrixJD115)
