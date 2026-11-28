"""Shared low-poly part builder for Cinderreach hero/boss redesigns.

Every part is a separate mesh object bound to the KayKit armature through an
Armature modifier. Rigid parts get one vertex group at weight 1.0; parts that
span several bones (torso, cape) are split by height so the KayKit
animations deform them the same way as the original segmented meshes.
"""
import bmesh
import bpy
from mathutils import Matrix, Vector

COLL = "Redesign"
# The shared Blender can host other sessions' scenes and switch the window
# scene; always target this scene explicitly, never bpy.context.scene.
SCENE = "Scene"


def scene():
    return bpy.data.scenes[SCENE]


def mat(name, rgb, rough=0.8, metal=0.0, emit=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emit:
        key = "Emission Color" if "Emission Color" in b.inputs else "Emission"
        b.inputs[key].default_value = (*emit, 1)
        b.inputs["Emission Strength"].default_value = 2.0
    m.diffuse_color = (*rgb, 1)
    return m


def hexrgb(h):
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def collection():
    c = bpy.data.collections.get(COLL)
    if not c:
        c = bpy.data.collections.new(COLL)
        scene().collection.children.link(c)
    return c


def clear(prefix):
    for o in list(bpy.data.objects):
        if o.name.startswith(prefix):
            bpy.data.objects.remove(o, do_unlink=True)


def _finish(name, bm, material, arm, bones, split_axis_z=None, bevel=0.0):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    collection().objects.link(ob)
    me.materials.append(material)
    for p in me.polygons:
        p.use_smooth = False
    if bevel:
        mod = ob.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
    ob.parent = arm
    # Part coordinates are rig-local (Z up, origin at the rig root): undo only
    # the armature's FBX rotation so any rig location works.
    ob.matrix_parent_inverse = arm.matrix_world.to_3x3().inverted().to_4x4()
    groups = {b: ob.vertex_groups.new(name=b) for b in bones}
    if split_axis_z is None:
        groups[bones[0]].add(list(range(len(me.vertices))), 1.0, "REPLACE")
    else:
        # split_axis_z: list of (z_upper_bound, bone) sorted ascending
        for v in me.vertices:
            z = v.co.z
            for bound, bone in split_axis_z:
                if z <= bound:
                    groups[bone].add([v.index], 1.0, "REPLACE")
                    break
            else:
                groups[split_axis_z[-1][1]].add([v.index], 1.0, "REPLACE")
    m = ob.modifiers.new("Armature", "ARMATURE")
    m.object = arm
    return ob


def box(name, material, arm, bones, center, size, taper=1.0, bevel=0.02, split=None, rot=None):
    """Box with optional top taper (top face scaled by `taper`)."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        if v.co.z > 0:
            v.co.x *= taper
            v.co.y *= taper
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
        if rot:
            v.co = rot @ v.co
        v.co += Vector(center)
    return _finish(name, bm, material, arm, bones, split, bevel)


def cyl(name, material, arm, bones, center, radius, depth, verts=8, axis="Z", r2=None, bevel=0.015, split=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=verts, radius1=radius,
                          radius2=radius if r2 is None else r2, depth=depth)
    rot = {"Z": Matrix.Identity(3), "X": Matrix.Rotation(1.5708, 3, "Y"),
           "Y": Matrix.Rotation(1.5708, 3, "X")}[axis]
    for v in bm.verts:
        v.co = rot @ v.co + Vector(center)
    return _finish(name, bm, material, arm, bones, split, bevel)


def dome(name, material, arm, bones, center, scale, segs=8, rings=4, rot=None, bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings * 2, radius=1.0)
    for v in [v for v in bm.verts if v.co.z < -0.01]:
        bm.verts.remove(v)
    bmesh.ops.contextual_create(bm, geom=[e for e in bm.edges if e.is_boundary])
    for v in bm.verts:
        v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
        if rot:
            v.co = rot @ v.co
        v.co += Vector(center)
    return _finish(name, bm, material, arm, bones, None, bevel)


def ball(name, material, arm, bones, center, scale, segs=10, rings=6, bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2])) + Vector(center)
    return _finish(name, bm, material, arm, bones, None, bevel)


def cloak(name, material, arm, top_z, bottom_z, width, y, teeth=7, depth_jag=0.09, flare=1.25):
    """Tattered back cloak: a slightly curved strip with a zig-zag hem."""
    bm = bmesh.new()
    cols = teeth * 2 + 1
    top, bot = [], []
    for i in range(cols):
        t = i / (cols - 1)
        x = (t - 0.5) * width
        curve = 0.06 * (1 - (2 * t - 1) ** 2)
        top.append(bm.verts.new((x * 0.8, y + curve, top_z)))
        jag = depth_jag if i % 2 else 0.0
        bot.append(bm.verts.new((x * flare, y + 0.08 + curve, bottom_z + jag)))
    for i in range(cols - 1):
        bm.faces.new((top[i], top[i + 1], bot[i + 1], bot[i]))
    bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.025)
    split = [(0.60, "hips"), (0.97, "spine"), (9.0, "chest")]
    return _finish(name, bm, material, arm, ["hips", "spine", "chest"], split, 0.0)


def hide_originals(prefix):
    for o in bpy.data.objects:
        if o.type == "MESH" and o.name.startswith(prefix):
            o.hide_render = True
            o.hide_viewport = True


def render_turn(rig_name, out_path, size=800):
    """Render the rig's visible parts from a 3/4 front view to a PNG (approval evidence)."""
    scn = scene()
    rig = bpy.data.objects[rig_name]
    cam = scn.camera
    if cam is None:
        cam = bpy.data.objects.new("RenderCam", bpy.data.cameras.new("RenderCam"))
        scn.collection.objects.link(cam)
        scn.camera = cam
    base = rig.matrix_world.translation
    cam.location = base + Vector((3.2, -4.4, 2.6))
    look = (base + Vector((0, 0, 1.0))) - cam.location
    cam.rotation_euler = look.to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = 50
    # Cycles on CPU: EEVEE/Workbench read back black under WSLg.
    scn.render.engine = "CYCLES"
    scn.cycles.device = "CPU"
    scn.cycles.samples = 48
    scn.cycles.use_denoising = False  # Ubuntu build ships without OIDN
    if scn.world is None:
        scn.world = bpy.data.worlds.new("World")
    scn.world.use_nodes = True
    scn.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
    scn.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.25, 0.25, 0.28, 1)
    scn.render.resolution_x = scn.render.resolution_y = size
    scn.render.film_transparent = False
    scn.render.image_settings.file_format = "PNG"
    scn.render.filepath = out_path
    hidden = []
    for o in scn.objects:
        if o.type == "MESH" and not o.hide_render and o.parent is not rig:
            o.hide_render = True
            hidden.append(o)
    try:
        bpy.ops.render.render(write_still=True, scene=scn.name)
    finally:
        for o in hidden:
            o.hide_render = False
    return out_path


def braid(name, material, arm, points, radius=0.06, head_z=1.3):
    """Hair braid as a chain of low-poly beads; beads above head_z follow the
    head bone, lower ones the chest so the braid swings with the torso."""
    obs = []
    for i, (x, y, z) in enumerate(points):
        r = radius * (1.0 - 0.5 * i / max(1, len(points) - 1))
        bone = "head" if z >= head_z else "chest"
        obs.append(ball(f"{name}{i}", material, arm, [bone], (x, y, z), (r, r, r * 1.3), segs=6, rings=4))
    return obs


def import_base(fbx, rig_name, location):
    """Import a KayKit character into SCENE, rename its armature, place it."""
    before = set(bpy.data.objects.keys())
    with bpy.context.temp_override(window=bpy.context.window, scene=scene()):
        bpy.ops.import_scene.fbx(filepath=fbx)
    new = [bpy.data.objects[n] for n in set(bpy.data.objects.keys()) - before]
    rig = [o for o in new if o.type == "ARMATURE"][0]
    rig.name = rig_name
    rig.location = location
    return rig, [o for o in new if o.type == "MESH"]


def hide_meshes(obs):
    for o in obs:
        o.hide_render = True
        o.hide_viewport = True
