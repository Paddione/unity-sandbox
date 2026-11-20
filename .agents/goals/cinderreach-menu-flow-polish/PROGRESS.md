# Progress: cinderreach-menu-flow-polish

Prozent: 60 %

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

## Nächste Schritte

- Autoplay `-seed 0` mit FPS-Gate, landen.
- Iteration 2: Waypoint-Panel an Inhalt anpassen, Startkarten gleich hoch, weitere Shot-Befunde.

## Offene Punkte

- Iterationen laufen.
