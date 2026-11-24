# Progress: cinderreach-voice-batch

Prozent: 0 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (Umfang = nur Stale, GPU = CUDA mit Auto-Restore,
  Stimmen = 8 Bestand, Ablage = S2-Ausführung von `cinderreach-sprache-voice`).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und
  Ausführungsplan `.agents/plans/cinderreach-voice-batch/tasks.md` geschrieben.
- 2026-11-24: B1-Preflight im Worktree verifiziert (vLLM-Worker ECONNREFUSED → Retry via
  muse-4b ok): `check-voice-lines.py` = `voice lines: 115, problems: 0` (Stale-Count 0),
  Lock `docs/audio/voice-lines.lock.json` = `{"lines": 115}`, Inkling `:1919` healthy,
  VRAM 15046/16303 MiB (~92 % durch Inkling belegt). Beleg: `docs/voice-batch-preflight.md`.
  Qwen-TTS lokal bestätigt (kein Cloud-TTS): venv `~/unsloth-boxes/voice-tts/.venv/bin/python`
  mit `qwen_tts`, Modelle im HF-Cache (`Qwen3-TTS-12Hz-0.6B-Base` 2,4 G + `1.7B-VoiceDesign`
  4,3 G). B1-Korrektur: Workermeldung „Cloud-Fallback" gilt nicht für TTS — B2 rendert
  lokal mit `TTS_DEVICE=cuda:0` nach Inkling-Pause.

## Nächste Schritte

- B1: erledigt/verifiziert (Stale 0 zum 2026-11-24-Stand) — kein weiterer B1-Lauf nötig,
  vor B2 nochmals kurz re-verifizieren (Stale kann mit S2-Texten > 0 werden).
- B2: HOLD — aktuell würde der Batch 0 Zeilen rendern (Stale 0); zudem Textstände aus
  Akt-I/II/III-S1 via `cinderreach-sprache-voice` S2 noch offen. B2 erst wenn S2-Neutexte
  im Lock als stale sichtbar sind, dann: Inkling pausieren, `TTS_DEVICE=cuda:0`-Render
  mit lokalem Qwen-TTS, Restart + Probe.
- B3: Abnahme (QA-Transkription, Voice-Gates, Suiten, Run-Log + Doku).

## Offene Punkte

- Eigner-Go für das GPU-Fenster steht aus (Inkling-Pause auf der 5070 Ti) — Go erst
  anfordern, wenn B2 tatsächlich Stale > 0 hat (sonst Inkling-Stopp für 0 Zeilen).
