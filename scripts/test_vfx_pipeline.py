"""Smoke tests for the Cinderreach O1 VFX tooling scripts and documentation."""
import ast
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def test_skill_vfx_python_scripts_parse_cleanly():
    vfx_dir = ROOT / "docs" / "skill-vfx"
    scripts = sorted(vfx_dir.glob("*.py"))
    assert len(scripts) >= 3, f"expected >= 3 python generators in {vfx_dir}"
    for script in scripts:
        ast.parse(script.read_text(encoding="utf-8"), filename=str(script))


def test_install_material_maker_shell_syntax():
    installer = ROOT / "scripts" / "install-material-maker.sh"
    assert installer.is_file()
    res = subprocess.run(["bash", "-n", str(installer)], capture_output=True, text=True)
    assert res.returncode == 0, res.stderr


def test_skill_vfx_readme_anchors():
    readme = (ROOT / "docs" / "skill-vfx" / "README.md").read_text(encoding="utf-8")
    for anchor in (
        "build_vfx_meshes.py",
        "build_vfx_textures.py",
        "build_vfx_flipbooks.py",
        "Cinderreach/VFX/AbilityMesh",
        "Cinderreach/VFX/AbilityFlipbook",
        "Cinderreach/VFX/AbilityGroundZone",
        "EmberGen",
    ):
        assert anchor in readme, f"missing anchor {anchor} in docs/skill-vfx/README.md"
