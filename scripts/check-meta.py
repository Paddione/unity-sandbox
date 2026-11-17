#!/usr/bin/env python3
"""Fail-closed .meta completeness check (track P5).

Every file and directory under sandbox/Assets must have a sibling .meta file;
Unity generates them on import, but a missing .meta (uncommitted, gitignored,
or lost in a merge) silently breaks GUID references. Stdlib only.

Usage: python3 scripts/check-meta.py [assets-dir]
Exit 1 lists every path missing its .meta.
"""
import os
import sys

IGNORED_DIRS = {".git", "Library", "Temp", "obj", "bin"}


def main(argv):
    root = argv[1] if len(argv) > 1 else os.path.join(
        os.path.dirname(os.path.abspath(__file__)), "..", "sandbox", "Assets")
    root = os.path.abspath(root)
    missing = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = sorted(d for d in dirnames if d not in IGNORED_DIRS and not d.startswith("."))
        if dirpath != root and not os.path.isfile(dirpath + ".meta"):
            missing.append(os.path.relpath(dirpath, root) + "/")
        for name in sorted(filenames):
            if name.endswith(".meta"):
                continue
            full = os.path.join(dirpath, name)
            if not os.path.isfile(full + ".meta"):
                missing.append(os.path.relpath(full, root))
    if missing:
        print(f"check-meta: {len(missing)} paths missing .meta:", file=sys.stderr)
        for path in missing:
            print(f"  MISSING {path}", file=sys.stderr)
        return 1
    print(f"check-meta: OK ({root})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
