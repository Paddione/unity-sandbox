#!/usr/bin/env python3
"""Parse Unity Editor.log import lines into an import-time report.

If iteration on visuals/audio feels slow, measure import first. Editor.log
`Start Importing ... / Done Importing ...` lines are the pipeline's trail for
Asset Import Times: each line yields asset path (and duration on Done lines);
categorizing by extension/importer and aggregating shows which asset types take
longest (Unity's own example was dominated by Textures, Models and Prefabs —
treat that ranking as a diagnostic starting point, your log is authoritative).

Usage:
  python3 scripts/parse-editor-import-log.py [Editor.log]
  (no arg reads stdin)
Stdlib only. Exit 0 prints the aggregate; exit 2 on unreadable input.
"""

import os
import re
import sys

START = re.compile(r"Start\s+[Ii]mporting\s+(.*?)\s*$")
DONE = re.compile(r"Done\s+[Ii]mporting\s+(.*?)(?:\s+in\s+([\d.]+)\s*s(?:ec(?:ond)?s?)?)?\s*$")


def parse_import_log(text):
    """Return {'by_ext': {ext: [count, seconds]}, 'slowest': [(path, secs)], 'done': n, 'start': m}."""
    by_ext = {}
    slowest = []
    done_count = 0
    start_count = 0
    for line in text.splitlines():
        done = DONE.search(line)
        if done:
            done_count += 1
            path = done.group(1).strip()
            try:
                secs = float(done.group(2)) if done.group(2) else 0.0
            except ValueError:
                secs = 0.0
            ext = os.path.splitext(path)[1].lower() or "(no-ext)"
            entry = by_ext.setdefault(ext, [0, 0.0])
            entry[0] += 1
            entry[1] += secs
            if secs > 0:
                slowest.append((path, secs))
            continue
        if START.search(line):
            start_count += 1
    slowest.sort(key=lambda item: item[1], reverse=True)
    return {"by_ext": by_ext, "slowest": slowest[:10],
            "done": done_count, "start": start_count}


def report(stats):
    lines = [f"imported={stats['done']} started={stats['start']}"]
    ranked = sorted(stats["by_ext"].items(),
                    key=lambda kv: kv[1][1], reverse=True)
    for ext, (count, secs) in ranked[:10]:
        lines.append(f"  {ext}: {count} assets, {secs:.2f}s")
    for path, secs in stats["slowest"][:10]:
        lines.append(f"  slowest: {path} ({secs:.2f}s)")
    if not ranked:
        lines.append("  no import lines found (not an Editor.log?)")
    return lines


def main(argv):
    if len(argv) > 1:
        try:
            with open(argv[1], encoding="utf-8", errors="replace") as fh:
                text = fh.read()
        except OSError as exc:
            print(f"parse-editor-import-log: FAIL cannot read {argv[1]}: {exc}",
                  file=sys.stderr)
            return 2
    else:
        text = sys.stdin.read()
    for line in report(parse_import_log(text)):
        print(f"parse-editor-import-log: {line}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
