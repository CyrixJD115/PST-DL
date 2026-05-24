#!/usr/bin/env python3
"""Build PST-Manager for one or all target platforms.

Usage:
    python3 build.py <ostype>

    <ostype> can be: linux, windows, mac, or all
"""

import os
import shutil
import stat
import subprocess
import sys
import urllib.request
import zipfile

PROJECT = "src/PSTManager/PSTManager.csproj"
DIST_DIR = "dist"
APPIMAGE_TOOL_URL = "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"

TARGETS = {
    "linux": {"rid": "linux-x64", "name": "linux"},
    "windows": {"rid": "win-x64", "name": "windows"},
    "mac": {"rid": "osx-x64", "name": "mac"},
}


def _run(cmd: list[str], **kwargs) -> subprocess.CompletedProcess:
    print(f"   $ {' '.join(cmd)}")
    return subprocess.run(cmd, check=True, **kwargs)


def publish(key: str) -> None:
    info = TARGETS[key]
    rid = info["rid"]
    name = info["name"]
    out_dir = os.path.join(DIST_DIR, name)
    print(f":: Publishing {name} ({rid}) -> {out_dir}")

    if os.path.exists(out_dir):
        shutil.rmtree(out_dir)

    cmd = [
        "dotnet", "publish", PROJECT,
        "-c", "Release",
        "-r", rid,
        "--self-contained", "true",
        "-o", out_dir,
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
    ]

    if key == "windows":
        cmd.append("-p:PublishSingleFile=true")
        cmd.append("-p:IncludeNativeLibrariesForSelfExtract=true")
        cmd.append("-p:EnableCompressionInSingleFile=true")

    _run(cmd)

    if key == "windows":
        exe_src = os.path.join(out_dir, "PSTManager.exe")
        if os.path.exists(exe_src):
            print(f"   Single-file exe: {os.path.getsize(exe_src) / 1024 / 1024:.1f} MB")
        _build_nsis()

    if key == "linux":
        _build_appimage(out_dir, name)

    if key == "mac":
        _zip_dir(out_dir, name)

    print(f"   Done: {out_dir}")
    print()


def _zip_dir(directory: str, name: str) -> None:
    zip_path = os.path.join(DIST_DIR, f"PST-Manager-{name}-x86_64.zip")
    print(f":: Creating zip archive -> {zip_path}")

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
        for root, dirs, files in os.walk(directory):
            for file in files:
                filepath = os.path.join(root, file)
                arcname = os.path.relpath(filepath, os.path.dirname(directory))
                zf.write(filepath, arcname)

    size_mb = os.path.getsize(zip_path) / 1024 / 1024
    print(f"   Archive: {zip_path} ({size_mb:.1f} MB)")


def _build_appimage(publish_dir: str, name: str) -> None:
    print(":: Building AppImage...")

    appdir = os.path.join(DIST_DIR, f"{name}.AppDir")
    usr_bin = os.path.join(appdir, "usr", "bin")

    if os.path.exists(appdir):
        shutil.rmtree(appdir)

    os.makedirs(usr_bin)

    for item in os.listdir(publish_dir):
        s = os.path.join(publish_dir, item)
        d = os.path.join(usr_bin, item)
        if os.path.isdir(s):
            shutil.copytree(s, d, symlinks=True)
        else:
            shutil.copy2(s, d)

    icon_src = os.path.join("assets", "icon.png")
    icon_dst = os.path.join(appdir, "pst-manager.png")
    if os.path.exists(icon_src):
        shutil.copy2(icon_src, icon_dst)

    desktop = os.path.join(appdir, "pst-manager.desktop")
    with open(desktop, "w") as f:
        f.write("[Desktop Entry]\n")
        f.write("Type=Application\n")
        f.write("Name=PST Manager\n")
        f.write("Comment=Palworld Save Tools Manager\n")
        f.write("Icon=pst-manager\n")
        f.write("Exec=PSTManager\n")
        f.write("Categories=Utility;\n")
        f.write("Terminal=false\n")

    apprun = os.path.join(appdir, "AppRun")
    with open(apprun, "w") as f:
        f.write("#!/bin/bash\n")
        f.write('HERE="$(dirname "$(readlink -f "$0")")"\n')
        f.write('export PATH="$HERE/usr/bin:$PATH"\n')
        f.write('export LD_LIBRARY_PATH="$HERE/usr/bin:$LD_LIBRARY_PATH"\n')
        f.write('exec "$HERE/usr/bin/PSTManager" "$@"\n')

    st = os.stat(apprun)
    os.chmod(apprun, st.st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)

    appimagetool = shutil.which("appimagetool")
    if appimagetool is None:
        temp_tool = os.path.join(DIST_DIR, ".appimagetool")
        if not os.path.exists(temp_tool):
            print("   appimagetool not found, downloading...")
            try:
                urllib.request.urlretrieve(APPIMAGE_TOOL_URL, temp_tool)
                os.chmod(temp_tool, os.stat(temp_tool).st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
                print("   Downloaded appimagetool")
            except Exception as e:
                print(f"   Warning: could not download appimagetool ({e})")
                print(f"   AppDir left at {appdir} — manually run: appimagetool {appdir}")
                return
        appimagetool = temp_tool

    output = os.path.join(DIST_DIR, f"PST-Manager-{name}-x86_64.AppImage")
    _run([appimagetool, appdir, output])

    if os.path.exists(appdir):
        shutil.rmtree(appdir)

    print(f"   AppImage created: {output}")


def _build_nsis() -> None:
    print(":: Building Windows installer...")
    makensis = shutil.which("makensis")
    if makensis is None:
        print("   Warning: makensis not found, skipping installer build")
        print("   Install NSIS: pacman -S nsis")
        return

    nsi_script = "installer.nsi"
    if not os.path.exists(nsi_script):
        print(f"   Warning: {nsi_script} not found, skipping installer build")
        return

    _run([makensis, "-V2", nsi_script])

    output = os.path.join(DIST_DIR, "PST-Manager-windows-x86_64-setup.exe")
    if os.path.exists(output):
        size_mb = os.path.getsize(output) / 1024 / 1024
        print(f"   Installer: {output} ({size_mb:.1f} MB)")
    else:
        print("   Warning: installer output not found")


def main():
    if len(sys.argv) < 2:
        print(__doc__.strip())
        sys.exit(1)

    target = sys.argv[1].lower()
    os.makedirs(DIST_DIR, exist_ok=True)

    if target == "all":
        for key in TARGETS:
            publish(key)
    elif target in TARGETS:
        publish(target)
    else:
        print(f"Unknown target: {target}")
        print(f"Valid options: {', '.join(TARGETS)} or 'all'")
        sys.exit(1)


if __name__ == "__main__":
    main()
