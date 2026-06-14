@echo off
echo.
echo ╔═══════════════════════════════════════════════════════════╗
echo ║    Maurya CRM — Server Startup                           ║
echo ╚═══════════════════════════════════════════════════════════╝
echo.

if not exist "Maurya_CRM_Server.exe" (
    echo [ERROR] Maurya_CRM_Server.exe not found!
    echo.
    echo Please run: compile_server.bat
    echo.
    pause
    exit /b 1
)

if not exist "CRM_RBAC_Final.html" (
    echo [ERROR] CRM_RBAC_Final.html not found!
    echo.
    echo All files must be in the same folder:
    echo   • Maurya_CRM_Server.exe
    echo   • CRM_RBAC_Final.html
    echo   • database.json
    echo.
    pause
    exit /b 1
)

echo [INFO] Starting Maurya CRM Server...
echo.
echo.
Maurya_CRM_Server.exe
pause

