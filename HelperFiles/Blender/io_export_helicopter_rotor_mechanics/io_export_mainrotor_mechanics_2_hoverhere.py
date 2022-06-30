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

##    Export helicopter 3d rotor mechanics model to 
##    "HoverHere a Free RC Helicopter Simulator" Unity project

## 2021.08.03 V1.00 Zulu

#################################################################################
##    ____      ____    
##   /  _/___  / __/___ 
##   / // __ \/ /_/ __ \
## _/ // / / / __/ /_/ /
##/___/_/ /_/_/  \____/ 
##
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







################################################################################
## XML
################################################################################
def xml_write_scalar_1x1_as_1x1(xml_file, scalar_1x1, element_name):
    xml_file.write('  <'+element_name+'>' + str(scalar_1x1)  +  '</'+element_name+'>' + '\n')

def xml_write_vector_1x3_as_1x3(xml_file, vector_1x3, element_name):
    xml_file.write('  <'+element_name+'>'+ '\n')
    xml_file.write('   <x>'+  str(vector_1x3[0])  + '</x>' + '\n')
    xml_file.write('   <y>'+  str(vector_1x3[1])  + '</y>' + '\n')
    xml_file.write('   <z>'+  str(vector_1x3[2])  + '</z>' + '\n')
    xml_file.write('  </'+element_name+'>' + '\n')
    
def xml_write_matrix_3x3_as_1x9(xml_file, matrix_3x3, element_name):
    xml_file.write('  <'+element_name+'>' + '  <!-- row-major order -->'  +'\n')
    xml_file.write('   <float>'+  str(matrix_3x3[0][0])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[0][1])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[0][2])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[1][0])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[1][1])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[1][2])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[2][0])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[2][1])  + '</float>' + '\n')
    xml_file.write('   <float>'+  str(matrix_3x3[2][2])  + '</float>' + '\n')
    xml_file.write('  </'+element_name+'>' + '\n')

