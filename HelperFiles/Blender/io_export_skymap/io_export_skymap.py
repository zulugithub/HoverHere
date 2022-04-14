## ##### BEGIN GPL LICENSE BLOCK #####
##
##  This program is free software; you can redistribute it and/or
##  modify it under the terms of the GNU General Public License
##  as published by the Free Software Foundation; either version 2
##  of the License, or (at your option) any later version.
##
##  This program is distributed in the hope that it will be useful,
##  but WITHOUT ANY WARRANTY; without even the implied warranty of
##  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
##  GNU General Public License for more details.
##
##  You should have received a copy of the GNU General Public License
##  along with this program; if not, write to the Free Software Foundation,
##  Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
##
## ##### END GPL LICENSE BLOCK #####
## ASCII Text: http://patorjk.com/software/taag/#p=display&f=Slant&t=Import

## Export helicopter 3d model to "Free RC Helicopter Simulator" Unity project

## 2020.12.25 V1.00 Zulu

#################################################################################
##    ____      ____    
##   /  _/___  / __/___ 
##   / // __ \/ /_/ __ \
## _/ // / / / __/ /_/ /
##/___/_/ /_/_/  \____/ 
##
#################################################################################
#################################################################################
## 
#################################################################################
bl_info = {
    "name": "Export blender scenery collision model to 'Hover Here, a Free-RC_Helicopter-Simulator.",
    "author": "zulu",
    "version": (1, 2, 0),
    "blender": (2, 91, 00),
    "location": "File > Export > HoverHere Scenery",
    "description": "Export collision object to HoverHere, a Free-RC-Helicopter-Simulator",
    "warning": "",
    "wiki_url": "x",
    "category": "Import-Export",
}
#################################################################################





#################################################################################
##    ____                           __ 
##   /  _/___ ___  ____  ____  _____/ /_
##   / // __ `__ \/ __ \/ __ \/ ___/ __/
## _/ // / / / / / /_/ / /_/ / /  / /_  
##/___/_/ /_/ /_/ .___/\____/_/   \__/  
##             /_/       
##
#################################################################################
## 
#################################################################################
import bpy
import os
from bpy.types import Operator
import mathutils
import math 
#################################################################################











###############################################################################  
## EXPORT functionality: export objects
###############################################################################  
def export_objects():
	
	######################################################################################
	#target_folder_relative = '//..//..//Free-RC-Helicopter-Simulator_//Assets//Helicopters_Available//'
	#target_folder_relative = '\\Free-RC-Helicopter-Simulator\\Free-RC-Helicopter-Simulator\\Assets\\StreamingAssets\\Skymaps'
	target_folder_relative = '\\HoverHere\\HoverHere\\Assets\\StreamingAssets\\Skymaps'
	scale = 1.0
	######################################################################################
	
	######################################################################################
	## Get the folder name of this belnder file, because the folder name is and 
	## has to be the same, as the helicopter's name in unity.
	######################################################################################
	name_in_unity = os.path.basename(os.path.dirname(bpy.data.filepath))
	#target_folder = bpy.path.abspath(target_folder_relative + name_in_unity)
	target_folder = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(bpy.data.filepath)))) + target_folder_relative + '\\' + name_in_unity
	print(target_folder)
	######################################################################################
	
	######################################################################################
	## function to export obj file
	######################################################################################
	def export_model_as_obj(collection_name, filename, scale):
		bpy.ops.object.mode_set(mode='OBJECT')
		bpy.ops.object.select_all(action='DESELECT')
		
		## check, if collection exists
		collection_exists = False
		for collection in bpy.data.collections:
			#print(collection.name)
			if collection.name == collection_name:
				collection_exists = True
				
		if collection_exists == True:	
			## select all objects of a collection 
			for obj in bpy.data.collections[collection_name].all_objects:
				obj.select_set(True)
				
			if len(bpy.context.selected_objects): 
				target_file = os.path.join( target_folder, filename)
				bpy.ops.export_scene.obj(filepath=target_file, check_existing=False, filter_glob="*.obj;*.mtl", use_selection=True, use_animation=False, use_mesh_modifiers=True, use_edges=True, use_smooth_groups=False, use_smooth_groups_bitflags=False, use_normals=True, use_uvs=False, use_materials=False, use_triangles=True, use_nurbs=False, use_vertex_groups=False, use_blen_objects=True, group_by_object=False, group_by_material=False, keep_vertex_order=False, global_scale=scale, path_mode='AUTO', axis_forward='-Z', axis_up='Y')
	######################################################################################
	
	
	
	######################################################################################
	## Recursivly transverse layer_collection for a particular name
	## https://blender.stackexchange.com/questions/127403/change-active-collection
	######################################################################################
	def recurLayerCollection(layerColl, collName):
		found = None
		if (layerColl.name == collName):
			return layerColl
		for layer in layerColl.children:
			found = recurLayerCollection(layer, collName)
			if found:
				return found
	######################################################################################
	
	
	#	######################################################################################
	#	## 
	#	######################################################################################
	#	def save_info_to_ascii(file,name):
	#		
	#		## check, if name exists
	#		exists = False
	#		for ob in bpy.context.scene.objects:
	#			if ob.name == name:
	#				exists = True
	#
	#		if exists:
	#			object = bpy.data.objects[name]
	#			##bpy.context.scene.objects["Cube"].select_get()
	#
	#			object.location
	#			object.rotation_euler
	#			
	#			f.writelines([name + '\n'])
	#			f.writelines(['location: ' + str(object.location.x) + '    ' + str(object.location.y) + '    ' + str(object.location.z) + '\n'])
	#			f.writelines(['orientation: ' + str(object.rotation_euler.x) + '    ' + str(object.rotation_euler.y) + '    ' + str(object.rotation_euler.z) + '\n'])
	#	######################################################################################	





	######################################################################################
	## export 
	######################################################################################
	## select all objects of a collection 
	export_model_as_obj('COLLISION_EXPORT', 'collision_object.obj', scale)
	export_model_as_obj('COLLISION_LANDING_EXPORT', 'collision_landing_object.obj', scale)
	



