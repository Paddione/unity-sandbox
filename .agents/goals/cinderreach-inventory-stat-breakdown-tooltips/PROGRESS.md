# Fortschritt – cinderreach-inventory-stat-breakdown-tooltips

Prozent: 100 %

## Erledigt

- 2026-11-27: Goal persistent angelegt (`GOAL.md` + `PROGRESS.md`, `health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]` ab 0 %) nach `/setgoal`-Klärungsrunde.
- 2026-11-27: Lokalisierungsdomäne `sandbox/Assets/Scripts/Localization/LocCatalog.StatBreakdown.cs` angelegt und in `LocCatalog.cs` registriert (vollständige DE/EN-Übersetzungen für Kategorien, Parameter, Formeln, Quellen, Caps, Skill-Breakdown und Item-Rollspannen).
- 2026-11-27: Pure C#-Modellschicht erstellt:
  - `sandbox/Assets/Scripts/Progression/CharacterStatBreakdown.cs`: Live-Aufschlüsselung für alle 21 Charakterparameter in 4 Kategorien (Attribute, Angriff, Verteidigung, Nützliches), Deckung aller 17 `AffixLibrary`-Einträge, Basis-/Level-/Attribut-/Slot-/Unique-/Cap-Terme und ehrlicher Rohwert-Ausweis für `Armor` und `AttackSpeed`.
  - `sandbox/Assets/Scripts/Progression/SkillBreakdown.cs`: Live-Aufschlüsselung je Skill (Beschreibung, aktive Variante, Basiswert + Waffenbonus, Rang + Gear-`SkillRank`, Fokus × Klassenfaktor, Spezialitätsfaktor, Unique-Heilbonus, effektive Abklingzeit und skalierte Wirkfläche).
- 2026-11-27: Tooltip- und UI-Verdrahtung umgesetzt:
  - `sandbox/Assets/Scripts/Menu/InfoTooltip.cs` + `HoverInfoSource.cs`: gemeinsamer schwebender Tooltip auf eigenem HD-Overlay-Canvas (`sortingOrder = 61`, ohne `GraphicRaycaster`) mit gegenseitigem Ausblenden zu `ItemTooltip`.
  - `sandbox/Assets/Scripts/Items/ItemTooltip.cs`: Roll-Spannen `[Min–Max]` je Itemlevel/Tier aus `AffixLibrary.GetRange`, Cap-Hinweise und Waffen-Leben-Hinweis (Shift-Vergleichsverträge unverändert).
  - `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` & `DiabloInventoryUI.cs`: 3-Spalten-Inventar (`CharacterColumn` | scrollbare `ParametersColumn` | `BagColumn`), sofortiges Refresh bei Equip/Unequip, Parität mit `MenuTab.Stats` (`StatsScroll` + `StatsDetail`), aktualisierte `SummaryBox` mit Live-Stärke im Nahkampfschaden und Fokus+Gear-CDR.
  - `sandbox/Assets/Scripts/Progression/DiabloCharacterUI.cs` & `sandbox/Assets/Scripts/SkillBarHud.cs`: Skill- und Attribut-Hover-Tooltips im Charakterblatt (`C`) und auf den HUD-Skillbar-Slots.
- 2026-11-27: Tests, Statische Checks & Windows-Sichtprüfung abgeschlossen:
  - `python3 scripts/check-hotpath-caching.py`: OK (3 Hot-Path-Methoden geprüft).
  - `python3 -m pytest scripts -q`: 34/34 bestanden.
  - `python3 scripts/check-meta.py`: OK (`G-DIABLO01`).
  - EditMode (gefiltert): 39/39 bestanden (`CharacterStatBreakdownTests`, `ItemCompareTests`, `DiabloMenuTests`, `DiabloHdTests`, `LocalizationTests`, `IdentityTests`).
  - EditMode (Vollauf): 695/695 bestanden (bei Lauf nach PlayMode-Save-Anlage auf `cinderreach-save.json` schlug ausschließlich das vorbestehende `GameSettingsTests.MainMenu_NewContinueValidation` an; alle anderen 694/694 grün).
  - PlayMode (Vollauf): **392/392 bestanden** (inkl. `InventoryStatBreakdownPlayTests`, `InventoryPaperdollPlayTests`, `DiabloMenuPlayTests`, `GearSlotsPlayTests`).
  - Windows-Build (`SandboxBuilder.BuildDiabloZoneWindows`) & WIP-Stage (`Games/cinderreach-wip`, `191 MB`) + Shotcheck (`-stat-breakdown-shots`, `1920×1080`, Exit 0): 4 Screenshots (`01_stat_breakdown_inventory.png`, `02_stat_breakdown_param_tip.png`, `03_stat_breakdown_item_tip.png`, `04_stat_breakdown_skill_tip.png`) visuell geprüft — 3 Spalten ohne Überlappung, lesbare Parameter-, Item- (mit Rollspannen und Shift-Pfeilen) und Skill-Tooltips.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
