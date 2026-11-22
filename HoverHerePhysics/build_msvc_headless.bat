@echo off
cd /d "%~dp0"

:: Build only the headless maneuver runner target (MSVC Release)
echo Building hoverhere_headless_test (MSVC Release)...
cmake --build build/msvc --config Release --target hoverhere_headless_test
if errorlevel 1 (
    echo ERROR: Build failed.
    pause
    exit /b 1
)

echo Build succeeded.
