"""Cinderreach world surface textures: seamless, deterministic, numpy only.

    python3 docs/world-art/build_zone_textures.py [--out sandbox/Assets/Art/Zones/Textures] [--only name,...]
            [--preview <dir>]

Per surface it writes
    <Name>_Albedo.png   RGBA, sRGB colour, alpha = smoothness (URP Lit "Albedo Alpha" channel)
    <Name>_Height.png   8-bit grey height, imported as normal map (Unity converts from greyscale)
    <Name>_Emission.png only for glowing surfaces (Kiln basalt veins)

Every noise is evaluated on an integer-periodic lattice, so all maps tile without seams.
Textures are authored for world-space box projection (ZoneSurfaceBuilder): V is world up on
vertical faces, so strata, soot and damp streaks run the right way on walls.
--preview writes a 2x2 tiled JPEG per surface for review.
"""
import argparse
import os

import numpy as np
from PIL import Image

# --------------------------------------------------------------------------- noise core


def grid(size):
    v, u = np.mgrid[0:size, 0:size].astype(np.float32) / size
    return u, v


def _fade(t):
    return t * t * t * (t * (t * 6 - 15) + 10)


def value_noise(u, v, freq, rng, freq_v=None):
    """Periodic value noise in [0,1]; u, v in [0,1) tile at integer freq (freq_v: other freq along V)."""
    fv = freq_v or freq
    lat = rng.random((fv, freq)).astype(np.float32)
    x, y = u * freq, v * fv
    x0, y0 = np.floor(x).astype(np.int64), np.floor(y).astype(np.int64)
    fx, fy = _fade(x - x0), _fade(y - y0)
    x0 %= freq
    y0 %= fv
    x1, y1 = (x0 + 1) % freq, (y0 + 1) % fv
    a = lat[y0, x0] * (1 - fx) + lat[y0, x1] * fx
    b = lat[y1, x0] * (1 - fx) + lat[y1, x1] * fx
    return a * (1 - fy) + b * fy


def fbm(u, v, freq, rng, octaves=5, gain=0.5):
    total, amp, norm = 0.0, 1.0, 0.0
    for o in range(octaves):
        total = total + value_noise(u, v, freq * 2 ** o, rng) * amp
        norm += amp
        amp *= gain
    return total / norm


def ridged(u, v, freq, rng, octaves=4):
    n = fbm(u, v, freq, rng, octaves)
    return 1.0 - np.abs(n * 2 - 1)


def voronoi(u, v, cells, rng, jitter=0.85):
    """Periodic cellular noise: (F1, F2, cell id) with distances in cell units."""
    pts = rng.random((cells, cells, 2)).astype(np.float32) * jitter + (1 - jitter) / 2
    ids = rng.random((cells, cells)).astype(np.float32)
    x, y = u * cells, v * cells
    cx, cy = np.floor(x).astype(np.int64), np.floor(y).astype(np.int64)
    f1 = np.full(u.shape, 9.0, np.float32)
    f2 = np.full(u.shape, 9.0, np.float32)
    cid = np.zeros(u.shape, np.float32)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            nx, ny = cx + dx, cy + dy
            px = nx + pts[ny % cells, nx % cells, 0]
            py = ny + pts[ny % cells, nx % cells, 1]
            d = np.sqrt((x - px) ** 2 + (y - py) ** 2)
            closer = d < f1
            f2 = np.where(closer, f1, np.minimum(f2, d))
            cid = np.where(closer, ids[ny % cells, nx % cells], cid)
            f1 = np.where(closer, d, f1)
    return f1, f2, cid


def warp(u, v, amount, freq, rng):
    du = (fbm(u, v, freq, rng, 4) - 0.5) * amount
    dv = (fbm(u, v, freq, rng, 4) - 0.5) * amount
    return (u + du) % 1.0, (v + dv) % 1.0


