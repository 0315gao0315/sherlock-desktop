@echo off
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo .NET Framework 4 compiler was not found.
    exit /b 1
)

if not exist "..\dist" mkdir "..\dist"

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
    /out:"..\dist\SherlockDesktop.exe" ^
    /win32manifest:"SherlockDesktop.manifest" ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    "SherlockDesktop.cs"

if errorlevel 1 exit /b 1
echo Built: dist\SherlockDesktop.exe
