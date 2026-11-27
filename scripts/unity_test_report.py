#!/usr/bin/env python3
"""Summarize a Unity (NUnit 3) test result XML for run-unity-tests.sh.

Prints the pass/fail line, every failing test with its message and the first
project stack frame, and the slowest tests, so a red run is readable from the
console without opening the XML. Appends the same text to
$GITHUB_STEP_SUMMARY when set.

Exit 0 only when the run passed with at least one test (or, with
--allow-empty, when a filtered run matched nothing) and no test failed.

--append-summary writes one versioned row per unfiltered full run to that
JSONL file (Goal unity-projektstand-board, D2). Filtered runs (TEST_FILTER or
TEST_CATEGORY set) and runs without tests append nothing.

Usage: unity_test_report.py <result.xml> <platform> [--slowest N] [--allow-empty]
       [--append-summary PATH]
"""
from __future__ import annotations

import argparse
import datetime
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

FRAME = re.compile(r"\(at (Assets/[^)]+)\)")
ROOT = Path(__file__).resolve().parent.parent


def git_commit() -> str:
    try:
        out = subprocess.run(
            ["git", "rev-parse", "--short", "HEAD"],
            capture_output=True,
            text=True,
            cwd=str(ROOT),
        )
        sha = out.stdout.strip()
        return sha if sha else "uncommitted"
    except OSError:
        return "uncommitted"


def read_counts(path: Path) -> dict | None:
    """Return total/passed/failed/skipped from a result XML, or None."""
    if not path.is_file():
        return None
    root = ET.parse(path).getroot()
    return {key: int(root.get(key, "0") or 0) for key in
            ("total", "passed", "failed", "skipped")}


def append_summary(path: Path, counts: dict, platform: str) -> dict:
    """Append one unfiltered full-run row as JSON and return it."""
    row = {
        "ts": datetime.datetime.now(datetime.timezone.utc)
        .isoformat(timespec="seconds")
        .replace("+00:00", "Z"),
        "commit": git_commit(),
        "platform": platform,
        "filter": "",
        **counts,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "a", encoding="utf-8") as stream:
        stream.write(json.dumps(row, sort_keys=True) + "\n")
    return row


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
    parser.add_argument(
        "--append-summary",
        type=Path,
        default=None,
        help="JSONL file for one row per unfiltered full run",
    )
    args = parser.parse_args(argv)
    text, ok = summarize(args.result, args.platform, args.slowest, args.allow_empty)
    print(text)
    if args.append_summary is not None:
        filtered = args.allow_empty or bool(
            os.environ.get("TEST_FILTER") or os.environ.get("TEST_CATEGORY")
        )
        counts = read_counts(args.result)
        if filtered:
            print(
                "unity_test_report: summary not appended (filtered run)",
                file=sys.stderr,
            )
        elif counts is None or counts["total"] < 1:
            print(
                "unity_test_report: summary not appended (no test results)",
                file=sys.stderr,
            )
        else:
            row = append_summary(args.append_summary, counts, args.platform)
            print(
                f"unity_test_report: summary appended to {args.append_summary} "
                f"({row['platform']} {row['passed']}/{row['total']})",
                file=sys.stderr,
            )
    summary_file = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_file:
        with open(summary_file, "a", encoding="utf-8") as stream:
            stream.write("```\n" + text + "\n```\n\n")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
