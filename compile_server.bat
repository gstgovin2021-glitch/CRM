@echo off
setlocal enabledelayedexpansion

echo.
echo ╔═══════════════════════════════════════════════════════════╗
echo ║                                                           ║
echo ║    Maurya CRM — Server Compilation                       ║
echo ║                                                           ║
echo ╚═══════════════════════════════════════════════════════════╝
echo.

REM Find C# compiler (csc.exe)
for /f "tokens=*" %%A in ('where csc.exe 2^>nul') do set CSC=%%A

if "%CSC%"=="" (
    echo [ERROR] C# compiler (csc.exe) not found!
    echo.
    echo This script requires Visual Studio or .NET Framework to be installed.
    echo.
    echo SOLUTIONS:
    echo 1. Install Visual Studio Community (Free)
    echo    https://visualstudio.microsoft.com/downloads/
    echo.
    echo 2. Install .NET Framework SDK
    echo    https://dotnet.microsoft.com/download/dotnet-framework
    echo.
    echo 3. Install .NET 6+ SDK (Recommended)
    echo    https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo [INFO] C# Compiler found at: %CSC%
echo.
echo [INFO] Compiling Server.cs...
echo.

REM Compile the server
"%CSC%" /out:Maurya_CRM_Server.exe Server.cs

if exist Maurya_CRM_Server.exe (
    echo.
    echo [SUCCESS] ✅ Server compiled successfully!
    echo.
    echo File created: Maurya_CRM_Server.exe
    echo.
    echo Next steps:
    echo 1. Right-click Maurya_CRM_Server.exe
    echo 2. Select "Run as administrator"
    echo 3. Server will start on port 8080
    echo.
    pause
) else (
    echo.
    echo [ERROR] Compilation failed. Check the errors above.
    echo.
    pause
    exit /b 1
)

