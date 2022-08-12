
import bpy 
import os

scn = bpy.context.scene

# create the first camera
cam1 = bpy.data.cameras.new("Camera 1")
cam1.lens = 40

# create the first camera object
cam_obj1 = bpy.data.objects.new("Camera 1", cam1)
#####
#cam_obj1.location = (2.75119, 3.8255, 1.04)
#cam_obj1.rotation_euler = (65*0.0177, 0, 143*0.0177)
#### agusta
cam_obj1.location = (-8.43877, -7.23933, 2.01414)
cam_obj1.rotation_euler = (86.0*0.0177, 0.000*0.0177, -49*0.0177)
scn.collection.objects.link(cam_obj1)


#bpy.ops.view3d.object_as_camera()
bpy.context.scene.camera = cam_obj1


bpy.context.scene.render.engine = 'BLENDER_EEVEE'
#bpy.context.scene.render.engine = 'CYCLES'
#bpy.context.scene.cycles.device = 'GPU'
#bpy.context.scene.cycles.samples = 128



filepath = bpy.data.filepath
output_dir = os.path.dirname(filepath)
output_filename = bpy.path.display_name(bpy.path.basename(filepath), has_ext=True)
w,h = 1920,1080
bpy.context.scene.render.image_settings.file_format='JPEG'
bpy.context.scene.render.resolution_x = w #perhaps set resolution in code
bpy.context.scene.render.resolution_y = h
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.render.filepath = os.path.join(output_dir, output_filename + "__img.jpg")
#bpy.context.scene.render.filepath =  "img.jpg"
bpy.ops.render.render(write_still=True)








# c:\temp\blender-3.2.1-windows-x64\blender.exe --background untitled_001.blend --python Text.py
# c:\temp\blender-3.2.1-windows-x64\blender.exe --background untitled_001.blend --python Text.py