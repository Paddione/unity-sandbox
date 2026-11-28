"""Rogue v2 (male) / v1 (female) — moor smuggler DPS (Rogue / Rogue_Hooded base).

Silhouette goals: teal face scarf, asymmetric single leather pauldron,
bandolier of venom vials, wrapped forearms and shins, lean build, powder keg
on the right hip with a glowing fuse. Male: low cowl with a pointed peak
falling over the brow (v2, replaces the cap-like brim). Female (Rogue_Hooded
base in game): deep full hood with a long tail down the back and an auburn
ponytail out of the hood.
Palette: soot leather, deep teal cloth, venom green, brass, ember fuse.
"""
import importlib
import sys

import bpy
from mathutils import Matrix

sys.path.insert(0, __import__("os").path.dirname(__file__))
import hero_kit as K  # noqa: E402

importlib.reload(K)


def build(arm, P, female=False):
    K.clear(P)
    soot = K.mat("rg_soot", K.hexrgb("#2b2420"), 0.85)
    leather = K.mat("rg_leather", K.hexrgb("#5a3b28"), 0.7)
    teal = K.mat("rg_teal", K.hexrgb("#1f4a4a"), 0.9)
    venom = K.mat("rg_venom", K.hexrgb("#7cd04a"), 0.2, 0.0, emit=K.hexrgb("#4fd020"))
    brass = K.mat("rg_brass", K.hexrgb("#b08a3c"), 0.4, 0.9)
    ember = K.mat("rg_ember", K.hexrgb("#ff8a2a"), 0.3, 0.0, emit=K.hexrgb("#ff6a10"))
    skin = K.mat("rg_skin", K.hexrgb("#c9946f"), 0.8)
    wrap = K.mat("rg_wrap", K.hexrgb("#8a7d68"), 0.95)
    hair = K.mat("hero_hair_auburn", K.hexrgb("#8a3a22"), 0.7)

    TORSO = [(0.60, "hips"), (0.97, "spine"), (9.0, "chest")]
    K.box(P + "Jerkin", soot, arm, ["hips", "spine", "chest"], (0, 0, 0.86), (0.46 if female else 0.5, 0.36, 0.74), taper=1.16 if female else 1.08, split=TORSO, bevel=0.03)
    K.box(P + "Coattail", leather, arm, ["hips", "spine"], (0, 0.08, 0.46), (0.54, 0.34, 0.26), taper=1.1, split=[(0.6, "hips"), (9, "spine")], bevel=0.02)
    K.cyl(P + "Belt", leather, arm, ["hips"], (0, 0, 0.62), 0.27 if female else 0.28, 0.08, verts=10)
    K.box(P + "Buckle", brass, arm, ["hips"], (0, -0.27, 0.62), (0.09, 0.03, 0.08), bevel=0.01)

    band_rot = Matrix.Rotation(-0.62, 3, "Y")
    K.box(P + "Bandolier", leather, arm, ["spine", "chest"], (0, -0.2, 0.9), (0.08, 0.04, 0.78), rot=band_rot, split=[(0.97, "spine"), (9, "chest")], bevel=0.0)
    for i, t in enumerate((-0.22, -0.07, 0.08, 0.23)):
        x, z = 0.58 * t, 0.9 + t * 0.8
        K.cyl(P + f"Vial{i}", venom, arm, ["chest" if z > 0.97 else "spine"], (x, -0.24, z), 0.035, 0.1, verts=6)

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

    K.ball(P + "Face", skin, arm, ["head"], (0, -0.02, 1.6), (0.37, 0.35, 0.36), segs=12, rings=8)
    K.box(P + "EyeL", soot, arm, ["head"], (0.12, -0.36, 1.66), (0.07, 0.03, 0.06), bevel=0.0)
    K.box(P + "EyeR", soot, arm, ["head"], (-0.12, -0.36, 1.66), (0.07, 0.03, 0.06), bevel=0.0)
    if female:
        # Scarf worn low as a collar: the face stays readable from the front.
        K.cyl(P + "Scarf", teal, arm, ["chest"], (0, 0, 1.27), 0.3, 0.12, verts=10, r2=0.26)
        K.box(P + "Lips", K.mat("rg_lips", K.hexrgb("#8a3c3c"), 0.6), arm, ["head"], (0, -0.36, 1.5), (0.1, 0.03, 0.03), bevel=0.0)
    else:
        K.cyl(P + "Scarf", teal, arm, ["head"], (0, -0.02, 1.48), 0.39, 0.2, verts=10, r2=0.36)
    K.box(P + "ScarfTail", teal, arm, ["chest"], (-0.22, 0.2, 1.12), (0.1, 0.04, 0.32), bevel=0.01)
    if female:
        # Deep hood with a long tail down the back, ponytail out of the hood.
        K.dome(P + "Hood", soot, arm, ["head"], (0, 0.08, 1.6), (0.45, 0.46, 0.5), segs=10)
        K.cyl(P + "HoodPeak", soot, arm, ["head"], (0, -0.44, 1.88), 0.13, 0.2, verts=6, axis="Y", r2=0.0)
        K.box(P + "HoodTail", soot, arm, ["chest"], (0, 0.3, 1.12), (0.22, 0.06, 0.4), taper=1.3, bevel=0.01)
        K.braid(P + "Ponytail", hair, arm, [(0.3, -0.18, 1.48), (0.31, -0.25, 1.34), (0.29, -0.28, 1.2), (0.27, -0.29, 1.08)], radius=0.065)
    else:
        K.dome(P + "Cowl", soot, arm, ["head"], (0, 0.06, 1.66), (0.42, 0.42, 0.4), segs=10)
        # v2: pointed peak over the brow instead of the cap-like brim.
        K.cyl(P + "HoodPeak", soot, arm, ["head"], (0, -0.42, 1.86), 0.12, 0.18, verts=6, axis="Y", r2=0.0)

    # v2: powder keg on the right hip (readable from the front).
    K.cyl(P + "Keg", leather, arm, ["hips"], (-0.36, -0.04, 0.5), 0.1, 0.2, verts=8)
    K.cyl(P + "KegHoopTop", brass, arm, ["hips"], (-0.36, -0.04, 0.58), 0.105, 0.025, verts=8)
    K.cyl(P + "KegHoopLow", brass, arm, ["hips"], (-0.36, -0.04, 0.42), 0.105, 0.025, verts=8)
    K.cyl(P + "Fuse", ember, arm, ["hips"], (-0.36, -0.04, 0.65), 0.015, 0.09, verts=4)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])
