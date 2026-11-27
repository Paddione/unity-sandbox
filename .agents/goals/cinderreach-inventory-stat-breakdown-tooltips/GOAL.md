---
goal: cinderreach-inventory-stat-breakdown-tooltips
slug: cinderreach-inventory-stat-breakdown-tooltips
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
---

# Goal: cinderreach-inventory-stat-breakdown-tooltips

## Objective

Den Inventar-Bildschirm von Cinderreach (`sandbox/Assets/Scripts/DiabloInventoryUI.cs` / `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs`) um eine vollständige, live berechnete Charakterparameter-Liste mit Berechnungs-Aufschlüsselung erweitern und ein durchgängiges Hover-Tooltip-System für Parameter, Skills und Items bereitstellen. Spieler sehen ohne Tab-Wechsel zu jedem Parameter die echten Live-Quellen (Basis, Charakterlevel, Attribut-Skalierung inkl. Klassen-/Spezialfaktor, Ausrüstungsbeiträge je Slot, Unique-Modifikatoren, aktive Caps) sowie detaillierte Hover-Tooltips auf Parametern, Skills (Charakterblatt `C` und Skillbar-HUD) und Items. Ausgangslage: [CINDERREACH.md](../CINDERREACH.md).

## Erfolgskriterien

1. **Pure Stat-Breakdown-SSOT**: Eine headless-testbare C#-Modellschicht (ohne `MonoBehaviour`-Abhängigkeit für die reine Berechnung) berechnet aus `Equipment`, `DiabloProgression`, `CharacterSheet`, `ClassDefinition`, `DiabloSkills` und `HealingFlask` alle Charakterparameter mit aufgeschlüsselten Termen (Basiswert, Level-Anteil, Attribut × Faktor, Gear-Beiträge pro Slot inkl. Itemnamen, Unique-Modifikatoren, Cap-Zustand und Endwert). Alle 17 Affixe aus `sandbox/Assets/Scripts/Items/AffixLibrary.cs` sind im Breakdown abgedeckt.
2. **Vollständige Live-Formeltreue (rein lesend)**:
   - Leben (`MaxHP`): Basis `20` + Level `(Level - 1) × 3` + Vitalität `vitality × 5` + Gear-`MaxHP` (nicht-Waffe) je Slot.
   - Nahkampfschaden: Basis `1` + Waffen-`Damage` + Waffen-`FireDamage` + Stärke `strength × 1` (exakt wie `DiabloMelee.CurrentDamage`).
   - Skill-Schaden / -Heilung: Klassen-Fokusfaktor (`ClassDefinition.focusSkillFactor`), Fokus-Multiplikator (`+8 % × focus × factor`), Rang-Multiplikator (`+20 %` je effektivem Rang aus vergebenen Rängen + Gear-`SkillRank`), Spezialitätsfaktor (`ClassDefinition.specialtyFactor`) und Unique-Heilfaktor (`GearUniqueEffects.HealMultiplier`).
   - Abklingzeit-Reduktion (CDR): multiplikative Kombination aus Fokus (`CharacterSheet.CooldownMultiplier()`, min. `0,5`) und Gear (`Equipment.CooldownFactor()`, Cap `40 %` bzw. `50 %` mit `EmberwreathCrown`).
   - Kritische Treffer: `5 %` Basis + Gear-`Crit` + Gear-`CritRating / 10` (Cap `75 %`) sowie Krit-Multiplikator (`2,0×` bzw. `2,5×` mit `GlassthornBow`).
   - Weitere Parameter: Bewegungsgeschwindigkeit (Basis `5,0` + Gear + aktiver Haste-Status), Flächeneffekt (`IncreasedArea`, Cap `100 %`, Radiusfaktor `sqrt(1 + inc/100)`), Lebensraub (Cap `25 %`), Dornen (inkl. `KilnheartMail` ×2), Goldfund, Magiefund (inkl. `ProspectorsHood` +50 % Goldfund), XP-Multiplikator, Heilflasche (Heilmenge, `FlaskHealing`-Gürtelbonus, Ladungen, Abklingzeit) sowie `Armor` und `AttackSpeed` ehrlich als Rohwert ausgewiesen (mit Hinweis, falls aktuell ohne Kampfformel).
