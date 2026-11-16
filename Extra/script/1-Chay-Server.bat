@echo off
setlocal
chcp 65001 >nul
title Whiteboard SERVER
call :check_dotnet || exit /b 1

for %%I in ("%~dp0..\..\code\WhiteboardServer") do set "SERVER=%%~fI"
cd /d "%SERVER%"

echo ============================================================
echo   WHITEBOARD SERVER
echo ============================================================
echo.
echo Dia chi IP cua may nay (gui cho nguoi dung may khac nhap vao Client).
echo Chon IP thuoc WiFi, thuong bat dau bang 192.168. hoac 10.
echo ------------------------------------------------------------
ipconfig | findstr /C:"IPv4"
echo ------------------------------------------------------------
echo.
echo Dang khoi dong Server... Lan dau can Internet de tai goi SQLite.
echo GIU cua so nay mo trong luc su dung. Nhan Ctrl+C de tat Server.
echo.
dotnet run

echo.
echo Server da dung. Nhan phim bat ky de dong.
pause >nul
exit /b 0

:check_dotnet
where dotnet >nul 2>&1
if errorlevel 1 (
  echo.
  echo [LOI] Chua cai .NET SDK. Hay cai .NET SDK 10 tai: https://dotnet.microsoft.com/download
  echo       Cai xong, DONG cua so nay va mo lai.
  pause
  exit /b 1
)
set "DNV="
for /f "tokens=1 delims=." %%v in ('dotnet --version') do set "DNV=%%v"
if not defined DNV exit /b 0
if %DNV% LSS 10 (
  echo.
  echo [CANH BAO] May dang dung .NET %DNV%, du an mac dinh can .NET 10.
  echo            Cai .NET SDK 10 hoac xem Extra\HUONG_DAN.md de doi sang .NET 8.
  echo.
  pause
)
exit /b 0
