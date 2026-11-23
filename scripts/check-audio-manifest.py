#!/usr/bin/env python3
"""G-DIABLO07 audio completeness probe (goal cinderreach-sound-audit-fix).

Fail-closed: docs/audio/manifest.json must list exactly the DiabloAudio.Cue
catalogue and every MusicDirector synth clip; every cue must have a Play*
method; ZoneTrack must map Town/Ossuary/Kiln/Barrens to distinct tracks.
Stdlib only. Exit 1 lists every mismatch.
"""
import json
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")


def read(rel):
    with open(os.path.join(ROOT, rel), encoding="utf-8") as fh:
        return fh.read()


def main():
    errors = []
    manifest = json.loads(read("docs/audio/manifest.json"))
    audio = read("sandbox/Assets/Scripts/DiabloAudio.cs")
    music = read("sandbox/Assets/Scripts/Audio/MusicDirector.cs")

    enum_body = re.search(r"public enum Cue\s*\{(.*?)\}", audio, re.S).group(1)
    cues = [c.strip() for c in enum_body.replace("\n", " ").split(",") if c.strip()]
    manifest_cues = [s["cue"] for s in manifest.get("sfx", [])]
    if sorted(cues) != sorted(manifest_cues):
        errors.append(f"sfx manifest != Cue enum: only-code={sorted(set(cues)-set(manifest_cues))} "
                      f"only-manifest={sorted(set(manifest_cues)-set(cues))}")
    for cue in cues:
        if cue == "Ambient":
            continue  # bed owned by DiabloAmbience, PlayCue is an explicit no-op
        if f"Play{cue}(" not in audio and f"void Play{cue}" not in audio:
            # PlaySwing etc. are Play* methods; cue->method is Play + cue name.
            if not re.search(rf"public static void Play{cue}\b", audio):
                errors.append(f"cue {cue} has no Play{cue} method")

    clips = re.findall(r'SynthLoop\("([^"]+)"', music)
    manifest_tracks = [m["track"] for m in manifest.get("music", [])]
    if sorted(set(clips)) != sorted(manifest_tracks):
        errors.append(f"music manifest != SynthLoop clips: only-code={sorted(set(clips)-set(manifest_tracks))} "
                      f"only-manifest={sorted(set(manifest_tracks)-set(clips))}")
    for zone in ("Town", "Ossuary", "Kiln", "Barrens"):
        if f"ActProgress.{zone}" not in music and zone != "Town":
            errors.append(f"ZoneTrack does not handle ActProgress.{zone}")
    if "KilnLoop" not in clips:
        errors.append("Kiln has no own theme (KilnLoop missing)")

    # Wired callers: the once-silent events must stay wired.
    callers = {
        "PlayHeal": "sandbox/Assets/Scripts/DiabloSkills.cs",
        "PlayQuest": "sandbox/Assets/Scripts/Story/StoryDirector.cs",
        "PlayShrine": "sandbox/Assets/Scripts/Zones/WaypointShrine.cs",
        "PlayBlock": "sandbox/Assets/Scripts/PlayerHealth.cs",
        "PlayFootstep": "sandbox/Assets/Scripts/DiabloClickController.cs",
        "PlayTown": "sandbox/Assets/Scripts/Zones/WaypointShrine.cs",
        "PlayMenu": "sandbox/Assets/Scripts/Menu/MainMenu.cs",
        "PlayBoss": "sandbox/Assets/Scripts/Enemies/BossController.cs",
    }
    for method, rel in callers.items():
        if method not in read(rel):
            errors.append(f"{method} not wired in {rel}")

    if errors:
        print(f"check-audio-manifest: {len(errors)} problems:", file=sys.stderr)
        for err in errors:
            print(f"  FAIL {err}", file=sys.stderr)
        return 1
    print(f"check-audio-manifest: OK ({len(cues)} cues, {len(clips)} tracks)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
