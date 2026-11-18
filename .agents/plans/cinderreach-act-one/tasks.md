---
goal: cinderreach-act-one
slug: cinderreach-act-one
branch: feature/cinderreach-act-one-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: cinderreach-replayability 100 %
---

# Plan: cinderreach-act-one — Akt I mit Cinder Kiln und Akt-Boss

Pfad: feature, 5 Stufen. Ausführung nach dem **Session-Protokoll** aus
`.agents/plans/arpg-session-loop/tasks.md` (Schritte 1–9), Lock-Name `task cinderreach-act-one-sN`.
Protokollschritt 1 prüft zusätzlich: `PROGRESS.md` von `cinderreach-replayability` zeigt `Prozent: 100 %`.

## Ausgangslage (Belege, Stand 2026-11-18, HEAD `c70ca4a`)

- `Editor/OssuaryBuilder.cs` (221 Z.): Zone 2 als abgesetzter Bereich in `DiabloZone.unity` bei
  `OssuaryOffset = (200, 0, 0)`, `RootName = "OssuaryRoot"`, 8 Caster + 2 Elites, `SpawnGate`, `SpawnBoss`.
- `Scripts/Zones/ZoneGate.cs`: Trigger → Teleport → `SaveService.SaveNow(hero, zoneReached)`. Zwei feste
  Namen (`FenGate`, `OssuaryGate`), `targetZone` 1 = Town, 2 = Ossuary.
- `Scripts/Enemies/BossController.cs` (155 Z.): Hollow Abbot, 150 HP, Phase 2 bei 50 % (`EnterPhaseTwo`),
  `OnBossDeath` öffnet `SessionComplete`. `Scripts/Enemies/RangedCaster.cs` für Bone Warden.
- `Scripts/Save/SaveGame.cs`: v1, `zoneReached` als einzige Fortschrittsangabe. Nach `cinderreach-replayability`
  S1 gibt es `SaveMigrations` und Save v2 (`difficulty`, `runSeed`, `stash`).
- Aus Vorgänger-Goals erwartet: `SessionAutoplay` (`-autoplay`, `-seed`, `-difficulty`), Ergebnis-JSON +
  nächtlicher Lauf + `G-DIABLO06` (loop-guard), `LayoutSeeder` + `DifficultyProfile` + `AffixDef`
  (replayability), Zonen-Lichtprofile `Settings/Volumes/*.asset` + `LightingBuilder` (presentation).

## Entscheide

1. Identität (Eigner 2026-11-18): Zone 3 **Cinder Kiln** (ausgebrannte Brennöfen-Abtei), Gegner
   **Cinder Thrall** (Nahkampf-Schwarm, explodiert beim Tod) und **Ash Lancer** (telegrafierter
   Sturmangriff), Akt-Boss **The Kilnmother** (Phase 1 Feuerwellen, Phase 2 brennender Boden + Adds).
2. Zone 3 liegt wie das Ossuary **in `DiabloZone.unity`** bei `KilnOffset = (400, 0, 0)`. Kein
   NGO-Szenenwechsel, Co-op-Pfad bleibt unberührt. Planannahme, folgt Entscheid 2 aus arpg-session-loop.
   Erfolgskriterium 1 („eigene Szene“) ist damit als eigener Szenenbereich mit eigenem Root erfüllt.
