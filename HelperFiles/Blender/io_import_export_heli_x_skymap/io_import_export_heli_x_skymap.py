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


## 2019.23.08 V1.00 Zulu
## 2019.30.08 V1.01
## 2020.04.14 V1.02
## 2020.10.09 V1.03 changed the height of the skybox,MS
## 2020.12.25 V1.04 find also skymap folder, if they are not explicitly given in xml

#################################################################################
##    ____      ____    
##   /  _/___  / __/___ 
##   / // __ \/ /_/ __ \
## _/ // / / / __/ /_/ /
##/___/_/ /_/_/  \____/ 
##
#################################################################################
## 
#################################################################################
bl_info = {
    "name": "Import Heli-X skybox environments and export Heli-X collision objects.",
    "author": "zulu",
    "version": (2, 0, 0),
    "blender": (2, 91, 00),
    "location": "File > Import > Heli-X Skybox",
    "description": "Imports Heli-X RC helicopter simulation skybox environment."
                   "The purpose is to create the collision objects for the simulation.",
    "warning": "",
    "wiki_url": "x",
    "support": 'OFFICIAL',
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
from xml.etree import cElementTree as ElementTree
import os
import warnings
import re
from itertools import count, repeat
from collections import namedtuple
from math import pi

import bpy
from bpy.types import Operator
from mathutils import Vector

import datetime
import bmesh

import copy
import time
#import gc # garbage collection
#################################################################################







#################################################################################
##    ____                           __     ______                 __  _                 
##   /  _/___ ___  ____  ____  _____/ /_   / ____/_  ______  _____/ /_(_)___  ____  _____
##   / // __ `__ \/ __ \/ __ \/ ___/ __/  / /_  / / / / __ \/ ___/ __/ / __ \/ __ \/ ___/
## _/ // / / / / / /_/ / /_/ / /  / /_   / __/ / /_/ / / / / /__/ /_/ / /_/ / / / (__  ) 
##/___/_/ /_/ /_/ .___/\____/_/   \__/  /_/    \__,_/_/ /_/\___/\__/_/\____/_/ /_/____/  
##             /_/  
##
#################################################################################
## IMPORT functionality: delete everything
#################################################################################
def resetScene():
    
    # bpy.ops.wm.read_homefile(use_empty=False)
    
    try:
        # go to object mode (if in edit mode)
        bpy.ops.object.mode_set(mode='OBJECT')
    except:
        print("No object selected")
        
    # deselect objects by type
    for obj in bpy.data.objects:
        obj.select_set(False)

    # select objects by type
    for o in bpy.context.scene.objects:
        o.select_set(True)
        
    # call the operator only once
    bpy.ops.object.delete()
    #bpy.data.objects.remove(objs["Cube"], do_unlink=True)
    
    
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)

    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)

    for block in bpy.data.textures:
        if block.users == 0:
            bpy.data.textures.remove(block)

    for block in bpy.data.images:
        if block.users == 0:
            bpy.data.images.remove(block)
        
    for block in bpy.data.cameras:
        if block.users == 0:
            bpy.data.cameras.remove(block)
    
    item='CAMERA'
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.object.select_by_type(type=item)
    bpy.ops.object.delete()
    
    
    # remove all collections
    for c in bpy.data.collections:
        bpy.data.collections.remove(c)
        
    # create new collection 
    y = bpy.data.collections.new('Collection')
    bpy.context.scene.collection.children.link(y) 
    # change the active LayerCollection to 'Collection'
    layer_collection = bpy.context.view_layer.layer_collection
    layerColl = recurLayerCollection(layer_collection, 'Collection')
    bpy.context.view_layer.active_layer_collection = layerColl

    # save and re-open the file to clean up the data blocks
    # bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
    # bpy.ops.wm.open_mainfile(filepath=bpy.data.filepath)
    
    # switch to workspace 
    bpy.context.window.workspace = bpy.data.workspaces['Modeling']
    # select view window
    # bpy.ops.screen.space_type_set_or_cycle(space_type='VIEW_3D')
    
    # set shading mode in all screens
    set_shading_mode("RENDERED", bpy.data.screens)
 
    # set visibility range in all screens
    set_clip_end(10000, bpy.data.screens)
    
    # set renderer
    bpy.data.scenes['Scene'].render.engine = 'BLENDER_EEVEE'
    
    # setup word light
    world = bpy.data.worlds['World']
    world.use_nodes = True
    # changing these values does affect the render.
    bg = world.node_tree.nodes['Background']
    bg.inputs[0].default_value[:3] = (0.9, 0.9, 0.9)

