import subprocess
import sys
from pathlib import Path

SCRIPT = Path(__file__).with_name("check-audio-manifest.py")


def test_audio_manifest_matches_code():
    result = subprocess.run([sys.executable, str(SCRIPT)], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    assert "OK" in result.stdout
