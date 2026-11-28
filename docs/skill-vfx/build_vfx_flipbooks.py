"""Render stylized 4x4 RGBA VFX flipbooks (1024x1024) in Blender 4.2.9 LTS.

Layout matches EmberGen's row-major top-left to bottom-right 4x4 convention
so artists can drop EmberGen exports straight into `Cinderreach/VFX/AbilityFlipbook`.

Run headless:
    ~/.local/blender/current/blender -b --factory-startup -P docs/skill-vfx/build_vfx_flipbooks.py -- --out sandbox/Assets/Art/SkillVfx/Textures
"""
import math
import os
import sys
import tempfile

import bpy
from mathutils import Vector

OUT = globals().get("VFX_FLIPBOOK_OUT")
if OUT is None and "--out" in sys.argv:
    OUT = sys.argv[sys.argv.index("--out") + 1]
OUT = OUT or "/tmp/skill-vfx-flipbooks"
os.makedirs(OUT, exist_ok=True)

CELL = 256
COLS = 4
ROWS = 4
FRAMES = COLS * ROWS


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 4
    scene.cycles.use_denoising = False
    scene.cycles.max_bounces = 2
    scene.cycles.diffuse_bounces = 0
    scene.cycles.glossy_bounces = 0
    scene.cycles.transparent_max_bounces = 8
    scene.render.resolution_x = CELL
    scene.render.resolution_y = CELL
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"

    cam_data = bpy.data.cameras.new("OrthoCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.6
    cam_obj = bpy.data.objects.new("OrthoCam", cam_data)
    cam_obj.location = (0.0, 0.0, 5.0)
    cam_obj.rotation_euler = (0.0, 0.0, 0.0)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    return scene


def make_flipbook_material(name, core_rgb, mid_rgb, rim_rgb, noise_scale, distortion, soft_edge):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    mix_sh = nt.nodes.new("ShaderNodeMixShader")
    transp = nt.nodes.new("ShaderNodeBsdfTransparent")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Strength"].default_value = 1.15

    texcoord = nt.nodes.new("ShaderNodeTexCoord")
    mapping = nt.nodes.new("ShaderNodeMapping")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = noise_scale
    noise.inputs["Detail"].default_value = 4.0
    noise.inputs["Roughness"].default_value = 0.65
    noise.inputs["Distortion"].default_value = distortion

    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.45

    # Mix facing & noise for stylized erosion alpha
    val_math = nt.nodes.new("ShaderNodeMath")
    val_math.operation = "MULTIPLY"
    inv_fresnel = nt.nodes.new("ShaderNodeMath")
    inv_fresnel.operation = "SUBTRACT"
    inv_fresnel.inputs[0].default_value = 1.0

    ramp_alpha = nt.nodes.new("ShaderNodeValToRGB")
    ramp_alpha.color_ramp.elements[0].position = 0.12 if soft_edge else 0.25
    ramp_alpha.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
    ramp_alpha.color_ramp.elements[1].position = 0.72 if soft_edge else 0.58
    ramp_alpha.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)

    ramp_col = nt.nodes.new("ShaderNodeValToRGB")
    ramp_col.color_ramp.elements[0].position = 0.15
    ramp_col.color_ramp.elements[0].color = (*rim_rgb, 1.0)
    e_mid = ramp_col.color_ramp.elements.new(0.50)
    e_mid.color = (*mid_rgb, 1.0)
    ramp_col.color_ramp.elements[2].position = 0.85
    ramp_col.color_ramp.elements[2].color = (*core_rgb, 1.0)

    # Fade multiplier node (value adjusted per frame)
    fade_mul = nt.nodes.new("ShaderNodeMath")
    fade_mul.name = "FrameAlpha"
    fade_mul.operation = "MULTIPLY"
    fade_mul.inputs[1].default_value = 1.0

    nt.links.new(texcoord.outputs["Object"], mapping.inputs["Vector"])
    nt.links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    nt.links.new(lw.outputs["Facing"], inv_fresnel.inputs[1])
    nt.links.new(inv_fresnel.outputs["Value"], val_math.inputs[0])
    nt.links.new(noise.outputs["Fac"], val_math.inputs[1])
    nt.links.new(val_math.outputs["Value"], ramp_alpha.inputs["Fac"])
    nt.links.new(ramp_alpha.outputs["Color"], fade_mul.inputs[0])
    nt.links.new(fade_mul.outputs["Value"], mix_sh.inputs["Fac"])
    nt.links.new(val_math.outputs["Value"], ramp_col.inputs["Fac"])
    nt.links.new(ramp_col.outputs["Color"], emit.inputs["Color"])
    nt.links.new(transp.outputs["BSDF"], mix_sh.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix_sh.inputs[2])
    nt.links.new(mix_sh.outputs["Shader"], out.inputs["Surface"])
    return mat, mapping, ramp_alpha, fade_mul


def add_puff_cluster(count, seed_offset, mat):
    objs = []
    for i in range(count):
        ang = (i / max(1, count)) * math.tau + seed_offset
        r = 0.0 if i == 0 else (0.36 + 0.18 * ((i * 37) % 5) / 4.0)
        bpy.ops.mesh.primitive_ico_sphere_add(
            subdivisions=2,
            radius=0.52 if i == 0 else 0.34,
            location=(math.cos(ang) * r, math.sin(ang) * r, 0.0),
        )
        obj = bpy.context.active_object
        obj.data.materials.append(mat)
        for p in obj.data.polygons:
            p.use_smooth = True
        objs.append((obj, ang, r))
    return objs


