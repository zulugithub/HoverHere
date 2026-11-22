@echo off
cd /d "%~dp0"

:: Use first CLI argument as maneuver name, or prompt interactively
set MANEUVER=%~1
if "%MANEUVER%"=="" (
    echo Available maneuvers: hover, collective_step, roll_step, pitch_step, yaw_step, roll_doublet, pitch_doublet, vrs_descent, takeoff_land
    set /p MANEUVER="Enter maneuver name (press Enter for hover): "
)
if "%MANEUVER%"=="" set MANEUVER=hover

set EXE=build\msvc\bin\Release\hoverhere_headless_test.exe
set HELI=..\HoverHere\Assets\Helicopters_Available\Logo600SE_V3\Resources\Logo600SE_V3_parameter_file.xml

if not exist "%EXE%" (
    echo ERROR: %EXE% not found. Please run build_headless.bat first.
    pause
    exit /b 1
)

:: Run headless simulation and export CSV logs
echo [1/2] Running maneuver "%MANEUVER%"...
"%EXE%" run %MANEUVER% --heli "%HELI%" --out out
if errorlevel 1 (
    echo ERROR: Simulation failed.
    pause
    exit /b 1
)

:: Plot state and BEMT grid telemetry
echo [2/2] Generating plots...
py -3 tools\headless\plot_example.py "out\%MANEUVER%_states.csv" "out\%MANEUVER%_bemt_grid.csv"
