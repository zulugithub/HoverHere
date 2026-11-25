@echo off
setlocal
cd /d "%~dp0"
if errorlevel 1 exit /b 1

if not exist "build\msvc\bin\Release\hoverhere_headless_test.exe" (
    echo ERROR: Build the MSVC Release headless target first.
    exit /b 1
)
if not exist "..\HoverHere\Assets\Helicopters_Available\Logo600SE_V3\Resources\Logo600SE_V3_parameter_file.xml" (
    echo ERROR: Logo600SE_V3 parameter file was not found.
    exit /b 1
)
if not exist "out" mkdir "out"
if errorlevel 1 exit /b 1

.\build\msvc\bin\Release\hoverhere_headless_test.exe airfoil --heli ..\HoverHere\Assets\Helicopters_Available\Logo600SE_V3\Resources\Logo600SE_V3_parameter_file.xml --out out\airfoil.csv
if errorlevel 1 exit /b %errorlevel%

py -3 tools\headless\plot_airfoil.py out\airfoil.csv
if errorlevel 1 exit /b %errorlevel%

echo Airfoil CSV exported and plotted successfully.
exit /b 0