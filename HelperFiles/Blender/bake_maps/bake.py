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

## 2024.05.14 V1.00 Blender 4.1
##

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
    "name": "Bake Textures",
    "author": "zulu",
    "version": (1, 0, 1),
    "blender": (4, 1, 1),
    "location": "Node Editor > Sidebar > TextureBake",
    "description": "Bakes Textures",
    "category": "Baking",
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
# give Python access to Blender's functionality
import bpy
import time
from pathlib import Path

from bpy.props import (StringProperty,
                       BoolProperty,
                       IntProperty,
                       FloatProperty,
                       FloatVectorProperty,
                       EnumProperty,
                       PointerProperty,
                       )
from bpy.types import (Panel,
                       Operator,
                       AddonPreferences,
                       PropertyGroup,
                       )
#################################################################################







#################################################################################
##    ____        __           _____      __  __  _                 
##   / __ )____ _/ /_____     / ___/___  / /_/ /_(_)___  ____ ______
##  / __  / __ `/ //_/ _ \    \__ \/ _ \/ __/ __/ / __ \/ __ `/ ___/
## / /_/ / /_/ / ,< /  __/   ___/ /  __/ /_/ /_/ / / / / /_/ (__  ) 
##/_____/\__,_/_/|_|\___/   /____/\___/\__/\__/_/_/ /_/\__, /____/  
##                                                    /____/      
#################################################################################
## IMPORT functionality: delete everything
#################################################################################                     
class Bake_Settings(PropertyGroup):

    my_float_scale_all: FloatProperty(
        name = "Set number of cycles samples",
        description="Set number of cycles samples",
        default = 0.1,
        min = 0.01,
        max = 2.0
        )
    my_int_resolution_DIFFUSE : IntProperty(
        name = "Resolution of diffuse map",
        description="Resolution of diffuse map",
        default = 4086,
        min = 16,
        max = 8192
        )
    my_int_resolution_METALLIC : IntProperty(
        name = "Resolution of metallic map",
        description="Resolution of metallic map",
        default = 4086,
        min = 16,
        max = 8192
        )
    my_int_resolution_ROUGHNESS : IntProperty(
        name = "Resolution of roughness map",
        description="Resolution of roughness map",
        default = 4086,
        min = 16,
        max = 8192
        )     
    my_int_resolution_NORMAL : IntProperty(
        name = "Resolution of normal map",
        description="Resolution of normal map",
        default = 4086,
        min = 16,
        max = 8192
        )       
    my_int_resolution_AO: IntProperty(
        name = "Resolution of AO map",
        description="Resolution of AO map",
        default = 4086,
        min = 16,
        max = 8192
        )       
                     

    my_bool_DIFFUSE : BoolProperty(
        name="Enable or disable diffuse",
        description="Enable or disable diffuse",
        default = True
        ) 
    my_bool_METALLIC : BoolProperty(
        name="Enable or disable metallic",
        description="Enable or disable metallic",
        default = True
        )
    my_bool_ROUGHNESS : BoolProperty(
        name="Enable or disable roughness",
        description="Enable or disable roughness",
        default = True
        )
    my_bool_NORMAL : BoolProperty(
        name="Enable or disable normal",
        description="Enable or disable normal",
        default = True
        )       
    my_bool_AO : BoolProperty(
        name="Enable or disable AO",
        description="Enable or disable AO",
        default = True
        )
        
        
        
    my_enum_normal_bitdepth : EnumProperty(
        name = "Normal Map",
        description = "Normal map: Bit depth per channel",
        items = [
            ("16" , "Float(Half)" , "16-bit color channels"),
            ("32" , "Float(Full)" , "32-bit color channels")
        ]
        )
    my_int_cycles_samples : IntProperty(
        name = "Set number of cycles samples",
        description="Set number of cycles samples",
        default = 10,
        min = 1,
        max = 100
        )
    my_int_cycles_samples_AO : IntProperty(
        name = "Set number of cycles samples fo AO maps",
        description="Set number of cycles samples fo AO maps",
        default = 100,
        min = 1,
        max = 1000
        )
      
        
    #Path(bpy.path.basename(bpy.data.filepath)).stem    
    my_file_name: bpy.props.StringProperty(name="File name",
                                        description="Additional string added at beginning of filenames",
                                        default="",
                                        maxlen=256,
                                        subtype="NONE")
