#!/usr/bin/env python3
"""Summarize a Unity (NUnit 3) test result XML for run-unity-tests.sh.

Prints the pass/fail line, every failing test with its message and the first
project stack frame, and the slowest tests, so a red run is readable from the
console without opening the XML. Appends the same text to
$GITHUB_STEP_SUMMARY when set.

Exit 0 only when the run passed with at least one test (or, with
--allow-empty, when a filtered run matched nothing) and no test failed.

Usage: unity_test_report.py <result.xml> <platform> [--slowest N] [--allow-empty]
"""
from __future__ import annotations

import argparse
import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

FRAME = re.compile(r"\(at (Assets/[^)]+)\)")


def first_frame(stack: str) -> str | None:
    match = FRAME.search(stack or "")
    return match.group(1) if match else None


def summarize(path: Path, platform: str, slowest: int, allow_empty: bool) -> tuple[str, bool]:
    if not path.is_file():
        return f"{platform}: test result XML missing ({path})", False
    root = ET.parse(path).getroot()
    total = int(root.get("total", "0"))
    failed = int(root.get("failed", "0"))
    passed = int(root.get("passed", "0"))
    skipped = int(root.get("skipped", "0"))
    duration = float(root.get("duration", "0") or 0)
    lines = [
        f"{platform}: {passed}/{total} passed, {failed} failed, {skipped} skipped "
        f"({root.get('result')}, {duration:.1f} s)"
    ]
    cases = list(root.iter("test-case"))
    failures = [c for c in cases if c.get("result") == "Failed"]
    for case in failures:
        message = (case.findtext("failure/message") or "").strip()
        frame = first_frame(case.findtext("failure/stack-trace") or "")
        lines.append(f"  FAIL {case.get('fullname')}" + (f" ({frame})" if frame else ""))
        for msg_line in message.splitlines()[:6]:
            lines.append(f"       {msg_line.strip()}")
    if slowest > 0 and cases:
        ranked = sorted(cases, key=lambda c: -float(c.get("duration", "0") or 0))[:slowest]
        lines.append(f"  slowest {len(ranked)}:")
        for case in ranked:
            lines.append(f"    {float(case.get('duration', '0') or 0):6.2f} s  {case.get('fullname')}")
    ok = not failures and failed == 0
    if total < 1:
        ok = ok and allow_empty
        lines.append("  no tests ran" + (" (filtered run)" if allow_empty else ""))
    elif root.get("result") != "Passed":
        ok = False
    return "\n".join(lines), ok


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("result", type=Path)
    parser.add_argument("platform")
    parser.add_argument("--slowest", type=int, default=5)
    parser.add_argument("--allow-empty", action="store_true")
    args = parser.parse_args(argv)
    text, ok = summarize(args.result, args.platform, args.slowest, args.allow_empty)
    print(text)
    summary_file = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_file:
        with open(summary_file, "a", encoding="utf-8") as stream:
            stream.write("```\n" + text + "\n```\n\n")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