################################################################################
    



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
    name_in_unity = os.path.basename(os.path.dirname(os.path.dirname(bpy.data.filepath)))
    #target_folder = bpy.path.abspath(target_folder_relative + name_in_unity)
    target_folder = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(bpy.data.filepath))))) + target_folder_relative + '\\' + name_in_unity
    print(target_folder)
    ######################################################################################


    ######################################################################################
    ## function to export obj file
    ######################################################################################
    def export_model_as_obj(collection_name, folder_name, object_name, scale):
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
                target_file = os.path.join( target_folder +'\\3D_' + folder_name, name_in_unity + '_' + object_name + '.obj')
                bpy.ops.export_scene.obj(filepath=target_file, check_existing=False, filter_glob="*.obj;*.mtl", use_selection=True, use_animation=False, use_mesh_modifiers=True, use_edges=True, use_smooth_groups=False, use_smooth_groups_bitflags=False, use_normals=True, use_uvs=True, use_materials=True, use_triangles=False, use_nurbs=False, use_vertex_groups=False, use_blen_objects=True, group_by_object=False, group_by_material=True, keep_vertex_order=False, global_scale=scale, path_mode='AUTO', axis_forward='Z', axis_up='Y')
    ######################################################################################
    
    
    
    
    
    ######################################################################################
    ## function to export obj file
    ######################################################################################
    def export_model_relative_to_empty_as_obj(collection_name, folder_name, object_name, empty_name, scale):
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
                
                ## select all objects of a collection   and also rotate and translate objects according to empty
                ii=0
                save_location = []
                save_rotation_quaternion = []
                for object in bpy.data.collections[collection_name].all_objects:
                    apply_transfrom(object)
                    
                    # store the current location
                    save_location.append(object.location.copy())
                    save_rotation_quaternion.append(object.matrix_world.to_quaternion().copy())
                         
                    # adding adjustment values to the property
                    object.location = mathutils.Quaternion(bpy.data.objects[empty_name].matrix_world.to_quaternion()).inverted() @ -bpy.data.objects[empty_name].location
                    object.rotation_mode = 'QUATERNION'
                    #object.rotation_quaternion = mathutils.Quaternion(bpy.data.objects[empty_name].matrix_world.to_quaternion()).copy()   # coordiante system in blender is different to unity
                    object.rotation_quaternion = bpy.data.objects[empty_name].matrix_world.to_quaternion().inverted()
                    
                    object.select_set(True)
                    ii=ii+1
                    
                if len(bpy.context.selected_objects): 
                    target_file = os.path.join( target_folder +'\\3D_' + folder_name, name_in_unity + '_' + object_name + '.obj')
                    bpy.ops.export_scene.obj(filepath=target_file, check_existing=False, filter_glob="*.obj;*.mtl", use_selection=True, use_animation=False, use_mesh_modifiers=True, use_edges=True, use_smooth_groups=False, use_smooth_groups_bitflags=False, use_normals=True, use_uvs=True, use_materials=True, use_triangles=False, use_nurbs=False, use_vertex_groups=False, use_blen_objects=True, group_by_object=False, group_by_material=True, keep_vertex_order=False, global_scale=scale, path_mode='AUTO', axis_forward='Y', axis_up='-Z')
                
                ii=0   
                for object in bpy.data.collections[collection_name].all_objects:   
                    ## restore postion and orientation 
                    object.location = save_location[ii]
                    object.rotation_quaternion = save_rotation_quaternion[ii]
                    object.rotation_mode = 'XYZ'
                    ii=ii+1
                    
    ######################################################################################
    
    
    
    
    ######################################################################################
    ## function to export obj file (must be parented to a Empty)
    ######################################################################################
    def export_rotor_mechanics_as_obj(collection_name, folder_name, object_name, empty_name, scale, ascii_file):
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
                
                # coordiante system in blender is different to unity, therefore empty is not rotated to [1,0,0,0], but to [0.707 0.707 0 0]
                reference_orientation = mathutils.Quaternion((0.7071068286895752, 0.7071068286895752, 0.0, 0.0)).copy()
                #difference_orientation = object.matrix_world.to_quaternion().rotation_difference(reference_orientation)
                difference_orientation = reference_orientation.rotation_difference(object.matrix_world.to_quaternion()).copy()
                
                # to_euler() function uses blender's space fixed (extrinsic), right handed x-y-z rotation (S123r)
                # ascii_file.writelines([empty_name + '\n'])
                #ascii_file.writelines(['location, coordiante system blender, right handed, scale 1.0: ' + str(object.location.x) + '    ' + str(object.location.y) + '    ' + str(object.location.z) + '\n'])
                #scale_test = 0.2 # 0.01132
                #ascii_file.writelines(['location, coordiante system untiy, left handed, scale ' + str(scale_test) + ': ' + str(object.location.x*scale_test) + '    ' + str(object.location.z*scale_test) + '    ' + str(-object.location.y*scale_test) + '\n'])
                #ascii_file.writelines(['location, coordiante system untiy, left handed, scale ' + str(1) + ': ' + str(object.location.x*1) + '    ' + str(object.location.z*1) + '    ' + str(-object.location.y*1) + '\n'])
                #ascii_file.writelines(['orientation blenders euler space-fixed xyz, rad: ' + str(difference_orientation.to_euler('XYZ').x) + '    ' + str(difference_orientation.to_euler('XYZ').y) + '    ' + str(difference_orientation.to_euler('XYZ').z) + '\n'])
                #ascii_file.writelines(['orientation blenders euler space-fixed xyz, deg: ' + str(difference_orientation.to_euler('XYZ').x*(180/math.pi)) + '    ' + str(difference_orientation.to_euler('XYZ').y*(180/math.pi)) + '    ' + str(difference_orientation.to_euler('XYZ').z*(180/math.pi)) + '\n'])
                #ascii_file.writelines(['\n'])
                
                # adding adjustment values to the property
                object.location = mathutils.Vector((0.0, 0.0, 0.0))
                object.rotation_mode = 'QUATERNION'
                object.rotation_quaternion = reference_orientation
                
                ## select all objects of a collection 
                #for obj in bpy.data.collections[collection_name].all_objects:
                #    obj.select_set(True)
                
                ## select all chilren objects of empty
                for obj in bpy.data.objects[empty_name].children:
                    obj.select_set(True)
                                    
                object.select_set(True)
                
                target_file = os.path.join( target_folder +'\\3D_' + folder_name, name_in_unity + '_' + folder_name + '_' + object_name + '.obj')
                bpy.ops.export_scene.obj(filepath=target_file, check_existing=False, filter_glob="*.obj;*.mtl", use_selection=True, use_animation=False, use_mesh_modifiers=True, use_edges=True, use_smooth_groups=False, use_smooth_groups_bitflags=False, use_normals=True, use_uvs=True, use_materials=True, use_triangles=False, use_nurbs=False, use_vertex_groups=False, use_blen_objects=True, group_by_object=False, group_by_material=True, keep_vertex_order=False, global_scale=scale, path_mode='AUTO', axis_forward='Z', axis_up='Y')
                
                # restore 
                object.location = save_location
                object.rotation_quaternion = save_rotation_quaternion
                object.rotation_mode = 'XYZ'
    ######################################################################################





    ###############################################################################  
    ## the coordinate systems are set up the way, that they are compatible to unitys "Quaternion.LookRotation()"
    ###############################################################################  
    def create_coordinate_system_from_points(par_blades_count):
        
        # swashplate stationary
        C0 = bpy.context.scene.objects["Empty_C0"].location  ## point in global system (the resulting coordinate system's y-axis will NOT hit this point (or only in special case))
        Mj = bpy.context.scene.objects["Empty_K"].location   ## point in global system (origin)
        nj = mathutils.Vector([0.0, 0.0, 1.0])               ## direction in global system  
        bpy.context.scene.objects["Empty_Axes_Swashplate_Stationary"].matrix_world = create_coordinate_system(C0,Mj,(nj-Mj)) ## TODO should be (nj+Mj) ????
        
        # swashplate rotating
        D0 = bpy.context.scene.objects["Empty_D0"].location
        Mj = bpy.context.scene.objects["Empty_K"].location
        nj = mathutils.Vector([0.0, 0.0, 1.0])     
        bpy.context.scene.objects["Empty_Axes_Swashplate_Rotating"].matrix_world = create_coordinate_system(D0,Mj,(nj-Mj)) ## TODO should be (nj+Mj) ????
        
        for ii in range (0,3):
            # servo arms from Ai to Bi
            Bi = bpy.context.scene.objects["Empty_B"+str(ii)].location    
            Ai = bpy.context.scene.objects["Empty_A"+str(ii)].location
            A0i = bpy.context.scene.objects["Empty_A"+str(ii)+"0"].location    ## creates with Empty_A i normal n_{Servoi}
            bpy.context.scene.objects["Empty_Axes_A"+str(ii)].matrix_world = create_coordinate_system(Bi,Ai,A0i)
            print((Bi-Ai).length)
              
            # connection rods from Bi to Ci
            Mj = bpy.context.scene.objects["Empty_K"].location
            Bi = bpy.context.scene.objects["Empty_B"+str(ii)].location
            Ci = bpy.context.scene.objects["Empty_C"+str(ii)].location    
            bpy.context.scene.objects["Empty_Axes_B"+str(ii)].matrix_world = create_coordinate_system(Mj,Bi,Ci)
            print((Ci-Bi).length)
            
        for ii in range (0,par_blades_count):   
            # connection rods from Di to Ei
            Mj = bpy.context.scene.objects["Empty_K"].location
            Di = bpy.context.scene.objects["Empty_D"+str(ii)].location
            Ei = bpy.context.scene.objects["Empty_E"+str(ii)].location  
            bpy.context.scene.objects["Empty_Axes_D"+str(ii)].matrix_world = create_coordinate_system(Mj,Di,Ei)   
            print((Ei-Di).length)
            # blades i
            Ei = bpy.context.scene.objects["Empty_E"+str(ii)].location
            Bi = mathutils.Vector([0.0, 0.0, 0.0])
            n_blade_i = mathutils.Matrix.Rotation(-ii*math.radians(360/par_blades_count), 3, 'Z') @ mathutils.Vector([1.0, 0.0, 0.0]) 
            bpy.context.scene.objects["Empty_Axes_Blade"+str(ii)].matrix_world = create_coordinate_system(Ei,Bi,n_blade_i)   
            
        ii=0
        Fi = bpy.context.scene.objects["Empty_F"+str(ii)].location
        Gi = bpy.context.scene.objects["Empty_G"+str(ii)].location
        n = mathutils.Vector([0.0, 1.0, 0.0])
        bpy.context.scene.objects["Empty_Axes_F"+str(ii)].matrix_world = create_coordinate_system(Gi,Fi,(n+Fi)) 
        Gi = bpy.context.scene.objects["Empty_G"+str(ii)].location
        Hi = bpy.context.scene.objects["Empty_H"+str(ii)].location
        n = mathutils.Vector([0.0, 1.0, 0.0])
        bpy.context.scene.objects["Empty_Axes_G"+str(ii)].matrix_world = create_coordinate_system(Hi,Gi,(n+Gi))   
        
        ii=1
        Fi = bpy.context.scene.objects["Empty_F"+str(ii)].location
        Gi = bpy.context.scene.objects["Empty_G"+str(ii)].location
        n = mathutils.Vector([0.0, -1.0, 0.0])
        bpy.context.scene.objects["Empty_Axes_F"+str(ii)].matrix_world = create_coordinate_system(Gi,Fi,(n+Fi))  
        Gi = bpy.context.scene.objects["Empty_G"+str(ii)].location
        Hi = bpy.context.scene.objects["Empty_H"+str(ii)].location
        n = mathutils.Vector([0.0, -1.0, 0.0])
        bpy.context.scene.objects["Empty_Axes_G"+str(ii)].matrix_world = create_coordinate_system(Hi,Gi,(n+Gi)) 
    ################################################################################  
      
      
      
      
      
    ################################################################################
    ## 
    ################################################################################     
    def create_coordinate_system(M,Start,End):
                
        forward = -(End-Start) # but with negative sign -> at left handed system the z will show in direction of Start->End
        upward = (M-Start)
        # build transforamtion matrix (see https://docs.unity3d.com/ScriptReference/Quaternion.LookRotation.html)
        # z = forward              
        # x = cross(upward, forward)
        # y = cross(z, x)
        matrix_ = mathutils.Matrix((  mathutils.Vector.cross(upward,forward).normalized(),     mathutils.Vector.cross(forward, mathutils.Vector.cross(upward,forward)).normalized(),    forward.normalized()  ))

        return mathutils.Matrix.Translation(Start) @ matrix_.to_4x4().transposed()
    ################################################################################



    ################################################################################
    ##
    ################################################################################
    def parent_3d_geometry_to_empty(name_child,name_parent):
        
        a = bpy.data.objects[name_parent]
        b = bpy.data.objects[name_child]

        bpy.ops.object.select_all(action='DESELECT') # deselect all object

        a.select_set(True)
        b.select_set(True)     # select the object for the 'parenting'

        bpy.context.view_layer.objects.active = a    # the active object will be the parent of all selected object

        bpy.ops.object.parent_set()
    ################################################################################



    ################################################################################
    ##
    ################################################################################
    def parent2empty_and_export_part(name_part, name_empty, par_scale, file_object):
        
        parent_3d_geometry_to_empty(name_part,name_empty)
        export_rotor_mechanics_as_obj('MAINROTOR_MECHANICS_EXPORT', 'Mainrotor', name_part, name_empty, par_scale, file_object)

    ################################################################################







    ################################################################################
    ## 3D object export (as .OBJ)
    ################################################################################
    print('########################################')
    par_blades_count = 2
    par_scale = 1
    textfile1 = 'mainrotor_mechanics_export_info.txt'
    ascii_file = open( os.path.dirname(bpy.data.filepath) + '\\' + textfile1 , 'w')
    
    for ii in range (0,3): 
        ## correct position of servo arm coordiante system along servo's local z-axis, that xy-plane will go through Bi
        r_BiO_O = bpy.context.scene.objects["Empty_B"+str(ii)].location
        A_AiO = bpy.data.objects["Empty_Axes_A"+str(ii)].matrix_world.to_quaternion().to_matrix() 
        z = A_AiO @ mathutils.Vector((0,0,1))
        r_AiO_O = bpy.data.objects["Empty_Axes_A"+str(ii)].location
        correction_along_Ai_z = (r_AiO_O - r_BiO_O) @ z
        bpy.data.objects["Empty_Axes_A"+str(ii)].location = r_AiO_O - z. normalized()*correction_along_Ai_z
        bpy.data.objects["Empty_A"+str(ii)].location = bpy.data.objects["Empty_Axes_A"+str(ii)].location
        
    create_coordinate_system_from_points(par_blades_count)
    
    parent2empty_and_export_part('Rod_C0B0','Empty_Axes_B0',par_scale,ascii_file)
    parent2empty_and_export_part('Rod_C1B1','Empty_Axes_B1',par_scale,ascii_file)
    parent2empty_and_export_part('Rod_C2B2','Empty_Axes_B2',par_scale,ascii_file)

    parent2empty_and_export_part('Servoarm_B0A0','Empty_Axes_A0',par_scale,ascii_file)
    parent2empty_and_export_part('Servoarm_B1A1','Empty_Axes_A1',par_scale,ascii_file)
    parent2empty_and_export_part('Servoarm_B2A2','Empty_Axes_A2',par_scale,ascii_file)

    parent2empty_and_export_part('Swashpalte_Stationary','Empty_Axes_Swashplate_Stationary',par_scale,ascii_file)
    parent2empty_and_export_part('Swashpalte_Rotating','Empty_Axes_Swashplate_Rotating',par_scale,ascii_file)

    for ii in range (0,par_blades_count):   
        parent2empty_and_export_part('Rod_E'+str(ii)+'D'+str(ii),'Empty_Axes_D'+str(ii)    ,par_scale,ascii_file)
        parent2empty_and_export_part('Blade'+str(ii)            ,'Empty_Axes_Blade'+str(ii),par_scale,ascii_file)
   
    ##export_model_as_obj('MAINROTOR_MECHANICS_FIXED_EXPORT', 'Mainrotor', 'Mainrotor_Mechanics_Fixed', scale)
    ##export_model_as_obj('MAINROTOR_MECHANICS_ROTATING_EXPORT', 'Mainrotor', 'Mainrotor_Mechanics_Rotating', scale)   
    export_model_relative_to_empty_as_obj('MAINROTOR_MECHANICS_FIXED_EXPORT', 'Mainrotor', 'Mainrotor_Mechanics_Fixed', 'Empty_O', scale)  ###
    export_model_relative_to_empty_as_obj('MAINROTOR_MECHANICS_ROTATING_EXPORT', 'Mainrotor', 'Mainrotor_Mechanics_Rotating', 'Empty_O', scale)    ###

    parent2empty_and_export_part('Ball','Empty_Axes_Ball',par_scale,ascii_file)
   
    parent2empty_and_export_part('Swashplate_Driver_F0G0','Empty_Axes_F0',par_scale,ascii_file) 
    parent2empty_and_export_part('Swashplate_Driver_G0H0','Empty_Axes_G0',par_scale,ascii_file) 
    parent2empty_and_export_part('Swashplate_Driver_F1G1','Empty_Axes_F1',par_scale,ascii_file) 
    parent2empty_and_export_part('Swashplate_Driver_G1H1','Empty_Axes_G1',par_scale,ascii_file) 
    ################################################################################




    ################################################################################
    ## parameter export (as .XML)
    ################################################################################
    with open(   os.path.dirname(bpy.data.filepath) + '\\' +  name_in_unity +'_mainrotor_mechanics.xml'  , "w", encoding="utf-8") as file: 
        file.write('<?xml version="1.0"?>' + '\n')
        file.write('<stru_mainrotor_mechanics xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">'+ '\n')  
        reference_coordinate_system_O = bpy.data.objects["Empty_O"].matrix_world.to_quaternion()
        xml_write_scalar_1x1_as_1x1(file, par_blades_count , 'blades_count')
        save_r_KO_O = mathutils.Quaternion(reference_coordinate_system_O).to_matrix().transposed() @ bpy.context.scene.objects["Empty_K"].location
        xml_write_vector_1x3_as_1x3(file, save_r_KO_O , 'r_KO_O')
        file.write('<limb_stationary>' + '\n')
        for ii in range (0,3):
            file.write('<!-- ############## STATIONARY LIMB ' + str(ii) + ' ############## -->' + '\n')
            file.write(' <stru_limb_stationary>' + '\n')
            file.write(' <parameter>' + '\n')
            save_A_OLAi         = mathutils.Quaternion(reference_coordinate_system_O).rotation_difference(bpy.data.objects["Empty_Axes_A"+str(ii)].matrix_world.to_quaternion()).to_matrix() # get rotation matrix A_RL (local --> reference) relative to reference coordinate system
            save_r_AiO_O        = mathutils.Quaternion(reference_coordinate_system_O).to_matrix().transposed() @ bpy.context.scene.objects["Empty_A"+str(ii)].location 
            save_r_BiAi_LAi     = (bpy.data.objects["Empty_Axes_A"+str(ii)].matrix_world.to_quaternion()).to_matrix().transposed()  @ (bpy.context.scene.objects["Empty_B"+str(ii)].location - bpy.context.scene.objects["Empty_A"+str(ii)].location) ## TODO transposed???
            save_r_CiK_LK       = mathutils.Quaternion(reference_coordinate_system_O).to_matrix().transposed() @ (bpy.context.scene.objects["Empty_C"+str(ii)].location - bpy.context.scene.objects["Empty_K"].location)
            save_l_BiAi         = (bpy.context.scene.objects["Empty_B"+str(ii)].location - bpy.context.scene.objects["Empty_A"+str(ii)].location).length
            save_l_CiBi         = (bpy.context.scene.objects["Empty_C"+str(ii)].location - bpy.context.scene.objects["Empty_B"+str(ii)].location).length
            xml_write_matrix_3x3_as_1x9(file, save_A_OLAi, 'A_OLAi')
            xml_write_vector_1x3_as_1x3(file, save_r_AiO_O, 'r_AiO_O')
            xml_write_vector_1x3_as_1x3(file, save_r_BiAi_LAi, 'r_BiAi_LAi') # "LAi": local coordinate system of servo-i
            xml_write_vector_1x3_as_1x3(file, save_r_CiK_LK, 'r_CiK_LK') # "LC": local coordinate system of stationary swashpalte == O == R
            xml_write_scalar_1x1_as_1x1(file, save_l_BiAi, 'l_BiAi')
            xml_write_scalar_1x1_as_1x1(file, save_l_CiBi, 'l_CiBi')
            file.write(' </parameter>' + '\n')
            file.write(' </stru_limb_stationary>' + '\n')
        file.write('</limb_stationary>' + '\n')
        file.write('<!-- ############## -->' + '\n')
        file.write('<limb_rotating>' + '\n')
        for ii in range (0,2):
             file.write('<!-- ############## ROTATING LIMB ' + str(ii) + ' ############## -->' + '\n')
             file.write(' <stru_limb_rotating>' + '\n')
             file.write(' <parameter>' + '\n')
             save_r_DiJ_LJ      =  mathutils.Quaternion(reference_coordinate_system_O).to_matrix().transposed() @ (bpy.context.scene.objects["Empty_D"+str(ii)].location - bpy.context.scene.objects["Empty_K"].location)
             save_r_EiO_O       =  mathutils.Quaternion(reference_coordinate_system_O).to_matrix().transposed() @ (bpy.context.scene.objects["Empty_E"+str(ii)].location)
             save_l_DiJ         = (bpy.context.scene.objects["Empty_D"+str(ii)].location - bpy.context.scene.objects["Empty_J"].location).length
             save_l_EiDi        = (bpy.context.scene.objects["Empty_E"+str(ii)].location - bpy.context.scene.objects["Empty_D"+str(ii)].location).length
             save_l_EiO         = (bpy.context.scene.objects["Empty_E"+str(ii)].location).length
             xml_write_vector_1x3_as_1x3(file, save_r_DiJ_LJ, 'r_DiJ_LJ')
             xml_write_vector_1x3_as_1x3(file, save_r_EiO_O, 'r_EiO_O')
             xml_write_scalar_1x1_as_1x1(file, save_l_DiJ, 'l_DiJ')
             xml_write_scalar_1x1_as_1x1(file, save_l_EiDi, 'l_EiDi')
             xml_write_scalar_1x1_as_1x1(file, save_l_EiO, 'l_EiO')
             file.write(' </parameter>' + '\n')
             file.write(' </stru_limb_rotating>' + '\n')
        file.write('</limb_rotating>' + '\n')
        file.write('<!-- ############## -->' + '\n')
        file.write('<driver_rotating>' + '\n')
        for ii in range (0,2):
             file.write('<!-- ############## ROTATING SWAHPLATE DRIVE ' + str(ii) + ' ############## -->' + '\n')
             file.write(' <stru_driver_rotating>' + '\n')
             file.write(' <parameter>' + '\n')
             save_r_F1O_O      =  mathutils.Quaternion(reference_coordinate_system_O).to_matrix().transposed() @ (bpy.context.scene.objects["Empty_F"+str(ii)].location)
             save_l_G1F1       = (bpy.context.scene.objects["Empty_G"+str(ii)].location - bpy.context.scene.objects["Empty_F"+str(ii)].location).length      
             save_l_H1G1       = (bpy.context.scene.objects["Empty_H"+str(ii)].location - bpy.context.scene.objects["Empty_G"+str(ii)].location).length
             save_l_H1J        = (bpy.context.scene.objects["Empty_H"+str(ii)].location - bpy.context.scene.objects["Empty_J"].location).length
             xml_write_vector_1x3_as_1x3(file, save_r_F1O_O, 'r_FiO_O')
             xml_write_scalar_1x1_as_1x1(file, save_l_G1F1, 'l_GiFi')
             xml_write_scalar_1x1_as_1x1(file, save_l_H1G1, 'l_HiGi')
             xml_write_scalar_1x1_as_1x1(file, save_l_H1J, 'l_HiJ')
             file.write(' </parameter>' + '\n')
             file.write(' </stru_driver_rotating>' + '\n')
        file.write('</driver_rotating>' + '\n')
        file.write('</stru_mainrotor_mechanics>')
    ################################################################################
        
        
       
       
       
       
        

export_objects()

      
                
                
################################################################################