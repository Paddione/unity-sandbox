"""Act bosses v2 — caricatures of AI-company CEOs (goal decisions D7-D10).

Human forms (spawn intro) on the KayKit Skeleton rig, chibi proportions like
the heroes. Recognition comes from exaggerated signature traits, never from
photo textures:
  A1 Elon Musk     - swept-back dark hair with a high hairline, broad square
                     jaw, smirk, black tee under a black blazer.
  A2 Sam Altman    - short tousled light-brown hair, wide eyes, slim, grey
                     crewneck sweater, jeans, white sneakers.
  A3 Dario Amodei  - big dark curls, round glasses, dark stubble, navy blazer
                     over a plain tee.
  A4 Jensen Huang  - swept silver hair, wide grin, black leather jacket over
                     a black tee, black jeans.
Monster forms follow in a separate module.
"""
import importlib
import sys

import bpy
from mathutils import Matrix

sys.path.insert(0, __import__("os").path.dirname(__file__))
import hero_kit as K  # noqa: E402

importlib.reload(K)

TORSO = [(0.60, "hips"), (0.97, "spine"), (9.0, "chest")]
HEAD_Z = 1.62


def _mats():
    m = {
        "skin": K.mat("ceo_skin", K.hexrgb("#e2b796"), 0.75),
        "skin_tan": K.mat("ceo_skin_tan", K.hexrgb("#d9a77e"), 0.75),
        "eye": K.mat("ceo_eye", K.hexrgb("#1c1a1a"), 0.4),
        "white": K.mat("ceo_eyewhite", K.hexrgb("#f2efe8"), 0.5),
        "mouth": K.mat("ceo_mouth", K.hexrgb("#7a3a34"), 0.6),
        "black": K.mat("ceo_black", K.hexrgb("#151515"), 0.8),
        "jeans": K.mat("ceo_jeans", K.hexrgb("#2c3a52"), 0.9),
        "sneaker": K.mat("ceo_sneaker", K.hexrgb("#ecebe6"), 0.7),
        "shoe": K.mat("ceo_shoe", K.hexrgb("#1a1614"), 0.5),
    }
    return m


def _head(P, arm, m, skin, brow_mat, jaw=1.0, eye_scale=1.0, smile=0.0, ears=True):
    """Chibi head with brows, eyes, nose, mouth; jaw >1 widens the lower face."""
    K.ball(P + "Head", skin, arm, ["head"], (0, -0.02, HEAD_Z), (0.36, 0.34, 0.37), segs=14, rings=10)
    if jaw > 1.0:
        K.box(P + "Jaw", skin, arm, ["head"], (0, 0.0, HEAD_Z - 0.17), (0.5 * jaw, 0.6, 0.22), taper=1.2, bevel=0.06)
    for side, s in (("L", 1), ("R", -1)):
        K.box(P + "EyeWhite" + side, m["white"], arm, ["head"], (0.12 * s, -0.33, HEAD_Z + 0.02), (0.1 * eye_scale, 0.03, 0.07 * eye_scale), bevel=0.0)
        K.box(P + "Pupil" + side, m["eye"], arm, ["head"], (0.12 * s, -0.345, HEAD_Z + 0.02), (0.045 * eye_scale, 0.02, 0.055 * eye_scale), bevel=0.0)
        K.box(P + "Brow" + side, brow_mat, arm, ["head"], (0.12 * s, -0.33, HEAD_Z + 0.1), (0.13, 0.03, 0.03), rot=Matrix.Rotation(0.12 * s, 3, "Y"), bevel=0.0)
        if ears:
            K.ball(P + "Ear" + side, skin, arm, ["head"], (0.355 * s, -0.02, HEAD_Z), (0.05, 0.08, 0.09), segs=6, rings=4)
    K.box(P + "Nose", skin, arm, ["head"], (0, -0.36, HEAD_Z - 0.05), (0.06, 0.06, 0.1), taper=0.7, bevel=0.01)
    K.box(P + "Mouth", m["mouth"], arm, ["head"], (0.02 * smile, -0.355, HEAD_Z - 0.15), (0.14 + 0.06 * abs(smile), 0.03, 0.03), rot=Matrix.Rotation(-0.25 * smile, 3, "Y"), bevel=0.0)


