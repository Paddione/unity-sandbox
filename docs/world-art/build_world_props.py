"""Blood-moor world props, low-poly, one FBX per prop.

Run through the Blender MCP bridge (execute_blender_code with
WORLD_ART_OUT = "<dir>" prepended) or headless:
    blender -b --factory-startup -P build_world_props.py -- --out <dir>
Deterministic (seeded). Origins sit at the base center; Blender Z-up is
exported as Unity Y-up. Material names are the contract with
sandbox/Assets/Editor/WorldArtBuilder.cs, which maps them to URP Lit
materials (palette from DiabloPalette.cs).
"""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

OUT = globals().get("WORLD_ART_OUT")
if OUT is None and "--out" in sys.argv:
    OUT = sys.argv[sys.argv.index("--out") + 1]
OUT = OUT or "/tmp/world-art"
os.makedirs(OUT, exist_ok=True)

# name -> sRGB hex (Blender preview only; Unity owns the final look)
PALETTE = {
    "Bark": "#4A3220", "DarkWood": "#5A3A22", "Stone": "#55505A", "Moss": "#3E4F2A",
    "Bone": "#D8CCB0", "Iron": "#3A3A40", "Flame": "#E84E00", "Coal": "#8A1A00",
    "BloodCloth": "#8E1620", "Gold": "#F2B134", "Heather": "#4F5A33",
    "HeatherBloom": "#7A3F6E", "Grass": "#5E6B34", "Rune": "#FF2A1A", "Blood": "#4A0608",
}

# Optional subset: WORLD_ART_ONLY = ["StoneWall_0", ...] or --only a,b exports just
# those props, so adding a prop does not rewrite (and re-diff) the others.
ONLY = globals().get("WORLD_ART_ONLY")
if ONLY is None and "--only" in sys.argv:
    ONLY = sys.argv[sys.argv.index("--only") + 1].split(",")


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        h = PALETTE[name].lstrip("#")
        rgb = [((int(h[i:i + 2], 16) / 255.0) ** 2.2) for i in (0, 2, 4)]
        m.diffuse_color = (*rgb, 1.0)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
            if name in ("Flame", "Coal", "Rune"):
                key = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
                bsdf.inputs[key].default_value = (*rgb, 1.0)
                bsdf.inputs["Emission Strength"].default_value = 3.0
    return m


def finish(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(mat(material))
    return obj


def cyl(r, depth, loc, rot=(0, 0, 0), verts=7, material="Bark", r2=None):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=depth, location=loc, rotation=rot)
    return finish(bpy.context.active_object, material)


def box(size, loc, rot=(0, 0, 0), material="Stone"):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.scale = size
    return finish(o, material)


def rock_blob(radius, loc, rng, material="Stone", squash=0.7, moss=True, subdiv=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=radius, location=loc)
    o = bpy.context.active_object
    for v in o.data.vertices:
        v.co *= 1.0 + rng.uniform(-0.22, 0.22)
        v.co.z *= squash
    finish(o, material)
    if moss:
        o.data.materials.append(mat("Moss"))
        o.data.update()
        for poly in o.data.polygons:
            if poly.normal.z > 0.75:
                poly.material_index = 1
    return o


def flame(loc, scale=1.0, rng=None):
    parts = []
    rng = rng or random.Random(3)
    for i in range(3):
        a = i * 2.1
        off = Vector((math.cos(a), math.sin(a), 0)) * 0.08 * scale
        h = (0.55 + rng.uniform(-0.1, 0.15)) * scale
        parts.append(cyl(0.13 * scale, h, Vector(loc) + off + Vector((0, 0, h / 2)), verts=5,
                         material="Flame", r2=0.0))
    return parts


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for p in o.data.polygons:  # flat shading: faceted low-poly look
        p.use_smooth = False
    return o


def spin(o, rx=0.0, ry=0.0, rz=0.0):
    o.rotation_euler = (rx, ry, rz)
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    return o


