#!/usr/bin/env python3
"""
Lanflix OTA Publisher & Automated Build Tool
Automates building Android APK releases, packaging Lanflix Server releases,
calculating SHA-256 checksums, enforcing version downgrade protection via version.json,
updating local OTA manifests, and Git push/tag releases.
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
GITHUB_REPO = "JarlTheGamer/LanFlix"
VERSION_FILE = PROJECT_ROOT / "version.json"
ANDROID_APP_DIR = PROJECT_ROOT / "build-tools" / "AndroidVersions" / "native-app"
SERVER_DIR = PROJECT_ROOT / "lanflix-server"
SERVER_PUBLISH_DIR = SERVER_DIR / "publish"
RELEASES_DIR = PROJECT_ROOT / "releases"
DEV_RELEASES_DIR = SERVER_DIR / "app" / "WebApi" / "bin" / "Release" / "net9.0" / "releases"

def calculate_sha256(file_path: Path) -> str:
    """Calculates SHA-256 checksum of a file."""
    sha256_hash = hashlib.sha256()
    with open(file_path, "rb") as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            sha256_hash.update(byte_block)
    return sha256_hash.hexdigest()

def load_version_config() -> dict:
    """Loads central version configuration from version.json."""
    if VERSION_FILE.exists():
        try:
            return json.loads(VERSION_FILE.read_text(encoding="utf-8"))
        except Exception as e:
            print(f"⚠️ Error reading version.json: {e}")
    return {
        "serverVersion": "1.2.7",
        "serverBuildNumber": 27,
        "androidVersionName": "1.2.7",
        "androidVersionCode": 27,
        "minSupportedServerVersion": "1.0.0",
        "minSupportedAndroidVersionCode": 1
    }

def save_version_config(config: dict):
    """Saves updated version configuration to version.json."""
    config["lastUpdated"] = datetime.utcnow().isoformat() + "Z"
    VERSION_FILE.write_text(json.dumps(config, indent=2), encoding="utf-8")

def compare_versions(v1: str, v2: str) -> int:
    """Compares semantic version strings. Returns 1 if v1 > v2, -1 if v1 < v2, 0 if equal."""
    def parse(v):
        return [int(x) for x in re.sub(r'[^0-9.]', '', v).split('.') if x.isdigit()]
    p1, p2 = parse(v1), parse(v2)
    for a, b in zip(p1, p2):
        if a > b: return 1
        if a < b: return -1
    return len(p1) - len(p2)

def bump_patch_version(v: str) -> str:
    """Bumps patch component of a version string (e.g. 1.2.7 -> 1.2.8)."""
    parts = v.split('.')
    if len(parts) >= 3 and parts[2].isdigit():
        parts[2] = str(int(parts[2]) + 1)
        return '.'.join(parts)
    elif len(parts) == 2 and parts[1].isdigit():
        return f"{parts[0]}.{parts[1]}.1"
    return f"{v}.1"

def validate_and_bump_version(proposed_version: str = None, auto_bump: bool = False) -> tuple:
    """
    Validates version against version.json.
    Enforces that new version cannot be lower than previous version.
    Returns (version_name, version_code).
    """
    config = load_version_config()
    current_version = config.get("serverVersion", "1.2.7")
    current_code = config.get("serverBuildNumber", 27)

    if auto_bump or not proposed_version or proposed_version == "1.0.0":
        new_version = bump_patch_version(current_version)
    else:
        new_version = proposed_version

    if compare_versions(new_version, current_version) < 0:
        raise ValueError(
            f"❌ VERSION DOWNGRADE REJECTED!\n"
            f"Proposed version '{new_version}' is lower than current version '{current_version}' in version.json.\n"
            f"Please specify a higher version (e.g. --version {bump_patch_version(current_version)})."
        )

    new_code = current_code + 1 if compare_versions(new_version, current_version) > 0 else current_code
    config["serverVersion"] = new_version
    config["serverBuildNumber"] = new_code
    config["androidVersionName"] = new_version
    config["androidVersionCode"] = new_code

    save_version_config(config)
    sync_version_to_files(new_version, new_code)

    print(f"📌 Version Verified & Synced: {new_version} (Build: {new_code})")
    return new_version, new_code

def sync_version_to_files(version_name: str, version_code: int):
    """Syncs version strings across Android Gradle and settings.html."""
    gradle_file = ANDROID_APP_DIR / "app" / "build.gradle.kts"
    if gradle_file.exists():
        content = gradle_file.read_text(encoding="utf-8")
        content = re.sub(r'versionCode\s*=\s*\d+', f'versionCode = {version_code}', content)
        content = re.sub(r'versionName\s*=\s*"[^"]+"', f'versionName = "{version_name}"', content)
        gradle_file.write_text(content, encoding="utf-8")

    settings_file = SERVER_DIR / "app" / "WebApi" / "ClientApp" / "pages" / "settings.html"
    if settings_file.exists():
        content = settings_file.read_text(encoding="utf-8")
        content = re.sub(r'<meta name="app-version" content="[^"]+" />', f'<meta name="app-version" content="{version_name}" />', content)
        settings_file.write_text(content, encoding="utf-8")

def clean_old_releases(keep_latest_version: str = None):
    """Deletes old .zip and .apk release files from RELEASES_DIR."""
    if RELEASES_DIR.exists():
        for file in RELEASES_DIR.glob("*"):
            if file.is_file() and (file.name.endswith(".zip") or file.name.endswith(".apk")):
                if keep_latest_version and keep_latest_version in file.name:
                    continue
                try:
                    file.unlink()
                    print(f"🧹 Deleted old release file: {file.name}")
                except Exception as e:
                    print(f"⚠️ Could not delete old file {file.name}: {e}")

def ensure_directories():
    """Ensures releases directories exist."""
    RELEASES_DIR.mkdir(parents=True, exist_ok=True)
    DEV_RELEASES_DIR.mkdir(parents=True, exist_ok=True)

def build_android_apk(version_name: str, version_code: int, notes: str = "Bug fixes and performance improvements", host_url: str = "http://lanflix.local:5037") -> Path:
    """Compiles Android APK and updates app-manifest.json."""
    print("=" * 60)
    print("🚀 [1/2] Building Android Native App APK...")
    print("=" * 60)

    ensure_directories()
    clean_old_releases(keep_latest_version=version_name)
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

def build_server(version_name: str, notes: str = "Server update release", host_url: str = "http://lanflix.local:5037") -> Path:
    """Builds and packages the Lanflix C# Server and updates server-manifest.json."""
    print("=" * 60)
    print("🚀 [2/2] Building Lanflix C# Backend Server...")
    print("=" * 60)

    ensure_directories()
    clean_old_releases(keep_latest_version=version_name)

    # Delete any nested releases directory inside publish to prevent recursive zip bomb
    nested_releases = SERVER_PUBLISH_DIR / "releases"
    if nested_releases.exists():
        shutil.rmtree(nested_releases, ignore_errors=True)

    build_script = SERVER_DIR.parent / "lanflix-server" / "build.ps1"
    if sys.platform == "win32" and build_script.exists():
        cmd = ["powershell", "-ExecutionPolicy", "Bypass", "-File", str(build_script)]
        print(f"⚙️ Running build script: {' '.join(cmd)}")
        res = subprocess.run(cmd, cwd=PROJECT_ROOT)
        if res.returncode != 0:
            print("❌ Server build script failed!")
            sys.exit(1)

    # Clean nested releases again after build script
    if nested_releases.exists():
        shutil.rmtree(nested_releases, ignore_errors=True)

    zip_file_name = f"lanflix-server-v{version_name}.zip"
    zip_target_path = RELEASES_DIR / zip_file_name

    print(f"📦 Creating server release package: {zip_target_path}")
    shutil.make_archive(str(zip_target_path).replace(".zip", ""), 'zip', SERVER_PUBLISH_DIR)

    file_size = zip_target_path.stat().st_size
    checksum = calculate_sha256(zip_target_path)

    manifest_data = {
        "version": version_name,
        "currentVersion": version_name,
        "releaseDate": datetime.utcnow().isoformat() + "Z",
        "downloadUrl": f"https://github.com/{GITHUB_REPO}/releases/download/v{version_name}/{zip_file_name}",
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
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')

    parser = argparse.ArgumentParser(description="Lanflix Automated OTA Build & Release Publisher Tool")
    parser.add_argument("--apk", action="store_true", help="Build and publish Android APK update")
    parser.add_argument("--server", action="store_true", help="Build and publish Server release ZIP")
    parser.add_argument("--all", action="store_true", help="Build and publish both APK and Server updates")
    parser.add_argument("--git", action="store_true", help="Automatically git add, commit, tag, and push release to remote repository")
    parser.add_argument("--notes", type=str, default="Release update with performance improvements and bug fixes", help="Release notes text")
    parser.add_argument("--version", type=str, default=None, help="Server release version string (e.g. 1.2.8)")
    parser.add_argument("--bump", action="store_true", help="Auto-bump version number to next patch release")
    parser.add_argument("--host", type=str, default="http://lanflix.local:5037", help="Base server host URL for download links")

    args = parser.parse_args()

    if not (args.apk or args.server or args.all):
        args.all = True

    print("\n🎬 LANFLIX OTA AUTOMATED PUBLISHER TOOL")

    try:
        version_name, version_code = validate_and_bump_version(args.version, args.bump)
    except ValueError as e:
        print(e)
        sys.exit(1)

    print(f"🌐 Target Host: {args.host}\n")

    if args.apk or args.all:
        build_android_apk(version_name=version_name, version_code=version_code, notes=args.notes, host_url=args.host)

    if args.server or args.all:
        build_server(version_name=version_name, notes=args.notes, host_url=args.host)

    if args.git:
        git_push_release(version=version_name, notes=args.notes)

    print("\n🎉 OTA Publish Pipeline Completed Successfully!")

if __name__ == "__main__":
    main()
