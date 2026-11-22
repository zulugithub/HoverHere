build\msvc\bin\Release\hoverhere_headless_test.exe airfoil --heli ..\HoverHere\Assets\Helicopters_Available\Logo600SE_V3\Resources\Logo600SE_V3_parameter_file.xml --out out\airfoil.csv

py -3 tools\headless\plot_airfoil.py out\airfoil.csv