def export(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    path = os.path.join(OUT, o.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)
    return path


# --------------------------------------------------------------------- props

def campfire():
    rng = random.Random(11)
    parts = []
    for i in range(9):
        a = i * 2 * math.pi / 9
        parts.append(rock_blob(0.17, (math.cos(a) * 0.62, math.sin(a) * 0.62, 0.1), rng, moss=False, subdiv=1))
    for i in range(4):
        a = i * math.pi / 2 + math.pi / 4
        parts.append(cyl(0.08, 0.95, (math.cos(a) * 0.2, math.sin(a) * 0.2, 0.22),
                         rot=(math.radians(65), 0, a + math.pi / 2), material="DarkWood"))
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.3, depth=0.06, location=(0, 0, 0.04))
    parts.append(finish(bpy.context.active_object, "Coal"))
    parts += flame((0, 0, 0.12), 1.2, rng)
    return join(parts, "Campfire")


def stake(i):
    rng = random.Random(100 + i)
    h = 2.4 + rng.uniform(-0.2, 0.25)
    parts = [cyl(0.22, h, (0, 0, h / 2), verts=7, material="Bark")]
    parts.append(cyl(0.22, 0.45, (0, 0, h + 0.225), verts=7, material="DarkWood", r2=0.02))
    parts.append(cyl(0.235, 0.12, (0, 0, h * 0.62), verts=7, material="Iron"))  # binding band
    o = join(parts, f"Stake_{i}")
    return spin(o, rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05), rng.uniform(0, 6.28))


def rock(i):
    rng = random.Random(200 + i)
    parts = [rock_blob(0.6 + 0.15 * i, (0, 0, 0.18), rng, squash=0.6)]
    for k in range(i + 1):
        a = rng.uniform(0, 6.28)
        parts.append(rock_blob(0.22, (math.cos(a) * 0.7, math.sin(a) * 0.7, 0.05), rng, squash=0.7, subdiv=1))
    return join(parts, f"Rock_{i}")


def dead_tree(i):
    rng = random.Random(300 + i)
    h = 3.2 + i * 0.8
    parts = [cyl(0.28, h, (0, 0, h / 2), verts=7, material="Bark", r2=0.07)]
    for b in range(4 + i):
        z = h * rng.uniform(0.35, 0.85)
        a = rng.uniform(0, 6.28)
        tilt = rng.uniform(0.6, 1.1)
        length = rng.uniform(0.9, 1.6) * (1.2 - z / h)
        d = Vector((math.cos(a) * math.sin(tilt), math.sin(a) * math.sin(tilt), math.cos(tilt)))
        mid = Vector((0, 0, z)) + d * (length / 2)
        parts.append(cyl(0.08, length, mid, rot=d.to_track_quat("Z", "Y").to_euler(), verts=5,
                         material="Bark", r2=0.02))
    for r in range(4):  # root flare
        a = r * math.pi / 2 + 0.4
        d = Vector((math.cos(a), math.sin(a), -0.35)).normalized()
        parts.append(cyl(0.12, 0.8, Vector((0, 0, 0.18)) + d * 0.35, rot=d.to_track_quat("Z", "Y").to_euler(),
                         verts=5, material="Bark", r2=0.03))
    return join(parts, f"DeadTree_{i}")


def gravestone(i):
    rng = random.Random(400 + i)
    if i == 0:
        parts = [box((0.6, 0.18, 0.75), (0, 0, 0.375))]
        parts.append(cyl(0.3, 0.18, (0, 0, 0.75), rot=(math.radians(90), 0, 0), verts=10, material="Stone"))
    else:
        parts = [box((0.16, 0.16, 1.2), (0, 0, 0.6)), box((0.62, 0.16, 0.16), (0, 0, 0.88))]
    parts.append(box((0.8, 0.5, 0.08), (0, 0.1, 0.04), material="Moss"))
    o = join(parts, f"Gravestone_{i}")
    return spin(o, rng.uniform(-0.12, 0.12), rng.uniform(-0.1, 0.1), 0)


