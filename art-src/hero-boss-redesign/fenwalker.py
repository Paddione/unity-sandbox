"""Fenwalker v2 — moor warden tank on the KayKit Rig_Medium.

Silhouette goals: very wide shoulders (stacked peat-iron pauldrons), closed
bucket helm with a glowing amber T-slit, reed cloak with a ragged hem, heavy
boots. Palette: moss cloth, peat leather, rusted iron, ember accent.
"""
import importlib
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, __import__("os").path.dirname(__file__))
import hero_kit as K  # noqa: E402

importlib.reload(K)

P = "FW_"
K.clear(P)
arm = bpy.data.objects.get("Rig_Fenwalker") or bpy.data.objects["Rig_Medium"]

moss = K.mat("fw_moss", K.hexrgb("#4b5a3a"), 0.95)
moss_dk = K.mat("fw_moss_dark", K.hexrgb("#2f3a26"), 0.95)
peat = K.mat("fw_peat", K.hexrgb("#4a3626"), 0.85)
iron = K.mat("fw_iron", K.hexrgb("#4a4c4f"), 0.45, 0.8)
rust = K.mat("fw_rust", K.hexrgb("#7a4a32"), 0.6, 0.5)
ember = K.mat("fw_ember", K.hexrgb("#f0a030"), 0.4, 0.0, emit=K.hexrgb("#ff9a20"))
skin = K.mat("fw_skin", K.hexrgb("#c99a7a"), 0.8)

# Torso: tapered gambeson split hips/spine/chest.
TORSO = [(0.60, "hips"), (0.97, "spine"), (9.0, "chest")]
K.box(P + "Torso", moss, arm, ["hips", "spine", "chest"], (0, 0, 0.86), (0.7, 0.48, 0.78), taper=1.2, split=TORSO, bevel=0.04)
K.box(P + "Breastplate", iron, arm, ["chest"], (0, -0.22, 1.06), (0.66, 0.1, 0.38), taper=1.08, bevel=0.03)
K.box(P + "PlateRivetBar", rust, arm, ["chest"], (0, -0.26, 1.06), (0.08, 0.04, 0.3), bevel=0.01)
K.box(P + "Tasset", peat, arm, ["hips", "spine"], (0, 0, 0.5), (0.68, 0.5, 0.22), taper=0.92, split=[(0.6, "hips"), (9, "spine")], bevel=0.03)
K.cyl(P + "Belt", peat, arm, ["hips"], (0, 0, 0.62), 0.37, 0.09, verts=10)
K.box(P + "Buckle", ember, arm, ["hips"], (0, -0.36, 0.62), (0.12, 0.04, 0.1), bevel=0.01)
K.cyl(P + "Gorget", moss_dk, arm, ["chest"], (0, 0, 1.24), 0.26, 0.14, verts=8, r2=0.2)

# Shoulders: two stacked domes per side, rust over iron.
for side, s in (("L", 1), ("R", -1)):
    ua = "upperarm." + side.lower()
    K.dome(P + "PauldronLow" + side, iron, arm, [ua], (0.4 * s, 0, 1.12), (0.36, 0.38, 0.28), rot=Matrix.Rotation(-0.4 * s, 3, "Y"))
    K.dome(P + "PauldronTop" + side, rust, arm, [ua], (0.37 * s, 0, 1.25), (0.27, 0.3, 0.19), rot=Matrix.Rotation(-0.35 * s, 3, "Y"))
    for i, dy in enumerate((-0.16, 0.0, 0.16)):
        K.cyl(P + f"Stud{side}{i}", ember if i == 1 else rust, arm, [ua], (0.42 * s, dy, 1.45), 0.035, 0.08, verts=6, r2=0.005)
    K.cyl(P + "UpperArm" + side, moss, arm, [ua], (0.42 * s, 0, 1.11), 0.1, 0.22, axis="X")
    K.cyl(P + "Bracer" + side, iron, arm, ["lowerarm." + side.lower()], (0.6 * s, 0, 1.11), 0.11, 0.24, axis="X", r2=0.13)
    K.box(P + "Fist" + side, peat, arm, ["hand." + side.lower()], (0.81 * s, 0, 1.1), (0.15, 0.17, 0.17), bevel=0.03)
    # Legs
    ul, ll, ft = "upperleg." + side.lower(), "lowerleg." + side.lower(), "foot." + side.lower()
    K.cyl(P + "Thigh" + side, peat, arm, [ul], (0.17 * s, 0, 0.4), 0.13, 0.24)
    K.cyl(P + "Greave" + side, iron, arm, [ll], (0.17 * s, 0, 0.21), 0.14, 0.18, r2=0.12)
    K.box(P + "Boot" + side, peat, arm, [ft], (0.17 * s, -0.06, 0.07), (0.24, 0.36, 0.16), taper=0.9, bevel=0.03)
    K.box(P + "BootCap" + side, rust, arm, [ft], (0.17 * s, -0.22, 0.06), (0.22, 0.08, 0.1), bevel=0.02)

# Head: closed bucket helm with T-slit and moss crest (helm hides hair in game).
K.cyl(P + "Helm", iron, arm, ["head"], (0, 0, 1.62), 0.42, 0.62, verts=8, r2=0.36, bevel=0.03)
K.dome(P + "HelmTop", iron, arm, ["head"], (0, 0, 1.93), (0.36, 0.36, 0.14))
K.box(P + "SlitH", ember, arm, ["head"], (0, -0.4, 1.66), (0.42, 0.05, 0.06), bevel=0.0)
K.box(P + "SlitV", ember, arm, ["head"], (0, -0.4, 1.53), (0.07, 0.05, 0.22), bevel=0.0)
K.box(P + "Crest", moss_dk, arm, ["head"], (0, 0.02, 2.06), (0.08, 0.6, 0.16), taper=0.6, bevel=0.02)
K.cyl(P + "HelmBand", rust, arm, ["head"], (0, 0, 1.36), 0.43, 0.08, verts=8)

# Reed cloak on the back.
K.cloak(P + "Cloak", moss_dk, arm, top_z=1.22, bottom_z=0.28, width=0.72, y=0.24)

K.hide_originals("Knight_")
print("Fenwalker v2 parts:", len([o for o in bpy.data.objects if o.name.startswith(P)]))
