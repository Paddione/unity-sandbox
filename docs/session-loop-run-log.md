# Session-loop run log — S6 proof (Basis ohne S5, 2026-11-18)

Branch: `feature/arpg-session-loop-s6` (Basis: origin/main mit S1–S4, OHNE S5).
S5-Beine (Ossuary-Root, `ZoneGate`, Bone Warden, The Hollow Abbot,
`SessionComplete`) laufen im Folgepass nach dem Rebase; Seams stehen in
`Scripts/SessionAutoplay.cs` und `Scripts/DiabloShotCheck.cs`.

## Autoplay-Lauf (Linux-Player, `-autoplay -autoplay-god`)

- Build: `SandboxBuilder.BuildDiabloZoneBuild` → Succeeded
  (`builds/diablo-zone/diablo-zone`, 116–118 MB, Ablagepfad des Builders im
  Hauptcheckout, git-ignored).
- Lauf: `xvfb-run -a ./diablo-zone -autoplay -autoplay-god`
  (1280×720, Timeout 330 s; Gott-Cheat nur für Determinismus, im Log als
  `god=ON (deterministic cheat)` ausgewiesen).
- Ergebnis: **Exit-Code 0, 0 Exceptions / 0 Errors** im Player-Log.
- Phasen (`[Autoplay]`):
  - `town_waypoints`: Campfire → Vendor → ZGatePillar_0 erreicht.
  - `vendor_potion`: `bought=True` (S3-Seam `VendorStock.BuyPotion`, Gold
    100 → 90, Trank im Bag).
  - `combat`: `kills=12` (10 Husks + Fen Brute + Ash Elite, Melee + Q/E,
    29 Schadens-Events im Log).
  - `fen_gate`: GoalPad erreicht (S5-Folge: `ZoneGate`-Paar passieren).
  - `ossuary` / `boss`: SKIPPED mit Seam-Verweis (S5, nicht vorhanden).
  - `DONE phases=4 exceptions=0`.
- Rohlog: `/tmp/s6-autoplay2.log` (erster Lauf `/tmp/s6-autoplay.log`;
  Unterschied: Kill-Zähler `kills~0` → `kills=12` gefixt, Neulauf).

## Shotcheck (Windows-Stage)

- Windows-Build: `SandboxBuilder.BuildDiabloZoneWindows` → Succeeded.
- Stage: `scripts/stage-windows.sh` → OK (`Games\cinderreach`, 119 MB).
- Shots: `scripts/shotcheck-windows.sh` → 18 Shots, `DONE 18 shots`, u. a. neu:
  - `artifacts/shotcheck/16_town_lantern_hold.png` — Lantern Hold von oben
    (Palisade, Campfire, Vendor-NPC; Vendor-Panel im Shot sichtbar).
  - `artifacts/shotcheck/17_vendor_open.png` — „Lantern Hold vendor“,
    Gold-Anzeige, Trank-Tooltip „Buy … Potion (+10 HP, key 1) — 10g“.
  - `artifacts/shotcheck/18_character_open.png` — Character-Sheet
    (Attribute points: 1, Skill points: 1, Lv-2-Held, Q/E/R-Leiste).
- Befund: alle drei Panels rendern korrekt (HD-Scaler). Nebenbefund (S3-Kosmetik,
  außerhalb S6-Scope, für Folgepass notiert): Vendor-Tooltip-Text läuft eng
  ineinander („Buy Healing Potion“-Zeilen überlappen leicht).
- S5-Shots (`ossuary_arrival`, `bone_warden`, `abbot_phase2`,
  `session_complete`) als reservierte Namen + TODO im `DiabloShotCheck`.

## Suiten / Verify (Basis-Stand vor S5-Rebase)

- `scripts/run-unity-tests.sh`: EditMode **83/83**, PlayMode **69/69**.
- `SandboxBuilder.VerifyDiabloZone`: OK
  (`player=True husks=10 … palisade=15 torches=4 net=True`,
  „ashen fen + loot + netcode present“).
- `KayKitAssets.Verify`: grün.
- `python3 scripts/check-meta.py`: OK.

## Voller Session-Lauf (Linux-Player, `-autoplay -autoplay-god`, S5+S6, 2026-11-18)

- Build: `SandboxBuilder.BuildDiabloZoneBuild` → Succeeded (117 MB).
- Lauf: `xvfb-run -a ./diablo-zone -autoplay -autoplay-god`, Timeout 590 s.
- Ergebnis: **Exit-Code 0, 0 Exceptions** (`/tmp/s6-autoplay-full.log`).
- Phasen (`[Autoplay]`): `town_waypoints` (Campfire → Vendor → ZGatePillar_0) →
  `vendor_potion` (`bought=True`, Gold 100 → 90) → `combat_fen` clear (kills=12:
  10 Husks + Fen Brute + Ash Elite) → `gate_FenGate` (teleports=1,
  zoneSaved=True, expectZone=2) → `combat_ossuary` (kills=0 in der Leash —
  Wardens außerhalb 80-m-Radius, fallen in der Boss-Phase) → `boss`
  (bossKills=1, phaseTwo=1, Drop „Rare+ boss cache“) → Overlay
  (`SessionComplete`, Zeit 73 s, Kills 17, Gold 103) → `gate_OssuaryGate`
  (teleports=1, zoneSaved=True, expectZone=1).
- `DONE phases=7 huskKills=13 bossKills=1 phaseTwo=1 overlay=1 exceptions=0`.
- Ehrliche Grenze: Ossuary-Clear findet in der 80-m-Leash keine Ziele
  (kills=0 dort); Wardens + Summons sterben im Boss-Kampf. Kein Fake-Grün:
  Zähler stehen so im Log. Follow-up: Leash/Anmarsch der Wardens.

## Shotcheck Vollpass (Windows-Stage, 2026-11-18)

- Build: `SandboxBuilder.BuildDiabloZoneWindows` → Succeeded (118 MB).
- Stage: `scripts/stage-windows.sh` → OK (`Games\cinderreach`, 147 MB;
  `cinderreach_Data` frisch, Launcher-Exe unverändert).
- Shots: 22 (`15`–`18` Basis + neu `19_ossuary_arrival.png`,
  `20_bone_warden.png` (Telegraph läuft), `21_abbot_phase2.png`
  (Summons + Fächer), `22_session_complete.png`), Größen 520–590 KB
  (echte Renders, keine Leerbilder).

## Suiten / Verify (finaler Stand)

- `scripts/run-unity-tests.sh`: EditMode **89/89**, PlayMode **71/71**.
- `SandboxBuilder.VerifyDiabloZone`: OK (`root=True fen=True ret=True
  casters=8 elites=2 boss=True`, `husks=21`).
- `KayKitAssets.Verify`: grün. `python3 scripts/check-meta.py`: OK.