def blocks(u, v, rows, rng, min_w=0.6, max_w=1.4, cols=None, row_jitter=0.0):
    """Running-bond blocks: returns (edge distance in block-height units, block id, row index).

    Each row is cut into blocks of random width; cuts are periodic so the pattern tiles.
    """
    cols = cols or rows
    y = v * rows
    row = np.floor(y).astype(np.int64) % rows
    fy = y - np.floor(y)
    edge = np.full(u.shape, 9.0, np.float32)
    bid = np.zeros(u.shape, np.float32)
    for r in range(rows):
        widths = rng.uniform(min_w, max_w, cols)
        cuts = np.concatenate([[0.0], np.cumsum(widths)])
        cuts = cuts / cuts[-1]
        shift = rng.random()
        ids = rng.random(cols)
        mask = row == r
        if not mask.any():
            continue
        x = (u[mask] + shift) % 1.0
        k = np.clip(np.searchsorted(cuts, x, side="right") - 1, 0, cols - 1)
        dx = np.minimum(x - cuts[k], cuts[k + 1] - x) * cols  # in block widths ~ rows units
        dyr = np.minimum(fy[mask], 1 - fy[mask])
        edge[mask] = np.minimum(dx * (rows / cols), dyr)
        bid[mask] = ids[k]
    return edge, bid, row


# --------------------------------------------------------------------------- helpers


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


def mix(a, b, t):
    t = np.asarray(t, np.float32)[..., None] if np.ndim(t) else t
    return a * (1 - t) + b * t


def sstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def solid(shape, c):
    return np.broadcast_to(c, shape + (3,)).astype(np.float32).copy()


def shade(col, k):
    return col * np.asarray(k, np.float32)[..., None]


def ao_from_height(h, rng_unused=None, radius=6):
    """Cheap cavity term: height minus its periodic box blur."""
    blur = h.copy()
    for axis in (0, 1):
        acc = np.zeros_like(blur)
        for s in range(-radius, radius + 1):
            acc += np.roll(blur, s, axis=axis)
        blur = acc / (2 * radius + 1)
    return np.clip(0.5 + (h - blur) * 3.0, 0, 1)


# --------------------------------------------------------------------------- surfaces
# Each returns (albedo RGB [0,1] linear-ish sRGB values, smoothness [0,1], height [0,1], emission or None)


def moor_ground(n, rng):
    """Ashen Fen: peat and moss, heather tint, dry grass grit, pebbles, wet hollows."""
    u, v = grid(n)
    wu, wv = warp(u, v, 0.25, 3, rng)
    patches = fbm(wu, wv, 4, rng, 5)
    detail = fbm(u, v, 32, rng, 3)
    peat, moss, heath, dry = hexc("#33281e"), hexc("#3d4a28"), hexc("#4f5531"), hexc("#6b6440")
    col = mix(peat, moss, sstep(0.38, 0.62, patches))
    col = mix(col, heath, sstep(0.55, 0.8, fbm(u, v, 8, rng, 4)) * 0.55)
    # dry grass blades: stretched noise in two directions
    blades = np.maximum(value_noise(u, v, 128, rng, freq_v=16), value_noise(u, v, 16, rng, freq_v=128))
    col = mix(col, dry, sstep(0.55, 0.85, blades) * 0.4 * sstep(0.4, 0.7, patches))
    # pebbles
    f1, f2, cid = voronoi(*warp(u, v, 0.04, 16, rng), 28, rng)
    pebble = sstep(0.3, 0.16, f1) * (cid > 0.88) * sstep(0.35, 0.6, 1 - patches)
    pebble_col = shade(mix(hexc("#4a4640"), hexc("#6a645b"), cid), 0.7 + 0.3 * sstep(0.3, 0.0, f1))
    col = mix(col, pebble_col, pebble * 0.85)
    # wet hollows: darker, glossier
    wet = sstep(0.68, 0.82, fbm(wu, wv, 3, rng, 3))
    col = shade(col, (0.82 + detail * 0.36) * (1 - wet * 0.35))
    height = 0.45 * patches + 0.25 * detail + 0.3 * pebble - 0.2 * wet
    smooth = 0.06 + wet * 0.45 + pebble * 0.08
    return col, smooth, height, None


