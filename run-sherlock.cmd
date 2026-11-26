@echo off
setlocal

set "SHERLOCK_EXE=%~dp0sherlock\runtime\python\Scripts\sherlock.exe"
if not exist "%SHERLOCK_EXE%" set "SHERLOCK_EXE=%~dp0sherlock\.venv\Scripts\sherlock.exe"
if not exist "%SHERLOCK_EXE%" (
    echo Sherlock is not installed. Run setup-windows.cmd first.
    exit /b 1
)

"%SHERLOCK_EXE%" %*
set "SHERLOCK_EXIT=%ERRORLEVEL%"
exit /b %SHERLOCK_EXIT%
