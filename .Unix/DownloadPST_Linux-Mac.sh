#!/bin/bash
# Filename: DownloadPST_Linux-Mac.sh

# --- ASCII Banner ---
echo ""
echo " ____          ___                             ___       __                 "
echo "/\  _\`       /\_ \                           /\_ \     /\ \                 "
echo "\ \ \L\ \ __  \//\ \   __  __  __    ___   _ _\//\ \    \_\ \                "
echo " \ \ ,__/'__\`  \ \ \ /\ \/\ \/\ \  / __\`/\`'\__\ \ \   /'_ \` \               "
echo "  \ \ \/\ \L\.\_ \_\ \\\ \ \_/ \_/ \/\ \L\ \ \ \/ \_\ \_/\ \L\ \              "
echo "   \ \_\ \__/.\_\/\____\ \___x___/'\ \____/\ \_\ /\____\ \___,_\             "
echo "    \/_/\/__/\/_/\/____/\/__//__/   \/___/  \/_/ \/____/\/__,_ /             "
echo " ____                                  ______               ___             "
echo "/\  _\`                               /\__  _\             /\_ \             "
echo "\ \,\L\_\     __    __  __    __      \/_/\ \/   ___     __\//\ \     ____  "
echo " \/_\__ \   /'__\` /\ \/\ \ /'__\`       \ \ \  / __\`  / __\` \ \ \   /',__\ "
echo "   /\ \L\ \/\ \L\.\\\ \ \_/ /\  __/        \ \ \/\ \L\ \/\ \L\ \_\ \_/\__, `\"
echo "   \ \`\____\ \__/.\_\ \___/\ \____\        \ \_\ \____/\ \____/\____\/\____/"
echo "    \/_____/\/__/\/_/\/__/  \/____/         \/_/\/___/  \/___/\/____/\/___/ "
echo ""
# --------------------

REPO_OWNER="deafdudecomputers"
REPO_NAME="PalworldSaveTools"
API_URL="https://api.github.com/repos/$REPO_OWNER/$REPO_NAME/releases/latest"

echo "--- Starting PalworldSaveTools Download (Bash) ---"

# 1. Fetch the latest release information from the GitHub API
echo "1. Querying GitHub API for latest release..."

TAG_NAME=$(curl -s "$API_URL" | grep '"tag_name"' | sed -E 's/.*"tag_name":\s*"([^"]+)".*/\1/')

if [ -z "$TAG_NAME" ]; then
    echo ""
    echo "❌ Error: Failed to find the latest release tag name. Check internet or repository."
    exit 1
fi

VERSION="${TAG_NAME#v}"
echo "-> Latest version found: **$TAG_NAME**"

# 2. Construct the Download URL
DOWNLOAD_URL="https://github.com/$REPO_OWNER/$REPO_NAME/releases/download/$TAG_NAME/PST_standalone_v$VERSION.7z"
OUTPUT_FILENAME="PST_standalone_v$VERSION.7z"

echo "2. Constructed Download URL: **$DOWNLOAD_URL**"
echo "-> Output Filename: **$OUTPUT_FILENAME**"

# 3. Download the file
echo "3. Downloading file to current directory..."
curl -L -o "$OUTPUT_FILENAME" "$DOWNLOAD_URL"

if [ -f "$OUTPUT_FILENAME" ]; then
    echo ""
    echo "✅ **Download Complete!**"
    echo "File saved as: **$OUTPUT_FILENAME**"
else
    echo "❌ Error: Download failed. The file was not created."
    exit 1
fi

# 4. Extract the 7z file
EXTRACT_DIR="PST_standalone_v$VERSION"
echo "4. Extracting $OUTPUT_FILENAME to folder '$EXTRACT_DIR'..."

if command -v 7z >/dev/null 2>&1; then
    7z x "$OUTPUT_FILENAME" -o"$EXTRACT_DIR" -y
    if [ $? -eq 0 ]; then
        echo ""
        echo "✅ **Extraction Complete!**"
        echo "Extracted to folder: **$EXTRACT_DIR**"
    else
        echo "❌ Error: Failed to extract the file with 7z."
        exit 1
    fi
elif command -v 7za >/dev/null 2>&1; then
    7za x "$OUTPUT_FILENAME" -o"$EXTRACT_DIR" -y
    if [ $? -eq 0 ]; then
        echo ""
        echo "✅ **Extraction Complete!**"
        echo "Extracted to folder: **$EXTRACT_DIR**"
    else
        echo "❌ Error: Failed to extract the file with 7za."
        exit 1
    fi
else
    echo "❌ Error: 7z or 7za command not found. Please install p7zip package."
    exit 1
fi

# 5. Delete the 7z file
echo "5. Deleting the downloaded 7z file..."

if rm "$OUTPUT_FILENAME"; then
    echo ""
    echo "✅ **Cleanup Complete!**"
    echo "Deleted file: **$OUTPUT_FILENAME**"
else
    echo "❌ Error: Failed to delete the file."
    exit 1
fi

echo "--- End of Script ---"
