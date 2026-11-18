#!/usr/bin/env python3
"""FPS gate over FpsReporter log lines (track P4).

Reads `[Perf] FPS avg <x> (<y> ms/frame) over <n> frames.` lines from a run
log and fails when any window averages below the threshold. Stdlib only.

Usage:
    python3 scripts/fps-gate.py <run-log> --min-fps 30 --resolution 1280x720
"""
import re
import sys

LINE_RE = re.compile(r"\[Perf\] FPS avg ([0-9.,]+) \(([0-9.,]+) ms/frame\) over (\d+) frames\.")


def _num(raw: str) -> float:
    return float(raw.replace(",", "."))


def main(argv):
    if len(argv) < 2 or "--min-fps" not in argv:
        print("usage: fps-gate.py <run-log> --min-fps <n> [--resolution WxH] [--skip-first N]",
              file=sys.stderr)
        return 2
    log = argv[1]
    min_fps = float(argv[argv.index("--min-fps") + 1])
    resolution = argv[argv.index("--resolution") + 1] if "--resolution" in argv else "unknown"
    # Warmup exclusion: the first window after load covers scene load, not play.
    # Explicit in the invocation so a skipped hitch never hides silently.
    skip_first = int(argv[argv.index("--skip-first") + 1]) if "--skip-first" in argv else 0
    try:
        text = open(log, encoding="utf-8", errors="replace").read()
    except OSError as exc:
        print(f"fps-gate: cannot read {log}: {exc}", file=sys.stderr)
        return 2
    windows = [_num(m.group(1)) for m in LINE_RE.finditer(text)]
    if not windows:
        print(f"fps-gate: no [Perf] windows in {log} — FpsReporter absent?", file=sys.stderr)
        return 2
    judged = windows[skip_first:]
    if not judged:
        print(f"fps-gate: nothing left after --skip-first {skip_first}", file=sys.stderr)
        return 2
    worst = min(judged)
    skipped_note = f" (skipped first {skip_first} warmup)" if skip_first else ""
    print(f"fps-gate: {len(judged)} judged windows at {resolution}{skipped_note}, "
          f"worst avg {worst:.1f} fps (threshold {min_fps:.1f})")
    if worst < min_fps:
        print("fps-gate: FAIL", file=sys.stderr)
        return 1
    print("fps-gate: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