def crypt_flagstone(n, rng):
    """Sunken Ossuary floor: worn slate flags, grimy joints, bone dust, hairline cracks."""
    u, v = grid(n)
    edge, bid, row = blocks(u, v, 4, rng, 0.7, 1.5, cols=4)
    wear = fbm(u, v, 16, rng, 4)
    base = mix(hexc("#2c2a33"), hexc("#46424d"), bid)
    base = mix(base, hexc("#3a3a3a"), fbm(u, v, 6, rng) * 0.5)
    joint = sstep(0.045, 0.015, edge)
    bevel = sstep(0.0, 0.09, edge)
    col = shade(base, 0.75 + wear * 0.45)
    col = mix(col, hexc("#141217"), joint * 0.9)
    cf1, cf2, _ = voronoi(*warp(u, v, 0.08, 8, rng), 7, rng)
    crack = sstep(0.035, 0.0, cf2 - cf1) * sstep(0.55, 0.7, fbm(u, v, 5, rng)) * (1 - joint)
    col = mix(col, hexc("#16141a"), crack * 0.85)
    dust = sstep(0.62, 0.8, fbm(u, v, 10, rng, 5)) * (0.5 + 0.5 * joint)
    col = mix(col, hexc("#8f8676"), dust * 0.45)
    specks = (value_noise(u, v, 256, rng) > 0.93) * 0.5
    col = mix(col, hexc("#b5aa94"), specks * dust)
    height = bevel * (0.75 + 0.25 * bid) + wear * 0.15 - crack * 0.35
    smooth = 0.12 + (1 - dust) * 0.18 * bevel + sstep(0.7, 0.9, wear) * 0.15
    return col, smooth, height, None


def crypt_ashlar(n, rng):
    """Ossuary pillars and sarcophagi: dressed ashlar, damp streaks running down, nitre bloom."""
    u, v = grid(n)
    edge, bid, row = blocks(u, v, 6, rng, 0.8, 1.6, cols=4)
    base = mix(hexc("#34313b"), hexc("#504a55"), bid)
    grain = fbm(u, v, 24, rng, 4)
    col = shade(base, 0.78 + grain * 0.4)
    mortar = sstep(0.06, 0.02, edge)
    col = mix(col, hexc("#1b191e"), mortar * 0.85)
    # damp streaks: noise stretched along V (world up)
    streak = value_noise(u, v, 64, rng, freq_v=4)  # long runs down the wall
    streak = sstep(0.3, 0.6, streak * fbm(u, v, 4, rng))
    col = mix(col, hexc("#1f2422"), streak * 0.6)
    nitre = sstep(0.7, 0.85, fbm(u, v, 12, rng, 5)) * (0.4 + 0.6 * mortar)
    col = mix(col, hexc("#9d9a8e"), nitre * 0.35)
    chips = sstep(0.75, 0.9, ridged(u, v, 10, rng)) * sstep(0.12, 0.05, edge)
    col = shade(col, 1 - chips * 0.3)
    height = sstep(0.0, 0.12, edge) * 0.8 + grain * 0.2 - chips * 0.3
    smooth = 0.1 + streak * 0.35
    return col, smooth, height, None


def kiln_basalt(n, rng):
    """Cinder Kiln floor: cracked basalt plates over molten veins, ash drifts."""
    u, v = grid(n)
    wu, wv = warp(u, v, 0.12, 4, rng)
    f1, f2, cid = voronoi(wu, wv, 9, rng)
    gap = f2 - f1
    crack = sstep(0.06, 0.0, gap)
    plate = sstep(0.0, 0.25, gap)
    rough = fbm(u, v, 20, rng, 5)
    base = mix(hexc("#1c1716"), hexc("#3a2f2b"), cid * 0.7 + rough * 0.3)
    col = shade(base, 0.7 + rough * 0.5)
    ash = sstep(0.55, 0.78, fbm(u, v, 6, rng, 5)) * plate
    col = mix(col, hexc("#6e655f"), ash * 0.55)
    heat = sstep(0.45, 0.8, fbm(u, v, 3, rng, 3))
    vein = crack * (0.12 + 0.88 * heat)
    col = mix(col, hexc("#2a0d05"), crack * 0.85)
    glow_col = mix(hexc("#c22a04"), hexc("#ff8a2a"), sstep(0.025, 0.0, gap) * heat)
    emission = glow_col * (vein * (0.6 + 0.4 * fbm(u, v, 24, rng, 2)))[..., None]
    height = plate * 0.8 + rough * 0.2 - crack * 0.5
    smooth = 0.18 + (1 - ash) * 0.22 * plate
    return col, smooth, height, emission


