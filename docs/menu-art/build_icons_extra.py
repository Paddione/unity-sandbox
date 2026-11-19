"""Blood-moor item icons, wave 2: bow, lantern, potion.

Same studio setup as build_icons.py (EEVEE, transparent film, 25/12
three-quarter framing) so the new icons sit next to the starter pack.
Renders one transparent 256px icon per missing base type:
ShortBow (covers HuntingBow/Worn Short Bow via keyword), HoodedLantern
(covers Lantern/EmberCenser/Worn Lantern), HealingPotion.
Usage: blender -b --factory-startup --python-exit-code 1 -P build_icons_extra.py [--final]
Preview: 128px/16sp; final: 256px/64sp EEVEE, transparent film.
"""
import os
import sys

sys.path.insert(0, "/home/patrick/.local/share/muse/skills/bundled/muse-core~v1/skills/blender/scripts")
import bpy_helpers as H

FINAL = "--final" in sys.argv
WORK = "/tmp/menu-art-extra"
OUT = os.path.join(WORK, "final" if FINAL else "preview")
os.makedirs(OUT, exist_ok=True)
RES = 256 if FINAL else 128
SAMPLES = 64 if FINAL else 16

H.reset_scene()

steel = H.material("Steel2", "#B8BCC4", roughness=0.35, metallic=1.0)
darksteel = H.material("DarkSteel2", "#62666F", roughness=0.5, metallic=1.0)
leather = H.material("Leather2", "#7A5230", roughness=0.85)
gold = H.material("Gold2", "#C9973F", roughness=0.3, metallic=1.0)
wood = H.material("Wood2", "#5A3D24", roughness=0.85)
string_mat = H.material("String2", "#D8CFAE", roughness=0.9)
glasswarm = H.emission_material("GlassWarm", "#FFB84D", 1.6)
liquid = H.material("Liquid2", "#A01218", roughness=0.25, metallic=0.0)
cork = H.material("Cork2", "#9A7248", roughness=0.95)


def build_bow(ox):
    o = []
    # Limbs: one swept curve across screen-X so the arc reads at 25/12.
    limb_pts = [(ox - 0.30, 0, -0.34), (ox - 0.16, 0, -0.18), (ox - 0.02, 0, 0.0),
                (ox - 0.16, 0, 0.18), (ox - 0.30, 0, 0.34)]
    o.append(H.add_tube("Limbs", limb_pts, radius=0.030, mat=wood))
    o.append(H.add_tube("Grip", [(ox - 0.035, 0, -0.09), (ox - 0.02, 0, 0.0), (ox - 0.035, 0, 0.09)],
                        radius=0.042, mat=leather))
    o.append(H.add_torus("BandTop", major=0.035, minor=0.012, location=(ox - 0.16, 0, 0.18), mat=gold))
    o.append(H.add_torus("BandBot", major=0.035, minor=0.012, location=(ox - 0.16, 0, -0.18), mat=gold))
    # String: straight gut line between the tips, arrow nocked on it.
    o.append(H.add_tube("String", [(ox - 0.30, 0, -0.34), (ox - 0.30, 0, 0.34)], radius=0.007, mat=string_mat))
    o.append(H.add_cylinder("Shaft", radius=0.011, depth=0.66, location=(ox - 0.22, 0, 0.02), mat=wood))
    o.append(H.add_cone("Head", radius=0.022, depth=0.07, location=(ox - 0.22, 0, 0.38), mat=steel))
    o.append(H.add_box("FletchA", size=(0.05, 0.005, 0.09), location=(ox - 0.22, 0, -0.28), mat=liquid))
    o.append(H.add_box("FletchB", size=(0.005, 0.05, 0.09), location=(ox - 0.22, 0, -0.28), mat=liquid))
    return o


def build_lantern(ox):
    o = []
    # Foot cup.
    o.append(H.add_lathe("Foot", [(0.13, 0.02), (0.15, 0.06), (0.11, 0.10), (0.11, 0.12)],
                         segments=32, location=(ox, 0, 0), mat=darksteel))
    # Glowing glass chimney.
    o.append(H.add_lathe("Glass", [(0.105, 0.12), (0.125, 0.22), (0.115, 0.34), (0.085, 0.44)],
                         segments=32, location=(ox, 0, 0), mat=glasswarm))
    # Four corner posts.
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(H.add_box("Post", size=(0.030, 0.030, 0.34),
                               location=(ox + sx * 0.105, sy * 0.105, 0.28), mat=darksteel))
    # Cap + handle.
    o.append(H.add_lathe("Cap", [(0.02, 0.50), (0.13, 0.48), (0.15, 0.44), (0.12, 0.42)],
                         segments=32, location=(ox, 0, 0), mat=darksteel))
    o.append(H.add_torus("Handle", major=0.075, minor=0.016, location=(ox, 0, 0.56), mat=gold))
    o.append(H.add_sphere("Knob", radius=0.025, location=(ox, 0, 0.50), mat=gold))
    return o


def build_potion(ox):
    o = []
    # Round red brew body (reads at icon size; glass shell would blow out).
    o.append(H.add_sphere("Brew", radius=0.20, scale=(1, 1, 1.1), location=(ox, 0, 0.26), mat=liquid))
    o.append(H.add_sphere("Shine", radius=0.05, location=(ox - 0.09, -0.10, 0.34), mat=glasswarm))
    o.append(H.add_cylinder("Neck", radius=0.055, depth=0.16, location=(ox, 0, 0.52), mat=darksteel))
    o.append(H.add_cylinder("Cork", radius=0.060, depth=0.07, location=(ox, 0, 0.62), mat=cork))
    o.append(H.add_torus("Collar", major=0.058, minor=0.014, location=(ox, 0, 0.45), mat=gold))
    return o


ICONS = [
    ("ShortBow", build_bow),
    ("HoodedLantern", build_lantern),
    ("HealingPotion", build_potion),
]

SPACING = 1.6
all_objs = []
framed = {}
for i, (name, fn) in enumerate(ICONS):
    parts = fn(i * SPACING)
    all_objs.extend(parts)
    framed[name] = parts

H.studio_lighting(target=all_objs, key_azimuth=-40, key_elevation=45, intensity=1.0)
import bpy
for o in [x for x in bpy.data.objects if x.name.startswith("Floor")]:
    bpy.data.objects.remove(o, do_unlink=True)
H.set_render(engine="EEVEE", resolution=(RES, RES), samples=SAMPLES, percentage=100, transparent=True)
cam = H.add_camera("IconCam", location=(0, -2, 1), target=(0, 0, 0.4), lens=50)
for name, parts in framed.items():
    H.frame_objects(cam, parts, azimuth=25, elevation=12, margin=1.2)
    H.render_still(os.path.join(OUT, name + ".png"), background=None)

H.report()
H.save_blend(os.path.join(WORK, "icons_extra.blend"))
print("ICONS_EXTRA_DONE", OUT)
