@echo off
setlocal
chcp 65001 >nul
title Whiteboard CLIENT
call :check_dotnet || exit /b 1

for %%I in ("%~dp0..\..\code\WhiteboardClient") do set "CLIENT=%%~fI"
cd /d "%CLIENT%"

echo Dang khoi dong Client WinUI 3...
echo Lan dau can Internet de tai Windows App SDK va Win2D, co the mat vai phut.
echo.
dotnet run -p:Platform=x64
if errorlevel 1 (
  echo.
  echo [LOI] Client khong chay duoc. Xem thong bao loi o tren.
  pause
)
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
