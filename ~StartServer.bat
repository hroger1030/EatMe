@echo off
rem Starts the published server. Publish first with: ~PublishPC.bat
set EXE_DIR=%~dp0publish\pc_server

if not exist "%EXE_DIR%\EatMeServer.exe" (
    echo EatMeServer.exe not found in %EXE_DIR%
    echo Publish first with: ~PublishPC.bat
    pause
    exit /b 1
)

pushd "%EXE_DIR%"
EatMeServer.exe
popd
pause
