"""Render the Cinderreach story voice clips (cinderreach-story-voice).

Run with a Python that has ``qwen_tts`` (the voice-tts box venv)::

    ~/unsloth-boxes/voice-tts/.venv/bin/python scripts/voice/render_voices.py [--device auto|cuda:0|cuda:1|cpu] [--only KEY ...] [--force]

Step 1 designs a missing reference per speaker (docs/audio/voices/<id>/ref.ogg)
from its ``instruct`` in docs/audio/voices.json. Step 2 clones every line whose
text, speaker, reference or model changed (hash in docs/audio/voice-lines.lock.json)
into sandbox/Assets/AddressableAssets/Voice/<key>.ogg. Uses GPU 0 (cuda:0 with
CUDA_DEVICE_ORDER=PCI_BUS_ID) by default when CUDA is available, falling back
to CPU (override via --device or TTS_DEVICE); threads follow OMP_NUM_THREADS.
"""
from __future__ import annotations

import argparse
import fcntl
import json
import os
import sys
import time
from pathlib import Path

import numpy as np
import soundfile as sf

sys.path.insert(0, str(Path(__file__).resolve().parent))
import voice_lines as vl  # noqa: E402

REF_DIR = vl.REPO / "docs" / "audio" / "voices"
SR_OUT = 24000
LOCK_FLOCK = Path("/tmp/cinderreach-voice-lines.flock")


def _configure_cuda_env(requested_device: str) -> None:
    """Pin CUDA device ordering before importing torch so each worker uses one physical GPU."""
    os.environ["CUDA_DEVICE_ORDER"] = "PCI_BUS_ID"
    if requested_device == "cuda:1":
        os.environ["CUDA_VISIBLE_DEVICES"] = "1"
    elif requested_device in ("auto", "cuda:0"):
        os.environ.setdefault("CUDA_VISIBLE_DEVICES", "0")


def _resolve_device(requested_device: str):
    _configure_cuda_env(requested_device)
    import torch

    if requested_device == "auto":
        device = "cuda:0" if torch.cuda.is_available() else "cpu"
        label = device
    elif requested_device == "cuda:1":
        device = "cuda:0"
        label = "cuda:1 (logical cuda:0)"
    else:
        device = requested_device
        label = device
    dtype = torch.float32 if device == "cpu" else torch.bfloat16
    return torch, device, label, dtype


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


def design_refs(voices: dict, force: bool, device: str, dtype, torch_mod) -> None:
    todo = [v for v in voices if force or not (REF_DIR / v / "ref.ogg").exists()]
    if not todo:
        return
    from qwen_tts import Qwen3TTSModel
    model_id = json.loads(vl.VOICES_JSON.read_text(encoding="utf-8"))["design_model"]
    print(f"[design] device={device} dtype={dtype} model={model_id}", flush=True)
    model = Qwen3TTSModel.from_pretrained(model_id, device_map=device, dtype=dtype, attn_implementation="sdpa")
    for vid in todo:
        v = voices[vid]
        torch_mod.manual_seed(v.get("seed", 0))
        t = time.time()
        wavs, sr = model.generate_voice_design(text=v["ref_text"], instruct=v["instruct"], language="German")
        write_ogg(REF_DIR / vid / "ref.ogg", polish(wavs[0]), sr)
        (REF_DIR / vid / "ref.txt").write_text(v["ref_text"] + "\n", encoding="utf-8")
        print(f"[design] {vid} {len(wavs[0]) / sr:.1f}s in {time.time() - t:.0f}s", flush=True)
    del model


def render_lines(
    voices: dict, only: set[str], force: bool, device: str, label: str, dtype, torch_mod, seed_offset: int = 0
) -> None:
    entries = vl.lines()
    lock = vl.load_lock()
    model_id = vl.clone_model()
    todo = []
    for e in entries:
        out = vl.VOICE_DIR / f"{e['key']}.ogg"
        h = vl.line_hash(e, voices, model_id=model_id)
        if only and e["key"] not in only:
            continue
        if force or not out.exists() or lock.get(e["key"], {}).get("hash") != h:
            todo.append((e, h, out))
    print(f"[render] {len(todo)} of {len(entries)} lines on {label}", flush=True)
    if todo:
        from qwen_tts import Qwen3TTSModel
        print(f"[render] device={label} dtype={dtype} model={model_id}", flush=True)
        model = Qwen3TTSModel.from_pretrained(model_id, device_map=device, dtype=dtype, attn_implementation="sdpa")
        prompts = {}
        for n, (e, h, out) in enumerate(todo, 1):
            sp = e["speaker"]
            if sp not in prompts:
                ref, rsr = sf.read(str(REF_DIR / sp / "ref.ogg"), dtype="float32")
                prompts[sp] = model.create_voice_clone_prompt(
                    ref_audio=(ref, rsr), ref_text=(REF_DIR / sp / "ref.txt").read_text(encoding="utf-8").strip())
            torch_mod.manual_seed(voices[sp].get("seed", 0) * 1000 + n + seed_offset * 10000)
            t = time.time()
            wavs, sr = model.generate_voice_clone(text=vl.tts_text(e["text"]), language="German", voice_clone_prompt=prompts[sp])
            wav = polish(wavs[0])
            write_ogg(out, wav, sr)
            update_lock_entry(
                e["key"],
                {"hash": h, "speaker": sp, "model": model_id, "seconds": round(wav.size / sr, 2)},
                entries,
            )
            print(f"[render] {n}/{len(todo)} {e['key']} ({sp}) {wav.size / sr:.1f}s in {time.time() - t:.0f}s", flush=True)
    elif not only:
        update_lock_entry(None, None, entries)


def update_lock_entry(key: str | None, entry_data: dict | None, entries: list) -> None:
    """Process-safe read-modify-write update of voice-lines.lock.json via flock."""
    keys = {e["key"] for e in entries}
    LOCK_FLOCK.parent.mkdir(parents=True, exist_ok=True)
    with open(LOCK_FLOCK, "w", encoding="utf-8") as lf:
        fcntl.flock(lf, fcntl.LOCK_EX)
        lock = vl.load_lock()
        if key is not None and entry_data is not None:
            lock[key] = entry_data
        clean = {k: lock[k] for k in sorted(lock) if k in keys}
        tmp = vl.LOCK_JSON.with_suffix(".json.tmp")
        tmp.write_text(json.dumps({"lines": clean}, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
        os.replace(tmp, vl.LOCK_JSON)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--device", choices=["auto", "cuda:0", "cuda:1", "cpu"],
                    default=os.environ.get("TTS_DEVICE", "auto"),
                    help="target device (default: auto -> cuda:0 on GPU 0, else cpu)")
    ap.add_argument("--only", nargs="*", default=[])
    ap.add_argument("--force", action="store_true", help="re-render selected lines even when unchanged")
    ap.add_argument("--seed-offset", type=int, default=0, help="deterministic seed offset for retries")
    ap.add_argument("--redesign", action="store_true", help="re-design all reference voices")
    ap.add_argument("--design-only", action="store_true")
    args = ap.parse_args()
    torch_mod, device, label, dtype = _resolve_device(args.device)
    torch_mod.set_num_threads(int(os.environ.get("OMP_NUM_THREADS", "6")))
    voices = vl.load_voices()
    design_refs(voices, args.redesign, device, dtype, torch_mod)
    if not args.design_only:
        render_lines(voices, set(args.only), args.force, device, label, dtype, torch_mod, seed_offset=args.seed_offset)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
