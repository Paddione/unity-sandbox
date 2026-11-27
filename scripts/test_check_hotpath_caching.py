import importlib.util
import subprocess
import sys
from pathlib import Path

SCRIPT = Path(__file__).with_name("check-hotpath-caching.py")


def load_module():
    spec = importlib.util.spec_from_file_location("check_hotpath_caching", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _write_clean_fixture(root: Path) -> None:
    loot = root / "sandbox/Assets/Scripts/Loot/LootDropView.cs"
    click = root / "sandbox/Assets/Scripts/DiabloClickController.cs"
    loot.parent.mkdir(parents=True, exist_ok=True)
    click.parent.mkdir(parents=True, exist_ok=True)
    loot.write_text(
        """
        public class LootDropView {
            // Camera.main and transform.Find in comments must be ignored.
            void LateUpdate() {
                if (cachedIcon == null) return;
                var cam = ResolveCamera();
                if (cam != null) cachedIcon.rotation = cam.transform.rotation;
            }
        }
        """,
        encoding="utf-8",
    )
    click.write_text(
        """
        public class DiabloClickController {
            void Update() {
                var cam = ResolveCamera();
                if (ShouldBlockClicks(ResolveInventoryUI())) return;
                TickChaseAttack(5f);
                UpdateCamera(cam);
            }
            void TickChaseAttack(float speed) {
                var driver = ResolveAnimator();
                SheatheBow();
            }
            void SheatheBow() {
                var skills = ResolveSkills();
            }
            void UpdateCamera(Camera cam) {
                if (cam == null) return;
            }
        }
        """,
        encoding="utf-8",
    )


def test_hotpath_probe_passes_on_real_repo():
    result = subprocess.run([sys.executable, str(SCRIPT)], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    assert "OK" in result.stdout


def test_flags_camera_main_in_loot_late_update(tmp_path: Path):
    mod = load_module()
    _write_clean_fixture(tmp_path)
    loot = tmp_path / "sandbox/Assets/Scripts/Loot/LootDropView.cs"
    loot.write_text(
        "public class LootDropView { void LateUpdate() { var c = Camera.main; } }",
        encoding="utf-8",
    )
    errors, _ = mod.audit_hotpaths(tmp_path)
    assert any("LootDropView.LateUpdate" in e and "Camera.main" in e for e in errors)


def test_flags_find_in_loot_late_update(tmp_path: Path):
    mod = load_module()
    _write_clean_fixture(tmp_path)
    loot = tmp_path / "sandbox/Assets/Scripts/Loot/LootDropView.cs"
    loot.write_text(
        'public class LootDropView { void LateUpdate() { var b = transform.Find("LootBall"); } }',
        encoding="utf-8",
    )
    errors, _ = mod.audit_hotpaths(tmp_path)
    assert any("LootDropView.LateUpdate" in e and ".Find(" in e for e in errors)


def test_flags_regressions_in_click_controller(tmp_path: Path):
    mod = load_module()
    _write_clean_fixture(tmp_path)
    click = tmp_path / "sandbox/Assets/Scripts/DiabloClickController.cs"
    click.write_text(
        """
        public class DiabloClickController {
            void Update() {
                var cam = Camera.main;
                if (ShouldBlockClicks(GetComponent<DiabloInventoryUI>())) return;
            }
            void TickChaseAttack(float speed) {}
            void SheatheBow() {}
            void UpdateCamera() {
                var c = Camera.main;
            }
        }
        """,
        encoding="utf-8",
    )
    errors, _ = mod.audit_hotpaths(tmp_path)
    assert len(errors) == 3


def test_flags_animator_and_skills_getcomponent_in_click_controller(tmp_path: Path):
    mod = load_module()
    _write_clean_fixture(tmp_path)
    click = tmp_path / "sandbox/Assets/Scripts/DiabloClickController.cs"
    click.write_text(
        """
        public class DiabloClickController {
            void Update() {
                var a = GetComponent<DiabloAnimator>();
                var s = GetComponent<DiabloSkills>();
            }
            void TickChaseAttack(float speed) {
                var a = GetComponent<DiabloAnimator>();
                var s = GetComponent<DiabloSkills>();
            }
            void SheatheBow() {
                var s = GetComponent<DiabloSkills>();
            }
            void UpdateCamera(Camera cam) {}
        }
        """,
        encoding="utf-8",
    )
    errors, _ = mod.audit_hotpaths(tmp_path)
    assert len(errors) == 5


def test_missing_file_fails_closed(tmp_path: Path):
    mod = load_module()
    errors, _ = mod.audit_hotpaths(tmp_path)
    assert errors
