"""Run with blender -b -P scripts/render-ossuary-trailer.py from the repository."""
import os
import re
import subprocess
import tempfile
from pathlib import Path

import bpy
from mathutils import Vector

assets = Path(__file__).resolve().parents[1] / "sandbox/Assets"
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 16
scene.cycles.use_denoising = False
scene.world = bpy.data.worlds.new("Ossuary dusk")
scene.world.color = (0.09, 0.07, 0.09)

# The floor and pillars share OssuaryRoot coordinates in DiabloZone.
zone = (assets / "Scenes/DiabloZone.unity").read_text()
for match in re.finditer(r"  m_Name: (OssuaryPillar_\d+|OssuaryFloor|OssuarySarcophagus_\d+)\n", zone):
    transform = zone[match.end():].split("m_LocalEulerAnglesHint", 1)[0]
    def vector(field):
        xyz = re.search(field + r": \{x: ([^,]+), y: ([^,]+), z: ([^}]+)", transform)
        x, y, z = map(float, xyz.groups())
        return x, z, y
    position = vector("m_LocalPosition")
    size = vector("m_LocalScale")
    if match[1] == "OssuaryFloor":
        size = (size[0] * 10, size[1] * 10, 0.15)
    bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    stone = bpy.context.object
    stone.name = match[1]
    stone.dimensions = size
    material = bpy.data.materials.new(stone.name)
    material.diffuse_color = (0.16, 0.12, 0.14, 1)
    stone.data.materials.append(material)

bpy.ops.import_scene.fbx(filepath=str(assets / "ThirdParty/KayKit/Skeletons/Characters/Skeleton_Mage.fbx"))
actor = next(obj for obj in bpy.context.selected_objects if obj.type == "ARMATURE")
bpy.ops.import_scene.fbx(filepath=str(assets / "ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_General.fbx"))
for obj in list(bpy.context.selected_objects):
    bpy.data.objects.remove(obj, do_unlink=True)
actor.animation_data_create().action = next(a for a in bpy.data.actions if a.name.endswith("|Idle_B"))
focus = Vector((0, 0, 1.65))
for position, color, energy in [
    ((-3, -4, 5), (1, 0.49, 0.19), 900),
    ((2, 3, 4), (0.32, 0.55, 1), 1200),
]:
    bpy.ops.object.light_add(type="AREA", location=position)
    light = bpy.context.object
    light.rotation_euler = (focus - light.location).to_track_quat("-Z", "Y").to_euler()
    light.data.energy = energy
    light.data.color = color
    light.data.shape = "DISK"
    light.data.size = 4

bpy.ops.object.camera_add()
scene.camera = camera = bpy.context.object
scene.frame_end = 72
for frame, position in [(1, (-5, -9, 3.6)), (72, (1, -6.8, 2.1))]:
    camera.location = position
    camera.rotation_euler = (focus - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.keyframe_insert(data_path="location", frame=frame)
    camera.keyframe_insert(data_path="rotation_euler", frame=frame)
scene.render.resolution_x = 640
scene.render.resolution_y = 360
scene.render.resolution_percentage = 100
scene.render.fps = 24
scene.render.image_settings.file_format = "FFMPEG"
scene.render.ffmpeg.format = "WEBM"
scene.render.ffmpeg.codec = "WEBM"
# Unity's Linux VideoPlayer decodes VP8. Blender's WEBM encoder outputs VP9.
with tempfile.TemporaryDirectory() as temporary:
    scene.render.filepath = str(Path(temporary) / "cycles.webm")
    bpy.ops.render.render(animation=True)
    subprocess.run([os.environ.get("FFMPEG", "ffmpeg"), "-y", "-i", scene.render.filepath,
                    "-vf", "hqdn3d=2:2:3:3", "-c:v", "libvpx", "-b:v", "1200k",
                    str(assets / "Editor/Trailers/Ossuary.webm")], check=True)