3. Akt-Fortschritt als eigener Typ `ActProgress` (pure C#): Liste freigeschalteter Zonen, `actOneComplete`.
   `zoneReached` bleibt für Rückwärtskompatibilität und wird aus `ActProgress` abgeleitet.
4. Wegpunkte: ein `WaypointShrine` je Zone (Town, Fen, Ossuary, Kiln) plus `DiabloWaypointUI`. Reisen
   nutzt die Teleport-Logik aus `ZoneGate` (ausgelagert nach `ZoneTravel.TeleportHero`), damit Co-op-S3
   (`ZoneTravelRelay`) nur eine Stelle replizieren muss.
5. Save: nächste freie Version nach replayability (geplant v3) mit `unlockedZones`, `actOneComplete`,
   `waypoints`. Migration aus v2: `zoneReached` → `unlockedZones`, Ossuary-Boss gilt als besiegt, wenn
   `difficultyUnlocked` gesetzt ist.
6. Kiln-Gegner nutzen `EnemyHealth`/`EnemyAttack` und die Seeds/Affixe/Kindled-Skalierung aus replayability,
   keine Sonderwege.

7. Nachtrag S1 (2026-11-18): Save wurde v4 (v2 replayability, v3 second-class). `ActProgress` lebt in
   `RunProfile.Act`; `zoneReached` bleibt als letzte betretene Zone erhalten.

## Partial-Manifest

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 act-save | ActProgress, Save v3, Migration, Freischaltkette | `Scripts/Zones/ActProgress.cs` (neu), `Scripts/Save/SaveGame.cs`, `Scripts/Save/SaveMigrations.cs`, `Scripts/Save/SaveService.cs`, `Scripts/Enemies/BossController.cs` (Ossuary-Kill → Kiln frei), `Tests/EditMode/ActProgressTests.cs` (neu), `Tests/EditMode/SaveMigrationTests.cs` |
| S2 kiln-world | Zone-3-Geometrie, Props, Licht, Gate Ossuary → Kiln | `Editor/KilnBuilder.cs` (neu), `Editor/SandboxBuilder.cs` (Aufruf + Verify), `Editor/WorldArtBuilder.cs` (Kiln-Props), `Settings/Volumes/CinderKiln.asset` (neu), `Scripts/Zones/ZoneGate.cs` (`KilnGateName`, `targetZone = 3`), `Tests/EditMode/KilnConfigTests.cs` (neu) |
| S3 kiln-enemies | Cinder Thrall, Ash Lancer, Spawns per Seed | `Scripts/Enemies/CinderThrall.cs` (neu), `Scripts/Enemies/AshLancer.cs` (neu), `Scripts/Loot/DroptableDatabase.cs` (Kiln-Tiers), `Scripts/Zones/LayoutSeeder.cs` (Kiln-Anker-Sets), `Tests/EditMode/KilnEnemyConfigTests.cs` (neu), `Tests/PlayMode/KilnEnemyTests.cs` (neu) |
| S4 kilnmother | Akt-Boss, Akt-Abschluss | `Scripts/Enemies/KilnmotherController.cs` (neu), `Scripts/Enemies/BurningGround.cs` (neu), `Scripts/SessionComplete.cs` (Akt-I-Abschluss), `Scripts/Loot/LootTable.cs` (`LootTier.ActBoss`), `Tests/PlayMode/KilnmotherLoopTests.cs` (neu) |
| S5 waypoints+proof | Wegpunkte, Autoplay bis Akt-Boss, Shotcheck, Docs | `Scripts/Zones/ZoneTravel.cs` (neu), `Scripts/Zones/WaypointShrine.cs` (neu), `Scripts/Zones/DiabloWaypointUI.cs` (neu), `Editor/TownBuilder.cs` (Town-Schrein), `Scripts/SessionAutoplay.cs` (Kiln-Route), `Scripts/DiabloShotCheck.cs` (Kiln-, Boss-Shots), `Tests/PlayMode/WaypointTests.cs` (neu), `docs/act-one-run-log.md` (neu), `docs/one-page-gdd.md`, `docs/dev-journal.md`; dotfiles: `scripts/health_probes/diablo_unity.py`, `scripts/test_diablo_unity_health.py` |

S1 zuerst (Save-Format und Freischaltkette). S2 nach S1. S3 und S4 nach S2, untereinander parallel
(disjunkt bis auf `DroptableDatabase.cs`/`LootTable.cs`, die je nur eine Stufe anfasst). S5 zuletzt.

## S1 — Akt-Fortschritt + Save

- `ActProgress`: `IsUnlocked(zone)`, `Unlock(zone)`, `CompleteAct()`, `ZoneReached` (größte freie Zone).
  Kette: Zone 1 + 2 ab Start wie heute, Zone 3 nach Ossuary-Boss-Kill, `actOneComplete` nach Kilnmother.
- `BossController.OnBossDeath` schaltet Zone 3 frei und speichert sofort (nur `Diablo.IsLocalAuthority`).
- Save v3 + Migration v2 → v3 in `SaveMigrations`, Backup `*.v2.bak` vor dem ersten v3-Schreiben.
- Tests: Kette in richtiger Reihenfolge, keine Freischaltung ohne Kill, v2-Fixture migriert verlustfrei,
  unbekannte höhere Version wird abgelehnt.

## S2 — Cinder Kiln (Welt)

- `KilnBuilder.BuildKiln()` analog `OssuaryBuilder`: Root `KilnRoot` bei `KilnOffset`, Bodenplatte,
  Brennofen-Ring, Schlackehaufen, Glut-Lichter. Props aus der Blender-/KayKit-Pipeline über
  `WorldArtBuilder`, keine Fremd-IP. Volume-Profil `CinderKiln` (warm, Asche-Partikel) über `LightingBuilder`.
- `ZoneGate` bekommt `KilnGateName`. Tor-Paar Ossuary ↔ Kiln, aktiv nur bei `ActProgress.IsUnlocked(3)`,
  gesperrtes Tor zeigt Hinweis „Die Glut ruht noch“.
- `VerifyDiabloZone` prüft `KilnRoot`, Tor-Paar, Lichtprofil, Bodenkollision.
- Tests: Config (Offset, Root, Tore, Lichtprofil vorhanden), gesperrtes Tor teleportiert nicht.

## S3 — Kiln-Gegner

- `CinderThrall`: schneller Nahkämpfer in Gruppen à 4–6, beim Tod 0,6 s Telegraph + Explosion (Radius
  2 m, 2 Schaden), Explosion trifft auch andere Thralls nicht (keine Kettenreaktion).
- `AshLancer`: hält 8 m, Sturmangriff mit 0,7 s Telegraph-Linie, 4 Schaden, 2 s Erschöpfung danach.
- Spawns über `LayoutSeeder`-Anker-Sets (3 Varianten), Zahlen: 18 Thralls, 6 Lancer, 2 Elites.
  Kindled-Skalierung über `DifficultyProfile`, Drop-Tiers über `DroptableDatabase`.
- Tests: Config-Werte, Explosion trifft Held im Radius und nicht außerhalb, Lancer-Telegraph vor
  Schaden, Kindled-Skalierung auf beide Typen, Erreichbarkeit aller Seeds (Muster aus replayability S3).

## S4 — The Kilnmother

- `KilnmotherController` (Basis `BossController`-Muster, eigene Klasse): ~320 HP auf Level 9–11 skaliert.
  Phase 1: drei Feuerwellen (Ring-Telegraph, Ausweichlücke). Phase 2 ab 50 %: `BurningGround`-Felder
  (Telegraph 1 s, Schaden pro Sekunde), beschwört 4 Thralls, schnellere Wellen.
- `LootTier.ActBoss`: garantiert Rare+, 1 Unique-Wurf, 300–450 Gold.
- Tod: `ActProgress.CompleteAct()`, Save, `SessionComplete` im Modus „Akt I abgeschlossen“ (Spielzeit,
  Kills, Gold, bester Drop über alle 3 Zonen).
- Tests: Phasenwechsel bei 50 %, Burning Ground schadet nur im Feld, Adds einmalig, Drop Rare+,
  Akt-Abschluss persistiert über Save/Load.

## S5 — Wegpunkte + Beweis

- `ZoneTravel.TeleportHero(hero, destination, zone)` aus `ZoneGate.Teleport` extrahieren (Verhalten
  gleich, `ZoneGate` ruft es auf). `WaypointShrine` je Zone, entdeckt beim ersten Betreten (Save-Feld
  `waypoints`). `DiabloWaypointUI` (HD-Scaler-Konvention, `DiabloHdTests` erweitert) listet freie Ziele.
- `SessionAutoplay`: Route Town → Fen → Ossuary-Boss → Kiln → Kilnmother, Wegpunkt-Reise einmal
  benutzt. Ergebnis-JSON trägt `actOneComplete`.
- `DiabloShotCheck`: Kiln-Eingang, Thrall-Gruppe, Kilnmother Phase 2, Akt-Abschluss. Windows über
  `scripts/stage-windows.sh` → `scripts/shotcheck-windows.sh`, PNGs lesen, im Run-Log verlinken.
- dotfiles-Worktree: `G-DIABLO02`-Vertrag um `KilnRoot`, Kiln-Tore, Kilnmother erweitern, `G-DIABLO06`
  wertet `actOneComplete` aus. `check`, Test, `land`.
- Docs: `act-one-run-log.md` (3 Seeds, Normal + Kindled), `one-page-gdd.md`, `dev-journal.md`.
- Akzeptanz: alle 6 Goal-Kriterien belegt, `PROGRESS.md` auf 100 %.

### Statustabelle

| Stufe | Abhängig von | Gewicht | Status |
|---|---|---|---|
| S1 act-save | Startbedingung | 15 % | landed 2026-11-18 |
| S2 kiln-world | S1 | 20 % | offen |
| S3 kiln-enemies | S2 | 20 % | offen |
| S4 kilnmother | S2 | 25 % | offen |
| S5 waypoints+proof | S3, S4 | 20 % | offen |

## Verify (je Stufe)

1. `scripts/run-unity-tests.sh` grün (EditMode + PlayMode, Zahlen ins Journal).
2. `VerifyDiabloZone` grün, ab S3 für alle Seeds; `KayKitAssets.Verify` (`repo.yaml:verify`).
3. `bash scripts/run-autoplay.sh` Exit 0, ab S5 bis `actOneComplete` (Normal + Kindled).
4. `python3 scripts/check-meta.py` ohne Befund.
5. dotfiles-Anteil (S5): `check` + `python3 -m pytest scripts/test_diablo_unity_health.py -q`, nach `land`
   `python3 ~/scripts/repo_health_collect.py` → `G-DIABLO02/03/06` grün.

## Risiken

- R1 `SandboxBuilder.cs` (1358 Z.) wächst weiter. Gegenmittel: Kiln komplett in `KilnBuilder`, im
  Monolithen nur Aufruf und Verify-Zeilen.
- R2 Drei Zonen in einer Szene erhöhen Ladezeit und Draw Calls. Gegenmittel: Zonen-Roots außerhalb der
  aktiven Zone deaktivieren (`ZoneTravel`), FPS-Gate aus loop-guard als Beleg.
- R3 Save-Kette v1 → v2 → v3. Gegenmittel: Migrationstest über die volle Kette mit v1-Fixture.
- R4 Co-op: Freischaltung ist lokal pro Client (Entscheid 3 aus arpg-session-loop). Ein Client ohne
  Freischaltung folgt dem Host über `ZoneTravelRelay` (coop S3). Prüfen in S5, Befund ins Run-Log.
- R5 Balance mit `-autoplay-god` nicht belegbar. Akzeptiert wie in replayability: Autoplay belegt
  Durchspielbarkeit, Balance-Urteil bleibt Eigner-Playtest.