def bone_pile():
    rng = random.Random(500)
    parts = []
    for k in range(7):
        a = rng.uniform(0, 6.28)
        r = rng.uniform(0, 0.35)
        parts.append(cyl(0.035, rng.uniform(0.35, 0.6), (math.cos(a) * r, math.sin(a) * r, 0.05),
                         rot=(math.radians(90), 0, rng.uniform(0, 6.28)), verts=5, material="Bone"))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=6, radius=0.16, location=(0.1, -0.05, 0.16))
    skull = finish(bpy.context.active_object, "Bone")
    skull.scale = (1, 1.15, 0.95)
    parts.append(skull)
    parts.append(box((0.16, 0.14, 0.08), (0.1, -0.2, 0.07), material="Bone"))
    return join(parts, "BonePile")


def broken_pillar():
    rng = random.Random(600)
    parts = [box((0.95, 0.95, 0.25), (0, 0, 0.125))]
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.36, depth=2.4, location=(0, 0, 1.45))
    col = finish(bpy.context.active_object, "Stone")
    for v in col.data.vertices:
        if v.co.z > 0:
            v.co.z += rng.uniform(-0.55, 0.1)
    parts.append(col)
    return join(parts, "BrokenPillar")


def brazier():
    rng = random.Random(700)
    parts = []
    for k in range(3):
        a = k * 2 * math.pi / 3
        d = Vector((math.cos(a) * 0.35, math.sin(a) * 0.35, 1.0)).normalized()
        parts.append(cyl(0.04, 1.05, Vector((math.cos(a) * 0.17, math.sin(a) * 0.17, 0.5)),
                         rot=(-d).to_track_quat("Z", "Y").to_euler(), verts=5, material="Iron"))
    parts.append(cyl(0.42, 0.28, (0, 0, 1.08), verts=10, material="Iron", r2=0.22))
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.36, depth=0.06, location=(0, 0, 1.2))
    parts.append(finish(bpy.context.active_object, "Coal"))
    parts += flame((0, 0, 1.2), 1.0, rng)
    return join(parts, "Brazier")


def banner():
    parts = [cyl(0.05, 3.2, (0, 0, 1.6), verts=6, material="DarkWood"),
             cyl(0.04, 1.1, (0, 0.06, 2.95), rot=(0, math.radians(90), 0), verts=6, material="DarkWood"),
             cyl(0.07, 0.12, (0, 0, 3.26), verts=6, material="Gold", r2=0.0)]
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=6, y_subdivisions=8, size=1, location=(0, 0.1, 2.25),
                                    rotation=(math.radians(90), 0, 0))
    cloth = finish(bpy.context.active_object, "BloodCloth")
    cloth.scale = (0.95, 1.35, 1)
    bpy.ops.object.transform_apply(location=True, scale=True, rotation=True)
    for v in cloth.data.vertices:
        v.co.y += math.sin(v.co.x * 4.0 + v.co.z * 2.0) * 0.05
        if v.co.z < 1.65:  # swallowtail cut
            v.co.z += abs(v.co.x) * 0.5
    solid = cloth.modifiers.new("Solid", "SOLIDIFY")
    solid.thickness = 0.03
    bpy.context.view_layer.objects.active = cloth
    bpy.ops.object.modifier_apply(modifier="Solid")
    parts.append(cloth)
    parts.append(box((0.98, 0.05, 0.08), (0, 0.1, 2.9), material="Gold"))
    return join(parts, "Banner")


def tuft(name, blades, height, base_mat, tip_mat, seed):
    rng = random.Random(seed)
    parts = []
    for k in range(blades):
        a = rng.uniform(0, 6.28)
        r = rng.uniform(0, 0.22)
        h = height * rng.uniform(0.6, 1.1)
        lean = rng.uniform(0.0, 0.45)
        d = Vector((math.cos(a) * lean, math.sin(a) * lean, 1)).normalized()
        base = Vector((math.cos(a) * r, math.sin(a) * r, 0))
        parts.append(cyl(0.035, h, base + d * (h / 2), rot=d.to_track_quat("Z", "Y").to_euler(),
                         verts=3, material=base_mat, r2=0.0))
        if tip_mat and k % 2 == 0:
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.05, location=base + d * h)
            parts.append(finish(bpy.context.active_object, tip_mat))
    return join(parts, name)


