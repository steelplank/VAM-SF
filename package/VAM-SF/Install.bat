@echo off
REM VAM-SF launcher - runs install.ps1 with the execution policy relaxed for this call only.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
echo.
pause
