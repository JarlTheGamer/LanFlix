================================================================================
  Lanflix Media Server
================================================================================

Thank you for installing Lanflix!

QUICK START
-----------

1. Edit the .env file to configure your media directories
2. Run start-server.bat
3. Open http://localhost:8080 in your browser
4. Install the Android app and connect to your server

CONFIGURATION
-------------

Edit the .env file to customize:

  PORT=8080                          # Server port
  MEDIA_ROOT_PATH=D:/Movies          # Your media folder
  DATABASE_PATH=./data/lanflix.db    # Database location

RUNNING THE SERVER
------------------

Option 1: Manual Start
  - Double-click start-server.bat
  - Server runs in a window
  - Close window to stop server

Option 2: Windows Service (Recommended)
  - Right-click install-service.bat > Run as administrator
  - Server runs in background
  - Starts automatically on boot
  - Manage via Windows Services (services.msc)

ACCESSING LANFLIX
-----------------

Web UI:
  - Local: http://localhost:8080
  - Network: http://YOUR_IP:8080

Android App:
  - Install the APK on your Android device
  - Enter your server URL (e.g., http://192.168.1.100:8080)
  - Start streaming!

FINDING YOUR IP ADDRESS
------------------------

Open Command Prompt and run:
  ipconfig

Look for "IPv4 Address" under your network adapter.

FIREWALL
--------

If you can't connect from other devices:
  1. Open Windows Firewall
  2. Allow port 8080 for Node.js
  3. Or run: netsh advfirewall firewall add rule name="Lanflix" dir=in action=allow protocol=TCP localport=8080

TROUBLESHOOTING
---------------

Server won't start:
  - Check Node.js is installed: node --version
  - Check port 8080 is not in use
  - Check logs in logs/ folder

Can't connect from Android:
  - Ensure server is running
  - Use IP address, not localhost
  - Check firewall allows port 8080
  - Ensure devices on same network

REQUIREMENTS
------------

- Windows 10 or higher
- Node.js 18 or higher
- 2GB RAM minimum
- Storage for media files

SUPPORT
-------

Documentation: See docs/BUILD.md and README.md
Issues: https://github.com/JarlTheGamer/Applications./issues

UNINSTALL
---------

1. Stop the server/service
2. If installed as service: nssm remove Lanflix confirm
3. Delete this folder

================================================================================
Enjoy your streaming server!
================================================================================
