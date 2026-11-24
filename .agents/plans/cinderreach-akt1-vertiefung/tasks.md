---
goal: cinderreach-akt1-vertiefung
slug: cinderreach-akt1-vertiefung
branch: feature/cinderreach-akt1-vertiefung-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: main @ 73ca3e2 sauber; kein anderes Quest-Goal schreibt gleichzeitig an StoryModel/StoryState (disjunkte Stufen, siehe Manifest)
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

## Ausgangslage (Belege, HEAD `73ca3e2`, 2026-11-24)

- Story-SSOT `docs/story-bible.md` §§Akt I/Nebenaufgaben/Folgezeilen: H1–H5 + N1–N3, Anker (H2 öffnet Ofen, H4 schließt Akt I, H5 löscht Laterne), Folge-Mechanik `x|nK`/`x|!nK` über `StoryDialog.Visible`.
- Code: `sandbox/Assets/Scripts/Story/` (`StoryModel`, `StoryState`, `StoryDirector`, `StoryDialogs`, `StoryBarks`, `StoryPickup`, `StoryNpc`, `StoryQuestUI`), `Localization/LocCatalog.Story.cs` + `LocCatalog.Dialog.cs`, Save v14 (`SaveGame`/`SaveMigrations`), Koop Host-Snapshot Max-Merge.
- Tests: `StoryStateTests`, `LocalizationTests`, `StoryVoiceTests` (Muster für neue Quest-/Rätsel-Tests).

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
| S1 bibel+modell | Akt-I-Tabellen neu (ca. +8), Rätsel-Definitionen, Model/State/Director-Erweiterung + Tests | `docs/story-bible.md` (§Akt I), `Scripts/Story/StoryModel.cs`, `Scripts/Story/StoryState.cs`, `Scripts/Story/StoryDirector.cs`, `Tests/EditMode/Akt1ModellTests.cs` (neu) |
| S2 moor-beinhaus | Moor/Beinhaus-Strang: H-Ergänzungen, Grabtafel-/Fuhrbrief-Rätsel, N-Folgen + Tests | `Scripts/Story/StoryDialogs.cs` (Akt-I-Moor/Beinhaus), `Scripts/Story/StoryPickup.cs`, `Scripts/Story/StoryNpc.cs` (Ottilie/Moor), `Localization/LocCatalog.Story.cs` (Moor/Beinhaus-Zeilen), `Tests/PlayMode/Akt1MoorTests.cs` (neu) |
| S3 ofen-wacht | Ofen/Wacht-Strang: Rußbuch-/Siegel-Rätsel, Jorin-/Ilsabe-Entscheidungen, H5-Finale + Tests | `Scripts/Story/StoryDialogs.cs` (Akt-I-Ofen/Wacht), `Scripts/Story/StoryQuestUI.cs`, `Scripts/Story/StoryBarks.cs` (Akt-I-Zurufe), `Localization/LocCatalog.Dialog.cs` (Ofen/Wacht-Zeilen), `Tests/PlayMode/Akt1OfenTests.cs` (neu) |
| S4 proof | Save-Migration, Koop-Buchung neuer Schritte, Autoplay bis actboss, Voice neuer Zeilen, Doku | `Scripts/Save/*` (Migration neuester Stand), `Scripts/SessionAutoplay.cs`, `docs/akt1-vertiefung-run-log.md` (neu), `docs/dev-journal.md`, `docs/story-bible.md` (Folgezeilen-Abgleich) |

## Verify je Stufe (vor Commit, im Worktree-Root)

- `bash scripts/run-unity-tests.sh` (Exit 3 = anderer Lauf hält den Lock, warten)
- `SandboxBuilder.VerifyDiabloZone` + `KayKitAssets.Verify` (`repo.yaml:verify`)
- `python3 scripts/check-meta.py`
- Ab S3 zusätzlich `bash scripts/run-autoplay.sh <out> -seed 0` (Exit 0, Phasen bis `actboss`), S4 zusätzlich Voice-Check `python3 scripts/check-voice-lines.py`
- `python3 ~/scripts/agent-workspace.py check` (still)
