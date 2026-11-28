"""Act bosses v1 on the KayKit Skeleton rig (game scales them 2.0-2.2x).

Kilnmother (Akt I, Glutofen): stooped third-generation burn-mistress; her
torso is a brick kiln with a glowing grate, leather apron, soot hood with a
chimney collar, long iron tongs, the Rußbuch chained to her belt.
Glasswright (Akt II, Glasbrache): gaunt glassblower in a sand-leather apron,
crystal shards grown from shoulders and back, the warm prism crown
(Wendeline), a long blowpipe.
Thewes (Akt III, Moortor): the homecomer husk, bone-white skull with ember
eyes, torn cloak of the Laternenwacht, his grave light burning in the
ribcage, one hand reaching forward for the party's lantern.
"""
import importlib
import sys

import bpy
from mathutils import Matrix

sys.path.insert(0, __import__("os").path.dirname(__file__))
import hero_kit as K  # noqa: E402

importlib.reload(K)

TORSO = [(0.60, "hips"), (0.97, "spine"), (9.0, "chest")]


def _limbs(P, arm, upper, lower, hand, thigh, shin, foot, r_arm=0.1, r_leg=0.13):
    for side, s in (("L", 1), ("R", -1)):
        sd = side.lower()
        K.cyl(P + "UpperArm" + side, upper, arm, ["upperarm." + sd], (0.36 * s, 0, 1.11), r_arm, 0.26, axis="X")
        K.cyl(P + "LowerArm" + side, lower, arm, ["lowerarm." + sd], (0.6 * s, 0, 1.11), r_arm * 1.05, 0.26, axis="X", r2=r_arm * 1.2)
        K.box(P + "Hand" + side, hand, arm, ["hand." + sd], (0.82 * s, 0, 1.1), (0.16, 0.17, 0.17), bevel=0.03)
        K.cyl(P + "Thigh" + side, thigh, arm, ["upperleg." + sd], (0.17 * s, 0, 0.4), r_leg, 0.24)
        K.cyl(P + "Shin" + side, shin, arm, ["lowerleg." + sd], (0.17 * s, 0, 0.21), r_leg * 1.05, 0.18, r2=r_leg * 0.9)
        K.box(P + "Foot" + side, foot, arm, ["foot." + sd], (0.17 * s, -0.06, 0.07), (0.24, 0.36, 0.15), taper=0.9, bevel=0.03)


def kilnmother(arm, P="KM_"):
    K.clear(P)
    brick = K.mat("km_brick", K.hexrgb("#5b2f22"), 0.95)
    soot = K.mat("km_soot", K.hexrgb("#1e1a18"), 0.95)
    apron = K.mat("km_apron", K.hexrgb("#4a3324"), 0.8)
    iron = K.mat("km_iron", K.hexrgb("#3c3a38"), 0.45, 0.85)
    glow = K.mat("km_glow", K.hexrgb("#ff7a1a"), 0.3, 0.0, emit=K.hexrgb("#ff5a00"))
    ash = K.mat("km_ash_skin", K.hexrgb("#8a7c72"), 0.9)
    page = K.mat("km_page", K.hexrgb("#cdbb94"), 0.9)

    # Kiln torso: wide brick barrel, glowing grate, iron bands.
    K.box(P + "Kiln", brick, arm, ["hips", "spine", "chest"], (0, 0, 0.84), (0.9, 0.7, 0.86), taper=0.82, split=TORSO, bevel=0.05)
    for i, z in enumerate((0.62, 1.0)):
        K.box(P + f"Band{i}", iron, arm, ["spine" if z < 0.97 else "chest"], (0, 0, z), (0.93, 0.73, 0.05), bevel=0.0)
    K.box(P + "Grate", glow, arm, ["spine"], (0, -0.36, 0.86), (0.36, 0.03, 0.26), bevel=0.0)
    for i, x in enumerate((-0.12, 0.0, 0.12)):
        K.box(P + f"GrateBar{i}", iron, arm, ["spine"], (x, -0.38, 0.86), (0.03, 0.03, 0.28), bevel=0.0)
    K.box(P + "Apron", apron, arm, ["hips", "spine"], (0, -0.37, 0.46), (0.6, 0.04, 0.42), taper=1.1, split=[(0.6, "hips"), (9, "spine")], bevel=0.01)
    # Chimney collar and soot hood with the stooped head forward.
    K.cyl(P + "Chimney", brick, arm, ["chest"], (0, 0.12, 1.32), 0.3, 0.22, verts=8, r2=0.26)
    K.cyl(P + "ChimneyGlow", glow, arm, ["chest"], (0, 0.12, 1.44), 0.2, 0.03, verts=8)
    K.ball(P + "Face", ash, arm, ["head"], (0, -0.12, 1.52), (0.32, 0.3, 0.32), segs=10, rings=7)
    K.box(P + "EyeL", glow, arm, ["head"], (0.1, -0.42, 1.55), (0.07, 0.03, 0.04), bevel=0.0)
    K.box(P + "EyeR", glow, arm, ["head"], (-0.1, -0.42, 1.55), (0.07, 0.03, 0.04), bevel=0.0)
    K.dome(P + "Hood", soot, arm, ["head"], (0, -0.02, 1.52), (0.42, 0.44, 0.48), segs=10)
    # Massive arms, soot gauntlets.
    _limbs(P, arm, soot, ash, iron, apron, soot, soot, r_arm=0.13, r_leg=0.16)
    # Tongs in the right hand, Rußbuch chained to the left hip.
    K.box(P + "Tongs", iron, arm, ["hand.r"], (-0.9, -0.1, 0.72), (0.05, 0.05, 0.9), bevel=0.0)
    K.box(P + "TongsJaw", glow, arm, ["hand.r"], (-0.9, -0.1, 0.25), (0.1, 0.06, 0.08), bevel=0.0)
    K.box(P + "Book", page, arm, ["hips"], (0.46, -0.1, 0.48), (0.08, 0.2, 0.26), bevel=0.01)
    K.box(P + "BookCover", soot, arm, ["hips"], (0.5, -0.1, 0.48), (0.03, 0.22, 0.28), bevel=0.0)
    K.cyl(P + "BookChain", iron, arm, ["hips"], (0.42, -0.1, 0.6), 0.015, 0.14, verts=4)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])


