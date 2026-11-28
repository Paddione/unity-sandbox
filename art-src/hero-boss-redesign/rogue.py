"""Rogue v1 — moor smuggler DPS on the KayKit Rig_Medium (Rogue base).

Silhouette goals: low cowl with a teal face scarf, asymmetric single leather
pauldron, bandolier of venom vials across the chest, small powder keg on the
back with a glowing fuse, wrapped forearms and shins, lean build.
Palette: soot leather, deep teal cloth, venom green, brass, ember fuse.
"""
import importlib
import sys

import bpy
from mathutils import Matrix

sys.path.insert(0, __import__("os").path.dirname(__file__))
import hero_kit as K  # noqa: E402

importlib.reload(K)

P = "RG_"
K.clear(P)
arm = bpy.data.objects["Rig_Rogue"]

soot = K.mat("rg_soot", K.hexrgb("#2b2420"), 0.85)
leather = K.mat("rg_leather", K.hexrgb("#5a3b28"), 0.7)
teal = K.mat("rg_teal", K.hexrgb("#1f4a4a"), 0.9)
venom = K.mat("rg_venom", K.hexrgb("#7cd04a"), 0.2, 0.0, emit=K.hexrgb("#4fd020"))
brass = K.mat("rg_brass", K.hexrgb("#b08a3c"), 0.4, 0.9)
ember = K.mat("rg_ember", K.hexrgb("#ff8a2a"), 0.3, 0.0, emit=K.hexrgb("#ff6a10"))
skin = K.mat("rg_skin", K.hexrgb("#c9946f"), 0.8)
wrap = K.mat("rg_wrap", K.hexrgb("#8a7d68"), 0.95)

TORSO = [(0.60, "hips"), (0.97, "spine"), (9.0, "chest")]
K.box(P + "Jerkin", soot, arm, ["hips", "spine", "chest"], (0, 0, 0.86), (0.5, 0.36, 0.74), taper=1.08, split=TORSO, bevel=0.03)
K.box(P + "Coattail", leather, arm, ["hips", "spine"], (0, 0.08, 0.46), (0.54, 0.34, 0.26), taper=1.1, split=[(0.6, "hips"), (9, "spine")], bevel=0.02)
K.cyl(P + "Belt", leather, arm, ["hips"], (0, 0, 0.62), 0.28, 0.08, verts=10)
K.box(P + "Buckle", brass, arm, ["hips"], (0, -0.28, 0.62), (0.09, 0.03, 0.08), bevel=0.01)

# Bandolier from right shoulder to left hip with venom vials.
band_rot = Matrix.Rotation(-0.62, 3, "Y")
K.box(P + "Bandolier", leather, arm, ["spine", "chest"], (0, -0.2, 0.9), (0.08, 0.04, 0.78), rot=band_rot, split=[(0.97, "spine"), (9, "chest")], bevel=0.0)
for i, t in enumerate((-0.22, -0.07, 0.08, 0.23)):
    x, z = -0.58 * t * -1, 0.9 + t * 0.8
    K.cyl(P + f"Vial{i}", venom, arm, ["chest" if z > 0.97 else "spine"], (x, -0.24, z), 0.035, 0.1, verts=6)

# Asymmetric shoulder: one layered leather pauldron (left), scarf knot right.
K.dome(P + "Pauldron", leather, arm, ["upperarm.l"], (0.3, 0, 1.13), (0.22, 0.26, 0.18), rot=Matrix.Rotation(-0.4, 3, "Y"))
K.dome(P + "PauldronTrim", brass, arm, ["upperarm.l"], (0.32, 0, 1.2), (0.12, 0.16, 0.06), rot=Matrix.Rotation(-0.4, 3, "Y"))

for side, s in (("L", 1), ("R", -1)):
    sd = side.lower()
    K.cyl(P + "UpperArm" + side, soot, arm, ["upperarm." + sd], (0.34 * s, 0, 1.11), 0.08, 0.24, axis="X")
    K.cyl(P + "Wrap" + side, wrap, arm, ["lowerarm." + sd], (0.6 * s, 0, 1.11), 0.085, 0.24, axis="X", r2=0.075, verts=6)
    K.box(P + "Glove" + side, leather, arm, ["hand." + sd], (0.8 * s, 0, 1.1), (0.12, 0.13, 0.13), bevel=0.03)
    K.cyl(P + "Thigh" + side, soot, arm, ["upperleg." + sd], (0.15 * s, 0, 0.4), 0.11, 0.24)
    K.cyl(P + "Shin" + side, wrap, arm, ["lowerleg." + sd], (0.16 * s, 0, 0.21), 0.1, 0.18, r2=0.09, verts=6)
    K.box(P + "Boot" + side, leather, arm, ["foot." + sd], (0.17 * s, -0.07, 0.06), (0.18, 0.3, 0.12), taper=0.85, bevel=0.03)

# Head: face ball, teal scarf over mouth/nose, low cowl leaving the eyes free.
K.ball(P + "Face", skin, arm, ["head"], (0, -0.02, 1.6), (0.37, 0.35, 0.36), segs=12, rings=8)
K.box(P + "EyeL", soot, arm, ["head"], (0.12, -0.36, 1.66), (0.07, 0.03, 0.06), bevel=0.0)
K.box(P + "EyeR", soot, arm, ["head"], (-0.12, -0.36, 1.66), (0.07, 0.03, 0.06), bevel=0.0)
K.cyl(P + "Scarf", teal, arm, ["head"], (0, -0.02, 1.48), 0.39, 0.2, verts=10, r2=0.36)
K.box(P + "ScarfTail", teal, arm, ["chest"], (-0.22, 0.2, 1.12), (0.1, 0.04, 0.32), bevel=0.01)
K.dome(P + "Cowl", soot, arm, ["head"], (0, 0.06, 1.66), (0.42, 0.42, 0.4), segs=10)
K.box(P + "CowlBrim", soot, arm, ["head"], (0, -0.3, 1.82), (0.5, 0.12, 0.05), bevel=0.01)

# Back powder keg with fuse.
K.cyl(P + "Keg", leather, arm, ["chest"], (0.08, 0.3, 1.0), 0.13, 0.26, verts=8, r2=0.13)
K.cyl(P + "KegHoopTop", brass, arm, ["chest"], (0.08, 0.3, 1.1), 0.135, 0.03, verts=8)
K.cyl(P + "KegHoopLow", brass, arm, ["chest"], (0.08, 0.3, 0.9), 0.135, 0.03, verts=8)
K.cyl(P + "Fuse", ember, arm, ["chest"], (0.08, 0.3, 1.18), 0.015, 0.1, verts=4)

K.hide_originals("Rogue_")
print("Rogue v1 parts:", len([o for o in bpy.data.objects if o.name.startswith(P)]))