def chamfer(o, width=0.04):
    """One-segment bevel, applied: chamfered edges catch the moon rim light."""
    bev = o.modifiers.new("Chamfer", "BEVEL")
    bev.width = width
    bev.segments = 1
    bev.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.modifier_apply(modifier="Chamfer")
    return o


def moss_top(o, threshold=0.8):
    """Second material slot: Moss on up-facing faces (rain-side growth)."""
    o.data.materials.append(mat("Moss"))
    for poly in o.data.polygons:
        if poly.normal.z > threshold:
            poly.material_index = 1
    return o


def stone_wall(i):
    """Drystone boundary wall, 4 m long (X), ~0.9 m deep, ~1.5 m high."""
    rng = random.Random(900 + i)
    parts = []
    rows = [(0.0, 0.5), (0.5, 0.45), (0.95, 0.4)]
    for r, (z0, h) in enumerate(rows):
        x = -2.0 + (0.25 if r % 2 else 0.0) * rng.uniform(0.6, 1.4)
        while x < 2.0:
            w = min(rng.uniform(0.55, 1.15), 2.0 - x)
            if w < 0.2:
                break
            d = 0.9 - r * 0.08 + rng.uniform(-0.05, 0.05)
            hh = h * rng.uniform(0.88, 1.02)
            b = box((w - 0.04, d, hh), (x + w / 2, rng.uniform(-0.04, 0.04), z0 + hh / 2),
                    rot=(rng.uniform(-0.04, 0.04), rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05)))
            parts.append(chamfer(b, 0.05))
            x += w
    # cap stones, some missing, mossy on top
    x = -2.0
    while x < 1.9:
        w = rng.uniform(0.35, 0.6)
        if rng.random() > 0.18:
            b = box((w - 0.03, 0.5, 0.16), (x + w / 2, rng.uniform(-0.06, 0.06), 1.43),
                    rot=(rng.uniform(-0.12, 0.12), rng.uniform(-0.1, 0.1), rng.uniform(-0.2, 0.2)))
            parts.append(moss_top(chamfer(b, 0.03), 0.6))
        x += w
    # fallen stones at the foot
    for k in range(2 + i):
        side = rng.choice((-1, 1))
        parts.append(rock_blob(0.22, (rng.uniform(-1.7, 1.7), side * rng.uniform(0.55, 0.75), 0.06),
                               rng, squash=0.55, moss=False, subdiv=1))
    return join(parts, f"StoneWall_{i}")


def stone_step():
    """Unit-height step block 4 m x 1.2 m; Unity scales Y to the step top."""
    rng = random.Random(950)
    parts = []
    x = -2.0
    while x < 2.0:
        w = min(rng.uniform(0.9, 1.5), 2.0 - x)
        if w < 0.3:
            break
        b = box((w - 0.05, 1.2 - rng.uniform(0.0, 0.06), 1.0 + rng.uniform(-0.03, 0.0)),
                (x + w / 2, 0, 0.5), rot=(0, 0, rng.uniform(-0.02, 0.02)))
        parts.append(chamfer(b, 0.06))  # no moss: whole step tops read as bright green tiles
        x += w
    return join(parts, "StoneStep")


