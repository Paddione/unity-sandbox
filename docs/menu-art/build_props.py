"""Blender 4.0+ headless renderer for Cinderreach town/menu architectural & loot props.

Run headless from the repo root:
    blender -b --factory-startup --python-exit-code 1 -P docs/menu-art/build_props.py

Outputs 256x256 RGBA PNG sprites into sandbox/Assets/AddressableAssets/MenuArt/:
    TorchSconce, LootChest, BloodBanner
"""
import math
import os
import sys

import bpy
from mathutils import Vector

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(REPO_ROOT, "sandbox", "Assets", "AddressableAssets", "MenuArt")
KAYKIT_DUNGEON = os.path.join(REPO_ROOT, "sandbox", "Assets", "ThirdParty", "KayKit", "Dungeon", "Models")
BPY_HELPERS_DIR = "/home/patrick/.local/share/muse/skills/bundled/muse-core~v1/skills/blender/scripts"
if os.path.isdir(BPY_HELPERS_DIR) and BPY_HELPERS_DIR not in sys.path:
    sys.path.insert(0, BPY_HELPERS_DIR)
import bpy_helpers as H  # noqa: E402

PROP_SIZE = 256
CYCLES_SAMPLES = int(os.environ.get("CINDERREACH_PROP_SAMPLES", "96"))


def reset_scene():
    H.reset_scene()
    scene = bpy.context.scene
    scene.render.resolution_x = PROP_SIZE
    scene.render.resolution_y = PROP_SIZE
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    H.engine_id("CYCLES")
    scene.cycles.samples = CYCLES_SAMPLES
    scene.cycles.use_adaptive_sampling = True
    if hasattr(scene.cycles, "use_denoising"):
        scene.cycles.use_denoising = H.has_denoiser()
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        scene.view_settings.view_transform = "Filmic"
    scene.view_settings.exposure = 0.25
    H.world_color("#181216", strength=0.40)


def import_fbx(rel_path):
    full = os.path.join(KAYKIT_DUNGEON, rel_path)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=full)
    return [o for o in bpy.data.objects if o not in before]


def bind_dungeon_texture(objs):
    tex_path = os.path.join(KAYKIT_DUNGEON, "dungeon_texture.png")
    img = bpy.data.images.load(tex_path, check_existing=True)
    for o in objs:
        if o.type != "MESH":
            continue
        for mat in o.data.materials:
            if not mat:
                continue
            mat.use_nodes = True
            nt = mat.node_tree
            bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
            if not bsdf:
                continue
            tex_node = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None)
            if tex_node is None:
                tex_node = nt.nodes.new("ShaderNodeTexImage")
            tex_node.image = img
            nt.links.new(tex_node.outputs["Color"], H.principled_input(bsdf, "base_color"))
            H.set_principled(bsdf, roughness=0.45, metallic=0.18)


def add_three_point_lighting(center):
    c = Vector(center)
    H.add_light("AREA", "KeyLight", location=(c.x - 3.2, c.y - 3.5, c.z + 3.0),
                target=center, energy=360.0, size=2.8, light_color="#ffebd0")
    H.add_light("AREA", "FillLight", location=(c.x + 3.0, c.y - 2.6, c.z + 1.0),
                target=center, energy=140.0, size=3.2, light_color="#90b2da")
    H.add_light("AREA", "RimLight", location=(c.x + 1.4, c.y + 3.4, c.z + 2.5),
                target=center, energy=380.0, size=2.2, light_color="#ff9d42")


def render_prop(name, objs, azimuth=28, elevation=18, margin=1.14, lens=50.0, extra_lights=None):
    os.makedirs(OUT_DIR, exist_ok=True)
    meshes = [o for o in objs if o.type == "MESH"]
    cam = H.add_camera("PropCam", location=(0, -4, 1), target=(0, 0, 0), lens=lens)
    H.frame_objects(cam, meshes, azimuth=azimuth, elevation=elevation, margin=margin)
    lo, hi = H.bbox(meshes)
    center = (lo + hi) * 0.5
    add_three_point_lighting(center)
    if extra_lights:
        for fn in extra_lights:
            fn(center, lo, hi)
    out_path = os.path.join(OUT_DIR, f"{name}.png")
    bpy.context.scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"[build_props] Rendered {name} -> {out_path}")
    return out_path


