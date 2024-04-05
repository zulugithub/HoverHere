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

## 2020.11.13 V1.00 Zulu
## 2020.12.25 V1.01 
## 2021.04.18 V1.02
## 2021.09.12 V1.03
## 2022.06.25 V1.04
## 2023.05.18 V1.05 use new obj exporter
## 2024.04.05 V1.06 Blender 4.1

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
    "name": "Export blender helicopter model to 'Hover Here, a Free-RC_Helicopter-Simulator.",
    "author": "zulu",
    "version": (1, 0, 6),
    "blender": (4, 1, 00),
    "location": "File > Export > HoverHere",
    "description": "Export to HoverHere",
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
	#target_folder_relative = '//..//..//HoverHere_//Assets//Helicopters_Available//'
	#target_folder_relative = '\\HoverHere\\HoverHere\\Assets\\Helicopters_Available'
	target_folder_relative = '\\HoverHere\\Git\\HoverHere\\Assets\\Helicopters_Available'
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
	def export_model_as_obj(collection_name, object_name, scale):
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
				target_file = os.path.join( target_folder +'\\3D_' + object_name, name_in_unity + '_' + object_name + '.obj')
				#bpy.ops.export_scene.obj(filepath=target_file, check_existing=False, filter_glob="*.obj;*.mtl", use_selection=True, use_animation=False, use_mesh_modifiers=True, use_edges=True, use_smooth_groups=False, use_smooth_groups_bitflags=False, use_normals=True, use_uvs=True, use_materials=True, use_triangles=False, use_nurbs=False, use_vertex_groups=False, use_blen_objects=True, group_by_object=False, group_by_material=True, keep_vertex_order=False, global_scale=scale, path_mode='AUTO', axis_forward='Z', axis_up='Y')
				bpy.ops.wm.obj_export(filepath=target_file, check_existing=False, filter_blender=False, filter_backup=False, filter_image=False, filter_movie=False, filter_python=False, filter_font=False, filter_sound=False, filter_text=False, filter_archive=False, filter_btx=False, filter_collada=False, filter_alembic=False, filter_usd=False, filter_obj=False, filter_volume=False, filter_folder=True, filter_blenlib=False, filemode=8, display_type='DEFAULT', sort_method='DEFAULT', export_animation=False, start_frame=-2147483648, end_frame=2147483647, forward_axis='Z', up_axis='Y',          global_scale=scale, apply_modifiers=True, export_eval_mode='DAG_EVAL_VIEWPORT', export_selected_objects=True, export_uv=True, export_normals=True, export_colors=False, export_materials=True, export_pbr_extensions=False, path_mode='AUTO', export_triangulated_mesh=False, export_curves_as_nurbs=False, export_object_groups=False, export_material_groups=True, export_vertex_groups=False, export_smooth_groups=False, smooth_group_bitflags=False, filter_glob="*.obj;*.mtl")
				#bpy.ops.wm.obj_export(filepath=target_file, check_existing=False, filter_blender=False, filter_backup=False, filter_image=False, filter_movie=False, filter_python=False, filter_font=False, filter_sound=False, filter_text=False, filter_archive=False, filter_btx=False, filter_collada=False, filter_alembic=False, filter_usd=False, filter_obj=False, filter_volume=False, filter_folder=True, filter_blenlib=False, filemode=8, display_type='DEFAULT', sort_method='',        export_animation=False, start_frame=-2147483648, end_frame=2147483647, forward_axis='NEGATIVE_Z', up_axis='Y', global_scale=scale, apply_modifiers=True, export_eval_mode='DAG_EVAL_VIEWPORT', export_selected_objects=True, export_uv=True, export_normals=True, export_colors=False, export_materials=True, export_pbr_extensions=False, path_mode='AUTO', export_triangulated_mesh=False, export_curves_as_nurbs=False, export_object_groups=False, export_material_groups=True, export_vertex_groups=False, export_smooth_groups=False, smooth_group_bitflags=False, filter_glob='*.obj;*.mtl')
	######################################################################################

	
	######################################################################################
	## function to export obj file (must be parented to a Empty)
	######################################################################################
	def export_rotor_as_obj(collection_name, folder_name, object_name, empty_name, scale, ascii_file):
		bpy.ops.object.mode_set(mode='OBJECT')
		bpy.ops.object.select_all(action='DESELECT')
		
		## check, if collection exists
		collection_exists = False
		for collection in bpy.data.collections:
			#print(collection.name)
			if collection.name == collection_name:
				collection_exists = True
				
		if collection_exists == True:	
		
			## check, if name exists
			exists = False
			for ob in bpy.context.scene.objects:
				if ob.name == empty_name:
					exists = True

			if exists:
				# get the object
				object = bpy.data.objects[empty_name]
				object.select_set(True)
				
				# store the current location
				save_location = object.location.copy()
				#save_rotation_euler = object.rotation_euler.copy()
				save_rotation_quaternion = object.matrix_world.to_quaternion().copy()
				
				# coordiante system in blender is different to unity	
				reference_orientation = mathutils.Quaternion((0.7071068286895752, 0.7071068286895752, 0.0, 0.0)).copy()
				#difference_orientation = object.matrix_world.to_quaternion().rotation_difference(reference_orientation)
				difference_orientation = reference_orientation.rotation_difference(object.matrix_world.to_quaternion()).copy()
				
				# to_euler() function uses blender's space fixed (extrinsic), right handed x-y-z rotation (S123r)
				ascii_file.writelines([empty_name + '\n'])
				ascii_file.writelines(['location, coordiante system blender, right handed, scale 1.0: ' + str(object.location.x) + '    ' + str(object.location.y) + '    ' + str(object.location.z) + '\n'])
				scale_test = 0.2 # 0.01132
				ascii_file.writelines(['location, coordiante system untiy, left handed, scale ' + str(scale_test) + ': ' + str(object.location.x*scale_test) + '    ' + str(object.location.z*scale_test) + '    ' + str(-object.location.y*scale_test) + '\n'])
				ascii_file.writelines(['location, coordiante system untiy, left handed, scale ' + str(1) + ': ' + str(object.location.x*1) + '    ' + str(object.location.z*1) + '    ' + str(-object.location.y*1) + '\n'])
				ascii_file.writelines(['orientation blenders euler space-fixed xyz, rad: ' + str(difference_orientation.to_euler('XYZ').x) + '    ' + str(difference_orientation.to_euler('XYZ').y) + '    ' + str(difference_orientation.to_euler('XYZ').z) + '\n'])
				ascii_file.writelines(['orientation blenders euler space-fixed xyz, deg: ' + str(difference_orientation.to_euler('XYZ').x*(180/math.pi)) + '    ' + str(difference_orientation.to_euler('XYZ').y*(180/math.pi)) + '    ' + str(difference_orientation.to_euler('XYZ').z*(180/math.pi)) + '\n'])
				ascii_file.writelines(['\n'])
				
				# adding adjustment values to the property
				object.location = mathutils.Vector((0.0, 0.0, 0.0))
				object.rotation_mode = 'QUATERNION'
				object.rotation_quaternion = reference_orientation
				
				## select all objects of a collection 
				for obj in bpy.data.collections[collection_name].all_objects:
					obj.select_set(True)
					
				object.select_set(True)
				
				target_file = os.path.join( target_folder +'\\3D_' + folder_name, name_in_unity + '_' + object_name + '.obj')
				#bpy.ops.export_scene.obj(filepath=target_file, check_existing=False, filter_glob="*.obj;*.mtl", use_selection=True, use_animation=False, use_mesh_modifiers=True, use_edges=True, use_smooth_groups=False, use_smooth_groups_bitflags=False, use_normals=True, use_uvs=True, use_materials=True, use_triangles=False, use_nurbs=False, use_vertex_groups=False, use_blen_objects=True, group_by_object=False, group_by_material=True, keep_vertex_order=False, global_scale=scale, path_mode='AUTO', axis_forward='Z', axis_up='Y')
				bpy.ops.wm.obj_export(filepath=target_file, check_existing=False, filter_blender=False, filter_backup=False, filter_image=False, filter_movie=False, filter_python=False, filter_font=False, filter_sound=False, filter_text=False, filter_archive=False, filter_btx=False, filter_collada=False, filter_alembic=False, filter_usd=False, filter_obj=False, filter_volume=False, filter_folder=True, filter_blenlib=False, filemode=8, display_type='DEFAULT', sort_method='DEFAULT',export_animation=False,start_frame=-2147483648, end_frame=2147483647, forward_axis='Z', up_axis='Y', global_scale=scale, apply_modifiers=True, export_eval_mode='DAG_EVAL_VIEWPORT', export_selected_objects=True, export_uv=True, export_normals=True, export_colors=False, export_materials=True, export_pbr_extensions=False, path_mode='AUTO', export_triangulated_mesh=False, export_curves_as_nurbs=False, export_object_groups=False, export_material_groups=True, export_vertex_groups=False, export_smooth_groups=False, smooth_group_bitflags=False,filter_glob="*.obj;*.mtl")      
				
				# restore 
				object.location = save_location
				object.rotation_quaternion = save_rotation_quaternion
				object.rotation_mode = 'XYZ'
	######################################################################################


	#######################################################################################
	### function to export fbx file
	#######################################################################################
	##def export_model_as_fbx(collection_name, object_name, scale):
	##	bpy.ops.object.mode_set(mode='OBJECT')	
	##	bpy.ops.object.select_all(action='DESELECT')
	##	
	##	## check, if collection exists
	##	collection_exists = False
	##	for collection in bpy.data.collections:
	##		#print(collection.name)
	##		if collection.name == collection_name:
	##			collection_exists = True
	##	
	##	if collection_exists == True:	
	##		## select all objects of a collection 
	##		for obj in bpy.data.collections[collection_name].all_objects:
	##			obj.select_set(True)
	##			
	##		if len(bpy.context.selected_objects):     
	##			target_file = os.path.join( target_folder + object_name) 
	##			bpy.ops.export_scene.fbx(filepath=target_file, check_existing=False, filter_glob='*.fbx', use_selection=True, use_active_collection=False, global_scale=scale, apply_unit_scale=True, apply_scale_options='FBX_SCALE_NONE', bake_space_transform=False, object_types={'ARMATURE', 'CAMERA', 'EMPTY', 'LIGHT', 'MESH', 'OTHER'}, use_mesh_modifiers=True, use_mesh_modifiers_render=True, mesh_smooth_type='OFF', use_subsurf=False, use_mesh_edges=False, use_tspace=False, use_custom_props=False, add_leaf_bones=True, primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=False, armature_nodetype='NULL', bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=True, bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=1.0, path_mode='STRIP', embed_textures=False, batch_mode='OFF', use_batch_own_dir=True, use_metadata=True, axis_forward='-Z', axis_up='Y')
	#######################################################################################

	######################################################################################
	def export_model_as_fbx(collection_name, object_name, scale):
		print("export_model_as_fbx: " + object_name) 
		bpy.ops.object.mode_set(mode='OBJECT')	
		bpy.ops.object.select_all(action='DESELECT')
		
		## check, if collection exists
		collection_exists = False
		for collection in bpy.data.collections:
			#print(collection.name)
			if collection.name == collection_name:
				collection_exists = True
			
		if collection_exists == True:	
			# select collection
			layerColl = recurLayerCollection(bpy.context.view_layer.layer_collection, collection_name)
			if layerColl:
				# select all objects of a collection  to check if collection is empty
				for obj in bpy.data.collections[collection_name].all_objects:
					obj.select_set(True)	
				# if collection is not excluded and is not empty then export content
				if not layerColl.exclude and len(bpy.context.selected_objects):
					bpy.context.view_layer.active_layer_collection = layerColl
					target_file = os.path.join( target_folder + object_name) 
					bpy.ops.export_scene.fbx(filepath=target_file, check_existing=False, filter_glob='*.fbx', use_selection=False, use_active_collection=True, global_scale=scale, apply_unit_scale=True, apply_scale_options='FBX_SCALE_NONE', bake_space_transform=False, object_types={'ARMATURE', 'EMPTY', 'MESH', 'OTHER'}, use_mesh_modifiers=True, use_mesh_modifiers_render=True, mesh_smooth_type='OFF', use_subsurf=False, use_mesh_edges=False, use_tspace=False, use_custom_props=False, add_leaf_bones=True, primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=False, armature_nodetype='NULL', bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=True, bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=1.0, path_mode='STRIP', embed_textures=False, batch_mode='OFF', use_batch_own_dir=True, use_metadata=True, axis_forward='-Z', axis_up='Y')
					print("Collection found")
			else:	
				print("Collection not found")
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
	export_model_as_obj('FUSELAGE_EXPORT', 'Fuselage', scale)
	#export_model_as_obj('MAINROTOR_EXPORT', 'Mainrotor', scale)
	#export_model_as_obj('TAILROTOR_EXPORT', 'Tailrotor', scale)
	#export_model_as_obj('PROPELLER_EXPORT', 'Propeller', scale)
	textfile1 = 'rotor_export_info.txt'
	f = open( os.path.dirname(bpy.data.filepath) + '\\' + textfile1 , 'w')
	f.writelines(['Info: Blenders rotation squence is a "space fixed 123" (S123) rotation and not a "body fixed 123" rotation.\n\n'])
	export_rotor_as_obj('MAINROTOR_EXPORT'          , 'Mainrotor'   , 'Mainrotor'           , 'Empty_Mainrotor'             , scale, f)
	export_rotor_as_obj('MAINROTOR_BLUR_EXPORT'     , 'Mainrotor'   , 'Mainrotor_Blur'      , 'Empty_Mainrotor_Blur'        , scale, f)
	export_rotor_as_obj('MAINGEAR_EXPORT'           , 'Maingear'    , 'Maingear'            , 'Empty_Maingear'              , scale, f)
	export_rotor_as_obj('TAILROTOR_EXPORT'          , 'Tailrotor'   , 'Tailrotor'           , 'Empty_Tailrotor'             , scale, f)
	export_rotor_as_obj('TAILROTOR_BLUR_EXPORT'     , 'Tailrotor'   , 'Tailrotor_Blur'      , 'Empty_Tailrotor_Blur'        , scale, f)
	export_rotor_as_obj('PROPELLER_EXPORT'          , 'Propeller'   , 'Propeller'           , 'Empty_Propeller'             , scale, f)
	export_rotor_as_obj('PROPELLER_BLUR_EXPORT'     , 'Propeller'   , 'Propeller_Blur'      , 'Empty_Propeller_Blur'        , scale, f)
	export_rotor_as_obj('MOTOR_EXPORT'              , 'Motor'       , 'Motor'               , 'Empty_Motor'                 , scale, f)
	f.close() 
	
	## SETUP EXPORT
	for i in range(9):
		num = str(i).zfill(3)
		export_model_as_fbx('SETUP_' + num + '_EXPORT', '\\Resources\\' + name_in_unity + '_Setup_' + num + '\\Prefabs\\' + name_in_unity + '_setup_' + num + '.fbx', scale)

	## SETUP DOORS ANIMATION EXPORT
	for i in range(9):
		num = str(i).zfill(3)
		export_model_as_fbx('ANIMATION_DOORS_SETUP_' + num + '_EXPORT', '\\Resources\\' + name_in_unity + '_Setup_' + num + '\\Prefabs\\' + name_in_unity + '_setup_' + num + '_Doors_Animation.fbx', scale)

	export_model_as_fbx('WHEEL_LEFT_EXPORT', '\\Animation\\' + name_in_unity + '_Gear_Or_Skid_Animation_Left.fbx', scale)
	export_model_as_fbx('WHEEL_RIGHT_EXPORT', '\\Animation\\' + name_in_unity + '_Gear_Or_Skid_Animation_Right.fbx', scale)
	export_model_as_fbx('WHEEL_STEERING_CENTER_EXPORT', '\\Animation\\' + name_in_unity + '_Gear_Or_Support_Animation_Steering_Center.fbx', scale)
	export_model_as_fbx('WHEEL_STEERING_RIGHT_EXPORT', '\\Animation\\' + name_in_unity + '_Gear_Or_Support_Animation_Steering_Right.fbx', scale)
	export_model_as_fbx('WHEEL_STEERING_LEFT_EXPORT', '\\Animation\\' + name_in_unity + '_Gear_Or_Support_Animation_Steering_Left.fbx', scale)

	export_model_as_fbx('SKIDS_LEFT_EXPORT', '\\Animation\\' + name_in_unity + '_Gear_Or_Skid_Animation_Left.fbx', scale)
	export_model_as_fbx('SKIDS_RIGHT_EXPORT', '\\Animation\\' + name_in_unity + '_Gear_Or_Skid_Animation_Right.fbx', scale)
	
	## ANIMATION_SKIDS
	for i in range(9):
		num = str(i).zfill(3)
		export_model_as_fbx('SKIDS_LEFT_SETUP_' + num + '_EXPORT', '\\Resources\\' + name_in_unity + '_Setup_' + num + '\\Prefabs\\' + name_in_unity + '_setup_' + num + '_Gear_Or_Skid_Animation_Left.fbx', scale)
		export_model_as_fbx('SKIDS_RIGHT_SETUP_' + num + '_EXPORT', '\\Resources\\' + name_in_unity + '_Setup_' + num + '\\Prefabs\\' + name_in_unity + '_setup_' + num + '_Gear_Or_Skid_Animation_Right.fbx', scale)
		export_model_as_fbx('WHEEL_STEERING_CENTER_SETUP_' + num + '_EXPORT', '\\Resources\\' + name_in_unity + '_Setup_' + num + '\\Prefabs\\' + name_in_unity + '_setup_' + num + '_Gear_Or_Support_Animation_Steering_Center.fbx', scale)
		export_model_as_fbx('ANIMATION_SPECIAL_SETUP_' + num + '_EXPORT', '\\Resources\\' + name_in_unity + '_Setup_' + num + '\\Prefabs\\' + name_in_unity + '_setup_' + num + '_Special_Animation.fbx', scale)


	export_model_as_fbx('ANIMATION_DOORS_EXPORT', '\\Animation\\' + name_in_unity + '_Doors_Animation.fbx', scale)
	export_model_as_fbx('SPEED_BRAKE_EXPORT', '\\Animation\\' + name_in_unity + '_Speed_Brake_Animation.fbx', scale)
	export_model_as_fbx('ANIMATION_PILOT_EXPORT', '\\Animation\\' + name_in_unity + '_Pilot_Animation.fbx', scale)

	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\3D_Fuselage\SikorskyS61_Fuselage.obj"
	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\3D_Mainrotor\SikorskyS61_Mainrotor.obj"
	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\3D_Tailrotor\SikorskyS61_Tailrotor.obj"
	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\Resources\SikorskyS61_Setup_000\Prefabs\SikorskyS61_setup_000.fbx"
	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\Resources\SikorskyS61_Setup_001\Prefabs\SikorskyS61_setup_001.fbx"
	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\Animation\SikorskyS61_Doors_Animation.fbx"
	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\Animation\SikorskyS61_Gear_Or_Support_Animation_Steering_Center.fbx"
	##"F:\Helikopter\UNITY\GitHub\HoverHere_\Assets\Helicopters_Available\SikorskyS61\Animation\SikorskyS61_Gear_Or_Skid_Animation_Right.fbx"

	##textfile1 = 'rotor_export_info.txt'
	##f = open(textfile1, 'w')
	##save_info_to_ascii(f,'Empty_Mainrotor')
	##save_info_to_ascii(f,'Empty_Tailrotor')
	##save_info_to_ascii(f,'Empty_Propeller')
	##f.close()




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
class EXPORT_HOVER_HERE_OT_objects(Operator):
	"""EXPORT HoverHere objects"""          # use this as a tooltip for menu items and buttons.

	bl_idname = "export_hover_here.objects"  # unique identifier for buttons and menu items to reference.
	bl_label = "Export HoverHere objects"   # display name in the interface.
	bl_options = {'REGISTER', 'UNDO'}   # enable undo for the operator.    

	# define this to tell 'fileselect_add' that we want a directory    name="Skymap environment path",
	# directory: bpy.props.StringProperty(maxlen=1024, subtype='FILE_PATH', options={'HIDDEN', 'SKIP_SAVE'})

	def execute(self, context):        # execute() is called when running the operator.
		
		#target_folder_relative = '../../../HoverHere_/Assets/Helicopters_Available/' 
		#name_in_unity = os.path.basename(os.path.dirname(bpy.data.filepath))
		#target_folder = os.path.abspath(target_folder_relative + name_in_unity)
		#print("Target Folder: '" + target_folder + "'")
		#print("Name: '" + name_in_unity + "'")
		bpy.data.scenes[0].frame_set(0)
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
def export_hover_here_objects_button(self, context):
    self.layout.operator(EXPORT_HOVER_HERE_OT_objects.bl_idname, text="Export HoverHere Helicopter", icon='IMAGE_DATA')


classes = (
    EXPORT_HOVER_HERE_OT_objects,
)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)

    bpy.types.TOPBAR_MT_file_export.append(export_hover_here_objects_button)


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(export_hover_here_objects_button)
 
    for cls in classes:
        if hasattr(bpy.types, cls.__name__):
            bpy.utils.unregister_class(cls)


if __name__ == "__main__":
    # run simple doc tests
    import doctest
    doctest.testmod()
    
    unregister()
    register()
