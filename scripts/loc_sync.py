"""Sync loc-pending.txt with the current lint findings (cinderreach-deutsch-story S2).

Drops pending lines the lint no longer finds and prints findings that are on
neither list (they need a catalog entry or an allowlist line).
Usage: python3 scripts/loc_sync.py
"""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TESTS = ROOT / "sandbox/Assets/Tests/EditMode"
found = set(subprocess.run([sys.executable, str(ROOT / "scripts/loc_lint.py"), str(ROOT / "sandbox/Assets/Scripts")],
                           capture_output=True, text=True, check=True).stdout.splitlines())
pending_path, allow_path = TESTS / "loc-pending.txt", TESTS / "loc-allowlist.txt"
pending = pending_path.read_text(encoding="utf-8").splitlines()
allow = allow_path.read_text(encoding="utf-8").splitlines()
keep = [l for l in pending if l.startswith("#") or l in found]
pending_path.write_text("\n".join(keep) + "\n", encoding="utf-8")
stale_allow = [l for l in allow if l and not l.startswith("#") and l not in found]
listed = set(keep) | set(allow)
new = sorted(f for f in found if f not in listed)
for l in stale_allow:
    print("STALE-ALLOW", l)
for l in new:
    print("UNLISTED", l)
print(f"pending {sum(1 for l in keep if not l.startswith('#'))}, unlisted {len(new)}, stale allow {len(stale_allow)}")
