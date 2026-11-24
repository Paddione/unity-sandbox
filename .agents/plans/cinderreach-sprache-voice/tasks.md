---
goal: cinderreach-sprache-voice
slug: cinderreach-sprache-voice
branch: feature/cinderreach-sprache-voice-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO03, G-DIABLO07]
start_condition: main @ 73ca3e2 sauber; S2 erst nach Textständen aus Akt-I-S1, Akt-II-S1, Akt-III-S1 (gestaffelt)
plan_refs: [.agents/goals/cinderreach-sprache-voice/GOAL.md]
---

# Plan: cinderreach-sprache-voice — Texte verständlich-modern + Voice neu

Pfad: feature, 3 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-sprache-voice-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).

## Goal

Siehe [GOAL.md](../../goals/cinderreach-sprache-voice/GOAL.md): alle Zeilen (115+ Bestand +
Neutexte) verständlich-modern glätten (Glossar + Ton bleiben), Clips neu rendern + QA-en,
`check-voice-lines.py` grün, kein Slang-Umbau, kein neues Voice-System.

## Success Criteria

Die fünf Erfolgskriterien aus GOAL.md. Gesamt-Gate S3: EditMode + PlayMode grün,
`check-meta.py` grün, `check-voice-lines.py` grün, Render- + QA-Belege, Voice-Regler und
Koop-Bark-Regel erhalten.

## Ausgangslage (Belege, HEAD `73ca3e2`, 2026-11-24)

- Ton-SSOT: Story-Bibel §Ton (düster/knapp/konkret, Anrede-, UI-, Zahlen-Regeln), Glossar (Orte/Gegner/Klassen/Systeme).
- Textorte: `LocCatalog.Story.cs` + `LocCatalog.Dialog.cs`, `StoryDialogs.cs`, `StoryBarks.cs` (115 Zeilen inkl. `x|nK`-Varianten).
- Voice: `docs/audio/voices.json` (8 Stimmen), Clips `sandbox/Assets/Resources/Voice/<key>.ogg`, Pipeline `scripts/voice/render_voices.py` + `qa_transcribe.py`, Gate `scripts/check-voice-lines.py`, Vertrag `scripts/check-audio-manifest.py` = G-DIABLO07.

## Entscheide (gelockt, Eigner 2026-11-24, setgoal-Runde)

1. Verständlich-modern (nur verstaubte Wendungen ersetzen), kein Slang, nicht nur UI.
2. Glossar + Ton bleiben; Glättungsregeln dokumentiert (max. 1 Seite + Beispiele).
3. Keine neuen Stimmen, kein neues System; Text-/Clip-Trennung bei Commits.

## Approach

Regeln zuerst (Ton-Fortschreibung + Inventur), dann Text-Glättung gestaffelt nach Quest-Goal-Ständen,
dann Render + QA, zuletzt Abnahme. Neutexte laufen immer zuerst durch ihre Quest-Goals S1,
dann hier durch S2.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 regeln+inventur | Glättungsregeln + Beispiele, Voll-Inventur aller Zeilen (Bestand + Neutext-Bedarf) | `docs/story-bible.md` (§Ton-Fortschreibung), `docs/sprache-inventur.md` (neu) |
| S2 text+voice | Glättung aller Loc-/Dialog-/Bark-Zeilen, Render + QA aller Clips | `Scripts/Localization/LocCatalog.Story.cs`, `Scripts/Localization/LocCatalog.Dialog.cs`, `Scripts/Story/StoryDialogs.cs`, `Scripts/Story/StoryBarks.cs`, `docs/audio/voices.json`, `Assets/Resources/Voice/*.ogg` (+ `.meta`), `Tests/EditMode/SpracheTests.cs` (neu: Glossar-/Anrede-/Zahlen-Regeln) |
| S3 proof | Suiten, Voice-Gates, Regler-/Koop-Regression, Doku | `docs/qa_transcribe`-Belege, `docs/sprache-voice-run-log.md` (neu), `docs/dev-journal.md` |

## Verify je Stufe (vor Commit, im Worktree-Root)

- S1: `python3 ~/scripts/agent-workspace.py check` (still), Review der Regel-Seite gegen Glossar
- S2/S3: `bash scripts/run-unity-tests.sh`; `python3 scripts/check-meta.py`; `python3 scripts/check-voice-lines.py`; `python3 scripts/check-audio-manifest.py`
- `python3 ~/scripts/agent-workspace.py check` (still)
