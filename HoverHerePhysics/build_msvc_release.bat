@echo off
cd /d "%~dp0"

echo [1/3] Baue MSVC Release...
cmake --build build/msvc --config Release
if errorlevel 1 (
    echo FEHLER beim Kompilieren! Abbruch.
    pause
    exit /b 1
)

echo [2/3] Fuehre Unit-Tests aus...
ctest --test-dir build/msvc -C Release --output-on-failure
if errorlevel 1 (
    echo WARNUNG: Mindestens ein Test ist fehlgeschlagen!
    pause
    exit /b 1
)

echo [3/3] Kopiere hoverhere.dll nach Unity...
copy /Y build\msvc\bin\Release\hoverhere.dll ..\HoverHere\Assets\Plugins\x86_64\hoverhere.dll
if errorlevel 1 (
    echo FEHLER beim Kopieren (ist Unity noch im Play-Modus oder geoeffnet?)!
    pause
    exit /b 1
)

echo Fertig! Neue hoverhere.dll liegt in Unity bereit.
pause
