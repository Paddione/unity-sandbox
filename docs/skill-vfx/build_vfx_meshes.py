"""Low-poly UV- and vertex-color-authored VFX meshes for Cinderreach abilities.

Run headless:
    ~/.local/blender/current/blender -b --factory-startup -P docs/skill-vfx/build_vfx_meshes.py -- --out sandbox/Assets/Art/SkillVfx/Meshes
Or via Blender MCP (execute_blender_code with VFX_MESH_OUT="<dir>" prepended).

Deterministic. All meshes stay <= 1500 vertices, include explicit UV0 and
SRGB vertex color masks (RGB=1, A=fade mask), and export as Unity Y-up FBX.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

OUT = globals().get("VFX_MESH_OUT")
if OUT is None and "--out" in sys.argv:
    OUT = sys.argv[sys.argv.index("--out") + 1]
OUT = OUT or "/tmp/skill-vfx-meshes"
os.makedirs(OUT, exist_ok=True)

ONLY = globals().get("VFX_MESH_ONLY")
if ONLY is None and "--only" in sys.argv:
    ONLY = sys.argv[sys.argv.index("--only") + 1].split(",")

MAT_COLORS = {
    "VfxMat": (1.0, 1.0, 1.0),
    "Wood": (0.36, 0.21, 0.11),
    "Iron": (0.24, 0.25, 0.29),
    "Fuse": (0.95, 0.55, 0.15),
    "Glass": (0.55, 0.85, 0.65),
    "Liquid": (0.35, 0.92, 0.25),
}


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def get_mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        rgb = MAT_COLORS.get(name, (1.0, 1.0, 1.0))
        m.diffuse_color = (*rgb, 1.0)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    return m


def make_bmesh_object(name, bm, material_names=("VfxMat",), smooth=False):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = smooth
    for mname in material_names:
        mesh.materials.append(get_mat(mname))
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def ensure_uv_and_vc(bm):
    uv_layer = bm.loops.layers.uv.verify()
    col_layer = bm.loops.layers.color.get("Color") or bm.loops.layers.color.new("Color")
    return uv_layer, col_layer


def set_loop(loop, uv_layer, col_layer, u, v, alpha=1.0, rgb=(1.0, 1.0, 1.0)):
    loop[uv_layer].uv = (u, v)
    loop[col_layer] = (rgb[0], rgb[1], rgb[2], max(0.0, min(1.0, alpha)))


def build_slash_arc(name, span_deg, inner_r, outer_r, segs, rings, tip_taper=False):
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    span = math.radians(span_deg)
    # Centered around Blender +Y (Unity +Z forward) when directional
    start_ang = math.pi * 0.5 - span * 0.5 if span_deg < 359.0 else 0.0

    grid = []
    for i in range(segs + 1):
        u = i / segs
        ang = start_ang + u * span
        # For directional cone, taper radius & edge alpha near tips
        tip_env = math.sin(u * math.pi) if tip_taper else 1.0
        cur_inner = inner_r + (outer_r - inner_r) * 0.35 * (1.0 - tip_env) if tip_taper else inner_r
        row = []
        for j in range(rings + 1):
            v = j / rings
            r = cur_inner + (outer_r - cur_inner) * v
            x = math.cos(ang) * r
            y = math.sin(ang) * r
            z = 0.04 * math.sin(v * math.pi)
            vert = bm.verts.new((x, y, z))
            # Soft radial falloff at inner/outer edges + tip fade for cone
            radial_a = math.sin(v * math.pi) ** 0.7 if rings >= 2 else 1.0
            a = radial_a * (tip_env ** 0.8 if tip_taper else 1.0)
            row.append((vert, u, v, a))
        grid.append(row)

    for i in range(segs):
        for j in range(rings):
            v00, u0, v0, a00 = grid[i][j]
            v10, u1, _, a10 = grid[i + 1][j]
            v11, _, v1, a11 = grid[i + 1][j + 1]
            v01, _, _, a01 = grid[i][j + 1]
            f = bm.faces.new((v00, v10, v11, v01))
            data = [(u0, v0, a00), (u1, v0, a10), (u1, v1, a11), (u0, v1, a01)]
            for loop, (u, v, a) in zip(f.loops, data):
                set_loop(loop, uv_layer, col_layer, u, v, a)
    return make_bmesh_object(name, bm, smooth=True)


def build_shockwave_ring():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    segs = 32
    profile = [
        (0.62, 0.00, 0.0),
        (0.76, 0.22, 1.0),
        (0.90, 0.25, 1.0),
        (1.00, 0.03, 0.0),
    ]
    grid = []
    for i in range(segs + 1):
        u = i / segs
        ang = u * math.tau
        ca, sa = math.cos(ang), math.sin(ang)
        row = []
        for j, (r, z, a) in enumerate(profile):
            v = j / (len(profile) - 1)
            vert = bm.verts.new((ca * r, sa * r, z))
            row.append((vert, u, v, a))
        grid.append(row)

    for i in range(segs):
        for j in range(len(profile) - 1):
            v00, u0, v0, a00 = grid[i][j]
            v10, u1, _, a10 = grid[i + 1][j]
            v11, _, v1, a11 = grid[i + 1][j + 1]
            v01, _, _, a01 = grid[i][j + 1]
            f = bm.faces.new((v00, v10, v11, v01))
            for loop, (u, v, a) in zip(f.loops, [(u0, v0, a00), (u1, v0, a10), (u1, v1, a11), (u0, v1, a01)]):
                set_loop(loop, uv_layer, col_layer, u, v, a)
    return make_bmesh_object("VfxShockwaveRing", bm, smooth=True)


def build_ground_disc():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    segs = 32
    radii = [(0.0, 1.0), (0.35, 1.0), (0.72, 1.0), (0.92, 0.85), (1.0, 0.0)]
    center = bm.verts.new((0.0, 0.0, 0.01))
    rings = []
    for r, a in radii[1:]:
        ring = []
        for i in range(segs):
            ang = i * math.tau / segs
            x = math.cos(ang) * r
            y = math.sin(ang) * r
            ring.append((bm.verts.new((x, y, 0.01)), x, y, a))
        rings.append(ring)

    for i in range(segs):
        ni = (i + 1) % segs
        v1, x1, y1, a1 = rings[0][i]
        v2, x2, y2, a2 = rings[0][ni]
        f = bm.faces.new((center, v1, v2))
        set_loop(f.loops[0], uv_layer, col_layer, 0.5, 0.5, 1.0)
        set_loop(f.loops[1], uv_layer, col_layer, 0.5 + x1 * 0.5, 0.5 + y1 * 0.5, a1)
        set_loop(f.loops[2], uv_layer, col_layer, 0.5 + x2 * 0.5, 0.5 + y2 * 0.5, a2)

    for r_idx in range(len(rings) - 1):
        r0, r1 = rings[r_idx], rings[r_idx + 1]
        for i in range(segs):
            ni = (i + 1) % segs
            v00, x00, y00, a00 = r0[i]
            v10, x10, y10, a10 = r0[ni]
            v11, x11, y11, a11 = r1[ni]
            v01, x01, y01, a01 = r1[i]
            f = bm.faces.new((v00, v10, v11, v01))
            pts = [(x00, y00, a00), (x10, y10, a10), (x11, y11, a11), (x01, y01, a01)]
            for loop, (x, y, a) in zip(f.loops, pts):
                set_loop(loop, uv_layer, col_layer, 0.5 + x * 0.5, 0.5 + y * 0.5, a)
    return make_bmesh_object("VfxGroundDisc", bm, smooth=True)


def build_hook_chain():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    # Crossed chain ribbons from y=0 to y=1.0 (Unity +Z)
    segs = 8
    w = 0.08
    for tilt in (0.0, math.pi * 0.5):
        ct, st = math.cos(tilt), math.sin(tilt)
        rows = []
        for i in range(segs + 1):
            t = i / segs
            y = t * 0.92
            a = 1.0 if t > 0.05 else t / 0.05
            p_l = bm.verts.new((-w * ct, y, -w * st))
            p_r = bm.verts.new((w * ct, y, w * st))
            rows.append((p_l, p_r, t * 4.0, a))
        for i in range(segs):
            l0, r0, u0, a0 = rows[i]
            l1, r1, u1, a1 = rows[i + 1]
            f = bm.faces.new((l0, r0, r1, l1))
            for loop, (u, v, a) in zip(f.loops, [(u0, 0.0, a0), (u0, 1.0, a0), (u1, 1.0, a1), (u1, 0.0, a1)]):
                set_loop(loop, uv_layer, col_layer, u, v, a)

    # Stylized barbed iron hook head at y=0.92..1.12
    tip = bm.verts.new((0.0, 1.14, 0.0))
    base_l = bm.verts.new((-0.14, 0.90, 0.0))
    base_r = bm.verts.new((0.14, 0.90, 0.0))
    top_k = bm.verts.new((0.0, 0.96, 0.06))
    bot_k = bm.verts.new((0.0, 0.96, -0.06))
    for tri in ((tip, base_l, top_k), (tip, top_k, base_r), (tip, base_r, bot_k), (tip, bot_k, base_l)):
        f = bm.faces.new(tri)
        for idx, loop in enumerate(f.loops):
            set_loop(loop, uv_layer, col_layer, idx * 0.5, 0.5, 1.0, (0.75, 0.82, 0.9))
    return make_bmesh_object("VfxHookChain", bm, smooth=False)


def build_charge_chevron():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    # Forward-swept plow wedge pointing along +Y (Unity +Z)
    wings = [
        ((-0.95, -0.55, 0.05), (0.0, 0.0, 0.0)),
        ((-0.55, 0.15, 0.28), (0.0, 0.5, 0.85)),
        ((0.0, 0.65, 0.35), (0.5, 1.0, 1.0)),
        ((0.55, 0.15, 0.28), (1.0, 0.5, 0.85)),
        ((0.95, -0.55, 0.05), (1.0, 0.0, 0.0)),
    ]
    inner_pts = [
        ((-0.60, -0.75, 0.02), (0.0, 0.0, 0.0)),
        ((-0.30, -0.15, 0.12), (0.2, 0.3, 0.7)),
        ((0.0, 0.22, 0.16), (0.5, 0.6, 1.0)),
        ((0.30, -0.15, 0.12), (0.8, 0.3, 0.7)),
        ((0.60, -0.75, 0.02), (1.0, 0.0, 0.0)),
    ]
    outer_v = [(bm.verts.new(p), uv, a) for p, (u, v, a) in wings for uv in [(u, 1.0)]]
    inner_v = [(bm.verts.new(p), uv, a) for p, (u, v, a) in inner_pts for uv in [(u, 0.0)]]
    for i in range(len(wings) - 1):
        vo0, uvo0, ao0 = outer_v[i]
        vo1, uvo1, ao1 = outer_v[i + 1]
        vi1, uvi1, ai1 = inner_v[i + 1]
        vi0, uvi0, ai0 = inner_v[i]
        f = bm.faces.new((vi0, vi1, vo1, vo0))
        for loop, (uv, a) in zip(f.loops, [(uvi0, ai0), (uvi1, ai1), (uvo1, ao1), (uvo0, ao0)]):
            set_loop(loop, uv_layer, col_layer, uv[0], uv[1], a)
    return make_bmesh_object("VfxChargeChevron", bm, smooth=True)


def build_shield_dome():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    segs = 20
    lat_rings = 6
    grid = []
    for j in range(lat_rings + 1):
        v = j / lat_rings
        phi = v * (math.pi * 0.5)
        r = math.cos(phi)
        z = math.sin(phi)
        a = 0.15 if j == 0 else (1.0 if j < lat_rings else 0.85)
        row = []
        for i in range(segs + 1):
            u = i / segs
            ang = u * math.tau
            vert = bm.verts.new((math.cos(ang) * r, math.sin(ang) * r, z))
            row.append((vert, u, v, a))
        grid.append(row)

    for j in range(lat_rings):
        for i in range(segs):
            v00, u0, v0, a00 = grid[j][i]
            v10, u1, _, a10 = grid[j][i + 1]
            v11, _, v1, a11 = grid[j + 1][i + 1]
            v01, _, _, a01 = grid[j + 1][i]
            if j == lat_rings - 1:
                f = bm.faces.new((v00, v10, v01))
                for loop, (u, v, a) in zip(f.loops, [(u0, v0, a00), (u1, v0, a10), (u0, v1, a01)]):
                    set_loop(loop, uv_layer, col_layer, u, v, a)
            else:
                f = bm.faces.new((v00, v10, v11, v01))
                for loop, (u, v, a) in zip(f.loops, [(u0, v0, a00), (u1, v0, a10), (u1, v1, a11), (u0, v1, a01)]):
                    set_loop(loop, uv_layer, col_layer, u, v, a)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0001)
    return make_bmesh_object("VfxShieldDome", bm, smooth=False)


def build_lantern_orb():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.35)
    for v in bm.verts:
        # Teardrop flame elongation along Z (Unity +Y)
        if v.co.z > 0:
            t = v.co.z / 0.35
            v.co.x *= 1.0 - 0.35 * t
            v.co.y *= 1.0 - 0.35 * t
            v.co.z *= 1.35
    for f in bm.faces:
        for loop in f.loops:
            p = loop.vert.co
            u = (math.atan2(p.y, p.x) / math.tau) + 0.5
            v_coord = max(0.0, min(1.0, (p.z + 0.35) / 0.82))
            set_loop(loop, uv_layer, col_layer, u, v_coord, 1.0)
    return make_bmesh_object("VfxLanternOrb", bm, smooth=True)


def build_bolt_streak():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    # Crossed tapered diamond blades along +Y (Unity +Z) from -0.55 to +0.55
    profile = [(-0.55, 0.02, 0.0), (-0.15, 0.14, 0.85), (0.25, 0.10, 1.0), (0.55, 0.01, 0.4)]
    for tilt in (0.0, math.pi * 0.5):
        ct, st = math.cos(tilt), math.sin(tilt)
        rows = []
        for idx, (y, w, a) in enumerate(profile):
            u = idx / (len(profile) - 1)
            vl = bm.verts.new((-w * ct, y, -w * st))
            vr = bm.verts.new((w * ct, y, w * st))
            rows.append((vl, vr, u, a))
        for i in range(len(rows) - 1):
            l0, r0, u0, a0 = rows[i]
            l1, r1, u1, a1 = rows[i + 1]
            f = bm.faces.new((l0, r0, r1, l1))
            for loop, (u, v, a) in zip(f.loops, [(u0, 0.0, a0), (u0, 1.0, a0), (u1, 1.0, a1), (u1, 0.0, a1)]):
                set_loop(loop, uv_layer, col_layer, u, v, a)
    return make_bmesh_object("VfxBoltStreak", bm, smooth=True)


def build_powder_keg():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    # Low-poly barrel (material 0=Wood, 1=Iron, 2=Fuse)
    segs = 12
    profile = [
        (0.22, 0.00, 0),
        (0.28, 0.14, 1),
        (0.29, 0.22, 1),
        (0.31, 0.36, 0),
        (0.29, 0.50, 1),
        (0.28, 0.58, 1),
        (0.22, 0.72, 0),
    ]
    rings = []
    for j, (r, z, mat_idx) in enumerate(profile):
        ring = []
        v = j / (len(profile) - 1)
        for i in range(segs + 1):
            u = i / segs
            ang = u * math.tau
            vert = bm.verts.new((math.cos(ang) * r, math.sin(ang) * r, z))
            ring.append((vert, u, v, mat_idx))
        rings.append(ring)

    for j in range(len(profile) - 1):
        mat_idx = profile[j][2] if profile[j][2] == profile[j + 1][2] else 0
        for i in range(segs):
            v00, u0, v0, _ = rings[j][i]
            v10, u1, _, _ = rings[j][i + 1]
            v11, _, v1, _ = rings[j + 1][i + 1]
            v01, _, _, _ = rings[j + 1][i]
            f = bm.faces.new((v00, v10, v11, v01))
            f.material_index = mat_idx
            for loop, (u, v) in zip(f.loops, [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]):
                set_loop(loop, uv_layer, col_layer, u, v, 1.0)

    # Top and bottom caps
    for ring_idx, z, flip, m_idx in ((0, 0.0, True, 0), (-1, 0.72, False, 0)):
        c = bm.verts.new((0.0, 0.0, z))
        ring = rings[ring_idx]
        for i in range(segs):
            va = ring[i][0]
            vb = ring[i + 1][0]
            f = bm.faces.new((c, vb, va) if flip else (c, va, vb))
            f.material_index = m_idx
            for idx, loop in enumerate(f.loops):
                set_loop(loop, uv_layer, col_layer, 0.5, 0.5, 1.0)

    # Fuse cylinder sticking out top
    ret = bmesh.ops.create_cone(
        bm, cap_ends=True, cap_tris=False, segments=6,
        radius1=0.025, radius2=0.015, depth=0.18,
        matrix=Matrix.Translation((0.04, 0.0, 0.80)) @ Matrix.Rotation(0.25, 4, "Y")
    )
    for vert in ret["verts"]:
        for f in vert.link_faces:
            f.material_index = 2
            for loop in f.loops:
                set_loop(loop, uv_layer, col_layer, 0.5, 0.5, 1.0)

    return make_bmesh_object("VfxPowderKeg", bm, material_names=("Wood", "Iron", "Fuse"), smooth=False)


def build_flask_projectile():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    segs = 10
    profile = [
        (0.08, 0.00, 1),
        (0.15, 0.09, 1),
        (0.14, 0.19, 1),
        (0.06, 0.25, 0),
        (0.07, 0.32, 0),
        (0.04, 0.35, 0),
    ]
    rings = []
    for j, (r, z, m_idx) in enumerate(profile):
        ring = []
        v = j / (len(profile) - 1)
        for i in range(segs + 1):
            u = i / segs
            ang = u * math.tau
            ring.append((bm.verts.new((math.cos(ang) * r, math.sin(ang) * r, z)), u, v))
        rings.append((ring, m_idx))

    for j in range(len(rings) - 1):
        r0, m0 = rings[j]
        r1, _ = rings[j + 1]
        for i in range(segs):
            v00, u0, v0 = r0[i]
            v10, u1, _ = r0[i + 1]
            v11, _, v1 = r1[i + 1]
            v01, _, _ = r1[i]
            f = bm.faces.new((v00, v10, v11, v01))
            f.material_index = m0
            for loop, (u, v) in zip(f.loops, [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]):
                set_loop(loop, uv_layer, col_layer, u, v, 1.0)
    return make_bmesh_object("VfxFlaskProjectile", bm, material_names=("Glass", "Liquid"), smooth=False)


def build_heal_pillar():
    bm = bmesh.new()
    uv_layer, col_layer = ensure_uv_and_vc(bm)
    segs = 24
    levels = [(0.75, 0.0, 0.0), (0.78, 0.5, 1.0), (0.86, 1.8, 0.85), (0.95, 3.2, 0.0)]
    grid = []
    for j, (r, z, a) in enumerate(levels):
        v = j / (len(levels) - 1)
        row = []
        for i in range(segs + 1):
            u = i / segs
            ang = u * math.tau
            row.append((bm.verts.new((math.cos(ang) * r, math.sin(ang) * r, z)), u, v, a))
        grid.append(row)

    for j in range(len(levels) - 1):
        for i in range(segs):
            v00, u0, v0, a00 = grid[j][i]
            v10, u1, _, a10 = grid[j][i + 1]
            v11, _, v1, a11 = grid[j + 1][i + 1]
            v01, _, _, a01 = grid[j + 1][i]
            f = bm.faces.new((v00, v10, v11, v01))
            for loop, (u, v, a) in zip(f.loops, [(u0, v0, a00), (u1, v0, a10), (u1, v1, a11), (u0, v1, a01)]):
                set_loop(loop, uv_layer, col_layer, u, v, a)
    return make_bmesh_object("VfxHealPillar", bm, smooth=True)


BUILDERS = {
    "VfxSlashArc360": lambda: build_slash_arc("VfxSlashArc360", 360.0, 0.55, 1.0, 36, 3, tip_taper=False),
    "VfxSlashCone90": lambda: build_slash_arc("VfxSlashCone90", 100.0, 0.38, 1.0, 18, 3, tip_taper=True),
    "VfxShockwaveRing": build_shockwave_ring,
    "VfxGroundDisc": build_ground_disc,
    "VfxHookChain": build_hook_chain,
    "VfxChargeChevron": build_charge_chevron,
    "VfxShieldDome": build_shield_dome,
    "VfxLanternOrb": build_lantern_orb,
    "VfxBoltStreak": build_bolt_streak,
    "VfxPowderKeg": build_powder_keg,
    "VfxFlaskProjectile": build_flask_projectile,
    "VfxHealPillar": build_heal_pillar,
}


def export_obj(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(OUT, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        colors_type="SRGB",
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )
    return path


def main():
    names = ONLY if ONLY else list(BUILDERS.keys())
    exported = []
    for name in names:
        fn = BUILDERS.get(name)
        if fn is None:
            raise KeyError(f"Unknown VFX mesh: {name}")
        reset()
        obj = fn()
        vcount = len(obj.data.vertices)
        assert vcount <= 1500, f"{name} exceeded 1500 vertices ({vcount})"
        path = export_obj(obj)
        exported.append((name, vcount, path))
    print(f"[build_vfx_meshes] Exported {len(exported)} meshes to {OUT}:")
    for name, vcount, path in exported:
        print(f"  {name}: {vcount} verts -> {os.path.basename(path)}")


if __name__ == "__main__":
    main()
