# Implementation Plan - Dynamic OTA Updates, Home Assistant Style `lanflix.local` Discovery & In-App OTA System

This plan details rewriting the Lanflix OTA update system using `build-tools/AndroidVersions/native-app` as the Home Assistant style native shell, eliminating hardcoded C# version blocks (`var latestVersion = new { ... }`), enabling `lanflix.local` local network discovery, supporting seamless server updates on the website frontend, and providing a dedicated in-app OTA download screen for Android APK updates.

---

## Architecture & Home Assistant Pattern Alignment

> [!IMPORTANT]
> **Home Assistant Architecture Match**:
> - **How Home Assistant Works**: The Home Assistant Android app is a native shell hosting a WebView that renders the server's HTML frontend. On launch, it uses mDNS (`_home-assistant._tcp.local`) to find local servers on Wi-Fi. When Home Assistant Server updates, the app's WebView displays a server rebooting overlay until back online.
> - **Selected Android Project**: `build-tools/AndroidVersions/native-app`. It already features the native WebView host, built-in JavaScript bridge, and the full-screen `UpdateActivity` for in-app OTA downloads.
> - **In-App Android OTA Updates**: When an Android app update is triggered, the app transitions to `UpdateActivity` which handles downloading the package inside the app layout (displaying "Downloading update... X%" with progress bar and release notes).
> - **Server Updates (Web Frontend)**: When a server update is triggered from the web UI (e.g. in Settings), a full-screen website overlay displays *"Server updating... Please wait while Lanflix Server updates and restarts"* while polling server health until the server comes back online.

---

## User Review Required

> [!IMPORTANT]
> **Key Technical Upgrades**:
> 1. **Zero-Hardcoding Version System**: Remove static `var latestVersion = new { ... }` from C# code. Releases are fetched dynamically from GitHub API releases or a build-generated local `releases/manifest.json`.
> 2. **mDNS Responder (`lanflix.local`)**: C# backend hosts an mDNS responder (`MDnsDiscoveryService`) broadcasting `lanflix.local` on UDP 5353, allowing LAN devices to connect to `http://lanflix.local:5037`.
> 3. **Home Assistant Style Android Discovery & Server Picker**: Android app scans `_lanflix._tcp.local` services on Wi-Fi using `NsdManager` and presents a server selection list if disconnected or requested.
> 4. **Live Devices & Network Telemetry**: `/api/system/telemetry` endpoint powering real-time active connected devices and network throughput stats in `settings.html`.

---

## Proposed Changes

### Component 1: Web UI & Telemetry (`lanflix-server/app/WebApi/ClientApp`)
- **`settings.html` & `settings-main.js`**:
  - Render real-time **Active Devices** (sessions, IP addresses, client app names) and **Network** statistics (throughput in Mbps).
  - Web browser update button triggers server update; native app update button triggers native in-app OTA download.
- **`app-updater.js`**:
  - Implements full-screen HTML progress overlay for server updates on the website with automatic reconnect polling.

### Component 2: Backend Dynamic OTA & Telemetry (C# Server)
- **`SystemTelemetryController.cs`**: Exposes `/api/system/telemetry` returning active device sessions and network throughput.
- **`AppUpdateController.cs` & `ServerUpdateController.cs`**: Query dynamic version info via `ReleaseMetadataService` (GitHub API with fallback to `releases/manifest.json`). No hardcoded C# version blocks.
- **`ServerUpdateService.cs`**: Downloads server zip, verifies SHA-256 checksum, executes update script, reboots server.

### Component 3: Local Network Discovery (`lanflix.local` & mDNS)
- **`MDnsDiscoveryService.cs`**: C# background service broadcasting `lanflix.local` and `_lanflix._tcp.local` on UDP 5353.

### Component 4: Android App (`build-tools/AndroidVersions/native-app`)
- **`UpdateActivity.kt` & `UpdateManager.kt`**: Full-screen in-app download experience displaying real-time percentage and progress bar inside the app UI.
- **`ServerDiscoveryManager.kt`**: Scans `_lanflix._tcp.local` via native `NsdManager`.
- **`MainActivity.kt`**: Resolves `lanflix.local:5037` via mDNS and provides server picker dialog.

---

## Verification Plan

### Automated Verification
1. **Build Solution**: Run `lanflix-server\build.ps1` to ensure clean compilation.

### Manual Verification
1. **Website Server Update**: Trigger server update from Web UI -> Confirm full-screen website progress overlay -> Server restarts -> WebUI reloads cleanly.
2. **In-App Android OTA Update**: Trigger app update from native app -> Confirm transition to `UpdateActivity` displaying in-app download progress bar -> Launch package installer when complete.
3. **mDNS Resolution**: Navigate to `http://lanflix.local:5037` in browser and test Android app discovery.