#################################################################################







#################################################################################
##   __  ______   
##  / / / /  _/   
## / / / // /     
##/ /_/ // /      
##\____/___/      
#################################################################################
## Setup UI panel
#################################################################################     
# ICONS https://docs.blender.org/api/current/bpy_types_enum_items/icon_items.html
class NODE_EDITOR_PT_bake_panel(bpy.types.Panel):  # class naming convention ‘CATEGORY_PT_name’
    
    # where to add the panel in the UI
    bl_space_type = "NODE_EDITOR"  # 3D Viewport area (find list of values here https://docs.blender.org/api/current/bpy_types_enum_items/space_type_items.html#rna-enum-space-type-items)
    bl_region_type = "UI"  # Sidebar region (find list of values here https://docs.blender.org/api/current/bpy_types_enum_items/region_type_items.html#rna-enum-region-type-items)

    bl_category = "TextureBake"  # found in the Sidebar
    bl_label = "Texture Baker"  # found at the top of the Panel

    def draw(self, context):
        layout = self.layout
        scene = context.scene
        mytool = scene.baking_maps_tool

        row = layout.row()
        row.alignment = 'RIGHT'
  
        # subpanel caption
        layout.use_property_split = True
        layout.use_property_decorate = False
        row = layout.row()
        icon = 'DOWNARROW_HLT' if context.scene.subpanel_status_resolutions else 'RIGHTARROW'
        row.prop(context.scene, 'subpanel_status_resolutions', icon=icon, icon_only=True)
        row.label(text='Render Resolutions')

        # display the properties
        if context.scene.subpanel_status_resolutions:
            row = layout.row()
            row.alignment = 'RIGHT'
            col0 = row.column()
            col0.scale_x = 1   
            col0.prop(mytool, "my_float_scale_all", text="Scale all", slider=True,  icon='NODE_SEL')
            col0.prop(mytool, "my_int_resolution_DIFFUSE", text="Diffuse")
            col0.prop(mytool, "my_int_resolution_METALLIC", text="Metallic")
            col0.prop(mytool, "my_int_resolution_ROUGHNESS", text="Roughness")
            col0.prop(mytool, "my_int_resolution_NORMAL", text="Normal")
            col0.prop(mytool, "my_int_resolution_AO", text="AO")
            
        # subpanel caption
        row = layout.row()
        icon = 'DOWNARROW_HLT' if context.scene.subpanel_status_enable_maps else 'RIGHTARROW'
        row.prop(context.scene, 'subpanel_status_enable_maps', icon=icon, icon_only=True)
        row.label(text='Maps Selection')
        
        if context.scene.subpanel_status_enable_maps:
            row = layout.row()
            row.alignment = 'RIGHT'
            col1 = row.column()
            col1.scale_x = 1
            col1.prop(mytool, "my_bool_DIFFUSE", text="Diffuse")
            col1.prop(mytool, "my_bool_METALLIC", text="Metallic")
            col1.prop(mytool, "my_bool_ROUGHNESS", text="Roughness")
            col1.prop(mytool, "my_bool_NORMAL", text="Normal") 
            col1.prop(mytool, "my_bool_AO", text="AO")
          
        if context.scene.baking_maps_tool.my_bool_NORMAL:
            layout.separator(factor=0) 
            layout.use_property_split = False
            layout.use_property_decorate = False
            row = layout.row(align=True)
            row.label(text='Normal Map')
            col = row.column(align=True)
            col.scale_x = 0.75
            col.use_property_split = False
            col.prop(mytool, "my_enum_normal_bitdepth", expand=True)
         
        layout.separator(factor=0) 
        layout.use_property_split = False
        layout.use_property_decorate = False
        
        layout.prop(mytool, "my_int_cycles_samples", text="Cycles samples", slider=True)
        if context.scene.baking_maps_tool.my_bool_AO:
            layout.prop(mytool, "my_int_cycles_samples_AO", text="Cycles samples AO", slider=True)

        layout.prop(mytool, "my_file_name", text="Name")

        if (context.scene.baking_maps_tool.my_bool_DIFFUSE or
           context.scene.baking_maps_tool.my_bool_METALLIC or
           context.scene.baking_maps_tool.my_bool_ROUGHNESS or
           context.scene.baking_maps_tool.my_bool_NORMAL or
           context.scene.baking_maps_tool.my_bool_AO ):     
            row = layout.row()
            row.alignment = 'CENTER'
            col = row.column()
            col.scale_x = 1 
            col.operator("object.bake_maps", text="Bake Maps")   
