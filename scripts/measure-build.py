#!/usr/bin/env python3
"""measure-build.py — Mess-Harness fuer Unity-Builds (Task 1, Goal unity-build-speed-5x).

Faellt die Matrix (Linux-Player / Windows-Player / Linux-Server) ueber
scripts/run-unity-build.sh, liest je Target totalTime/totalSize aus den
[SandboxBuilder] ... build result:-Logzeilen und haengt JSON-Zeilen an
docs/build-metrics.jsonl an (Felder: ts, label, target, duration_s,
size_mb, commit, note). Die Datei ist versioniert (Goal
unity-projektstand-board, D2); builds/ bleibt gitignored.

Usage (vom Repo-Root):
  python3 scripts/measure-build.py [--note TEXT] [--targets a,b]
  [--timeout S] [--logdir DIR] [--metrics PATH]

Exit 0 = alle Targets Succeeded und Zeilen angehaengt.
Exit 1 = mindestens ein Target fehlgeschlagen (kein/partial Append moeglich).
Exit 2 = Usage-/Logikfehler. Stdlib-only.
"""

import argparse
import datetime
import json
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_METRICS = ROOT / "docs" / "build-metrics.jsonl"

# method | marker | artifact-dir | label | player-name — gespiegelt aus
# run-unity-matrix.sh. Der Player landet per -buildOut im Worktree (kein
# Schreiben in den Hauptcheckout mehr); ohne UNITY_BUILD_EXTRA_ARGS mit
# -buildOut setzt measure-build ihn automatisch (Task 2/3).
TARGETS = [
    (
        "SandboxBuilder.BuildDiabloZoneBuild",
        "DiabloZone build result: Succeeded",
        "builds/diablo-zone",
        "linux-player",
        "diablo-zone",
    ),
    (
        "SandboxBuilder.BuildDiabloZoneWindows",
        "DiabloZone Windows build result: Succeeded",
        "builds/diablo-zone-windows",
        "windows-player",
        "diablo-zone-windows.exe",
    ),
    (
        "SandboxBuilder.BuildDiabloZoneServer",
        "DiabloZone server build result: Succeeded",
        "builds/diablo-server",
        "linux-server",
        "diablo-server",
    ),
]


def build_out_for(target_label):
    """Gibt [\"-buildOut\", <worktree-absoluter Player-Pfad>] für ein Target zurück."""
    for _, _, artdir, label, player in TARGETS:
        if label == target_label:
            return ["-buildOut", str(ROOT / artdir / player)]
    raise ValueError(f"unknown target: {target_label!r}")

RESULT_RE = re.compile(
    r"\[SandboxBuilder\].*?build result:\s*(\w+)\s+in\s+([\d.]+)s,\s*(\d+)\s*MB"
)


def parse_build_log(text, target=""):
    """Parse [SandboxBuilder] build result lines.

    Gibt {'target', 'result', 'duration_s', 'size_mb'} der letzten
    Succeeded-Zeile zurueck; wirft ValueError ohne Succeeded-Zeile.
    """
    hits = RESULT_RE.findall(text)
    succeeded = [(r, d, s) for (r, d, s) in hits if r == "Succeeded"]
    if not succeeded:
        raise ValueError(f"no Succeeded build result line found (target={target!r})")
    result, duration, size = succeeded[-1]
    return {
        "target": target,
        "result": result,
        "duration_s": float(duration),
        "size_mb": int(size),
    }


def append_metric(path, row):
    """Haengt eine Metrics-Zeile als JSON an; legt Elternverzeichnisse an."""
    required = {"ts", "label", "target", "duration_s", "size_mb", "commit", "note"}
    missing = required - set(row)
    if missing:
        raise ValueError(f"metric row missing keys: {sorted(missing)}")
    path = Path(path)
    if path.parent != Path(".") and str(path.parent):
        path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "a", encoding="utf-8") as fh:
        fh.write(json.dumps(row, sort_keys=True) + "\n")
    return row


