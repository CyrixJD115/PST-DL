# Filename: download_palworld_save_tools.py

import requests
import os
import subprocess
import shutil

# --- ASCII Banner ---
print(r"""
 ____          ___                             ___       __                 
/\  _`\       /\_ \                           /\_ \     /\ \                
\ \ \L\ \ __  \//\ \   __  __  __    ___   _ _\//\ \    \_\ \               
 \ \ ,__/'__`\  \ \ \ /\ \/\ \/\ \  / __`\/\`'__\ \ \   /'_` \              
  \ \ \/\ \L\.\_ \_\ \\ \ \_/ \_/ \/\ \L\ \ \ \/ \_\ \_/\ \L\ \             
   \ \_\ \__/.\_\/\____\ \___x___/'\ \____/\ \_\ /\____\ \___,_\            
    \/_/\/__/\/_/\/____/\/__//__/   \/___/  \/_/ \/____/\/__,_ /            
 ____                                  ______               ___             
/\  _`\                               /\__  _\             /\_ \            
\ \,\L\_\     __    __  __    __      \/_/\ \/   ___     __\//\ \     ____  
 \/_\__ \   /'__`\ /\ \/\ \ /'__`\       \ \ \  / __`\  / __`\ \ \   /',__\ 
   /\ \L\ \/\ \L\.\\ \ \_/ /\  __/        \ \ \/\ \L\ \/\ \L\ \_\ \_/\__, `\
   \ `\____\ \__/.\_\ \___/\ \____\        \ \_\ \____/\ \____/\____\/\____/
    \/_____/\/__/\/_/\/__/  \/____/         \/_/\/___/  \/___/\/____/\/___/ 
"""
)
# --------------------

REPO_OWNER = "deafdudecomputers"
REPO_NAME = "PalworldSaveTools"
API_URL = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest"

print("--- Starting PalworldSaveTools Download (Python) ---")

# 1. Fetch the latest release information from the GitHub API
print("1. Querying GitHub API for latest release...")

try:
    response = requests.get(API_URL, timeout=10)
    response.raise_for_status()  # Raise an exception for bad status codes (4xx or 5xx)
    latest_release = response.json()
    tag_name = latest_release['tag_name']

except requests.exceptions.RequestException as e:
    print(f"\n❌ Error: Failed to fetch latest release information. {e}")
    print("Check your internet connection or the repository name.")
    exit(1)

if not tag_name:
    print("\n❌ Error: Could not find the latest release tag name.")
    exit(1)

# Extract version number (e.g., 'v1.0.0' -> '1.0.0')
version = tag_name.lstrip('v')

print(f"-> Latest version found: **{tag_name}**")

# 2. Construct the Download URL
DOWNLOAD_URL = (
    f"https://github.com/{REPO_OWNER}/{REPO_NAME}/releases/download/"
    f"{tag_name}/PST_standalone_v{version}.7z"
)
OUTPUT_FILENAME = f"PST_standalone_v{version}.7z"
DOWNLOAD_DIR = os.getcwd()

print(f"2. Constructed Download URL: **{DOWNLOAD_URL}**")
print(f"-> Output Filename: **{OUTPUT_FILENAME}**")

# 3. Download the file
print(f"3. Downloading file to {DOWNLOAD_DIR}...")

try:
    # Use streaming to handle large files and follow redirects
    with requests.get(DOWNLOAD_URL, stream=True) as r:
        r.raise_for_status()
        with open(OUTPUT_FILENAME, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192):
                f.write(chunk)

    print("\n✅ **Download Complete!**")
    print(f"File saved as: **{OUTPUT_FILENAME}**")

except requests.exceptions.RequestException as e:
    print(f"\n❌ Error: Failed to download the file. The asset might be missing. {e}")
    print(f"Attempted URL: {DOWNLOAD_URL}")
    exit(1)

# 4. Extract the 7z file
EXTRACT_DIR = f"PST_standalone_v{version}"
print(f"4. Extracting {OUTPUT_FILENAME} to folder '{EXTRACT_DIR}'...")

try:
    # Try using 7z command line tool
    result = subprocess.run(['7z', 'x', OUTPUT_FILENAME, f'-o{EXTRACT_DIR}', '-y'],
                          capture_output=True, text=True, check=True)
    print("\n✅ **Extraction Complete!**")
    print(f"Extracted to folder: **{EXTRACT_DIR}**")
except subprocess.CalledProcessError as e:
    print(f"\n❌ Error: Failed to extract the file with 7z. {e}")
    print("Trying alternative extraction methods...")
    try:
        # Try using 7za if 7z is not available
        result = subprocess.run(['7za', 'x', OUTPUT_FILENAME, f'-o{EXTRACT_DIR}', '-y'],
                              capture_output=True, text=True, check=True)
        print("\n✅ **Extraction Complete!**")
        print(f"Extracted to folder: **{EXTRACT_DIR}**")
    except subprocess.CalledProcessError as e2:
        print(f"\n❌ Error: Failed to extract the file with 7za. {e2}")
        try:
            # Try using py7zr library if available
            import py7zr # type: ignore
            with py7zr.SevenZipFile(OUTPUT_FILENAME, mode='r') as z:
                z.extractall(EXTRACT_DIR)
            print("\n✅ **Extraction Complete!**")
            print(f"Extracted to folder: **{EXTRACT_DIR}**")
        except ImportError:
            print("\n❌ Error: py7zr library not available. Please install 7-Zip command line tool or py7zr.")
            exit(1)
        except Exception as e3:
            print(f"\n❌ Error: Failed to extract the file with py7zr. {e3}")
            exit(1)
except FileNotFoundError:
    print("\n❌ Error: 7z command not found. Please install 7-Zip.")
    exit(1)

# 5. Delete the 7z file
print("5. Deleting the downloaded 7z file...")

try:
    os.remove(OUTPUT_FILENAME)
    print("\n✅ **Cleanup Complete!**")
    print(f"Deleted file: **{OUTPUT_FILENAME}**")
except OSError as e:
    print(f"\n❌ Error: Failed to delete the file. {e}")
    exit(1)

print("--- End of Script ---")
