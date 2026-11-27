# Progress: cinderreach-akt3-finale

Prozent: 0 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (Entscheid: Laternen-Finale linear, kein neuer Ort, keine Verzweigung).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-akt3-finale/tasks.md` geschrieben.
- 2026-11-26: Status geprüft (main `5978149`): weiter 0 % — keine S-Stufe gelandet, kein `feature/cinderreach-akt3-finale-s<N>`-Branch. Story-Bibel weiter nur §Ausblick-Platzhalter (kein Akt-III-Abschnitt H10–H13/N6–N7), keine `Akt3*Tests`, kein "akt3-finale-run-log.md". Plan-Basis (`73ca3e2`) überholt. Reihenfolge gilt: S1 erst nach Akt-I-S1 + Akt-II-S1 (H9→H10-Haken muss auf deren H9-Varianten passen). `ActProgress`-/Save-/Koop-Muster weiter wie im Plan (Abbot→Kiln, Kilnmother→Brache).

- 2026-11-27: Plan auf `1ff1b95` rebased und als P5 priorisiert: Akt-I-S1 erfüllt (`6646cff`, IDs `h10–h13`/`n6–n7` freigehalten), Akt-II-S1 steht weiter aus — S1 bleibt blockiert; Save-Stand auf v15 korrigiert. Prozent bleibt 0 %.

## Nächste Schritte

- S1: Story-Bibel Akt III schreiben (H10–H13/N6–N7, Boss- und Entscheidungs-Definition) + ActProgress-Entwurf — nach Akt-I-S1 + Akt-II-S1.
- S2: Wacht-Strang (H10, N6–N7, NPC-Dialoge Ottilie/Merle/Ilsabe/Jorin).
- S3: Moortor-Finale (H11-Rätsel, H12-Boss, H13-Entscheidung, Save-Migration, Koop-Buchung).
- S4: Abnahme (Autoplay bis complete, Voice, Doku).

## Offene Punkte

- Boss-Name und -Mechanik stehen erst mit S1 (Story-Bibel Akt III).
- S1-Start auf aktuellem main; Moortor-Logik (`*Gate*.cs`) gegen `group-quest-map`-Kartenstand abgleichen.
