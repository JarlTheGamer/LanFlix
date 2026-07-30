#!/usr/bin/env python3
"""
Lanflix OTA Publisher & Automated Build Tool
Automates building Android APK releases, packaging Lanflix Server releases,
calculating SHA-256 checksums, updating local OTA manifests, and Git push/tag releases.
"""

import os
import sys
import json
import hashlib
import shutil
import argparse
import subprocess
import re
from datetime import datetime
from pathlib import Path

# Paths relative to project root
SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
ANDROID_APP_DIR = PROJECT_ROOT / "build-tools" / "AndroidVersions" / "native-app"
SERVER_DIR = PROJECT_ROOT / "lanflix-server"
SERVER_PUBLISH_DIR = SERVER_DIR / "publish"
RELEASES_DIR = SERVER_PUBLISH_DIR / "releases"
DEV_RELEASES_DIR = SERVER_DIR / "app" / "WebApi" / "bin" / "Release" / "net9.0" / "releases"

def calculate_sha256(file_path: Path) -> str:
    """Calculates SHA-256 checksum of a file."""
    sha256_hash = hashlib.sha256()
    with open(file_path, "rb") as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            sha256_hash.update(byte_block)
    return sha256_hash.hexdigest()

def extract_android_version() -> tuple:
    """Extracts versionCode and versionName from build.gradle.kts."""
    gradle_file = ANDROID_APP_DIR / "app" / "build.gradle.kts"
    version_code = 1
    version_name = "1.0.0"

    if gradle_file.exists():
        content = gradle_file.read_text(encoding="utf-8")
        code_match = re.search(r'versionCode\s*=\s*(\d+)', content)
        name_match = re.search(r'versionName\s*=\s*"([^"]+)"', content)

        if code_match:
            version_code = int(code_match.group(1))
        if name_match:
            version_name = name_match.group(1)

    return version_code, version_name

def ensure_directories():
    """Ensures releases directories exist."""
    RELEASES_DIR.mkdir(parents=True, exist_ok=True)
    DEV_RELEASES_DIR.mkdir(parents=True, exist_ok=True)

def build_android_apk(notes: str = "Bug fixes and performance improvements", host_url: str = "http://lanflix.local:5037") -> Path:
    """Compiles Android APK and updates app-manifest.json."""
    print("=" * 60)
    print("🚀 [1/2] Building Android Native App APK...")
    print("=" * 60)

    ensure_directories()
    version_code, version_name = extract_android_version()
    print(f"📦 Version: {version_name} (Code: {version_code})")

    gradle_cmd = "gradlew.bat" if sys.platform == "win32" else "./gradlew"
    cmd = [str(ANDROID_APP_DIR / gradle_cmd), "assembleDebug"]

    print(f"⚙️ Running command: {' '.join(cmd)}")
    res = subprocess.run(cmd, cwd=ANDROID_APP_DIR, shell=(sys.platform == "win32"))
    if res.returncode != 0:
        print("❌ Android APK build failed!")
        sys.exit(1)

    apk_source = ANDROID_APP_DIR / "app" / "build" / "outputs" / "apk" / "debug" / "app-debug.apk"
    if not apk_source.exists():
        apks = list((ANDROID_APP_DIR / "app" / "build" / "outputs" / "apk").rglob("*.apk"))
        if apks:
            apk_source = apks[0]
        else:
            print("❌ Built APK file not found!")
            sys.exit(1)

    target_apk_name = f"lanflix-app-v{version_name}.apk"
    target_apk_path = RELEASES_DIR / target_apk_name
    shutil.copy2(apk_source, target_apk_path)
    shutil.copy2(apk_source, RELEASES_DIR / "app-release.apk")
    
    if DEV_RELEASES_DIR.exists():
        shutil.copy2(apk_source, DEV_RELEASES_DIR / target_apk_name)
        shutil.copy2(apk_source, DEV_RELEASES_DIR / "app-release.apk")

    file_size = target_apk_path.stat().st_size
    checksum = calculate_sha256(target_apk_path)

    manifest_data = {
        "versionName": version_name,
        "versionCode": version_code,
        "downloadUrl": f"{host_url.rstrip('/')}/api/app/download/{target_apk_name}",
        "releaseNotes": notes,
        "mandatory": False,
        "fileSize": file_size,
        "checksum": checksum,
        "updatedAt": datetime.utcnow().isoformat() + "Z"
    }

    manifest_path = RELEASES_DIR / "app-manifest.json"
    manifest_path.write_text(json.dumps(manifest_data, indent=2), encoding="utf-8")
    if DEV_RELEASES_DIR.exists():
        (DEV_RELEASES_DIR / "app-manifest.json").write_text(json.dumps(manifest_data, indent=2), encoding="utf-8")

    print(f"✅ Android APK build complete!")
    print(f"📄 Target APK: {target_apk_path}")
    print(f"🔑 SHA-256: {checksum}")
    print(f"📝 Manifest updated: {manifest_path}")

    return target_apk_path

