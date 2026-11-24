# Progress: cinderreach-voice-batch

Prozent: 100 %

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
- 2026-11-24: B2-GPU-Fenster auf explizites Eigner-Go („now gpu fenster") ausgeführt
  (Stopp → Render → Restart + Probe): Inkling gestoppt (inactive dead), VRAM
  15075 → 1712 MiB frei, `TTS_DEVICE=cuda:0`-Render mit lokalem Qwen-TTS (`voice-tts`-venv)
  = `[render] 0 of 115 lines`, Exit 0 (alle 8 Refs vorhanden, kein Design-Modell nötig),
  Inkling restartet + `/health` ok (`inkling-2-mini-sq-10`, `loaded true`), VRAM nachher
  14167 MiB. Beleg: `docs/voice-batch-render.log`. (Loop-Schönheitsfehler: Health-Wait
  lief alle 24 Tries trotz ok ab Try 3 — Service war durchgehend healthy.)

## Nächste Schritte

- B1: erledigt/verifiziert. B2: ausgeführt am 2026-11-24 (`0 of 115`, Inkling
  restoret + healthy) — bei künftigen S2-Neutexte (Stale > 0) neues Fenster nötig.
- B3: Abnahme grün am 2026-11-24 — QA `clips 115, mean CER 0.007, above 0.25: 0`
  (schlechteste: `bark.abbot.death` 0.14); Gates `check-voice-lines` 115/0,
  `check-meta` OK, `check-audio-manifest` OK (35 cues, 8 tracks); Suiten EditMode
  539/539 + PlayMode 303/303 (Unity 6000.0.85f2). Belege: `docs/voice-batch-run-log.md`,
  `docs/dev-journal.md` (Eintrag 2026-11-24).

## Offene Punkte

Keine.