#################################################################################



#################################################################################
## IMPORT functionality: find folder
## 
#################################################################################
def check_folder(root_folder, folder_name):
    for root, subdirs, files in os.walk(root_folder):
        for d in subdirs:
            if d == folder_name:                                               
                #print(root_folder + folder_name + " true")
                return True
            else:
                #print(root_folder + folder_name + " false")
                return False
#################################################################################




#################################################################################
## IMPORT functionality: activate camera
## https://blender.stackexchange.com/questions/124347/blender-2-8-python-code-to-switch-shading-mode-between-wireframe-and-solid-mo
#################################################################################
def set_shading_mode(mode="SOLID", screens=[]):
    """
    Performs an action analogous to clicking on the display/shade button of
    the 3D view. Mode is one of "RENDERED", "MATERIAL", "SOLID", "WIREFRAME".
    The change is applied to the given collection of bpy.data.screens.
    If none is given, the function is applied to bpy.context.screen (the
    active screen) only. E.g. set all screens to rendered mode:
      set_shading_mode("RENDERED", bpy.data.screens)
    """
    screens = screens if screens else [bpy.context.screen]
    for s in screens:
        for spc in s.areas:
            if spc.type == "VIEW_3D":
                spc.spaces[0].shading.type = mode
                break # we expect at most 1 VIEW_3D space
#################################################################################



#################################################################################
## IMPORT functionality: set view clipping end
## https://blender.stackexchange.com/questions/124347/blender-2-8-python-code-to-switch-shading-mode-between-wireframe-and-solid-mo
#################################################################################
def set_clip_end(clip_end=1000, screens=[]):
    """
    Performs an action analogous to clicking on the display/shade button of
    the 3D view. Mode is one of "RENDERED", "MATERIAL", "SOLID", "WIREFRAME".
    The change is applied to the given collection of bpy.data.screens.
    If none is given, the function is applied to bpy.context.screen (the
    active screen) only. E.g. set all screens to rendered mode:
      set_clip_end(20000, bpy.data.screens)
    """
    screens = screens if screens else [bpy.context.screen]
    for s in screens:
        for spc in s.areas:
            if spc.type == "VIEW_3D":
                spc.spaces[0].clip_end = clip_end
                break # we expect at most 1 VIEW_3D space
#################################################################################



#################################################################################
## IMPORT functionality: activate camera
#################################################################################
def activateCamera():
    # bpy.context.active_object.name
    obj_camera = bpy.context.scene.camera
    bpy.context.scene.camera = bpy.context.scene.objects['Camera']
  
    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces[0].region_3d.view_perspective = 'CAMERA'
            break 
#################################################################################



#################################################################################
## IMPORT functionality: get xml file names from heli-x skymap project file
#################################################################################
def getTopographyFilesFromProjectFile(xmlPath):
    xmlData = ElementTree.parse(xmlPath).getroot() # get xml information
    # print("xmlData: '" + ElementTree.tostring(xmlRoot).decode() + "'")
    
    # get following properties from xml-project file
    HeightOfEyes = 0;
    TopographyFileList = []; # <-- list
    Resolution1024 = 'false';
    Resolution2048 = 'false';
    Resolution4096 = 'false';
    
    for element in xmlData:
        if 'HeightOfEyes' in element.tag:
            HeightOfEyes = float(element.text)
            print("HeightOfEyes: '" + "{:.2f}".format(HeightOfEyes) + "'")
        if 'TopographyFile' in element.tag:
            TopographyFileList.append(element.text)
            print("TopographyFileList: '" + element.text + "'") 
        if 'Resolution1024' in element.tag:
            Resolution1024 = element.text
            print("Resolution1024: '" + Resolution1024 + "'")   
        if 'Resolution2048' in element.tag:
            Resolution2048 = element.text
            print("Resolution2048: '" + Resolution2048 + "'")   
        if 'Resolution4096' in element.tag:
            Resolution4096 = element.text
            print("Resolution4096: '" + Resolution4096 + "'")  
    
    return HeightOfEyes, TopographyFileList, Resolution1024, Resolution2048, Resolution4096
#################################################################################