def crypt_seal():
    """Round crypt seal slab, 3 m: stone disc, carved glowing rune ring, centre sigil."""
    rng = random.Random(970)
    parts = []
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=1.5, depth=0.2, location=(0, 0, 0.1))
    parts.append(chamfer(finish(bpy.context.active_object, "Stone"), 0.06))
    # rune ring: 12 short glowing bars sunk into the face
    for k in range(12):
        a = k * 2 * math.pi / 12
        parts.append(box((0.26, 0.07, 0.04), (math.cos(a) * 1.15, math.sin(a) * 1.15, 0.2),
                         rot=(0, 0, a + math.pi / 2 + rng.uniform(-0.2, 0.2)), material="Rune"))
        if k % 3 == 0:  # tick marks
            parts.append(box((0.06, 0.18, 0.04), (math.cos(a) * 0.9, math.sin(a) * 0.9, 0.2),
                             rot=(0, 0, a + math.pi / 2), material="Rune"))
    # inner iron ring and sigil (pentagon star of bars)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.62, minor_radius=0.05, major_segments=20,
                                     minor_segments=4, location=(0, 0, 0.2))
    parts.append(finish(bpy.context.active_object, "Iron"))
    pts = [Vector((math.cos(math.pi / 2 + k * 2 * math.pi / 5) * 0.55,
                   math.sin(math.pi / 2 + k * 2 * math.pi / 5) * 0.55, 0.21)) for k in range(5)]
    for k in range(5):
        a, b = pts[k], pts[(k + 2) % 5]
        mid, d = (a + b) / 2, (b - a)
        parts.append(box((d.length, 0.06, 0.04), mid, rot=(0, 0, math.atan2(d.y, d.x)), material="Rune"))
    # cracked rim chips
    for k in range(5):
        a = rng.uniform(0, 6.28)
        parts.append(rock_blob(0.16, (math.cos(a) * 1.55, math.sin(a) * 1.55, 0.05), rng,
                               squash=0.5, moss=True, subdiv=1))
    return join(parts, "CryptSeal")


def blood_splat(i):
    """Flat irregular blood pool with satellite drops, 1.8 m across, 1 cm thick."""
    rng = random.Random(990 + i)
    parts = []
    n = 18
    bpy.ops.mesh.primitive_circle_add(vertices=n, radius=0.6, fill_type="NGON", location=(0, 0, 0.006))
    pool = finish(bpy.context.active_object, "Blood")
    for v in pool.data.vertices:
        if v.co.length > 0.01:
            v.co.xy *= rng.uniform(0.7, 1.35)
    pool.scale = (1.0 + 0.3 * i, 1.0, 1.0)
    parts.append(pool)
    for k in range(7 + i * 3):
        a = rng.uniform(0, 6.28)
        r = rng.uniform(0.75, 0.95 + 0.2 * i)
        bpy.ops.mesh.primitive_circle_add(vertices=6, radius=rng.uniform(0.04, 0.11), fill_type="NGON",
                                          location=(math.cos(a) * r, math.sin(a) * r, 0.006))
        parts.append(finish(bpy.context.active_object, "Blood"))
    o = join(parts, f"BloodSplat_{i}")
    bpy.ops.object.mode_set(mode="EDIT")  # 1 cm slab so the decal never z-fights the ground
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.extrude_region_move(TRANSFORM_OT_translate={"value": (0, 0, 0.01)})
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    for p in o.data.polygons:
        p.use_smooth = False
    return o


BUILDERS = [campfire] + [lambda i=i: stake(i) for i in range(3)] + \
    [lambda i=i: rock(i) for i in range(3)] + [lambda i=i: dead_tree(i) for i in range(2)] + \
    [lambda i=i: gravestone(i) for i in range(2)] + [bone_pile, broken_pillar, brazier, banner] + \
    [lambda: tuft("Heather_0", 14, 0.45, "Heather", "HeatherBloom", 801),
     lambda: tuft("Heather_1", 10, 0.35, "Heather", "HeatherBloom", 802),
     lambda: tuft("Grass_0", 16, 0.5, "Grass", None, 803)] + \
    [lambda i=i: stone_wall(i) for i in range(2)] + [stone_step, crypt_seal] + \
    [lambda i=i: blood_splat(i) for i in range(2)]

reset()
exported = []
for build in BUILDERS:
    obj = build()
    if ONLY and obj.name not in ONLY:
        bpy.data.objects.remove(obj, do_unlink=True)
        continue
    exported.append(export(obj))
    obj.hide_set(True)  # out of the next selection; unhidden for the preview below

x = 0.0
for o in bpy.data.objects:
    if o.type == "MESH":
        o.hide_set(False)
        o.location = (x, 0, 0)
        x += max(o.dimensions.x, 1.0) + 0.6
print("WORLD_ART exported", len(exported), "to", OUT)
for p in exported:
    print(" -", os.path.basename(p))
