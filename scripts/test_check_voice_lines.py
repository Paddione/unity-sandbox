"""Tests for the story voice line parser and gate (cinderreach-story-voice)."""
import importlib.util
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE / "voice"))
import voice_lines as vl  # noqa: E402

spec = importlib.util.spec_from_file_location("check_voice_lines", HERE / "check-voice-lines.py")
check = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check)


def test_every_line_has_text_and_known_speaker():
    voices = vl.load_voices()
    entries = vl.lines()
    assert len(entries) > 100
    for e in entries:
        assert e["text"], e["key"]
        assert e["speaker"] in voices, e


def test_speakers_resolve_constants_and_flags():
    by_key = {e["key"]: e for e in vl.lines()}
    assert by_key["dlg.h1.done.2"]["speaker"] == "narrator"
    assert by_key["dlg.h3.talk.jorin.1"]["speaker"] == "jorin"
    assert by_key["dlg.h9.talk.merle.5"]["speaker"] == "merle"  # "M + \"|!n4\""
    assert by_key["bark.kilnmother.death"]["speaker"] == "kilnmother"
    assert by_key["bark.h7.done"]["speaker"] == "narrator"


def test_hash_tracks_text_and_voice():
    voices = vl.load_voices()
    e = {"key": "k", "speaker": "ottilie", "text": "Sechsundvierzig."}
    h = vl.line_hash(e, voices)
    assert h != vl.line_hash(dict(e, text="Fünfundvierzig."), voices)
    assert h != vl.line_hash(dict(e, speaker="merle"), voices)


def test_catalog_texts_decode_unicode_escapes():
    entries = {e["key"]: e for e in vl.lines()}
    for key, entry in entries.items():
        assert "\\u" not in entry["text"], f"undecoded \\u escape in {key}: {entry['text']}"
    assert "ö" in entries["dlg.h5.done.6"]["text"] and "ä" in entries["dlg.h5.done.6"]["text"]
    assert "ä" in entries["dlg.n10.offer.2"]["text"] or "ö" in entries["dlg.n10.offer.2"]["text"] or "ü" in entries["dlg.n10.offer.2"]["text"] or "ß" in entries["dlg.n10.offer.2"]["text"]


def test_repository_clips_are_complete():
    assert check.problems() == []