def kiln_brick(n, rng):
    """Kiln domes: firebrick courses scorched black towards the top, soot runs and slag drips."""
    u, v = grid(n)
    edge, bid, row = blocks(u, v, 10, rng, 0.9, 1.2, cols=5)
    base = mix(hexc("#5a2a1c"), hexc("#7d4630"), bid)
    grain = fbm(u, v, 32, rng, 4)
    col = shade(base, 0.75 + grain * 0.45)
    mortar = sstep(0.08, 0.03, edge)
    col = mix(col, hexc("#2a2420"), mortar * 0.85)
    soot = sstep(0.35, 0.75, 0.6 * fbm(u, v, 5, rng, 5) + 0.4 * value_noise(u, v, 24, rng, freq_v=2))
    col = mix(col, hexc("#141010"), soot * 0.75)
    glaze = sstep(0.72, 0.9, fbm(u, v, 14, rng))
    col = mix(col, hexc("#2f1f1a"), glaze * 0.5)
    height = sstep(0.0, 0.14, edge) * 0.85 + grain * 0.15
    smooth = 0.08 + glaze * 0.5
    return col, smooth, height, None


def slag(n, rng):
    """Cooled slag heaps: glassy black clinker with pitted vesicles."""
    u, v = grid(n)
    f1, f2, cid = voronoi(u, v, 18, rng)
    pits = sstep(0.3, 0.08, f1) * (cid > 0.55) * (0.4 + 0.6 * fbm(u, v, 6, rng))
    lumps = fbm(u, v, 8, rng, 5)
    col = mix(hexc("#121011"), hexc("#2e2624"), lumps)
    col = mix(col, hexc("#3b1a0e"), sstep(0.65, 0.85, fbm(u, v, 5, rng)) * 0.6)
    col = shade(col, 1 - pits * 0.45)
    height = lumps * 0.7 - pits * 0.4 + 0.3
    smooth = 0.55 - pits * 0.4
    return col, smooth, height, None


def barrens_sand(n, rng):
    """Glass Barrens: pale ash sand, wind ripples, scattered glass grit that glints."""
    u, v = grid(n)
    wu, wv = warp(u, v, 0.3, 2, rng)
    ripple = 0.5 + 0.5 * np.sin((wv * 14 + fbm(u, v, 3, rng) * 3) * 2 * np.pi)
    ripple = ripple ** 2 * sstep(0.3, 0.7, fbm(u, v, 2, rng, 3))
    drift = fbm(wu, wv, 3, rng, 5)
    col = mix(hexc("#9a9384"), hexc("#c2b8a3"), drift)
    col = mix(col, hexc("#7d776d"), sstep(0.6, 0.85, fbm(u, v, 5, rng)) * 0.25)
    col = shade(col, 0.95 + ripple * 0.07)
    grain = value_noise(u, v, 384, rng)
    col = shade(col, 0.93 + grain * 0.12)
    glass = (value_noise(u, v, 256, rng) > 0.965).astype(np.float32)
    glass *= sstep(0.4, 0.7, fbm(u, v, 6, rng))
    col = mix(col, hexc("#7fd8d2"), glass * 0.7)
    height = ripple * 0.2 + drift * 0.65 + glass * 0.15
    smooth = 0.08 + glass * 0.85
    return col, smooth, height, None


def barrens_stone(n, rng):
    """Glass Barrens spires: wind-cut sandstone strata with glass seams."""
    u, v = grid(n)
    wu, wv = warp(u, v, 0.08, 4, rng)
    layer = wv * 14 + fbm(u, v, 3, rng) * 1.5
    band = np.floor(layer) % 14
    tone = np.sin(band * 12.9898) * 43758.5453 % 1.0
    tone = np.abs(tone)
    base = mix(hexc("#6d6457"), hexc("#a3967f"), tone)
    erode = fbm(u, v, 18, rng, 5)
    col = shade(base, 0.75 + erode * 0.45)
    seam = sstep(0.05, 0.0, np.abs(layer - np.round(layer))) * sstep(0.5, 0.7, fbm(u, v, 4, rng))
    col = mix(col, hexc("#4b4840"), seam * 0.7)
    glass_seam = (band % 5 == 0) * sstep(0.6, 0.8, fbm(u, v, 8, rng))
    col = mix(col, hexc("#5fb6b0"), glass_seam * 0.55)
    height = erode * 0.6 + (layer % 1.0) * 0.25 - seam * 0.3
    smooth = 0.1 + glass_seam * 0.7
    return col, smooth, height, None


