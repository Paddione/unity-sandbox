"""RED test for scripts/measure-build.py (Task 1, Goal unity-build-speed-5x).

Parses a fixture [SandboxBuilder] log line and checks JSONL append.
Expected to FAIL while scripts/measure-build.py does not exist yet.
"""
import importlib.util
import json
import sys
from pathlib import Path

SCRIPT = Path(__file__).with_name("measure-build.py")

FIXTURE_LOG = (
    "[SandboxBuilder] DiabloZone build result: Succeeded "
    "in 123.4s, 101 MB\n"
)


def load_module():
    spec = importlib.util.spec_from_file_location("measure_build", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_parse_build_log_line(tmp_path):
    mod = load_module()
    entry = mod.parse_build_log(FIXTURE_LOG, target="linux-player")
    assert entry["duration_s"] == 123.4
    assert entry["size_mb"] == 101
    assert entry["target"] == "linux-player"


def test_append_metrics_jsonl(tmp_path):
    mod = load_module()
    out = tmp_path / "build-metrics.jsonl"
    mod.append_metric(
        out,
        {
            "ts": "2026-11-27T00:00:00Z",
            "label": "baseline",
            "target": "linux-player",
            "duration_s": 123.4,
            "size_mb": 101,
            "commit": "abc1234",
            "note": "baseline",
        },
    )
    row = json.loads(out.read_text(encoding="utf-8").strip().splitlines()[-1])
    assert row["duration_s"] == 123.4
    assert row["size_mb"] == 101
    assert row["target"] == "linux-player"


def test_write_build_info_json(tmp_path, monkeypatch):
    mod = load_module()
    monkeypatch.setattr(mod, "ROOT", tmp_path)
    # Echter Artefakt-Ordner (Build hat ihn angelegt) -> du-Wert wie im Matrix-Skript.
    (tmp_path / "builds" / "diablo-zone").mkdir(parents=True)
    info = mod.write_build_info(
        "builds/diablo-zone", "linux-player",
        "SandboxBuilder.BuildDiabloZoneBuild",
        "DiabloZone build result: Succeeded",
        "abc1234",
        101,
    )
    assert info["target"] == "linux-player"
    stored = json.loads(
        (tmp_path / "builds" / "diablo-zone" / "BUILD_INFO.json").read_text(
            encoding="utf-8"
        )
    )
    assert stored["commit"] == "abc1234"
    assert isinstance(stored["artifact_mb"], int)

    # Fallback ohne du (z.B. du fehlt): Log-Groesse bleibt erhalten.
    import subprocess as _sp

    monkeypatch.setattr(
        _sp, "run", lambda *a, **k: (_ for _ in ()).throw(OSError("no du"))
    )
    (tmp_path / "builds" / "diablo-zone-2").mkdir(parents=True)
    info2 = mod.write_build_info(
        "builds/diablo-zone-2", "linux-server",
        "SandboxBuilder.BuildDiabloZoneServer",
        "DiabloZone server build result: Succeeded",
        "abc1234",
        42,
    )
    assert info2["artifact_mb"] == 42


def test_write_build_info_no_phantom_dir(tmp_path, monkeypatch):
    mod = load_module()
    monkeypatch.setattr(mod, "ROOT", tmp_path)
    # Kein Artefakt -> keine BUILD_INFO.json, keine Phantom-Verzeichnisse.
    info = mod.write_build_info(
        "builds/diablo-zone", "linux-player",
        "SandboxBuilder.BuildDiabloZoneBuild",
        "DiabloZone build result: Succeeded",
        "abc1234",
        101,
    )
    assert info["artifact_mb"] == 101
    assert not (tmp_path / "builds").exists()


def test_build_out_for_targets():
    mod = load_module()
    flag, path = mod.build_out_for("linux-player")
    assert flag == "-buildOut"
    assert path.endswith("builds/diablo-zone/diablo-zone")
    assert Path(path).is_relative_to(mod.ROOT)
    _, win = mod.build_out_for("windows-player")
    assert win.endswith(".exe")
    _, srv = mod.build_out_for("linux-server")
    assert srv.endswith("builds/diablo-server/diablo-server")


def test_default_metrics_path_is_versioned():
    mod = load_module()
    assert mod.DEFAULT_METRICS == mod.ROOT / "docs" / "build-metrics.jsonl"
    assert "builds" not in mod.DEFAULT_METRICS.parts


def test_cli_note_flag_exists():
    import subprocess

    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--help"],
        capture_output=True,
        text=True,
    )
    assert result.returncode == 0, result.stderr
    assert "--note" in result.stdout
