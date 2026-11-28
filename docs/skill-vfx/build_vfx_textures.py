#!/usr/bin/env python3
"""Export Material Maker (.ptex) noise and mask textures to Unity VFX PNGs.

Usage:
    python3 docs/skill-vfx/build_vfx_textures.py [--out sandbox/Assets/Art/SkillVfx/Textures]
"""
import argparse
import os
import shutil
import signal
import subprocess
import tempfile
import time
from pathlib import Path
from PIL import Image, ImageStat

ROOT = Path(__file__).resolve().parent
PTEX_DIR = ROOT / "ptex"
DEFAULT_OUT = ROOT.parent.parent / "sandbox" / "Assets" / "Art" / "SkillVfx" / "Textures"


def find_material_maker() -> str:
    env = os.environ.get("MATERIAL_MAKER_BIN")
    if env and os.path.isfile(env):
        return env
    for cand in (
        shutil.which("material-maker"),
        str(Path.home() / ".local" / "bin" / "material-maker"),
    ):
        if cand and os.path.isfile(cand):
            return cand
    raise FileNotFoundError("material-maker binary not found; run scripts/install-material-maker.sh")


def export_ptex_batch(mm_bin: str, ptex_files: list[Path], tmp_dir: Path, timeout_s: int = 90) -> None:
    cmd = [
        "xvfb-run", "-a",
        mm_bin,
        "--display-driver", "x11",
        "--export-material",
        "--target", "Blender",
        "-o", str(tmp_dir),
        *[str(p) for p in ptex_files],
    ]
    log_path = tmp_dir / "material_maker.log"
    expected = [tmp_dir / f"{p.stem}_albedo.png" for p in ptex_files]
    with open(log_path, "w", encoding="utf-8") as log_f:
        proc = subprocess.Popen(
            cmd,
            stdin=subprocess.DEVNULL,
            stdout=log_f,
            stderr=subprocess.STDOUT,
            start_new_session=True,
        )
        deadline = time.monotonic() + timeout_s
        prev_sizes: list[int] | None = None
        try:
            while time.monotonic() < deadline:
                if all(f.is_file() for f in expected):
                    sizes = [f.stat().st_size for f in expected]
                    if all(s > 0 for s in sizes) and sizes == prev_sizes:
                        break
                    prev_sizes = sizes
                if proc.poll() is not None:
                    break
                time.sleep(0.25)
        finally:
            if proc.poll() is None:
                try:
                    os.killpg(proc.pid, signal.SIGTERM)
                    proc.wait(timeout=5)
                except Exception:
                    try:
                        os.killpg(proc.pid, signal.SIGKILL)
                    except Exception:
                        pass
    if not all(f.is_file() and f.stat().st_size > 0 for f in expected):
        tail = log_path.read_text(encoding="utf-8", errors="replace")[-1500:] if log_path.is_file() else ""
        raise RuntimeError(f"Material Maker export did not produce expected PNGs:\n{tail}")


def verify_and_copy(src: Path, dst: Path, target_size: int = 512) -> tuple[tuple[int, int], float]:
    with Image.open(src) as im:
        if im.width > target_size or im.height > target_size:
            im = im.resize((target_size, target_size), Image.Resampling.LANCZOS)
        rgb = im.convert("L")
        stat = ImageStat.Stat(rgb)
        stddev = stat.stddev[0]
        if stddev < 1.0:
            raise RuntimeError(f"Exported texture {src.name} looks flat (stddev={stddev:.2f})")
        im.save(dst, format="PNG")
        return im.size, stddev


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--only", type=str, default="")
    args = parser.parse_args()

    ptex_files = sorted(PTEX_DIR.glob("*.ptex"))
    if args.only:
        wanted = set(args.only.split(","))
        ptex_files = [p for p in ptex_files if p.stem in wanted]
    if not ptex_files:
        raise RuntimeError(f"No .ptex files selected in {PTEX_DIR}")

    args.out.mkdir(parents=True, exist_ok=True)
    mm_bin = find_material_maker()
    with tempfile.TemporaryDirectory(prefix="mm-vfx-") as tmp:
        tmp_dir = Path(tmp)
        export_ptex_batch(mm_bin, ptex_files, tmp_dir)
        for p in ptex_files:
            src = tmp_dir / f"{p.stem}_albedo.png"
            if not src.is_file():
                raise FileNotFoundError(f"Missing Material Maker output: {src}")
            dst = args.out / f"{p.stem}.png"
            target_sz = 256 if "SoftGlow" in p.stem else 512
            size, stddev = verify_and_copy(src, dst, target_size=target_sz)
            print(f"[build_vfx_textures] {dst.name}: {size[0]}x{size[1]} stddev={stddev:.1f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
