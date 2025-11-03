@echo off
echo Testing Lanflix Server Network Configuration...
echo.

REM Get local IP address
echo Getting local IP address...
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4 Address"') do (
    set "ip=%%a"
    set "ip=!ip: =!"
    if not "!ip!"=="" (
        echo Local IP: !ip!
        goto :found_ip
    )
)

:found_ip
echo.

REM Test if port 5037 is listening
echo Testing if server is running on port 5037...
netstat -an | findstr :5037 >nul
if %errorLevel% == 0 (
    echo ✓ Server is listening on port 5037
) else (
    echo ✗ Server is NOT listening on port 5037
    echo   Make sure the Lanflix server is running
)

echo.

REM Test firewall rules
echo Checking firewall rules...
netsh advfirewall firewall show rule name="Lanflix Server HTTP" >nul 2>&1
if %errorLevel% == 0 (
    echo ✓ Firewall rule exists for Lanflix Server
) else (
    echo ✗ Firewall rule NOT found
    echo   Run configure-firewall.bat as Administrator to add firewall rules
)

echo.

REM Test local connectivity
echo Testing local connectivity...
curl -s -o nul -w "%%{http_code}" http://localhost:5037/health >nul 2>&1
if %errorLevel% == 0 (
    echo ✓ Server responds to local requests
) else (
    echo ✗ Server does NOT respond to local requests
    echo   Check if the server is running and configured correctly
)

echo.
echo Network test complete!
echo.
echo If all tests pass, your server should be accessible from other devices at:
echo http://192.168.178.13:5037
echo.
pause