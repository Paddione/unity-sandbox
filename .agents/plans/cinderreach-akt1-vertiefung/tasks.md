---
goal: cinderreach-akt1-vertiefung
slug: cinderreach-akt1-vertiefung
branch: feature/cinderreach-akt1-vertiefung-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
priority: P1 (Kettenkopf Story; entblockt Akt II, Akt III und sprache-voice S2)
status: in_progress — S1 gelandet (6646cff, 25 %), nächster Schritt S2
start_condition: main @ 1ff1b95 sauber; kein anderes Quest-Goal schreibt gleichzeitig an StoryModel/StoryState (disjunkte Stufen, siehe Manifest)
plan_refs: [.agents/goals/cinderreach-akt1-vertiefung/GOAL.md]
---

# Plan: cinderreach-akt1-vertiefung — Akt I doppelt (Umfang + Tiefe + Rätsel)

Pfad: feature, 4 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-akt1-vertiefung-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).
Reihenfolge ist Abhängigkeitsreihenfolge; keine Stufe lässt Suiten, Verify oder Host-Start rot zurück.

## Goal

Siehe [GOAL.md](../../goals/cinderreach-akt1-vertiefung/GOAL.md): Akt I 8 → ca. 16 Einträge,
beides kombiniert (mehr Quests + tiefere Einzelquests), mind. 2 Umgebungs- + 1 Entscheidungsrätsel,
jede neue Nebenaufgabe mit `x|nK`-Folgezeile, Anker H2-Tor/H4-Abschluss/H5 erhalten.

## Success Criteria

Die sieben Erfolgskriterien aus GOAL.md, je Stufe unten mit Test-/Lauf-Beleg verknüpft.
Gesamt-Gate S4: EditMode + PlayMode grün, `VerifyDiabloZone` + `KayKitAssets.Verify` grün,
`check-meta.py` grün, Autoplay `-seed 0` bis `actboss` Exit 0 / 0 Exceptions.

## Ausgangslage (Belege, HEAD `1ff1b95`, 2026-11-27, Plan-Rebase)

- S1 ist gelandet (`6646cff`): Story-Bibel Akt I auf 16 Einträge erweitert (H14–H17, N8–N11, Rätsel R1–R3 in `docs/story-bible.md` §Akt I), `StoryModel.cs` + 8 Quests/6 Fundstück-Ids (IDs `h10–h13`/`n6–n7` für Akt III freigehalten), Titel/Ziele in `LocCatalog.Story.cs`, `Akt1ModellTests` (12 Fälle) + `QuestRuleTests`/`QuestJourneyTests` erweitert. Goal steht bei 25 %.
- Abweichung zum ursprünglichen S1-Manifest: `StoryState.cs`/`StoryDirector.cs` wurden in S1 nicht angefasst. Ob Zustands-/Director-Erweiterungen für die neuen Schritte nötig sind, ist in S2 am Ist-Code zu prüfen und dort ggf. nachzuholen (kein stiller Scope-Verlust).
- Seit der alten Plan-Basis (`73ca3e2`) gelandet: `cinderreach-group-quest-map` (`4434b6d`/`a2b36ca`) mit `StoryDialogShare`, `StoryTracking`, `QuestDestinations` und geänderter `StoryQuestUI.cs` — S3-Ziel `StoryQuestUI.cs` vor Start gegen diesen Stand abgleichen (neue Quests müssen in Tracker/Fuhrbuch/Karten auftauchen). `cinderreach-quest-interaktionen` (100 %) bleibt Ketten-Baseline.
- Code: `sandbox/Assets/Scripts/Story/` (`StoryModel`, `StoryState`, `StoryDirector`, `StoryDialogs`, `StoryBarks`, `StoryPickup`, `StoryNpc`, `StoryQuestUI`), `Localization/LocCatalog.Story.cs` + `LocCatalog.Dialog.cs`, Save v15 (`SaveGame.CurrentVersion = 15`, gear-slots; nicht mehr v14), Koop Host-Snapshot Max-Merge.
- Tests: `StoryStateTests`, `LocalizationTests`, `StoryVoiceTests` (Muster für neue Quest-/Rätsel-Tests), dazu die S1-Tests `Akt1ModellTests`, `QuestRuleTests`, `QuestJourneyTests` als Regressionsbasis für S2/S3.

