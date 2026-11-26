#!/usr/bin/env python3
"""Fail-closed asset-bloat audit (Goal unity-asset-pipeline-iteration).

Whole-package imports are the largest avoidable drag: Unity's import window
opens with every item selected, pulling demo scenes, variants, hi-res textures,
docs and sample code. Example: Lowpoly Township Set is 497 MB to download while
the one needed prefab (meshes, materials, textures) is only 33.7 MB.
Every file under Assets is re-imported into the Library cache on fresh clones
or Library deletes, so unused files cost import, compile, clone and disk time
even though Unity strips unused assets at build time (except Resources/).

This script enforces three budgets over sandbox/Assets (stdlib only):
  TOTAL_BUDGET ......... 500 MB  (whole Assets tree)
  SINGLE_FILE_BUDGET ... 100 MB  (one committed file; packages hide here)
  THIRDPARTY_SHARE ..... 60 %    (ThirdParty must stay below of total)

Usage: python3 scripts/asset-bloat-audit.py [assets-dir]
Exit 1 lists every budget breach; exit 0 prints the size report.
"""

import os
import sys

TOTAL_BUDGET = 500 * 1024 * 1024
SINGLE_FILE_BUDGET = 100 * 1024 * 1024
THIRDPARTY_SHARE = 0.60

IGNORED_DIRS = {".git", "Library", "Temp", "obj", "bin"}


def audit_assets(assets_dir, total_budget=TOTAL_BUDGET,
                 single_budget=SINGLE_FILE_BUDGET,
                 thirdparty_share=THIRDPARTY_SHARE):
    """Walk assets_dir; return (errors, report-lines). Pure stdlib, no Unity."""
    assets_dir = os.path.abspath(assets_dir)
    if not os.path.isdir(assets_dir):
        return [f"assets dir missing: {assets_dir}"], []
    total = 0
    top_dirs = {}
    oversized = []
    for dirpath, dirnames, filenames in os.walk(assets_dir):
        dirnames[:] = sorted(d for d in dirnames
                             if d not in IGNORED_DIRS and not d.startswith("."))
        for name in sorted(filenames):
            if name.endswith(".meta"):
                continue
            full = os.path.join(dirpath, name)
            try:
                size = os.path.getsize(full)
            except OSError:
                continue
            total += size
            rel = os.path.relpath(full, assets_dir)
            top = rel.split(os.sep)[0]
            top_dirs[top] = top_dirs.get(top, 0) + size
            if size > single_budget:
                oversized.append((rel, size))
    thirdparty = sum(size for top, size in top_dirs.items()
                     if top == "ThirdParty")
    lines = [f"total={total} bytes ({total / 1048576:.1f} MB) under {assets_dir}"]
    for top in sorted(top_dirs):
        lines.append(f"  {top}: {top_dirs[top] / 1048576:.1f} MB")
    errors = []
    if total > total_budget:
        errors.append(f"total {total} bytes exceeds budget {total_budget} bytes "
                      f"({total_budget / 1048576:.0f} MB): import only prefab + "
                      f"dependencies, remove unused files")
    for rel, size in sorted(oversized):
        errors.append(f"oversized file {rel} ({size / 1048576:.1f} MB > "
                      f"{single_budget / 1048576:.0f} MB budget)")
    if total > 0 and thirdparty / total > thirdparty_share:
        errors.append(f"ThirdParty share {thirdparty / total:.0%} exceeds "
                      f"{thirdparty_share:.0%} of total: audit unused packages")
    return errors, lines


def main(argv):
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        "..", "sandbox", "Assets")
    if len(argv) > 1:
        root = argv[1]
    errors, lines = audit_assets(root)
    for line in lines:
        print(f"asset-bloat-audit: {line}")
    if errors:
        for err in errors:
            print(f"asset-bloat-audit: FAIL {err}", file=sys.stderr)
        return 1
    print("asset-bloat-audit: OK (budgets hold)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
