# Progress: cinderreach-deutsch-story

Prozent: 100 %

## Erledigt

- Goal angelegt (2026-11-23).
- S3 Story-Bibel `docs/story-bible.md`: Prämisse, Kette, drei Wendungen, Figuren, Glossar, Akt I/II mit Haupt- und Nebenaufgaben, Zwischentexte, Ausblick.
- S1 Lokalisierungs-Fundament: `Loc` (Deutsch Standard, `-lang en` Fallback, `Loc.F` mit deutscher Zahlenkultur, `Loc.Display` für Szenen-/Datennamen), `LocCatalog.*.cs` (Deutsch und Englisch je Zeile), `LocalizationTests` (Katalog konsistent, Schlüssel vorhanden, Lint gegen englische Literale mit Allowlist, alte englische Eigennamen gesperrt).
- S2 vollständige Übersetzung: alle 567 Lint-Funde plus zusätzlich gefundene Fragmente übersetzt oder begründet auf `loc-allowlist.txt`, `loc-pending.txt` leer (Obergrenze 0). Eigennamen laut Glossar, Item-/Affix-/Seltenheits-/Slot-Anzeige über den Katalog, Spielstand- und Netz-IDs unverändert. Werkzeuge `scripts/loc_lint.py`, `loc_apply.py`, `loc_sync.py`, `loc_skel.py`. Beleg: EditMode 488/488, PlayMode 280/284 → die vier Rest-Asserts angepasst, gefiltert 26/26.

- S4 Quest-System: `LocCatalog.Story.cs` (NPC-Namen, Questtitel/-ziele, Toasts), `StoryDirector` (Zonen-/Kill-/Fund-Haken, Belohnungen, Toasts, HUD-Zielzeile, Autosave), `StoryDialogUI`, `StoryQuestUI` (Fuhrbuch, Taste J), Modal-Gate in `DiabloUIFactory`, Save v13 (`V12ToV13`, Story in `RunProfile`/`SaveService`), Koop-RPC (`BroadcastStory`/`SubmitStoryTalkServerRpc`, Max-Merge). `StoryStateTests` + `SaveMigrationTests` grün.
- S5/S6 Inhalte: Ottilie und Merle als Szene-NPCs (`TownBuilder.SpawnStoryNpcs`), Marker auf Ilsabe/Jorin, Fundstücke Fuhrbrief/Lieferbuch/Grabtafeln, Szene neu gebaut, `VerifyDiabloZone` grün. Alle Quests H1–H9/N1–N5 über Dialoge, Kills, Funde und Zonenwechsel buchbar.
- Nachweis bisher: Kompilierlauf grün, EditMode 496/496, PlayMode 284/284.

- S7 Nachweis komplett: Session-Autoplay grün (Exit 0, beide Akte, deutsche Abschlusszeile), Koop-Trio `COOP_CLIENTS=2` grün mit identischem Queststand in allen drei v13-Saves, Windows-Shotcheck 60/60 (Dialog, Fuhrbuch, deutsche Menüs/Tooltips). Belege in `docs/story-run-log.md`.

## Nächste Schritte

- Abschluss: `CINDERREACH.md` nachgezogen, Goal entfernt, `land` (ff auf `main` + Push).

## Offene Punkte

Keine.
