# Progress: cinderreach-akt1-vertiefung

Prozent: 25 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (beides kombiniert: mehr Quests + tiefere Einzelquests; gemischte Rätsel; verständlich-modern; 4 Goals inkl. separatem Sprach-Goal).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-akt1-vertiefung/tasks.md` geschrieben.
- 2026-11-26: Status geprüft (main `5978149`): weiter 0 % — keine S-Stufe gelandet, kein `feature/cinderreach-akt1-vertiefung-s<N>`-Branch. Story-Bibel weiter H1–H5/N1–N3 (8 Einträge), keine `Akt1*Tests`, kein `akt1-vertiefung-run-log.md`. Plan-Basis (`73ca3e2`) überholt; S1 vor Start auf aktuelles main rebasen. Neu seit Plan: `cinderreach-group-quest-map` (100 %, `4434b6d`) berührt Story-UI (Share/Tracking/Destinations/Karten) — S3-Ziel `StoryQuestUI.cs` vor Start dagegen abgleichen; `cinderreach-quest-interaktionen` (100 %, `QuestJourneyTests` 14/14, `WorldInteractionTests` 24/24) bleibt Baseline für die Questkette.
- 2026-11-26 (S1 bibel+modell, dieser Branch): Story-Bibel Akt I auf 16 Einträge erweitert (H14–H17, N8–N11, Rätsel R1–R3, Folgezeilen-Mapping N8→H2/N9→H3/N10→H4/N11→H5); `StoryModel.cs` + 8 Quests und 6 Fundstück-Ids (Anker H2/H4/H5 unangetastet, h10–h13/n6–n7 für Akt III freigehalten); Loc-Titel/Zusammenfassungen/Ziele + Fundstücknamen in `LocCatalog.Story.cs`; `Akt1ModellTests.cs` (neu, 12 Fälle) + 10 Fälle in `QuestRuleTests.cs` + 8 Journeys in `QuestJourneyTests.cs`. Katalog-Reihenfolge nach N3 hält alle Bestands-Plan- und Journey-Asserts stabil (erster Angebot je NPC bleibt h1/h2/h3/h5).

- 2026-11-27: Plan auf `1ff1b95` rebased und als P1 priorisiert (Kettenkopf): S1 als erledigt markiert (`6646cff`), Save-Stand auf v15 korrigiert, `group-quest-map`-Abgleich für S3 und State/Director-Prüfung für S2 in den Plan aufgenommen. Prozent bleibt 25 %.

## Nächste Schritte

- S2: Moor/Beinhaus-Strang (H-Ergänzungen, Grabtafel-/Fuhrbrief-Rätsel, N-Folgen).
- S3: Ofen/Wacht-Strang (Rußbuch-/Siegel-Rätsel, Jorin-/Ilsabe-Entscheidungen, H5-Finale).
- S4: Abnahme (Autoplay bis actboss, Voice für neue Zeilen, Doku).

## Offene Punkte

- Quest-Namen und Rätsel stehen mit S1 (Bibel-Tabellen R1–R3); Dialogzeilen/Fundorte dazu kommen in S2/S3.
- S1-Start auf aktuellem main (`5978149`); S3-Umfang gegen `group-quest-map`-Stand abgleichen.
