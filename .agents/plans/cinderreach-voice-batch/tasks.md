---
goal: cinderreach-voice-batch
slug: cinderreach-voice-batch
branch: feature/cinderreach-voice-batch
repo_main: main
tickets: none
health_goals: [G-DIABLO03, G-DIABLO07]
start_condition: main sauber; Textstände aus Akt-I-S1, Akt-II-S1, Akt-III-S1 stehen (gestaffelt via cinderreach-sprache-voice S2); Eigner-Go für Inkling-Pause liegt vor
plan_refs: [.agents/goals/cinderreach-voice-batch/GOAL.md]
---

# Plan: cinderreach-voice-batch — TTS-Batch auf der 5070 Ti (Inkling-Pause, nur Stale)

Pfad: feature, 3 Stufen in einem Worktree
(`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-voice-batch`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).

## Goal

Siehe [GOAL.md](../../goals/cinderreach-voice-batch/GOAL.md): alle geänderten
Cinderreach-Voice-Lines nach Sprach-/Quest-Rework als ein CUDA-Batch neu rendern
(nur Stale, 8 Bestandsstimmen), Inkling-Pause nur im freigegebenen Fenster, danach QA + Gates grün.

## Success Criteria

Die fünf Erfolgskriterien aus GOAL.md. Gesamt-Gate B3: `check-voice-lines.py` grün,
`qa_transcribe.py` unter Schwelle, `check-meta.py` + `check-audio-manifest.py` grün,
EditMode + PlayMode grün, Inkling nach dem Batch wieder healthy auf `:1919`.

## Ausgangslage (Belege, 2026-11-24)

- Inkling: `inkling2-mini-strata.service` auf `:1919` (SQ-10 + Vision + DSpark, `-c 131072`,
  Single-Slot), VRAM 14,3/16,3 GiB belegt, GPU idle — kein Parallelbetrieb mit TTS möglich.
- Voice-Pipeline: `scripts/voice/render_voices.py` (Default ohne `--force`/`--redesign`),
  `voice_lines.py` (Keys `dlg.*`/`bark.*`, Lock-Hash über TTS-Text + Speaker + `ref_text` +
  Seed + `instruct`), Lock `docs/audio/voice-lines.lock.json`, Clips
  `sandbox/Assets/Resources/Voice/<key>.ogg`, Gate `scripts/check-voice-lines.py`
  (Budget 25 MiB), Vertrag `scripts/check-audio-manifest.py` = G-DIABLO07.
- Stimmen: `docs/audio/voices.json` (8 Stimmen, Klon-Modell `Qwen3-TTS-12Hz-0.6B-Base`,
  Design-Modell `Qwen3-TTS-12Hz-1.7B-VoiceDesign` nur bei fehlender `ref.ogg`,
  Sprache German, max. 600 Zeichen/Zeile).
- Textquelle: `Story/StoryDialogs.cs` + `StoryBarks.cs`, Texte aus
  `Localization/LocCatalog.Story.cs` + `LocCatalog.Dialog.cs` (115+ Zeilen + Neutexte).

## Entscheide (gelockt, Eigner 2026-11-24, setgoal-Runde)

1. Nur Stale rendern (kein `--force`): Lock-Hash entscheidet, spart GPU-Zeit.
2. `TTS_DEVICE=cuda:0` auf der 5070 Ti mit Stopp + Auto-Restore von
   `inkling2-mini-strata.service` inkl. `/health`-Probe (CPU-Render kein Standard).
3. 8 Bestandsstimmen, kein `--redesign` (Redesign würde alle Hashes ungültig machen).
4. Ablage als S2-Ausführung von `cinderreach-sprache-voice` mit
   `health_goals: [G-DIABLO03, G-DIABLO07]` (kein Einmal-Goal mit `none`).

## Approach