#################################################################################
##    ______                      __     ________               
##   / ____/  ______  ____  _____/ /_   / ____/ /___ ___________
##  / __/ | |/_/ __ \/ __ \/ ___/ __/  / /   / / __ `/ ___/ ___/
## / /____>  </ /_/ / /_/ / /  / /_   / /___/ / /_/ (__  |__  ) 
##/_____/_/|_/ .___/\____/_/   \__/   \____/_/\__,_/____/____/  
##          /_/      
#################################################################################
## Operator
#################################################################################
class EXPORT_SCENERY_2_HOVER_HERE_OT_objects(Operator):
	"""EXPORT Hover Here objects"""          # use this as a tooltip for menu items and buttons.

	bl_idname = "export_scenery_2_hover_here.objects"  # unique identifier for buttons and menu items to reference.
	bl_label = "Export scenery 2 HoverHere objects"   # display name in the interface.
	bl_options = {'REGISTER', 'UNDO'}   # enable undo for the operator.    

	# define this to tell 'fileselect_add' that we want a directory    name="Skymap environment path",
	# directory: bpy.props.StringProperty(maxlen=1024, subtype='FILE_PATH', options={'HIDDEN', 'SKIP_SAVE'})

	def execute(self, context):        # execute() is called when running the operator.
		
		#target_folder_relative = '../../../Free-RC-Helicopter-Simulator_/Assets/Helicopters_Available/' 
		#name_in_unity = os.path.basename(os.path.dirname(bpy.data.filepath))
		#target_folder = os.path.abspath(target_folder_relative + name_in_unity)
		#print("Target Folder: '" + target_folder + "'")
		#print("Name: '" + name_in_unity + "'")

		export_objects()

		return {'FINISHED'}            # lets Blender know the operator finished successfully.


    ## def invoke(self, context, event):
        ## open browser, take reference to 'self' read the path to selected
        ## file, put path in predetermined self fields.
        ## see: https://docs.blender.org/api/current/bpy.types.WindowManager.html#bpy.types.WindowManager.fileselect_add
        ## context.window_manager.fileselect_add(self)
        ## tells Blender to hang on for the slow user input
        ## return {'RUNNING_MODAL'}
        
#################################################################################










#################################################################################
##    ____             _      __           
##   / __ \___  ____ _(_)____/ /____  _____
##  / /_/ / _ \/ __ `/ / ___/ __/ _ \/ ___/
## / _, _/  __/ /_/ / (__  ) /_/  __/ /    
##/_/ |_|\___/\__, /_/____/\__/\___/_/     
##           /____/     
#################################################################################
## Register
#################################################################################
def export_scenery_2_hover_here_objects_button(self, context):
    self.layout.operator(EXPORT_SCENERY_2_HOVER_HERE_OT_objects.bl_idname, text="Export HoverHere Scenery", icon='IMAGE_DATA')


classes = (
    EXPORT_SCENERY_2_HOVER_HERE_OT_objects,
)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)

    bpy.types.TOPBAR_MT_file_export.append(export_scenery_2_hover_here_objects_button)


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(export_scenery_2_hover_here_objects_button)
 
    for cls in classes:
        if hasattr(bpy.types, cls.__name__):
            bpy.utils.unregister_class(cls)


if __name__ == "__main__":
    # run simple doc tests
    import doctest
    doctest.testmod()
    
    unregister()
    register()
