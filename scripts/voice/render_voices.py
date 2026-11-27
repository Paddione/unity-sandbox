"""Render the Cinderreach story voice clips (cinderreach-story-voice).

Run with a Python that has ``qwen_tts`` (the voice-tts box venv)::

    ~/unsloth-boxes/voice-tts/.venv/bin/python scripts/voice/render_voices.py [--only KEY ...] [--force]

Step 1 designs a missing reference per speaker (docs/audio/voices/<id>/ref.ogg)
from its ``instruct`` in docs/audio/voices.json. Step 2 clones every line whose
text, speaker or reference changed (hash in docs/audio/voice-lines.lock.json)
into sandbox/Assets/AddressableAssets/Voice/<key>.ogg. CPU by default (TTS_DEVICE=cuda:0
to use a GPU); threads follow OMP_NUM_THREADS.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import time
from pathlib import Path

import numpy as np
import soundfile as sf
import torch

sys.path.insert(0, str(Path(__file__).resolve().parent))
import voice_lines as vl  # noqa: E402

REF_DIR = vl.REPO / "docs" / "audio" / "voices"
DEVICE = os.environ.get("TTS_DEVICE", "cpu")
DTYPE = torch.float32 if DEVICE == "cpu" else torch.bfloat16
SR_OUT = 24000


def polish(wav: np.ndarray) -> np.ndarray:
    """Trim leading/trailing silence, add 80 ms pads, peak-normalise to -1 dBFS."""
    wav = np.asarray(wav, dtype=np.float32).reshape(-1)
    loud = np.flatnonzero(np.abs(wav) > 0.01)
    if loud.size:
        a = max(0, loud[0] - int(0.03 * SR_OUT))
        b = min(wav.size, loud[-1] + int(0.12 * SR_OUT))
        wav = wav[a:b]
    pad = np.zeros(int(0.08 * SR_OUT), dtype=np.float32)
    wav = np.concatenate([pad, wav, pad])
    peak = float(np.max(np.abs(wav))) or 1.0
    return wav * (0.89 / peak)


def write_ogg(path: Path, wav: np.ndarray, sr: int) -> None:
    if sr != SR_OUT:
        raise SystemExit(f"unexpected sample rate {sr}")
    path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(str(path), wav, sr, format="OGG", subtype="VORBIS")


def design_refs(voices: dict, force: bool) -> None:
    todo = [v for v in voices if force or not (REF_DIR / v / "ref.ogg").exists()]
    if not todo:
        return
    from qwen_tts import Qwen3TTSModel
    model = Qwen3TTSModel.from_pretrained(json.loads(vl.VOICES_JSON.read_text())["design_model"],
                                          device_map=DEVICE, dtype=DTYPE, attn_implementation="sdpa")
    for vid in todo:
        v = voices[vid]
        torch.manual_seed(v.get("seed", 0))
        t = time.time()
        wavs, sr = model.generate_voice_design(text=v["ref_text"], instruct=v["instruct"], language="German")
        write_ogg(REF_DIR / vid / "ref.ogg", polish(wavs[0]), sr)
        (REF_DIR / vid / "ref.txt").write_text(v["ref_text"] + "\n", encoding="utf-8")
        print(f"[design] {vid} {len(wavs[0]) / sr:.1f}s in {time.time() - t:.0f}s", flush=True)
    del model


def render_lines(voices: dict, only: set[str], force: bool) -> None:
    entries = vl.lines()
    lock = vl.load_lock()
    todo = []
    for e in entries:
        out = vl.VOICE_DIR / f"{e['key']}.ogg"
        h = vl.line_hash(e, voices)
        if only and e["key"] not in only:
            continue
        if force or not out.exists() or lock.get(e["key"], {}).get("hash") != h:
            todo.append((e, h, out))
    print(f"[render] {len(todo)} of {len(entries)} lines", flush=True)
    if todo:
        from qwen_tts import Qwen3TTSModel
        model = Qwen3TTSModel.from_pretrained(json.loads(vl.VOICES_JSON.read_text())["clone_model"],
                                              device_map=DEVICE, dtype=DTYPE, attn_implementation="sdpa")
        prompts = {}
        for n, (e, h, out) in enumerate(todo, 1):
            sp = e["speaker"]
            if sp not in prompts:
                ref, rsr = sf.read(str(REF_DIR / sp / "ref.ogg"), dtype="float32")
                prompts[sp] = model.create_voice_clone_prompt(
                    ref_audio=(ref, rsr), ref_text=(REF_DIR / sp / "ref.txt").read_text(encoding="utf-8").strip())
            torch.manual_seed(voices[sp].get("seed", 0) * 1000 + n)
            t = time.time()
            wavs, sr = model.generate_voice_clone(text=vl.tts_text(e["text"]), language="German", voice_clone_prompt=prompts[sp])
            wav = polish(wavs[0])
            write_ogg(out, wav, sr)
            lock[e["key"]] = {"hash": h, "speaker": sp, "seconds": round(wav.size / sr, 2)}
            save_lock(lock, entries)
            print(f"[render] {n}/{len(todo)} {e['key']} ({sp}) {wav.size / sr:.1f}s in {time.time() - t:.0f}s", flush=True)
    save_lock(lock, entries)


def save_lock(lock: dict, entries: list) -> None:
    keys = {e["key"] for e in entries}
    clean = {k: lock[k] for k in sorted(lock) if k in keys}
    vl.LOCK_JSON.write_text(json.dumps({"lines": clean}, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", nargs="*", default=[])
    ap.add_argument("--force", action="store_true", help="re-render selected lines even when unchanged")
    ap.add_argument("--redesign", action="store_true", help="re-design all reference voices")
    ap.add_argument("--design-only", action="store_true")
    args = ap.parse_args()
    torch.set_num_threads(int(os.environ.get("OMP_NUM_THREADS", "6")))
    voices = vl.load_voices()
    design_refs(voices, args.redesign)
    if not args.design_only:
        render_lines(voices, set(args.only), args.force)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
