@echo off
setlocal
set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "DOTNET=%ROOT%\tools\dotnet-sdk\dotnet.exe"
if not exist "%DOTNET%" (
    echo [ERROR] Portable .NET SDK not found:
    echo         %DOTNET%
    echo         Re-extract tools\dotnet-sdk-8.0.427-win-x64.zip into tools\dotnet-sdk\.
    pause
    exit /b 1
)

set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_CLI_HOME=%ROOT%\.build\dotnet-home"
set "NUGET_PACKAGES=%ROOT%\.build\nuget"
set "TMP=%ROOT%\.build\temp"
set "TEMP=%ROOT%\.build\temp"
if not exist "%DOTNET_CLI_HOME%" mkdir "%DOTNET_CLI_HOME%"
if not exist "%NUGET_PACKAGES%" mkdir "%NUGET_PACKAGES%"
if not exist "%TMP%" mkdir "%TMP%"

cd /d "%ROOT%\src\PawnshopMod"
echo Building PawnshopMod (Release) ...
echo.
"%DOTNET%" build -c Release %*
set "RC=%ERRORLEVEL%"
echo.
if "%RC%"=="0" (
    echo Build OK - the mod was copied into the game's Mods\PawnshopMod folder.
) else (
    echo Build FAILED - read the errors above.
)
pause
exit /b %RC%
