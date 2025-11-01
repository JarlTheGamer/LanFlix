# Lanflix Server Installer

## Overview

The Lanflix server is a self-contained application that serves both the web UI and API backend.

## Installation

1. Run `lanflix-installer.exe`
2. Choose installation directory
3. The installer will:
   - Copy server files
   - Create desktop shortcut
   - Optionally install as Windows service
   - Configure firewall rules

## Manual Installation

1. Extract `lanflix-server-portable.zip` to desired location
2. Run `start-server.bat` to start the server
3. Or run `lanflix-server.exe` directly
4. Optionally run `install-service.bat` to install as Windows service

## Configuration

Edit `config.json` to customize:
- Server port (default: 8080)
- Media directories
- Database location
- Cache settings

## Accessing Lanflix

After starting the server:
- Web UI: `http://localhost:8080`
- From other devices: `http://YOUR_IP:8080`
- Android app: Configure server IP in app settings

## Uninstallation

1. Stop the server/service
2. Run `uninstall-service.bat` (if installed as service)
3. Delete installation directory
