# Progress: cinderreach-sprache-voice

Prozent: 0 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (Entscheid: verständlich-modern als eigenes Goal, Glossar + Ton bleiben).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-sprache-voice/tasks.md` geschrieben.
- 2026-11-26: Status geprüft (main `5978149`): weiter 0 % — keine S-Stufe gelandet, kein `feature/cinderreach-sprache-voice-s<N>`-Branch. Kein `sprache-inventur.md`, keine `SpracheTests.cs`, kein `sprache-voice-run-log.md`; Zeilenstand weiter ca. 115+ Bestand (deutsch, 8 Stimmen, `check-voice-lines.py` als Gate). Plan-Basis (`73ca3e2`) überholt. Reihenfolge gilt gestaffelt: S2 erst nach Textständen aus Akt-I-S1, Akt-II-S1, Akt-III-S1 — alle drei stehen noch aus, daher S2 weiter blockiert.

## Nächste Schritte

- S1: Glättungsregeln (Ton-Fortschreibung + Beispiele) + Bestands-Inventur aller 115+ Zeilen — sofort startbar, entblockt nichts, wird aber erst mit den Quest-S1-Ständen vollständig.
- S2: Text-Glättung (Loc + Dialogs/Barks) + Voice-Render + QA — nach Akt-I/II/III-S1.
- S3: Abnahme (Suiten, check-voice-lines, Doku).

## Offene Punkte

- Neutexte aus den Quest-Goals kommen gestaffelt; S2 läuft erst nach deren S1-Textständen (alle drei noch offen).
