@echo off
setlocal
chcp 65001 >nul
title Whiteboard Online - Menu
set "S=%~dp0Extra\scripts"

:menu
cls
echo ============================================================
echo   WHITEBOARD ONLINE
echo ============================================================
echo.
echo   1. Chay Server
echo   2. Chay Client
echo   3. Mo Firewall cho Server (chay nhieu may, lam 1 lan)
echo   4. Dong goi Client thanh thu muc chay duoc tren may khac
echo   5. Xem dia chi IP cua may nay
echo   0. Thoat
echo.
set "CHOICE="
set /p CHOICE=Chon (0-5):

if "%CHOICE%"=="1" start "" "%S%\1-Chay-Server.bat"
if "%CHOICE%"=="2" start "" "%S%\2-Chay-Client.bat"
if "%CHOICE%"=="3" start "" "%S%\3-Mo-Firewall.bat"
if "%CHOICE%"=="4" start "" "%S%\4-Dong-Goi-Client.bat"
if "%CHOICE%"=="5" (
  echo.
  ipconfig | findstr /C:"IPv4"
  echo.
  echo Chon IP thuoc WiFi, thuong bat dau bang 192.168. hoac 10.
  pause
)
if "%CHOICE%"=="0" exit /b 0
goto menu
