#!/usr/bin/env python3
"""
Lanflix OTA Publisher GUI
A modern dark-themed GUI desktop tool to automate building, checksumming,
git pushing, and publishing OTA updates for the Lanflix Android App and Lanflix C# Server.
"""

import os
import sys
import json
import hashlib
import shutil
import subprocess
import re
import threading
from datetime import datetime
from pathlib import Path
import tkinter as tk
from tkinter import ttk, messagebox, scrolledtext

# Directory Paths
SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
ANDROID_APP_DIR = PROJECT_ROOT / "build-tools" / "AndroidVersions" / "native-app"
SERVER_DIR = PROJECT_ROOT / "lanflix-server"
SERVER_PUBLISH_DIR = SERVER_DIR / "publish"
RELEASES_DIR = SERVER_PUBLISH_DIR / "releases"
DEV_RELEASES_DIR = SERVER_DIR / "app" / "WebApi" / "bin" / "Release" / "net9.0" / "releases"

class OtaPublisherGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Lanflix OTA Release Publisher")
        self.root.geometry("820x700")
        self.root.configure(bg="#0D0D11")

        self.setup_styles()
        self.build_ui()
        self.load_current_versions()

    def setup_styles(self):
        self.style = ttk.Style()
        self.style.theme_use("clamp")

        self.style.configure("TFrame", background="#0D0D11")
        self.style.configure("TLabel", background="#0D0D11", foreground="#FFFFFF", font=("Segoe UI", 10))
        self.style.configure("Header.TLabel", background="#0D0D11", foreground="#FFFFFF", font=("Segoe UI", 16, "bold"))
        self.style.configure("SubHeader.TLabel", background="#0D0D11", foreground="#9E9EA9", font=("Segoe UI", 9))
        self.style.configure("Card.TFrame", background="#16161F", relief="flat")
        self.style.configure("TEntry", fieldbackground="#16161F", foreground="#FFFFFF", insertcolor="#FFFFFF")

    def build_ui(self):
        # Header Container
        header_frame = tk.Frame(self.root, bg="#0D0D11", padx=20, pady=16)
        header_frame.pack(fill="x")

        title_lbl = tk.Label(header_frame, text="LANFLIX OTA PUBLISHER", font=("Segoe UI", 18, "bold"), fg="#FFFFFF", bg="#0D0D11")
        title_lbl.pack(anchor="w")

        subtitle_lbl = tk.Label(header_frame, text="Automated APK & Server Package Manager • SHA-256 Checksums • Git & GitHub Push", font=("Segoe UI", 9), fg="#9E9EA9", bg="#0D0D11")
        subtitle_lbl.pack(anchor="w", pady=(2, 0))

        # Main Card Panel
        main_card = tk.Frame(self.root, bg="#16161F", padx=20, pady=16, bd=1, relief="solid")
        main_card.pack(fill="x", padx=20, pady=5)

        # Form Fields Grid
        tk.Label(main_card, text="Target Server Host URL:", font=("Segoe UI", 10, "bold"), fg="#FFFFFF", bg="#16161F").grid(row=0, column=0, sticky="w", pady=6)
        self.host_entry = tk.Entry(main_card, font=("Segoe UI", 10), bg="#222230", fg="#FFFFFF", insertbackground="white", bd=1, relief="flat")
        self.host_entry.insert(0, "http://lanflix.local:5037")
        self.host_entry.grid(row=0, column=1, sticky="ew", padx=(10, 0), pady=6)

        tk.Label(main_card, text="Server Release Version:", font=("Segoe UI", 10, "bold"), fg="#FFFFFF", bg="#16161F").grid(row=1, column=0, sticky="w", pady=6)
        self.server_ver_entry = tk.Entry(main_card, font=("Segoe UI", 10), bg="#222230", fg="#FFFFFF", insertbackground="white", bd=1, relief="flat")
        self.server_ver_entry.insert(0, "1.0.0")
        self.server_ver_entry.grid(row=1, column=1, sticky="ew", padx=(10, 0), pady=6)

        tk.Label(main_card, text="Detected Android Version:", font=("Segoe UI", 10, "bold"), fg="#FFFFFF", bg="#16161F").grid(row=2, column=0, sticky="w", pady=6)
        self.apk_ver_label = tk.Label(main_card, text="Reading gradle...", font=("Segoe UI", 10), fg="#E50914", bg="#16161F")
        self.apk_ver_label.grid(row=2, column=1, sticky="w", padx=(10, 0), pady=6)

        tk.Label(main_card, text="Release Notes:", font=("Segoe UI", 10, "bold"), fg="#FFFFFF", bg="#16161F").grid(row=3, column=0, sticky="nw", pady=6)
        self.notes_entry = tk.Entry(main_card, font=("Segoe UI", 10), bg="#222230", fg="#FFFFFF", insertbackground="white", bd=1, relief="flat")
        self.notes_entry.insert(0, "In-app OTA updates, Home Assistant mDNS discovery, & performance fixes")
        self.notes_entry.grid(row=3, column=1, sticky="ew", padx=(10, 0), pady=6)

        # Git Integration Checkbox
        self.git_push_var = tk.BooleanVar(value=True)
        self.git_check = tk.Checkbutton(
            main_card,
            text="Git Add, Commit, Tag & Push Release to Remote Repository",
            variable=self.git_push_var,
            font=("Segoe UI", 10),
            fg="#FFFFFF",
            bg="#16161F",
            activebackground="#16161F",
            activeforeground="#FFFFFF",
            selectcolor="#222230"
        )
        self.git_check.grid(row=4, column=0, columnspan=2, sticky="w", pady=(10, 4))

        main_card.columnconfigure(1, weight=1)

        # Action Buttons Container
        btn_frame = tk.Frame(self.root, bg="#0D0D11", padx=20, pady=10)
        btn_frame.pack(fill="x")

        self.btn_apk = tk.Button(btn_frame, text="📱 Build & Publish APK", font=("Segoe UI", 10, "bold"), bg="#E50914", fg="white", activebackground="#B20710", activeforeground="white", bd=0, padding=(12, 8), command=self.on_build_apk)
        self.btn_apk.pack(side="left", padx=(0, 10))

        self.btn_server = tk.Button(btn_frame, text="📦 Package Server ZIP", font=("Segoe UI", 10, "bold"), bg="#222230", fg="white", activebackground="#2A2A3A", activeforeground="white", bd=0, padding=(12, 8), command=self.on_build_server)
        self.btn_server.pack(side="left", padx=(0, 10))

        self.btn_all = tk.Button(btn_frame, text="⚡ Publish ALL & Push Git", font=("Segoe UI", 10, "bold"), bg="#10B981", fg="white", activebackground="#059669", activeforeground="white", bd=0, padding=(14, 8), command=self.on_build_all)
        self.btn_all.pack(side="right")

        # Progress / Output Log Console
        log_frame = tk.Frame(self.root, bg="#0D0D11", padx=20, pady=10)
        log_frame.pack(fill="both", expand=True)

        tk.Label(log_frame, text="Live Output Log:", font=("Segoe UI", 10, "bold"), fg="#FFFFFF", bg="#0D0D11").pack(anchor="w", pady=(0, 4))

        self.log_text = scrolledtext.ScrolledText(log_frame, bg="#050507", fg="#10B981", font=("Consolas", 9), bd=1, relief="solid")
        self.log_text.pack(fill="both", expand=True)

    def log(self, message: str):
        def _append():
            self.log_text.insert(tk.END, message + "\n")
            self.log_text.see(tk.END)
        self.root.after(0, _append)

    def load_current_versions(self):
        gradle_file = ANDROID_APP_DIR / "app" / "build.gradle.kts"
        if gradle_file.exists():
            content = gradle_file.read_text(encoding="utf-8")
            code_match = re.search(r'versionCode\s*=\s*(\d+)', content)
            name_match = re.search(r'versionName\s*=\s*"([^"]+)"', content)
            if code_match and name_match:
                self.apk_ver_label.config(text=f"v{name_match.group(1)} (Code: {code_match.group(1)})")

    def toggle_buttons(self, state: bool):
        st = "normal" if state else "disabled"
        self.btn_apk.config(state=st)
        self.btn_server.config(state=st)
        self.btn_all.config(state=st)

    def on_build_apk(self):
        threading.Thread(target=self._worker_apk, daemon=True).start()

    def on_build_server(self):
        threading.Thread(target=self._worker_server, daemon=True).start()

    def on_build_all(self):
        threading.Thread(target=self._worker_all, daemon=True).start()

    def _worker_apk(self):
        self.toggle_buttons(False)
        try:
            self.build_apk_logic()
            if self.git_push_var.get():
                self.git_push_logic()
        finally:
            self.toggle_buttons(True)

    def _worker_server(self):
        self.toggle_buttons(False)
        try:
            self.build_server_logic()
            if self.git_push_var.get():
                self.git_push_logic()
        finally:
            self.toggle_buttons(True)

    def _worker_all(self):
        self.toggle_buttons(False)
        try:
            self.build_apk_logic()
            self.build_server_logic()
            if self.git_push_var.get():
                self.git_push_logic()
        finally:
            self.toggle_buttons(True)

    def calculate_sha256(self, file_path: Path) -> str:
        sha256_hash = hashlib.sha256()
        with open(file_path, "rb") as f:
            for byte_block in iter(lambda: f.read(4096), b""):
                sha256_hash.update(byte_block)
        return sha256_hash.hexdigest()

    def build_apk_logic(self):
        self.log("=" * 60)
        self.log("🚀 Building Android APK Package...")
        self.log("=" * 60)

        RELEASES_DIR.mkdir(parents=True, exist_ok=True)
        DEV_RELEASES_DIR.mkdir(parents=True, exist_ok=True)

        gradle_file = ANDROID_APP_DIR / "app" / "build.gradle.kts"
        version_code, version_name = 1, "1.0.0"
        if gradle_file.exists():
            content = gradle_file.read_text(encoding="utf-8")
            code_m = re.search(r'versionCode\s*=\s*(\d+)', content)
            name_m = re.search(r'versionName\s*=\s*"([^"]+)"', content)
            if code_m and name_m:
                version_code = int(code_m.group(1))
                version_name = name_m.group(1)

        self.log(f"📦 Version: {version_name} (Code: {version_code})")

        gradle_cmd = "gradlew.bat" if sys.platform == "win32" else "./gradlew"
        cmd = [str(ANDROID_APP_DIR / gradle_cmd), "assembleDebug"]

        res = subprocess.run(cmd, cwd=ANDROID_APP_DIR, shell=(sys.platform == "win32"), capture_output=True, text=True)
        if res.returncode != 0:
            self.log(f"❌ APK Build Failed:\n{res.stderr}")
            return

        apk_source = ANDROID_APP_DIR / "app" / "build" / "outputs" / "apk" / "debug" / "app-debug.apk"
        if not apk_source.exists():
            apks = list((ANDROID_APP_DIR / "app" / "build" / "outputs" / "apk").rglob("*.apk"))
            if apks:
                apk_source = apks[0]
            else:
                self.log("❌ Could not locate generated APK file!")
                return

        target_apk_name = f"lanflix-app-v{version_name}.apk"
        target_apk_path = RELEASES_DIR / target_apk_name
        shutil.copy2(apk_source, target_apk_path)
        shutil.copy2(apk_source, RELEASES_DIR / "app-release.apk")
        if DEV_RELEASES_DIR.exists():
            shutil.copy2(apk_source, DEV_RELEASES_DIR / target_apk_name)
            shutil.copy2(apk_source, DEV_RELEASES_DIR / "app-release.apk")

        checksum = self.calculate_sha256(target_apk_path)
        file_size = target_apk_path.stat().st_size
        host_url = self.host_entry.get().strip().rstrip("/")
        notes = self.notes_entry.get().strip()

        manifest_data = {
            "versionName": version_name,
            "versionCode": version_code,
            "downloadUrl": f"{host_url}/api/app/download/{target_apk_name}",
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

        self.log(f"✅ APK Build Complete: {target_apk_path.name}")
        self.log(f"🔑 SHA-256 Checksum: {checksum}")
        self.log(f"📝 App Manifest Updated: {manifest_path}")

    def build_server_logic(self):
        self.log("=" * 60)
        self.log("🚀 Building Lanflix C# Backend Server Package...")
        self.log("=" * 60)

        RELEASES_DIR.mkdir(parents=True, exist_ok=True)
        DEV_RELEASES_DIR.mkdir(parents=True, exist_ok=True)

        build_script = PROJECT_ROOT / "lanflix-server" / "build.ps1"
        if sys.platform == "win32" and build_script.exists():
            cmd = ["powershell", "-ExecutionPolicy", "Bypass", "-File", str(build_script)]
            res = subprocess.run(cmd, cwd=PROJECT_ROOT, capture_output=True, text=True)
            if res.returncode != 0:
                self.log(f"❌ Server Build Script Failed:\n{res.stderr}")
                return

        version = self.server_ver_entry.get().strip()
        zip_file_name = f"lanflix-server-v{version}.zip"
        zip_target_path = RELEASES_DIR / zip_file_name

        shutil.make_archive(str(zip_target_path).replace(".zip", ""), 'zip', SERVER_PUBLISH_DIR)

        checksum = self.calculate_sha256(zip_target_path)
        file_size = zip_target_path.stat().st_size
        host_url = self.host_entry.get().strip().rstrip("/")
        notes = self.notes_entry.get().strip()

        manifest_data = {
            "version": version,
            "currentVersion": version,
            "releaseDate": datetime.utcnow().isoformat() + "Z",
            "downloadUrl": f"{host_url}/releases/{zip_file_name}",
            "fileSize": file_size,
            "checksum": checksum,
            "releaseNotes": notes,
            "isUpdateAvailable": True
        }

        manifest_path = RELEASES_DIR / "server-manifest.json"
        manifest_path.write_text(json.dumps(manifest_data, indent=2), encoding="utf-8")
        if DEV_RELEASES_DIR.exists():
            (DEV_RELEASES_DIR / "server-manifest.json").write_text(json.dumps(manifest_data, indent=2), encoding="utf-8")

        self.log(f"✅ Server Build Complete: {zip_target_path.name}")
        self.log(f"🔑 SHA-256 Checksum: {checksum}")
        self.log(f"📝 Server Manifest Updated: {manifest_path}")

    def git_push_logic(self):
        self.log("=" * 60)
        self.log("🐙 Executing Git Release Push...")
        self.log("=" * 60)

        notes = self.notes_entry.get().strip()
        server_ver = self.server_ver_entry.get().strip()
        tag_name = f"v{server_ver}"

        # 1. Git add manifest & release files
        cmd_add = ["git", "add", "-A"]
        res_add = subprocess.run(cmd_add, cwd=PROJECT_ROOT, capture_output=True, text=True)
        self.log(f"➔ git add: {res_add.stdout.strip()}")

        # 2. Git commit
        commit_msg = f"release: Publish Lanflix OTA update {tag_name} - {notes}"
        cmd_commit = ["git", "commit", "-m", commit_msg]
        res_commit = subprocess.run(cmd_commit, cwd=PROJECT_ROOT, capture_output=True, text=True)
        self.log(f"➔ git commit: {res_commit.stdout.strip()}")

        # 3. Git tag (force replace if exists)
        cmd_tag = ["git", "tag", "-fa", tag_name, "-m", commit_msg]
        res_tag = subprocess.run(cmd_tag, cwd=PROJECT_ROOT, capture_output=True, text=True)
        self.log(f"➔ git tag ({tag_name}): {res_tag.stdout.strip()}")

        # 4. Git push branch and tags
        cmd_push = ["git", "push", "origin", "HEAD", "--tags", "--force"]
        res_push = subprocess.run(cmd_push, cwd=PROJECT_ROOT, capture_output=True, text=True)
        if res_push.returncode == 0:
            self.log(f"✅ Git push successful: Pushed commits & tag {tag_name} to origin!")
        else:
            self.log(f"⚠️ Git push result:\n{res_push.stderr}")

        # 5. Optional GitHub CLI (gh) Release creation if available
        try:
            gh_cmd = ["gh", "release", "create", tag_name, "--title", f"Lanflix Release {tag_name}", "--notes", notes]
            res_gh = subprocess.run(gh_cmd, cwd=PROJECT_ROOT, capture_output=True, text=True)
            if res_gh.returncode == 0:
                self.log(f"🎉 GitHub Release created via GitHub CLI: {tag_name}")
        except Exception:
            pass

def main():
    root = tk.Tk()
    app = OtaPublisherGUI(root)
    root.mainloop()

if __name__ == "__main__":
    main()
