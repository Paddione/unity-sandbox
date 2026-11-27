# Progress: cinderreach-akt1-vertiefung

Prozent: 50 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (beides kombiniert: mehr Quests + tiefere Einzelquests; gemischte Rätsel; verständlich-modern; 4 Goals inkl. separatem Sprach-Goal).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-akt1-vertiefung/tasks.md` geschrieben.
- 2026-11-26: Status geprüft (main `5978149`): weiter 0 % — keine S-Stufe gelandet, kein `feature/cinderreach-akt1-vertiefung-s<N>`-Branch. Story-Bibel weiter H1–H5/N1–N3 (8 Einträge), keine `Akt1*Tests`, kein `akt1-vertiefung-run-log.md`. Plan-Basis (`73ca3e2`) überholt; S1 vor Start auf aktuelles main rebasen. Neu seit Plan: `cinderreach-group-quest-map` (100 %, `4434b6d`) berührt Story-UI (Share/Tracking/Destinations/Karten) — S3-Ziel `StoryQuestUI.cs` vor Start dagegen abgleichen; `cinderreach-quest-interaktionen` (100 %, `QuestJourneyTests` 14/14, `WorldInteractionTests` 24/24) bleibt Baseline für die Questkette.
- 2026-11-26 (S1 bibel+modell, dieser Branch): Story-Bibel Akt I auf 16 Einträge erweitert (H14–H17, N8–N11, Rätsel R1–R3, Folgezeilen-Mapping N8→H2/N9→H3/N10→H4/N11→H5); `StoryModel.cs` + 8 Quests und 6 Fundstück-Ids (Anker H2/H4/H5 unangetastet, h10–h13/n6–n7 für Akt III freigehalten); Loc-Titel/Zusammenfassungen/Ziele + Fundstücknamen in `LocCatalog.Story.cs`; `Akt1ModellTests.cs` (neu, 12 Fälle) + 10 Fälle in `QuestRuleTests.cs` + 8 Journeys in `QuestJourneyTests.cs`. Katalog-Reihenfolge nach N3 hält alle Bestands-Plan- und Journey-Asserts stabil (erster Angebot je NPC bleibt h1/h2/h3/h5).
- 2026-11-27 (S2 moor-beinhaus, dieser Branch): R1/R2 als echte Fundstück-Rätsel — 7 neue `StoryPickup`s in `TownBuilder.SpawnStoryPickups` (2 Fuhrfetzen + Siegelband am zweiten Karren im Moor, 3 Tafelbruchstücke + Grablicht im Beinhaus; Moor-Positionen paarweise ≥3,6 m entzerrt) und per `BuildDiabloZone` in die Szene gebaut (12 Pickups, Coverage 0); 15 neue Skripte + 2 Folgezeilen in `StoryDialogs.cs` (`h14.offer/hint/done` mit R1-Vollsatz `O|n8`, `h15.offer/hint/talk.ottilie`, `n8/n9.offer/hint/done` mit R2-Auflösung in `n9.done`, `h2.offer.5`/`h3.offer.8`); 28 Zeilen de/en in `LocCatalog.Dialog.cs` + 28 synthetische Voice-Clips gerendert (`check-voice-lines` 143/0); `Akt1MoorTests.cs` (neu, 2 PlayMode-Reisen über echte Trigger) + 3 Fälle in `QuestRuleTests.cs`; `GroupQuestMapTests` auf 8-zeiliges `h3.offer` nachgezogen. Plan-Notiz S2 (State/Director-Prüfung): keine Erweiterung nötig — neue Schritte laufen auf Bestandssystemen (Collect/Kill/Talk, Turn-in, Catch-up, Flags via `SyncDoneFlags`, Rewards via `GrantRewards`), belegt durch Journeys + Regeln. Belege: EditMode 673/673, PlayMode 379/379, `VerifyDiabloZone` + `KayKitAssets.Verify` grün. Unterwegs: `VfxSpawnTests.Boss_TelegraphAndPhaseTwoFire` war auf alter Base (6646cff) rot — kein S2-Fehler, sondern fehlender `main`-Fix `a4d3dc3`; nach Rebase auf `origin/main` grün.

- 2026-11-27: Plan auf `1ff1b95` rebased und als P1 priorisiert (Kettenkopf): S1 als erledigt markiert (`6646cff`), Save-Stand auf v15 korrigiert, `group-quest-map`-Abgleich für S3 und State/Director-Prüfung für S2 in den Plan aufgenommen. Prozent bleibt 25 %.

## Nächste Schritte

- S3: Ofen/Wacht-Strang (Rußbuch-/Siegel-Rätsel H16/H17, Jorin-/Ilsabe-Entscheidungen, H5-Finale mit N10/N11-Folgen).
- S4: Abnahme (Autoplay bis actboss, Voice für neue Zeilen, Doku).

## Offene Punkte

- S2-Dialoge/Fundorte drin (R1/R2); R3 (Siegel-Entscheidung) plus Ofen-Fundstücke (Fuhrliste, Rußseite) kommen in S3.
- S3-Umfang gegen `group-quest-map`-Stand abgleichen (S2 berührte ihn bereits: `h3.offer`-Zeilenanzahl).
