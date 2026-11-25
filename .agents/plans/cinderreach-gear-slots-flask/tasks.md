---
title: "Cinderreach Handschuhe/Stiefel/Gürtel, Heilflasche, Unique-Ausbau, Dropbalance"
domains: [unity, gameplay, ui, networking]
status: done
goal: cinderreach-gear-slots-flask
slug: cinderreach-gear-slots-flask
branch: feature/cinderreach-gear-slots-flask
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
---

# cinderreach-gear-slots-flask — Implementation Plan

[Ziel und Erfolgskriterien](../../goals/cinderreach-gear-slots-flask/GOAL.md). Planbasis `b2a2006`. Pfade relativ zu `sandbox/Assets/`.

| Stufe | Zuständigkeit und Dateipfade | Prüfung |
| --- | --- | --- |
| S1 Slots + Save v15 | `Scripts/Items/{ItemDefinition,Equipment,ItemDatabase,AffixLibrary}.cs`, `Scripts/DiabloInventoryUI.cs`, `Scripts/{DiabloNetworkPlayer,DiabloNetworkPlayer.Items}.cs`, `Scripts/Net/NetworkItemOwnership.cs`, `Scripts/Save/{SaveGame,SaveMigrations,SaveService}.cs` | `GearSlotsTests` (Enum-Vertrag, Equip, Pools, Validierung, v14→v15, Roundtrip), `SaveMigrationTests` |
| S2 Heilflasche | neu `Scripts/Items/HealingFlask.cs`, `Scripts/DiabloNetworkPlayer.Flask.cs`, `Scripts/DiabloLifeGlobe.cs`, Trank-Rückbau in `Town/{Vendor,VendorStock,DiabloVendorUI,DiabloStashUI}.cs`, `SessionAutoplay.cs` | `GearSlotsTests.Flask_*`, `GearSlotsPlayTests.FlaskHud_*`, `NetworkItemLifecycleTests.ClientFlaskDrink_*` |
| S3 3D-Gear | `Scripts/Classes/{HeroGearModels,HeroEquipmentAppearance}.cs` (Bone-Space-Mounts an `wrist`, `lowerleg`, `foot`, `hips`) | `GearSlotsPlayTests.LimbGear_*`, `HeroEquipmentAppearanceTests` |
| S4/S5 Uniques | `Scripts/Items/{ItemDatabase,GearUniqueEffects,BuildUniqueEffects}.cs`, Hooks in `PlayerHealth`, `EnemyHealth`, `DiabloJuice`, `DiabloSkills`, Loc `Item`/`Unique`, `DiabloMenuArt` | `GearSlotsTests.Uniques_*`, `GearSlotsPlayTests.{TimedEffects,KillAndCritEffects}_*` |
| S6 Drops | `Scripts/Loot/{LootTable,LootDrop,DroptableDatabase}.cs` (Slotgewichte, `UniquePity`) | `GearSlotsTests.{SlotWeights,Simulation,Pity}_*` |
| S7 Abnahme | Voll-Suiten, `check-meta.py`, Autoplay normal/kindled, Koop-Paar, Windows-Shotcheck | Belege in `PROGRESS.md` |
