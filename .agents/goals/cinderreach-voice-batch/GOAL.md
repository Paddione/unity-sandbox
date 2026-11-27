---
goal: cinderreach-voice-batch
slug: cinderreach-voice-batch
health_goals: [G-DIABLO03, G-DIABLO07]
plan_refs: [.agents/plans/cinderreach-voice-batch/tasks.md]
---

# Goal: cinderreach-voice-batch — TTS-Batch für neue Dialoge nach Sprach-/Quest-Rework (5070 Ti, S2-Ausführung)

Angelegt: 2026-11-24. Herkunft: Eigner-Vorgabe /setgoal (eine Fragerunde 2026-11-24: Umfang = nur Stale,
GPU = CUDA mit Auto-Restore, Stimmen = 8 Bestand, Ablage = S2-Ausführung von `cinderreach-sprache-voice`).

## Objective

In `unity-sandbox` alle durch das Sprach-/Quest-Rework geänderten Cinderreach-Voice-Lines als ein
Batch neu rendern: `scripts/voice/render_voices.py` ohne `--force` / ohne `--redesign` mit
`TTS_DEVICE=cuda:0` auf der RTX 5070 Ti; das dort gehostete Inkling-Modell (`:1919`) wird für die
Batch-Dauer pausiert und danach mit Health-Probe wieder gestartet — Start nur auf explizites
Eigner-Go. Danach QA + Voice-Gates grün. Ausführung von S2 aus `cinderreach-sprache-voice`.

## Erfolgskriterien

1. Batch läuft auf der 5070 Ti, Inkling vorher pausiert, nachher automatisch wieder gestartet und per
   Health-Probe belegt (Beleg: GPU-Belegung, Unit-Status, Render-Log mit Sekunden pro Zeile).
2. Nur geänderte/fehlenede Zeilen gerendert (Lock-Hash aus TTS-Text + Sprecher + Referenztext + Seed +
   Instruct); Design-Modell nur bei fehlender Sprecher-Referenz, sonst nur Klon-Modell
   (Beleg: Render-Log `X of N`, aktualisierte Lock-Datei).
3. Voice-Gate grün: keine fehlenden/veralteten Clips, keine Orphans, Budget eingehalten
   (Beleg: `scripts/check-voice-lines.py` grün).
4. Verständlichkeit belegt: Transkriptions-QA unter Fehlerschwelle, schlechteste Zeilen berichtet
   (Beleg: `scripts/voice/qa_transcribe.py`-Log).
5. Abnahme: Meta-Dateien vollständig, Audio-Manifest grün, EditMode + PlayMode grün,
   Voice-Regler und Koop-Bark-Regel erhalten (Beleg: Suite-Logs).

## Scope

- `sandbox/Assets/Scripts/Story/StoryDialogs.cs`, `sandbox/Assets/Scripts/Story/StoryBarks.cs` (Keys `dlg.*` / `bark.*`)
- `sandbox/Assets/Scripts/Localization/LocCatalog.Story.cs`, `sandbox/Assets/Scripts/Localization/LocCatalog.Dialog.cs` (gelesene Texte)
- `docs/audio/voices.json` (8 Bestandsstimmen: abbot, glasswright, ilsabe, jorin,
  kilnmother, merle, ottilie, narrator)
- `docs/audio/voices/<id>/ref.ogg` + `ref.txt` (nur fehlende designen)
- `sandbox/Assets/Resources/Voice/<key>.ogg` (+ `.meta`), `docs/audio/voice-lines.lock.json`
- `scripts/voice/render_voices.py`, `scripts/voice/voice_lines.py`, `scripts/voice/qa_transcribe.py`,
  `scripts/check-voice-lines.py`, `scripts/check-audio-manifest.py`

## Nicht-Ziele

- Kein Full-Rerender mit `--force`, kein Referenz-Redesign mit `--redesign`, keine neuen Sprecher.
- Keine Text-/Glossar-/Ton-Änderung (gehört zu S1/S2-Textseite von `cinderreach-sprache-voice`
  und den Akt-Goals); keine Quest-Logik, keine Akt-Freischaltung.
- Kein Parallelbetrieb Inkling + TTS auf der 5070 Ti; kein CPU-Ersatzrender als Standard.
- Kein neues Voice-System, kein neues TTS-Modell.

## Abhängigkeiten / Referenzen

- `cinderreach-sprache-voice` (S2-Ausführung dieses Batches; Texte aus
  `cinderreach-akt1-vertiefung`, `cinderreach-akt2-vertiefung`, `cinderreach-akt3-finale` S1
  müssen vor dem Batch stehen)
- `cinderreach-deutsch-story` + `cinderreach-story-voice`: 115+ Zeilen, 8 Stimmen, Voice-Pipeline
- Vertrag: `scripts/check-audio-manifest.py` = Probe G-DIABLO07;
  "scripts/health_probes/diablo_unity.py" = G-DIABLO03
- Ausführung: `.agents/plans/cinderreach-voice-batch/tasks.md` (Batch-Stufen B1–B3, ein Worktree + `land: ff`)
- health_goals: [G-DIABLO03, G-DIABLO07]