Preflight ohne GPU-Eingriff (Inventur + Lease-Check), dann genau ein GPU-Fenster
(Stopp → Render → Restart + Probe), danach CPU-QA + Gates. Lock-Datei wird pro Zeile
fortgeschrieben (`save_lock`), kein Sammel-Commit von Text + Clips vermischen.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| B1 preflight | Stale-Inventur, Inkling-/VRAM-Status, Go-Bereitschaft | `docs/voice-batch-preflight.md` (neu), `docs/audio/voice-lines.lock.json` (nur gelesen) |
| B2 batch | Inkling-Pause, CUDA-Render, Lock-Update, Restart + Probe | `sandbox/Assets/Resources/Voice/*.ogg` (+ `.meta`), `docs/audio/voice-lines.lock.json`, `docs/audio/voices/*/ref.ogg` (nur falls fehlend), `docs/voice-batch-render.log` (neu) |
| B3 proof | QA-Transkription, Voice-Gates, Suiten, Run-Log | `scripts/voice/qa_transcribe.py`-Beleg, `docs/voice-batch-run-log.md` (neu), `docs/dev-journal.md` |

## Stufe B1 — Preflight (kein Stopp, kein Render)

1. Im Worktree-Root: `python3 scripts/check-voice-lines.py` (erwartet: listet genau die
   Stale-/Missing-Keys nach Rework), `nvidia-smi`, `systemctl --user status
   inkling2-mini-strata.service`, `curl -s http://127.0.0.1:1919/health`.
2. `docs/voice-batch-preflight.md` schreiben: Stale-Count, VRAM, Inkling-Status, Go-Voraussetzung.
3. Gate: `python3 ~/scripts/agent-workspace.py check` (still). Kein Commit mit Clip-Änderung.

## Stufe B2 — Batch (nur auf Eigner-Go)

1. `systemctl --user stop inkling2-mini-strata.service`; `nvidia-smi` (VRAM frei für TTS).
2. Render (voice-tts-venv, GPU):
   `TTS_DEVICE=cuda:0 ~/unsloth-boxes/voice-tts/.venv/bin/python scripts/voice/render_voices.py 2>&1 | tee docs/voice-batch-render.log`
   (kein `--force`, kein `--redesign`, kein `--only` außer bei Retry einzelner Keys).
3. Bei Abbruch/Fehler: sofort `systemctl --user start inkling2-mini-strata.service` + Probe,
   Fehler + letzte Lock-Zeile in `docs/voice-batch-run-log.md` sichern.
4. Erfolg: `systemctl --user start inkling2-mini-strata.service`; warten bis
   `curl -s http://127.0.0.1:1919/health` ok + 1-Token-Probe; `nvidia-smi`-Nachherstand
   ins Run-Log.

## Stufe B3 — Proof (CPU)

1. QA: `~/models/voice/stt-server/.venv/bin/python scripts/voice/qa_transcribe.py --max-cer 0.25`
   (Worst-Lines in `docs/voice-batch-run-log.md` übernehmen).
2. Gates im Worktree-Root: `python3 scripts/check-voice-lines.py`,
   `python3 scripts/check-meta.py`, `python3 scripts/check-audio-manifest.py`,
   `bash scripts/run-unity-tests.sh`.
3. `python3 ~/scripts/agent-workspace.py check` (still), PROGRESS auf 100 % nur mit allen Belegen.

## Verify je Stufe (vor Commit, im Worktree-Root)

- B1: `python3 ~/scripts/agent-workspace.py check` (still), Preflight-Review gegen Lock-Diff
- B2/B3: `python3 scripts/check-voice-lines.py`; `python3 scripts/check-meta.py`;
  `python3 scripts/check-audio-manifest.py`; `bash scripts/run-unity-tests.sh`
- `python3 ~/scripts/agent-workspace.py check` (still)
- Gates: `node ~/scripts/goal-lint.mjs` (G6), `python3 ~/scripts/plan-goal-gate.py` (Goal↔Plan-Link)
