---
title: "Cinderreach Itemisierung und Rollen-Uniques"
domains: [unity, gameplay, ui, networking]
status: planned
goal: cinderreach-itemization
slug: cinderreach-itemization
branch: feature/cinderreach-itemization
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
---

# cinderreach-itemization — Implementation Plan

[Ziel und Erfolgskriterien](../../goals/cinderreach-itemization/GOAL.md). Planbasis `1842d1f`. Befehle laufen im Unity-Sandbox-Worktree. Vor Umsetzung den Plan mit aktuellem `origin/main` abgleichen, besonders Kampf-APIs, Klassen und Save-Version. Party Roles S1 ist bereits enthalten. Die Rollenfähigkeiten und ihre Koop-Validierung sind Abhängigkeiten für S3/S4 und die abschließende Abnahme. S1/S2 und die Darstellung können vorher beginnen.

## File Structure

Pfade in den Stufen sind relativ zu `sandbox/Assets/`. Für neue Assets und Ordner die Unity-Metadaten mitführen. Der Repo-Vertrag enthält keinen S1-Zeilenratchet. Umfangreiche Logik in die benannten neuen Komponenten auslagern und große bestehende Dateien an ihren Anschlussstellen ändern.

| Stufe | Zuständigkeit und Dateipfade |
| --- | --- |
| S1 | `Scripts/Items/{ItemDefinition,AffixLibrary,AffixDef,ItemDatabase}.cs`, neu `Scripts/Items/ItemLevelRules.cs`, `Scripts/Loot/{LootDrop,LootNetRelay}.cs`, `Scripts/EnemyHealth.cs`, `Scripts/Town/VendorStock.cs`, bestehende Schmiede-Produzenten und Belohnungsquellen, `Scripts/Save/{SaveGame,SaveMigrations,SaveService}.cs`. Testanschlüsse `Tests/EditMode/{AffixTableTests,SaveMigrationTests,SmithingServiceTests}.cs`. |
| S2 | `Scripts/Loot/{LootDrop,LootNetRelay}.cs`, `Scripts/Items/{Inventory,Equipment}.cs`, `Scripts/{DiabloNetworkPlayer,DiabloNetworkBootstrap}.cs`, neue Besitzkomponente `Scripts/Net/NetworkItemOwnership.cs` und `Resources/NetworkLootDrop.prefab`. Neu `Tests/PlayMode/NetworkItemLifecycleTests.cs`. |
| S3 | `Scripts/Items/Equipment.cs`, `Scripts/{DiabloSkills,DiabloMelee,DiabloProjectile,DiabloJuice,EnemyHealth,DiabloNetworkPlayer}.cs`, `Scripts/Net/HitValidator.cs`. Neu `Tests/PlayMode/ItemCombatStatsTests.cs`. |
| S4 | `Scripts/Items/{ItemDatabase,Equipment,BuildUniqueEffects}.cs`, `Scripts/Loot/{LootDrop,DroptableDatabase}.cs`, Skill- und Rollenanschlüsse aus Party Roles. `Tests/EditMode/BuildUniqueLootTests.cs`, `Tests/PlayMode/BuildUniqueEffectTests.cs`. |
| S5 | `Scripts/Loot/LootDrop.cs`, neu `Scripts/Loot/LootDropView.cs` und `Scripts/Items/ItemDetailsView.cs`, bestehende `DiabloMenuArt`, `Scripts/DiabloInventoryUI.cs`, `Scripts/Town/DiabloVendorUI.cs`, Item-Art-Assets. `Tests/EditMode/DiabloMenuTests.cs`, `Tests/PlayMode/VendorLoopTests.cs`, neu `Tests/PlayMode/ItemPresentationTests.cs`. |
| S6 | Bestehende Koop-Autoplay- und Build-Harnesses für Evidenz nutzen. Ergebnisse und Build-Revision im Goal-Fortschritt verlinken. |

## S1 · Dropquellen, Itemlevel und Spielstände

