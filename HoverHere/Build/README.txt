HoverHere\Build\
	HoverHere_V2022_02_11_Win_PORTABLE.zip  <--  Win
	HoverHere_V2022_02_11_Mac.zip <--  HoverHere_V2022_02_11_Mac.app

HoverHere\Work\InnoSetup\Output\ 
	HoverHere_V2022_02_11_Win_INSTALLER.zip <-- Setup_HoverHere_V2022_02_11_Win.exe


When zipping Mac build (i.e. HoverHere_V2022_02_11_Mac.app), then the zip file should be "HoverHere_V2022_02_11_Mac.zip" (without .app) 






-----MAC-------------------------------------------------------------------------------------------------------------------------
https://forum.unity.com/threads/issue-opening-unity-mac-applications-on-mac-big-sur-os-from-unidentified-developer.1026697/

1.) Add executable bit to the executable:
"chmod +x <appname.app>/Contents/MacOS/<appname>"
chmod +x Free-RC-Helicopter_Simulator_V2021_03_01_Mac.app/Contents/MacOS/Free-RC-Helicopter_Simulator_V2021_03_01_Mac

2.) Remove quarantine attributes:
"xattr -r -d com.apple.quarantine <appname.app>"
xattr -r -d com.apple.quarantine Free-RC-Helicopter_Simulator_V2021_03_01_Mac.app/

3.) If Gatekeeper activated:
	1.) ÷ffnen Sie das Terminal
	2.) Geben Sie den folgenden Befehl zum Ausschalten von Gatekeeper
	3.) sudo spctl --master-disable
	4.) Geben Sie Ihr Administratorkennwort ein und bet‰tigen anschlieﬂend die Returntaste
	5.) Starten Sie die angeblich besch‰digte App
	6.) Geben Sie im Terminal den Befehl zum erneuten Einschalten von Gatekeeper
	7.) sudo spctl --master-enable


