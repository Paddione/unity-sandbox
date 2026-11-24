#!/usr/bin/env python3
"""Gate for the story voice clips (cinderreach-story-voice).

Every dialog and bark line in the C# sources must have a rendered clip in
sandbox/Assets/Resources/Voice/<key>.ogg whose lock hash matches the current
text, speaker and reference voice; every speaker needs a voice definition and
a reference; no orphan clips; total size within budget. Exit 0 = ok.
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent / "voice"))
import voice_lines as vl  # noqa: E402

BUDGET_BYTES = 25 * 1024 * 1024


def problems(repo: Path | None = None) -> list[str]:
    out: list[str] = []
    voices = vl.load_voices()
    lock = vl.load_lock()
    entries = vl.lines()
    keys = set()
    for e in entries:
        keys.add(e["key"])
        if not e["text"]:
            out.append(f"{e['key']}: no German text in the catalog")
            continue
        if e["speaker"] not in voices:
            out.append(f"{e['key']}: speaker '{e['speaker']}' has no voice in voices.json")
            continue
        clip = vl.VOICE_DIR / f"{e['key']}.ogg"
        if not clip.exists():
            out.append(f"{e['key']}: clip missing (render_voices.py)")
        elif lock.get(e["key"], {}).get("hash") != vl.line_hash(e, voices):
            out.append(f"{e['key']}: clip stale, text or voice changed (render_voices.py)")
    for vid in voices:
        if not (vl.REPO / "docs" / "audio" / "voices" / vid / "ref.ogg").exists():
            out.append(f"voice {vid}: reference missing")
    total = 0
    if vl.VOICE_DIR.exists():
        for clip in vl.VOICE_DIR.glob("*.ogg"):
            total += clip.stat().st_size
            if clip.stem not in keys:
                out.append(f"{clip.name}: orphan clip without a line")
    if total > BUDGET_BYTES:
        out.append(f"voice clips {total / 1e6:.1f} MB exceed budget {BUDGET_BYTES / 1e6:.0f} MB")
    return out


def main() -> int:
    found = problems()
    for p in found:
        print(p)
    n = len(vl.lines())
    print(f"voice lines: {n}, problems: {len(found)}")
    return 1 if found else 0


if __name__ == "__main__":
    raise SystemExit(main())
