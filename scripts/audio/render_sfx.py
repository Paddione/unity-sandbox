#!/usr/bin/env python3
"""Render and deterministically master Cinderreach HQ SFX via Unsloth Studio GPU 0.

Pure Python stdlib (no numpy/ffmpeg dependency) so both rendering and unit tests
run directly under system python3. Produces the full 73-clip catalogue:
43 base cues from docs/audio/manifest.json + 30 surface/variant pool clips.

Usage::

    python3 scripts/audio/render_sfx.py [--only SwordHit1 Hit_Glass_0 ...] [--force] [--dry-run]
    python3 scripts/audio/render_sfx.py --from-wav SwordHit1=path/to/raw.wav
"""
from __future__ import annotations

import argparse
import array
import hashlib
import json
import math
import os
import struct
import sys
import time
import urllib.error
import urllib.request
import wave
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
MANIFEST_JSON = REPO / "docs" / "audio" / "manifest.json"
SFX_DIR = REPO / "sandbox" / "Assets" / "AddressableAssets" / "Sfx"
LOCK_JSON = REPO / "docs" / "audio" / "sfx.lock.json"

DEFAULT_URL = "http://127.0.0.1:8888"
DEFAULT_MODEL = "audio-cpp/audio.cpp-gguf/Stable-Audio-3-Small-SFX-GGUF"
DEFAULT_TOKEN_FILE = Path.home() / ".config" / "remote-workspace" / "studio-mcp.token"
STYLE_LINE = "dry, no reverb, close mic, dark medieval fantasy foley"

PEAK_CEILING_DBFS = -1.0
PEAK_CEILING_AMP = 10.0 ** (-1.0 / 20.0)  # ~=0.8913, hard cap checked by gate

CATEGORY_TARGET_RMS_DB = {
    "ui": -20.0,
    "combat": -16.0,
    "event": -17.0,
    "step": -22.0,
    "ambient": -24.0,
}

UI_CUES = {"UIClick", "VendorBuy", "VendorSell", "Loot", "Gold"}
EVENT_CUES = {"RareDrop", "LevelUp", "Gate", "Heal", "Quest", "Shrine"}
SHORT_COMBAT_CUES = {
    "Swing", "Hit", "SwordHit1", "SwordHit2", "SwordHit3",
    "Block", "Arrow", "Twang", "Crit", "Hurt", "Death",
    "MireHook", "ShadowRoll", "VenomVial", "SmokePot",
}
DEFAULT_DURATION_BY_CAT = {
    "ui": 1.0,
    "combat": 1.5,
    "event": 1.8,
    "step": 1.0,
    "ambient": 6.0,
}

