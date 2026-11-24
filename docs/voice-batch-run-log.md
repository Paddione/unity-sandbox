# Run-Log — cinderreach-voice-batch (2026-11-24, Worktree `feature/cinderreach-voice-batch`)

## B1 — Preflight (kein GPU-Eingriff)

- `python3 scripts/check-voice-lines.py` → `voice lines: 115, problems: 0`, Exit 0 (Stale 0).
- Lock `docs/audio/voice-lines.lock.json` = `{"lines": 115}` (Beispiel `bark.abbot.death`).
- `voices.json`: 8 Stimmen (ottilie, ilsabe, jorin, merle, narrator, abbot, kilnmother, glasswright).
- Inkling `:1919` healthy (`inkling-2-mini-sq-10`, `loaded true`), VRAM 15046/16303 MiB.
- Worker-Hinweis: erster B1-Versuch via `vllm-4b` mit ECONNREFUSED gescheitert (vLLM `:8123/:8001`
  down) → Retry via `muse-4b` ok. Beleg: `docs/voice-batch-preflight.md`.
- Qwen-TTS lokal (kein Cloud-TTS): venv `~/unsloth-boxes/voice-tts/.venv/bin/python` mit
  `qwen_tts`; HF-Cache `models--Qwen--Qwen3-TTS-12Hz-0.6B-Base` (2,4 G) +
  `models--Qwen--Qwen3-TTS-12Hz-1.7B-VoiceDesign` (4,3 G).

## B2 — GPU-Fenster (Eigner-Go „now gpu fenster", 2026-11-24 14:49 UTC)

- `systemctl --user stop inkling2-mini-strata.service` → inactive (dead); VRAM 15075 → 1712 MiB.
- Render: `TTS_DEVICE=cuda:0 …/voice-tts/.venv/bin/python scripts/voice/render_voices.py`
  → `[render] 0 of 115 lines`, Exit 0 (alle 8 `ref.ogg` vorhanden → nur Klon-Modell-Pfad,
  kein Modell-Load nötig). Beleg: `docs/voice-batch-render.log`.
- `systemctl --user start inkling2-mini-strata.service`; `/health` ok ab ~20 s
  (`inkling-2-mini-sq-10`, `loaded true`); VRAM nachher 14167 MiB.

## B3 — Proof (CPU)

- QA: `…/stt-server/.venv/bin/python scripts/voice/qa_transcribe.py --max-cer 0.25`
  → `clips 115, mean CER 0.007, above 0.25: 0`, Exit 0.
  Schlechteste sichtbare Zeilen: `bark.abbot.death` 0.14, `bark.abbot.phase2` 0.05,
  `bark.glasswright.phase2` 0.04 (alle unter Schwelle).
- Gates: `check-voice-lines.py` 115/0 ok; `check-meta.py` OK; `check-audio-manifest.py`
  OK (35 cues, 8 tracks).
- Suiten: EditMode 539/539 (20,6 s), PlayMode 303/303 (138,1 s), Unity 6000.0.85f2, Exit 0.

## Health-Goals

- G-DIABLO07: `scripts/check-audio-manifest.py` = OK (35 cues, 8 tracks).
- G-DIABLO03: Referenz-Skript `scripts/health_probes/diablo_unity.py` existiert weder im
  Worktree noch auf `main` (veraltete GOAL-Referenz) — Substanz via EditMode 539/539 +
  PlayMode 303/303 + `check-meta.py` OK abgedeckt.