#################################################################################







#################################################################################
##    ____        __      
##   / __ )____ _/ /_____ 
##  / __  / __ `/ //_/ _ \
## / /_/ / /_/ / ,< /  __/
##/_____/\__,_/_/|_|\___/
#################################################################################
## Baking the maps
#################################################################################
class Bake_Routines(bpy.types.Operator):
    bl_idname = "object.bake_maps"
    bl_label = "Bake"
    bl_description = "Baking might take several minutes..."
    
    def execute(self, context):
        print("ok")
        

        #############################################################################################
        # input
        #############################################################################################
        par = {} # create an empty dictionary
        par['img_size'] = {}
        par['img_size']['scale_all']   = context.scene.baking_maps_tool.my_float_scale_all
        par['img_size']['DIFFUSE']     = context.scene.baking_maps_tool.my_int_resolution_DIFFUSE
        par['img_size']['METALLIC']    = context.scene.baking_maps_tool.my_int_resolution_METALLIC
        par['img_size']['ROUGHNESS']   = context.scene.baking_maps_tool.my_int_resolution_ROUGHNESS
        par['img_size']['NORMAL']      = context.scene.baking_maps_tool.my_int_resolution_NORMAL
        par['img_size']['AO']          = context.scene.baking_maps_tool.my_int_resolution_AO
        par['enable'] = {}
        par['enable']['DIFFUSE']       = context.scene.baking_maps_tool.my_bool_DIFFUSE
        par['enable']['METALLIC']      = context.scene.baking_maps_tool.my_bool_METALLIC
        par['enable']['ROUGHNESS']     = context.scene.baking_maps_tool.my_bool_ROUGHNESS
        par['enable']['NORMAL']        = context.scene.baking_maps_tool.my_bool_NORMAL
        par['enable']['AO']            = context.scene.baking_maps_tool.my_bool_AO
        par['normal_bitdepth'] 	       = context.scene.baking_maps_tool.my_enum_normal_bitdepth 
        par['cycles_samples'] 		   = context.scene.baking_maps_tool.my_int_cycles_samples
        par['cycles_samples_AO'] 	   = context.scene.baking_maps_tool.my_int_cycles_samples_AO  
        print(context.scene.baking_maps_tool.my_file_name)
        if context.scene.baking_maps_tool.my_file_name == "":
            par['my_file_name']        = ""
        else:  
            par['my_file_name'] 	   = context.scene.baking_maps_tool.my_file_name + '_'
        #############################################################################################





        #############################################################################################
        # setup gpu renderer and cycles
        #############################################################################################
        restore = {} # create an empty dictionary
        restore['render_engine'] = bpy.data.scenes[0].render.engine
        bpy.data.scenes[0].render.engine = "CYCLES"

        # Set the device_type
        bpy.context.preferences.addons["cycles"].preferences.compute_device_type = "CUDA" # or "OPENCL"

        # Set the device and feature set
        bpy.context.scene.cycles.device = "GPU"

        # get_devices() to let Blender detects GPU device
        bpy.context.preferences.addons["cycles"].preferences.get_devices()
        print(bpy.context.preferences.addons["cycles"].preferences.compute_device_type)
        for d in bpy.context.preferences.addons["cycles"].preferences.devices:
            d["use"] = 1 # Using all devices, include GPU and CPU
            print(d["name"], d["use"])

        # reduce cycles render sample count
        restore['number_of_samples'] = bpy.context.scene.cycles.samples
        bpy.context.scene.cycles.samples = par['cycles_samples']
        #############################################################################################    





        #############################################################################################  
        # bake
        #############################################################################################  
        obj = bpy.context.active_object
        img = bpy.data.images.new(  obj.name + '_BakedTextureTemp', 
                                    int(par['img_size']['DIFFUSE']*par['img_size']['scale_all']), 
                                    int(par['img_size']['DIFFUSE']*par['img_size']['scale_all']), 
                                    alpha=False,
                                    float_buffer=True,
                                    is_data=True,
        )


        #----------------------------------------------------------------------------------
        # Create image texture node in material
        # Due to the presence of any multiple materials, it seems necessary to iterate on all the materials, and assign them a node + the image to bake.
        #for mat in obj.data.materials:
        #    mat.use_nodes = True #Here it is assumed that the materials have been created with nodes, otherwise it would not be possible to assign a node for the Bake, so this step is a bit useless
        #    nodes = mat.node_tree.nodes
        #    texture_node = nodes.new('ShaderNodeTexImage')
        #    texture_node.name = 'Bake_node'
        #    texture_node.select = True
        #    nodes.active = texture_node
        #    texture_node.image = img #Assign the image to the node
         
        mat = bpy.context.object.active_material
        mat.use_nodes = True #Here it is assumed that the materials have been created with nodes, otherwise it would not be possible to assign a node for the Bake, so this step is a bit useless
        nodes = mat.node_tree.nodes
        texture_node = nodes.new('ShaderNodeTexImage')
        texture_node.name = 'Bake_node'
        texture_node.select = True
        nodes.active = texture_node
        texture_node.image = img #Assign the image to the node
        #----------------------------------------------------------------------------------
            

        bpy.context.view_layer.objects.active = obj




        #----------------------------------------------------------------------------------
        # For baking Metallic nodes the Emission Color will be used
        if(par['enable']['METALLIC']):
            # https://blender.stackexchange.com/questions/10510/get-node-links-from-socket-index-value
            link_to_temporarily_disconnect = mat.node_tree.nodes["Principled BSDF"].inputs[1].links
            if bool(link_to_temporarily_disconnect): 
                # save connection info for later reconnection
                restore['metallic_link_found'] = True
                restore['from_node']   = mat.node_tree.nodes["Principled BSDF"].inputs['Metallic'].links[0].from_node.name
                restore['from_socket'] = mat.node_tree.nodes["Principled BSDF"].inputs['Metallic'].links[0].from_socket.name
                restore['to_node']     = mat.node_tree.nodes["Principled BSDF"].inputs['Metallic'].links[0].to_node.name
                restore['to_socket']   = mat.node_tree.nodes["Principled BSDF"].inputs['Metallic'].links[0].to_socket.name
                # remove the link
                mat.node_tree.links.remove(link_to_temporarily_disconnect[0])
            else:
                restore['metallic_link_found'] = False
                
            # if metallic is not 0, then color will be black when baking
            #for n in mat.node_tree.nodes:
            #    if n.type == 'BSDF_PRINCIPLED':
            #       n.inputs["Metallic"].default_value = 0
            mat.node_tree.nodes["Principled BSDF"].inputs["Metallic"].default_value = 0
        #----------------------------------------------------------------------------------




        #----------------------------------------------------------------------------------
        # DIFFUSE
        if(par['enable']['DIFFUSE']):
            img.generated_width = int(par['img_size']['DIFFUSE']*par['img_size']['scale_all'])
            img.generated_height = int(par['img_size']['DIFFUSE']*par['img_size']['scale_all'])
            img.colorspace_settings.name = 'sRGB'
            settings = context.scene.render.image_settings 
            settings.file_format = 'PNG'  # Options: 'BMP', 'IRIS', 'PNG', 'JPEG', 'JPEG2000', 'TARGA', 'TARGA_RAW', 'CINEON', 'DPX', 'OPEN_EXR_MULTILAYER', 'OPEN_EXR', 'HDR', 'TIFF', 'WEBP'
            settings.color_mode = 'RGB'  # Options: 'BW', 'RGB', 'RGBA' (depends on file_format)
            settings.color_depth = '8'  # Options: '8', '10', '12', '16', '32' (depends on file_format)
            settings.compression = 20  # Range: 0 - 100
            bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, save_mode='EXTERNAL')
            img.save_render(filepath=bpy.path.abspath('//' + par['my_file_name'] + obj.name + '_Color.png'))
        #----------------------------------------------------------------------------------
        # METALLIC - use/connect to emission color input to bake
        if(par['enable']['METALLIC']):
            img.generated_width = int(par['img_size']['METALLIC']*par['img_size']['scale_all'])
            img.generated_height = int(par['img_size']['METALLIC']*par['img_size']['scale_all'])
            if restore['metallic_link_found']:
                mat.node_tree.links.new(nodes[restore['from_node']].outputs[restore['from_socket']],
                                        mat.node_tree.nodes["Principled BSDF"].inputs['Emission Color'] )

            mat.node_tree.nodes["Principled BSDF"].inputs['Emission Strength'].default_value = 1    
                              
            img.colorspace_settings.name = 'Non-Color'
            settings.file_format = 'PNG'  # Options: 'BMP', 'IRIS', 'PNG', 'JPEG', 'JPEG2000', 'TARGA', 'TARGA_RAW', 'CINEON', 'DPX', 'OPEN_EXR_MULTILAYER', 'OPEN_EXR', 'HDR', 'TIFF', 'WEBP'
            settings.color_mode = 'RGB'  # Options: 'BW', 'RGB', 'RGBA' (depends on file_format)
            settings.color_depth = '8'  # Options: '8', '10', '12', '16', '32' (depends on file_format)
            settings.compression = 20  # Range: 0 - 100
            bpy.ops.object.bake(type='EMIT', save_mode='EXTERNAL') # use EMIT but bake METALLIC info
            img.save_render(filepath=bpy.path.abspath('//' +  par['my_file_name'] + obj.name + '_Metallic.png'))   

            if restore['metallic_link_found']:
                # remove emission again
                mat.node_tree.links.remove(mat.node_tree.nodes["Principled BSDF"].inputs['Emission Color'].links[0])
                # restore the link -> reconnect metallic 
                mat.node_tree.links.new(nodes[restore['from_node']].outputs[restore['from_socket']],
                                        nodes[restore['to_node']].inputs[restore['to_socket']])
        #----------------------------------------------------------------------------------
        # ROUGHNESS
        if(par['enable']['ROUGHNESS']):
            img.generated_width = int(par['img_size']['ROUGHNESS']*par['img_size']['scale_all'])
            img.generated_height = int(par['img_size']['ROUGHNESS']*par['img_size']['scale_all'])
            img.colorspace_settings.name = 'Non-Color'
            settings.file_format = 'PNG'  # Options: 'BMP', 'IRIS', 'PNG', 'JPEG', 'JPEG2000', 'TARGA', 'TARGA_RAW', 'CINEON', 'DPX', 'OPEN_EXR_MULTILAYER', 'OPEN_EXR', 'HDR', 'TIFF', 'WEBP'
            settings.color_mode = 'RGB'  # Options: 'BW', 'RGB', 'RGBA' (depends on file_format)
            settings.color_depth = '8'  # Options: '8', '10', '12', '16', '32' (depends on file_format)
            settings.compression = 20  # Range: 0 - 100
            bpy.ops.object.bake(type='ROUGHNESS', save_mode='EXTERNAL')
            img.save_render(filepath=bpy.path.abspath('//' + par['my_file_name'] + obj.name + '_Roughness.png')) 
        #----------------------------------------------------------------------------------
        # NORMAL
        if(par['enable']['NORMAL']):
            img.generated_width = int(par['img_size']['NORMAL']*par['img_size']['scale_all'])
            img.generated_height = int(par['img_size']['NORMAL']*par['img_size']['scale_all'])
            img.colorspace_settings.name = 'Non-Color'
            img.use_generated_float = True
            settings = context.scene.render.image_settings 
            settings.file_format = 'OPEN_EXR'  # Options: 'BMP', 'IRIS', 'PNG', 'JPEG', 'JPEG2000', 'TARGA', 'TARGA_RAW', 'CINEON', 'DPX', 'OPEN_EXR_MULTILAYER', 'OPEN_EXR', 'HDR', 'TIFF', 'WEBP'
            settings.color_mode = 'RGB'  # Options: 'BW', 'RGB', 'RGBA' (depends on file_format)
            settings.color_depth = context.scene.baking_maps_tool.my_enum_normal_bitdepth  # Options: '8', '10', '12', '16', '32' (depends on file_format)
            settings.compression = 0  # Range: 0 - 100
            print(f' has_linear_colorspace: ' + str(settings.has_linear_colorspace))
            bpy.ops.object.bake(type='NORMAL', save_mode='EXTERNAL')
            img.save_render(filepath=bpy.path.abspath('//' + par['my_file_name'] + obj.name + '_Normal.exr')) 
        #----------------------------------------------------------------------------------
        # AO
        if(par['enable']['AO']):
            t = time.time()
            bpy.context.scene.cycles.samples = par['cycles_samples_AO']
            img.generated_width = int(par['img_size']['AO']*par['img_size']['scale_all'])
            img.generated_height = int(par['img_size']['AO']*par['img_size']['scale_all'])
            img.colorspace_settings.name = 'Non-Color'
            bpy.ops.object.bake(type='AO', save_mode='EXTERNAL')
            settings.file_format = 'PNG'  # Options: 'BMP', 'IRIS', 'PNG', 'JPEG', 'JPEG2000', 'TARGA', 'TARGA_RAW', 'CINEON', 'DPX', 'OPEN_EXR_MULTILAYER', 'OPEN_EXR', 'HDR', 'TIFF', 'WEBP'
            settings.color_mode = 'RGB'  # Options: 'BW', 'RGB', 'RGBA' (depends on file_format)
            settings.color_depth = '16'  # Options: '8', '10', '12', '16', '32' (depends on file_format)
            settings.compression = 20  # Range: 0 - 100
            img.save_render(filepath=bpy.path.abspath('//' + par['my_file_name'] + obj.name + '_AO.png'))   
            print('AO baking time is:', "{:.2f}".format(time.time()-t), 'sec')
        #############################################################################################  




        #############################################################################################  
        # clean up
        #############################################################################################     
        #In the last step, we are going to delete the nodes we created earlier
        for mat in obj.data.materials:
            for n in mat.node_tree.nodes:
                if n.name == 'Bake_node':
                    mat.node_tree.nodes.remove(n)
                    
        bpy.context.scene.cycles.samples = restore['number_of_samples']
        bpy.data.scenes[0].render.engine = restore['render_engine']

        bpy.data.images.remove(img) 
        #############################################################################################  

        return {'FINISHED'}
#################################################################################










#################################################################################
##    ____             _      __           
##   / __ \___  ____ _(_)____/ /____  _____
##  / /_/ / _ \/ __ `/ / ___/ __/ _ \/ ___/
## / _, _/  __/ /_/ / (__  ) /_/  __/ /    
##/_/ |_|\___/\__, /_/____/\__/\___/_/     
##           /____/     
#################################################################################
## Register / Unregister
#################################################################################
classes = (
    Bake_Settings,
    NODE_EDITOR_PT_bake_panel,
    Bake_Routines,
)

def register():
    from bpy.utils import register_class
    for cls in classes:
        register_class(cls)
    bpy.types.Scene.baking_maps_tool = PointerProperty(type=Bake_Settings)
    bpy.types.Scene.subpanel_status_resolutions = BoolProperty( default=False )
    bpy.types.Scene.subpanel_status_enable_maps = BoolProperty( default=False )

def unregister():
    from bpy.utils import unregister_class
    for cls in reversed(classes):
        unregister_class(cls)
    del bpy.types.Scene.baking_maps_tool

if __name__ == "__main__":
    register()
#################################################################################