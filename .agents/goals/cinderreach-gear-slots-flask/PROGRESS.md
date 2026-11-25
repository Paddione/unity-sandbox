# Progress – cinderreach-gear-slots-flask

Prozent: 100 %

## Erledigt

- Setgoal-Entscheidungen: Flasche statt Kauftrank, Slots mit 3D-Modell, 3 Uniques je Slot, Raritätsgewichte fix mit Slotgewichten und Pity, Health `G-DIABLO01/02/03/06`. Der [Plan](../../plans/cinderreach-gear-slots-flask/tasks.md) ist verlinkt.
- K1 Slots: `ItemSlot` Gloves=4, Boots=5, Belt=6, die alten Werte bleiben stabil. `Equipment` mit sechs Plätzen (`GearSlots`, `GearTotal`, `Wears`), je 3 Basen, Affix-Pools (17 Affixe, neu `FlaskHealing` nur auf Gürteln), Paperdoll in zwei Spalten, Drag/Rechtsklick/Vergleich und Schmiede über die gemeinsamen Pfade. Belege `GearSlotsTests.{SlotEnum,NewSlots_*,HostValidation}`, `GearSlotsPlayTests.Paperdoll_*`.
- K2 3D: Bone-Space-Modelle an `wrist`, `lowerleg`/`foot`, `hips` für alle 9 Basen und 9 Rollen-Uniques. Netz-Keys `visualGloves/Boots/Belt`. Zusätzlich Modelle für die 9 neuen Waffen-, Rüstungs- und Helm-Uniques. Belege `GearSlotsPlayTests.LimbGear_*` (alle Klassen, Anlegen und Ablegen), `HeroEquipmentAppearanceTests`, Windows-Shots `gearset_<klasse>_{front,back,paperdoll}`.
- K3 Flasche: `HealingFlask` auf Taste 1, 40 % max. HP in 10 Ticks über 5 s, Schaden bricht nicht ab, 60 s Cooldown, 1 Ladung, Refill in der Lantern Hold (`SkillVariantCatalog.IsInLanternHold`) und an Risk-Shrines. HUD mit Sweep und Restsekunden. Koop: Der Host gewährt, der Besitzer tickt. Belege `GearSlotsTests.Flask_*`, `GearSlotsPlayTests.FlaskHud_*`, `NetworkItemLifecycleTests.ClientFlaskDrink_IsGrantedByTheHostAndHealsTheOwner`.
- K4 Kauftrank entfernt (`BuyPotion`, Potion-Zeile, `CreateHealingPotion`, `Vendor.DrinkPotion`). Autoplay-Phase `flask` trinkt und läuft zum Refill in die Hold. Händler- und Art-Tests sind angepasst.
- K5 Save v15: neue Plätze, `uniquePity`, Trankstapel werden entfernt und mit 2 Gold je Trank erstattet (A3). Belege `GearSlotsTests.Migration_V14ToV15_*`, `Save_RoundtripsNewSlotsAndPity`, `SaveMigrationTests` (v1 → v15).
- K6 18 Uniques (`GearUniqueEffects`): Rollen-Matrix für Handschuhe, Stiefel und Gürtel sowie je 3 Zonenstücke für Waffe (klassengebunden, ab Ilvl 5), Rüstung und Helm (Ilvl 1/5/10). Jedes hat Effekt-Text, Namen, Art-Key `unique:<id>`, Top-Bracket-Rolls und Host-Katalog. Belege `GearSlotsTests.Uniques_*` (Katalog, Zonen-Gate, Erreichbarkeit über den Drop-Pfad, Passiv-/Treffer-Effekte, Ironbound-Schild), `GearSlotsPlayTests.{TimedEffects,KillAndCritEffects}_*`.
- K7 Drops: Raritätsgewichte unverändert, Slotgewichte 25/20/15/15/15/10, Pity 40 (gespeichert, unabhängig von MF und Kindled), Zonenstück-Anteil 1/3 innerhalb des Slots. Die Simulation mit 10 000 Drops je Tier liegt bei Unique-Anteil und Slotverteilung innerhalb von ±1,5 pp. Der längste Lauf ohne Unique ist ≤ 40. Belege `GearSlotsTests.{SlotWeights,Simulation,Pity}_*`. Rechnerische Folge: Die effektive Unique-Quote normaler Gegner steigt durch Pity auf etwa 3,6 % (sonst 2 %).
- K8 Abnahme: EditMode 586/586, PlayMode 354/354 (Volllauf auf `710be00`), nach den letzten kosmetischen Änderungen erneut EditMode 586/586 und PlayMode-Filter 10/10. `check-meta.py` OK, `agent-workspace check` still. Autoplay unter Xvfb: normal und kindled (eigene Save-Files, `difficulty: Kindled`) jeweils exit 0, 10 Phasen inklusive `town` und `complete`, 0 Exceptions, Flasche `drank=True`, `refilled=True`. Koop-Paar fenwalker-Host + rogue-Client: beide exit 0, 0 Exceptions. Windows-Shotcheck 1920×1080 und 960×540: `flask_cooldown_hud` (sha16 `9548897b17f20a26`), `gear_slots_ground` (`961ae31096a8b90c`), `gearset_*` × 9 je Auflösung (z. B. Fenwalker-Paperdoll `39a7872381f59af3`). Ablage: `~/unity-sandbox/artifacts/gear-slots-flask/`.
- Health: G-DIABLO01 (`check-meta` OK), G-DIABLO02 (Szenen-/Art-Vertrag in EditMode grün), G-DIABLO03 (Suiten grün), G-DIABLO06 (Autoplay normal und kindled DONE, 0 Exceptions).
- Umgebungsbefund: Unter WSLg stürzt der Linux-Player in `driCreateNewScreen3` (Mesa) mit Exit 132 ab, bevor Spielcode läuft. Die Autoplay-Belege liefen deshalb mit `AUTOPLAY_DISPLAY=xvfb`. Fremdbefund: `NEW_HERO FAIL level=2` im Shotcheck tritt identisch mit dem installierten Main-Build auf, kein Regress dieses Ziels.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
