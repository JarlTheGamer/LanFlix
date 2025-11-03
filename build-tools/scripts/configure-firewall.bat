@echo off
echo Configuring Windows Firewall for Lanflix Server...
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running as administrator - configuring firewall rules...
    
    REM Add inbound rule for port 5037
    netsh advfirewall firewall add rule name="Lanflix Server HTTP" dir=in action=allow protocol=TCP localport=5037
    
    REM Add outbound rule for port 5037 (optional, usually not needed)
    netsh advfirewall firewall add rule name="Lanflix Server HTTP Out" dir=out action=allow protocol=TCP localport=5037
    
    echo.
    echo Firewall rules added successfully!
    echo Port 5037 is now open for incoming connections.
    echo.
    echo Your server should now be accessible at:
    echo http://192.168.178.13:5037
    echo.
) else (
    echo ERROR: This script must be run as Administrator!
    echo.
    echo Right-click on this file and select "Run as administrator"
    echo.
)

pause