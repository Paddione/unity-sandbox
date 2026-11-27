"""Tests for the SFX completeness gate (check-sfx-clips.py)."""
from __future__ import annotations

import importlib.util
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE / "audio"))
import render_sfx as rsfx  # noqa: E402

spec = importlib.util.spec_from_file_location("check_sfx_clips", HERE / "check-sfx-clips.py")
check = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check)


def test_expected_sfx_clips_cover_manifest_and_variants():
    manifest = json.loads(rsfx.MANIFEST_JSON.read_text(encoding="utf-8"))
    manifest_cues = {s["cue"] for s in manifest["sfx"]}
    items = rsfx.expected_clips(manifest)
    keys = [item["key"] for item in items]
    assert len(items) == 73
    assert len(set(keys)) == 73
    assert manifest_cues.issubset(set(keys))
    for surf in ("FleshBone", "ArmorMetal", "StoneAsh", "Glass", "GlassShield"):
        for idx in range(3):
            assert f"Hit_{surf}_{idx}" in keys
    for ground in ("Moor", "Stone", "Basalt", "Sand"):
        for idx in range(3):
            assert f"Footstep_{ground}_{idx}" in keys
    for idx in range(3):
        assert f"Swing_{idx}" in keys


def test_repository_sfx_clips_are_complete():
    assert check.problems() == []