- `ItemInstance` um persistierte `int itemLevel` und `string instanceId` erweitern. `itemLevel == 0` steht für historische Funde oder ungewürfelte Startausrüstung. Unique-Identität bleibt `uniqueId`, die Instanzidentität gehört dem konkreten Exemplar. Bestehende Affix-Keys und Werte serialisieren wie bisher.
- `ItemLevelRules.GetBracket(int itemLevel)` und `AffixLibrary.GetRange(string key, int itemLevel, int affixTier)` als gemeinsame Regeln für Roll, Schmiede, Unique-Werte und Anzeige. Initiale Brackets 1–4, 5–9, 10+ als Tuningvorschlag. Pro Affix, Bracket und Normal-/Kindled-Tier die auflösbaren Grenzen, Rundung und erlaubten Slots festlegen. Bestehende Normal-/Kindled-Spannen als Kalibrierung verwenden und `affixTier` als eingefrorene Herkunft behalten. Die höchsten Unique-Rolls liegen am oberen Ende des passenden Bereichs.
- Die konkrete Quellstufe aus Zonen-/Encounter-Konfiguration durch `LootDrop.SpawnLoot` und `BuildItem` übergeben. Gegner, Elite, Truhe und feste Belohnungen explizit zuordnen. Händlerware erhält eine eigene konfigurierte Quellstufe. Dieselbe Quelle liefert bei gleichem Seed dieselben Rolls unabhängig von der Heldenstufe. Die Schmiede zieht Ersatzwerte aus dem gespeicherten Bracket und Tier.
- Auf aktuellem main die nächste Save-Version vergeben. Historische Gegenstände in Ausrüstung, Tasche, Lager und gespeicherten Belohnungen einmalig mit stabilen neuen Instanz-IDs versehen. Numerische Werte, Unique-ID und Schmiedebindung bleiben erhalten. Historische Bracket-0-Reforges nutzen ihre bisherigen Tier-Spannen.
- Bracket-Grenzen 4/5 und 9/10, Min/Max, Slotfilter, Quellunabhängigkeit, Migration und Roundtrip prüfen. `PLATFORMS=EditMode bash scripts/run-unity-tests.sh` vor der Umsetzung ausführen (expected: FAIL in den neuen Assertions), danach Exit 0.

## S2 · Autoritative Gegenstandsbewegungen im Koop

- Ein vollständiger, serialisierbarer Gegenstandssnapshot transportiert Instanz-ID, Itemlevel, Basis, Rarität, Unique-ID, Affixe und Schmiedebindung. `LootDrop` verwendet ein registriertes Pickup-Prefab. `DiabloNetworkBootstrap.EnsureZoneNetcode` registriert es vor Sitzungsstart. Der Host setzt den Snapshot vor Spawn. Clients und später Beitretende lesen dieselben Daten. Das Netzobjekt und der Trigger bleiben von der Darstellung getrennt.
- Hostseitigen Besitz für die betroffenen Inventar-, Ausrüstungs- und Lageraktionen führen. Bei Save-Import Datensatz und Besitz einmal beim Beitritt validieren, daraus Host-Zustand initialisieren und alle nachfolgenden Mutationen serverseitig anwenden. Client-Anfragen referenzieren die Instanz-ID. Eigentümer, Reichweite, Zielslot, Kapazität und Händlerpreis kommen aus Host-Zustand. Bestehende lokale Saves behalten ihren Importpfad.
- Einen Drop erst nach erfolgreichem Besitztransfer entfernen. Bei Verkauf Gold und Gegenstand gemeinsam aktualisieren. Beim Wegwerfen dieselbe Instanz spawnen, die Tasche erst bei erfolgreicher Übergabe leeren und sofortiges Rückaufheben durch Spawn-Offset oder erneuten Triggereintritt verhindern. Veraltete, doppelte und fremde Anfragen werden ohne Mutation abgewiesen.
- `PLATFORMS=PlayMode bash scripts/run-unity-tests.sh` muss voller Tasche, gleichzeitigen Pickup, Late Join, stale Verkauf und Wegwerfen/Wiederaufheben mit gleicher Instanz und gleichen Rolls abdecken.

