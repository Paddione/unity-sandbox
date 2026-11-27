"""Unit tests for the Cinderreach SFX rendering & mastering pipeline."""
from __future__ import annotations

import math
import struct
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE / "audio"))
import render_sfx as sfx  # noqa: E402


def _sine_burst(sr: int, lead_s: float, body_s: float, tail_s: float, amp: float, freq: float = 440.0) -> list[float]:
    n_lead = int(round(lead_s * sr))
    n_body = int(round(body_s * sr))
    n_tail = int(round(tail_s * sr))
    body = [amp * math.sin(2.0 * math.pi * freq * (i / sr)) for i in range(n_body)]
    return [0.0] * n_lead + body + [0.0] * n_tail


def test_trim_removes_leading_and_trailing_silence_under_5ms():
    sr = 44100
    raw = _sine_burst(sr, lead_s=0.20, body_s=0.25, tail_s=0.30, amp=0.5)
    out, metrics = sfx.master_sfx(raw, sr, category="combat")
    assert metrics["lead_ms"] < 5.0
    assert 0.24 < metrics["duration_s"] < 0.32
    assert abs(out[-1]) < 1e-4


def test_master_clamps_peak_below_minus_1_dbfs_no_clipping():
    sr = 44100
    hot = _sine_burst(sr, lead_s=0.0, body_s=0.3, tail_s=0.0, amp=1.8, freq=220.0)
    out, metrics = sfx.master_sfx(hot, sr, category="combat")
    assert metrics["clipped"] == 0
    assert metrics["peak_dbfs"] <= -1.0
    assert max(abs(x) for x in out) <= 10.0 ** (-1.0 / 20.0)


def test_category_loudness_spread_within_2_lu():
    sr = 44100
    quiet = _sine_burst(sr, lead_s=0.01, body_s=0.3, tail_s=0.02, amp=0.04, freq=330.0)
    loud = _sine_burst(sr, lead_s=0.01, body_s=0.3, tail_s=0.02, amp=0.80, freq=330.0)
    _, m_quiet = sfx.master_sfx(quiet, sr, category="combat")
    _, m_loud = sfx.master_sfx(loud, sr, category="combat")
    assert abs(m_quiet["rms_dbfs"] - m_loud["rms_dbfs"]) <= 2.0


def test_loop_crossfade_seamless_boundary():
    sr = 44100
    # Slowly varying low-frequency drone over 2 seconds (non-integer periods so edges would jump without blend)
    n = 2 * sr
    drone = [0.25 * math.sin(2.0 * math.pi * 110.3 * (i / sr)) for i in range(n)]
    out, metrics = sfx.master_sfx(drone, sr, category="ambient", loop=True)
    assert abs(out[0] - out[-1]) < 0.02
    assert metrics["clipped"] == 0


def test_wav_roundtrip_pcm16_and_ieee_float(tmp_path: Path):
    sr = 44100
    src = [0.4 * math.sin(2.0 * math.pi * 220.0 * (i / sr)) for i in range(1000)]
    wav_path = tmp_path / "pcm16.wav"
    sfx.write_wav_pcm16(wav_path, src, sr)
    decoded, dec_sr = sfx.read_wav_mono(wav_path.read_bytes())
    assert dec_sr == sr
    assert len(decoded) == len(src)
    assert max(abs(a - b) for a, b in zip(src, decoded)) < 1e-4

    # Synthetic stereo IEEE float32 RIFF WAV
    frames = 200
    payload = bytearray()
    for i in range(frames):
        payload.extend(struct.pack("<ff", 0.5, -0.1))  # mono avg = 0.2
    fmt_chunk = struct.pack("<HHIIHH", 3, 2, sr, sr * 8, 8, 32)
    riff_body = (
        b"WAVEfmt "
        + struct.pack("<I", len(fmt_chunk))
        + fmt_chunk
        + b"data"
        + struct.pack("<I", len(payload))
        + bytes(payload)
    )
    riff = b"RIFF" + struct.pack("<I", len(riff_body)) + riff_body
    f_mono, f_sr = sfx.read_wav_mono(riff)
    assert f_sr == sr
    assert len(f_mono) == frames
    assert abs(f_mono[0] - 0.2) < 1e-6


def test_dry_run_manifest_coverage():
    res = subprocess.run(
        [sys.executable, str(HERE / "audio" / "render_sfx.py"), "--dry-run", "--force", "--only", "SwordHit1", "Fireball", "UIClick"],
        capture_output=True,
        text=True,
    )
    assert res.returncode == 0, res.stderr
    assert "SwordHit1" in res.stdout
    assert "Fireball" in res.stdout
    assert "UIClick" in res.stdout
