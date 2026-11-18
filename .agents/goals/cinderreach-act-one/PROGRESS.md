# Progress: cinderreach-act-one

Prozent: 100 %

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

- 2026-11-18: S5 waypoints+proof gelandet. `ZoneTravel` (ein Teleport-Pfad für Tore und Schreine),
  `WaypointShrine` je Zone (Lantern Hold, Ashen Fen, Sunken Ossuary, Cinder Kiln; Entdeckung per Nähe, im Save),
  `DiabloWaypointUI` (HD-Scaler), Autoplay-Route bis zur Kilnmother und per Wegpunkt zurück, Pflichtphasen
  `zone3`/`actboss`, `actOneComplete` im Ergebnis, Nachtlauf-Timeout 60 min. Perf-Fix: `HeroLocator` (eine
  Hero-Suche pro Frame statt ~50) und Leichen-Deckel (16, Animator aus nach 1,5 s). Belege: EditMode 191/191
  (5 neu `WaypointTests`), PlayMode 84/84 (2 neu `CorpseCapTests`), Verify mit vier Schreinen, neun Läufe
  (Normal/Kindled/Lanternbearer × Seeds 0–2) 8/8 Phasen, Akt I abgeschlossen; finaler Code schlechtestes
  FPS-Fenster 36,5/36,9/38,6. Shots `26_kiln_entrance`, `27_thrall_pack`, `28_kilnmother_phase2`,
  `29_act_one_complete`. Run-Log `docs/act-one-run-log.md`.

- 2026-11-18: Live-Beleg. dotfiles `023ac60b` (acht Pflichtphasen, Kiln-Skripte in `G-DIABLO02`) und `e6463e03`
  (Dauer-Basis nur aus vollständigen Läufen der aktuellen Route; die alte Route ohne Kiln hätte jeden neuen Lauf rot
  gemacht). Dispatch https://github.com/Paddione/unity-sandbox/actions/runs/44288445907 grün: Normal 170 s,
  Kindled 350 s, Lanternbearer 175 s, je 8/8 Phasen, `actOneComplete`, FPS-Minimum 37,8/41,2/35,0.
  Live: `G-DIABLO01`–`06` achieved.

Kriterien-Abgleich:
1. Cinder Kiln als eigener Szenenbereich mit Cinder Thrall, Ash Lancer und The Kilnmother (zwei Phasen):
   `VerifyDiabloZone`/`VerifyKiln`, `KilnEnemyConfigTests`, `KilnmotherLoopTests`.
2. Freischaltkette Abbot → Kiln → Kilnmother → Akt I, Save/Load: `ActProgressTests`, `KilnmotherLoopTests`.
3. Wegpunkte zwischen Lantern Hold und allen freigeschalteten Zonen: `WaypointTests` (EditMode statt PlayMode),
   Autoplay reist Kiln → Lantern Hold.
4. Seeds, Affixe, Kindled im Kiln: drei Anker-Varianten über `LayoutSeeder`, Kindled-Kilnmother 576 statt 320 HP.
5. Nächtlicher Autoplay bis zum Akt-Boss, FPS-Gate grün: Dispatch oben, `G-DIABLO06` achieved.
6. Windows-Build gestaged, Shots `26_kiln_entrance`, `27_thrall_pack`, `28_kilnmother_phase2`, `29_act_one_complete`.

## Nächste Schritte

- Keine. Offen laut `.agents/goals/ROADMAP.md`: `cinderreach-coop-session`, `cinderreach-presentation`;
  bewusst offen: Eigner-Playtest, Steam-Build.

## Offene Punkte

Keine.
