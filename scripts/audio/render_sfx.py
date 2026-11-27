#!/usr/bin/env python3
"""Render and deterministically master Cinderreach HQ SFX via Unsloth Studio GPU 0.

Pure Python stdlib (no numpy/ffmpeg dependency) so both rendering and unit tests
run directly under system python3.

Usage::

    python3 scripts/audio/render_sfx.py [--only SwordHit1 Fireball ...] [--force] [--dry-run]
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
PEAK_CEILING_AMP = 10.0 ** (-1.05 / 20.0)  # ~0.8861, strictly <= -1.0 dBFS

CATEGORY_TARGET_RMS_DB = {
    "ui": -20.0,
    "combat": -16.0,
    "event": -17.0,
    "step": -22.0,
    "ambient": -24.0,
}

UI_CUES = {"UIClick", "VendorBuy", "VendorSell", "Loot", "Gold"}
EVENT_CUES = {"RareDrop", "LevelUp", "Gate", "Heal", "Quest", "Shrine"}
DEFAULT_DURATION_BY_CAT = {
    "ui": 1.0,
    "combat": 1.5,
    "event": 1.8,
    "step": 1.0,
    "ambient": 6.0,
}


def cue_category(cue: str) -> str:
    if cue in UI_CUES:
        return "ui"
    if cue in EVENT_CUES:
        return "event"
    if cue == "Footstep" or cue.startswith("Step"):
        return "step"
    if cue == "Ambient":
        return "ambient"
    return "combat"


def build_sfx_prompt(entry: dict) -> str:
    """Combine manifest cue description with consistent Cinderreach acoustic style line."""
    base = entry.get("prompt", entry.get("cue", "")).strip()
    return f"{base}, {STYLE_LINE}" if base else STYLE_LINE


def clip_hash(cue: str, prompt: str, seed: int, model: str) -> str:
    blob = json.dumps([cue, prompt, seed, model, STYLE_LINE], ensure_ascii=False)
    return hashlib.sha1(blob.encode("utf-8")).hexdigest()[:12]


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


def _soft_limit(data: list[float], gain: float, ceiling: float) -> list[float]:
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
    """Deterministically trim, fade/crossfade, loudness-normalize and peak-cap SFX."""
    if sr <= 0:
        raise ValueError(f"invalid sample rate {sr}")
    if not samples:
        raise ValueError("empty audio buffer")

    # 1. DC-offset removal
    mean = sum(samples) / len(samples)
    data = [float(x) - mean for x in samples]

    # 2. Trim silence / build seamless loop
    if loop:
        if max_duration_s is not None and max_duration_s > 0.0:
            limit = max(8, int(round(max_duration_s * sr)))
            data = data[:limit]
        xfade = min(int(round(0.5 * sr)), len(data) // 4)
        if xfade >= 4:
            # Put equal-power blend from tail->head at start; boundary data[-1]->data[0]
            # corresponds to consecutive source samples right before tail start.
            head = data[:xfade]
            mid = data[xfade : len(data) - xfade]
            tail = data[len(data) - xfade :]
            blended = [
                math.cos(0.5 * math.pi * (i / xfade)) * tail[i]
                + math.sin(0.5 * math.pi * (i / xfade)) * head[i]
                for i in range(xfade)
            ]
            data = blended + mid
    else:
        raw_peak = max((abs(x) for x in data), default=0.0)
        start_thresh = max(0.01, raw_peak * 0.025) if raw_peak > 0.01 else 0.005
        end_thresh = max(0.0056, raw_peak * 0.012) if raw_peak > 0.0056 else 0.003

        first_active = next((i for i, x in enumerate(data) if abs(x) > start_thresh), 0)
        pre_samples = int(round(0.002 * sr))
        start = max(0, first_active - pre_samples)

        last_active = next(
            (i for i in range(len(data) - 1, start - 1, -1) if abs(data[i]) > end_thresh),
            len(data) - 1,
        )
        tail_pad = int(round(0.040 * sr))
        end = min(len(data), last_active + 1 + tail_pad)
        if max_duration_s is not None and max_duration_s > 0.0:
            end = min(end, start + max(1, int(round(max_duration_s * sr))))
        data = data[start:max(start + 1, end)]
        _apply_cosine_fades(data, sr, fade_in_ms=2.0 if start > 0 else 0.0, fade_out_ms=10.0)

    # 3. Category loudness normalization + <= -1.0 dBFS True Peak ceiling
    target_db = CATEGORY_TARGET_RMS_DB.get(category, CATEGORY_TARGET_RMS_DB["combat"])
    target_rms = 10.0 ** (target_db / 20.0)
    rms = _active_rms(data, floor_amp=0.01)
    if rms < 1e-6:
        rms = _active_rms(data, floor_amp=0.0)

    gain = target_rms / rms if rms > 1e-9 else 1.0
    for _ in range(3):
        candidate = _soft_limit(data, gain, PEAK_CEILING_AMP)
        cur_rms = _active_rms(candidate, floor_amp=0.01)
        if cur_rms <= 1e-9 or abs(cur_rms - target_rms) / target_rms < 0.05:
            data = candidate
            break
        peak_cand = max((abs(x) for x in candidate), default=0.0)
        if peak_cand >= PEAK_CEILING_AMP * 0.995 and cur_rms < target_rms:
            gain *= target_rms / cur_rms
            data = candidate
        else:
            gain *= target_rms / cur_rms
            data = candidate
    else:
        data = _soft_limit(data, gain, PEAK_CEILING_AMP)

    peak = max((abs(x) for x in data), default=0.0)
    if peak > PEAK_CEILING_AMP:
        scale = PEAK_CEILING_AMP / peak
        data = [x * scale for x in data]
        peak = PEAK_CEILING_AMP

    clipped = sum(1 for x in data if abs(x) >= 0.9999)
    final_rms = _active_rms(data, floor_amp=0.01)
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


def save_lock(model_id: str, clips: dict) -> None:
    LOCK_JSON.parent.mkdir(parents=True, exist_ok=True)
    payload = {"model": model_id, "style_line": STYLE_LINE, "clips": {k: clips[k] for k in sorted(clips)}}
    LOCK_JSON.write_text(json.dumps(payload, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")


def _parse_from_wav(items: list[str]) -> dict[str, Path]:
    out: dict[str, Path] = {}
    for item in items:
        if "=" not in item:
            raise SystemExit(f"--from-wav expects CUE=PATH, got {item!r}")
        cue, p = item.split("=", 1)
        out[cue.strip()] = Path(p.strip())
    return out


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", nargs="*", default=[], help="render only named manifest cues")
    ap.add_argument("--force", action="store_true", help="re-render even when lock hash matches")
    ap.add_argument("--dry-run", action="store_true", help="print planned cues and exit without calling Studio")
    ap.add_argument("--keep-loaded", action="store_true", help="keep Studio audio model loaded after run")
    ap.add_argument("--from-wav", nargs="*", default=[], help="master local raw WAV(s) as CUE=PATH without Studio")
    ap.add_argument("--url", default=os.environ.get("STUDIO_URL", DEFAULT_URL))
    ap.add_argument("--token-file", type=Path, default=DEFAULT_TOKEN_FILE)
    ap.add_argument("--model", default=DEFAULT_MODEL)
    args = ap.parse_args(argv)

    manifest = json.loads(MANIFEST_JSON.read_text(encoding="utf-8"))
    by_cue = {item["cue"]: item for item in manifest.get("sfx", [])}
    only = set(args.only)
    from_wav = _parse_from_wav(args.from_wav)
    selected_cues = [c for c in by_cue if not only or c in only]
    if only:
        unknown = sorted(only - set(by_cue))
        if unknown:
            raise SystemExit(f"unknown manifest cue(s): {', '.join(unknown)}")

    lock = load_lock()
    todo: list[tuple[dict, str, str, int, Path]] = []
    for cue in selected_cues:
        entry = by_cue[cue]
        prompt = build_sfx_prompt(entry)
        seed = int(entry.get("seed", 1234))
        h = clip_hash(cue, prompt, seed, args.model)
        out_path = SFX_DIR / f"{cue}.wav"
        if cue in from_wav or args.force or not out_path.exists() or lock.get(cue, {}).get("hash") != h:
            todo.append((entry, prompt, h, seed, out_path))

    print(f"[sfx] {len(todo)} of {len(selected_cues)} selected cues queued (model={args.model})", flush=True)
    if args.dry_run:
        for entry, prompt, h, seed, out_path in todo:
            cat = cue_category(entry["cue"])
            print(f"  [dry-run] {entry['cue']} cat={cat} seed={seed} hash={h} -> {out_path.relative_to(REPO)}")
        return 0

    if not todo:
        return 0

    client = None
    loaded_by_us = False
    need_studio = any(entry["cue"] not in from_wav for entry, *_ in todo)
    if need_studio:
        client = StudioClient(args.url, _read_token(args.token_file))
        loaded_by_us = client.ensure_loaded_gpu0(args.model)

    try:
        for idx, (entry, prompt, h, seed, out_path) in enumerate(todo, 1):
            cue = entry["cue"]
            cat = cue_category(cue)
            if cue in from_wav:
                raw_bytes = from_wav[cue].read_bytes()
            else:
                dur = float(entry.get("duration_s", DEFAULT_DURATION_BY_CAT[cat]))
                raw_bytes = client.generate_sfx(args.model, prompt, duration_s=dur, seed=seed, variations=1)

            mono, sr = read_wav_mono(raw_bytes)
            mastered, metrics = master_sfx(
                mono,
                sr,
                category=cat,
                loop=(cat == "ambient"),
                max_duration_s=None if cat == "ambient" else entry.get("max_duration_s"),
            )
            write_wav_pcm16(out_path, mastered, sr)
            lock[cue] = {
                "cue": cue,
                "category": cat,
                "hash": h,
                "prompt": prompt,
                "seed": seed,
                **metrics,
            }
            save_lock(args.model, lock)
            print(
                f"[sfx] {idx}/{len(todo)} {cue} ({cat}) {metrics['duration_s']:.2f}s "
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
