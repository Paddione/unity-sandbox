"""Spec skeleton for one file from loc-pending.txt (cinderreach-deutsch-story S2).

Prints `<file> | <literal as in source> | KEY | <English body>` per pending line;
replace KEY and the text with key and German (holes may stay as {expr}).
Usage: python3 scripts/loc_skel.py <path under Assets/Scripts>
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
rel = sys.argv[1]
src = (ROOT / "sandbox/Assets/Scripts" / rel).read_text(encoding="utf-8")
for line in (ROOT / "sandbox/Assets/Tests/EditMode/loc-pending.txt").read_text(encoding="utf-8").splitlines():
    if not line.startswith(rel + "\t"):
        continue
    body = line.split("\t", 1)[1]
    for pre in ("$\"", "\""):
        lit = pre + body + "\""
        if lit in src:
            print(f"{rel} | {lit} | KEY | {body}")
            break
    else:
        print(f"# not found as whole literal: {body}")
