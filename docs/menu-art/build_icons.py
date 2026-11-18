"""Blender 4.0+ headless renderer for Cinderreach 2D sprite item icons.

Run headless from the repo root:
    blender -b --factory-startup --python-exit-code 1 -P docs/menu-art/build_icons.py

Outputs 256x256 RGBA PNG sprites into sandbox/Assets/Resources/MenuArt/:
    ShortSword, HeavyAxe, Dagger,
    ClothTunic, LeatherVest, Breastplate,
    Hood, Helm, Circlet,
    Lantern, HoodedLantern, EmberCenser,
    HealingPotion, GoldCoin

Also writes a quick review contact sheet to $TMP/cinderreach_icons_sheet.png when
CINDERREACH_PREVIEW_SHEET is set (used during art authoring, never shipped).
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Euler, Matrix, Vector

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(REPO_ROOT, "sandbox", "Assets", "Resources", "MenuArt")
BPY_HELPERS_DIR = "/home/patrick/.local/share/muse/skills/bundled/muse-core~v1/skills/blender/scripts"
if os.path.isdir(BPY_HELPERS_DIR) and BPY_HELPERS_DIR not in sys.path:
    sys.path.insert(0, BPY_HELPERS_DIR)
import bpy_helpers as H  # noqa: E402

ICON_SIZE = 256
CYCLES_SAMPLES = int(os.environ.get("CINDERREACH_ICON_SAMPLES", "96"))
PREVIEW_SHEET = os.environ.get("CINDERREACH_PREVIEW_SHEET", "")


def reset_scene():
    H.reset_scene()
    scene = bpy.context.scene
    scene.render.resolution_x = ICON_SIZE
    scene.render.resolution_y = ICON_SIZE
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    H.engine_id("CYCLES")
    scene.cycles.samples = CYCLES_SAMPLES
    scene.cycles.use_adaptive_sampling = True
    scene.cycles.max_bounces = 6
    scene.cycles.glossy_bounces = 4
    scene.cycles.transmission_bounces = 4
    if hasattr(scene.cycles, "use_denoising"):
        scene.cycles.use_denoising = H.has_denoiser()
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        scene.view_settings.view_transform = "Filmic"
    scene.view_settings.exposure = 0.25
    H.world_color("#171216", strength=0.35)


def make_materials():
    m = {}
    m["steel"] = H.add_noise_variation(H.material_preset("ForgedSteel", "steel", base_color="#b8c0c8", roughness=0.22, metallic=0.95), scale=12.0, amount=0.14, roughness_amount=0.06, bump=0.04)
    m["edge_steel"] = H.add_noise_variation(H.material_preset("EdgeSteel", "chrome", base_color="#dce3ea", roughness=0.14, metallic=0.98), scale=16.0, amount=0.08, roughness_amount=0.04, bump=0.02)
    m["dark_iron"] = H.material_preset("DarkIron", "stone", base_color="#34373e", roughness=0.38, metallic=0.88, scale=8.0)
    m["gold"] = H.add_noise_variation(H.material_preset("AgedGold", "gold", base_color="#d8a244", roughness=0.22, metallic=0.96), scale=10.0, amount=0.12, roughness_amount=0.05, bump=0.03)
    m["brass"] = H.add_noise_variation(H.material_preset("TarnishedBrass", "brass", base_color="#a87936", roughness=0.30, metallic=0.92), scale=10.0, amount=0.14, roughness_amount=0.06, bump=0.04)
    m["bronze"] = H.material_preset("DarkBronze", "copper", base_color="#734d2c", roughness=0.34, metallic=0.88)
    m["leather"] = H.material_preset("OxbloodLeather", "leather", base_color="#5a261d", roughness=0.56, scale=12.0)
    m["dark_leather"] = H.material_preset("DarkLeather", "leather", base_color="#2a1d17", roughness=0.62, scale=12.0)
    m["wood"] = H.material_preset("AshWood", "wood", base_color="#4c3120", roughness=0.62, scale=10.0)
    m["cloth_crimson"] = H.material_preset("CrimsonCloth", "fabric", base_color="#721d1d", roughness=0.78, scale=12.0)
    m["cloth_ash"] = H.material_preset("AshCloth", "fabric", base_color="#3b3842", roughness=0.82, scale=12.0)
    m["cloth_hood"] = H.material_preset("DarkHoodCloth", "fabric", base_color="#2c2428", roughness=0.84, scale=12.0)
    m["ruby"] = H.material("GemRuby", "#b80a16", roughness=0.05, metallic=0.15, emission="#980814", emission_strength=1.2)
    m["amber"] = H.material("GemAmber", "#e86410", roughness=0.08, metallic=0.1, emission="#e05808", emission_strength=2.2)
    m["ember_core"] = H.material("EmberCore", "#ff7018", roughness=0.18, emission="#ff5a08", emission_strength=4.2)
    m["potion_liquid"] = H.material("PotionLiquid", "#7a020c", roughness=0.14, emission="#68020a", emission_strength=0.65)
    m["potion_glass"] = H.material("PotionGlass", "#ffe8ec", roughness=0.05, transmission=0.82, ior=1.5, alpha=0.38)
    m["cork"] = H.material_preset("Cork", "wood", base_color="#9d744a", roughness=0.82, scale=20.0)
    m["wax"] = H.material("WaxSeal", "#8c1018", roughness=0.30, coat=0.5)
    m["parchment"] = H.material_preset("ParchmentTag", "fabric", base_color="#c8b08a", roughness=0.72, scale=14.0)
    m["gold_coin"] = H.add_noise_variation(H.material_preset("SovereignGold", "gold", base_color="#f5c44c", roughness=0.22, metallic=0.96), scale=12.0, amount=0.08, roughness_amount=0.05, bump=0.03)
    m["void_black"] = H.material("VoidBlack", "#050406", roughness=0.95)
    return m


def add_lighting(cam_pos, target):
    # Warm key from front-upper-left, subtle camera bounce fill so front metals gleam, cool fill right, warm ember rim
    t = Vector(target)
    H.add_light("AREA", "KeyLight", location=(t.x - 2.4, t.y - 3.2, t.z + 2.6),
                target=target, energy=290.0, size=2.6, light_color="#ffebd2")
    H.add_light("AREA", "FrontFill", location=(t.x + 0.6, t.y - 3.6, t.z + 0.6),
                target=target, energy=120.0, size=3.4, light_color="#f4d8bc")
    H.add_light("AREA", "FillLight", location=(t.x + 2.8, t.y - 2.0, t.z + 1.0),
                target=target, energy=110.0, size=3.0, light_color="#98b8e0")
    H.add_light("AREA", "RimLight", location=(t.x + 1.2, t.y + 3.0, t.z + 2.2),
                target=target, energy=320.0, size=1.8, light_color="#ffaa55")
    H.add_light("AREA", "CoolRim", location=(t.x - 2.0, t.y + 2.8, t.z - 0.6),
                target=target, energy=120.0, size=2.2, light_color="#78a6cc")


def render_icon(name, objs, mats, azimuth=22, elevation=16, margin=1.10, roll=0.0,
                extra_lights=None, lens=55.0):
    os.makedirs(OUT_DIR, exist_ok=True)
    cam = H.add_camera("IconCam", location=(0, -4, 1), target=(0, 0, 0), lens=lens)
    H.frame_objects(cam, objs, azimuth=azimuth, elevation=elevation, margin=margin, roll=roll)
    lo, hi = H.bbox(objs)
    center = (lo + hi) * 0.5
    add_lighting(cam.location, center)
    if extra_lights:
        for fn in extra_lights:
            fn(center)
    out_path = os.path.join(OUT_DIR, f"{name}.png")
    bpy.context.scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"[build_icons] Rendered {name} -> {out_path}")
    return out_path


def bevel_obj(obj, width=0.012, segments=2):
    H.add_modifier(obj, "BEVEL", width=width, segments=segments, limit_method="ANGLE", angle_limit=math.radians(28))
    H.shade_smooth(obj)
    return obj


def make_beveled_blade(name, profile_pts, half_thick_center, half_thick_edge, mat):
    """Build a symmetrical diamond-cross-section weapon blade vertically along +Z."""
    bm = bmesh.new()
    rings = []
    for z, half_w in profile_pts:
        if half_w < 1e-4:
            v = bm.verts.new((0, 0, z))
            rings.append([v])
        else:
            ht = half_thick_center * max(0.28, min(1.0, half_w / max(p[1] for p in profile_pts)))
            v_left = bm.verts.new((-half_w, 0, z))
            v_f = bm.verts.new((0, -ht, z))
            v_right = bm.verts.new((half_w, 0, z))
            v_b = bm.verts.new((0, ht, z))
            rings.append([v_left, v_f, v_right, v_b])
    for i in range(len(rings) - 1):
        r0, r1 = rings[i], rings[i + 1]
        if len(r0) == 4 and len(r1) == 4:
            for k in range(4):
                kn = (k + 1) % 4
                bm.faces.new((r0[k], r0[kn], r1[kn], r1[k]))
        elif len(r0) == 4 and len(r1) == 1:
            tip = r1[0]
            for k in range(4):
                kn = (k + 1) % 4
                bm.faces.new((r0[k], r0[kn], tip))
        elif len(r0) == 1 and len(r1) == 4:
            bot = r0[0]
            for k in range(4):
                kn = (k + 1) % 4
                bm.faces.new((bot, r1[kn], r1[k]))
    if len(rings[0]) == 4:
        bm.faces.new((rings[0][3], rings[0][2], rings[0][1], rings[0][0]))
    if len(rings[-1]) == 4:
        bm.faces.new((rings[-1][0], rings[-1][1], rings[-1][2], rings[-1][3]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    H.link(obj)
    H.assign(obj, mat)
    H.shade_smooth(obj, angle=24)
    return obj


# --------------------------------------------------------------------------- 14 ITEM BUILDERS

def build_short_sword(m):
    parts = []
    # Broad tapering double-edged blade
    blade = make_beveled_blade(
        "Blade",
        [(0.18, 0.088), (0.35, 0.084), (0.95, 0.072), (1.18, 0.048), (1.36, 0.0)],
        half_thick_center=0.036, half_thick_edge=0.004, mat=m["edge_steel"]
    )
    parts.append(blade)
    # Fuller rib highlight bar
    fuller = H.add_box("Fuller", (0.022, 0.048, 0.72), location=(0, 0, 0.56), mat=m["dark_iron"], bevel=0.004)
    parts.append(fuller)
    # Swept bronze/gold crossguard
    guard_pts = [(-0.26, 0, 0.21), (-0.14, 0, 0.16), (0.0, 0, 0.14), (0.14, 0, 0.16), (0.26, 0, 0.21)]
    guard = H.add_tube("Guard", guard_pts, radius=0.028, mat=m["gold"], taper=[0.6, 1.0, 1.15, 1.0, 0.6])
    parts.append(guard)
    parts.append(H.add_box("GuardCenter", (0.14, 0.08, 0.08), location=(0, 0, 0.16), mat=m["brass"], bevel=0.015))
    parts.append(H.add_sphere("GuardGem", 0.028, location=(0, -0.042, 0.16), mat=m["ruby"], segments=20))
    # Wrapped leather grip + gold collars
    parts.append(H.add_cylinder("Grip", 0.034, 0.30, location=(0, 0, -0.01), mat=m["leather"], bevel=0.005))
    for z in (-0.11, -0.04, 0.03, 0.10):
        parts.append(H.add_cylinder(f"Ring{z:.2f}", 0.039, 0.016, location=(0, 0, z), mat=m["brass"]))
    # Faceted heavy pommel
    parts.append(H.add_cylinder("Pommel", 0.072, 0.065, location=(0, 0, -0.20), rotation=(90, 0, 0), mat=m["gold"], vertices=8, bevel=0.010))
    parts.append(H.add_cone("PommelSpike", 0.028, 0.04, radius_top=0.002, location=(0, 0, -0.27), rotation=(180, 0, 0), mat=m["gold"]))
    root = bpy.data.objects.new("Root_ShortSword", None)
    H.link(root)
    for p in parts:
        p.parent = root
    root.rotation_euler = Euler((0, math.radians(-38), math.radians(8)))
    return parts, {"azimuth": 18, "elevation": 14, "margin": 1.10}


def build_heavy_axe(m):
    parts = []
    # Ash wood haft + bottom cap + brass bands
    parts.append(H.add_cylinder("Haft", 0.042, 1.34, location=(0, 0, 0.25), mat=m["wood"], bevel=0.008))
    parts.append(H.add_cylinder("GripWrap", 0.048, 0.36, location=(0, 0, -0.18), mat=m["leather"], bevel=0.006))
    parts.append(H.add_cylinder("ButtCap", 0.060, 0.08, location=(0, 0, -0.43), mat=m["gold"], vertices=8, bevel=0.01))
    parts.append(H.add_cylinder("CollarTop", 0.056, 0.06, location=(0, 0, 0.88), mat=m["brass"], bevel=0.008))
    parts.append(H.add_cone("TopSpike", 0.052, 0.19, radius_top=0.002, location=(0, 0, 1.00), mat=m["edge_steel"], vertices=4))
    # Crescent bearded axe head built with bmesh profile
    bm = bmesh.new()
    outer_arc = [
        (0.06, 0.78), (0.28, 0.84), (0.46, 0.90),
        (0.50, 0.72), (0.48, 0.48), (0.41, 0.27),
        (0.24, 0.36), (0.14, 0.56), (0.06, 0.62)
    ]
    edge_verts_f = []
    edge_verts_b = []
    eye_f = bm.verts.new((0.05, -0.055, 0.70))
    eye_b = bm.verts.new((0.05, 0.055, 0.70))
    for i, (x, z) in enumerate(outer_arc):
        t = 0.012 if 2 <= i <= 5 else 0.036
        vf = bm.verts.new((x, -t, z))
        vb = bm.verts.new((x, t, z))
        edge_verts_f.append(vf)
        edge_verts_b.append(vb)
    for i in range(len(outer_arc) - 1):
        bm.faces.new((edge_verts_f[i], edge_verts_f[i + 1], eye_f))
        bm.faces.new((eye_b, edge_verts_b[i + 1], edge_verts_b[i]))
        bm.faces.new((edge_verts_f[i], edge_verts_b[i], edge_verts_b[i + 1], edge_verts_f[i + 1]))
    bm.faces.new((edge_verts_f[-1], edge_verts_f[0], eye_f))
    bm.faces.new((eye_b, edge_verts_b[0], edge_verts_b[-1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    axemesh = bpy.data.meshes.new("AxeBladeMesh")
    bm.to_mesh(axemesh)
    bm.free()
    axe = bpy.data.objects.new("AxeHead", axemesh)
    H.link(axe)
    H.assign(axe, m["edge_steel"])
    bevel_obj(axe, width=0.008, segments=2)
    parts.append(axe)
    # Dark iron socket collar & brass rivets + back spike
    parts.append(H.add_box("EyeSocket", (0.16, 0.13, 0.18), location=(0.03, 0, 0.69), mat=m["dark_iron"], bevel=0.015))
    parts.append(H.add_cone("BackSpike", 0.042, 0.18, radius_top=0.003, location=(-0.11, 0, 0.70), rotation=(0, -84, 0), mat=m["steel"], vertices=4))
    parts.append(H.add_box("BrassPlate", (0.11, 0.142, 0.08), location=(0.14, 0, 0.69), mat=m["gold"], bevel=0.01))
    root = bpy.data.objects.new("Root_HeavyAxe", None)
    H.link(root)
    for p in parts:
        p.parent = root
    root.rotation_euler = Euler((0, math.radians(-30), math.radians(10)))
    return parts, {"azimuth": 18, "elevation": 14, "margin": 1.12}


def build_dagger(m):
    parts = []
    # Sinister leaf-curved stiletto blade with fuller ridge
    blade = make_beveled_blade(
        "DaggerBlade",
        [(0.14, 0.072), (0.36, 0.084), (0.62, 0.068), (0.86, 0.036), (1.02, 0.0)],
        half_thick_center=0.030, half_thick_edge=0.003, mat=m["edge_steel"]
    )
    parts.append(blade)
    parts.append(H.add_box("DaggerRidge", (0.018, 0.042, 0.56), location=(0, 0, 0.45), mat=m["dark_iron"], bevel=0.004))
    # S-curve dark iron + brass guard
    pts = [(-0.21, 0, 0.10), (-0.11, 0, 0.15), (0.0, 0, 0.14), (0.11, 0, 0.13), (0.21, 0, 0.19)]
    parts.append(H.add_tube("SGuard", pts, radius=0.022, mat=m["brass"], taper=[0.6, 1.0, 1.2, 1.0, 0.6]))
    parts.append(H.add_cylinder("Grip", 0.030, 0.25, location=(0, 0, -0.01), mat=m["dark_leather"], bevel=0.005))
    for z in (-0.09, -0.03, 0.03, 0.09):
        parts.append(H.add_cylinder(f"Ring{z:.2f}", 0.035, 0.012, location=(0, 0, z), mat=m["brass"]))
    parts.append(H.add_cylinder("Pommel", 0.056, 0.055, location=(0, 0, -0.17), rotation=(90, 0, 0), mat=m["dark_iron"], vertices=6, bevel=0.008))
    parts.append(H.add_sphere("PommelGem", 0.022, location=(0, -0.028, -0.17), mat=m["ruby"], segments=16))
    root = bpy.data.objects.new("Root_Dagger", None)
    H.link(root)
    for p in parts:
        p.parent = root
    root.rotation_euler = Euler((0, math.radians(-40), math.radians(10)))
    return parts, {"azimuth": 18, "elevation": 14, "margin": 1.12}


def build_cloth_tunic(m):
    parts = []
    # Flared layered robe/tunic silhouette with natural hanging sleeves and open V-neck stole
    prof = [
        (0.28, -0.45), (0.24, -0.22), (0.175, 0.0), (0.21, 0.24),
        (0.22, 0.36), (0.13, 0.42), (0.01, 0.40)
    ]
    body = H.add_lathe("TunicBody", prof, segments=32, mat=m["cloth_ash"], smooth=True)
    body.scale = (1.18, 0.72, 1.0)
    parts.append(body)
    # Recessed neck shadow void + upright folded collar scarf
    parts.append(H.add_cylinder("NeckVoid", 0.115, 0.06, location=(0, 0, 0.41), mat=m["void_black"], vertices=24))
    collar = H.add_torus("CollarRoll", major=0.14, minor=0.042, location=(0, 0, 0.40), mat=m["cloth_crimson"])
    collar.scale = (1.15, 0.85, 0.9)
    parts.append(collar)
    # Draped hanging cloth sleeves smoothly swept downward along shoulders
    for sx in (-1, 1):
        sl = H.add_tube(
            f"DrapedSleeve{sx}",
            [(sx * 0.16, 0, 0.35), (sx * 0.31, 0.01, 0.26), (sx * 0.38, 0.02, 0.08)],
            radius=0.075, mat=m["cloth_ash"], taper=[0.95, 1.05, 0.85]
        )
        cuff = H.add_cylinder(f"Cuff{sx}", 0.068, 0.032, location=(sx * 0.38, 0.02, 0.07),
                              rotation=(0, sx * 18, 0), mat=m["brass"], vertices=20)
        parts += [sl, cuff]
    # Twin crimson chest stole straps forming V-opening over chest
    for sx in (-1, 1):
        stole = H.add_box(f"ChestStole{sx}", (0.075, 0.04, 0.42), location=(sx * 0.075, -0.135, 0.22),
                          rotation=(8, sx * 16, 0), mat=m["cloth_crimson"], bevel=0.01)
        parts.append(stole)
    # Leather belt + brass buckle + hanging center tabard
    belt = H.add_cylinder("Belt", 0.188, 0.075, location=(0, 0, 0.01), mat=m["leather"], vertices=28, bevel=0.008)
    belt.scale = (1.18, 0.74, 1.0)
    parts.append(belt)
    parts.append(H.add_box("Buckle", (0.088, 0.04, 0.088), location=(0, -0.145, 0.01), mat=m["gold"], bevel=0.012))
    parts.append(H.add_box("CenterTabard", (0.11, 0.028, 0.42), location=(0, -0.148, -0.21), rotation=(10, 0, 0), mat=m["cloth_crimson"], bevel=0.008))
    parts.append(H.add_cylinder("ThroatClasp", 0.038, 0.025, location=(0, -0.155, 0.35), rotation=(80, 0, 0), mat=m["gold"], vertices=8, bevel=0.006))
    return parts, {"azimuth": 16, "elevation": 14, "margin": 1.12}


def build_leather_vest(m):
    parts = []
    # Tailored leather jerkin with open neck cavity & flared collar flaps
    prof = [(0.23, -0.32), (0.185, -0.05), (0.24, 0.24), (0.20, 0.37), (0.12, 0.42), (0.01, 0.39)]
    vest = H.add_lathe("Jerkin", prof, segments=28, mat=m["leather"], smooth=True)
    vest.scale = (1.22, 0.74, 1.0)
    parts.append(vest)
    parts.append(H.add_cylinder("NeckShadow", 0.12, 0.06, location=(0, 0, 0.40), mat=m["void_black"], vertices=24))
    for sx in (-1, 1):
        pad = H.add_sphere(f"Pauldron{sx}", 0.13, location=(sx * 0.25, 0, 0.30), mat=m["dark_leather"], segments=20)
        pad.scale = (1.12, 0.88, 0.56)
        lapel = H.add_box(f"Lapel{sx}", (0.065, 0.03, 0.18), location=(sx * 0.075, -0.148, 0.32),
                          rotation=(12, sx * 22, sx * 10), mat=m["dark_leather"], bevel=0.008)
        parts += [pad, lapel]
    # Crossed chest baldrics + heavy belt + buckle
    b1 = H.add_box("BaldricL", (0.052, 0.30, 0.44), location=(0, -0.01, 0.14), rotation=(0, 32, 0), mat=m["dark_leather"], bevel=0.008)
    b2 = H.add_box("BaldricR", (0.052, 0.30, 0.44), location=(0, -0.01, 0.14), rotation=(0, -32, 0), mat=m["dark_leather"], bevel=0.008)
    parts += [b1, b2]
    belt = H.add_cylinder("WaistBelt", 0.205, 0.09, location=(0, 0, -0.06), mat=m["dark_leather"], vertices=28, bevel=0.008)
    belt.scale = (1.22, 0.76, 1.0)
    parts.append(belt)
    parts.append(H.add_box("BeltBuckle", (0.10, 0.05, 0.095), location=(0, -0.155, -0.06), mat=m["gold"], bevel=0.012))
    for z in (-0.22, -0.14, 0.04, 0.14, 0.24):
        for sx in (-1, 1):
            parts.append(H.add_sphere(f"Rivet{sx}{z:.2f}", 0.014, location=(sx * 0.06, -0.172, z), mat=m["brass"], segments=12))
    return parts, {"azimuth": 16, "elevation": 14, "margin": 1.12}


def build_breastplate(m):
    parts = []
    # Fluted Gothic steel cuirass
    prof = [(0.22, -0.26), (0.19, -0.04), (0.26, 0.24), (0.23, 0.38), (0.12, 0.43), (0.10, 0.43)]
    cuirass = H.add_lathe("Cuirass", prof, segments=32, mat=m["steel"], smooth=True)
    cuirass.scale = (1.24, 0.78, 1.0)
    parts.append(cuirass)
    # Central chest keel ridge
    keel = make_beveled_blade("ChestKeel", [(-0.02, 0.055), (0.22, 0.085), (0.40, 0.03)],
                              half_thick_center=0.05, half_thick_edge=0.01, mat=m["edge_steel"])
    keel.location = (0, -0.165, 0.0)
    parts.append(keel)
    # Raised gold gorget collar
    gorget = H.add_cylinder("Gorget", 0.14, 0.07, location=(0, 0, 0.42), mat=m["gold"], vertices=28, bevel=0.01)
    gorget.scale = (1.1, 0.85, 1.0)
    parts.append(gorget)
    # Layered Gothic pauldrons + gold trim
    for sx in (-1, 1):
        for k in range(2):
            p = H.add_sphere(f"Spaulder{sx}_{k}", 0.15 - k * 0.018, location=(sx * (0.27 + k * 0.05), 0, 0.33 - k * 0.08),
                             mat=m["steel"] if k == 0 else m["dark_iron"], segments=22)
            p.scale = (1.1, 0.9, 0.65)
            parts.append(p)
        rim = H.add_cylinder(f"SpaulderRim{sx}", 0.142, 0.022, location=(sx * 0.27, 0, 0.30),
                             rotation=(0, sx * 35, 0), mat=m["gold"], vertices=24)
        parts.append(rim)
    # Segmented fauld waist plates + center crimson tabard hint
    for k in range(3):
        f = H.add_cylinder(f"Fauld{k}", 0.21 + k * 0.018, 0.068, location=(0, 0, -0.12 - k * 0.07),
                           mat=m["dark_iron"] if k % 2 else m["steel"], vertices=28, bevel=0.008)
        f.scale = (1.22, 0.78, 1.0)
        parts.append(f)
    # Gold chest crest medaillon
    parts.append(H.add_cylinder("ChestCrest", 0.055, 0.025, location=(0, -0.205, 0.23),
                                rotation=(80, 0, 0), mat=m["gold"], vertices=6, bevel=0.008))
    parts.append(H.add_sphere("CrestGem", 0.024, location=(0, -0.220, 0.23), mat=m["ruby"], segments=16))
    return parts, {"azimuth": 18, "elevation": 14, "margin": 1.12}


def build_hood(m):
    parts = []
    # Draped assassin/ranger cowl with pointed Gothic brow overhang & shadowed face recess
    mantle = H.add_cone("Mantle", 0.42, 0.22, radius_top=0.22, location=(0, 0.02, -0.22), mat=m["cloth_hood"], vertices=32)
    mantle.scale = (1.16, 0.94, 1.0)
    bevel_obj(mantle, width=0.012)
    parts.append(mantle)
    # Outer cowl shell sculpted via bmesh: deep horseshoe hood pointed up-forward at brow peak
    bm = bmesh.new()
    slices = 10
    rings = []
    for si in range(slices):
        t = si / (slices - 1)  # 0 = front arch opening, 1 = back tail tip
        y = -0.22 + t * 0.56
        w = 0.20 * math.sin(max(0.06, (1 - t * 0.72)) * math.pi * 0.55) + 0.03 * (1 - t)
        h = 0.28 * (1 - t * 0.65)
        zc = 0.14 + 0.12 * math.sin(t * 2.2) - 0.10 * t
        pts_ring = []
        segs = 18
        for k in range(segs):
            a = -math.pi * 0.35 + (math.pi * 1.70) * k / (segs - 1)
            # Pinch top center (cos~0, sin~1) into sharp Gothic peak near front
            sin_a = math.sin(a)
            cos_a = math.cos(a)
            peak_lift = (0.09 * (1 - t)) if sin_a > 0.5 else 0.0
            rx = cos_a * w * (0.85 if sin_a > 0.6 else 1.0)
            rz = zc + sin_a * h + peak_lift * ((sin_a - 0.5) * 2.0)
            pts_ring.append(bm.verts.new((rx, y, rz)))
        rings.append(pts_ring)
    for si in range(len(rings) - 1):
        r0, r1 = rings[si], rings[si + 1]
        for k in range(len(r0) - 1):
            bm.faces.new((r0[k], r0[k + 1], r1[k + 1], r1[k]))
    # Close tail cap
    tail_v = bm.verts.new((0, 0.42, 0.02))
    for k in range(len(rings[-1]) - 1):
        bm.faces.new((rings[-1][k], rings[-1][k + 1], tail_v))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("HoodShellMesh")
    bm.to_mesh(me)
    bm.free()
    shell = bpy.data.objects.new("HoodShell", me)
    H.link(shell)
    H.assign(shell, m["cloth_hood"])
    H.add_modifier(shell, "SOLIDIFY", thickness=0.032, offset=-1.0)
    H.shade_smooth(shell)
    parts.append(shell)
    # Recessed black shadow inside cowl
    void = H.add_sphere("FaceShadowVoid", 0.145, location=(0, -0.04, 0.12), mat=m["void_black"], segments=24)
    void.scale = (0.85, 0.90, 1.15)
    parts.append(void)
    # Crimson neck scarf & brass clasp
    scarf = H.add_torus("NeckScarf", major=0.19, minor=0.032, location=(0, 0.0, -0.07), mat=m["cloth_crimson"])
    scarf.scale = (1.06, 0.96, 1.0)
    parts.append(scarf)
    parts.append(H.add_cylinder("HoodClasp", 0.044, 0.025, location=(0, -0.22, -0.11),
                                rotation=(72, 0, 0), mat=m["gold"], vertices=8, bevel=0.006))
    return parts, {"azimuth": 24, "elevation": 14, "margin": 1.14}


def build_helm(m):
    parts = []
    # Great gothic sallet / barbute helm
    dome = H.add_sphere("SkullDome", 0.24, location=(0, 0, 0.22), mat=m["steel"], segments=28)
    dome.scale = (0.92, 1.04, 1.02)
    parts.append(dome)
    # Face guard cylinder
    visor = H.add_cylinder("VisorCylinder", 0.225, 0.36, location=(0, -0.01, 0.04), mat=m["steel"], vertices=28, bevel=0.012)
    visor.scale = (0.94, 1.02, 1.0)
    parts.append(visor)
    # Heavy pointed brow plate + central keel crest
    brow = H.add_cylinder("BrowBand", 0.236, 0.075, location=(0, -0.01, 0.18), mat=m["gold"], vertices=28, bevel=0.008)
    brow.scale = (0.95, 1.04, 1.0)
    parts.append(brow)
    crest = make_beveled_blade("HelmCrest", [(-0.02, 0.04), (0.16, 0.06), (0.32, 0.01)],
                               half_thick_center=0.025, half_thick_edge=0.005, mat=m["gold"])
    crest.location = (0, -0.02, 0.26)
    crest.rotation_euler = Euler((math.radians(-18), 0, 0))
    parts.append(crest)
    # T-slit shadow visor & cheek guards
    slit_h = H.add_box("EyeSlit", (0.28, 0.06, 0.048), location=(0, -0.225, 0.12), mat=m["dark_iron"], bevel=0.005)
    slit_v = H.add_box("BreathSlit", (0.042, 0.06, 0.22), location=(0, -0.232, 0.01), mat=m["dark_iron"], bevel=0.005)
    parts += [slit_h, slit_v]
    # Gold nasal bar + glowing visor pinpoints
    parts.append(H.add_box("NasalBar", (0.028, 0.04, 0.16), location=(0, -0.242, 0.13), mat=m["gold"], bevel=0.006))
    # Flared neck guard / bevor
    bevor = H.add_cone("NeckGuard", 0.27, 0.14, radius_top=0.215, location=(0, 0.01, -0.16), mat=m["dark_iron"], vertices=28)
    bevel_obj(bevor, width=0.008)
    parts.append(bevor)
    return parts, {"azimuth": 22, "elevation": 14, "margin": 1.14}


def build_circlet(m):
    parts = []
    # Clean crisp royal gold diadem band with subtle front brow arch + tall gothic spires
    band = H.add_torus("CircletBand", major=0.26, minor=0.024, location=(0, 0, 0), mat=m["gold"], major_segments=48, minor_segments=18)
    band.scale = (1.0, 1.08, 1.0)
    parts.append(band)
    inner = H.add_torus("InnerBand", major=0.245, minor=0.012, location=(0, 0, -0.02), mat=m["brass"], major_segments=48, minor_segments=14)
    inner.scale = (1.0, 1.08, 1.0)
    parts.append(inner)
    r = 0.26
    for ang_deg, h in [(0, 0.18), (-36, 0.11), (36, 0.11), (-72, 0.08), (72, 0.08)]:
        a = math.radians(ang_deg)
        x, y = r * math.sin(a), -r * 1.08 * math.cos(a)
        spire = H.add_cone(f"Spire{ang_deg}", 0.028, h, radius_top=0.002, location=(x, y, h * 0.48 + 0.015), mat=m["gold"], vertices=4)
        parts.append(spire)
    # Center bezel + large faceted deep crimson ruby + amber side gems
    parts.append(H.add_cylinder("CenterBezel", 0.056, 0.028, location=(0, -r * 1.08 - 0.01, 0.04),
                                rotation=(82, 0, 0), mat=m["gold"], vertices=8, bevel=0.006))
    gem = H.add_sphere("CenterRuby", 0.042, location=(0, -r * 1.08 - 0.022, 0.04), mat=m["ruby"], segments=16)
    gem.scale = (0.85, 0.55, 1.15)
    parts.append(gem)
    for sx in (-1, 1):
        a = math.radians(sx * 36)
        gx, gy = (r + 0.012) * math.sin(a), -(r * 1.08 + 0.012) * math.cos(a)
        parts.append(H.add_sphere(f"SideGem{sx}", 0.022, location=(gx, gy, 0.022), mat=m["amber"], segments=14))
    return parts, {"azimuth": 22, "elevation": 26, "margin": 1.16}


def build_lantern(m):
    parts = []
    # Hexagonal wrought-iron lantern with domed chimney and bright amber flame core
    parts.append(H.add_cylinder("BasePlinth", 0.22, 0.07, location=(0, 0, -0.32), mat=m["dark_iron"], vertices=6, bevel=0.012))
    parts.append(H.add_cylinder("BaseGoldRim", 0.19, 0.03, location=(0, 0, -0.27), mat=m["brass"], vertices=6, bevel=0.006))
    parts.append(H.add_cone("RoofCap", 0.24, 0.16, radius_top=0.07, location=(0, 0, 0.30), mat=m["dark_iron"], vertices=6))
    parts.append(H.add_cylinder("Chimney", 0.085, 0.11, location=(0, 0, 0.42), mat=m["brass"], vertices=6, bevel=0.008))
    parts.append(H.add_cone("Finial", 0.07, 0.09, radius_top=0.004, location=(0, 0, 0.52), mat=m["gold"], vertices=6))
    # 6 iron corner cage mullions + crossbars
    for i in range(6):
        a = math.radians(i * 60 + 30)
        x, y = 0.175 * math.cos(a), 0.175 * math.sin(a)
        parts.append(H.add_box(f"Post{i}", (0.026, 0.026, 0.52), location=(x, y, -0.01),
                               rotation=(0, 0, i * 60 + 30), mat=m["dark_iron"], bevel=0.005))
    # Carry bail arch
    bail_pts = [(-0.21, 0, 0.26), (-0.24, 0, 0.48), (0, 0, 0.68), (0.24, 0, 0.48), (0.21, 0, 0.26)]
    parts.append(H.add_tube("BailHandle", bail_pts, radius=0.016, mat=m["gold"]))
    # Warm glowing lantern core flame + glass shell
    core = H.add_cone("FlameCore", 0.085, 0.24, radius_top=0.005, location=(0, 0, -0.04), mat=m["ember_core"], vertices=16)
    parts.append(core)
    parts.append(H.add_sphere("InnerGlow", 0.095, location=(0, 0, -0.11), mat=m["amber"], segments=20))
    glass = H.add_cylinder("GlassCage", 0.15, 0.44, location=(0, 0, -0.02), mat=m["potion_glass"], vertices=6)
    parts.append(glass)

    def lantern_light(center):
        H.add_light("POINT", "LanternInternal", location=(center.x, center.y - 0.05, center.z - 0.05),
                    energy=140.0, light_color="#ff9a38", shadow_soft=0.12)

    return parts, {"azimuth": 22, "elevation": 16, "margin": 1.12, "extra_lights": [lantern_light]}


def build_hooded_lantern(m):
    parts = []
    # Bullseye inquisitor lantern with hinged focusing cowl, heavy brass frame, shutter plates
    parts.append(H.add_cylinder("Plinth", 0.23, 0.08, location=(0, 0, -0.32), mat=m["dark_iron"], vertices=8, bevel=0.012))
    parts.append(H.add_cylinder("BodyBack", 0.20, 0.50, location=(0, 0.04, -0.04), mat=m["dark_iron"], vertices=8, bevel=0.012))
    # Focused conical hood jutting forward towards viewer
    cowl = H.add_cone("FrontCowl", 0.16, 0.22, radius_top=0.21, location=(0, -0.17, -0.01),
                      rotation=(90, 0, 0), mat=m["brass"], vertices=16)
    bevel_obj(cowl, width=0.008)
    parts.append(cowl)
    # Angled upper blinders / inquisitor visor roof
    visor = H.add_box("UpperHood", (0.44, 0.34, 0.10), location=(0, -0.06, 0.24), rotation=(18, 0, 0), mat=m["dark_iron"], bevel=0.016)
    parts.append(visor)
    parts.append(H.add_cylinder("VentStack", 0.10, 0.18, location=(0, 0.04, 0.36), mat=m["brass"], vertices=8, bevel=0.01))
    parts.append(H.add_cone("VentCap", 0.14, 0.10, radius_top=0.04, location=(0, 0.04, 0.48), mat=m["gold"], vertices=8))
    # Glowing bullseye ember lens
    lens = H.add_sphere("EmberLens", 0.14, location=(0, -0.15, -0.01), mat=m["ember_core"], segments=24)
    lens.scale = (1.0, 0.55, 1.0)
    parts.append(lens)
    parts.append(H.add_cylinder("LensBezel", 0.165, 0.035, location=(0, -0.22, -0.01), rotation=(90, 0, 0), mat=m["gold"], vertices=16, bevel=0.006))
    # Rear carry ring
    parts.append(H.add_torus("SideHandle", major=0.08, minor=0.014, location=(0.22, 0.06, 0.02), rotation=(0, 90, 0), mat=m["brass"]))

    def bullseye_light(center):
        H.add_light("POINT", "BullseyeGlow", location=(center.x, center.y - 0.3, center.z),
                    energy=180.0, light_color="#ff8c2a", shadow_soft=0.14)

    return parts, {"azimuth": -24, "elevation": 16, "margin": 1.12, "extra_lights": [bullseye_light]}


def build_ember_censer(m):
    parts = []
    # Ornate hanging gothic brass censer with pierced flame bowl & triple chains
    bowl_prof = [
        (0.01, -0.34), (0.05, -0.26), (0.14, -0.18), (0.21, -0.05),
        (0.22, 0.04), (0.18, 0.08), (0.01, 0.08)
    ]
    bowl = H.add_lathe("CenserBowl", bowl_prof, segments=24, mat=m["brass"], smooth=True)
    parts.append(bowl)
    parts.append(H.add_cone("BottomDrop", 0.06, 0.14, radius_top=0.004, location=(0, 0, -0.40), rotation=(180, 0, 0), mat=m["gold"], vertices=8))
    # Glowing brazier coal belly visible through rim band
    coal = H.add_sphere("EmberCoals", 0.185, location=(0, 0, 0.05), mat=m["ember_core"], segments=20)
    coal.scale = (1.0, 1.0, 0.65)
    parts.append(coal)
    # 6 pierced Gothic cage ribs arching to spire cap
    for i in range(6):
        a = math.radians(i * 60)
        rx, ry = math.cos(a), math.sin(a)
        rib = H.add_tube(
            f"CageRib{i}",
            [(0.20 * rx, 0.20 * ry, 0.02), (0.18 * rx, 0.18 * ry, 0.16), (0.07 * rx, 0.07 * ry, 0.29)],
            radius=0.018, mat=m["dark_iron"]
        )
        parts.append(rib)
    parts.append(H.add_cone("LidCrown", 0.09, 0.14, radius_top=0.01, location=(0, 0, 0.34), mat=m["gold"], vertices=6))
    # Triple suspension chains meeting top ring
    for i in range(3):
        a = math.radians(i * 120 + 30)
        rx, ry = math.cos(a), math.sin(a)
        ch = H.add_tube(
            f"Chain{i}",
            [(0.22 * rx, 0.22 * ry, 0.02), (0.14 * rx, 0.14 * ry, 0.34), (0.0, 0.0, 0.62)],
            radius=0.011, mat=m["gold"]
        )
        parts.append(ch)
    parts.append(H.add_torus("TopRing", major=0.048, minor=0.012, location=(0, 0, 0.66), rotation=(90, 0, 0), mat=m["gold"]))

    def censer_light(center):
        H.add_light("POINT", "CenserGlow", location=(center.x, center.y - 0.06, center.z - 0.05),
                    energy=150.0, light_color="#ff7a20", shadow_soft=0.14)

    return parts, {"azimuth": 22, "elevation": 14, "margin": 1.10, "extra_lights": [censer_light]}


def build_healing_potion(m):
    parts = []
    # Lathed round-bellied apothecary flask + tapered neck + rolled lip
    glass_prof = [
        (0.01, -0.34), (0.18, -0.34), (0.28, -0.22), (0.30, -0.06),
        (0.25, 0.10), (0.11, 0.21), (0.095, 0.34), (0.125, 0.38),
        (0.125, 0.42), (0.08, 0.42), (0.07, 0.32)
    ]
    flask = H.add_lathe("FlaskGlass", glass_prof, segments=32, mat=m["potion_glass"], smooth=True)
    parts.append(flask)
    # Glowing crimson elixir filling 75% of flask
    liq_prof = [(0.01, -0.31), (0.16, -0.31), (0.255, -0.20), (0.275, -0.05), (0.23, 0.07), (0.01, 0.07)]
    liquid = H.add_lathe("RubyElixir", liq_prof, segments=28, mat=m["potion_liquid"], smooth=True)
    parts.append(liquid)
    # Cork stopper + dripping crimson wax seal collar + brass filigree band + parchment tag
    cork = H.add_cone("Cork", 0.078, 0.14, radius_top=0.095, location=(0, 0, 0.46), mat=m["cork"], vertices=20)
    bevel_obj(cork, width=0.008)
    parts.append(cork)
    parts.append(H.add_cylinder("WaxNeck", 0.128, 0.065, location=(0, 0, 0.37), mat=m["wax"], vertices=24, bevel=0.012))
    parts.append(H.add_cylinder("BrassCollar", 0.115, 0.032, location=(0, 0, 0.25), mat=m["gold"], vertices=24, bevel=0.006))
    parts.append(H.add_box("ParchmentTag", (0.14, 0.018, 0.19), location=(0.21, -0.08, 0.11), rotation=(10, -28, 12), mat=m["parchment"], bevel=0.004))

    def potion_light(center):
        H.add_light("POINT", "ElixirGlow", location=(center.x, center.y - 0.12, center.z - 0.12),
                    energy=110.0, light_color="#ff2e3d", shadow_soft=0.14)

    return parts, {"azimuth": 20, "elevation": 18, "margin": 1.12, "extra_lights": [potion_light]}


def build_gold_coin(m):
    parts = []
    # 3 stepped gold sovereign coin stacks + leaning hero embossed coin catching front light
    stacks = [
        ((-0.18, 0.12, -0.14), 5),
        ((0.02, 0.18, -0.14), 7),
        ((0.20, 0.06, -0.14), 3),
    ]
    for si, ((sx, sy, sz), count) in enumerate(stacks):
        for k in range(count):
            c = H.add_cylinder(
                f"Stack{si}_{k}", 0.14, 0.032,
                location=(sx + (k % 2) * 0.006, sy - (k % 3) * 0.004, sz + k * 0.035),
                mat=m["gold_coin"], vertices=28, bevel=0.005
            )
            parts.append(c)
    # Hero standing sovereign coin leaned cleanly at 3/4 towards key light (+Z normal tilts to -Y)
    hero_loc = Vector((0.04, -0.16, 0.02))
    hero_rot = Euler((math.radians(58), math.radians(-8), math.radians(14)))
    hero = H.add_cylinder("HeroCoin", 0.21, 0.042, location=hero_loc, rotation=(58, -8, 14), mat=m["gold_coin"], vertices=32, bevel=0.006)
    parts.append(hero)
    rim_off = hero_rot.to_matrix() @ Vector((0, 0, -0.021))
    rim_front_off = hero_rot.to_matrix() @ Vector((0, 0, 0.021))
    # Add embossed rim & stamped diamond crown on BOTH faces so camera side always shows crisp relief
    for tag, off in [("F", rim_front_off), ("B", rim_off)]:
        rim = H.add_torus(f"HeroRim{tag}", major=0.175, minor=0.013, location=hero_loc + off, rotation=(58, -8, 14), mat=m["brass"])
        emblem = H.add_cylinder(f"CoinEmblem{tag}", 0.095, 0.016, location=hero_loc + off,
                                rotation=(58, -8, 14), mat=m["brass"], vertices=4, bevel=0.004)
        star = H.add_cylinder(f"CoinInner{tag}", 0.055, 0.020, location=hero_loc + off,
                              rotation=(58, 37, 14), mat=m["gold_coin"], vertices=4, bevel=0.003)
        parts += [rim, emblem, star]
    return parts, {"azimuth": 24, "elevation": 32, "margin": 1.15}


ITEMS = [
    ("ShortSword", build_short_sword),
    ("HeavyAxe", build_heavy_axe),
    ("Dagger", build_dagger),
    ("ClothTunic", build_cloth_tunic),
    ("LeatherVest", build_leather_vest),
    ("Breastplate", build_breastplate),
    ("Hood", build_hood),
    ("Helm", build_helm),
    ("Circlet", build_circlet),
    ("Lantern", build_lantern),
    ("HoodedLantern", build_hooded_lantern),
    ("EmberCenser", build_ember_censer),
    ("HealingPotion", build_healing_potion),
    ("GoldCoin", build_gold_coin),
]


def make_contact_sheet(paths, out_path):
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        return
    cols = 5
    rows = math.ceil(len(paths) / cols)
    pad = 10
    cell = 200
    bg = Image.new("RGBA", (cols * (cell + pad) + pad, rows * (cell + pad + 20) + pad), (28, 22, 24, 255))
    draw = ImageDraw.Draw(bg)
    for idx, (name, p) in enumerate(paths):
        r, c = divmod(idx, cols)
        x0 = pad + c * (cell + pad)
        y0 = pad + r * (cell + pad + 20)
        draw.rectangle([x0, y0, x0 + cell, y0 + cell], fill=(42, 32, 30, 255), outline=(140, 108, 60, 255))
        im = Image.open(p).convert("RGBA").resize((cell, cell), Image.Resampling.LANCZOS)
        bg.paste(im, (x0, y0), im)
        draw.text((x0 + 6, y0 + cell + 3), name, fill=(225, 210, 185, 255))
    bg.save(out_path)
    print(f"[build_icons] Wrote preview sheet -> {out_path}")


def main():
    rendered = []
    only = set(x.strip() for x in os.environ.get("CINDERREACH_ONLY", "").split(",") if x.strip())
    for name, fn in ITEMS:
        if only and name not in only:
            continue
        reset_scene()
        mats = make_materials()
        objs, cfg = fn(mats)
        p = render_icon(name, objs, mats, **cfg)
        rendered.append((name, p))
    if PREVIEW_SHEET and rendered:
        make_contact_sheet(rendered, PREVIEW_SHEET)
    print(f"[build_icons] Complete: {len(rendered)} icons.")


if __name__ == "__main__":
    main()
