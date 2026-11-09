@echo off
title Whiteboard Server
cd /d "%~dp0code\WhiteboardServer\bin\Debug\net10.0"
echo ==================================================
echo         DANG KHOI CHAY WHITEBOARD SERVER
echo ==================================================
WhiteboardServer.exe
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Server bi dung voi ma loi %ERRORLEVEL%.
    pause
)