def _body(P, arm, top, sleeve, legs, feet, hands, slim=1.0, jacket=None, jacket_open=True):
    K.box(P + "Torso", top, arm, ["hips", "spine", "chest"], (0, 0, 0.86), (0.56 * slim, 0.38 * slim, 0.76), taper=1.08, split=TORSO, bevel=0.04)
    if jacket is not None:
        for side, s in (("L", 1), ("R", -1)):
            K.box(P + "Jacket" + side, jacket, arm, ["hips", "spine", "chest"], (0.17 * s * slim, -0.005, 0.88), (0.26 * slim, 0.42 * slim, 0.8), taper=1.1, split=TORSO, bevel=0.03)
            K.box(P + "Lapel" + side, jacket, arm, ["chest"], (0.1 * s, -0.22 * slim, 1.1), (0.08, 0.03, 0.22), rot=Matrix.Rotation(-0.35 * s, 3, "Y"), bevel=0.0)
    for side, s in (("L", 1), ("R", -1)):
        sd = side.lower()
        K.cyl(P + "UpperArm" + side, sleeve, arm, ["upperarm." + sd], (0.35 * s, 0, 1.11), 0.1 * slim, 0.26, axis="X")
        K.cyl(P + "LowerArm" + side, sleeve, arm, ["lowerarm." + sd], (0.6 * s, 0, 1.11), 0.095 * slim, 0.26, axis="X")
        K.box(P + "Hand" + side, hands, arm, ["hand." + sd], (0.8 * s, 0, 1.1), (0.12, 0.13, 0.13), bevel=0.03)
        K.cyl(P + "Thigh" + side, legs, arm, ["upperleg." + sd], (0.15 * s, 0, 0.4), 0.12 * slim, 0.26)
        K.cyl(P + "Shin" + side, legs, arm, ["lowerleg." + sd], (0.16 * s, 0, 0.21), 0.11 * slim, 0.2)
        K.box(P + "Shoe" + side, feet, arm, ["foot." + sd], (0.17 * s, -0.07, 0.06), (0.2, 0.32, 0.12), taper=0.9, bevel=0.03)


def musk(arm, P="EM_"):
    K.clear(P)
    m = _mats()
    hair = K.mat("em_hair", K.hexrgb("#3b2a20"), 0.6)
    blazer = K.mat("em_blazer", K.hexrgb("#222326"), 0.7)
    _body(P, arm, m["black"], blazer, m["black"], m["shoe"], m["skin"], slim=1.08, jacket=blazer)
    _head(P, arm, m, m["skin"], hair, jaw=1.25, eye_scale=0.85, smile=0.6)
    # Swept-back hair, high hairline (starts well behind the forehead).
    K.dome(P + "Hair", hair, arm, ["head"], (0, 0.06, HEAD_Z + 0.05), (0.37, 0.33, 0.33), segs=12)
    K.box(P + "HairSweep", hair, arm, ["head"], (0, -0.12, HEAD_Z + 0.3), (0.5, 0.24, 0.12), taper=0.8, rot=Matrix.Rotation(0.25, 3, "X"), bevel=0.04)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])


def altman(arm, P="SA_"):
    K.clear(P)
    m = _mats()
    hair = K.mat("sa_hair", K.hexrgb("#8a6a4a"), 0.7)
    sweater = K.mat("sa_sweater", K.hexrgb("#7d8084"), 0.95)
    _body(P, arm, sweater, sweater, m["jeans"], m["sneaker"], m["skin"], slim=0.9)
    K.cyl(P + "Collar", sweater, arm, ["chest"], (0, 0, 1.24), 0.2, 0.06, verts=10)
    _head(P, arm, m, m["skin"], hair, jaw=1.0, eye_scale=1.3, smile=0.2)
    # Short tousled hair: dome plus a few tufts on top and the fringe.
    K.dome(P + "Hair", hair, arm, ["head"], (0, 0.0, HEAD_Z + 0.08), (0.38, 0.37, 0.32), segs=12)
    for i, (x, y) in enumerate(((-0.15, -0.2), (0.0, -0.26), (0.15, -0.2), (-0.08, 0.0), (0.1, 0.05))):
        K.ball(P + f"Tuft{i}", hair, arm, ["head"], (x, y, HEAD_Z + 0.32), (0.11, 0.1, 0.08), segs=6, rings=4)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])


