# Progress: cinderreach-act-one

Prozent: 35 %

## Erledigt

- 2026-11-18: Plan `.agents/plans/cinderreach-act-one/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: Goal angelegt (Folge-Goal #5).

- 2026-11-18: Startbedingung erfüllt (`cinderreach-replayability` 100 %).
- 2026-11-18: S1 act-save gelandet. `ActProgress` (Ossuary offen wie bisher, Kiln nach dem ersten Abbot-Kill,
  Akt-Abschluss nur mit offenem Kiln, Wegpunkte), Save v4 (`highestUnlockedZone`, `actOneComplete`,
  `waypoints`), Migration v3 → v4 (Kindled-Freischaltung belegt den Abbot-Kill, Kiln dann offen), Backup
  `*.v3.bak`. Belege: EditMode 174/174 (6 neu `ActProgressTests`), PlayMode 80/80, Verifies, Autoplay mit
  `[Act] Cinder Kiln unlocked.` beim Abbot.

- 2026-11-18: S2 kiln-world gelandet. `KilnBuilder` baut den Cinder Kiln bei +400 m X (Ascheboden, sechs
  Brennofen-Kuppeln mit Glutöffnung, Schlacke, sechs Glutlichter, KayKit-Schutt/-Fässer/-Fackeln), Tor-Paar
  `KilnGate` (Ossuary, gesperrt bis zum Abbot, Hinweis „Die Glut ruht noch“) und `KilnReturnGate`, `VerifyKiln`
  im `VerifyDiabloZone`. `ZoneGate` lehnt gesperrte Zielzonen ab. Belege: EditMode 177/177 (3 neu
  `KilnConfigTests`), PlayMode 80/80, Verify `domes=6 embers=6 gate=True hint=True ret=True`, Autoplay 6/6.

## Nächste Schritte

1. S3 kiln-enemies und S4 kilnmother.

## Offene Punkte

- S3 bis S5 offen.