def glasswright(arm, P="GW_"):
    K.clear(P)
    sand = K.mat("gw_sand", K.hexrgb("#b39b74"), 0.9)
    apron = K.mat("gw_apron", K.hexrgb("#6a4f36"), 0.75)
    robe = K.mat("gw_robe", K.hexrgb("#3c4a4f"), 0.9)
    glass = K.mat("gw_glass", K.hexrgb("#7fd8e0"), 0.05, 0.0, emit=K.hexrgb("#3fb8d0"))
    prism = K.mat("gw_prism", K.hexrgb("#ffc070"), 0.05, 0.0, emit=K.hexrgb("#ff9a30"))
    skin = K.mat("gw_skin", K.hexrgb("#b89882"), 0.85)
    brass = K.mat("gw_brass", K.hexrgb("#9a7a3a"), 0.4, 0.9)

    # Gaunt, tall torso and long robe.
    K.box(P + "Robe", robe, arm, ["hips", "spine", "chest"], (0, 0, 0.82), (0.48, 0.34, 0.86), taper=1.15, split=TORSO, bevel=0.03)
    K.box(P + "Apron", apron, arm, ["hips", "spine", "chest"], (0, -0.19, 0.72), (0.44, 0.04, 0.8), taper=0.9, split=TORSO, bevel=0.01)
    for side, s in (("L", 1), ("R", -1)):
        sd = side.lower()
        K.box(P + "Skirt" + side, robe, arm, ["upperleg." + sd], (0.12 * s, 0, 0.28), (0.26, 0.38, 0.46), taper=0.8, bevel=0.02)
    # Crystal shards grown from shoulders and back.
    shards = [((0.3, 0.06, 1.3), 0.4, -0.5), ((-0.3, 0.06, 1.3), 0.35, 0.5), ((0.12, 0.22, 1.38), 0.5, -0.2),
              ((-0.14, 0.24, 1.32), 0.42, 0.25), ((0.0, 0.26, 1.18), 0.32, 0.0)]
    for i, ((x, y, z), h, tilt) in enumerate(shards):
        bones = ["upperarm.l"] if x > 0.25 else ["upperarm.r"] if x < -0.25 else ["chest"]
        bm_rot = Matrix.Rotation(tilt, 3, "Y") @ Matrix.Rotation(-0.35, 3, "X")
        K.box(P + f"Shard{i}", glass, arm, bones, (x, y, z + h * 0.3), (0.1, 0.1, h), taper=0.05, rot=bm_rot, bevel=0.0)
    # Head with prism crown.
    K.ball(P + "Face", skin, arm, ["head"], (0, -0.02, 1.62), (0.32, 0.31, 0.38), segs=10, rings=7)
    K.box(P + "EyeL", glass, arm, ["head"], (0.1, -0.31, 1.66), (0.06, 0.03, 0.04), bevel=0.0)
    K.box(P + "EyeR", glass, arm, ["head"], (-0.1, -0.31, 1.66), (0.06, 0.03, 0.04), bevel=0.0)
    K.cyl(P + "CrownBand", brass, arm, ["head"], (0, 0, 1.86), 0.3, 0.06, verts=8)
    K.box(P + "Prism", prism, arm, ["head"], (0, -0.12, 2.0), (0.14, 0.14, 0.26), taper=0.1, bevel=0.0)
    for i, x in enumerate((-0.18, 0.18)):
        K.box(P + f"CrownShard{i}", glass, arm, ["head"], (x, -0.04, 1.96), (0.06, 0.06, 0.16), taper=0.1, bevel=0.0)
    _limbs(P, arm, robe, apron, skin, robe, robe, apron, r_arm=0.08, r_leg=0.1)
    # Blowpipe held upright in the left hand, glowing gather at the end.
    K.cyl(P + "Blowpipe", brass, arm, ["hand.l"], (0.9, -0.1, 1.1), 0.025, 1.3, verts=6)
    K.ball(P + "Gather", glass, arm, ["hand.l"], (0.9, -0.1, 0.42), (0.12, 0.12, 0.12), segs=8, rings=6)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])


