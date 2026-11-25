# Cinderreach: Handschuhe/Stiefel/Gürtel, Heilflasche, Unique-Ausbau, Dropbalance

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-gear-slots-flask/tasks.md]

## Objective

Cinderreach bekommt drei neue Ausrüstungsslots (Handschuhe, Stiefel, Gürtel) mit sichtbarem 3D-Modell am Helden. Der gekaufte Heiltrank wird durch eine wiederauffüllbare Heilflasche auf Taste 1 ersetzt, die über 5 s heilt und 60 s Cooldown hat. Dazu kommen 18 neue Uniques, je 3 pro Slot über alle sechs Slots. Die Dropverteilung wird so ausbalanciert, dass die größere Vielfalt die Unique-Gesamtrate nicht verwässert.

## Erfolgskriterien

1. Slots: `ItemSlot` wird um `Gloves`, `Boots`, `Belt` erweitert, die bestehenden Enum-Werte bleiben stabil (Netz-/Save-Vertrag). `Equipment` Get/Set/Unequip, Paperdoll-Felder in `DiabloInventoryUI`, Drag & Drop, Rechtsklick-Tausch, Vergleich und `ItemDetailsView` funktionieren für alle sechs Slots. Jeder neue Slot hat eigene Basis-Items und einen Affix-Pool mit Itemlevel-Brackets. Die Schmiede gilt auch dort.
2. 3D: Handschuhe, Stiefel und Gürtel sind für alle drei Klassen am Helden sichtbar (`HeroGearModels`/`HeroEquipmentAppearance`). Sie wechseln bei Equip und Unequip, auch beim Koop-Client und bei Late Join. Shot-Beleg je Klasse mit allen sechs Slots belegt.
3. Heilflasche: Taste 1 heilt 40 % der max. HP (Annahme A1) über 5 s in gleichmäßigen Ticks, die nie kritisch sind. Schaden bricht nicht ab. 60 s Cooldown, 1 Ladung. Die Flasche füllt sich im Lantern Hold und an Risk-Shrines (Annahme A2) wieder auf. Das HUD zeigt Ladung und Cooldown-Sweep. Im Koop sind Intent, Tick und Cooldown host-autoritativ, Drücken im Cooldown wird abgelehnt.
4. Kauf-Trank entfernt: `VendorStock.BuyPotion`, der Potion-Eintrag beim Händler und `CreateHealingPotion` sind als Quelle abgebaut. `SessionAutoplay` nutzt eine Flaschen-Phase statt `vendor_potion`. Art-Coverage- und Händler-Tests sind angepasst.
5. Save v15: Die Migration erhält alle bestehenden Items, Rolls, Instanz-IDs und Unique-IDs. Neue Slots starten leer. Alte Trankstapel werden zum Verkaufspreis (25 %) in Gold erstattet (Annahme A3). Flaschenzustand wird nicht gespeichert, die Flasche startet voll. Der Roundtrip-Test v14 → v15 ist grün.
6. Uniques: 18 neue. Handschuhe, Stiefel und Gürtel bekommen je 3, eines pro Rolle (Fenwalker/Lanternbearer/Rogue) als Erweiterung der S4-Matrix `(PartyRole, Slot)`. Waffe, Rüstung und Helm bekommen je 3 zusätzliche mit Zonenbindung. Jedes Unique hat einen eigenen, im Spiel wirksamen Effekt, einen expliziten Art-Key (kein Unknown-Fallback), einen Eintrag in `IsKnownUniqueId` und Tests für Equip, Trigger, Ablegen und Reload. Rolls liegen an der Bracket-Obergrenze.
7. Drops: Die Raritätsgewichte in `LootTable.WeightsFor` bleiben unverändert. Slotgewichte Waffe 25 / Rüstung 20 / Helm 15 / Handschuhe 15 / Stiefel 15 / Gürtel 10. Unique-Pools sind nach Zone gegated, die 70-%-Klassenbindung bei Waffen bleibt. Pity: Nach 40 Rare-Drops in Folge ohne Unique ist der nächste Drop ein Unique. Der Zähler gilt pro Held, wird gespeichert und hängt nicht von Magic Find/Kindled ab. Eine seeded Simulation mit 10 000 Drops pro Tier hält Unique-Anteil und Slotverteilung innerhalb von ±1,5 Prozentpunkten. Kein Lauf ohne Unique ist länger als 40.
8. Abnahme: EditMode und PlayMode komplett grün, `check-meta.py` OK, `agent-workspace check` still. Autoplay `DONE` auf Normal und Kindled (je eigenes `-save-file`) mit 0 Exceptions. Koop-Paar Exit 0. Shots 960×540 und 1920×1080 für Paperdoll mit sechs Slots, Flaschen-HUD im Cooldown, Boden-Drops der neuen Slots und Held mit 3D-Gear.

## Nicht-Ziele

Keine Ringe, kein Amulett, keine weiteren Slots. Keine Änderung der Raritätsgewichte und keine Wiederbelebung von `ItemRarity.Magic`. Kein Umbau von Skills, Klassen oder Schwierigkeitsprofilen außer den Hooks, die Unique-Effekte brauchen. Kein Windows-Release-Deploy als Pflicht. Das Linux-30-fps-Gate gilt nur als Hinweis.

## Abhängigkeiten

- `cinderreach-itemization`, `cinderreach-inventory-paperdoll`, `cinderreach-equipment-art` (alle 100 %).
- Host-autoritativer Itempfad (`DiabloNetworkPlayer.Items.cs`, `NetworkItemOwnership`). Clients senden nur Intents.
- Annahmen A1–A3 sind Startwerte. Änderungen nach Playtest werden mit Beleg in `PROGRESS.md` festgehalten.
