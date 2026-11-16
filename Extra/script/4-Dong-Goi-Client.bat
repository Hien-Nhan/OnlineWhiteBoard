@echo off
setlocal
chcp 65001 >nul
title Whiteboard - Dong goi Client
call :check_dotnet || exit /b 1

for %%I in ("%~dp0..\..\code\WhiteboardClient") do set "CLIENT=%%~fI"
for %%I in ("%~dp0..\..") do set "ROOT=%%~fI"
set "OUT=%ROOT%\Client-Publish"

echo ============================================================
echo   DONG GOI CLIENT chay duoc tren may KHONG cai .NET
echo ============================================================
echo Can Internet. Ket qua o thu muc: %OUT%
echo.
pushd "%CLIENT%"
dotnet publish -c Release -r win-x64 -p:Platform=x64 --self-contained true -o "%OUT%"
set "RESULT=%errorlevel%"
popd

echo.
if not "%RESULT%"=="0" (
  echo [LOI] Dong goi that bai. Xem thong bao loi o tren.
) else (
  echo Xong! Chep CA thu muc "Client-Publish" sang may khac, mo WhiteboardClient.exe
  echo WinUI 3 khong gop duoc thanh 1 file .exe duy nhat, nen phai chep ca thu muc.
  explorer "%OUT%"
)
pause
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