3. **3-Spalten-Inventar & Stats-Tab-Parität**: Der Inventar-Tab (`MenuTab.Inventory`) zeigt drei Spalten nebeneinander: Paperdoll (links), scrollbare Parameterliste nach Kategorien (Mitte) und Tasche (rechts). Jedes An-/Ablegen aktualisiert die Parameterliste sofort. Der bestehende `Stats`-Tab (`MenuTab.Stats`) speist sich aus derselben Breakdown-Quelle statt aus einem getrennten Textblock. Alle bestehenden öffentlichen Schnittstellen und Hierarchienamen von `DiabloInventoryUI` (`MenuTab`, `IsOpen`, `CurrentTab`, `Paperdoll`, `SetOpen`, `ShowTab`, `EquipIndex`, `EquipById`, `DiscardById`, `UnequipSlot`, `DiscardIndex`, `DropBagOnSlot`, `DropSlotOnBag`, `RightClickBag`, `RightClickSlot`, `PaperdollSlotPosition`, `Notice`, `CompareSummary`, `DifficultyText`, `AreaLine`, `InventoryCanvas`, `BagGrid`, `BagButton{i}`, `BagEmpty`) bleiben unverändert.
4. **Detaillierte Hover-Tooltips (Parameter, Skills, Items)**:
   - **Parameter-Hover** (im Inventar- und Stats-Tab): zeigt Formel, Term-für-Term-Aufschlüsselung (Basis, Level, Attribut × Multiplikator, Beitrag je ausgerüstetem Slot, Unique-Effekte, Cap-Limit) und Endwert.
   - **Skill-Hover** (in `sandbox/Assets/Scripts/Progression/DiabloCharacterUI.cs` und in der HUD-Skillbar `SkillBarHud` aus `sandbox/Assets/Scripts/SkillBarHud.cs`): zeigt Skill-Beschreibung, aktive Variante, Basiswert, Rang + Gear-`SkillRank`-Bonus, Fokus × Klassen-/Spezialfaktor, effektiven Schaden bzw. Heilung, effektive Abklingzeit (Basis × Fokus × Gear) und skalierte Fläche.
   - **Item-Hover** (`sandbox/Assets/Scripts/Items/ItemTooltip.cs`): erweitert die bestehende Karte (inkl. Shift-Vergleich in Inventar, Truhe, Händler und Schmied) um Roll-Spannen (`Min–Max` je Itemlevel/Tier aus `AffixLibrary.GetRange`), klare Klassen-/Slot-Hinweise und Cap-Hinweise bei gedeckelten Affixen.
   - Alle Texte sind vollständig über `Loc` lokalisiert (Deutsch als Standard, Englisch unter `-lang en`; `IdentityTests` bleibt grün).
5. **Automatisierte Tests & Sichtprüfung**:
   - EditMode-Tests decken jede Parameter-Breakdown-Formel (Basis, Level, Attribute, Klassenfaktoren, Slot-Beiträge, Uniques, Caps), die Vollständigkeit aller 17 `AffixLibrary`-Einträge im Breakdown sowie die Formatierung von Parameter-, Skill- und erweiterten Item-Tooltips ab.
   - PlayMode-Tests decken 3-Spalten-Layout ohne Überlappung bei 1920×1080 (`DiabloHdTests`), Live-Aktualisierung beim Equip/Unequip, Parameter-Hover, Skill-Hover (Charakterblatt + Skillbar-HUD) und Item-Hover inkl. Shift-Vergleich ab.
   - Vollständige Suiten (`bash scripts/run-unity-tests.sh`), `python3 scripts/check-meta.py` und Windows-GPU-Shotcheck (`scripts/shotcheck-windows.sh`) mit Screenshots von 3-Spalten-Inventar, Parameter-Tooltip, Skill-Tooltip und erweitertem Item-Tooltip sind grün.
6. **Health-Gates**: `G-DIABLO01`, `G-DIABLO02`, `G-DIABLO03` und `G-DIABLO06` grün, Session-Autoplay ohne Regression.

## Scope und Nicht-Ziele

- **Scope**:
  - Neue Breakdown-/Tooltip-Logik und gemeinsamer Floating-Tooltip-Renderer unter `sandbox/Assets/Scripts/` (inkl. `.meta`).
  - `sandbox/Assets/Scripts/DiabloInventoryUI.cs` und `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` (3-Spalten-Inventar, Parameterliste, `Stats`-Tab-Anbindung).
  - `sandbox/Assets/Scripts/Items/ItemTooltip.cs` (Roll-Spannen, Klassen-/Cap-Hinweise).
  - `sandbox/Assets/Scripts/Progression/DiabloCharacterUI.cs` und `sandbox/Assets/Scripts/SkillBarHud.cs` (Skill-Hover-Tooltips).
  - Lokalisierung (`Loc`), EditMode-/PlayMode-Tests unter `sandbox/Assets/Tests/` und Shotcheck-Szenarien in `sandbox/Assets/Scripts/DiabloShotCheck.cs`.
- **Nicht-Ziele**:
  - Keine Änderungen an Kampf-, Schadens- oder Balancing-Formeln (`ClassDefinition.strengthMeleeFactor`, `AffixLibrary.Armor` und `AffixLibrary.AttackSpeed` werden nicht nachträglich in die Kampfmechanik verdrahtet).
  - Keine neuen Items, Affixe, Slots, Skills, Save-Migrationen oder Netcode-Protokolländerungen.
  - Keine Änderung an Drag&Drop-, Swap- oder Server-Autoritätslogik (`DiabloNetworkPlayer.Items.cs`).

## Abhängigkeiten

- `unity-sandbox main` (Stand [CINDERREACH.md](../CINDERREACH.md), nach `cinderreach-inventory-ui-layout-split`).
- Health-Vertrag: `health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]`.
