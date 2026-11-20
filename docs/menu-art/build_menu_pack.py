"""Blender 4.0+ headless renderer for Cinderreach portraits, skill icons, UI crests & startup backdrop.

Run headless from the repo root:
    blender -b --factory-startup --python-exit-code 1 -P docs/menu-art/build_menu_pack.py

Outputs into sandbox/Assets/Resources/MenuArt/:
    Portraits (256x256 RGBA):
        PortraitFenwalker, PortraitLanternbearer, PortraitVendor
    Skill Icons (256x256 RGBA):
        SkillFireball, SkillArrow, SkillCleave,
        SkillLanternBolt, SkillWisps, SkillSearingHalo, SkillVeilStep
    Navigation / Badges (256x256 RGBA):
        CrestCinderreach, WaypointSigil, StashChestIcon, SettingsCog
    Startup Key Art Backdrop (1024x576 RGB):
        StartupBackdrop
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Euler, Vector

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.environ.get("CINDERREACH_OUT_DIR") or os.path.join(REPO_ROOT, "sandbox", "Assets", "Resources", "MenuArt")
ADV_DIR = os.path.join(REPO_ROOT, "sandbox", "Assets", "ThirdParty", "KayKit", "Adventurers")
DUN_DIR = os.path.join(REPO_ROOT, "sandbox", "Assets", "ThirdParty", "KayKit", "Dungeon", "Models")
MOOR_DIR = os.path.join(REPO_ROOT, "sandbox", "Assets", "Art", "BloodMoor")
BPY_HELPERS_DIR = "/home/patrick/.local/share/muse/skills/bundled/muse-core~v1/skills/blender/scripts"
if os.path.isdir(BPY_HELPERS_DIR) and BPY_HELPERS_DIR not in sys.path:
    sys.path.insert(0, BPY_HELPERS_DIR)
import bpy_helpers as H  # noqa: E402

ICON_SIZE = 256
BACKDROP_SIZE = (1024, 576)
SAMPLES = int(os.environ.get("CINDERREACH_PACK_SAMPLES", "84"))
BACKDROP_SAMPLES = int(os.environ.get("CINDERREACH_BACKDROP_SAMPLES", "96"))


def reset_scene(size=(ICON_SIZE, ICON_SIZE), transparent=True, samples=SAMPLES, world_col="#120e16", world_strength=0.35):
    H.reset_scene()
    scene = bpy.context.scene
    scene.render.resolution_x = size[0]
    scene.render.resolution_y = size[1]
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = transparent
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    H.engine_id("CYCLES")
    scene.cycles.samples = samples
    scene.cycles.use_adaptive_sampling = True
    scene.cycles.max_bounces = 6
    if hasattr(scene.cycles, "use_denoising"):
        scene.cycles.use_denoising = H.has_denoiser()
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        scene.view_settings.view_transform = "Filmic"
    scene.view_settings.exposure = 0.15
    H.world_color(world_col, strength=world_strength)


def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    return [o for o in bpy.data.objects if o not in before]


def bind_texture(objs, tex_path, roughness=0.55, metallic=0.14, sat=0.80, val=0.85, tint=None):
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
            tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None)
            if tex is None:
                tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = img
            hsv = nt.nodes.new("ShaderNodeHueSaturation")
            hsv.inputs["Saturation"].default_value = sat
            hsv.inputs["Value"].default_value = val
            nt.links.new(tex.outputs["Color"], hsv.inputs["Color"])
            out_sock = hsv.outputs["Color"]
            if tint:
                mix = nt.nodes.new("ShaderNodeMixRGB")
                mix.blend_type = "MULTIPLY"
                mix.inputs["Fac"].default_value = 0.65
                mix.inputs["Color2"].default_value = H.color(tint)
                nt.links.new(out_sock, mix.inputs["Color1"])
                out_sock = mix.outputs["Color"]
            nt.links.new(out_sock, H.principled_input(bsdf, "base_color"))
            H.set_principled(bsdf, roughness=roughness, metallic=metallic)


def make_materials():
    m = {}
    m["steel"] = H.add_noise_variation(H.material_preset("P_Steel", "steel", base_color="#b8c0cc", roughness=0.22, metallic=0.95), scale=12.0, amount=0.12, roughness_amount=0.05, bump=0.03)
    m["dark_iron"] = H.material_preset("P_Iron", "stone", base_color="#282b32", roughness=0.38, metallic=0.88, scale=10.0)
    m["gold"] = H.add_noise_variation(H.material_preset("P_Gold", "gold", base_color="#dfa842", roughness=0.22, metallic=0.96), scale=12.0, amount=0.10, roughness_amount=0.04, bump=0.03)
    m["brass"] = H.material_preset("P_Brass", "brass", base_color="#a27232", roughness=0.30, metallic=0.92)
    m["wood"] = H.material_preset("P_Wood", "wood", base_color="#482d1c", roughness=0.64, scale=10.0)
    m["hood_cloth"] = H.material_preset("P_Cloth", "fabric", base_color="#241c24", roughness=0.86, scale=12.0)
    m["crimson"] = H.material_preset("P_Crimson", "fabric", base_color="#6e161b", roughness=0.76, scale=12.0)
    m["basalt"] = H.material_preset("P_Basalt", "stone", base_color="#383c44", roughness=0.80, scale=8.0)
    # Zero-specular pure emission flame shaders prevent bright key-light peach washout!
    m["fire_core"] = H.emission_material("P_FireCore", "#ff9618", strength=2.6)
    m["fire_red"] = H.emission_material("P_FireRed", "#d62804", strength=1.8)
    m["crimson_slash"] = H.emission_material("P_Slash", "#e0240c", strength=2.1)
    m["cyan_rune"] = H.emission_material("P_CyanRune", "#28d0f8", strength=2.3)
    m["wisp_gold"] = H.emission_material("P_WispGold", "#ffa628", strength=2.4)
    m["veil_violet"] = H.emission_material("P_VeilViolet", "#7462f0", strength=2.2)
    m["ruby"] = H.material("P_Ruby", "#b40c18", roughness=0.06, metallic=0.1, emission="#940a14", emission_strength=1.2)
    m["charcoal"] = H.material_preset("P_Charcoal", "stone", base_color="#1a1416", roughness=0.88, scale=12.0)
    return m


def pose_character(armature, pose_angles_deg):
    for pb in armature.pose.bones:
        pb.rotation_mode = "XYZ"
    for bname, deg_xyz in pose_angles_deg.items():
        pb = armature.pose.bones.get(bname)
        if pb:
            pb.rotation_euler = Euler([math.radians(a) for a in deg_xyz], "XYZ")
    bpy.context.view_layer.update()


def bone_world_pos(armature, bname, use_tail=False):
    pb = armature.pose.bones[bname]
    pt = pb.tail if use_tail else pb.head
    return armature.matrix_world @ pt


def build_mini_lantern(m, loc, scale=0.14):
    parts = []
    parts.append(H.add_cylinder("MiniLanternBase", 0.22 * scale, 0.08 * scale, location=(loc[0], loc[1], loc[2] - 0.30 * scale), mat=m["dark_iron"], vertices=6))
    parts.append(H.add_cone("MiniLanternCap", 0.24 * scale, 0.16 * scale, radius_top=0.06 * scale, location=(loc[0], loc[1], loc[2] + 0.24 * scale), mat=m["brass"], vertices=6))
    parts.append(H.add_sphere("MiniLanternFlame", 0.12 * scale, location=(loc[0], loc[1], loc[2] - 0.03 * scale), mat=m["fire_core"], segments=16))
    for i in range(4):
        a = math.radians(i * 90 + 45)
        parts.append(H.add_box(f"MiniBar{i}", (0.03 * scale, 0.03 * scale, 0.48 * scale),
                               location=(loc[0] + 0.16 * scale * math.cos(a), loc[1] + 0.16 * scale * math.sin(a), loc[2] - 0.03 * scale),
                               mat=m["dark_iron"]))
    return parts


def portrait_lighting(center):
    c = Vector(center)
    H.add_light("AREA", "P_Key", location=(c.x - 1.8, c.y - 2.2, c.z + 1.1),
                target=center, energy=115.0, size=1.5, light_color="#ffdaaa")
    H.add_light("AREA", "P_Fill", location=(c.x + 2.0, c.y - 1.6, c.z + 0.3),
                target=center, energy=48.0, size=2.0, light_color="#6ea0d8")
    H.add_light("AREA", "P_Rim", location=(c.x + 1.0, c.y + 1.8, c.z + 1.2),
                target=center, energy=210.0, size=1.3, light_color="#ff8a34")


def render_portrait(name, fbx_file, tex_file, pose_deg, azimuth=-18, elevation=8,
                    sat=0.78, val=0.84, tint=None, prop_fn=None, margin=1.08, head_room=0.08):
    m = make_materials()
    objs = import_fbx(os.path.join(ADV_DIR, "Characters", fbx_file))
    bind_texture(objs, os.path.join(ADV_DIR, "Characters", tex_file), roughness=0.58, metallic=0.14, sat=sat, val=val, tint=tint)
    arm = next(o for o in objs if o.type == "ARMATURE")
    pose_character(arm, pose_deg)

    if prop_fn:
        prop_fn(arm, m)

    # Full bust + head + crest portrait framing box so helmets/hats never crop!
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    lo, hi = H.bbox(meshes)
    portrait_z_lo = max(lo.z, 0.72)
    portrait_z_hi = hi.z + head_room
    portrait_center = Vector(((lo.x + hi.x) * 0.5, (lo.y + hi.y) * 0.5, (portrait_z_lo + portrait_z_hi) * 0.5))
    bust_proxy = H.add_box("_BustFrameProxy", (1.28, 0.85, portrait_z_hi - portrait_z_lo),
                           location=portrait_center)
    bust_proxy.hide_render = True
    cam = H.add_camera("PortraitCam", location=(0, -4, 1.3), target=portrait_center, lens=55.0)
    H.frame_objects(cam, [bust_proxy], azimuth=azimuth, elevation=elevation, margin=margin, target=portrait_center)
    portrait_lighting(portrait_center)

    os.makedirs(OUT_DIR, exist_ok=True)
    out_path = os.path.join(OUT_DIR, f"{name}.png")
    bpy.context.scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"[build_menu_pack] Rendered {name} -> {out_path}")
    return out_path


# --------------------------------------------------------------------------- 3 PORTRAITS

def build_portrait_fenwalker():
    pose = {
        "spine": (3, 0, -6),
        "chest": (3, 0, -8),
        "head": (2, 0, 12),
        "upperarm.l": (-66, 0, -14),
        "lowerarm.l": (-12, 0, -28),
        "upperarm.r": (-38, 0, 34),
        "lowerarm.r": (-8, 0, 52),
    }

    def attach_sword(arm, m):
        hand = bone_world_pos(arm, "handslot.r")
        sword_objs = import_fbx(os.path.join(ADV_DIR, "Accessories", "sword_1handed.fbx"))
        bind_texture(sword_objs, os.path.join(ADV_DIR, "Characters", "knight_texture.png"), roughness=0.25, metallic=0.88)
        for o in sword_objs:
            o.location = hand + Vector((0.02, -0.02, -0.02))
            o.rotation_euler = Euler((math.radians(65), math.radians(-16), math.radians(-14)))
            o.scale = (0.85, 0.85, 0.85)
        return sword_objs

    return render_portrait("PortraitFenwalker", "Knight.fbx", "knight_texture.png", pose,
                           azimuth=-22, elevation=8, sat=0.80, val=0.84, prop_fn=attach_sword)


def build_portrait_lanternbearer():
    pose = {
        "spine": (2, 0, 8),
        "chest": (2, 0, 8),
        "head": (3, 0, -10),
        "upperarm.l": (-66, 0, -12),
        "lowerarm.l": (-12, 0, -26),
        "upperarm.r": (-28, 0, 38),
        "lowerarm.r": (-6, 0, 58),
    }

    def attach_lantern(arm, m):
        hand = bone_world_pos(arm, "handslot.r")
        lpos = hand + Vector((0.0, -0.10, -0.04))
        parts = build_mini_lantern(m, lpos, scale=0.42)
        H.add_light("POINT", "LanternHeroGlow", location=(lpos.x - 0.04, lpos.y - 0.14, lpos.z),
                    energy=85.0, light_color="#ff8624", shadow_soft=0.16)
        return parts

    return render_portrait("PortraitLanternbearer", "Mage.fbx", "mage_texture.png", pose,
                           azimuth=20, elevation=6, sat=0.76, val=0.84, prop_fn=attach_lantern)


def build_portrait_vendor():
    pose = {
        "spine": (5, 0, -6),
        "chest": (4, 0, -6),
        "head": (4, -4, 10),
        "upperarm.l": (-64, 0, -20),
        "lowerarm.l": (-18, 0, -45),
        "upperarm.r": (-60, 0, 22),
        "lowerarm.r": (-18, 0, 48),
    }

    def attach_gold(arm, m):
        coins = import_fbx(os.path.join(DUN_DIR, "coin_stack_large.fbx"))
        bind_texture(coins, os.path.join(DUN_DIR, "dungeon_texture.png"), roughness=0.24, metallic=0.92)
        chest = bone_world_pos(arm, "chest")
        for o in coins:
            o.location = (chest.x + 0.34, chest.y - 0.28, chest.z - 0.05)
            o.scale = (0.36, 0.36, 0.36)
        H.add_light("POINT", "MerchantLamp", location=(chest.x + 0.55, chest.y - 0.65, chest.z + 0.30),
                    energy=75.0, light_color="#ffa238", shadow_soft=0.22)
        return coins

    # Tone bright emerald rogue cloak down into weathered dark-fantasy moss-bronze leather!
    return render_portrait("PortraitVendor", "Rogue_Hooded.fbx", "rogue_texture.png", pose,
                           azimuth=-16, elevation=8, sat=0.52, val=0.72, tint="#8a7468", prop_fn=attach_gold)


def build_portrait_rogue():
    pose = {
        "spine": (4, 0, 10),
        "chest": (3, 0, 10),
        "head": (2, 0, -14),
        "upperarm.l": (-60, 0, -22),
        "lowerarm.l": (-22, 0, -40),
        "upperarm.r": (-34, 0, 40),
        "lowerarm.r": (-10, 0, 60),
    }

    def attach_daggers(arm, m):
        objs = []
        for side, rot in (("r", (70, -20, -10)), ("l", (70, 20, 10))):
            hand = bone_world_pos(arm, f"handslot.{side}")
            dag = import_fbx(os.path.join(ADV_DIR, "Accessories", "dagger.fbx"))
            bind_texture(dag, os.path.join(ADV_DIR, "Characters", "rogue_texture.png"), roughness=0.22, metallic=0.9)
            for o in dag:
                o.location = hand + Vector((0.0, -0.03, -0.02))
                o.rotation_euler = Euler([math.radians(a) for a in rot])
                o.scale = (0.9, 0.9, 0.9)
            objs += dag
        chest = bone_world_pos(arm, "chest")
        H.add_light("POINT", "RogueEmber", location=(chest.x - 0.5, chest.y - 0.7, chest.z + 0.2),
                    energy=60.0, light_color="#ff6a2a", shadow_soft=0.2)
        return objs

    # Game rig (Rogue.fbx, not the hooded vendor model) with twin daggers; desaturated like the other heroes.
    return render_portrait("PortraitRogue", "Rogue.fbx", "rogue_texture.png", pose,
                           azimuth=-14, elevation=8, sat=0.66, val=0.76, prop_fn=attach_daggers,
                           margin=1.22, head_room=0.22)


# --------------------------------------------------------------------------- 7 SKILL ICONS & 4 BADGES

def skill_lighting(center):
    c = Vector(center)
    H.add_light("AREA", "S_Key", location=(c.x - 2.2, c.y - 3.0, c.z + 2.4),
                target=center, energy=210.0, size=2.4, light_color="#ffe6c8")
    H.add_light("AREA", "S_Front", location=(c.x + 0.5, c.y - 3.2, c.z + 0.4),
                target=center, energy=85.0, size=3.0, light_color="#f0d8c0")
    H.add_light("AREA", "S_Rim", location=(c.x + 1.2, c.y + 2.8, c.z + 2.0),
                target=center, energy=250.0, size=2.0, light_color="#ffaa55")


def render_icon_objs(name, objs, azimuth=16, elevation=14, margin=1.12, extra_lights=None):
    cam = H.add_camera("SkillCam", location=(0, -4, 1), target=(0, 0, 0), lens=55.0)
    H.frame_objects(cam, objs, azimuth=azimuth, elevation=elevation, margin=margin)
    lo, hi = H.bbox(objs)
    center = (lo + hi) * 0.5
    skill_lighting(center)
    if extra_lights:
        for fn in extra_lights:
            fn(center)
    os.makedirs(OUT_DIR, exist_ok=True)
    out_path = os.path.join(OUT_DIR, f"{name}.png")
    bpy.context.scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"[build_menu_pack] Rendered {name} -> {out_path}")
    return out_path


def build_skill_fireball():
    m = make_materials()
    parts = []
    # Volcanic magma-veined meteor core blasting diagonal fire plumes through charred brass seal
    ring = H.add_torus("SigilRing", major=0.38, minor=0.026, location=(0, 0.06, 0), rotation=(90, 0, 0), mat=m["brass"])
    parts.append(ring)
    # Molten magma sphere: dark basalt crust outside + exposed glowing lava belly + jagged ember tongues
    core = H.add_sphere("FireCore", 0.21, location=(-0.06, -0.02, -0.06), mat=m["fire_red"], segments=24)
    inner = H.add_sphere("SolarCore", 0.16, location=(-0.09, -0.06, -0.08), mat=m["fire_core"], segments=20)
    parts += [core, inner]
    for idx, (loc, sc) in enumerate([
        ((-0.14, -0.14, 0.04), (0.11, 0.09, 0.10)),
        ((0.00, -0.16, -0.12), (0.10, 0.08, 0.08)),
        ((-0.16, -0.09, -0.13), (0.09, 0.08, 0.09)),
        ((0.06, -0.12, 0.02), (0.07, 0.07, 0.08)),
    ]):
        slag = H.add_sphere(f"Slag{idx}", 1.0, location=loc, scale=sc, mat=m["charcoal"], segments=8)
        parts.append(slag)
    trails = [
        [(-0.08, 0.0, 0.10), (0.10, 0.02, 0.24), (0.34, 0.04, 0.36)],
        [(0.04, 0.0, 0.02), (0.24, 0.02, 0.16), (0.44, 0.05, 0.22)],
        [(0.02, 0.02, -0.14), (0.22, 0.04, -0.02), (0.38, 0.06, 0.12)],
        [(-0.14, 0.01, 0.0), (-0.02, 0.04, 0.22), (0.18, 0.05, 0.42)],
    ]
    for i, pts in enumerate(trails):
        parts.append(H.add_tube(f"FlameJet{i}", pts, radius=0.058, mat=m["fire_red"] if i % 2 else m["fire_core"],
                                taper=[1.05, 0.72, 0.08]))
    return render_icon_objs("SkillFireball", parts, azimuth=14, elevation=10, margin=1.12)


def build_skill_arrow():
    m = make_materials()
    parts = []
    # Bow arc on lower-left + diagonal steel arrow AND aligned spectral trail all along +45 deg!
    bow_pts = [(-0.22, 0.04, -0.34), (-0.34, 0.02, -0.06), (-0.28, 0.0, 0.22), (-0.06, 0.02, 0.38)]
    parts.append(H.add_tube("BowArc", bow_pts, radius=0.028, mat=m["wood"], taper=[0.5, 1.0, 1.0, 0.5]))
    parts.append(H.add_tube("BowString", [(-0.22, 0.04, -0.34), (-0.28, -0.02, -0.08), (-0.06, 0.02, 0.38)], radius=0.008, mat=m["brass"]))
    shaft = H.add_cylinder("ArrowShaft", 0.022, 0.90, location=(0.04, -0.02, 0.02), rotation=(0, 45, 0), mat=m["gold"])
    tip = H.add_cone("ArrowHead", 0.095, 0.25, radius_top=0.002, location=(0.38, -0.02, 0.36), rotation=(0, 45, 0), mat=m["steel"], vertices=4)
    tip.scale = (0.7, 0.45, 1.0)
    parts += [shaft, tip]
    for d in (-0.035, 0.035):
        f = H.add_box(f"Fletch{d:.3f}", (0.14, 0.015, 0.065), location=(-0.26, -0.02, -0.28 + d),
                      rotation=(0, 45, 0), mat=m["crimson"], bevel=0.004)
        parts.append(f)
    wake = H.add_tube("WindWake", [(-0.16, 0.03, -0.18), (0.10, 0.02, 0.08), (0.34, 0.01, 0.32)],
                      radius=0.036, mat=m["cyan_rune"], taper=[0.1, 0.65, 0.95])
    parts.append(wake)
    return render_icon_objs("SkillArrow", parts, azimuth=12, elevation=10, margin=1.14)


def build_skill_cleave():
    m = make_materials()
    parts = []
    # Smooth tapered crimson-gold crescent slash ribbon curving clean across dark axe/sword strike
    bm = bmesh.new()
    steps = 28
    inner_verts, outer_verts = [], []
    for i in range(steps):
        t = i / (steps - 1)
        a = math.radians(-130 + t * 145)
        w = 0.22 * (math.sin(t * math.pi) ** 0.75)
        r_mid = 0.34
        r_in = r_mid - w * 0.45
        r_out = r_mid + w * 0.55
        vi = bm.verts.new((r_in * math.sin(a), 0.02, r_in * math.cos(a)))
        vo = bm.verts.new((r_out * math.sin(a), -0.04, r_out * math.cos(a)))
        inner_verts.append(vi)
        outer_verts.append(vo)
    for i in range(steps - 1):
        bm.faces.new((inner_verts[i], outer_verts[i], outer_verts[i + 1], inner_verts[i + 1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("SlashWaveMesh")
    bm.to_mesh(me)
    bm.free()
    wave = bpy.data.objects.new("SlashWave", me)
    H.link(wave)
    H.assign(wave, m["crimson_slash"])
    H.add_modifier(wave, "SOLIDIFY", thickness=0.018, offset=0.0)
    H.shade_smooth(wave)
    parts.append(wave)
    core_arc = H.add_tube("SlashCoreArc", [
        (-0.28, -0.05, 0.22), (-0.02, -0.06, 0.38), (0.30, -0.06, 0.16), (0.30, -0.04, -0.22)
    ], radius=0.028, mat=m["fire_core"], taper=[0.05, 0.85, 1.0, 0.1])
    # Pointed beveled steel blade slicing ahead of the fiery wake
    blade = H.add_cone("StrikeBlade", 0.095, 0.70, radius_top=0.008, location=(0.04, 0.05, 0.04), rotation=(8, 42, 0), mat=m["steel"], vertices=4)
    blade.scale = (0.75, 0.32, 1.0)
    hilt = H.add_cylinder("StrikeHilt", 0.026, 0.24, location=(-0.22, 0.05, -0.22), rotation=(8, 42, 0), mat=m["gold"])
    parts += [core_arc, blade, hilt]
    for i, loc in enumerate([(0.39, -0.08, 0.02), (0.28, -0.08, -0.30), (0.14, -0.08, 0.34)]):
        parts.append(H.add_sphere(f"Spark{i}", 0.022, location=loc, mat=m["wisp_gold"], segments=12))
    return render_icon_objs("SkillCleave", parts, azimuth=14, elevation=10, margin=1.14)


def build_skill_lantern_bolt():
    m = make_materials()
    parts = []
    parts += build_mini_lantern(m, (-0.24, 0.02, -0.24), scale=0.60)
    beam = H.add_cone("RadiantBolt", 0.10, 0.68, radius_top=0.014, location=(0.14, -0.03, 0.14),
                      rotation=(0, 45, 0), mat=m["fire_core"], vertices=16)
    outer_beam = H.add_cone("OuterBolt", 0.13, 0.56, radius_top=0.02, location=(0.11, 0.0, 0.11),
                            rotation=(0, 45, 0), mat=m["fire_red"], vertices=16)
    ring1 = H.add_torus("ShockRing1", major=0.14, minor=0.015, location=(0.02, -0.03, 0.02), rotation=(0, 45, 0), mat=m["gold"])
    ring2 = H.add_torus("ShockRing2", major=0.09, minor=0.012, location=(0.20, -0.03, 0.20), rotation=(0, 45, 0), mat=m["wisp_gold"])
    parts += [beam, outer_beam, ring1, ring2]
    return render_icon_objs("SkillLanternBolt", parts, azimuth=14, elevation=12, margin=1.14)


def build_skill_wisps():
    m = make_materials()
    parts = []
    # Ornate dark-iron brazier bowl with 3 sinuous layered luminous teal/gold/ember wisps
    ped = H.add_cylinder("BrazierPedestal", 0.16, 0.09, location=(0, 0, -0.34), mat=m["dark_iron"], vertices=6, bevel=0.01)
    rim = H.add_cylinder("BrazierRim", 0.18, 0.03, location=(0, 0, -0.29), mat=m["brass"], vertices=6, bevel=0.006)
    parts += [ped, rim]
    wisps = [
        ([(0.0, 0.0, -0.26), (-0.06, -0.04, -0.04), (0.04, -0.06, 0.16), (-0.02, -0.04, 0.34)], m["wisp_gold"], 0.056),
        ([(-0.08, 0.02, -0.26), (-0.25, -0.02, -0.06), (-0.18, -0.05, 0.12), (-0.26, -0.03, 0.25)], m["cyan_rune"], 0.048),
        ([(0.08, 0.02, -0.26), (0.25, -0.02, -0.08), (0.18, -0.05, 0.10), (0.24, -0.03, 0.24)], m["fire_red"], 0.050),
    ]
    for i, (pts, mat, rad) in enumerate(wisps):
        parts.append(H.add_tube(f"WispRibbon{i}", pts, radius=rad, mat=mat, taper=[0.25, 0.95, 0.65, 0.06]))
        parts.append(H.add_sphere(f"WispOrb{i}", rad * 0.95, location=pts[1], mat=mat, segments=18))
    return render_icon_objs("SkillWisps", parts, azimuth=16, elevation=12, margin=1.14)


def build_skill_searing_halo():
    m = make_materials()
    parts = []
    # Forged dark-iron spiked solar corona ring ablaze with ember core flames
    iron_ring = H.add_torus("HaloIron", major=0.30, minor=0.038, location=(0, 0.02, 0), rotation=(76, 0, 0), mat=m["dark_iron"])
    fire_ring = H.add_torus("HaloFire", major=0.31, minor=0.032, location=(0, -0.02, 0), rotation=(76, 0, 0), mat=m["fire_red"])
    gold_ring = H.add_torus("HaloGold", major=0.22, minor=0.020, location=(0, -0.03, 0), rotation=(76, 0, 0), mat=m["gold"])
    parts += [iron_ring, fire_ring, gold_ring]
    for i in range(10):
        a = math.radians(i * 36)
        x = 0.40 * math.sin(a)
        z = 0.40 * math.cos(a) * math.cos(math.radians(14))
        y = 0.40 * math.cos(a) * math.sin(math.radians(14))
        sp = H.add_cone(f"HaloSpike{i}", 0.038, 0.16, radius_top=0.002, location=(x, y, z),
                        rotation=(14, i * 36, 0), mat=m["fire_core"] if i % 2 else m["gold"], vertices=4)
        parts.append(sp)
    parts.append(H.add_sphere("SolarHeart", 0.095, location=(0, -0.02, 0), mat=m["fire_core"], segments=20))
    return render_icon_objs("SkillSearingHalo", parts, azimuth=14, elevation=12, margin=1.14)


def build_skill_veil_step():
    m = make_materials()
    parts = []
    # Hooded dash silhouette built cleanly (cowl + shoulder mantle + billowed violet shadow ribbons)
    mantle = H.add_cone("VeilMantle", 0.22, 0.24, radius_top=0.11, location=(0.14, 0, -0.10), rotation=(0, 24, 0), mat=m["hood_cloth"], vertices=20)
    cowl = H.add_cone("VeilCowl", 0.14, 0.22, radius_top=0.02, location=(0.24, 0, 0.14), rotation=(-15, 38, 0), mat=m["hood_cloth"], vertices=20)
    portal = H.add_torus("PhaseRing", major=0.34, minor=0.022, location=(-0.05, 0.06, 0.02), rotation=(78, 12, 0), mat=m["veil_violet"])
    parts += [mantle, cowl, portal]
    swirls = [
        [(-0.42, 0.02, -0.14), (-0.12, -0.06, -0.06), (0.22, -0.08, 0.02)],
        [(-0.40, 0.04, 0.10), (-0.10, -0.05, 0.16), (0.20, -0.06, 0.22)],
        [(-0.36, 0.0, -0.28), (0.02, -0.04, -0.22), (0.30, -0.02, -0.14)],
    ]
    for i, pts in enumerate(swirls):
        parts.append(H.add_tube(f"VeilStreak{i}", pts, radius=0.036, mat=m["veil_violet"] if i != 1 else m["cyan_rune"],
                                taper=[0.1, 0.9, 0.3]))
    return render_icon_objs("SkillVeilStep", parts, azimuth=16, elevation=10, margin=1.14)


def build_skill_quick_shot():
    m = make_materials()
    parts = []
    # Three parallel arrows fanned flat, each with a short cyan speed wake: "many, fast".
    for i, off in enumerate((-0.16, 0.0, 0.16)):
        z = off
        shaft = H.add_cylinder(f"QShaft{i}", 0.016, 0.62, location=(0.02, -0.02 + i * 0.01, z),
                               rotation=(0, 90, 0), mat=m["gold"])
        tip = H.add_cone(f"QTip{i}", 0.07, 0.18, radius_top=0.002, location=(0.41, -0.02 + i * 0.01, z),
                         rotation=(0, 90, 0), mat=m["steel"], vertices=4)
        tip.scale = (0.45, 0.7, 1.0)
        fl = H.add_box(f"QFletch{i}", (0.12, 0.012, 0.05), location=(-0.26, -0.02, z), mat=m["crimson"], bevel=0.004)
        wake = H.add_tube(f"QWake{i}", [(-0.46, 0.02, z), (-0.30, 0.01, z), (-0.12, 0.0, z)],
                          radius=0.03, mat=m["cyan_rune"], taper=[0.05, 0.5, 0.9])
        parts += [shaft, tip, fl, wake]
    ring = H.add_torus("QRing", major=0.40, minor=0.016, location=(0, 0.08, 0), rotation=(90, 0, 0), mat=m["brass"])
    parts.append(ring)
    return render_icon_objs("SkillQuickShot", parts, azimuth=0, elevation=4, margin=1.10)


def build_skill_blade_flurry():
    m = make_materials()
    parts = []
    # Ring of four daggers spinning around a crimson vortex.
    for i in range(4):
        a = i * 90 + 20
        r = 0.26
        x, z = r * math.cos(math.radians(a)), r * math.sin(math.radians(a))
        blade = H.add_cone(f"FBlade{i}", 0.06, 0.34, radius_top=0.004, location=(x, -0.02, z),
                           rotation=(0, -a + 180, 0), mat=m["steel"], vertices=4)
        blade.scale = (0.8, 0.3, 1.0)
        parts.append(blade)
    for i in range(3):
        a0 = i * 120
        pts = [(0.36 * math.cos(math.radians(a0 + d)), -0.05, 0.36 * math.sin(math.radians(a0 + d))) for d in (0, 35, 70)]
        parts.append(H.add_tube(f"FSwirl{i}", pts, radius=0.03, mat=m["crimson_slash"], taper=[0.05, 0.9, 0.1]))
    parts.append(H.add_sphere("FCore", 0.07, location=(0, -0.02, 0), mat=m["fire_red"], segments=16))
    return render_icon_objs("SkillBladeFlurry", parts, azimuth=0, elevation=4, margin=1.12)


def build_skill_seismic_slam():
    m = make_materials()
    parts = []
    # Hammer-fist impact: cracked basalt slab, shockwave rings, ember fissures.
    slab = H.add_cylinder("SSlab", 0.40, 0.08, location=(0, 0, -0.22), mat=m["basalt"], vertices=7, bevel=0.01)
    parts.append(slab)
    for i, (maj, mat) in enumerate(((0.30, m["fire_red"]), (0.42, m["gold"]))):
        parts.append(H.add_torus(f"SWave{i}", major=maj, minor=0.016 + 0.006 * i, location=(0, 0, -0.16 + 0.01 * i), rotation=(0, 0, 0), mat=mat))
    for i in range(5):
        a = math.radians(i * 72 + 10)
        pts = [(0.05 * math.cos(a), 0.05 * math.sin(a), -0.17), (0.20 * math.cos(a + 0.2), 0.20 * math.sin(a + 0.2), -0.17), (0.36 * math.cos(a), 0.36 * math.sin(a), -0.17)]
        parts.append(H.add_tube(f"SCrack{i}", pts, radius=0.016, mat=m["fire_core"], taper=[1.0, 0.7, 0.1]))
    head = H.add_box("SHammerHead", (0.30, 0.18, 0.16), location=(0, 0, 0.06), mat=m["dark_iron"], bevel=0.02)
    handle = H.add_cylinder("SHandle", 0.028, 0.46, location=(0, 0, 0.34), mat=m["wood"])
    band = H.add_cylinder("SBand", 0.034, 0.05, location=(0, 0, 0.16), mat=m["brass"])
    parts += [head, handle, band]
    return render_icon_objs("SkillSeismicSlam", parts, azimuth=18, elevation=22, margin=1.12)


def build_skill_mending_light():
    m = make_materials()
    parts = []
    # Cupped golden light: a warm cross-shaped beacon above two curved brass wards.
    v = H.add_box("MCrossV", (0.08, 0.06, 0.40), location=(0, -0.02, 0.08), mat=m["wisp_gold"], bevel=0.02)
    h = H.add_box("MCrossH", (0.30, 0.06, 0.08), location=(0, -0.02, 0.14), mat=m["wisp_gold"], bevel=0.02)
    parts += [v, h]
    for s in (-1, 1):
        pts = [(0.34 * s, 0.0, 0.22), (0.38 * s, 0.0, -0.06), (0.16 * s, 0.0, -0.30)]
        parts.append(H.add_tube(f"MWard{s}", pts, radius=0.034, mat=m["brass"], taper=[0.3, 1.0, 0.6]))
    for i, loc in enumerate([(-0.22, -0.06, 0.34), (0.24, -0.06, 0.30), (0.0, -0.06, -0.24), (-0.12, -0.06, -0.10)]):
        parts.append(H.add_sphere(f"MMote{i}", 0.03, location=loc, mat=m["fire_core"], segments=12))
    return render_icon_objs("SkillMendingLight", parts, azimuth=0, elevation=4, margin=1.12)


def build_skill_renewing_circle():
    m = make_materials()
    parts = []
    # Ground rune circle seen from above-ish: brass ring, inner gold ring, rising light pillars.
    parts.append(H.add_torus("RRingOuter", major=0.38, minor=0.024, location=(0, 0, -0.10), mat=m["brass"]))
    parts.append(H.add_torus("RRingInner", major=0.26, minor=0.018, location=(0, 0, -0.10), mat=m["wisp_gold"]))
    for i in range(6):
        a = math.radians(i * 60)
        parts.append(H.add_box(f"RRune{i}", (0.06, 0.06, 0.02), location=(0.32 * math.cos(a), 0.32 * math.sin(a), -0.09),
                               rotation=(0, 0, i * 60 + 45), mat=m["cyan_rune"]))
    for i in range(3):
        a = math.radians(i * 120 + 30)
        parts.append(H.add_cylinder(f"RPillar{i}", 0.022, 0.30 + 0.06 * i, location=(0.14 * math.cos(a), 0.14 * math.sin(a), 0.06 + 0.03 * i),
                                    mat=m["fire_core"]))
    return render_icon_objs("SkillRenewingCircle", parts, azimuth=16, elevation=34, margin=1.12)


# --------------------------------------------------------------------------- 4 NAVIGATION / CREST / SETTINGS ICONS

def build_crest_cinderreach():
    m = make_materials()
    parts = []
    shield_pts = [(0.0, 0.34), (0.30, 0.28), (0.28, -0.04), (0.16, -0.26), (0.0, -0.40), (-0.16, -0.26), (-0.28, -0.04), (-0.30, 0.28)]
    plate = H.add_extrusion("Escutcheon", shield_pts, depth=0.07, location=(0, 0.04, 0), rotation=(90, 0, 0), mat=m["dark_iron"], bevel=0.014)
    gold_border = H.add_tube("CrestRim", [(x * 1.03, -0.01, z * 1.03) for x, z in shield_pts], radius=0.024, closed=True, mat=m["gold"])
    parts += [plate, gold_border]
    for sx in (-1, 1):
        sw = H.add_box(f"CrestSword{sx}", (0.055, 0.022, 0.98), location=(0, 0.09, 0.02), rotation=(0, sx * 34, 0), mat=m["steel"], bevel=0.005)
        parts.append(sw)
    parts += build_mini_lantern(m, (0, -0.05, 0.02), scale=0.48)
    gem = H.add_sphere("CrestRuby", 0.058, location=(0, -0.07, -0.20), mat=m["ruby"], segments=18)
    gem.scale = (0.85, 0.55, 1.2)
    parts.append(gem)
    for dx, h in [(0.0, 0.14), (-0.14, 0.10), (0.14, 0.10)]:
        parts.append(H.add_cone(f"Crown{dx:.2f}", 0.036, h, radius_top=0.002, location=(dx, 0.01, 0.37 + h * 0.4), mat=m["gold"], vertices=4))
    return render_icon_objs("CrestCinderreach", parts, azimuth=16, elevation=12, margin=1.12)


def build_waypoint_sigil():
    m = make_materials()
    parts = []
    plinth = H.add_cylinder("Plinth", 0.34, 0.09, location=(0, 0, -0.34), mat=m["basalt"], vertices=8, bevel=0.014)
    arch = H.add_torus("PortalStone", major=0.30, minor=0.068, location=(0, 0, 0.04), rotation=(90, 0, 0), mat=m["basalt"], major_segments=12, minor_segments=12)
    rune_outer = H.add_torus("RuneOuter", major=0.22, minor=0.018, location=(0, -0.03, 0.04), rotation=(90, 0, 0), mat=m["cyan_rune"])
    rune_inner = H.add_torus("RuneInner", major=0.13, minor=0.014, location=(0, -0.04, 0.04), rotation=(90, 0, 0), mat=m["gold"])
    core = H.add_sphere("SigilCore", 0.065, location=(0, -0.03, 0.04), mat=m["cyan_rune"], segments=20)
    parts += [plinth, arch, rune_outer, rune_inner, core]
    for i in range(4):
        a = math.radians(i * 90)
        parts.append(H.add_box(f"Keystone{i}", (0.08, 0.15, 0.08), location=(0.30 * math.sin(a), 0, 0.04 + 0.30 * math.cos(a)),
                               rotation=(0, i * 90, 0), mat=m["gold"], bevel=0.01))
    return render_icon_objs("WaypointSigil", parts, azimuth=22, elevation=16, margin=1.12)


def build_stash_chest_icon():
    m = make_materials()
    objs = import_fbx(os.path.join(DUN_DIR, "chest_gold.fbx"))
    coins = import_fbx(os.path.join(DUN_DIR, "coin_stack_large.fbx"))
    bind_texture(objs + coins, os.path.join(DUN_DIR, "dungeon_texture.png"), roughness=0.40, metallic=0.30, sat=0.88, val=0.72, tint="#9c7c66")
    lo, hi = H.bbox([o for o in objs if o.type == "MESH"])
    span = hi.x - lo.x
    for c in coins:
        c.location = (hi.x + span * 0.14, lo.y + span * 0.15, lo.z)
        c.scale = (0.7, 0.7, 0.7)
    all_meshes = [o for o in (objs + coins) if o.type == "MESH"]
    return render_icon_objs("StashChestIcon", all_meshes, azimuth=30, elevation=22, margin=1.14)


def build_settings_cog():
    m = make_materials()
    parts = []
    parts.append(H.add_torus("CogRim", major=0.28, minor=0.055, location=(0, 0, 0), rotation=(90, 0, 0), mat=m["brass"], major_segments=32))
    parts.append(H.add_torus("InnerGoldRing", major=0.16, minor=0.025, location=(0, -0.02, 0), rotation=(90, 0, 0), mat=m["gold"], major_segments=28))
    parts.append(H.add_cylinder("Hub", 0.095, 0.11, location=(0, 0, 0), rotation=(90, 0, 0), mat=m["dark_iron"], vertices=16, bevel=0.01))
    parts.append(H.add_sphere("HubRuby", 0.048, location=(0, -0.055, 0), mat=m["ruby"], segments=18))
    for i in range(8):
        a = math.radians(i * 45)
        tx, tz = 0.37 * math.sin(a), 0.37 * math.cos(a)
        tooth = H.add_box(f"Tooth{i}", (0.10, 0.09, 0.11), location=(tx, 0, tz), rotation=(0, i * 45, 0), mat=m["gold"], bevel=0.012)
        spoke = H.add_box(f"Spoke{i}", (0.04, 0.045, 0.20), location=(0.14 * math.sin(a), 0, 0.14 * math.cos(a)),
                          rotation=(0, i * 45, 0), mat=m["brass"], bevel=0.008)
        parts += [tooth, spoke]
    return render_icon_objs("SettingsCog", parts, azimuth=22, elevation=18, margin=1.14)


# --------------------------------------------------------------------------- STARTUP BACKDROP SCENE (1024x576)

def build_startup_backdrop():
    reset_scene(size=BACKDROP_SIZE, transparent=False, samples=BACKDROP_SAMPLES, world_col="#121826", world_strength=0.45)
    m = make_materials()

    # Ground plane textured with MoorGround.png
    ground_mat = H.material("SanctuaryGround", "#2c282a", roughness=0.88)
    moor_tex = os.path.join(MOOR_DIR, "Textures", "MoorGround.png")
    if os.path.exists(moor_tex):
        nt = ground_mat.node_tree
        bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        tc = nt.nodes.new("ShaderNodeTexCoord")
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (7.0, 7.0, 1.0)
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(moor_tex, check_existing=True)
        mix = nt.nodes.new("ShaderNodeMixRGB")
        mix.blend_type = "MULTIPLY"
        mix.inputs["Fac"].default_value = 1.0
        mix.inputs["Color2"].default_value = H.color("#544d52")
        nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])
        nt.links.new(mp.outputs["Vector"], tex.inputs["Vector"])
        nt.links.new(tex.outputs["Color"], mix.inputs["Color1"])
        nt.links.new(mix.outputs["Color"], H.principled_input(bsdf, "base_color"))
    H.add_plane("Ground", (26, 26), location=(0, 0, 0), mat=ground_mat)

    # Central sanctuary campfire brazier (right of center x = +1.55 so left-side UI panels have calm dark stone/fog backdrop!)
    brazier_pos = Vector((1.55, 0.35, 0.0))
    if os.path.exists(os.path.join(MOOR_DIR, "Models", "Brazier.fbx")):
        br_objs = import_fbx(os.path.join(MOOR_DIR, "Models", "Brazier.fbx"))
        for o in br_objs:
            o.location = brazier_pos
            o.scale = (0.78, 0.78, 0.78)
            for mat in getattr(o.data, "materials", []):
                if mat and mat.use_nodes:
                    bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
                    if bsdf:
                        H.set_principled(bsdf, base_color="#262022", roughness=0.58, metallic=0.65)
    # Dark glowing ember coals & organic twisting S-curve flames inside brazier bowl
    bx, by = brazier_pos.x, brazier_pos.y
    H.add_sphere("EmberCoalL", 0.14, location=(bx - 0.07, by, 0.42), scale=(1.1, 1.0, 0.55), mat=m["fire_red"])
    H.add_sphere("EmberCoalR", 0.13, location=(bx + 0.08, by - 0.03, 0.42), scale=(1.0, 1.1, 0.55), mat=m["charcoal"])
    flames = [
        ([(bx, by, 0.40), (bx - 0.05, by - 0.02, 0.65), (bx + 0.04, by + 0.02, 0.94)], m["fire_red"], 0.095),
        ([(bx - 0.08, by + 0.03, 0.40), (bx - 0.15, by, 0.58), (bx - 0.08, by - 0.02, 0.78)], m["fire_red"], 0.065),
        ([(bx + 0.06, by - 0.03, 0.40), (bx + 0.10, by - 0.02, 0.56), (bx + 0.04, by + 0.02, 0.76)], m["fire_core"], 0.065),
        ([(bx, by - 0.02, 0.41), (bx + 0.03, by - 0.03, 0.55), (bx - 0.02, by, 0.72)], m["fire_core"], 0.055),
    ]
    for idx, (pts, fmat, rad) in enumerate(flames):
        H.add_tube(f"HearthFlame{idx}", pts, radius=rad, mat=fmat, taper=[0.95, 0.72, 0.05])

    # Fenwalker (Knight) on RIGHT of hearth with relaxed natural sword pose!
    knight_objs = import_fbx(os.path.join(ADV_DIR, "Characters", "Knight.fbx"))
    bind_texture(knight_objs, os.path.join(ADV_DIR, "Characters", "knight_texture.png"), roughness=0.52, metallic=0.22, sat=0.82, val=0.84)
    k_arm = next(o for o in knight_objs if o.type == "ARMATURE")
    k_arm.location = (2.68, 0.65, 0.0)
    k_arm.rotation_euler = Euler((math.radians(90), 0, math.radians(-30)))
    pose_character(k_arm, {
        "spine": (3, 0, 4),
        "head": (2, 0, -14),
        "upperarm.l": (-66, 0, -12),
        "lowerarm.l": (-12, 0, -28),
        "upperarm.r": (-52, 0, 26),
        "lowerarm.r": (-10, 0, 42),
    })
    hand_k = bone_world_pos(k_arm, "handslot.r")
    sw_objs = import_fbx(os.path.join(ADV_DIR, "Accessories", "sword_1handed.fbx"))
    bind_texture(sw_objs, os.path.join(ADV_DIR, "Characters", "knight_texture.png"), roughness=0.25, metallic=0.88)
    for o in sw_objs:
        o.location = hand_k + Vector((0.0, -0.02, -0.02))
        o.rotation_euler = Euler((math.radians(-68), math.radians(15), math.radians(-10)))
        o.scale = (0.9, 0.9, 0.9)

    # Lanternbearer (Mage) mid-right standing guard facing sanctuary fire with held glowing lantern
    mage_objs = import_fbx(os.path.join(ADV_DIR, "Characters", "Mage.fbx"))
    bind_texture(mage_objs, os.path.join(ADV_DIR, "Characters", "mage_texture.png"), roughness=0.62, metallic=0.10, sat=0.80, val=0.82)
    m_arm = next(o for o in mage_objs if o.type == "ARMATURE")
    m_arm.location = (0.45, 1.15, 0.0)
    m_arm.rotation_euler = Euler((math.radians(90), 0, math.radians(26)))
    pose_character(m_arm, {
        "spine": (2, 0, -6),
        "head": (2, 0, 12),
        "upperarm.l": (-66, 0, -12),
        "lowerarm.l": (-12, 0, -26),
        "upperarm.r": (-34, 0, 34),
        "lowerarm.r": (-8, 0, 52),
    })
    hand_m = bone_world_pos(m_arm, "handslot.r")
    mlpos = hand_m + Vector((-0.02, -0.08, -0.04))
    build_mini_lantern(m, mlpos, scale=0.40)
    H.add_light("POINT", "HeldLanternLight", location=(mlpos.x, mlpos.y - 0.10, mlpos.z),
                energy=55.0, light_color="#ff9428", shadow_soft=0.18)

    # Architectural ruins / dark stone arches / gravestones across skyline with soft moonlight catch
    ruins = [
        ("StoneWall_0.fbx", (-2.6, 4.2, 0.0), (90, 0, -16), 1.4),
        ("StoneWall_1.fbx", (1.2, 5.0, 0.0), (90, 0, 6), 1.3),
        ("BrokenPillar.fbx", (-0.6, 4.6, 0.0), (90, 0, 12), 1.2),
        ("BrokenPillar.fbx", (3.8, 3.8, 0.0), (90, 0, -22), 1.3),
        ("Gravestone_0.fbx", (-1.8, 2.5, 0.0), (90, 0, 22), 1.1),
        ("Rock_0.fbx", (4.1, 1.8, 0.0), (90, 0, 40), 1.1),
        ("Rock_1.fbx", (-3.5, 1.8, 0.0), (90, 0, -30), 1.2),
    ]
    stone_mat = H.material_preset("RuinStone", "stone", base_color="#353b46", roughness=0.84, scale=4.0)
    for fname, loc, rot, sc in ruins:
        fp = os.path.join(MOOR_DIR, "Models", fname)
        if os.path.exists(fp):
            r_objs = import_fbx(fp)
            for o in r_objs:
                o.location = loc
                o.rotation_euler = Euler([math.radians(a) for a in rot])
                o.scale = (sc, sc, sc)
                if o.type == "MESH":
                    o.data.materials.clear()
                    o.data.materials.append(stone_mat)

    banner = import_fbx(os.path.join(DUN_DIR, "banner_red.fbx"))
    bind_texture(banner, os.path.join(DUN_DIR, "dungeon_texture.png"), roughness=0.72, metallic=0.1, sat=0.75, val=0.65)
    for o in banner:
        o.location = (3.45, 3.35, 1.85)
        o.rotation_euler = Euler((math.radians(90), 0, math.radians(-20)))
        o.scale = (0.72, 0.72, 0.72)

    # Warm campfire key + dramatic blue moonlight sky rim illuminating silhouettes cleanly
    H.add_light("POINT", "HearthFireLight", location=(brazier_pos.x, brazier_pos.y - 0.10, 0.95),
                energy=280.0, light_color="#ff7e24", shadow_soft=0.30)
    H.add_light("AREA", "MoonSkyLight", location=(-3.2, 4.8, 5.5), target=(1.2, 1.8, 0.9),
                energy=520.0, size=5.0, light_color="#648ec6")
    H.add_light("AREA", "MoonFrontWash", location=(-3.0, -4.0, 3.2), target=(0.8, 1.4, 0.8),
                energy=95.0, size=5.0, light_color="#486694")

    H.add_camera("StartupCam", location=(-0.35, -5.6, 1.72), target=(0.75, 1.2, 0.96), lens=32.0)
    os.makedirs(OUT_DIR, exist_ok=True)
    out_path = os.path.join(OUT_DIR, "StartupBackdrop.png")
    bpy.context.scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"[build_menu_pack] Rendered StartupBackdrop -> {out_path}")
    return out_path


TASKS = [
    ("PortraitFenwalker", build_portrait_fenwalker),
    ("PortraitLanternbearer", build_portrait_lanternbearer),
    ("PortraitVendor", build_portrait_vendor),
    ("SkillFireball", build_skill_fireball),
    ("SkillArrow", build_skill_arrow),
    ("SkillCleave", build_skill_cleave),
    ("SkillLanternBolt", build_skill_lantern_bolt),
    ("SkillWisps", build_skill_wisps),
    ("SkillSearingHalo", build_skill_searing_halo),
    ("SkillVeilStep", build_skill_veil_step),
    ("PortraitRogue", build_portrait_rogue),
    ("SkillQuickShot", build_skill_quick_shot),
    ("SkillBladeFlurry", build_skill_blade_flurry),
    ("SkillSeismicSlam", build_skill_seismic_slam),
    ("SkillMendingLight", build_skill_mending_light),
    ("SkillRenewingCircle", build_skill_renewing_circle),
    ("CrestCinderreach", build_crest_cinderreach),
    ("WaypointSigil", build_waypoint_sigil),
    ("StashChestIcon", build_stash_chest_icon),
    ("SettingsCog", build_settings_cog),
    ("StartupBackdrop", build_startup_backdrop),
]


def main():
    rendered = []
    only = set(x.strip() for x in os.environ.get("CINDERREACH_ONLY", "").split(",") if x.strip())
    for name, fn in TASKS:
        if only and name not in only:
            continue
        if name != "StartupBackdrop":
            reset_scene(size=(ICON_SIZE, ICON_SIZE), transparent=True, samples=SAMPLES)
        p = fn()
        rendered.append((name, p))
    print(f"[build_menu_pack] Complete: {len(rendered)} assets.")


if __name__ == "__main__":
    main()
