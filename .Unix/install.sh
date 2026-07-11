#!/bin/bash

PSTM_REPO="CyrixJD115/PST-Manager"
PSTM_RAW_BASE="https://raw.githubusercontent.com/$PSTM_REPO/main"
PSTM_BIN="$HOME/.local/bin/pstm"

VERBOSE=0
case "${1:-}" in
    --verbose|-v)
        VERBOSE=1
        ;;
esac

echo ""
echo "Installing pstm..."
if [ $VERBOSE -eq 1 ]; then
    echo "  (verbose mode enabled)"
fi
echo ""

mkdir -p "$HOME/.local/bin"

if [ $VERBOSE -eq 1 ]; then
    echo "  URL: ${PSTM_RAW_BASE}/.Unix/pstm"
    echo "  To:  $PSTM_BIN"
fi

echo "> Downloading pstm..."
if [ $VERBOSE -eq 1 ]; then
    curl -L "${PSTM_RAW_BASE}/.Unix/pstm" -o "$PSTM_BIN"
else
    curl -sL "${PSTM_RAW_BASE}/.Unix/pstm" -o "$PSTM_BIN" 2>/dev/null
fi

if [ ! -f "$PSTM_BIN" ] || [ ! -s "$PSTM_BIN" ]; then
    echo "x Error: Failed to download pstm."
    exit 1
fi

chmod +x "$PSTM_BIN"
echo "* Downloaded to: $PSTM_BIN"
echo ""

if [ $VERBOSE -eq 1 ]; then
    echo "  Platform:   $OSTYPE"
    echo "  Shell:      $SHELL"
    echo "  Config dir: $HOME/.local/bin"
    echo ""
fi

SHELL_NAME=$(basename "$SHELL")
PATH_UPDATED=0

create_config_file() {
    local file="$1"
    if [ ! -f "$file" ]; then
        if [ $VERBOSE -eq 1 ]; then
            echo "  Creating: $file"
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
            echo "  Adding PATH to: $rc_file"
        fi
        echo "" >> "$rc_file"
        echo "$line" >> "$rc_file"
        PATH_UPDATED=1
    else
        if [ $VERBOSE -eq 1 ]; then
            echo "  Already in PATH: $rc_file"
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
    echo "* Added ~/.local/bin to PATH"
else
    echo "  ~/.local/bin already in PATH"
fi

if [ $VERBOSE -eq 1 ]; then
    echo ""
    echo "  Binary installed:   yes"
    if [ $PATH_UPDATED -eq 1 ]; then
        echo "  PATH updated:      yes"
    else
        echo "  PATH updated:      no (already configured)"
    fi
    echo ""
fi

echo ""
echo "pstm installed successfully!"
echo ""
echo "Reload your shell or open a new terminal, then:"
echo ""
echo "  pstm -i    Install PalworldSaveTools"
echo "  pstm -h    Show all commands"
echo ""