def render_atlas(name, config, tmp_dir):
    scene = reset_scene()
    mat, mapping, ramp_alpha, fade_mul = make_flipbook_material(
        name,
        config["core"],
        config["mid"],
        config["rim"],
        config["noise_scale"],
        config["distortion"],
        config["soft_edge"],
    )
    cluster = add_puff_cluster(config["lobes"], config["seed"], mat)

    frame_paths = []
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        scale = config["start_scale"] + (config["end_scale"] - config["start_scale"]) * (t ** 0.72)
        spread = config["spread"] * (t ** 0.85)
        alpha_env = math.sin(min(1.0, t * 1.15) * math.pi * 0.5) if t < 0.25 else max(0.05, (1.0 - (t - 0.25) / 0.75) ** 0.85)
        fade_mul.inputs[1].default_value = alpha_env

        # Eroding threshold rises across lifetime
        ramp_alpha.color_ramp.elements[0].position = min(0.65, 0.08 + 0.52 * (t ** 1.3))
        mapping.inputs["Location"].default_value = (0.0, t * config["drift"], t * 0.45)

        for idx, (obj, ang, base_r) in enumerate(cluster):
            rad = base_r * (1.0 + spread)
            lift = config["rise"] * t * (0.4 + 0.15 * (idx % 3))
            obj.location = (math.cos(ang + t * config["swirl"]) * rad, math.sin(ang + t * config["swirl"]) * rad + lift, 0.0)
            s = scale * (1.0 - 0.25 * t if idx == 0 else 0.85 + 0.35 * t)
            obj.scale = (s, s, s)

        fp = os.path.join(tmp_dir, f"{name}_{f:02d}.png")
        scene.render.filepath = fp
        bpy.ops.render.render(write_still=True)
        frame_paths.append(fp)

    # Pack 4x4 row-major top-left -> bottom-right using Blender + numpy
    import numpy as np
    atlas_w, atlas_h = CELL * COLS, CELL * ROWS
    buf = np.zeros((atlas_h, atlas_w, 4), dtype=np.float32)
    for idx, fp in enumerate(frame_paths):
        col = idx % COLS
        row_top = idx // COLS
        row_bottom = (ROWS - 1) - row_top
        img = bpy.data.images.load(fp, check_existing=False)
        px = np.array(img.pixels[:], dtype=np.float32).reshape((CELL, CELL, 4))
        bpy.data.images.remove(img)
        y0, y1 = row_bottom * CELL, (row_bottom + 1) * CELL
        x0, x1 = col * CELL, (col + 1) * CELL
        buf[y0:y1, x0:x1, :] = px
    out_img = bpy.data.images.new(name, width=atlas_w, height=atlas_h, alpha=True)
    out_img.pixels.foreach_set(buf.ravel())
    out_path = os.path.join(OUT, f"{name}.png")
    out_img.filepath_raw = out_path
    out_img.file_format = "PNG"
    out_img.save()
    bpy.data.images.remove(out_img)
    print(f"[build_vfx_flipbooks] {os.path.basename(out_path)}: {atlas_w}x{atlas_h} (16 frames)")


def write_zero_motion():
    import numpy as np
    w, h = 16, 16
    buf = np.zeros((h, w, 4), dtype=np.float32)
    buf[:, :, 0] = 0.5
    buf[:, :, 1] = 0.5
    buf[:, :, 2] = 0.0
    buf[:, :, 3] = 1.0
    out_img = bpy.data.images.new("VfxFlipbook_ZeroMotion", width=w, height=h, alpha=True)
    out_img.pixels.foreach_set(buf.ravel())
    path = os.path.join(OUT, "VfxFlipbook_ZeroMotion.png")
    out_img.filepath_raw = path
    out_img.file_format = "PNG"
    out_img.save()
    bpy.data.images.remove(out_img)
    print(f"[build_vfx_flipbooks] {os.path.basename(path)}: 16x16 neutral motion map")


FLIPBOOKS = {
    "VfxFlipbook_FireBlast_4x4": {
        "core": (1.0, 0.96, 0.78),
        "mid": (1.0, 0.56, 0.14),
        "rim": (0.72, 0.12, 0.06),
        "noise_scale": 3.4,
        "distortion": 1.1,
        "soft_edge": False,
        "lobes": 7,
        "seed": 0.4,
        "start_scale": 0.42,
        "end_scale": 1.05,
        "spread": 1.35,
        "swirl": 0.8,
        "rise": 0.15,
        "drift": 0.9,
    },
    "VfxFlipbook_SmokePuff_4x4": {
        "core": (0.85, 0.86, 0.90),
        "mid": (0.52, 0.54, 0.60),
        "rim": (0.24, 0.25, 0.30),
        "noise_scale": 2.6,
        "distortion": 0.7,
        "soft_edge": True,
        "lobes": 8,
        "seed": 1.2,
        "start_scale": 0.48,
        "end_scale": 1.12,
        "spread": 1.15,
        "swirl": 0.55,
        "rise": 0.28,
        "drift": 0.65,
    },
    "VfxFlipbook_MireSplash_4x4": {
        "core": (0.95, 1.0, 0.90),
        "mid": (0.68, 0.88, 0.56),
        "rim": (0.36, 0.52, 0.28),
        "noise_scale": 4.2,
        "distortion": 1.4,
        "soft_edge": False,
        "lobes": 9,
        "seed": 2.1,
        "start_scale": 0.38,
        "end_scale": 1.08,
        "spread": 1.55,
        "swirl": 0.4,
        "rise": 0.20,
        "drift": 1.2,
    },
}


def main():
    with tempfile.TemporaryDirectory(prefix="vfx-flipbook-") as tmp_dir:
        for name, cfg in FLIPBOOKS.items():
            render_atlas(name, cfg, tmp_dir)
    write_zero_motion()


if __name__ == "__main__":
    main()
