@echo off
title Online Whiteboard Launcher
echo ==================================================
echo         KHOI CHAY HE THONG ONLINE WHITEBOARD
echo ==================================================

echo [1/2] Dang khoi dong Server...
start "Whiteboard Server" "%~dp0run-server.bat"

echo Cho 2 giay de Server san sang...
ping 127.0.0.1 -n 3 >nul

echo [2/2] Dang khoi dong Client WinUI 3...
start "Whiteboard Client" "%~dp0run-client.bat"

echo ==================================================
echo He thong da khoi chay xong!
echo ==================================================