## S3 · Krit-Wertung und erhöhte Fläche

- Legacy-`Crit` als Prozentpunkte erhalten. Starttuning für `Equipment.GetCrit()` sind die vorhandenen 5 % Basis, Legacy-Prozentpunkte und ein Prozentpunkt pro zehn Punkten neuer `CritRating`, gedeckelt bei 75 %. Das vorhandene 2×-Krit beibehalten. Je direktem Schadenstreffer einmal würfeln. Periodische Ticks, Dornenschaden und Heilung verwenden ihr normales Ergebnis.
- `Equipment.ScaleArea(float baseRadius)` wendet `baseRadius × sqrt(1 + increasedArea / 100)` nach Rang- und Variantenskalierung an. Als Startwert die erhöhte Fläche bei 100 % deckeln. Tooltip nennt Flächenprozent. Trefferraum und Effektgrafik erhalten denselben skalierten Radius. Cleave, Halo, Feuerball-Explosion sowie ausgewiesene Flächenheilung und Tank-Kontrolle integrieren. Einzelziel-, Projektilflug- und Bewegungsreichweiten behalten ihre Regeln.
- Den bestehenden Koop-Pfad für betroffene Treffer auf validierte Angriffsabsicht führen. Gültige Skill-IDs über den Klassen- und Skillvertrag prüfen. Der Host bestimmt aus S2-Ausrüstung und autoritativem Skillzustand Radius, Ziele, Crit und Schaden, prüft Sender, Klasse, zulässige Ausrichtung und Cooldown und repliziert das Ergebnis einschließlich Krit-Flag. Ein vom Client gesendeter Schadenswert dient dabei nicht als Eingabe. Legitimen serverberechneten Krit getrennt vom bisherigen Eingabelimit behandeln.
- `PLATFORMS=PlayMode bash scripts/run-unity-tests.sh` prüft feste RNG-Schwellen, einen Treffer pro gültigem Ziel, Ziele direkt innen/außen, Heil-/Kontrollflächen, unveränderte Einzelziel- und Ticks sowie abgewiesenen Cooldown- oder Radius-Missbrauch.

## S4 · Neun Rollen-Uniques

- Eine explizite Matrix `(PartyRole, ItemSlot) → uniqueId` in `ItemDatabase` und den regulären Unique-Pool pro Slot verwenden. Pro ID eine regulär erreichbare Quelle nachweisen. Unbekannte Unique-IDs in der Host-Validierung explizit ablehnen. Die aggregierte Seltenheitswahrscheinlichkeit erhalten oder eine Abweichung anhand der Erreichbarkeit begründen. Matrix-IDs können unabhängig von der Klasse ausgerüstet werden. Bestehende Einschränkungen anderer Waffen bleiben wirksam.
- Die folgende Matrix ist ein Implementierungsvorschlag. IDs bestehender Waffen erhalten. Text und Laufzeitwirkung über dieselbe Unique-ID auflösen. Zustände und Auslöseschutz gehören dem jeweiligen Träger oder Cast.

| Rolle | Waffe | Rüstung | Helm |
| --- | --- | --- | --- |
| Fenwalker | Lancer Ward unterbricht mit Cleave einen Lancer-Ansturm | Frontalblock erzeugt Bedrohung beim Angreifer | Tank-Stun kontrolliert das Primärziel länger |
| Lanternbearer | Wick of the Halo lenkt Wisps auf Ziele im eigenen Halo | Zielheilung verleiht einen kurzen Schadensschild | Gruppenheilung gibt dem schwächsten gültigen Verbündeten einen zusätzlichen Heilimpuls |
| Rogue | Fernkampfspezialangriff durchdringt ein weiteres Ziel | Nahkampfspezialtreffer gewährt kurz Lauftempo | Der erste kritische Spezialtreffer eines Casts reduziert dessen verbleibende Abklingzeit |

