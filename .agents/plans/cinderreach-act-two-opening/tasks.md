---
goal: cinderreach-act-two-opening
slug: cinderreach-act-two-opening
branch: feature/cinderreach-act-two-opening-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO06]
start_condition: Akt I komplett auf main (bf6d5cd) — erfüllt
plan_refs: [.agents/goals/cinderreach-act-two-opening/GOAL.md]
---

# Plan: cinderreach-act-two-opening — Glass Barrens (Akt II, Zone 4)

Pfad: feature, 5 Stufen, je eigener Worktree (`agent-workspace.py create --repo ~/unity-sandbox
feature/cinderreach-act-two-opening-sN`, `lock claim task cinderreach-act-two-opening-sN`, Abschluss `land`).

## Entscheide (S1)

1. Zone 4 heißt **Glass Barrens**: verglaste Ascheebene hinter dem Kiln, +600 m X, Root `BarrensRoot`.
2. `ActProgress.Barrens = 4`; die Zone öffnet mit dem Kilnmother-Kill (`CompleteAct` öffnet sie mit).
   `highestUnlockedZone` trägt die Freischaltung, kein neues Feld dafür.
3. Save v11 hängt `actTwoBossDefeated` an; Migration v10 → v11 öffnet die Barrens für Saves mit
   `actOneComplete`.
4. Wegpunkt „Glass Barrens“ wird als fünfter Eintrag an `KilnLayout.Shrines` angehängt (Index 3 bleibt Kiln).
5. Kein Auftragsbrett-Inhalt für Zone 4 (Nicht-Ziel).

## Stufen

### S1 Zone, Tor, Akt-Kette, Wegpunkt, Save v11
- `Zones/BarrensLayout.cs` (Konstanten), `Editor/BarrensBuilder.cs` (Boden, Glas-Splitter, Licht,
  KayKit-Dressing, Tore Kiln↔Barrens), Verify; `SandboxBuilder` ruft Build + Verify.
- `ActProgress` Zone 4 + `ZoneBossDefeated`; `SaveGame` v11 + Migration; `SaveService` Roundtrip.
- `EncounterLayout.CurrentZone` (x ≥ 500 → 4), `ZoneAtmosphere`/`DiabloAmbience`/`MusicDirector` mit
  eigenem Zone-4-Eintrag. `ItemLevelRules` folgt in S4 (Bracket-Design).
- Tests: `ActProgressTests`, `SaveMigrationTests`, `WaypointTests`, neu `BarrensConfigTests`.
- Verify: EditMode + PlayMode, `VerifyDiabloZone`, `KayKitAssets.Verify`, `check-meta.py`.

### S2 Gegnerfamilie + Elite, Encounter-Layout, Seeds
- Schildträger (Frontalblock, reflektiert keine Projektile, Flanke/Rücken voller Schaden) und
  Flächenbeschwörer (Glas-Splitterfeld mit Telegraph); Elite-Variante; 3 Layout-Varianten; Arrival-Plaza.
- Netz: HP/Tod über bestehende `EnemyHealth`-Replikation, Summons über Broadcast-Pfad.

### S3 Zonen-Boss
- Zwei Phasen, Telegraphen, Bossbalken mit Phase, Musik-Cue; Kindled-Phase-2-Mechanik;
  Kill setzt `ZoneBossDefeated` + Save.

### S4 Beute + Koop-Parität
- `ItemLevelRules` Zone 4 (Itemlevel 15, neues Bracket 4), ≥ 2 Zonen-Uniques. Der Koop-Paarlauf durch Tor und Boss
  wandert nach S5, weil er die Autoplay-Phasen der Barrens braucht.

### S5 Nachweis + Fold-in
- Autoplay-Phasen `zone4`/`boss3`, Koop-Paarlauf durch Tor und Boss, Matrix 3 Klassen × Normal/Kindled × Seeds 0–2, FPS-Gate,
  Windows-Shotcheck, Nightly + dotfiles-Health-Vertrag, Doku, CINDERREACH.md-Fold-in.
