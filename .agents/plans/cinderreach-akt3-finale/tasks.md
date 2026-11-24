---
goal: cinderreach-akt3-finale
slug: cinderreach-akt3-finale
branch: feature/cinderreach-akt3-finale-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: main @ 73ca3e2 sauber; S1 erst nach Akt-I-S1 + Akt-II-S1 (H9→H10-Haken muss auf deren H9-Varianten passen)
plan_refs: [.agents/goals/cinderreach-akt3-finale/GOAL.md]
---

# Plan: cinderreach-akt3-finale — Akt III Laternen-Finale linear

Pfad: feature, 4 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-akt3-finale-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).

## Goal

Siehe [GOAL.md](../../goals/cinderreach-akt3-finale/GOAL.md): lineare Kette H10–H13 + N6–N7
in Wacht + Moortor, End-Entscheidung zerschlagen/halten mit je eigener Schlusszeile,
eine offene Zeile auflösen, kein neuer Ort, keine Verzweigung.

## Success Criteria

Die fünf Erfolgskriterien aus GOAL.md. Gesamt-Gate S4: EditMode + PlayMode grün,
`VerifyDiabloZone` + `KayKitAssets.Verify` grün, `check-meta.py` grün,
Autoplay erreicht `complete` mit 0 Exceptions.

## Ausgangslage (Belege, HEAD `73ca3e2`, 2026-11-24)

- Haken: Story-Bibel §Ausblick (Scherbe gegen Laterne, „Wenn wir sie zerschlagen, ist es dunkel. Wenn nicht, kommen sie weiter.“).
- Freischalt-Muster: `ActProgress` (Abbot→Kiln, Kilnmother→Brache), `ZoneGate`/`ZoneTravel`, Save v14 + Migrationskette, Koop Host-Snapshot Max-Merge.
- Gegner-Baukasten: Husks/Hüllen, Elite-Muster, Boss-Phasen (Phase 2 ab 50 %), Bossbalken, Barks.

## Entscheide (gelockt, Eigner 2026-11-24, setgoal-Runde)

1. Laternen-Finale linear (keine 2 Enden, kein neuer Ort/neues Boss-System).
2. Schauplätze Wacht + Moortor; Finalboss aus Hüllen-Antagonist auf vorhandenem System.
3. Entscheidung mit je eigener Schlusszeile bei gleichem Durchlauf (kein Content-Baum).
4. Neue Zeilen verständlich-modern; Bestand in `cinderreach-sprache-voice`.

## Approach

Bibel zuerst (Akt-III-Abschnitt ersetzt Ausblick-Platzhalter, inkl. Boss- und Entscheidungs-Definition),
dann Wacht-Strang (H10, N6–N7, NPC-Dialoge), dann Moortor-Finale (H11-Rätsel, H12-Boss, H13-Entscheidung,
ActProgress/Save/Koop), zuletzt Beweis bis `complete`.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 bibel+akt | Akt-III-Tabellen (H10–H13/N6–N7), Boss-/Entscheidungs-Definition, ActProgress-Entwurf + Tests | `docs/story-bible.md` (§Akt III), `Scripts/Zones/ActProgress*.cs`, `Tests/EditMode/Akt3ModellTests.cs` (neu) |
| S2 wacht | Wacht-Strang: H10, N6–N7, Ottilie/Merle/Ilsabe/Jorin-Dialoge + Tests | `Scripts/Story/StoryDialogs.cs` (Akt III Wacht), `Scripts/Story/StoryNpc.cs` (Wacht), `Localization/LocCatalog.Story.cs` (Akt-III-Wacht), `Tests/PlayMode/Akt3WachtTests.cs` (neu) |
| S3 moortor | Moortor-Finale: H11-Rätsel, H12-Boss, H13-Entscheidung, Save-Migration, Koop-Buchung + Tests | `Scripts/Story/StoryDirector.cs` (Finale), `Scripts/Story/StoryPickup.cs` (Moortor-Rätsel), `Scripts/Save/*` (Migration), `Scripts/Zones/*Gate*.cs` (Moortor), `Tests/PlayMode/Akt3FinaleTests.cs` (neu) |
| S4 proof | Autoplay bis complete, Voice neuer Zeilen, Doku | `Scripts/SessionAutoplay.cs`, `docs/akt3-finale-run-log.md` (neu), `docs/dev-journal.md`, `docs/mechanics-questions.md` + `docs/decision-log.md` (Finale-Entscheid) |

## Verify je Stufe (vor Commit, im Worktree-Root)

- `bash scripts/run-unity-tests.sh`; `SandboxBuilder.VerifyDiabloZone` + `KayKitAssets.Verify`; `python3 scripts/check-meta.py`
- Ab S3 `bash scripts/run-autoplay.sh <out> -seed 0` (Phasen bis `complete`), S4 zusätzlich `python3 scripts/check-voice-lines.py`
- `python3 ~/scripts/agent-workspace.py check` (still)
