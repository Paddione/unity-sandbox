"""Intelligibility check for rendered voice clips (cinderreach-story-voice).

Run with a Python that has ``faster_whisper``::

    ~/models/voice/stt-server/.venv/bin/python scripts/voice/qa_transcribe.py [--model DIR] [--max-cer 0.25]

Transcribes every clip, compares it with the catalog text (character error
rate on normalised text) and prints the worst lines. Exit 1 when a line is
above --max-cer.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import unicodedata
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import voice_lines as vl  # noqa: E402


NUMBERS = {str(i): w for i, w in enumerate("null eins zwei drei vier fünf sechs sieben acht neun zehn elf zwölf".split())}


def norm(s: str) -> str:
    s = re.sub(r"\b(\d{1,2})\b", lambda m: NUMBERS.get(m.group(1), m.group(1)), s)
    s = unicodedata.normalize("NFC", s.lower()).replace("ß", "ss")
    return re.sub(r"[^a-zäöü0-9 ]+", "", re.sub(r"\s+", " ", s)).strip()


def cer(a: str, b: str) -> float:
    a, b = norm(a), norm(b)
    if not a:
        return 0.0 if not b else 1.0
    prev = list(range(len(b) + 1))
    for i, ca in enumerate(a, 1):
        cur = [i]
        for j, cb in enumerate(b, 1):
            cur.append(min(prev[j] + 1, cur[j - 1] + 1, prev[j - 1] + (ca != cb)))
        prev = cur
    return prev[-1] / len(a)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", default=str(Path.home() / "models/voice/primeline-turbo-german-ct2"))
    ap.add_argument("--max-cer", type=float, default=0.25)
    ap.add_argument("--only", nargs="*", default=[])
    ap.add_argument("--json", default="")
    args = ap.parse_args()
    from faster_whisper import WhisperModel
    model = WhisperModel(args.model, device="cpu", compute_type="int8", cpu_threads=6)
    rows = []
    for e in vl.lines():
        if args.only and e["key"] not in args.only:
            continue
        clip = vl.VOICE_DIR / f"{e['key']}.ogg"
        if not clip.exists():
            continue
        # Short, clean TTS clips trip whisper's no-speech gate: loosen it, keep beam search.
        segs, _ = model.transcribe(str(clip), language="de", beam_size=5, vad_filter=False,
                                   condition_on_previous_text=False, no_speech_threshold=0.95,
                                   log_prob_threshold=None)
        heard = " ".join(s.text for s in segs).strip()
        rows.append({"key": e["key"], "speaker": e["speaker"], "cer": round(cer(e["text"], heard), 3),
                     "text": e["text"], "heard": heard})
        print(f"{rows[-1]['cer']:.2f} {e['key']}", flush=True)
    rows.sort(key=lambda r: -r["cer"])
    bad = [r for r in rows if r["cer"] > args.max_cer]
    mean = sum(r["cer"] for r in rows) / max(1, len(rows))
    print(f"clips {len(rows)}, mean CER {mean:.3f}, above {args.max_cer}: {len(bad)}")
    for r in bad:
        print(f"  {r['key']} {r['cer']}: heard '{r['heard']}'")
    if args.json:
        Path(args.json).write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding="utf-8")
    return 1 if bad else 0


if __name__ == "__main__":
    raise SystemExit(main())