#################################################################################
## IMPORT functionality: give every imported xml file a new material with different diffuse colors 
#################################################################################
def createMaterialForCollisionObjects(obj, material, xmlfile, i):

    color_preset = [(0.85, 0.10, 0.10, 1),
                    (0.10, 0.85, 0.10, 1),
                    (0.10, 0.10, 0.85, 1),
                    (0.10, 0.85, 0.85, 1),
                    (0.85, 0.10, 0.85, 1),
                    (0.85, 0.85, 0.10, 1)]
                    
    for m in range(len(material)):
  
        # create material
        mat = bpy.data.materials.new(name="Material_" + xmlfile)
        mat.use_nodes = True
        
        # add Custom Properties to Blender's material
        mat["FrictionFactor"] = material[m].FrictionFactor 
        mat["CrashSensitivityFactor"] = material[m].CrashSensitivityFactor
        mat["Thickness"] = material[m].Thickness
        mat["Alpha"] = material[m].Alpha
        mat["MakeCrashObject"] = material[m].MakeCrashObject
        mat["CeilingType"] = material[m].CeilingType
        mat["GroundType"] = material[m].GroundType
        mat["ShadowReceiver"] = material[m].ShadowReceiver
  
        # setup Blender's material 
        mat.node_tree.nodes['Principled BSDF'].inputs[0].default_value = color_preset[i%(len(color_preset)-1)] # color
        mat.node_tree.nodes['Principled BSDF'].inputs[7].default_value = 0.15 # roughness
        mat.node_tree.nodes["Principled BSDF"].inputs[18].default_value = 0.15 # alpha
        mat.blend_method = 'BLEND'


        # assign material to object
        # obj = bpy.context.active_object
        obj.data.materials.append(mat)
        bpy.context.object.active_material_index = m


        # assign material to specific triangles/polygons only
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='DESELECT')
        bpy.ops.object.material_slot_deselect()
        bpy.ops.object.mode_set(mode='OBJECT')
                
        for s in material[m].triangle_ID_list:
            obj.data.polygons[s].select = True
            
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.object.material_slot_assign()
        bpy.ops.object.mode_set(mode='OBJECT')
   
#################################################################################



#################################################################################
## IMPORT functionality: create the geometry out of the xml files storing the collision information 
#################################################################################
class struct_topography_file (object):
    __slots__ = ['FrictionFactor', 'CrashSensitivityFactor', 'Thickness',  'Alpha', 'MakeCrashObject',  'CeilingType', 'GroundType', 'ShadowReceiver', 'triangle_ID_list']
   
def parse_True_False_str2num(string):
    if string=='True': 
        return 1
    if string=='true': 
        return 1
    if string=='False': 
        return 0
    if string=='false': 
        return 0
 
def parse_True_False_num2str(value):
    if bool(value) == False: 
        return 'False'
    if bool(value) == True: 
        return 'True'
 
 
 
