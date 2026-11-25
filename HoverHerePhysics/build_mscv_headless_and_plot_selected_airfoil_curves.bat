@echo off
setlocal
cd /d "%~dp0"
if errorlevel 1 exit /b 1

if not exist "build\msvc\bin\Release\hoverhere_headless_test.exe" (
    echo ERROR: Build the MSVC Release headless target first.
    exit /b 1
)

if not "%~1"=="" (
    set "profile_name=%~1"
) else (
    echo Select an airfoil profile:
    echo   NACA0012
    echo   NACA0009sm-il
    echo   NACA23012
    echo   NACA8H12
    echo   VR-7
    set /p "profile_name=Profile name: "
)

set "profile_file="
if /I "%profile_name%"=="NACA0012" set "profile_file=n0012-il_polar.xml"
if /I "%profile_name%"=="NACA0009sm-il" set "profile_file=n0009sm-il_polar.xml"
if /I "%profile_name%"=="NACA0009sm-il (Selig-modified)" set "profile_file=n0009sm-il_polar.xml"
if /I "%profile_name%"=="NACA23012" set "profile_file=naca23012-il_polar.xml"
if /I "%profile_name%"=="NACA8H12" set "profile_file=n8h12-il_polar.xml"
if /I "%profile_name%"=="VR-7" set "profile_file=vr7-il_polar.xml"

if not defined profile_file (
    echo ERROR: Unknown profile "%profile_name%".
    exit /b 2
)

if not exist "out" mkdir "out"
if errorlevel 1 exit /b 1

.\build\msvc\bin\Release\hoverhere_headless_test.exe airfoil --airfoil-polar "%profile_file%" --out out\airfoil.csv
if errorlevel 1 exit /b 1

py -3 tools\headless\plot_airfoil.py out\airfoil.csv
if errorlevel 1 exit /b 1

echo %profile_name% airfoil curves exported and plotted.
exit /b 0
