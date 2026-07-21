@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy-WebBuild.ps1" %*
exit /b %ERRORLEVEL%