def build_server(version: str = "1.0.0", notes: str = "Server update release", host_url: str = "http://lanflix.local:5037") -> Path:
    """Builds and packages the Lanflix C# Server and updates server-manifest.json."""
    print("=" * 60)
    print("🚀 [2/2] Building Lanflix C# Backend Server...")
    print("=" * 60)

    ensure_directories()

    build_script = SERVER_DIR.parent / "lanflix-server" / "build.ps1"
    if sys.platform == "win32" and build_script.exists():
        cmd = ["powershell", "-ExecutionPolicy", "Bypass", "-File", str(build_script)]
        print(f"⚙️ Running build script: {' '.join(cmd)}")
        res = subprocess.run(cmd, cwd=PROJECT_ROOT)
        if res.returncode != 0:
            print("❌ Server build script failed!")
            sys.exit(1)

    zip_file_name = f"lanflix-server-v{version}.zip"
    zip_target_path = RELEASES_DIR / zip_file_name

    print(f"📦 Creating server release package: {zip_target_path}")
    shutil.make_archive(str(zip_target_path).replace(".zip", ""), 'zip', SERVER_PUBLISH_DIR)

    file_size = zip_target_path.stat().st_size
    checksum = calculate_sha256(zip_target_path)

    manifest_data = {
        "version": version,
        "currentVersion": version,
        "releaseDate": datetime.utcnow().isoformat() + "Z",
        "downloadUrl": f"{host_url.rstrip('/')}/releases/{zip_file_name}",
        "fileSize": file_size,
        "checksum": checksum,
        "releaseNotes": notes,
        "isUpdateAvailable": True
    }

    manifest_path = RELEASES_DIR / "server-manifest.json"
    manifest_path.write_text(json.dumps(manifest_data, indent=2), encoding="utf-8")
    if DEV_RELEASES_DIR.exists():
        (DEV_RELEASES_DIR / "server-manifest.json").write_text(json.dumps(manifest_data, indent=2), encoding="utf-8")

    print(f"✅ Server package complete!")
    print(f"📄 Package ZIP: {zip_target_path}")
    print(f"🔑 SHA-256: {checksum}")
    print(f"📝 Manifest updated: {manifest_path}")

    return zip_target_path

def git_push_release(version: str, notes: str):
    """Performs Git add, commit, tag, and push to origin remote repository."""
    print("=" * 60)
    print("🐙 Executing Git Release Push...")
    print("=" * 60)

    tag_name = f"v{version}"
    commit_msg = f"release: Publish Lanflix OTA update {tag_name} - {notes}"

    subprocess.run(["git", "add", "-A"], cwd=PROJECT_ROOT)
    subprocess.run(["git", "commit", "-m", commit_msg], cwd=PROJECT_ROOT)
    subprocess.run(["git", "tag", "-fa", tag_name, "-m", commit_msg], cwd=PROJECT_ROOT)
    res = subprocess.run(["git", "push", "origin", "HEAD", "--tags", "--force"], cwd=PROJECT_ROOT)

    if res.returncode == 0:
        print(f"✅ Git push successful: Pushed commits & tag {tag_name} to origin!")
    else:
        print(f"⚠️ Git push completed with status code {res.returncode}")

def main():
    parser = argparse.ArgumentParser(description="Lanflix Automated OTA Build & Release Publisher Tool")
    parser.add_argument("--apk", action="store_true", help="Build and publish Android APK update")
    parser.add_argument("--server", action="store_true", help="Build and publish Server release ZIP")
    parser.add_argument("--all", action="store_true", help="Build and publish both APK and Server updates")
    parser.add_argument("--git", action="store_true", help="Automatically git add, commit, tag, and push release to remote repository")
    parser.add_argument("--notes", type=str, default="Release update with performance improvements and bug fixes", help="Release notes text")
    parser.add_argument("--version", type=str, default="1.0.0", help="Server release version string (e.g. 1.1.0)")
    parser.add_argument("--host", type=str, default="http://lanflix.local:5037", help="Base server host URL for download links")

    args = parser.parse_args()

    if not (args.apk or args.server or args.all):
        args.all = True

    print("\n🎬 LANFLIX OTA AUTOMATED PUBLISHER TOOL")
    print(f"🌐 Target Host: {args.host}\n")

    if args.apk or args.all:
        build_android_apk(notes=args.notes, host_url=args.host)

    if args.server or args.all:
        build_server(version=args.version, notes=args.notes, host_url=args.host)

    if args.git:
        git_push_release(version=args.version, notes=args.notes)

    print("\n🎉 OTA Publish Pipeline Completed Successfully!")

if __name__ == "__main__":
    main()