- Pro Rolle und Slot den Drop-Pfad, die höchste Wertestufe, freie Tragbarkeit, Beschreibung, Save/Lager-Roundtrip und die bestehende Schmiede-Regel belegen. Pro Effekt Ausrüsten, Ablegen, Reload und passenden Trigger sowie zwei Träger ohne Zustandsübertragung testen. Für zeitlich begrenzte Effekte die Dauer und Rücksetzregeln beim Spieltest festhalten. `bash scripts/run-unity-tests.sh` erwartet Exit 0.

## S5 · Gegenstandsgrafik, Inventar und Händler

- Eine explizite Art-Zuordnung nach Basis und Unique-ID an `DiabloMenuArt.ItemIcon(ItemInstance item, int size = 64)` anschließen. Asset-Abdeckung für jede auslieferbare Basis und alle Matrix-Uniques prüfen. `LootDropView` zeigt dieses Sprite auf dem Boden und legt den Raritätseffekt separat darüber oder darunter. Magic ist blau, Rare gelb, Unique orange. Host, Client und Late Join leiten beides aus S2 ab.
- `ItemDetailsView` aus Inventar und Händler wiederverwenden. Name, Icon, Rarität und Slot zuerst zeigen, dann Itemlevel, formatierte Rolls, Vergleichsdeltas je Affix und den Unique-Effekt. Bracket 0 als Altbestand kenntlich machen. Boolesche Unique-Effekte in Klartext darstellen. Leere Slots und untragbare Items erklären den Zustand über die jeweilige Aktion.
- Ausrüsten, Verkauf beim Händler und Wegwerfen über sichtbare, getrennte Aktionen anbieten. UI wählt über Instanz-ID, rendert den bestätigten Zustand neu und zeigt fehlende Kapazität oder eine abgewiesene Aktion an. Screenshots im laufenden Player bei 960×540 und 1920×1080 aufnehmen. Kontrolliere lesbare Details, positive und negative Deltas, die drei Aktionen sowie erkennbare Originalgrafik und Leuchten für alle Slots und Raritäten.
- `PLATFORMS=PlayMode bash scripts/run-unity-tests.sh` verifiziert unterschiedliche Grafiken, Raritätsfarben, Statusänderungen durch alle drei Aktionen und den erneuten Pickup eines weggeworfenen Items.

## S6 · Integration und Abnahme

1. `bash scripts/run-unity-tests.sh`, `python3 scripts/check-meta.py`, `python3 ~/scripts/agent-workspace.py check` und `python3 ~/scripts/agent-workspace.py verify` erwarten Exit 0. Den registrierten Loot-Prefab- und Item-Art-Vertrag in der Szenenverifikation absichern.
2. `bash scripts/run-autoplay.sh artifacts/autoplay/cinderreach-itemization-normal -seed 0` und `bash scripts/run-autoplay.sh artifacts/autoplay/cinderreach-itemization-kindled -seed 0 -difficulty kindled` erwarten vollständige Erfolgsberichte und ein grünes FPS-Gate. Den Rollen-Effektvergleich einschließlich Rogue auf demselben Build belegen.
3. `bash scripts/run-coop-pair.sh artifacts/coop/cinderreach-itemization` erwartet Exit 0 und zwei Erfolgsberichte. Den Lauf um einen deterministischen Loot-Nachweis ergänzen, der Host/Client-Snapshot und Art-Key, autoritativen Grant, Wegwerfen/Wiederaufheben und einen Host-validierten Crit-/AoE-Fall prüft. Bild- und Testbelege mit Revision im Goal-Fortschritt verlinken.
4. `node ~/scripts/goal-lint.mjs --repo "$PWD"` und `python3 ~/scripts/plan-goal-gate.py .agents/plans/cinderreach-itemization/tasks.md` erwarten Exit 0. Nach Landung die realen Befunde zu G-DIABLO02, G-DIABLO03 und G-DIABLO06 aus der Health-Auswertung erfassen. Fortschritt erst nach den messbaren Nachweisen auf 100 % setzen.
