"""Build all six hero variants into the shared Blender's "Scene".

Run inside Blender: exec(open(p).read(), {"__file__": p, "__name__": "__main__"}).
Male row at y=0, female row at y=3; bases follow ClassAppearance.SkinModelPath
(female Fenwalker = Ranger, female Rogue = Rogue_Hooded, Lanternbearer = Mage).
"""
import importlib
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(__file__)
sys.path.insert(0, HERE)
import hero_kit as K  # noqa: E402
import fenwalker  # noqa: E402
import lanternbearer  # noqa: E402
import rogue  # noqa: E402

for m in (K, fenwalker, lanternbearer, rogue):
    importlib.reload(m)

CHARS = os.path.expanduser("~/unity-sandbox/sandbox/Assets/ThirdParty/KayKit/Adventurers/Characters/")
ROSTER = [
    # rig name, base fbx, location, module, prefix, female
    ("Rig_Fenwalker", "Knight.fbx", (0.0, 0, 0), fenwalker, "FW_", False),
    ("Rig_Lantern", "Mage.fbx", (2.2, 0, 0), lanternbearer, "LB_", False),
    ("Rig_Rogue", "Rogue.fbx", (4.4, 0, 0), rogue, "RG_", False),
    ("Rig_FenwalkerF", "Ranger.fbx", (0.0, 3, 0), fenwalker, "FWF_", True),
    ("Rig_LanternF", "Mage.fbx", (2.2, 3, 0), lanternbearer, "LBF_", True),
    ("Rig_RogueF", "Rogue_Hooded.fbx", (4.4, 3, 0), rogue, "RGF_", True),
]

bpy.context.window.scene = K.scene()
for rig_name, fbx, loc, mod, prefix, female in ROSTER:
    rig = bpy.data.objects.get(rig_name)
    if rig is None:
        rig, base_meshes = K.import_base(CHARS + fbx, rig_name, Vector(loc))
        K.hide_meshes(base_meshes)
    n = mod.build(rig, prefix, female)
    print(f"{rig_name}: {n} parts")
# Base meshes of the first-run rigs (imported before this driver existed).
for base in ("Knight_", "Mage_", "Rogue_", "Ranger_"):
    K.hide_originals(base)
