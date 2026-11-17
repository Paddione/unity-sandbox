"""Blood-moor item icons: 9 low-poly props, one rebuildable script.

Renders one transparent 256px icon per base type in ItemDatabase
(ShortSword HeavyAxe Dagger ClothTunic LeatherVest Breastplate Hood Helm
Circlet). Uniques reuse their base-type icon; the UI tints the frame.
Usage: blender -b --factory-startup --python-exit-code 1 -P build_icons.py [--final]
Preview: 128px/16sp; final: 256px/64sp EEVEE, transparent film.
"""
import os
import sys

sys.path.insert(0, "/home/patrick/.local/share/muse/skills/bundled/muse-core~v1/skills/blender/scripts")
import bpy_helpers as H

FINAL = "--final" in sys.argv
WORK = "/tmp/menu-art"
OUT = os.path.join(WORK, "final" if FINAL else "preview")
os.makedirs(OUT, exist_ok=True)
RES = 256 if FINAL else 128
SAMPLES = 64 if FINAL else 16

H.reset_scene()

steel = H.material("Steel", "#B8BCC4", roughness=0.35, metallic=1.0)
darksteel = H.material("DarkSteel", "#62666F", roughness=0.5, metallic=1.0)
leather = H.material("Leather", "#7A5230", roughness=0.85)
cloth = H.material("Cloth", "#8A7A66", roughness=0.95)
gold = H.material("Gold", "#C9973F", roughness=0.3, metallic=1.0)
wood = H.material("Wood", "#5A3D24", roughness=0.85)
gem = H.emission_material("Gem", "#C01020", 2.0)


def build_sword(ox):
    o = []
    o.append(H.add_box("Blade", size=(0.10, 0.035, 0.62), bevel=0.008, location=(ox, 0, 0.55), mat=steel))
    o.append(H.add_cone("Tip", radius=0.055, depth=0.14, location=(ox, 0, 0.93), mat=steel))
    o.append(H.add_box("Guard", size=(0.30, 0.05, 0.05), bevel=0.01, location=(ox, 0, 0.22), mat=gold))
    o.append(H.add_cylinder("Grip", radius=0.028, depth=0.16, location=(ox, 0, 0.12), mat=leather))
    o.append(H.add_sphere("Pommel", radius=0.04, location=(ox, 0, 0.02), mat=gold))
    return o


def build_axe(ox):
    o = []
    o.append(H.add_cylinder("Haft", radius=0.032, depth=0.95, location=(ox, 0, 0.475), mat=wood))
    o.append(H.add_box("Head", size=(0.30, 0.07, 0.26), bevel=0.015, location=(ox + 0.10, 0, 0.83), mat=steel))
    o.append(H.add_box("Edge", size=(0.06, 0.05, 0.32), bevel=0.01, location=(ox + 0.27, 0, 0.83), mat=steel))
    o.append(H.add_box("Poll", size=(0.10, 0.09, 0.14), bevel=0.01, location=(ox - 0.10, 0, 0.83), mat=darksteel))
    o.append(H.add_torus("Band", major=0.045, minor=0.015, location=(ox, 0, 0.68), mat=gold))
    return o


def build_dagger(ox):
    o = []
    o.append(H.add_box("Blade", size=(0.07, 0.03, 0.44), bevel=0.007, location=(ox, 0, 0.42), mat=steel))
    o.append(H.add_cone("Tip", radius=0.04, depth=0.10, location=(ox, 0, 0.69), mat=steel))
    o.append(H.add_box("Guard", size=(0.20, 0.04, 0.04), bevel=0.008, location=(ox, 0, 0.18), mat=gold))
    o.append(H.add_cylinder("Grip", radius=0.024, depth=0.14, location=(ox, 0, 0.10), mat=leather))
    o.append(H.add_sphere("Pommel", radius=0.032, location=(ox, 0, 0.02), mat=gold))
    return o


