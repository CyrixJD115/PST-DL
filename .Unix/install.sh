#!/bin/bash

PSTM_REPO="CyrixJD115/PST-Manager"
PSTM_RAW_BASE="https://raw.githubusercontent.com/$PSTM_REPO/main"
PSTM_BIN="$HOME/.local/bin/pstm"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
BOLD='\033[1m'
DIM='\033[2m'
NC='\033[0m'

VERBOSE=0
case "${1:-}" in
    --verbose|-v)
        VERBOSE=1
        ;;
esac

echo ""
echo -e "${WHITE}"
cat << 'EOF'
██████╗ ███████╗████████╗███╗   ███╗
██╔══██╗██╔════╝╚══██╔══╝████╗ ████║
██████╔╝███████╗   ██║   ██╔████╔██║
██╔═══╝ ╚════██║   ██║   ██║╚██╔╝██║
██║     ███████║   ██║   ██║ ╚═╝ ██║
╚═╝     ╚══════╝   ╚═╝   ╚═╝     ╚═╝
EOF
echo -e "${NC}"
echo -e "${WHITE}════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${BOLD}${WHITE}Installing pstm...${NC}"
if [ $VERBOSE -eq 1 ]; then
    echo -e "${DIM}  (verbose mode enabled)${NC}"
fi
echo ""

mkdir -p "$HOME/.local/bin"

if [ $VERBOSE -eq 1 ]; then
    echo -e "${DIM}─ Download ───────────────────────────────────────────${NC}"
    echo -e "${CYAN}  URL: ${WHITE}${PSTM_RAW_BASE}/.Unix/pstm${NC}"
    echo -e "${CYAN}  To:  ${WHITE}$PSTM_BIN${NC}"
fi

echo -e "${YELLOW}> Downloading pstm...${NC}"
if [ $VERBOSE -eq 1 ]; then
    curl -L "${PSTM_RAW_BASE}/.Unix/pstm" -o "$PSTM_BIN"
else
    curl -sL "${PSTM_RAW_BASE}/.Unix/pstm" -o "$PSTM_BIN" 2>/dev/null
fi

if [ ! -f "$PSTM_BIN" ] || [ ! -s "$PSTM_BIN" ]; then
    echo -e "${RED}x Error: Failed to download pstm.${NC}"
    exit 1
fi

chmod +x "$PSTM_BIN"
echo -e "${GREEN}* Downloaded to: ${CYAN}$PSTM_BIN${NC}"
echo ""

if [ $VERBOSE -eq 1 ]; then
    echo -e "${DIM}─ Environment Info ─────────────────────────────────────${NC}"
    echo -e "${CYAN}  Platform:${NC}   ${WHITE}$OSTYPE${NC}"
    echo -e "${CYAN}  Shell:${NC}      ${WHITE}$SHELL${NC}"
    echo -e "${CYAN}  Config dir:${NC} ${WHITE}$HOME/.local/bin${NC}"
    echo ""
fi

SHELL_NAME=$(basename "$SHELL")
PATH_UPDATED=0

create_config_file() {
    local file="$1"
    if [ ! -f "$file" ]; then
        if [ $VERBOSE -eq 1 ]; then
            echo -e "${DIM}  Creating: ${CYAN}$file${NC}"
        fi
        touch "$file"
    fi
}

add_to_path() {
    local rc_file="$1"
    local line='export PATH="$HOME/.local/bin:$PATH"'

    create_config_file "$rc_file"
    if ! grep -qF '.local/bin' "$rc_file" 2>/dev/null; then
        if [ $VERBOSE -eq 1 ]; then
            echo -e "${DIM}  Adding PATH to: ${CYAN}$rc_file${NC}"
        fi
        echo "" >> "$rc_file"
        echo "$line" >> "$rc_file"
        PATH_UPDATED=1
    else
        if [ $VERBOSE -eq 1 ]; then
            echo -e "${DIM}  Already in PATH: ${DIM}${rc_file}${NC}"
        fi
    fi
}

case "$OSTYPE" in
    darwin*)
        if [ "$SHELL_NAME" = "zsh" ]; then
            add_to_path "$HOME/.zprofile"
            add_to_path "$HOME/.zshrc"
        elif [ "$SHELL_NAME" = "bash" ]; then
            add_to_path "$HOME/.bash_profile"
            add_to_path "$HOME/.bashrc"
        fi
        ;;
    *)
        if [ -f "$HOME/.bashrc" ]; then
            add_to_path "$HOME/.bashrc"
        fi
        if [ -f "$HOME/.zshrc" ]; then
            add_to_path "$HOME/.zshrc"
        fi
        ;;
esac

if [ -f "$HOME/.profile" ]; then
    add_to_path "$HOME/.profile"
fi

if [ $PATH_UPDATED -eq 1 ]; then
    echo -e "${GREEN}* Added ~/.local/bin to PATH${NC}"
else
    echo -e "${DIM}  ~/.local/bin already in PATH${NC}"
fi

if [ $VERBOSE -eq 1 ]; then
    echo ""
    echo -e "${DIM}─ Summary ────────────────────────────────────────────${NC}"
    echo -e "${CYAN}  Binary installed:${NC}   ${GREEN}yes${NC}"
    if [ $PATH_UPDATED -eq 1 ]; then
        echo -e "${CYAN}  PATH updated:${NC}      ${GREEN}yes${NC}"
    else
        echo -e "${CYAN}  PATH updated:${NC}      ${DIM}no (already configured)${NC}"
    fi
    echo ""
fi

echo ""
echo -e "${WHITE}════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${BOLD}${GREEN}pstm installed successfully!${NC}"
echo ""
echo -e "${BOLD}Next steps:${NC}"
echo ""
echo -e "  ${DIM}Reload your shell:${NC}"
case "$OSTYPE" in
    darwin*)
        if [ "$SHELL_NAME" = "zsh" ]; then
            echo -e "  ${CYAN}source ~/.zprofile${NC}"
        elif [ "$SHELL_NAME" = "bash" ]; then
            echo -e "  ${CYAN}source ~/.bash_profile${NC}"
        fi
        ;;
    *)
        echo -e "  ${CYAN}source ~/.bashrc${NC} ${DIM}(or ~/.zshrc)${NC}"
        ;;
esac
echo ""
echo -e "  ${DIM}Or open a new terminal window${NC}"
echo ""
echo -e "  ${DIM}Then install PalworldSaveTools:${NC}"
echo -e "  ${CYAN}pstm -i${NC}"
echo ""
echo -e "  ${DIM}Show all commands:${NC}"
echo -e "  ${CYAN}pstm -h${NC}"
echo ""