# 30 surface/variant pool clips resolved dynamically by DiabloAudio.ResolveVariant
SFX_VARIANTS: list[dict] = [
    {"key": "Swing_0", "cue": "Swing", "category": "combat", "seed": 1244, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "light crisp weapon swing air whoosh, quick iron blade arc"},
    {"key": "Swing_1", "cue": "Swing", "category": "combat", "seed": 1245, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "medium weight sword slash whoosh, balanced air cut"},
    {"key": "Swing_2", "cue": "Swing", "category": "combat", "seed": 1246, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "heavy warhammer or greatsword overhead swing air rush, low chesty whoosh"},

    {"key": "Hit_FleshBone_0", "cue": "Hit", "category": "combat", "seed": 1250, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "heavy melee thud into undead flesh and brittle dry bone crack"},
    {"key": "Hit_FleshBone_1", "cue": "Hit", "category": "combat", "seed": 1251, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "melee cleave impact onto skeleton ribcage, dull thump and bone snap"},
    {"key": "Hit_FleshBone_2", "cue": "Hit", "category": "combat", "seed": 1252, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "heavy blunt strike onto corpse flesh and snapping knuckle bone"},

    {"key": "Hit_ArmorMetal_0", "cue": "Hit", "category": "combat", "seed": 1260, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "sword blade biting into dark iron plate armor, tight steel ring"},
    {"key": "Hit_ArmorMetal_1", "cue": "Hit", "category": "combat", "seed": 1261, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "heavy steel strike deflecting off knight pauldrons, mid iron clang"},
    {"key": "Hit_ArmorMetal_2", "cue": "Hit", "category": "combat", "seed": 1262, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "brutal iron mace blow against steel breastplate, low resonant metallic thud"},

    {"key": "Hit_StoneAsh_0", "cue": "Hit", "category": "combat", "seed": 1270, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "heavy weapon strike on cracked volcanic basalt and dry cinder ash"},
    {"key": "Hit_StoneAsh_1", "cue": "Hit", "category": "combat", "seed": 1271, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "stone crust impact with crumbling kiln slag gravel burst"},
    {"key": "Hit_StoneAsh_2", "cue": "Hit", "category": "combat", "seed": 1272, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "iron blade chopping into brittle charcoal rock and soot"},

    {"key": "Hit_Glass_0", "cue": "Hit", "category": "combat", "seed": 1280, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "sharp strike shattering cursed crystal glass shard, crisp brittle crunch"},
    {"key": "Hit_Glass_1", "cue": "Hit", "category": "combat", "seed": 1281, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "melee hit fracturing thick obsidian glass spire, crystalline crack"},
    {"key": "Hit_Glass_2", "cue": "Hit", "category": "combat", "seed": 1282, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "heavy blow smashing fused glass shell with sharp glassy scatter"},

    {"key": "Hit_GlassShield_0", "cue": "Hit", "category": "combat", "seed": 1290, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "glancing steel deflecting off thick crystal shield ward, resonant chime and knock"},
    {"key": "Hit_GlassShield_1", "cue": "Hit", "category": "combat", "seed": 1291, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "blocked strike against hardened glass barrier, dull knock with harmonic glass ring"},
    {"key": "Hit_GlassShield_2", "cue": "Hit", "category": "combat", "seed": 1292, "duration_s": 1.0, "max_duration_s": 0.55,
     "prompt": "ricochet off magical prism buckler, hard deflect thud and short bell ping"},

    {"key": "Footstep_Moor_0", "cue": "Footstep", "category": "step", "seed": 1300, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single heavy leather boot step onto damp dark peat moss and wet mud"},
    {"key": "Footstep_Moor_1", "cue": "Footstep", "category": "step", "seed": 1301, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single armored boot step pressing into soft soggy moorland soil"},
    {"key": "Footstep_Moor_2", "cue": "Footstep", "category": "step", "seed": 1302, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single muddy leather footstep squelching lightly in shallow bog turf"},

    {"key": "Footstep_Stone_0", "cue": "Footstep", "category": "step", "seed": 1310, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single leather boot sole tap on cold cathedral flagstone floor"},
    {"key": "Footstep_Stone_1", "cue": "Footstep", "category": "step", "seed": 1311, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single dry heel step onto worn limestone crypt slab"},
    {"key": "Footstep_Stone_2", "cue": "Footstep", "category": "step", "seed": 1312, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single solid boot step on dusty ancient stone tile"},

    {"key": "Footstep_Basalt_0", "cue": "Footstep", "category": "step", "seed": 1320, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single heavy boot step crunching volcanic basalt grit and slag"},
    {"key": "Footstep_Basalt_1", "cue": "Footstep", "category": "step", "seed": 1321, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single boot step across brittle kiln cinders and charred rock"},
    {"key": "Footstep_Basalt_2", "cue": "Footstep", "category": "step", "seed": 1322, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single leather step scraping hard black ashstone gravel"},

    {"key": "Footstep_Sand_0", "cue": "Footstep", "category": "step", "seed": 1330, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single boot step sinking softly into coarse desert sand and glass grit"},
    {"key": "Footstep_Sand_1", "cue": "Footstep", "category": "step", "seed": 1331, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single quick footstep crunching fine dry barrens sand"},
    {"key": "Footstep_Sand_2", "cue": "Footstep", "category": "step", "seed": 1332, "duration_s": 1.0, "max_duration_s": 0.35,
     "prompt": "single leather boot step brushing dry crystalline desert dunes"},
]


# Concrete Studio prompts for base cues whose manifest text is engine/meta
# (material lists, rotation notes, runtime references) rather than a sound.
# Without this the model renders quiet mush (e.g. Hit peak -15 dBFS).
STUDIO_PROMPT_OVERRIDES: dict[str, str] = {
    "Hit": "deep heavy melee impact thud with dry bone crunch and short dark steel ring, single hit",
    "Swing": "single warm airy melee sword swing whoosh, quick iron blade arc through air",
    "Footstep": "single heavy leather boot step on packed dirt ground, dry close thud",
    "Ambient": "seamless looping warm moor breeze with subtle low air bed, no birds, no voices, no music",
}


def cue_category(cue: str) -> str:
    if cue in UI_CUES:
        return "ui"
    if cue in EVENT_CUES:
        return "event"
    if cue == "Footstep" or cue.startswith("Footstep_") or cue.startswith("Step"):
        return "step"
    if cue == "Ambient":
        return "ambient"
    return "combat"


def default_max_duration_s(key: str, category: str) -> float | None:
    if category == "ambient":
        return None
    if key == "UIClick":
        return 0.25
    if category == "ui":
        return 0.55
    if category == "step":
        return 0.35
    if key in SHORT_COMBAT_CUES:
        return 0.65
    return 1.8


def build_sfx_prompt(entry: dict) -> str:
    """Combine manifest cue description with consistent Cinderreach acoustic style line."""
    key = entry.get("key", entry.get("cue", ""))
    if key in STUDIO_PROMPT_OVERRIDES:
        base = STUDIO_PROMPT_OVERRIDES[key]
    else:
        base = entry.get("prompt", entry.get("cue", entry.get("key", ""))).strip()
        # Strip engine/meta parentheticals that confuse the generative model
        # (e.g. "(DiabloAmbience bed, PlayCue no-op)", "(3-variant rotation)").
        if "(" in base and ")" in base:
            import re as _re

            base = _re.sub(r"\s*\([^)]*(?:bed|no-op|rotation|routing|variant)[^)]*\)", "", base).strip()
    return f"{base}, {STYLE_LINE}" if base else STYLE_LINE


def clip_hash(key: str, prompt: str, seed: int, model: str) -> str:
    blob = json.dumps([key, prompt, seed, model, STYLE_LINE], ensure_ascii=False)
    return hashlib.sha1(blob.encode("utf-8")).hexdigest()[:12]


def expected_clips(manifest: dict | None = None) -> list[dict]:
    """Return all 73 expected SFX entries (43 base manifest cues + 30 variants)."""
    if manifest is None:
        manifest = json.loads(MANIFEST_JSON.read_text(encoding="utf-8"))
    items: list[dict] = []
    for entry in manifest.get("sfx", []):
        cue = entry["cue"]
        cat = cue_category(cue)
        items.append({
            "key": cue,
            "cue": cue,
            "category": cat,
            "prompt": build_sfx_prompt(entry),
            "seed": int(entry.get("seed", 1234)),
            "duration_s": float(entry.get("duration_s", DEFAULT_DURATION_BY_CAT[cat])),
            "loop": cat == "ambient",
            "max_duration_s": entry.get("max_duration_s", default_max_duration_s(cue, cat)),
        })
    for var in SFX_VARIANTS:
        key = var["key"]
        cat = var["category"]
        items.append({
            "key": key,
            "cue": var["cue"],
            "category": cat,
            "prompt": build_sfx_prompt(var),
            "seed": int(var["seed"]),
            "duration_s": float(var["duration_s"]),
            "loop": False,
            "max_duration_s": var.get("max_duration_s", default_max_duration_s(key, cat)),
        })
    return items


def read_wav_mono(raw_bytes: bytes) -> tuple[list[float], int]:
    """Decode RIFF/WAVE PCM (8/16/24/32-bit) or IEEE float32 into mono [-1, 1] floats."""
    if len(raw_bytes) < 12 or raw_bytes[:4] != b"RIFF" or raw_bytes[8:12] != b"WAVE":
        raise ValueError("not a RIFF/WAVE stream")

    fmt_tag = None
    channels = 0
    sample_rate = 0
    bits_per_sample = 0
    data_chunk = None

    pos = 12
    total = len(raw_bytes)
    while pos + 8 <= total:
        chunk_id = raw_bytes[pos : pos + 4]
        chunk_size = struct.unpack_from("<I", raw_bytes, pos + 4)[0]
        payload_start = pos + 8
        payload_end = min(total, payload_start + chunk_size)
        if chunk_id == b"fmt " and payload_end - payload_start >= 16:
            fmt_tag, channels, sample_rate, _, _, bits_per_sample = struct.unpack_from(
                "<HHIIHH", raw_bytes, payload_start
            )
            if fmt_tag == 0xFFFE and payload_end - payload_start >= 40:
                fmt_tag = struct.unpack_from("<H", raw_bytes, payload_start + 24)[0]
        elif chunk_id == b"data":
            data_chunk = raw_bytes[payload_start:payload_end]
        pos = payload_start + chunk_size + (chunk_size & 1)

    if fmt_tag is None or channels <= 0 or sample_rate <= 0 or data_chunk is None:
        raise ValueError("missing fmt or data chunk in WAV")

    bytes_per_sample = bits_per_sample // 8
    frame_size = channels * bytes_per_sample
    if frame_size <= 0:
        raise ValueError(f"invalid sample geometry: {channels}ch {bits_per_sample}b")
    n_frames = len(data_chunk) // frame_size

    mono: list[float] = [0.0] * n_frames
    if fmt_tag == 3 and bits_per_sample == 32:
        floats = struct.unpack_from(f"<{n_frames * channels}f", data_chunk, 0)
        for i in range(n_frames):
            base = i * channels
            mono[i] = sum(floats[base : base + channels]) / channels
    elif fmt_tag == 1 and bits_per_sample == 16:
        ints = struct.unpack_from(f"<{n_frames * channels}h", data_chunk, 0)
        for i in range(n_frames):
            base = i * channels
            mono[i] = (sum(ints[base : base + channels]) / channels) / 32768.0
    elif fmt_tag == 1 and bits_per_sample == 32:
        ints = struct.unpack_from(f"<{n_frames * channels}i", data_chunk, 0)
        for i in range(n_frames):
            base = i * channels
            mono[i] = (sum(ints[base : base + channels]) / channels) / 2147483648.0
    elif fmt_tag == 1 and bits_per_sample == 24:
        for i in range(n_frames):
            acc = 0.0
            for ch in range(channels):
                off = i * frame_size + ch * 3
                b0, b1, b2 = data_chunk[off : off + 3]
                val = b0 | (b1 << 8) | (b2 << 16)
                if val & 0x800000:
                    val -= 0x1000000
                acc += val / 8388608.0
            mono[i] = acc / channels
    elif fmt_tag == 1 and bits_per_sample == 8:
        for i in range(n_frames):
            base = i * channels
            mono[i] = (sum((data_chunk[base + ch] - 128) / 128.0 for ch in range(channels))) / channels
    else:
        raise ValueError(f"unsupported WAV format tag={fmt_tag} bits={bits_per_sample}")

    return mono, sample_rate


def write_wav_pcm16(path: Path, samples: list[float], sr: int) -> None:
    """Write mono 16-bit PCM WAV."""
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = array.array(
        "h",
        (int(round(max(-1.0, min(1.0, s)) * 32767.0)) for s in samples),
    )
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(sr)
        wf.writeframes(pcm.tobytes())


def _apply_cosine_fades(data: list[float], sr: int, fade_in_ms: float, fade_out_ms: float) -> None:
    n = len(data)
    if n == 0:
        return
    fin = min(n // 4, int(round(fade_in_ms * 0.001 * sr))) if fade_in_ms > 0.0 else 0
    fout = min(n // 2, max(1, int(round(fade_out_ms * 0.001 * sr)))) if fade_out_ms > 0.0 else 0
    for i in range(fin):
        data[i] *= 0.5 * (1.0 - math.cos(math.pi * i / fin))
    for i in range(fout):
        idx = n - 1 - i
        data[idx] *= 0.5 * (1.0 - math.cos(math.pi * i / fout))


def _remove_dc(data: list[float]) -> list[float]:
    """Subtract block mean (post-trim) so local asymmetric transients keep no DC push."""
    if not data:
        return data
    mean = sum(data) / len(data)
    if mean == 0.0:
        return data
    return [x - mean for x in data]


def _full_rms(data: list[float]) -> float:
    if not data:
        return 0.0
    return math.sqrt(sum(x * x for x in data) / len(data))


def _soft_limit_single(data: list[float], gain: float, ceiling: float) -> list[float]:
    """Single-pass gain + tanh soft-knee only above threshold (no iterative re-limiting).

    The old 3-iteration loop re-limited already limited peaks and diverged:
    short transients ended ~8 dB hot (Hit_Glass_*), beds ended ~4 dB quiet
    (Hit base). One pass keeps crest intact and hits the RMS target.
    """
    thresh = ceiling * 0.78
    headroom = ceiling - thresh
    out = [0.0] * len(data)
    for i, x in enumerate(data):
        v = x * gain
        ax = abs(v)
        if ax > thresh:
            limited = thresh + headroom * math.tanh((ax - thresh) / headroom)
            v = math.copysign(min(ceiling, limited), v)
        out[i] = v
    return out


def _active_rms(data: list[float], floor_amp: float = 0.01) -> float:
    active = [x * x for x in data if abs(x) > floor_amp]
    if not active:
        active = [x * x for x in data]
    if not active:
        return 0.0
    return math.sqrt(sum(active) / len(active))


def master_sfx(
    samples: list[float],
    sr: int,
    *,
    category: str = "combat",
    loop: bool = False,
    max_duration_s: float | None = None,
) -> tuple[list[float], dict]:
    """Deterministically trim, fade/crossfade, loudness-normalize and peak-cap SFX.

    Fixes vs. the first generation pass (Nov 2026, all 73 clips hit audible
    defects): DC is removed *after* trimming (pre-trim mean kept Block at
    +0.0035 and Hit_GlassShield_1 at -0.0126 DC); loops trim silence before
    the equal-power crossfade (Ambient started with 298 ms silence and faded
    to -78 dB tail instead of looping); trim thresholds are capped absolute
    so quiet beds keep their tail (FULL 0.65/1.5/1.8 s hard cuts) and short
    transients keep their decay (Footstep_Sand_2 was cut to 82 ms); fades are
    5 ms in / 45 ms out (80 ms when capped) so sub-bass (Blast/BossRoar) does
    not click on a 10 ms chop; loudness is a single gain pass on full-signal
    RMS (the iterative active-RMS loop left Glass/Swing ~8 dB hot and Hit
    ~4 dB quiet).
    """
    if sr <= 0:
        raise ValueError(f"invalid sample rate {sr}")
    if not samples:
        raise ValueError("empty audio buffer")

    # Coarse DC removal first (protects threshold detectors), fine removal
    # again after trim (local segment mean differs from whole-buffer mean).
    data = _remove_dc([float(x) for x in samples])

    capped = False
    if loop:
        # Trim leading/trailing silence BEFORE the loop seam, then crossfade.
        data, _s, _e, _capped = _trim_oneshot(data, sr, None)
        data = _remove_dc(data)
        if max_duration_s is not None and max_duration_s > 0.0:
            limit = max(8, int(round(max_duration_s * sr)))
            data = data[:limit]
        xfade = min(int(round(0.5 * sr)), len(data) // 4)
        if xfade >= 4:
            head = data[:xfade]
            mid = data[xfade : len(data) - xfade]
            tail = data[len(data) - xfade :]
            blended = [
                math.cos(0.5 * math.pi * (i / xfade)) * tail[i]
                + math.sin(0.5 * math.pi * (i / xfade)) * head[i]
                for i in range(xfade)
            ]
            # mid + blended keeps chronological order (blended seam at loop end).
            data = mid + blended
        data = _remove_dc(data)
    else:
        data, start, _end, capped = _trim_oneshot(data, sr, max_duration_s)
        data = _remove_dc(data)
        if capped:
            _apply_cosine_fades(data, sr, fade_in_ms=5.0 if start > 0 else 0.0, fade_out_ms=80.0)
        else:
            _apply_cosine_fades(data, sr, fade_in_ms=5.0 if start > 0 else 0.0, fade_out_ms=45.0)

    target_db = CATEGORY_TARGET_RMS_DB.get(category, CATEGORY_TARGET_RMS_DB["combat"])
    target_rms = 10.0 ** (target_db / 20.0)
    rms = _full_rms(data)

    gain = target_rms / rms if rms > 1e-9 else 1.0
    # Clamp absurd gains from near-silent inputs (model silence) instead of
    # boosting noise floor into a loud hiss: max +24 dB.
    gain = min(gain, 15.85)
    data = _soft_limit_single(data, gain, PEAK_CEILING_AMP)
    data = _remove_dc(data)

    peak = max((abs(x) for x in data), default=0.0)
    if peak > PEAK_CEILING_AMP:
        scale = PEAK_CEILING_AMP / peak
        data = [x * scale for x in data]
        peak = PEAK_CEILING_AMP

    clipped = sum(1 for x in data if abs(x) >= 0.9999)
    final_rms = _full_rms(data)
    first_loud = next((i for i, x in enumerate(data) if abs(x) > 0.01), 0)
    metrics = {
        "sr": sr,
        "duration_s": round(len(data) / sr, 3),
        "lead_ms": round(1000.0 * first_loud / sr, 2),
        "peak_dbfs": round(20.0 * math.log10(max(peak, 1e-9)), 2),
        "rms_dbfs": round(20.0 * math.log10(max(final_rms, 1e-9)), 2),
        "clipped": clipped,
    }
    return data, metrics


def _trim_oneshot(
    data: list[float], sr: int, max_duration_s: float | None
) -> tuple[list[float], int, int, bool]:
    """Trim leading/trailing silence, keep 120 ms natural decay.

    Thresholds are capped absolute (not purely peak-relative) so loud blasts
    do not keep their noise floor as 'active' (old code pinned every base cue
    to FULL 0.65/1.5/1.8 s) and quiet steps do not lose their decay tail
    (Footstep_Sand_2 was cut to 82 ms). Returns (trimmed, start, end, capped).
    """
    raw_peak = max((abs(x) for x in data), default=0.0)
    if raw_peak <= 1e-9:
        return data, 0, len(data), False
    start_thresh = min(0.01, max(0.003, raw_peak * 0.02))
    end_thresh = min(0.006, max(0.002, raw_peak * 0.01))

    first_active = next((i for i, x in enumerate(data) if abs(x) > start_thresh), 0)
    pre_samples = int(round(0.003 * sr))
    start = max(0, first_active - pre_samples)

    last_active = next(
        (i for i in range(len(data) - 1, start - 1, -1) if abs(data[i]) > end_thresh),
        len(data) - 1,
    )
    tail_pad = int(round(0.120 * sr))
    end = min(len(data), last_active + 1 + tail_pad)
    capped = False
    if max_duration_s is not None and max_duration_s > 0.0:
        cap_end = start + max(1, int(round(max_duration_s * sr)))
        if end > cap_end:
            end = cap_end
            capped = True
    return data[start:max(start + 1, end)], start, end, capped


def _read_token(token_file: Path) -> str:
    env_tok = os.environ.get("STUDIO_API_TOKEN", "").strip()
    if env_tok:
        return env_tok
    if token_file.is_file():
        return token_file.read_text(encoding="utf-8").strip()
    raise SystemExit(f"Studio API token not found (set STUDIO_API_TOKEN or provide {token_file})")


class StudioClient:
    """Minimal stdlib HTTP client for Unsloth Studio GPU 0 audio inference."""

    def __init__(self, base_url: str, token: str):
        self.base_url = base_url.rstrip("/")
        self._token = token

    def _request(self, method: str, path: str, body: dict | None = None, timeout: int = 120):
        url = path if path.startswith("http") else f"{self.base_url}{path}"
        headers = {"Authorization": f"Bearer {self._token}"}
        data = None
        if body is not None:
            headers["Content-Type"] = "application/json"
            data = json.dumps(body).encode("utf-8")
        req = urllib.request.Request(url, data=data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                return resp.read(), resp.headers.get("Content-Type", "")
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")[:400]
            raise RuntimeError(f"Studio {method} {path} -> HTTP {exc.code}: {detail}") from exc

    def request_json(self, method: str, path: str, body: dict | None = None, timeout: int = 120) -> dict | list:
        raw, _ = self._request(method, path, body=body, timeout=timeout)
        return json.loads(raw.decode("utf-8"))

    def download(self, url_or_path: str) -> bytes:
        raw, _ = self._request("GET", url_or_path, body=None, timeout=120)
        return raw

    def loaded_model_ids(self) -> list[str]:
        res = self.request_json("GET", "/api/inference/loaded-models", timeout=30)
        items = res.get("data", res.get("models", res)) if isinstance(res, dict) else res
        if not isinstance(items, list):
            return []
        out = []
        for item in items:
            if isinstance(item, dict):
                if item.get("loaded") is False:
                    continue
                mid = item.get("model_path") or item.get("model") or item.get("id")
                if mid:
                    out.append(str(mid))
            elif isinstance(item, str):
                out.append(item)
        return out

    def ensure_loaded_gpu0(self, model_id: str, wait_s: int = 180) -> bool:
        """Load model_id pinned to GPU 0 if not already resident. Returns True if we loaded it."""
        if model_id in self.loaded_model_ids():
            return False
        self.request_json("POST", "/api/inference/load", body={"model_path": model_id, "gpu_ids": [0]}, timeout=wait_s)
        deadline = time.time() + wait_s
        while time.time() < deadline:
            if model_id in self.loaded_model_ids():
                return True
            time.sleep(1.0)
        raise TimeoutError(f"Timed out waiting for Studio to load {model_id} on GPU 0")

    def unload(self, model_id: str) -> None:
        self.request_json("POST", "/api/inference/unload", body={"model_path": model_id}, timeout=60)

    def generate_sfx(self, model_id: str, prompt: str, duration_s: float, seed: int, variations: int = 1) -> bytes:
        body = {
            "workflow": "music",
            "mode": "sfx",
            "model": model_id,
            "text": prompt,
            "duration_s": max(1.0, float(duration_s)),
            "variations": variations,
            "seed": seed,
        }
        res = self.request_json("POST", "/api/inference/audio/run", body=body, timeout=180)
        clips = res.get("clips") or []
        if not clips:
            raise RuntimeError(f"Studio returned no clips for text={prompt!r}: {res}")
        return self.download(clips[0]["url"])


def load_lock() -> dict:
    if not LOCK_JSON.is_file():
        return {}
    return json.loads(LOCK_JSON.read_text(encoding="utf-8")).get("clips", {})


def save_lock(model_id: str, clips: dict, valid_keys: set[str] | None = None) -> None:
    LOCK_JSON.parent.mkdir(parents=True, exist_ok=True)
    clean = {k: clips[k] for k in sorted(clips) if valid_keys is None or k in valid_keys}
    payload = {"model": model_id, "style_line": STYLE_LINE, "clips": clean}
    LOCK_JSON.write_text(json.dumps(payload, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")


def _parse_from_wav(items: list[str]) -> dict[str, Path]:
    out: dict[str, Path] = {}
    for item in items:
        if "=" not in item:
            raise SystemExit(f"--from-wav expects KEY=PATH, got {item!r}")
        cue, p = item.split("=", 1)
        out[cue.strip()] = Path(p.strip())
    return out


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", nargs="*", default=[], help="render only named manifest cues or variant keys")
    ap.add_argument("--force", action="store_true", help="re-render even when lock hash matches")
    ap.add_argument("--dry-run", action="store_true", help="print planned cues and exit without calling Studio")
    ap.add_argument("--keep-loaded", action="store_true", help="keep Studio audio model loaded after run")
    ap.add_argument("--from-wav", nargs="*", default=[], help="master local raw WAV(s) as KEY=PATH without Studio")
    ap.add_argument("--url", default=os.environ.get("STUDIO_URL", DEFAULT_URL))
    ap.add_argument("--token-file", type=Path, default=DEFAULT_TOKEN_FILE)
    ap.add_argument("--model", default=DEFAULT_MODEL)
    args = ap.parse_args(argv)

    all_items = expected_clips()
    by_key = {item["key"]: item for item in all_items}
    valid_keys = set(by_key)
    only = set(args.only)
    from_wav = _parse_from_wav(args.from_wav)
    if only:
        unknown = sorted(only - valid_keys)
        if unknown:
            raise SystemExit(f"unknown SFX key(s): {', '.join(unknown)}")
    selected = [item for item in all_items if not only or item["key"] in only]

    lock = load_lock()
    todo: list[tuple[dict, str, Path]] = []
    for item in selected:
        key = item["key"]
        h = clip_hash(key, item["prompt"], item["seed"], args.model)
        out_path = SFX_DIR / f"{key}.wav"
        if key in from_wav or args.force or not out_path.exists() or lock.get(key, {}).get("hash") != h:
            todo.append((item, h, out_path))

    print(f"[sfx] {len(todo)} of {len(selected)} selected clips queued (model={args.model})", flush=True)
    if args.dry_run:
        for item, h, out_path in todo:
            print(
                f"  [dry-run] {item['key']} (cue={item['cue']}, cat={item['category']}, "
                f"seed={item['seed']}, hash={h}) -> {out_path.relative_to(REPO)}"
            )
        return 0

    if not todo:
        save_lock(args.model, lock, valid_keys)
        return 0

    client = None
    need_studio = any(item["key"] not in from_wav for item, *_ in todo)
    if need_studio:
        client = StudioClient(args.url, _read_token(args.token_file))
        client.ensure_loaded_gpu0(args.model)

    try:
        for idx, (item, h, out_path) in enumerate(todo, 1):
            key = item["key"]
            cat = item["category"]
            if key in from_wav:
                raw_bytes = from_wav[key].read_bytes()
            else:
                raw_bytes = client.generate_sfx(
                    args.model, item["prompt"], duration_s=item["duration_s"], seed=item["seed"], variations=1
                )

            mono, sr = read_wav_mono(raw_bytes)
            mastered, metrics = master_sfx(
                mono,
                sr,
                category=cat,
                loop=item["loop"],
                max_duration_s=item["max_duration_s"],
            )
            write_wav_pcm16(out_path, mastered, sr)
            lock[key] = {
                "cue": item["cue"],
                "category": cat,
                "hash": h,
                "prompt": item["prompt"],
                "seed": item["seed"],
                **metrics,
            }
            save_lock(args.model, lock, valid_keys)
            print(
                f"[sfx] {idx}/{len(todo)} {key} ({cat}) {metrics['duration_s']:.2f}s "
                f"lead={metrics['lead_ms']:.1f}ms peak={metrics['peak_dbfs']:.2f}dBFS "
                f"rms={metrics['rms_dbfs']:.2f}dBFS",
                flush=True,
            )
    finally:
        if client is not None and not args.keep_loaded:
            try:
                client.unload(args.model)
                print(f"[sfx] unloaded {args.model}", flush=True)
            except Exception as exc:
                print(f"[sfx] warning: unload failed: {exc}", file=sys.stderr)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
