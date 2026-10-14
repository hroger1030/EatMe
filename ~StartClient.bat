@echo off
rem Starts the published client. Publish first with: PublishApps.bat
set EXE_DIR=%~dp0publish\client

if not exist "%EXE_DIR%\EatMeClient.exe" (
    echo EatMeClient.exe not found in %EXE_DIR%
    echo Publish first with: PublishApps.bat
    pause
    exit /b 1
)

pushd "%EXE_DIR%"
start "" EatMeClient.exe
popd
