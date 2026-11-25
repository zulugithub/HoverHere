@echo off
cd /d "%~dp0"

echo [1/4] Baue MSVC Release...
cmake --build build/msvc --config Release
if errorlevel 1 (
    echo FEHLER beim Kompilieren! Abbruch.
    pause
    exit /b 1
)

echo [2/4] Fuehre Unit-Tests aus...
ctest --test-dir build/msvc -C Release --output-on-failure
if errorlevel 1 (
    echo WARNUNG: Mindestens ein Test ist fehlgeschlagen!
    pause
    exit /b 1
)

echo [3/4] Kopiere hoverhere.dll nach Unity...
copy /Y build\msvc\bin\Release\hoverhere.dll ..\HoverHere\Assets\Plugins\x86_64\hoverhere.dll
if errorlevel 1 (
    echo FEHLER beim Kopieren. Ist Unity noch im Play-Modus oder geoeffnet?
    pause
    exit /b 1
)

echo [4/4] Kopiere Airfoil-Polaren nach Unity StreamingAssets...
if not exist ..\HoverHere\Assets\StreamingAssets\AirfoilPolars mkdir ..\HoverHere\Assets\StreamingAssets\AirfoilPolars
if errorlevel 1 (
    echo FEHLER beim Erstellen des AirfoilPolar-Verzeichnisses!
    pause
    exit /b 1
)
copy /Y data\airfoil_polars\*.xml ..\HoverHere\Assets\StreamingAssets\AirfoilPolars\
if errorlevel 1 (
    echo FEHLER beim Kopieren der Airfoil-Polar-XML-Dateien!
    pause
    exit /b 1
)

echo Fertig! Neue hoverhere.dll liegt in Unity bereit.
pause
