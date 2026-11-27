# Progress: cinderreach-akt3-finale

Prozent: 25 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (Entscheid: Laternen-Finale linear, kein neuer Ort, keine Verzweigung).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-akt3-finale/tasks.md` geschrieben.
- 2026-11-26: Status geprüft (main `5978149`): weiter 0 % — keine S-Stufe gelandet, kein `feature/cinderreach-akt3-finale-s<N>`-Branch. Story-Bibel weiter nur §Ausblick-Platzhalter (kein Akt-III-Abschnitt H10–H13/N6–N7), keine `Akt3*Tests`, kein "akt3-finale-run-log.md". Plan-Basis (`73ca3e2`) überholt. Reihenfolge gilt: S1 erst nach Akt-I-S1 + Akt-II-S1 (H9→H10-Haken muss auf deren H9-Varianten passen). `ActProgress`-/Save-/Koop-Muster weiter wie im Plan (Abbot→Kiln, Kilnmother→Brache).

- 2026-11-27: Plan auf `1ff1b95` rebased und als P5 priorisiert: Akt-I-S1 erfüllt (`6646cff`, IDs `h10–h13`/`n6–n7` freigehalten), Akt-II-S1 steht weiter aus — S1 bleibt blockiert; Save-Stand auf v15 korrigiert. Prozent bleibt 0 %.

- 2026-11-27: S1 bibel+akt gelandet (Branch `cinderreach-akt3-finale-s1`, dieser Worktree): Bibel §Akt III „Was nach Hause will" (H10 Letzte Nacht in der Wacht / H11 Glut am Moortor mit Rätsel R7 Laternenlesung / H12 Finalboss Thewes der Heimkehrer / H13 Entscheidung zerschlagen-halten; N6 Jorins Einschmelzen / N7 Ilsabes letztes Angebot; Ausblick-Platzhalter ersetzt, Haken-Zitat „Wenn wir sie zerschlagen, ist es dunkel. Wenn nicht, kommen sie weiter." wörtlich erhalten; Glossar: Der Heimkehrer + Akt III; Zurufe Thewes Phase 2/besiegt; Zwischentexte H12/H13; genau 1 offene Zeile aufgelöst: N9 Thewes' Licht). `ActProgressAkt3.cs` neu (Entwurf: OpenMoortor braucht Barrens-Boss wie Kilnmother→Brache, DefeatFinalBoss braucht Moortor, Decide genau 1 Schlusszeile Shatter/Hold — kein Balance-/Drop-/Gear-Eingriff, Save v16 unangetastet, Save/Koop in S3). `Akt3ModellTests.cs` neu (12/12: Reservierung h10–h13/n6–n7, H9-Anker für H10, H9-Varianten merle.7/ottilie.7-8 seit Akt-II-S2, Katalog-Reihenfolge, Catch-up, Wacht-/Moortor-Gates, Finalboss-Reihenfolge, H13-Entscheidung, v16-Stillstand). Belege: EditMode 702/702, PlayMode 389/389, `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK, `check-meta` OK, workspace-check still. Diagnose: `GameSettingsTests.MainMenu_NewContinueValidation` war einmal rot durch einen liegengebliebenen Spielstand unter `~/.config/unity3d/.../cinderreach-save.json` (fremder Testlauf, kein S1-Fehler) — nach Löschen des Stands voll grün. Handoff an S2: Bibel-Drop steht, Voice-Render neuer Zeilen erst ab S2 (S2-Textglättung wartet auf diesen Bibel-Drop); S2 hängt H10–H13/N6–N7 an H9 (prereq-Kette H9→H10→H11→H12→H13, N6/N7→H13-Folge).

## Nächste Schritte

- S1: Story-Bibel Akt III schreiben (H10–H13/N6–N7, Boss- und Entscheidungs-Definition) + ActProgress-Entwurf — nach Akt-I-S1 + Akt-II-S1.
- S2: Wacht-Strang (H10, N6–N7, NPC-Dialoge Ottilie/Merle/Ilsabe/Jorin).
- S3: Moortor-Finale (H11-Rätsel, H12-Boss, H13-Entscheidung, Save-Migration, Koop-Buchung).
- S4: Abnahme (Autoplay bis complete, Voice, Doku).

## Offene Punkte

- Boss-Name und -Mechanik stehen erst mit S1 (Story-Bibel Akt III).
- S1-Start auf aktuellem main; Moortor-Logik (`*Gate*.cs`) gegen `group-quest-map`-Kartenstand abgleichen.
