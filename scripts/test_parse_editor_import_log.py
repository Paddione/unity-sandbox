import importlib.util
import subprocess
import sys
from pathlib import Path

SCRIPT = Path(__file__).with_name("parse-editor-import-log.py")

SAMPLE = """\
Start importing Assets/Textures/grass.png ...
Done importing Assets/Textures/grass.png in 1.50 seconds
Start importing Assets/Models/tree.fbx ...
Done importing Assets/Models/tree.fbx in 0.50 seconds
Start importing Assets/Prefabs/tree.prefab ...
Done importing Assets/Prefabs/tree.prefab in 0.25 seconds
"""


def load_module():
    spec = importlib.util.spec_from_file_location("parse_editor_import_log",
                                                  SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_parse_aggregates_by_extension():
    mod = load_module()
    stats = mod.parse_import_log(SAMPLE)
    assert stats["done"] == 3
    assert stats["start"] == 3
    assert stats["by_ext"][".png"] == [1, 1.50]
    assert stats["slowest"][0][0].endswith("grass.png")


def test_parse_handles_log_without_durations():
    mod = load_module()
    stats = mod.parse_import_log("Start importing Assets/a.png ...\n")
    assert stats["start"] == 1
    assert stats["done"] == 0


def test_cli_reads_stdin():
    result = subprocess.run([sys.executable, str(SCRIPT)],
                            input=SAMPLE, capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    assert ".png" in result.stdout
