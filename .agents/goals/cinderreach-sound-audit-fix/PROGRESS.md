# Progress – cinderreach-sound-audit-fix

Prozent: 100 %

## Erledigt

- Setgoal-Runde beantwortet (alle Empfehlungen übernommen: Audit + Fix, nur lokale Synth-Sounds, volles Ereignis-Inventar, 5 Zonen + Menü/Boss, Auto-Tests + Eigner-Hörabnahme, G-DIABLO01/02/03/06 + neue Audio-Probe G-DIABLO07).
- Basis belegt: `main @ c2b14bb`, sauber, Gleichstand mit `origin/main` per Fetch geprüft. Worktree `feature/cinderreach-sound-audit-fix` angelegt, Goal persistent ab 0 % mit Health-Feld.
- Audit-Befunde (Code-belegt): Kiln teilte das Ossuary-Thema (Clamp in `PlayZone`), `LanternHoldLoop` unerreichbar, `PlayMenu` ohne Aufrufer (keine Musik bis zum ersten Gate), `PlayCue(Ambient)` fiel auf `Hit` durch, Heilung/Quest-Abgabe/Schrein-Entdeckung/Block/Schritte stumm, Manifest ohne `Ambient` und ohne Kiln-/Barrens-/Glasswright-Tracks, alle Musik-Loops gleiches Rezept nur mit anderem Grundton.
- Fix: 5 neue Cues (`Heal`, `Quest`, `Shrine`, `Block`, `Footstep`) inkl. Counter, Clips und Verdrahtung (Skills-Heals, Story-TurnIn, WaypointShrine, PlayerHealth/Projectile-Block, ClickController-Schrittweite 1,9 m); `PlayCue(Ambient)` expliziter No-Op; `KilnLoop` (Zone3) + `ZoneTrack`-Mapping, `PlayTown` für Lantern Hold per Wegpunkt, Menü-Thema in `MainMenu.Open`, Zonen-Thema beim Verlassen des Startbildschirms, Boss-Thema + Roar in `BossController.EnterPhaseTwo`; per-Track-Synth-Charakter in `MusicDirector.SpecFor` (Dur/Moll, Sub, Glocken-/Glas-Partiale, Wind, Puls).
- Manifest auf 35 Cues / 8 Tracks nachgezogen (Version `S3-sound-audit`); Probe `scripts/check-audio-manifest.py` (G-DIABLO07) + Pytest grün.
- Tests: PlayMode voll 286/286, EditMode nach Allowlist-Bereinigung grün (volle Zahl im Land-Lauf), `AudioRoutingTests` 8/8 inkl. neuer Vertrags-Tests, `check-meta` OK, `pytest scripts` 6/6.
- Nachtrag 2026-11-23: Erste Hörabnahme meldete totale Stille. Ursache: Builder-Kamera ohne `AudioListener` (0 Treffer in der Szene), behoben und verifiziert (EditMode 496/496, PlayMode 287/287).
- Nachbesserung 2026-11-24 (Hör-Review „unharmonisch / zu schrill / Musik ohne Melodie"):
  - `DiabloAudio`: Phasen-integrierte Sweeps (beseitigt Frequenzverdopplung und Negativ-Frequenz-Artefakte), 2-polige Tiefpass-/Bandpass-Filter auf alle 35 Cues, warme Bronze-Glocken/Harp-Stings statt 1760-Hz-Pings, 5,4x-Teiltönen und ungefiltertem Weißem Rauschen; doppelte `SfxGain`-Multiplikation in `Varied`-Aufrufen sowie `SfxGain`-Umgehung in Legacy-Cues und UI-Click-Mute-Kopplung behoben; `GoldDrop` auf `PlayGold`, `DiabloVendorUI` auf `PlayVendorBuy`/`PlayVendorSell` verdrahtet.
  - `MusicDirector`: Alle 8 Tracks auf 16-s-Kreispuffer mit 4-Takt-Akkordprogression, ruhiger thematischer Melodie (Lute/Cello/BronzeBell/GlassMarimba/LowHorn), gedämpftem Forge-/Boss-Puls, 2-Pass-Tiefpass (950–1250 Hz) und nahtlosem Loop-Wrap ohne Klicks oder 5-ms-Endfade umgestellt; Live-Volume-Nachführung in `Update()` korrigiert; `ClassSelectUI.SetOpen(true)` startet `MenuTheme` und `DiabloAmbience` skaliert per `EffectiveMusic`.
  - Manifest auf `S4-audio-warmth-melody` aktualisiert; Spektral-/Naht-/Melodie-Tests in `AudioRoutingTests` ergänzt (EditMode 512/512, PlayMode 293/293 inkl. `AudioRoutingTests` 12/12, `check-audio-manifest` 35 Cues / 8 Tracks OK, `pytest scripts` 10/10, `check-meta` OK).
  - Nachbesserung 2026-11-24 (Varianz & Materialtreffer): Nicht-wiederholende 3-Clip-Variantenpools plus ±7 % Pitch/Mikro-Volume für `Swing`, alle 5 Treffer-Materialien (`FleshBone`, `ArmorMetal`, `StoneAsh`, `Glass`, `GlassShield` über `PlayHitFor`) und alle 4 Zonenböden (`Moor`, `Stone`, `Basalt`, `Sand` über `PlayFootstep(Vector3)`), redundante Doppel-`PlayHit`-Aufrufe in `DiabloMelee`/`DiabloProjectile.Reflect` bereinigt; EditMode 512/512, PlayMode 296/296 (`AudioRoutingTests` 15/15), `check-audio-manifest` OK, `pytest scripts` 10/10, `check-meta` OK.
- 2026-11-24: Eigner-Hörabnahme auf Windows-Build (Commit `ea59d7b`) bestanden („zufrieden“).

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
