@echo off
setlocal
chcp 65001 >nul
title Whiteboard - Mo Firewall

rem Tu dong xin quyen Administrator neu chua co
net session >nul 2>&1
if errorlevel 1 (
  echo Dang xin quyen Administrator... Bam "Yes" o hop thoai hien ra.
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

rem Doc cong tu config.ini cua Server (mac dinh 8888)
set "PORT=8888"
for %%I in ("%~dp0..\..\code\WhiteboardServer\config.ini") do set "CFG=%%~fI"
if exist "%CFG%" (
  for /f "tokens=1,2 delims==" %%a in ('findstr /i /b "PORT=" "%CFG%"') do set "PORT=%%b"
)

echo Dang mo cong %PORT% (TCP, chieu vao) cho Whiteboard Server...
netsh advfirewall firewall delete rule name="Whiteboard Server" >nul 2>&1
netsh advfirewall firewall add rule name="Whiteboard Server" dir=in action=allow protocol=TCP localport=%PORT% profile=any
if errorlevel 1 (
  echo [LOI] Khong mo duoc cong %PORT%.
) else (
  echo.
  echo Da mo cong %PORT%. Chi can lam 1 lan tren may chay Server.
  echo Luu y: mang WiFi nen dat la "Private".
)
echo.
pause
exit /b 0