def write_build_info(artdir, target_label, method, marker, commit, size_mb):
    """Schreibt BUILD_INFO.json im Format von run-unity-matrix.sh.

    Legt keine Verzeichnisse an: Die Datei entsteht nur neben einem echten
    Build-Artefakt; sonst bleibt artifact_mb der aus dem Build-Log (Task 2).
    """
    info = {
        "target": target_label,
        "method": method,
        "marker": marker,
        "commit": commit,
        "built_at": datetime.datetime.now(datetime.timezone.utc).strftime(
            "%Y-%m-%dT%H:%M:%SZ"
        ),
        "artifact_dir": artdir,
        "artifact_mb": size_mb,
    }
    artifact_path = ROOT / artdir
    if artifact_path.is_dir():
        dest = artifact_path / "BUILD_INFO.json"
        try:
            out = subprocess.run(
                ["du", "-sm", str(artifact_path)],
                capture_output=True,
                text=True,
            )
            info["artifact_mb"] = int(out.stdout.split()[0])
        except (OSError, ValueError, IndexError):
            pass
        dest.write_text(json.dumps(info, sort_keys=True) + "\n", encoding="utf-8")
    return info


def git_commit():
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


def run_one(method, marker, logfile, timeout_s, target_label):
    """Ein Target ueber run-unity-build.sh fahren. Gibt Returncode zurueck.

    Setzt -buildOut auf den Worktree-Output, sofern der Aufrufer kein eigenes
    UNITY_BUILD_EXTRA_ARGS mit -buildOut vorgibt.
    """
    env = dict(os.environ)
    extra = env.get("UNITY_BUILD_EXTRA_ARGS", "")
    if "-buildOut" not in extra:
        out_args = build_out_for(target_label)
        env["UNITY_BUILD_EXTRA_ARGS"] = (extra + " " + " ".join(out_args)).strip()
    cmd = [
        "bash",
        str(ROOT / "scripts" / "run-unity-build.sh"),
        method,
        marker,
        str(timeout_s),
        str(logfile),
    ]
    proc = subprocess.run(cmd, cwd=str(ROOT), env=env)
    return proc.returncode


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--note", default="", help="freie Notiz (z.B. baseline)")
    parser.add_argument(
        "--targets",
        default=",".join(label for _, _, _, label, _ in TARGETS),
        help="Kommagetrennte Teilmenge von: linux-player,windows-player,linux-server",
    )
    parser.add_argument("--timeout", type=int, default=1500)
    parser.add_argument("--logdir", default=None)
    parser.add_argument("--metrics", default=str(DEFAULT_METRICS))
    args = parser.parse_args(argv)

    wanted = {t.strip() for t in args.targets.split(",") if t.strip()}
    known = {label for _, _, _, label, _ in TARGETS}
    unknown = wanted - known
    if unknown:
        print(f"measure-build: FAIL unknown targets: {sorted(unknown)}", file=sys.stderr)
        return 2
    selected = [spec for spec in TARGETS if spec[3] in wanted]
    if not selected:
        print("measure-build: FAIL no targets selected", file=sys.stderr)
        return 2

    stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d-%H%M%S")
    logdir = Path(args.logdir) if args.logdir else Path(f"/tmp/unity-matrix-{stamp}")
    logdir.mkdir(parents=True, exist_ok=True)
    commit = git_commit()
    ts = (
        datetime.datetime.now(datetime.timezone.utc)
        .isoformat(timespec="seconds")
        .replace("+00:00", "Z")
    )
    label = args.note or "matrix"

    failed = []
    for method, marker, artdir, target_label, _player in selected:
        logfile = logdir / f"{target_label}.log"
        print(f"measure-build: === {target_label} ({method}) -> {logfile}")
        rc = run_one(method, marker, logfile, args.timeout, target_label)
        if rc != 0:
            print(
                f"measure-build: {target_label} FAILED (rc={rc}, see {logfile})",
                file=sys.stderr,
            )
            failed.append(target_label)
            continue
        try:
            text = logfile.read_text(encoding="utf-8", errors="replace")
        except OSError as exc:
            print(f"measure-build: cannot read {logfile}: {exc}", file=sys.stderr)
            failed.append(target_label)
            continue
        try:
            entry = parse_build_log(text, target=target_label)
        except ValueError as exc:
            print(f"measure-build: {target_label}: {exc}", file=sys.stderr)
            failed.append(target_label)
            continue
        append_metric(
            args.metrics,
            {
                "ts": ts,
                "label": label,
                "target": entry["target"],
                "duration_s": entry["duration_s"],
                "size_mb": entry["size_mb"],
                "commit": commit,
                "note": args.note,
            },
        )
        write_build_info(
            artdir, target_label, method, marker, commit, entry["size_mb"]
        )
        print(
            f"measure-build: {target_label} ok "
            f"({entry['duration_s']}s, {entry['size_mb']} MB)"
        )

    if failed:
        print(
            f"measure-build: FAIL targets failed: {failed}", file=sys.stderr
        )
        return 1
    print(f"measure-build: OK ({len(selected)} targets, metrics: {args.metrics})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
