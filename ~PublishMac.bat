@echo off
rem Publishes macOS (Apple silicon) release builds of the client and server to publish\mac_client and publish\mac_server, with no .pdb files. Players need the .NET runtime installed.
set CLIENT_DIR=%~dp0publish\mac_client
set SERVER_DIR=%~dp0publish\mac_server

if exist "%CLIENT_DIR%" rmdir /s /q "%CLIENT_DIR%"
if exist "%SERVER_DIR%" rmdir /s /q "%SERVER_DIR%"

dotnet publish "%~dp0EatMeClient\EatMeClient.csproj" -c Release -r osx-arm64 --self-contained false -p:DebugType=none -o "%CLIENT_DIR%"
if errorlevel 1 (
    echo Client publish failed.
    pause
    exit /b 1
)

dotnet publish "%~dp0EatMeServer\EatMeServer.csproj" -c Release -r osx-arm64 --self-contained false -p:DebugType=none -o "%SERVER_DIR%"
if errorlevel 1 (
    echo Server publish failed.
    pause
    exit /b 1
)

echo Client published to %CLIENT_DIR%
echo Server published to %SERVER_DIR%
pause