def thewes(arm, P="TH_"):
    K.clear(P)
    bone = K.mat("th_bone", K.hexrgb("#d9d2bf"), 0.8)
    cloak = K.mat("th_cloak", K.hexrgb("#3a4038"), 0.95)
    moss = K.mat("th_moss", K.hexrgb("#4f6a38"), 0.95)
    rag = K.mat("th_rag", K.hexrgb("#6b6455"), 0.95)
    ember = K.mat("th_ember", K.hexrgb("#ffd27a"), 0.3, 0.0, emit=K.hexrgb("#ffb040"))
    iron = K.mat("th_iron", K.hexrgb("#3a3b3e"), 0.5, 0.8)
    hollow = K.mat("th_hollow", K.hexrgb("#0d0d0d"), 1.0)

    # Ribcage open at the front; the grave light burns inside.
    K.box(P + "Spine", bone, arm, ["spine", "chest"], (0, 0.1, 0.92), (0.1, 0.1, 0.62), split=[(0.97, "spine"), (9, "chest")], bevel=0.0)
    for i, z in enumerate((0.88, 0.98, 1.08, 1.17)):
        w = 0.5 - abs(z - 1.02) * 0.6
        K.box(P + f"Rib{i}", bone, arm, ["spine" if z < 0.97 else "chest"], (0, -0.02, z), (w, 0.32, 0.04), taper=1.0, bevel=0.0)
    K.ball(P + "GraveLight", ember, arm, ["chest"], (0, -0.02, 1.02), (0.12, 0.12, 0.14), segs=8, rings=6)
    K.box(P + "Pelvis", bone, arm, ["hips"], (0, 0, 0.6), (0.38, 0.24, 0.12), bevel=0.02)
    # Torn Laternenwacht cloak with moss on the shoulders.
    K.cloak(P + "Cloak", cloak, arm, top_z=1.26, bottom_z=0.18, width=0.86, y=0.2, teeth=9, depth_jag=0.14, flare=1.35)
    K.dome(P + "Mantle", cloak, arm, ["chest"], (0, 0.04, 1.16), (0.48, 0.34, 0.18), segs=10)
    K.dome(P + "MossL", moss, arm, ["upperarm.l"], (0.32, 0, 1.2), (0.16, 0.2, 0.08))
    K.dome(P + "MossR", moss, arm, ["upperarm.r"], (-0.32, 0, 1.2), (0.16, 0.2, 0.08))
    K.box(P + "WatchSash", rag, arm, ["spine", "chest"], (0, -0.18, 0.95), (0.08, 0.03, 0.7), rot=Matrix.Rotation(0.6, 3, "Y"), split=[(0.97, "spine"), (9, "chest")], bevel=0.0)
    # Skull with ember eyes and a jaw.
    K.ball(P + "Skull", bone, arm, ["head"], (0, -0.02, 1.62), (0.33, 0.31, 0.33), segs=10, rings=7)
    K.box(P + "EyeL", hollow, arm, ["head"], (0.11, -0.3, 1.64), (0.1, 0.04, 0.09), bevel=0.0)
    K.box(P + "EyeR", hollow, arm, ["head"], (-0.11, -0.3, 1.64), (0.1, 0.04, 0.09), bevel=0.0)
    K.box(P + "EmberL", ember, arm, ["head"], (0.11, -0.32, 1.64), (0.04, 0.02, 0.04), bevel=0.0)
    K.box(P + "EmberR", ember, arm, ["head"], (-0.11, -0.32, 1.64), (0.04, 0.02, 0.04), bevel=0.0)
    K.box(P + "Jaw", bone, arm, ["head"], (0, -0.18, 1.4), (0.24, 0.16, 0.08), bevel=0.01)
    K.dome(P + "HoodBack", cloak, arm, ["head"], (0, 0.12, 1.56), (0.38, 0.32, 0.42), segs=10)
    # Bony limbs with rag wraps; the empty lantern hook on the belt.
    _limbs(P, arm, bone, rag, bone, bone, rag, iron, r_arm=0.06, r_leg=0.08)
    K.box(P + "Hook", iron, arm, ["hips"], (-0.28, -0.12, 0.5), (0.03, 0.03, 0.22), bevel=0.0)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])


if __name__ == "__main__":
    bpy.context.window.scene = K.scene()
    print("Kilnmother", kilnmother(bpy.data.objects["Rig_Kilnmother"]))
    print("Glasswright", glasswright(bpy.data.objects["Rig_Glasswright"]))
    print("Thewes", thewes(bpy.data.objects["Rig_Thewes"]))