## Entscheide (gelockt, Eigner 2026-11-24, setgoal-Runde)

1. Doppelt = beides kombiniert (mehr Quests + tiefere Einzelquests), nicht nur Zählung oder nur Tiefe.
2. Rätsel gemischt: Umgebung/Fundstücke UND Dialog/Entscheidung (kein reiner Typ).
3. Sprache neuer Zeilen verständlich-modern (Glossar + Ton bleiben); Bestands-Glättung in `cinderreach-sprache-voice`.
4. T-Taste/`IUsable` wiederverwenden (kein neues Interaktionssystem); Anker H2/H4/H5 unangetastet.

## Approach

Story zuerst (Bibel-Tabellen + Rätsel-Definitionen als SSOT), dann Daten (Model/State/Director),
dann Stränge (Moor/Beinhaus, Ofen/Wacht) auf denselben Pfaden wie Bestand, zuletzt Beweis
(Autoplay bis actboss, Voice neuer Zeilen, Doku in Run-Log + Dev-Journal).

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 bibel+modell (erledigt, `6646cff`) | Akt-I-Tabellen neu (ca. +8), Rätsel-Definitionen, Model-Erweiterung + Tests; State/Director-Prüfung nach S2 verlegt (s. Ausgangslage) | `docs/story-bible.md` (§Akt I), `Scripts/Story/StoryModel.cs`, `Localization/LocCatalog.Story.cs`, `Tests/EditMode/Akt1ModellTests.cs`, `Tests/EditMode/QuestRuleTests.cs`, `Tests/PlayMode/QuestJourneyTests.cs` |
| S2 moor-beinhaus | Moor/Beinhaus-Strang: H-Ergänzungen, Grabtafel-/Fuhrbrief-Rätsel, N-Folgen + Tests | `Scripts/Story/StoryDialogs.cs` (Akt-I-Moor/Beinhaus), `Scripts/Story/StoryPickup.cs`, `Scripts/Story/StoryNpc.cs` (Ottilie/Moor), `Localization/LocCatalog.Story.cs` (Moor/Beinhaus-Zeilen), `Tests/PlayMode/Akt1MoorTests.cs` (neu) |
| S3 ofen-wacht | Ofen/Wacht-Strang: Rußbuch-/Siegel-Rätsel, Jorin-/Ilsabe-Entscheidungen, H5-Finale + Tests | `Scripts/Story/StoryDialogs.cs` (Akt-I-Ofen/Wacht), `Scripts/Story/StoryQuestUI.cs`, `Scripts/Story/StoryBarks.cs` (Akt-I-Zurufe), `Localization/LocCatalog.Dialog.cs` (Ofen/Wacht-Zeilen), `Tests/PlayMode/Akt1OfenTests.cs` (neu) |
| S4 proof | Save-Migration, Koop-Buchung neuer Schritte, Autoplay bis actboss, Voice neuer Zeilen, Doku | `Scripts/Save/*` (Migration neuester Stand), `Scripts/SessionAutoplay.cs`, `docs/akt1-vertiefung-run-log.md` (neu), `docs/dev-journal.md`, `docs/story-bible.md` (Folgezeilen-Abgleich) |

## Verify je Stufe (vor Commit, im Worktree-Root)

- `bash scripts/run-unity-tests.sh` (Exit 3 = anderer Lauf hält den Lock, warten)
- `SandboxBuilder.VerifyDiabloZone` + `KayKitAssets.Verify` (`repo.yaml:verify`)
- `python3 scripts/check-meta.py`
- Ab S3 zusätzlich `bash scripts/run-autoplay.sh <out> -seed 0` (Exit 0, Phasen bis `actboss`), S4 zusätzlich Voice-Check `python3 scripts/check-voice-lines.py`
- `python3 ~/scripts/agent-workspace.py check` (still)