def planks(n, rng):
    """Weathered boards (contract board, ossuary torch posts)."""
    u, v = grid(n)
    edge, bid, row = blocks(v, u, 5, rng, 2.0, 4.0, cols=2)  # boards run along V
    grain = value_noise(u, v, 128, rng, freq_v=8)
    streak = 0.5 + 0.5 * np.sin((u * 5 + fbm(u, v, 6, rng) * 0.8 + bid) * 2 * np.pi * 7)
    base = mix(hexc("#3e2b1c"), hexc("#6a4b2f"), bid * 0.6 + streak * 0.4)
    col = shade(base, 0.75 + grain * 0.4)
    gap = sstep(0.05, 0.015, edge)
    col = mix(col, hexc("#140d08"), gap)
    weather = sstep(0.6, 0.85, fbm(u, v, 5, rng))
    col = mix(col, hexc("#6f685e"), weather * 0.35)
    height = sstep(0.0, 0.08, edge) * 0.7 + streak * 0.3
    smooth = 0.12
    return col, smooth, height, None


# Prop sets: the palette colour of the art bible stays the mid-tone, the texture adds material detail.


def _prop(n, rng, mid, kind):
    u, v = grid(n)
    c = hexc(mid)
    if kind == "stone":
        f1, f2, cid = voronoi(u, v, 6, rng)
        rough = fbm(u, v, 12, rng, 5)
        cr = sstep(0.04, 0.0, f2 - f1)
        lich = sstep(0.68, 0.82, fbm(u, v, 7, rng, 5))
        col = shade(np.broadcast_to(c, u.shape + (3,)), 0.72 + rough * 0.5 + (cid - 0.5) * 0.12)
        col = mix(col, c * 0.35, cr * 0.8)
        col = mix(col, hexc("#7b8a5a"), lich * 0.4)
        return col, 0.1 + lich * 0.0, rough * 0.8 - cr * 0.4, None
    if kind == "bark":
        fib = value_noise(u, v, 96, rng, freq_v=6)
        ridges = 0.5 + 0.5 * np.sin((u * 18 + fbm(u, v, 4, rng) * 2.5) * 2 * np.pi)
        col = shade(np.broadcast_to(c, u.shape + (3,)), 0.55 + ridges * 0.6 + fib * 0.2)
        col = mix(col, hexc("#3a3f2a"), sstep(0.65, 0.85, fbm(u, v, 5, rng)) * 0.35)
        return col, 0.06, ridges * 0.8, None
    if kind == "wood":
        ring = 0.5 + 0.5 * np.sin((u * 9 + fbm(u, v, 5, rng) * 1.6) * 2 * np.pi * 3)
        fine = value_noise(u, v, 160, rng)
        col = shade(np.broadcast_to(c, u.shape + (3,)), 0.7 + ring * 0.35 + fine * 0.15)
        return col, 0.14, ring * 0.5 + fine * 0.2, None
    if kind == "moss":
        clump = fbm(u, v, 12, rng, 4) * 0.7 + fbm(u, v, 48, rng, 2) * 0.3
        col = shade(np.broadcast_to(c, u.shape + (3,)), 0.6 + clump * 0.7)
        col = mix(col, hexc("#7d8a3e"), sstep(0.65, 0.85, clump) * 0.4)
        return col, 0.04, clump, None
    if kind == "bone":
        pores = value_noise(u, v, 200, rng)
        stain = fbm(u, v, 6, rng, 5)
        col = shade(np.broadcast_to(c, u.shape + (3,)), 0.85 + pores * 0.12)
        col = mix(col, hexc("#7d6a4c"), sstep(0.5, 0.85, stain) * 0.55)
        return col, 0.3 - stain * 0.15, stain * 0.4 + pores * 0.2, None
    if kind == "iron":
        hammer = fbm(u, v, 16, rng, 4)
        rust = sstep(0.62, 0.85, fbm(u, v, 6, rng, 5))
        col = shade(np.broadcast_to(c, u.shape + (3,)), 0.8 + hammer * 0.4)
        col = mix(col, hexc("#4a2a18"), rust * 0.6)
        return col, 0.55 - rust * 0.45, hammer * 0.5 - rust * 0.2, None
    if kind == "cloth":
        weave = (0.5 + 0.25 * np.sin(u * n * np.pi / 2) + 0.25 * np.sin(v * n * np.pi / 2))
        dirt = fbm(u, v, 5, rng, 5)
        col = shade(np.broadcast_to(c, u.shape + (3,)), 0.75 + weave * 0.25)
        col = mix(col, c * 0.35, sstep(0.55, 0.85, dirt) * 0.6)
        return col, 0.1, weave * 0.4, None
    raise ValueError(kind)


