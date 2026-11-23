# Progress – cinderreach-sound-audit-fix

Prozent: 85 %

## Erledigt

- Setgoal-Runde beantwortet (alle Empfehlungen übernommen: Audit + Fix, nur lokale Synth-Sounds, volles Ereignis-Inventar, 5 Zonen + Menü/Boss, Auto-Tests + Eigner-Hörabnahme, G-DIABLO01/02/03/06 + neue Audio-Probe G-DIABLO07).
- Basis belegt: `main @ c2b14bb`, sauber, Gleichstand mit `origin/main` per Fetch geprüft. Worktree `feature/cinderreach-sound-audit-fix` angelegt, Goal persistent ab 0 % mit Health-Feld.
- Audit-Befunde (Code-belegt): Kiln teilte das Ossuary-Thema (Clamp in `PlayZone`), `LanternHoldLoop` unerreichbar, `PlayMenu` ohne Aufrufer (keine Musik bis zum ersten Gate), `PlayCue(Ambient)` fiel auf `Hit` durch, Heilung/Quest-Abgabe/Schrein-Entdeckung/Block/Schritte stumm, Manifest ohne `Ambient` und ohne Kiln-/Barrens-/Glasswright-Tracks, alle Musik-Loops gleiches Rezept nur mit anderem Grundton.
- Fix: 5 neue Cues (`Heal`, `Quest`, `Shrine`, `Block`, `Footstep`) inkl. Counter, Clips und Verdrahtung (Skills-Heals, Story-TurnIn, WaypointShrine, PlayerHealth/Projectile-Block, ClickController-Schrittweite 1,9 m); `PlayCue(Ambient)` expliziter No-Op; `KilnLoop` (Zone3) + `ZoneTrack`-Mapping, `PlayTown` für Lantern Hold per Wegpunkt, Menü-Thema in `MainMenu.Open`, Zonen-Thema beim Verlassen des Startbildschirms, Boss-Thema + Roar in `BossController.EnterPhaseTwo`; per-Track-Synth-Charakter in `MusicDirector.SpecFor` (Dur/Moll, Sub, Glocken-/Glas-Partiale, Wind, Puls).
- Manifest auf 35 Cues / 8 Tracks nachgezogen (Version `S3-sound-audit`); Probe `scripts/check-audio-manifest.py` (G-DIABLO07) + Pytest grün.
- Tests: PlayMode voll 286/286, EditMode nach Allowlist-Bereinigung grün (volle Zahl im Land-Lauf), `AudioRoutingTests` 8/8 inkl. neuer Vertrags-Tests, `check-meta` OK, `pytest scripts` 6/6.

## Nächste Schritte

- Windows-Build gebaut und gestaged: `SandboxBuilder.BuildDiabloZoneWindows` (Commit `5f25105`, 180 MB) per `scripts/stage-windows.sh` nach `C:\Users\PatrickKorczewski\Games\cinderreach` (Beleg `BUILD_INFO.json`, 2026-11-23). Jetzt Eigner-Hörabnahme je Zone/Cue.
- Nach Hörabnahme: Prozent auf 100 %, Offene Punkte auf `Keine.`, Goal in CINDERREACH.md abschließen.

## Offene Punkte

- Eigner-Hörabnahme (Windows-Build) steht aus — echter Blocker für 100 %, braucht Eigner-Ohren; Klangpassung zum Setting ist per Unit-Test nicht beweisbar.
