@echo off
setlocal
cd /d "%~dp0"

set "PROJECT_DIR=%~dp0sherlock"
set "VENV_PY=%PROJECT_DIR%\.venv\Scripts\python.exe"

where py >nul 2>nul
if errorlevel 1 (
    echo Python launcher "py" was not found.
    echo Install Python 3.10-3.13 from https://www.python.org/downloads/ and enable "py launcher".
    pause
    exit /b 1
)

if not exist "%VENV_PY%" (
    echo Creating Python virtual environment...
    py -3 -m venv "%PROJECT_DIR%\.venv"
    if errorlevel 1 (
        echo Failed to create the Python virtual environment.
        pause
        exit /b 1
    )
)

echo Installing Sherlock dependencies...
"%VENV_PY%" -m pip install --disable-pip-version-check --no-input -e "%PROJECT_DIR%"
if errorlevel 1 (
    echo Dependency installation failed.
    pause
    exit /b 1
)

echo.
echo Setup complete. Double-click dist\SherlockDesktop.exe to start.
pause
