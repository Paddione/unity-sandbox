#!/usr/bin/env python3
"""Static health probe G-DIABLO08: guard frame hot-paths against per-frame lookups.

Ensures LootDropView.LateUpdate and DiabloClickController.Update / UpdateCamera
do not reintroduce per-frame Camera.main, transform.Find / .Find(, or
GetComponent<DiabloInventoryUI>() calls. Fail-closed on missing files/methods.

Usage: python3 scripts/check-hotpath-caching.py [repo-root]
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

DEFAULT_ROOT = Path(__file__).resolve().parent.parent

HOTPATH_RULES = [
    (
        "sandbox/Assets/Scripts/Loot/LootDropView.cs",
        "LateUpdate",
        [
            (r"\bCamera\.main\b", "Camera.main lookup inside LootDropView.LateUpdate"),
            (r"\.\s*Find\s*\(", ".Find(...) lookup inside LootDropView.LateUpdate"),
        ],
    ),
    (
        "sandbox/Assets/Scripts/DiabloClickController.cs",
        "Update",
        [
            (r"\bCamera\.main\b", "Camera.main lookup inside DiabloClickController.Update"),
            (
                r"\bGetComponent\s*<\s*DiabloInventoryUI\s*>\s*\(",
                "GetComponent<DiabloInventoryUI>() lookup inside DiabloClickController.Update",
            ),
        ],
    ),
    (
        "sandbox/Assets/Scripts/DiabloClickController.cs",
        "UpdateCamera",
        [
            (r"\bCamera\.main\b", "Camera.main lookup inside DiabloClickController.UpdateCamera"),
        ],
    ),
]

_COMMENT_OR_STRING_RE = re.compile(
    r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/'
)


def strip_comments(source: str) -> str:
    """Strip C# line and block comments while keeping string literals intact."""
    def _replace(match: re.Match[str]) -> str:
        text = match.group(0)
        if text.startswith("//") or text.startswith("/*"):
            return "\n" * text.count("\n")
        return text

    return _COMMENT_OR_STRING_RE.sub(_replace, source)


def extract_method_body(source: str, method_name: str) -> str | None:
    """Return the braced body of the named C# method, or None if not found."""
    clean = strip_comments(source)
    sig_re = re.compile(rf"\b{re.escape(method_name)}\s*\([^;{{}}]*\)\s*\{{")
    match = sig_re.search(clean)
    if match is None:
        return None
    brace_start = clean.find("{", match.start())
    if brace_start < 0:
        return None
    depth = 0
    for idx in range(brace_start, len(clean)):
        ch = clean[idx]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return clean[brace_start + 1 : idx]
    return None


def audit_hotpaths(repo_root: Path | str | None = None) -> tuple[list[str], list[str]]:
    """Audit hot-path methods under repo_root; return (errors, checked_labels)."""
    root = Path(repo_root) if repo_root is not None else DEFAULT_ROOT
    errors: list[str] = []
    checked: list[str] = []
    cache: dict[str, str] = {}

    for rel_path, method_name, forbidden_patterns in HOTPATH_RULES:
        full_path = root / rel_path
        if not full_path.is_file():
            errors.append(f"missing file: {rel_path}")
            continue
        if rel_path not in cache:
            cache[rel_path] = full_path.read_text(encoding="utf-8")
        body = extract_method_body(cache[rel_path], method_name)
        if body is None:
            errors.append(f"missing method {method_name}() in {rel_path}")
            continue
        checked.append(f"{rel_path}:{method_name}")
        for pattern, message in forbidden_patterns:
            if re.search(pattern, body):
                errors.append(f"{rel_path}:{method_name}: {message}")

    return errors, checked


def main(argv: list[str] | None = None) -> int:
    args = sys.argv[1:] if argv is None else argv
    root = Path(args[0]) if args else DEFAULT_ROOT
    errors, checked = audit_hotpaths(root)
    if errors:
        for err in errors:
            print(f"check-hotpath-caching FAIL: {err}", file=sys.stderr)
        return 1
    print(f"check-hotpath-caching: OK ({len(checked)} hot-path methods verified)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