def createMeshFromXMLFile(xmlfolder, xmlfile):

    # get xml data
    xmlData = ElementTree.parse(xmlfolder + xmlfile).getroot()
    
    # garbage collection
    # gc.disable()
    
    # parse xml data
    cntr = 0
    vertices = list();
    faces = list();
    struct = []
    struct.append(struct_topography_file()) 
    s = 0
    id=-1
    material = []
    
    for element in xmlData:
        if 'Triangle' in element.tag:
            id = id + 1
            # get Alpha (transparency) information (may be stored in png)
            items = element.items()
            
            
            
            
            # --------------------------------------  
            # In one file different triangle items are stored. Each item cositst of a set of parameters (friction, alpha ,...). 
            # Here we create for every UNIQUE triangle item a "material" and store the triangle's-id
            # that belong to this material i the list "triangle_ID_list"
            struct.append(struct_topography_file())
 
            struct[s].FrictionFactor = 1.0 # default value
            struct[s].CrashSensitivityFactor = 1.0 # default value
            struct[s].Thickness = 0.0 # default value
            struct[s].Alpha = 1.0 # default value
            struct[s].MakeCrashObject = 1 # default value
            struct[s].ShadowReceiver = 0 # default value
            struct[s].CeilingType = 0 # default value
            struct[s].GroundType = 0 # default value
  
            for i in range(len(items)):
                if items[i][0] == 'FrictionFactor':
                    struct[s].FrictionFactor = float(items[i][1]) # set last element of list
                if items[i][0] == 'CrashSensitivityFactor':
                    struct[s].CrashSensitivityFactor = float(items[i][1]) # set last element of list
                if items[i][0] == 'Thickness':
                    struct[s].Thickness = float(items[i][1]) # set last element of list
                if items[i][0] == 'Alpha':
                    struct[s].Alpha = float(items[i][1]) # set last element of list
                if items[i][0] == 'MakeCrashObject':
                    struct[s].MakeCrashObject = float(parse_True_False_str2num(items[i][1])) # set last element of list
                if items[i][0] == 'ShadowReceiver':
                    struct[s].ShadowReceiver = float(parse_True_False_str2num(items[i][1])) # set last element of list
                if items[i][0] == 'CeilingType':
                    struct[s].CeilingType = float(parse_True_False_str2num(items[i][1])) # set last element of list 
                if items[i][0] == 'GroundType':
                    struct[s].GroundType = float(parse_True_False_str2num(items[i][1])) # set last element of list 
            
            if id == 0: 
                # set first material
                material.append(struct_topography_file())         
                material[0].FrictionFactor = float(struct[s].FrictionFactor) 
                material[0].CrashSensitivityFactor = float(struct[s].CrashSensitivityFactor) 
                material[0].Thickness = float(struct[s].Thickness)
                material[0].Alpha = float(struct[s].Alpha) 
                material[0].MakeCrashObject = float(struct[s].MakeCrashObject) 
                material[0].ShadowReceiver = float(struct[s].ShadowReceiver)
                material[0].CeilingType = float(struct[s].CeilingType)
                material[0].GroundType = float(struct[s].GroundType)
                material[0].triangle_ID_list = list()
                material[0].triangle_ID_list.append(id) 
            else:
                # check for unique topography settings
                for m in range(len(material)):    
                    if (material[m].FrictionFactor == struct[s].FrictionFactor and
                        material[m].CrashSensitivityFactor == struct[s].CrashSensitivityFactor and
                        material[m].Thickness == struct[s].Thickness and
                        material[m].Alpha == struct[s].Alpha and
                        material[m].MakeCrashObject == struct[s].MakeCrashObject and
                        material[m].ShadowReceiver == struct[s].ShadowReceiver and
                        material[m].CeilingType == struct[s].CeilingType and
                        material[m].GroundType == struct[s].GroundType):
                        # if already exists -> add triangle's id to material's list 
                        material[m].triangle_ID_list.append(id)    
                        break
                    if m == (len(material)-1): 
                        # if not found in materials, create new material -> and add triangle's data and id to new material    
                        material.append(struct_topography_file())  
                        material[-1].FrictionFactor = float(struct[s].FrictionFactor) 
                        material[-1].CrashSensitivityFactor = float(struct[s].CrashSensitivityFactor) 
                        material[-1].Thickness = float(struct[s].Thickness)
                        material[-1].Alpha = float(struct[s].Alpha) 
                        material[-1].MakeCrashObject = float(struct[s].MakeCrashObject)
                        material[-1].ShadowReceiver = float(struct[s].ShadowReceiver)
                        material[-1].CeilingType = float(struct[s].CeilingType)
                        material[-1].GroundType = float(struct[s].GroundType)
                        material[-1].triangle_ID_list = list()
                        material[-1].triangle_ID_list.append(id) 
                        
            s = s + 1   
            # --------------------------------------       



    
            # get triange
            for data in element:
                if 'Point' in data.tag:
                    plist_helix_coordinatesystem = list(map(float,data.text.split(',')))
                    # xyz_blender = xyz_helix * transformationmatrix_helix2blender
                    # [x_blender,y_blender,z_blender]' = [x_helix,y_helix,z_helix]' * [1 0 0; 0 0 -1; 0 1 0] 
                    plist_blender_coordinatesystem = [plist_helix_coordinatesystem[0], -plist_helix_coordinatesystem[2], plist_helix_coordinatesystem[1]]
                    vertices.append(plist_blender_coordinatesystem)
            faces.append([cntr+0, cntr+1, cntr+2])
            cntr = cntr + 3
            #print(len(vertices))
            #print(len(faces))
    
    # create mesh and object
    objname = xmlfile[:-4] # without .xml
    mesh = bpy.data.meshes.new(objname)
    obj = bpy.data.objects.new(objname,mesh)
     
    # set mesh location
    obj.location = [0,0,0]
    
    # Blender 2.8 collection
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(1)
     
    # create mesh from python data
    mesh.from_pydata(vertices=vertices, edges=[], faces=faces)
    mesh.update(calc_edges=True)

    
    # show wireframe
    obj.show_wire = True
    
    
    # https://blender.stackexchange.com/questions/94411/how-to-modify-vertex-groups-through-script
    # add vertex group to store Alpha (transparency) information
    # obj = bpy.data.collections[1].objects[0]
    #obj.vertex_groups.new(name="Alpha")
    #bpy.ops.object.mode_set(mode='EDIT')
    #bpy.ops.object.vertex_group_assign()
    #bpy.ops.object.mode_set(mode='OBJECT')
    ## for i in range(0, cntr):
    ##     obj.vertex_groups['Alpha'].weight(i) = alpha(i)
    #      
    ##for i in range(len(alpha)):
    ##    obj.vertex_groups['Alpha'].add(id,alpha(i),'REPLACE')
    
    #print(alpha)
    #print(cntr)
    #print(len(alpha))
    #print(len(obj.data.vertices))
    
    #Alpha_index = obj.vertex_groups['Alpha'].index
    #i=0
    #for v in obj.data.vertices:
    #    v.groups[Alpha_index].weight = alpha[int(i)]
    #    i=i+1/3
    
    
    
    ## weld mesh
    ##bpy.ops.object.mode_set(mode='EDIT')
    ##bpy.ops.mesh.remove_doubles()
    ##bpy.ops.object.mode_set(mode='OBJECT')
    
    
    #print(xmlfile)

    #for m in range(len(material)):
    #    print(material[m].FrictionFactor)
    #    print(material[m].CrashSensitivityFactor)
    #    print(material[m].Thickness)
    #    print(material[m].Alpha)
    #    print(material[m].MakeCrashObject)
    #    print(material[m].ShadowReceiver)
    #    print(material[m].CeilingType)
    #    print(material[m].GroundType) 

    #for i in range(len(material)): 
    #    print('i: ' + ("%d" % i))  
    #    for j in range(len(material[i].triangle_ID_list)):
    #        print(' j: ' + ("%d" % material[i].triangle_ID_list[j])  ) 
 
    #print(len(material))
 
    ##print(material_ids)
    
    #for i in range(len(material)):
    #    print(material[i].FrictionFactor)
    #    print(material[i].CrashSensitivityFactor)
    #    print(material[i].Thickness)
    #    print(material[i].Alpha)
    #    print(material[i].MakeCrashObject)
    #    print(material[i].ShadowReceiver)
    #    print(material[i].CeilingType)
    #    print(material[i].GroundType)
    

    return obj, material