def amodei(arm, P="DA_"):
    K.clear(P)
    m = _mats()
    hair = K.mat("da_hair", K.hexrgb("#2a1f1a"), 0.85)
    stubble = K.mat("da_stubble", K.hexrgb("#9c7a66"), 0.95)
    navy = K.mat("da_blazer", K.hexrgb("#24304a"), 0.75)
    tee = K.mat("da_tee", K.hexrgb("#5e6a78"), 0.9)
    frame = K.mat("da_frames", K.hexrgb("#1a1a1a"), 0.3, 0.6)
    _body(P, arm, tee, navy, m["jeans"], m["shoe"], m["skin"], slim=0.98, jacket=navy)
    _head(P, arm, m, m["skin"], hair, jaw=1.0, eye_scale=1.0, smile=0.15)
    # Dark stubble over the lower face.
    # Thin shell hugging the lower face, below the mouth line.
    K.ball(P + "Stubble", stubble, arm, ["head"], (0, -0.02, HEAD_Z - 0.2), (0.37, 0.335, 0.18), segs=14, rings=10)
    # Big curls: a ring of bumpy balls over the scalp.
    K.dome(P + "Hair", hair, arm, ["head"], (0, 0.02, HEAD_Z + 0.06), (0.39, 0.38, 0.34), segs=12)
    curls = [(-0.26, -0.18), (-0.1, -0.27), (0.08, -0.27), (0.25, -0.18), (-0.3, 0.05), (0.3, 0.05),
             (-0.18, 0.2), (0.0, 0.26), (0.18, 0.2), (-0.1, -0.05), (0.12, -0.02)]
    for i, (x, y) in enumerate(curls):
        K.ball(P + f"Curl{i}", hair, arm, ["head"], (x, y, HEAD_Z + 0.27 + (0.05 if abs(x) < 0.2 else 0)), (0.11, 0.11, 0.1), segs=7, rings=5)
    # Round glasses: two rims and a bridge in front of the eyes.
    for side, s in (("L", 1), ("R", -1)):
        K.ring(P + "Rim" + side, frame, arm, ["head"], (0.12 * s, -0.37, HEAD_Z + 0.02), 0.08, 0.012)
    K.box(P + "Bridge", frame, arm, ["head"], (0, -0.37, HEAD_Z + 0.03), (0.06, 0.015, 0.015), bevel=0.0)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])


def huang(arm, P="JH_"):
    K.clear(P)
    m = _mats()
    hair = K.mat("jh_hair", K.hexrgb("#b8b8b6"), 0.5)
    leather = K.mat("jh_leather", K.hexrgb("#0e0e0f"), 0.25, 0.1)
    zip_ = K.mat("jh_zip", K.hexrgb("#9a9a9a"), 0.3, 1.0)
    _body(P, arm, m["black"], leather, m["black"], m["shoe"], m["skin_tan"], slim=1.0, jacket=leather)
    K.box(P + "Zip", zip_, arm, ["chest"], (0.12, -0.215, 1.0), (0.015, 0.01, 0.4), bevel=0.0)
    K.box(P + "CollarL", leather, arm, ["chest"], (0.16, -0.12, 1.25), (0.14, 0.18, 0.06), rot=Matrix.Rotation(-0.5, 3, "Y"), bevel=0.0)
    K.box(P + "CollarR", leather, arm, ["chest"], (-0.16, -0.12, 1.25), (0.14, 0.18, 0.06), rot=Matrix.Rotation(0.5, 3, "Y"), bevel=0.0)
    _head(P, arm, m, m["skin_tan"], K.mat("jh_brow", K.hexrgb("#4a4440"), 0.6), jaw=1.08, eye_scale=0.8, smile=1.0)
    # Swept silver hair with volume on top and short sides.
    K.dome(P + "Hair", hair, arm, ["head"], (0, 0.02, HEAD_Z + 0.07), (0.38, 0.36, 0.3), segs=12)
    K.box(P + "HairWave", hair, arm, ["head"], (0.04, -0.1, HEAD_Z + 0.3), (0.52, 0.3, 0.13), taper=0.75, rot=Matrix.Rotation(0.2, 3, "X") @ Matrix.Rotation(-0.08, 3, "Y"), bevel=0.04)
    return len([o for o in bpy.data.objects if o.name.startswith(P)])


ROSTER = [
    ("Rig_Kilnmother", "A1 Musk", musk),
    ("Rig_Glasswright", "A2 Altman", altman),
    ("Rig_Thewes", "A3 Amodei", amodei),
    ("Rig_Jensen", "A4 Huang", huang),
]

if __name__ == "__main__":
    from mathutils import Vector
    bpy.context.window.scene = K.scene()
    if "Rig_Jensen" not in bpy.data.objects:
        path = __import__("os").path.expanduser("~/unity-sandbox/sandbox/Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Warrior.fbx")
        rig, meshes = K.import_base(path, "Rig_Jensen", Vector((9.0, -4.5, 0)))
        K.hide_meshes(meshes)
    for prefix in ("KM_", "GW_", "TH_"):  # v1 bosses are discarded (D7)
        K.hide_meshes([o for o in bpy.data.objects if o.name.startswith(prefix)])
    for rig, label, fn in ROSTER:
        print(label, fn(bpy.data.objects[rig]))
