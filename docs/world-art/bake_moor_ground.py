"""Blood-moor ground texture, baked in Blender (1024 px, seamless).

Run through the Blender MCP bridge (execute_blender_code with
GROUND_OUT = "<file.png>" prepended) or headless:
    blender -b --factory-startup -P bake_moor_ground.py -- --out <file.png>
Output replaces sandbox/Assets/Art/BloodMoor/Textures/MoorGround.png
(WorldArtBuilder tiles it 6x over the 40 m moor).

Look: moss vs peat patches, a heather tint, fine grain and dry-grass grit.
Procedural nodes are rendered flat (emission, Standard view transform) from
an ortho camera, then made tileable by blending each axis with a half-offset
copy (seam of the copy lands where the original is kept).
"""
import os
import sys

import bpy
import numpy as np

OUT = globals().get("GROUND_OUT")
if OUT is None and "--out" in sys.argv:
    OUT = sys.argv[sys.argv.index("--out") + 1]
OUT = OUT or "/tmp/world-art/MoorGround.png"
SIZE = 1024


def srgb(h):
    h = h.lstrip("#")
    return tuple(((int(h[i:i + 2], 16) / 255) ** 2.2) for i in (0, 2, 4)) + (1.0,)


def build_material():
    m = bpy.data.materials.new("MoorGroundBake")
    m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    for n in list(nodes):
        nodes.remove(n)
    out = nodes.new("ShaderNodeOutputMaterial")
    emis = nodes.new("ShaderNodeEmission")
    links.new(emis.outputs[0], out.inputs[0])
    coord = nodes.new("ShaderNodeTexCoord").outputs["Object"]

    def noise(scale, detail, rough, w):
        n = nodes.new("ShaderNodeTexNoise")
        n.noise_dimensions = "4D"
        n.inputs["Scale"].default_value = scale
        n.inputs["Detail"].default_value = detail
        n.inputs["Roughness"].default_value = rough
        n.inputs["W"].default_value = w
        links.new(coord, n.inputs["Vector"])
        return n.outputs["Fac"]

    def ramp(src, stops):
        r = nodes.new("ShaderNodeValToRGB")
        els = r.color_ramp.elements
        els[0].position, els[0].color = stops[0]
        els[1].position, els[1].color = stops[1]
        for pos, col in stops[2:]:
            e = els.new(pos)
            e.color = col
        links.new(src, r.inputs[0])
        return r.outputs[0]

    def mix(a, b, fac, mode="MIX"):
        mx = nodes.new("ShaderNodeMix")
        mx.data_type = "RGBA"
        mx.blend_type = mode
        if isinstance(fac, float):
            mx.inputs[0].default_value = fac
        else:
            links.new(fac, mx.inputs[0])
        links.new(a, mx.inputs[6])
        links.new(b, mx.inputs[7])
        return mx.outputs[2]

    def rgb(h):
        n = nodes.new("ShaderNodeRGB")
        n.outputs[0].default_value = srgb(h)
        return n.outputs[0]

    black, white = (0, 0, 0, 1), (1, 1, 1, 1)
    col = ramp(noise(1.1, 6, 0.55, 1.0),
               [(0.38, srgb("#3B4A27")), (0.52, srgb("#433A26")), (0.62, srgb("#2F2619"))])
    col = mix(col, rgb("#4B3A44"), ramp(noise(5.0, 8, 0.6, 7.0), [(0.55, black), (0.68, white)]))
    col = mix(col, ramp(noise(22.0, 2, 0.7, 3.0), [(0.3, srgb("#6A6A6A")), (0.7, white)]), 1.0, "MULTIPLY")
    grit = ramp(noise(70.0, 1, 0.5, 11.0), [(0.66, black), (0.72, (0.45, 0.45, 0.45, 1))])
    col = mix(col, rgb("#6E6338"), grit)
    links.new(col, emis.inputs["Color"])
    return m


def render_raw(path):
    scn = bpy.context.scene
    r = scn.render
    bpy.ops.mesh.primitive_plane_add(size=2, location=(1000, 0, 0))
    plane = bpy.context.active_object
    plane.data.materials.append(build_material())
    cam_data = bpy.data.cameras.new("MoorGroundCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.0
    cam = bpy.data.objects.new("MoorGroundCam", cam_data)
    scn.collection.objects.link(cam)
    cam.location = (1000, 0, 5)
    hidden = [o for o in scn.objects if o not in (plane, cam) and not o.hide_render]
    for o in hidden:
        o.hide_render = True
    saved = (scn.camera, r.engine, r.resolution_x, r.resolution_y, r.resolution_percentage,
             scn.view_settings.view_transform, r.filepath, r.image_settings.file_format)
    try:
        scn.camera = cam
        r.engine = "BLENDER_EEVEE"
        r.resolution_x = r.resolution_y = SIZE
        r.resolution_percentage = 100
        scn.view_settings.view_transform = "Standard"
        r.image_settings.file_format = "PNG"
        r.filepath = path
        bpy.ops.render.render(write_still=True)
    finally:
        (scn.camera, r.engine, r.resolution_x, r.resolution_y, r.resolution_percentage,
         scn.view_settings.view_transform, r.filepath, r.image_settings.file_format) = saved
        for o in hidden:
            o.hide_render = False
        mat = plane.data.materials[0]
        bpy.data.objects.remove(plane, do_unlink=True)
        bpy.data.objects.remove(cam, do_unlink=True)
        bpy.data.materials.remove(mat)


def seamless(a, axis):
    n = a.shape[axis]
    rolled = np.roll(a, n // 2, axis)
    idx = np.arange(n)
    t = np.clip(np.minimum(idx, n - 1 - idx) / (n * 0.25), 0, 1)
    mask = (t * t * (3 - 2 * t)).reshape([n if i == axis else 1 for i in range(3)])
    return a * mask + rolled * (1 - mask)


os.makedirs(os.path.dirname(OUT), exist_ok=True)
raw = OUT + ".raw.png"
render_raw(raw)
img = bpy.data.images.load(raw, check_existing=False)
w, h = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
px = seamless(seamless(px, 1), 0)
px[..., 3] = 1.0
img.pixels[:] = px.ravel()
img.filepath_raw = OUT
img.file_format = "PNG"
img.save()
bpy.data.images.remove(img)
os.remove(raw)
print("MOOR_GROUND baked", w, "x", h, "->", OUT)
