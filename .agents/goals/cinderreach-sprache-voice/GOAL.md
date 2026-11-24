---
goal: cinderreach-sprache-voice
slug: cinderreach-sprache-voice
health_goals: [G-DIABLO03, G-DIABLO07]
plan_refs: [.agents/plans/cinderreach-sprache-voice/tasks.md]
---

# Goal: cinderreach-sprache-voice — Spieltexte verständlich-modern + Voice neu (Querschnitt)

Angelegt: 2026-11-24. Herkunft: Eigner-Anfrage (setgoal-Runde 2026-11-24, Entscheid: verständlich-modern, nicht voll Umgangssprache, nicht nur UI; eigenes Goal; Persistenz + Ausführungsplan auf Eigner-Wunsch).

## Objective

In `unity-sandbox` alle Spieltexte (Bestand H1–H9/N1–N5/Barks + neue Zeilen aus den Quest-Goals, ca. 115+ Zeilen) auf verständlich-modern glätten und alle Clips neu rendern + QA-en: heutige Wörter, kurze Sätze; Glossar und Ton (düster, knapp, konkret — kein Monolog, keine Auserwählten, Ironie nur Ilsabe/Schmuggler, Anrede-Regel Figuren-duzen/Vogt-siezen) bleiben.

## Erfolgskriterien

1. Glättungsregeln dokumentiert (max. 1 Seite, mit Vorher-/Nachher-Beispielen) in `docs/story-bible.md` §Ton (Fortschreibung, kein Ton-Bruch).
2. Glossar-Namen unverändert; alle Dialog-/Bark-Zeilen ersetzt, `Loc`-Tabelle + `-lang en`-Fallback konsistent.
3. `scripts/check-voice-lines.py` grün (keine fehlenden/veralteten Clips); `scripts/voice/render_voices.py` + `scripts/voice/qa_transcribe.py` belegt verständlich.
4. Voice-Regler „Stimmen“ funktioniert weiter; Koop-Bark-Regel (nur lokaler Empfänger bei Phase-2-/Todes-Zurufen) bleibt.
5. Belege: EditMode + PlayMode grün, `check-meta.py` grün (neue Clips je mit `.meta`).

## Scope

- `docs/story-bible.md` §Ton (Glättungsregeln, Beispiele)
- `sandbox/Assets/Scripts/Localization/LocCatalog.Story.cs`, `LocCatalog.Dialog.cs` (alle betroffenen Zeilen)
- `sandbox/Assets/Scripts/Story/StoryDialogs.cs`, `StoryBarks.cs` (Textseite; Quest-Logik unangetastet)
- `docs/audio/voices.json`, Clips unter `sandbox/Assets/Resources/Voice/<key>.ogg`, `scripts/voice/*`

## Nicht-Ziele

- Keine Quest-Logik, keine neuen Quests, kein Slang-/Humor-Umbau der Figuren
- Kein neues Voice-System, keine neuen Stimmen (8 Bestand-Stimmen: abbot, glasswright, ilsabe, jorin, kilnmother, merle, ottilie, narrator)
- Kein Eingriff in Quest-Reihenfolge oder Akt-Freischaltung (dafür die drei Quest-Goals)

## Abhängigkeiten / Referenzen

- `cinderreach-deutsch-story` + `cinderreach-story-voice` (100 %): 115 Zeilen, 8 Stimmen, Voice-Pipeline
- Zu glättende Neutexte kommen aus `cinderreach-akt1-vertiefung`, `cinderreach-akt2-vertiefung`, `cinderreach-akt3-finale` (Reihenfolge: deren S1-Texte stehen vor diesem Goal S2)
- Vertrag: `scripts/check-audio-manifest.py` = Probe G-DIABLO07; `scripts/health_probes/diablo_unity.py` G-DIABLO03
- Ausführung: `.agents/plans/cinderreach-sprache-voice/tasks.md` (Stufen S1–S3, je eigener Worktree + `land: ff`)
- health_goals: [G-DIABLO03, G-DIABLO07]
