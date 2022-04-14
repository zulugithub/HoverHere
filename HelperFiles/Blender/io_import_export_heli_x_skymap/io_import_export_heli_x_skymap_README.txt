#################################################################################
##     ____                 __             
##    / __ \___  ____ _____/ /___ ___  ___ 
##   / /_/ / _ \/ __ `/ __  / __ `__ \/ _ \
##  / _, _/  __/ /_/ / /_/ / / / / / /  __/
## /_/ |_|\___/\__,_/\__,_/_/ /_/ /_/\___/ 
##
#################################################################################
## Import Heli-X skybox environments and export Heli-X collision objects with Blender >2.80
## io_import_heli_x_skymap.py
#################################################################################


1.) Installing plugin
- Start Blender 2.8 and go to "Edit" -> "Preferences" -> "Add-ons" 
- Select "Install..." and browse to the location of the file "io_import_heli_x_skymap.py" -> click on "Install Add-on"
- Activate the check box of the add on -> The addon is now installed
 
2.) Open Heli-X Skymap
- Go to "File" -> "Import" -> "Heli-X Skymap"
- Browse to the skybox folder, for example to ...\HELI-X9\resources\environments\SkyBox\Ahornkopf\
- Hit "Import Heli-X Skymap"  in the lower right corner

3.) Edit Heli-X Skymap
- The Outliner window  in the top right corner shows two collections. 
	First one is "Collection", which holds the background skybox. These should not be edited.
	Second one is the "Collection Collision Object" collection, which holds the collision objects.
- Selecting for example in the Ahornkopf Skybox the "ground"-object and going to the "Material Properties" (red ball in 
  the properties vertical tab list) lists the assosiated materials to this object.
- Each material "Material_ground.xml.00x" has special properties, which define the "Alpha", "GroundType", "Shadowreceiver", "Frictionfactor",.. properties of Heli-X
  These properties are visible and setable under "Custom Protperties" of the object. See also attached figure in the right lower corner.
	
4.) Exporting Heli-X Skymap
- Go to "File" -> "Export" -> "Heli-X Collision Objects"
- Browse to the skybox folder, for eample to ...\HELI-X9\resources\environments\SkyBox\Ahornkopf\
- Hit "Export Heli-X collision objects" in the lower right corner





