"""Pytest checks for deterministic procedural event synth (mastered)."""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent / "audio"))
sys.path.insert(0, str(Path(__file__).resolve().parent))

from audio.synth_events import synth_event  # noqa: E402
from audio.render_sfx import master_sfx  # noqa: E402

KEYS = ["Gate", "Heal", "Quest", "RareDrop", "LevelUp", "Shrine"]


def _rms(xs):
    return math.sqrt(sum(x * x for x in xs) / len(xs)) if xs else 0.0


def test_synth_events():
    for key in KEYS:
        s1, sr1 = synth_event(key)
        s2, sr2 = synth_event(key)
        assert sr1 == 44100 and sr2 == 44100
        assert s1 == s2, f"{key}: non-deterministic"
        assert max((abs(x) for x in s1), default=0.0) <= 0.99 + 1e-12
        assert len(s1) / sr1 <= 1.8 + 1e-9
        # natural decay: last 50ms RMS >= 20dB below peak
        tail = s1[-int(0.05 * sr1):]
        peak = max((abs(x) for x in s1), default=1e-12)
        tail_rms = _rms(tail)
        assert 20.0 * math.log10(tail_rms / peak + 1e-12) <= -20.0, (
            f"{key}: tail not decayed")
        # mastered crest >= 8dB
        _, m = master_sfx(list(s1), 44100, category="event", loop=False,
                           max_duration_s=1.8)
        crest = m["peak_dbfs"] - m["rms_dbfs"]
        assert crest >= 8.0, f"{key}: crest {crest} < 8dB ({m})"