def build_tunic(ox):
    o = []
    o.append(H.add_lathe("Body", [(0.20, 0.02), (0.23, 0.15), (0.19, 0.35), (0.16, 0.50), (0.14, 0.56)],
                         segments=48, location=(ox, 0, 0), mat=cloth))
    o.append(H.add_torus("Belt", major=0.205, minor=0.03, location=(ox, 0, 0.30), mat=leather))
    o.append(H.add_tube("SleeveL", [(ox - 0.16, 0, 0.48), (ox - 0.30, 0, 0.35)], radius=0.062, mat=cloth))
    o.append(H.add_tube("SleeveR", [(ox + 0.16, 0, 0.48), (ox + 0.30, 0, 0.35)], radius=0.062, mat=cloth))
    return o


def build_vest(ox):
    o = []
    o.append(H.add_cylinder("Torso", radius=0.22, depth=0.42, radius_top=0.18, vertices=6,
                            location=(ox, 0, 0.30), mat=leather))
    o.append(H.add_box("StrapL", size=(0.06, 0.03, 0.40), location=(ox - 0.08, -0.20, 0.30), mat=darksteel))
    o.append(H.add_box("StrapR", size=(0.06, 0.03, 0.40), location=(ox + 0.08, -0.20, 0.30), mat=darksteel))
    o.append(H.add_box("Buckle", size=(0.09, 0.04, 0.07), location=(ox, -0.20, 0.22), mat=gold))
    return o


def build_breastplate(ox):
    o = []
    o.append(H.add_lathe("Chest", [(0.16, 0.05), (0.21, 0.20), (0.19, 0.35), (0.15, 0.48), (0.10, 0.55)],
                         segments=48, location=(ox, 0, 0), mat=steel))
    o.append(H.add_torus("NeckTrim", major=0.11, minor=0.02, location=(ox, 0, 0.55), mat=gold))
    o.append(H.add_box("Ridge", size=(0.035, 0.03, 0.38), location=(ox, -0.185, 0.30), mat=darksteel))
    return o


def build_hood(ox):
    o = []
    o.append(H.add_lathe("Hood", [(0.23, 0.02), (0.22, 0.15), (0.18, 0.35), (0.12, 0.50), (0.02, 0.66)],
                         segments=48, location=(ox, 0, 0), mat=cloth))
    o.append(H.add_torus("Hem", major=0.225, minor=0.025, location=(ox, 0, 0.04), mat=leather))
    return o


def build_helm(ox):
    o = []
    o.append(H.add_sphere("Dome", radius=0.20, scale=(1, 1, 1.15), location=(ox, 0, 0.32), mat=steel))
    o.append(H.add_cylinder("Brim", radius=0.24, depth=0.035, location=(ox, 0, 0.13), mat=darksteel))
    o.append(H.add_box("Nasal", size=(0.05, 0.035, 0.22), location=(ox, -0.185, 0.28), mat=darksteel))
    o.append(H.add_sphere("Crest", radius=0.035, location=(ox, 0, 0.56), mat=gold))
    return o


def build_circlet(ox):
    o = []
    o.append(H.add_torus("Band", major=0.15, minor=0.032, location=(ox, 0, 0.35), mat=gold))
    o.append(H.add_sphere("Gem", radius=0.05, location=(ox, -0.15, 0.37), mat=gem))
    o.append(H.add_sphere("StudL", radius=0.022, location=(ox + 0.13, -0.075, 0.35), mat=gem))
    o.append(H.add_sphere("StudR", radius=0.022, location=(ox - 0.13, -0.075, 0.35), mat=gem))
    return o


ICONS = [
    ("ShortSword", build_sword),
    ("HeavyAxe", build_axe),
    ("Dagger", build_dagger),
    ("ClothTunic", build_tunic),
    ("LeatherVest", build_vest),
    ("Breastplate", build_breastplate),
    ("Hood", build_hood),
    ("Helm", build_helm),
    ("Circlet", build_circlet),
]

SPACING = 1.4
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
H.save_blend(os.path.join(WORK, "icons.blend"))
print("ICONS_DONE", OUT)
