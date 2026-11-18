# Progress: cinderreach-act-one

Prozent: 80 %

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

- 2026-11-18: S3 kiln-enemies und S4 kilnmother gelandet. Cinder Thrall (Nahkampf über `EnemyAttack`, beim Tod
  0,6 s Telegraph + 2-m-Explosion nur gegen den Helden), Ash Lancer (hält 8 m, 0,7 s Telegraph-Linie, Sturm 14 m/s,
  einmal 4 Schaden, 2 s erschöpft, Aggro 16 m), 18 Thralls in sechs Packs + 6 Lancer + 2 Kiln Elites, drei
  Anker-Varianten über `LayoutSeeder` (Kiln-Ziehung zuletzt, Fen-Picks bestehender Seeds unverändert). The
  Kilnmother (320 HP, Feuerwellen mit 60°-Lücke, ab 50 % schnellere Wellen + brennender Boden + 4 Thralls einmalig,
  Tod → `ActProgress.CompleteAct`, Save, Overlay „AKT I ABGESCHLOSSEN“), `LootTier.ActBoss` (Rare+, 40 % Unique,
  300–450 Gold). Belege: EditMode 186/186 (10 neu `KilnEnemyConfigTests`, u. a. Erreichbarkeit aller Kiln-Anker in
  drei Varianten), PlayMode 82/82 (2 neu `KilnmotherLoopTests`), Verify `thralls=18 lancers=6 elites=2
  kilnmother=True armed=True`, Autoplay 6/6.

## Nächste Schritte

1. S5 waypoints+proof (Wegpunkte, Autoplay bis zur Kilnmother, Shotcheck, Probe).

## Offene Punkte

- S5 offen.
