import importlib.util
import subprocess
import sys
from pathlib import Path

SCRIPT = Path(__file__).with_name("asset-bloat-audit.py")


def load_module():
    spec = importlib.util.spec_from_file_location("asset_bloat_audit", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_bloat_audit_passes_on_real_assets():
    result = subprocess.run([sys.executable, str(SCRIPT)],
                            capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    assert "OK" in result.stdout


def test_bloat_audit_flags_oversized_file(tmp_path):
    mod = load_module()
    assets = tmp_path / "Assets"
    (assets / "ThirdParty").mkdir(parents=True)
    (assets / "pad.bin").write_bytes(b"x" * 64)
    errors, _ = mod.audit_assets(str(assets), total_budget=10 ** 9,
                                 single_budget=32,
                                 thirdparty_share=0.99)
    assert any("oversized file" in e for e in errors)


def test_bloat_audit_flags_thirdparty_share(tmp_path):
    mod = load_module()
    assets = tmp_path / "Assets"
    (assets / "ThirdParty").mkdir(parents=True)
    (assets / "ThirdParty" / "pack.bin").write_bytes(b"x" * 100)
    (assets / "tiny.txt").write_bytes(b"x")
    errors, _ = mod.audit_assets(str(assets), total_budget=10 ** 9,
                                 single_budget=10 ** 9,
                                 thirdparty_share=0.5)
    assert any("ThirdParty share" in e for e in errors)


def test_bloat_audit_missing_dir():
    mod = load_module()
    errors, _ = mod.audit_assets("/nonexistent-assets-dir-xyz")
    assert errors
