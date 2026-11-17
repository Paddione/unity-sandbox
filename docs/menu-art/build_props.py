"""Ashen-fen dungeon props: TorchSconce, LootChest, BloodBanner.
Rebuildable Blender script in the style of docs/menu-art/build_icons.py.
Usage: blender -b --factory-startup --python-exit-code 1 -P build_props.py [--final]
Preview: 128px/16sp; final: 256px/64sp EEVEE, transparent film.
"""
import os
import sys

sys.path.insert(0, "/home/patrick/.local/share/muse/skills/bundled/muse-core~v1/skills/blender/scripts")
import bpy_helpers as H

FINAL = "--final" in sys.argv
WORK = "/tmp/menu-art"
OUT = os.path.join(WORK, "props-final" if FINAL else "props-preview")
os.makedirs(OUT, exist_ok=True)
RES = 256 if FINAL else 128
SAMPLES = 64 if FINAL else 16

H.reset_scene()

# Ashen-fen palette (DiabloPalette.cs)
bronze = H.material("Bronze", "#8A6238", roughness=0.45, metallic=0.65)
gold = H.material("EmberGold", "#F2B134", roughness=0.3, metallic=1.0)
stone = H.material("CryptStone", "#232028", roughness=0.9)
darkwood = H.material("DarkWood", "#5A3A22", roughness=0.8)
blood = H.material("BloodCloth", "#8E1620", roughness=0.9)
flame = H.emission_material("Flame", "#E84E00", 1.5)
steel = H.material("Steel", "#8A8F99", roughness=0.45, metallic=1.0)
gem = H.emission_material("Gem", "#C01020", 2.0)


def build_torch(ox):
    o = []
    o.append(H.add_box("Bracket", size=(0.10, 0.06, 0.30), bevel=0.01, location=(ox, 0, 0.35), mat=bronze))
    o.append(H.add_cylinder("Cup", radius=0.11, depth=0.14, location=(ox, 0, 0.55), mat=bronze))
    o.append(H.add_cylinder("FlameCore", radius=0.055, depth=0.22, location=(ox, 0, 0.68), mat=flame))
    o.append(H.add_sphere("FlameTip", radius=0.06, location=(ox, 0, 0.80), mat=flame))
    return o


def build_chest(ox):
    o = []
    o.append(H.add_box("Body", size=(0.62, 0.40, 0.30), bevel=0.015, location=(ox, 0, 0.15), mat=darkwood))
    lid = H.add_cylinder("Lid", radius=0.20, depth=0.62, location=(ox, 0, 0.30), mat=darkwood)
    lid.rotation_euler[1] = 1.5708
    o.append(lid)
    o.append(H.add_box("BandL", size=(0.05, 0.42, 0.34), bevel=0.008, location=(ox - 0.22, 0, 0.20), mat=gold))
    o.append(H.add_box("BandR", size=(0.05, 0.42, 0.34), bevel=0.008, location=(ox + 0.22, 0, 0.20), mat=gold))
    o.append(H.add_box("Lock", size=(0.10, 0.05, 0.12), bevel=0.01, location=(ox, -0.21, 0.24), mat=bronze))
    o.append(H.add_sphere("GemLock", radius=0.035, location=(ox, -0.24, 0.26), mat=gem))
    return o


def build_banner(ox):
    o = []
    o.append(H.add_cylinder("Pole", radius=0.035, depth=1.05, location=(ox, 0, 0.52), mat=bronze))
    o.append(H.add_sphere("Finial", radius=0.055, location=(ox, 0, 1.08), mat=gold))
    o.append(H.add_cylinder("Crossbar", radius=0.025, depth=0.55, location=(ox, 0, 0.96), mat=bronze))
    o[-1].rotation_euler[1] = 1.5708
    o.append(H.add_box("Cloth", size=(0.42, 0.02, 0.62), bevel=0.005, location=(ox, -0.06, 0.60), mat=blood))
    o.append(H.add_box("ClothHem", size=(0.44, 0.025, 0.06), bevel=0.005, location=(ox, -0.06, 0.30), mat=gold))
    o.append(H.add_box("Sigil", size=(0.16, 0.025, 0.16), bevel=0.005, location=(ox, -0.075, 0.62), mat=gold))
    return o


PROPS = [
    ("TorchSconce", build_torch),
    ("LootChest", build_chest),
    ("BloodBanner", build_banner),
]

SPACING = 1.4
all_objs = []
framed = {}
for i, (name, fn) in enumerate(PROPS):
    parts = fn(i * SPACING)
    all_objs.extend(parts)
    framed[name] = parts

H.studio_lighting(target=all_objs, key_azimuth=-40, key_elevation=45, intensity=1.0)
import bpy
for o in [x for x in bpy.data.objects if x.name.startswith("Floor")]:
    bpy.data.objects.remove(o, do_unlink=True)
H.set_render(engine="EEVEE", resolution=(RES, RES), samples=SAMPLES, percentage=100, transparent=True)
cam = H.add_camera("PropCam", location=(0, -2, 1), target=(0, 0, 0.45), lens=50)
for name, parts in framed.items():
    for other_name, other_parts in framed.items():
        hide = other_name != name
        for ob in other_parts:
            ob.hide_render = hide
            ob.hide_viewport = hide
    H.frame_objects(cam, parts, azimuth=25, elevation=12, margin=1.35)
    H.render_still(os.path.join(OUT, name + ".png"), background=None)

H.report()
H.save_blend(os.path.join(WORK, "props.blend"))
print("PROPS_DONE", OUT)
