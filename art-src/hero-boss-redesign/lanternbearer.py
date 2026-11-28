"""Lanternbearer v1 — healer/support on the KayKit Rig_Medium (Mage base).

Silhouette goals: tall pointed hood, bone-white shoulder mantle with tassels,
bell sleeves, A-line robe split per leg so walking still reads, a glowing
lantern on the hip. Palette: ash robe, bone mantle, plum trim, ember light.
"""
import importlib
import sys

import bpy
from mathutils import Matrix

sys.path.insert(0, __import__("os").path.dirname(__file__))
import hero_kit as K  # noqa: E402

importlib.reload(K)

P = "LB_"
K.clear(P)
arm = bpy.data.objects["Rig_Lantern"]

ash = K.mat("lb_ash", K.hexrgb("#5f5a55"), 0.95)
bone = K.mat("lb_bone", K.hexrgb("#d6cab0"), 0.85)
plum = K.mat("lb_plum", K.hexrgb("#3d2638"), 0.9)
iron = K.mat("lb_iron", K.hexrgb("#3a3b3e"), 0.5, 0.8)
ember = K.mat("lb_ember", K.hexrgb("#ffcf6a"), 0.3, 0.0, emit=K.hexrgb("#ffb030"))
skin = K.mat("lb_skin", K.hexrgb("#d9b08f"), 0.8)
peat = K.mat("lb_peat", K.hexrgb("#3e2e22"), 0.85)

TORSO = [(0.60, "hips"), (0.97, "spine"), (9.0, "chest")]
K.box(P + "Robe", ash, arm, ["hips", "spine", "chest"], (0, 0, 0.88), (0.54, 0.4, 0.74), taper=0.92, split=TORSO, bevel=0.03)
K.cyl(P + "Sash", plum, arm, ["hips"], (0, 0, 0.62), 0.3, 0.1, verts=10)
K.box(P + "SashTail", plum, arm, ["hips"], (0.12, -0.28, 0.45), (0.1, 0.03, 0.3), bevel=0.01)

# Mantle with tassels.
K.dome(P + "Mantle", bone, arm, ["chest"], (0, 0, 1.12), (0.46, 0.36, 0.2), segs=10)
for i in range(7):
    x = -0.36 + i * 0.12
    K.cyl(P + f"Tassel{i}", plum, arm, ["chest"], (x, -0.3, 1.04), 0.03, 0.12, verts=6, r2=0.005)

for side, s in (("L", 1), ("R", -1)):
    sd = side.lower()
    # Robe skirt panel per leg (narrow top, flared hem).
    K.box(P + "Skirt" + side, ash, arm, ["upperleg." + sd], (0.13 * s, 0, 0.3), (0.3, 0.44, 0.48), taper=0.72, bevel=0.02)
    K.box(P + "Hem" + side, plum, arm, ["upperleg." + sd], (0.13 * s, 0, 0.07), (0.31, 0.45, 0.04), bevel=0.0)
    K.box(P + "Boot" + side, peat, arm, ["foot." + sd], (0.17 * s, -0.08, 0.05), (0.18, 0.28, 0.1), bevel=0.02)
    # Bell sleeves.
    K.cyl(P + "UpperArm" + side, ash, arm, ["upperarm." + sd], (0.34 * s, 0, 1.11), 0.09, 0.24, axis="X")
    K.cyl(P + "Sleeve" + side, ash, arm, ["lowerarm." + sd], (0.6 * s, 0, 1.11), 0.1 if s > 0 else 0.17, 0.26, axis="X", r2=0.17 if s > 0 else 0.1)
    K.cyl(P + "Cuff" + side, plum, arm, ["lowerarm." + sd], (0.72 * s, 0, 1.11), 0.175, 0.04, axis="X")
    K.box(P + "Hand" + side, skin, arm, ["hand." + sd], (0.8 * s, 0, 1.1), (0.12, 0.13, 0.13), bevel=0.03)

# Head: own face ball, pointed hood set back so the face reads.
K.ball(P + "Face", skin, arm, ["head"], (0, -0.02, 1.6), (0.38, 0.36, 0.36), segs=12, rings=8)
K.box(P + "EyeL", plum, arm, ["head"], (0.12, -0.36, 1.63), (0.06, 0.03, 0.08), bevel=0.0)
K.box(P + "EyeR", plum, arm, ["head"], (-0.12, -0.36, 1.63), (0.06, 0.03, 0.08), bevel=0.0)
K.dome(P + "Hood", plum, arm, ["head"], (0, 0.1, 1.55), (0.46, 0.44, 0.5), segs=10)
K.cyl(P + "HoodTip", plum, arm, ["head"], (0, 0.2, 2.15), 0.22, 0.42, verts=8, r2=0.0)
K.cyl(P + "HoodRim", bone, arm, ["head"], (0, -0.26, 1.58), 0.36, 0.05, axis="Y", verts=10)

# Hip lantern: iron cap/base around a glowing core.
K.cyl(P + "LanternChain", iron, arm, ["hips"], (0.3, -0.12, 0.6), 0.012, 0.12, verts=4)
K.cyl(P + "LanternCap", iron, arm, ["hips"], (0.3, -0.12, 0.53), 0.08, 0.05, verts=6, r2=0.03)
K.box(P + "LanternCore", ember, arm, ["hips"], (0.3, -0.12, 0.43), (0.11, 0.11, 0.14), bevel=0.0)
K.cyl(P + "LanternBase", iron, arm, ["hips"], (0.3, -0.12, 0.35), 0.08, 0.03, verts=6)

K.hide_originals("Mage_")
print("Lanternbearer v1 parts:", len([o for o in bpy.data.objects if o.name.startswith(P)]))