SURFACES = {
    # name: (generator or (mid, kind), size, seed)
    "FenMoor": (moor_ground, 1024, 11),
    "CryptFlagstone": (crypt_flagstone, 1024, 21),
    "CryptAshlar": (crypt_ashlar, 1024, 22),
    "KilnBasalt": (kiln_basalt, 1024, 31),
    "KilnBrick": (kiln_brick, 1024, 32),
    "Slag": (slag, 512, 33),
    "BarrensSand": (barrens_sand, 1024, 41),
    "BarrensStone": (barrens_stone, 1024, 42),
    "Planks": (planks, 512, 51),
    "Stone": (("#5A5560", "stone"), 512, 61),
    "Bark": (("#4A3220", "bark"), 512, 62),
    "DarkWood": (("#5A3A22", "wood"), 512, 63),
    "Moss": (("#46592E", "moss"), 512, 64),
    "Bone": (("#D8CCB0", "bone"), 512, 65),
    "Iron": (("#3A3A40", "iron"), 512, 66),
    "BloodCloth": (("#8E1620", "cloth"), 512, 67),
}


def build(name, out, preview=None):
    gen, size, seed = SURFACES[name]
    rng = np.random.default_rng(seed)
    if callable(gen):
        col, smooth, height, emission = gen(size, rng)
    else:
        col, smooth, height, emission = _prop(size, rng, gen[0], gen[1])
    height = np.asarray(height, np.float32)
    height = (height - height.min()) / max(1e-6, float(height.max() - height.min()))
    cavity = ao_from_height(height)
    col = shade(col, 0.8 + 0.2 * cavity * 2 - 0.2)  # bake crevice darkening
    smooth = np.broadcast_to(np.asarray(smooth, np.float32), height.shape)
    rgba = np.dstack([np.clip(col, 0, 1), np.clip(smooth, 0, 1)])
    os.makedirs(out, exist_ok=True)
    Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA").save(f"{out}/{name}_Albedo.png", optimize=True)
    Image.fromarray((height * 255 + 0.5).astype(np.uint8), "L").save(f"{out}/{name}_Height.png", optimize=True)
    if emission is not None:
        Image.fromarray((np.clip(emission, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB").save(
            f"{out}/{name}_Emission.png", optimize=True)
    if preview:
        os.makedirs(preview, exist_ok=True)
        rgb = np.clip(col, 0, 1)
        if emission is not None:
            rgb = np.clip(rgb + emission * 0.9, 0, 1)
        tiled = np.tile(rgb, (2, 2, 1))
        Image.fromarray((tiled * 255).astype(np.uint8)).resize((768, 768), Image.LANCZOS).save(
            f"{preview}/{name}.jpg", quality=88)
    print(f"{name}: {size}px{' +emission' if emission is not None else ''}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="sandbox/Assets/Art/Zones/Textures")
    ap.add_argument("--only", default="")
    ap.add_argument("--preview", default="")
    a = ap.parse_args()
    names = [s for s in a.only.split(",") if s] or list(SURFACES)
    for name in names:
        build(name, a.out, a.preview or None)


if __name__ == "__main__":
    main()
