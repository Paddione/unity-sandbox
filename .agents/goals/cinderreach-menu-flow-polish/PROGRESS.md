# Progress: cinderreach-menu-flow-polish

Prozent: 90 %

## Erledigt

- 2026-11-21: Branch `feature/cinderreach-menu-art-polish` auf `main` (`0619770`) rebased.
  Konflikte in 14 Dateien aufgelöst (Vendor-Koop-Handel + Notice + Klick-außerhalb im neuen
  Layout, Inventory-Equip/Discard-Aktionen + AreaLine, Skill-Varianten im Charakterblatt,
  Kindled-Finale-Zeile im Abschlussbanner, Melee-Rework ohne Strike-Step, Potion/Lantern-Icons
  von `main`).
- 2026-11-21: Iteration 1 (K1–K4): Layout-Wurzelfix `DiabloUIFactory.SetLayout` (Rect-Größe),
  `FitToBox` für Item-Karten, Charakterblatt per `ContentSizeFitter`; ein Startbildschirm
  (`MainMenu.ClassSelectOwnsStart`), Continue mit Spielstand-Info (`SaveService.TryPeek`),
  Solo-Pause friert Zeit ein, Todesschleier, „Press On“ nach dem Abbot, Varianten-Umschalter;
  Rogue-Portrait + 5 Skill-Icons (Blender), datengetriebene Klassenkarten, Rogue-Akzent,
  HUD-Bronzeplatte, `LootDrop.PrettyName`.
- Belege: EditMode 375/375, PlayMode 268/268 (`scripts/run-unity-tests.sh`),
  `python3 scripts/check-meta.py` OK, Windows-Shotcheck 46 Motive (staged nach
  `Games\cinderreach-wip`, installierte Version unberührt), u. a. `class_select`, `start_settings`,
  `vendor_open`, `bag_open`, `character_open`, `pause_menu`, `death_veil`.

- 2026-11-21: Perf-Regression aus dem Menü-Branch per A/B belegt und behoben (K5):
  `DiabloSettings.BootApply` erzwang 1080p-Vollbild + VSync und überstimmte `-screen-*`/Automation.
  Vorher worst 20,5 fps (Gate rot), `main` 34,6; nachher worst 37,3 fps, PASS, Akt I komplett,
  0 Exceptions (`run-autoplay.sh -seed 0`). Modal-Checks nur in Klick-Frames, `AnyOpen` statt Szenen-Scan.
- 2026-11-21: Iteration 2: Waypoint-Fenster zeigt gesperrte Schreine („not attuned“/„sealed“),
  „(Here)“ nicht klickbar; kompaktere Klassenkarten. EditMode 377/377, PlayMode 268/268,
  Shotcheck 46 Motive.

- 2026-11-21: Iteration 3: leere Ausrüstungsslots als echte Leerstellen (`Equipment.IsBlank`,
  kein Phantom-Item beim Ablegen), Startbild 1920×1080 mit allen drei Helden rechts der Karten,
  0,45-s-Ausblenden des Startbildschirms. EditMode 378/378, PlayMode 268/268, Autoplay `-seed 0`
  PASS worst 38,8 fps, Shotcheck 46 Motive.

## Nächste Schritte

- Landen, danach weitere Iterationen nach Shot-Befunden.

## Offene Punkte

- Iterationen laufen.
