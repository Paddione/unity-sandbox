#!/usr/bin/env python3
"""Gate for rendered Cinderreach SFX clips (analogous to check-voice-lines.py).

Every expected SFX clip (43 base cues in docs/audio/manifest.json + 30 surface/
variant pool clips) must exist as sandbox/Assets/AddressableAssets/Sfx/<key>.wav,
match its lock hash and mastering limits in docs/audio/sfx.lock.json (no
clipping, True Peak <= -1.0 dBFS, one-shot lead silence < 5.0 ms), have no
orphan WAVs and stay within the 25 MB budget. Exit 0 = ok.
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent / "audio"))
import render_sfx as rsfx  # noqa: E402

BUDGET_BYTES = 25 * 1024 * 1024


def problems() -> list[str]:
    out: list[str] = []
    expected = rsfx.expected_clips()
    lock = rsfx.load_lock()
    keys: set[str] = set()

    for item in expected:
        key = item["key"]
        keys.add(key)
        wav = rsfx.SFX_DIR / f"{key}.wav"
        if not wav.is_file():
            out.append(f"{key}: clip missing (render_sfx.py)")
            continue
        entry = lock.get(key)
        expected_hash = rsfx.clip_hash(
            key, item["prompt"], item["seed"], item.get("model", rsfx.DEFAULT_MODEL)
        )
        if not entry or entry.get("hash") != expected_hash:
            out.append(f"{key}: clip stale or missing in sfx.lock.json (render_sfx.py)")
            continue
        if entry.get("clipped", 0) != 0:
            out.append(f"{key}: clipped samples={entry.get('clipped')}")
        if entry.get("peak_dbfs", 0.0) > -1.0:
            out.append(f"{key}: peak {entry.get('peak_dbfs')} dBFS exceeds -1.0 dBFS")
        if not item["loop"] and entry.get("lead_ms", 999.0) >= 5.0:
            out.append(f"{key}: lead silence {entry.get('lead_ms')} ms >= 5.0 ms")

    total = 0
    if rsfx.SFX_DIR.is_dir():
        for wav in sorted(rsfx.SFX_DIR.glob("*.wav")):
            total += wav.stat().st_size
            if wav.stem not in keys:
                out.append(f"{wav.name}: orphan clip without manifest/variant entry")

    if total > BUDGET_BYTES:
        out.append(f"sfx clips {total / 1e6:.1f} MB exceed budget {BUDGET_BYTES / 1e6:.0f} MB")

    return out


def main() -> int:
    found = problems()
    for p in found:
        print(p)
    n = len(rsfx.expected_clips())
    print(f"sfx clips: {n}, problems: {len(found)}")
    return 1 if found else 0


if __name__ == "__main__":
    raise SystemExit(main())