def build_torch_sconce():
    objs = import_fbx("torch_mounted.fbx")
    bind_dungeon_texture(objs)
    lo, hi = H.bbox([o for o in objs if o.type == "MESH"])
    top_center = ((lo.x + hi.x) * 0.5, lo.y + (hi.y - lo.y) * 0.30, hi.z)
    # Sculpted triple layered stylized ember flame (pure emission shader prevents white key-light washout)
    flame_outer = H.emission_material("TorchOuterFlame", "#e83204", strength=1.7)
    flame_inner = H.emission_material("TorchInnerFlame", "#ff9418", strength=2.4)
    span = max(hi.x - lo.x, hi.z - lo.z)
    s = span * 0.16
    f1 = H.add_cone("FlameOuter", s * 0.95, s * 2.5, radius_top=s * 0.04,
                    location=(top_center[0], top_center[1], top_center[2] + s * 0.95),
                    rotation=(8, -6, 0), mat=flame_outer, vertices=16)
    f2 = H.add_cone("FlameLickL", s * 0.55, s * 1.6, radius_top=s * 0.03,
                    location=(top_center[0] - s * 0.38, top_center[1], top_center[2] + s * 0.65),
                    rotation=(4, -22, 0), mat=flame_outer, vertices=12)
    f3 = H.add_cone("FlameCore", s * 0.52, s * 1.4, radius_top=s * 0.03,
                    location=(top_center[0] + s * 0.12, top_center[1] - s * 0.05, top_center[2] + s * 0.55),
                    rotation=(0, 12, 0), mat=flame_inner, vertices=14)
    objs += [f1, f2, f3]

    def torch_glow(c, _lo, _hi):
        H.add_light("POINT", "TorchFlameLight",
                    location=(top_center[0] + s * 2.0, top_center[1] - s * 2.5, top_center[2] + s * 1.4),
                    energy=70.0, light_color="#ff7a20", shadow_soft=0.35)

    return render_prop("TorchSconce", objs, azimuth=32, elevation=14, margin=1.14, extra_lights=[torch_glow])


def build_loot_chest():
    objs = import_fbx("chest_gold.fbx")
    bind_dungeon_texture(objs)
    # Gently crack lid open to reveal warm golden coin hoard without hiding keyhole
    for o in objs:
        if "lid" in o.name.lower():
            o.rotation_euler.x -= math.radians(14)
    lo, hi = H.bbox([o for o in objs if o.type == "MESH"])
    cx, cy, cz = (lo.x + hi.x) * 0.5, (lo.y + hi.y) * 0.5, (lo.z + hi.z) * 0.5
    gold_mat = H.material_preset("ChestCoinGold", "gold", base_color="#f5c448", roughness=0.20, metallic=0.96)
    span = hi.x - lo.x
    for i, (dx, dy) in enumerate([(-0.18, -0.58), (0.04, -0.62), (0.22, -0.54), (-0.08, -0.52)]):
        coin = H.add_cylinder(f"HoardCoin{i}", span * 0.075, span * 0.018,
                              location=(cx + dx * span, cy + dy * span, lo.z + span * 0.01),
                              rotation=(0, 0, i * 25), mat=gold_mat, vertices=20, bevel=0.004)
        objs.append(coin)

    def treasure_glow(_c, _lo, _hi):
        H.add_light("POINT", "HoardGlow", location=(cx, cy - span * 0.28, lo.z + span * 0.28),
                    energy=28.0, light_color="#ffae38", shadow_soft=0.30)

    return render_prop("LootChest", objs, azimuth=34, elevation=24, margin=1.14, extra_lights=[treasure_glow])


def build_blood_banner():
    objs = import_fbx("banner_red.fbx")
    bind_dungeon_texture(objs)
    lo, hi = H.bbox([o for o in objs if o.type == "MESH"])
    span = hi.x - lo.x
    cx, cy = (lo.x + hi.x) * 0.5, (lo.y + hi.y) * 0.5
    # Darken red albedo into gothic Cinderreach oxblood crimson + add brass crossbar & finials
    for o in objs:
        if o.type != "MESH":
            continue
        for mat in o.data.materials:
            if not mat or not mat.use_nodes:
                continue
            nt = mat.node_tree
            bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
            tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None)
            if bsdf and tex:
                mix = nt.nodes.new("ShaderNodeMixRGB")
                mix.blend_type = "MULTIPLY"
                mix.inputs["Fac"].default_value = 1.0
                mix.inputs["Color2"].default_value = H.color("#8a3c42")
                nt.links.new(tex.outputs["Color"], mix.inputs["Color1"])
                nt.links.new(mix.outputs["Color"], H.principled_input(bsdf, "base_color"))
    brass = H.material_preset("BannerBrass", "brass", base_color="#c69540", roughness=0.28, metallic=0.92)
    bar = H.add_cylinder("CrossBar", span * 0.032, span * 1.24, location=(cx, cy, hi.z - span * 0.03),
                         rotation=(0, 90, 0), mat=brass, vertices=18, bevel=0.004)
    fin_l = H.add_cone("FinL", span * 0.052, span * 0.12, radius_top=span * 0.004,
                       location=(lo.x - span * 0.14, cy, hi.z - span * 0.03), rotation=(0, -90, 0), mat=brass, vertices=6)
    fin_r = H.add_cone("FinR", span * 0.052, span * 0.12, radius_top=span * 0.004,
                       location=(hi.x + span * 0.14, cy, hi.z - span * 0.03), rotation=(0, 90, 0), mat=brass, vertices=6)
    objs += [bar, fin_l, fin_r]
    return render_prop("BloodBanner", objs, azimuth=22, elevation=10, margin=1.14)


PROPS = [
    ("TorchSconce", build_torch_sconce),
    ("LootChest", build_loot_chest),
    ("BloodBanner", build_blood_banner),
]


def main():
    rendered = []
    for name, fn in PROPS:
        reset_scene()
        p = fn()
        rendered.append((name, p))
    print(f"[build_props] Complete: {len(rendered)} props.")


if __name__ == "__main__":
    main()