#################################################################################



#################################################################################
## IMPORT functionality: create the background skymap 
#################################################################################
def createSkymap(folder, skymap_name, HeightOfEyes):

    # create six planes of skybox cube
    f = 1000; # size of skybox / 2
    vertices = [(-f, -f, f+HeightOfEyes), (f, -f, f+HeightOfEyes), (f, -f, -f+HeightOfEyes), (-f, -f, -f+HeightOfEyes), (-f, f, f+HeightOfEyes), (f, f, f+HeightOfEyes), (f, f, -f+HeightOfEyes), (-f, f, -f+HeightOfEyes)]
    face = [(0, 1, 2, 3), (4, 0, 3, 7), (5, 4, 7, 6), (1, 5, 6, 2), (0, 4, 5, 1), (3, 2, 6, 7)]
    face_name = [("front"),("left"),("back"),("right"),("top"),("bottom")]

    # create the six faces of the skymap cube as planes and assign them materials with textures
    for i in range(0, 6):
    
        # create mesh   
        bpy.ops.object.select_all(action='DESELECT')
        mesh = bpy.data.meshes.new(skymap_name + "_" + face_name[i])
        obj = bpy.data.objects.new(skymap_name + "_" + face_name[i], mesh)
        
        # Blender 2.8 collection
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(1)
        
        # generate mesh data
        mesh.from_pydata(vertices,[],[face[i]])
        mesh.update(calc_edges=True)
        
        # unwrap uv
        bpy.ops.object.editmode_toggle()
        bpy.ops.uv.cube_project(cube_size=0.001)
        bpy.ops.object.editmode_toggle()
        
        # pragmatic way to mirror two "wrong" oriented maps
        if i==1:
            obj.scale = (1, -1, 1)
            bpy.ops.object.transform_apply(location = True, scale = True, rotation = True)
        if i==2:
            obj.scale = (-1, 1, 1)
            bpy.ops.object.transform_apply(location = True, scale = True, rotation = True)
        if i==5:
            obj.scale = (1, -1, 1)
            bpy.ops.object.transform_apply(location = True, scale = True, rotation = True)
        
        
        # create material
        mat = bpy.data.materials.new(name="Material_" + skymap_name + "_" + face_name[i])
        mat.use_nodes = True
        # new texture
        tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
        tex.location = (-500,200)
        # new math
        math = mat.node_tree.nodes.new("ShaderNodeMath")
        math.location = (-200,100)
        # load texture files
        try:
            tex.image = bpy.data.images.load(folder + "\\" + skymap_name + "_" + face_name[i] + ".png")  
        except:
            tex.image = bpy.data.images.load(folder + "\\" + skymap_name + "_" + face_name[i] + ".jpg")  
        # connect texture to material's displacement
        mat.node_tree.links.new(tex.outputs['Color'], mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
        mat.node_tree.links.new(tex.outputs['Alpha'], mat.node_tree.nodes['Math'].inputs['Value']) 
        mat.node_tree.links.new(math.outputs['Value'], mat.node_tree.nodes['Principled BSDF'].inputs['Alpha']) 

        # set "specular" to 0 
        mat.node_tree.nodes["Principled BSDF"].inputs[5].default_value = 0 

        # assign material to object
        ob = bpy.context.active_object
        ob.data.materials.append(mat)

        # bpy.ops.node.select("deselect_all")
        # mat.node_tree.select("deselect_all")
    return obj
###############################################################################











###############################################################################
##    ______                      __     ______                 __  _                 
##   / ____/  ______  ____  _____/ /_   / ____/_  ______  _____/ /_(_)___  ____  _____
##  / __/ | |/_/ __ \/ __ \/ ___/ __/  / /_  / / / / __ \/ ___/ __/ / __ \/ __ \/ ___/
## / /____>  </ /_/ / /_/ / /  / /_   / __/ / /_/ / / / / /__/ /_/ / /_/ / / / (__  ) 
##/_____/_/|_/ .___/\____/_/   \__/  /_/    \__,_/_/ /_/\___/\__/_/\____/_/ /_/____/  
##          /_/                
###############################################################################  
## EXPORT functionality: Triangulate mesh: Get the active object (could be any mesh object)
###############################################################################  
def triangulate_object(obj):
    me = obj.data
    # get a BMesh representation
    bm = bmesh.new()
    bm.from_mesh(me)
    
    # bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method=0, ngon_method=0)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])

    # finish up, write the bmesh back to the mesh
    bm.to_mesh(me)
    bm.free()
###############################################################################  
  
    
    
###############################################################################  
## EXPORT functionality: export objects
###############################################################################  
def export_objects(target_folder, skymap_name):

    # for each object in the scene
    for obj in bpy.data.objects:
     
        bpy.ops.object.mode_set(mode='OBJECT')
     
        # if this is a mesh
        if obj.type == "MESH" and obj.visible_get() == True and  obj.users_collection[0].name == 'Collection Collision Objects':
            
            mesh = obj.data
            
            # triangulate_object(bpy.context.active_object)
            triangulate_object(obj)
            
            # apply transformations 
            obj.select_set(True)
            bpy.ops.object.transform_apply(location = True, scale = True, rotation = True)
            obj.select_set(False)

            # create XML structure
            xmlString = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            xmlString += "<!--Heli-X Topography File -->\n"
            #xmlString = "<!--$Id: -->"
            xmlString += "<!--Project:        " + skymap_name + "-->\n"
            xmlString += "<!--Source:         " + skymap_name + "-->\n"
            xmlString += "<!--Author:         TBD-->\n"
            xmlString += "<!--Created:        " + datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S") + "-->\n"
            xmlString += "<!--Object Name:    " + obj.name + "-->\n"
            xmlString += "<!--Vertices Count: " + ("%d" % (len(mesh.polygons) * 3)) + "-->\n"      
            xmlString += "<project>"

            # write verticles coordinates and face normal to file
            for face in mesh.polygons:  
            
                # get triangle's material
                mat = obj.material_slots[face.material_index].material

                # xmlString += "\n  <Triangle FrictionFactor=\"0.70\" CrashSensitivityFactor=\"1.00\" Thickness=\"0.00\" Alpha=\"1.00\" CeilingType=\"false\" MakeCrashObject=\"true\" ShadowReceiver=\"true\" GroundType=\"true\">"    
                xmlString += "\n  <Triangle" \
                                " FrictionFactor=\"" + ("%0.2f" % mat['FrictionFactor']) + "\"" + \
                                " CrashSensitivityFactor=\"" + ("%0.2f" % mat['CrashSensitivityFactor']) + "\"" + \
                                " Thickness=\"" + ("%0.2f" % mat['Thickness']) + "\"" + \
                                " Alpha=\"" + ("%0.4f" % mat['Alpha']) + "\"" + \
                                " MakeCrashObject=\"" + parse_True_False_num2str(mat['MakeCrashObject']) + "\"" + \
                                " ShadowReceiver=\"" + parse_True_False_num2str(mat['ShadowReceiver']) + "\"" + \
                                " CeilingType=\"" + parse_True_False_num2str(mat['CeilingType']) + "\"" + \
                                " GroundType=\"" + parse_True_False_num2str(mat['GroundType']) + "\"" + \
                                ">" 
                                
                for i in range(0, len(face.vertices)):
                    vertInFace = face.vertices[i]
                    # Heli x uses y as uper direction, Ftherefore coordinates are changed  
                    xmlString += "\n    <Point>" + ("%0.2f" % mesh.vertices[vertInFace].co.x) + "," + ("%0.2f" % (mesh.vertices[vertInFace].co.z)) + "," + ("%0.2f" % (mesh.vertices[vertInFace].co.y*-1)) + "</Point>"

                xmlString += "\n    <Normal>" + ("%0.2f" % mesh.vertices[vertInFace].normal.x) + "," + ("%0.2f" % (mesh.vertices[vertInFace].normal.z)) + "," + ("%0.2f" % (mesh.vertices[vertInFace].normal.y*-1)) + "</Normal>"
                xmlString += "\n  </Triangle>"

            xmlString += "\n</project>"
                
            print(target_folder)
            # make a filename
            filename = os.path.join (target_folder, obj.name  + ".xml")
            # open a file to write to
            file = open(filename, "w")
            # write the data to file
            file.write(xmlString)
            # close the file
            file.close()
#################################################################################



#################################################################################
## https://blender.stackexchange.com/questions/127403/change-active-collection
#################################################################################
# recursivly transverse layer_collection for a particular name
def recurLayerCollection(layerColl, collName):
    found = None
    if (layerColl.name == collName):
        return layerColl
    for layer in layerColl.children:
        found = recurLayerCollection(layer, collName)
        if found:
            return found
#################################################################################






#################################################################################
##    ____                           __     ________               
##   /  _/___ ___  ____  ____  _____/ /_   / ____/ /___ ___________
##   / // __ `__ \/ __ \/ __ \/ ___/ __/  / /   / / __ `/ ___/ ___/
## _/ // / / / / / /_/ / /_/ / /  / /_   / /___/ / /_/ (__  |__  ) 
##/___/_/ /_/ /_/ .___/\____/_/   \__/   \____/_/\__,_/____/____/  
##             /_/        
#################################################################################
## Operator
#################################################################################
class IMPORT_HELI_X_OT_skymap(Operator):
    """Import Heli-X skymap"""          # use this as a tooltip for menu items and buttons.
    
    bl_idname = "import_heli_x.skymap"  # unique identifier for buttons and menu items to reference.
    bl_label = "Import Heli-X Skymap"   # display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}   # enable undo for the operator.    

    # define this to tell 'fileselect_add' that we want a directory    name="Skymap environment path",
    directory: bpy.props.StringProperty(maxlen=1024, subtype='FILE_PATH', options={'HIDDEN', 'SKIP_SAVE'})
    
    def execute(self, context):        # execute() is called when running the operator.
        
        resetScene()
        
        print("Path imported: '" + self.directory + "'")
        print("Folder imported: '" + os.path.dirname(self.directory) + "'")
        print("Folder imported: '" + os.path.dirname(os.path.dirname(self.directory)) + "'")
        print("basename imported: '" + os.path.basename(os.path.dirname(self.directory)) + "'")
        
        skymap_name = os.path.basename(os.path.dirname(self.directory))
        print("basename imported: '" + skymap_name + "'")
        
        skymap_project_file = self.directory + skymap_name + ".xml"
        print("skymap_project_file: '" + skymap_project_file + "'")
        
        
        HeightOfEyes, TopographyFileList, Resolution1024, Resolution2048, Resolution4096 = getTopographyFilesFromProjectFile(skymap_project_file)
        
        # create new collection to store skymap environment planes in
        y = bpy.data.collections.new('Collection Collision Objects')
        bpy.context.scene.collection.children.link(y)
        
        # create geometry
        i=0
        for xmlfile in TopographyFileList:
            obj, material = createMeshFromXMLFile(self.directory, xmlfile)
            createMaterialForCollisionObjects(obj, material, xmlfile, i)
            
            # Remove object from collection
            bpy.data.collections['Collection'].objects.unlink(obj)
            # add it to our specific collection
            bpy.data.collections['Collection Collision Objects'].objects.link(obj)
            
            i=i+1
        
        
        # use highest resolution textures for skybox environment
        if Resolution1024 == 'true' or check_folder(self.directory, "1024"):
            resolution_folder = '1024'
        if Resolution2048 == 'true' or check_folder(self.directory, "2048"):
            resolution_folder = '2048'   
        if Resolution4096 == 'true' or check_folder(self.directory, "4096"):
            resolution_folder = '4096'
 
        
        print("used resolution_folder: " + resolution_folder)
        
        # create skymap with mesh and materials
        createSkymap(self.directory + resolution_folder, skymap_name, HeightOfEyes)    
       
        # create camera
        bpy.ops.object.camera_add(enter_editmode=False, align='VIEW', location=(0, 0, HeightOfEyes), rotation=(1.5708, 0, 3.14159))
        bpy.context.object.data.lens_unit = 'FOV'
        bpy.context.object.data.angle = 1.5708
        bpy.context.object.data.clip_end = 100000

        # switch to camera view
        activateCamera()
       
        # change the Active LayerCollection to 'Collection Collision Objects'
        layer_collection = bpy.context.view_layer.layer_collection
        layerColl = recurLayerCollection(layer_collection, 'Collection Collision Objects')
        bpy.context.view_layer.active_layer_collection = layerColl
       
        return {'FINISHED'}            # lets Blender know the operator finished successfully.


    def invoke(self, context, event):
        # open browser, take reference to 'self' read the path to selected
        # file, put path in predetermined self fields.
        # see: https://docs.blender.org/api/current/bpy.types.WindowManager.html#bpy.types.WindowManager.fileselect_add
        context.window_manager.fileselect_add(self)
        # tells Blender to hang on for the slow user input
        return {'RUNNING_MODAL'}
        
#################################################################################




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
class EXPORT_HELI_X_OT_collision_objects(Operator):
    """Export Heli-X collision objects"""          # use this as a tooltip for menu items and buttons.
    
    bl_idname = "export_heli_x.collision_objects"  # unique identifier for buttons and menu items to reference.
    bl_label = "Export Heli-X collision objects"   # display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}   # enable undo for the operator.    

    # define this to tell 'fileselect_add' that we want a directory    name="Skymap environment path",
    directory: bpy.props.StringProperty(maxlen=1024, subtype='FILE_PATH', options={'HIDDEN', 'SKIP_SAVE'})
    
    def execute(self, context):        # execute() is called when running the operator.
        
        print("Path imported: '" + self.directory + "'")
        print("Folder imported: '" + os.path.dirname(self.directory) + "'")
        print("Folder imported: '" + os.path.dirname(os.path.dirname(self.directory)) + "'")
        print("basename imported: '" + os.path.basename(os.path.dirname(self.directory)) + "'")
        
        skymap_name = os.path.basename(os.path.dirname(self.directory))
        print("basename imported: '" + skymap_name + "'")
        
        skymap_project_file = self.directory + skymap_name + ".xml"
        print("skymap_project_file: '" + skymap_project_file + "'")
          
        export_objects(self.directory, skymap_name)
       
        return {'FINISHED'}            # lets Blender know the operator finished successfully.


    def invoke(self, context, event):
        # open browser, take reference to 'self' read the path to selected
        # file, put path in predetermined self fields.
        # see: https://docs.blender.org/api/current/bpy.types.WindowManager.html#bpy.types.WindowManager.fileselect_add
        context.window_manager.fileselect_add(self)
        # tells Blender to hang on for the slow user input
        return {'RUNNING_MODAL'}
        
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
def import_heli_x_skymap_button(self, context):
    self.layout.operator(IMPORT_HELI_X_OT_skymap.bl_idname, text="Heli-X Skymap", icon='IMAGE_DATA')
def export_heli_x_collision_objects_button(self, context):
    self.layout.operator(EXPORT_HELI_X_OT_collision_objects.bl_idname, text="Heli-X Collision Objects", icon='IMAGE_DATA')


classes = (
    IMPORT_HELI_X_OT_skymap,
    EXPORT_HELI_X_OT_collision_objects,
)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)

    bpy.types.TOPBAR_MT_file_import.append(import_heli_x_skymap_button)
    bpy.types.TOPBAR_MT_file_export.append(export_heli_x_collision_objects_button)


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(export_heli_x_collision_objects_button)
    bpy.types.TOPBAR_MT_file_import.remove(import_heli_x_skymap_button)
 
    for cls in classes:
        if hasattr(bpy.types, cls.__name__):
            bpy.utils.unregister_class(cls)


if __name__ == "__main__":
    # run simple doc tests
    import doctest
    doctest.testmod()
    
    unregister()
    register()