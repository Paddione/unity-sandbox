---
goal: cinderreach-akt2-vertiefung
slug: cinderreach-akt2-vertiefung
branch: feature/cinderreach-akt2-vertiefung-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: main @ 73ca3e2 sauber; S1 erst nach Akt-I-S1-Modellstand (kein paralleles Schreiben an StoryModel/StoryState)
plan_refs: [.agents/goals/cinderreach-akt2-vertiefung/GOAL.md]
---

# Plan: cinderreach-akt2-vertiefung — Akt II doppelt (Umfang + Tiefe + Rätsel)

Pfad: feature, 3 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-akt2-vertiefung-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).

## Goal

Siehe [GOAL.md](../../goals/cinderreach-akt2-vertiefung/GOAL.md): Akt II 6 → ca. 12 Einträge
(H6–H9/N4–N5 + Folgen), Wendung 3 als roter Faden, mind. 2 Umgebungs- + 1 Entscheidungsrätsel,
neue H9-`x|nK`-Varianten, Anker H6/H8/H9 erhalten.

## Success Criteria

Die sechs Erfolgskriterien aus GOAL.md. Gesamt-Gate S3: EditMode + PlayMode grün,
`VerifyDiabloZone` + `KayKitAssets.Verify` grün, `check-meta.py` grün,
Autoplay `-seed 0` bis `boss3` Exit 0 / 0 Exceptions.

## Ausgangslage (Belege, HEAD `73ca3e2`, 2026-11-24)

- Story-SSOT `docs/story-bible.md` §Akt II: H6–H9 + N4–N5, Wendung 3, H9 als Haken für Akt III.
- Code/Test-Basis wie Akt-I-Plan; Zonen-Basis `cinderreach-akt2-opening` (`BarrensLayout`, `Barrens*Builder`, `Shardback`/`DuneAugur`/`GlassElite`, Glasswright 420 HP, Bracket 4).

## Entscheide (gelockt, Eigner 2026-11-24, setgoal-Runde)

1. Doppelt = beides kombiniert; Tamm-Strang (N4) und Ilsabe-Strang (N5) je + mind. 1 Folgequest.
2. Rätsel gemischt (Sehglas-/Scherben-Umgebung + behalten/abliefern-Entscheidung).
3. Neue Zeilen verständlich-modern; Bestand in `cinderreach-sprache-voice`.
4. Keine neuen Zonen-Layouts/Gegner-Typen; Bracket-4-/Unique-Regeln unangetastet.

## Approach

Bibel zuerst (Akt-II-Tabellen + Rätsel), dann Daten auf dem Akt-I-Modellstand, dann Brachen-Strang
(Sehglas-/Scherben-Rätsel, Tamm-/Ilsabe-Folgen, Glasmacher-Anker), zuletzt Beweis bis `boss3`.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 bibel+modell | Akt-II-Tabellen neu (ca. +6), Rätsel-Definitionen, Model/State/Director auf Akt-I-Stand + Tests | `docs/story-bible.md` (§Akt II), `Scripts/Story/StoryModel.cs`, `Scripts/Story/StoryState.cs`, `Scripts/Story/StoryDirector.cs`, `Tests/EditMode/Akt2ModellTests.cs` (neu) |
| S2 brache | Brachen-Strang: H-Ergänzungen, Sehglas-/Scherben-Rätsel, Tamm-/Ilsabe-Folgen, H9-Varianten + Tests | `Scripts/Story/StoryDialogs.cs` (Akt II), `Scripts/Story/StoryPickup.cs` (Sehglas/Scherben), `Scripts/Story/StoryBarks.cs` (Wendung-3-Zurufe), `Localization/LocCatalog.Story.cs` (Brache-Zeilen), `Tests/PlayMode/Akt2BrachenTests.cs` (neu) |
| S3 proof | Save/Koop neuer Schritte, Autoplay bis boss3, Voice neuer Zeilen, Doku | `Scripts/Save/*` (Migration), `Scripts/SessionAutoplay.cs`, `docs/akt2-vertiefung-run-log.md` (neu), `docs/dev-journal.md` |

## Verify je Stufe (vor Commit, im Worktree-Root)

- `bash scripts/run-unity-tests.sh`; `SandboxBuilder.VerifyDiabloZone` + `KayKitAssets.Verify`; `python3 scripts/check-meta.py`
- Ab S2 `bash scripts/run-autoplay.sh <out> -seed 0` (Phasen bis `boss3`), S3 zusätzlich `python3 scripts/check-voice-lines.py`
- `python3 ~/scripts/agent-workspace.py check` (still)
