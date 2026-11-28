# Goal: cinderreach-loot-click-filter

health_goals: [G-DIABLO03]

## Objective
Beute wird nicht mehr beim Darüberlaufen aufgehoben, sondern per Linksklick auf das Item-Symbol oder sein Namensschild. Optional verlangt eine Einstellung zusätzlich eine gedrückte Loot-Taste. Ein Beutefilter blendet Rare-Schilder aus, und Goldhaufen am Boden gibt es nicht mehr.

## Ist-Befunde
- B1 `Loot/LootDrop.cs` und `Loot/GoldDrop.cs` heben per `OnTriggerEnter` beim Überlaufen auf; `LootDrop` ist schon `IUsable` (nur für Truhen, `UseRadius` 2,5 m).
- B2 `DiabloLootLabels.cs` zeigt rarity-farbige Schilder nur bei gedrückter Alt-Taste (`ShowWithoutAlt` für Tests/Shotcheck).
- B3 Es entstehen nur Rare (gelb) und Unique; `ItemRarity.Magic` (blau) wird nie erzeugt (CINDERREACH.md, Beute seit 2026-11-23).
- B4 Gold fließt über `GoldDrop.CollectLocal` → `Wallet.Add(Equipment.ApplyGoldFind(...))`; Händler, Schmied (50 Gold) und Aufträge brauchen Gold weiter.
- B5 Einstellungen liegen als statische Felder in `DiabloSettings.cs` (ShowFPS, ScreenShake, …).

## Entscheidungen
- D0 Modus: Anlegen + planen + umsetzen in der Reihenfolge loot-click-filter → inventory-cursor-item → bag-quest-upgrades → vendor-sell-all → town-npc-identity (Eigner, /setgoal 2026-11-28).
- D1 Gold wird beim Kill direkt gutgeschrieben, Gold Find wirkt weiter; keine Goldhaufen mehr (Q2).
- D2 Schilder immer sichtbar (abschaltbar per Einstellung); Linksklick auf Schild oder Item-Modell hebt auf (Laufweg zum Item wie beim Truhenklick). Einstellung „Nur mit Taste looten“ standardmäßig aus, Standardtaste Alt, frei belegbar, persistent (Q3).
- D3 Einstellung „Beutefilter: Alles / Nur Unique“. Ausgefilterte Items haben kein Schild, bleiben sichtbar und per Klick aufhebbar; Filter wirkt lokal je Spieler (Q4).
- D4 Koop: Aufheben bleibt host-autoritativ über `LootNetRelay`; Klick ersetzt nur den Auslöser.

## Erfolgskriterien
1. Überlaufen hebt nichts auf; Linksklick auf Schild oder Modell hebt auf — Nachweis: PlayMode-Test (Held läuft über Drop → Tasche unverändert; Klick → Item in Tasche).
2. Mit aktiver Einstellung „Nur mit Taste looten“ hebt ein Klick ohne Taste nichts auf, mit Taste schon; Taste umbelegbar und gespeichert — Nachweis: PlayMode-/EditMode-Test der Einstellung inkl. Persistenz.
3. Beutefilter „Nur Unique“ blendet Rare-Schilder aus, Unique-Schilder bleiben — Nachweis: PlayMode-Test zählt Schilder je Filterstufe.
4. Gegner hinterlassen keine Goldhaufen, das Wallet steigt beim Kill um den Goldbetrag inkl. Gold Find, auch im Koop für den Besitzer — Nachweis: Tests (kein `GoldDrop` in der Szene nach Kill, Wallet-Delta).
5. Voll-Suiten grün und Windows-Shotcheck zeigt Schilder und Einstellungsseite — Nachweis: `bash scripts/run-unity-tests.sh` (EditMode + PlayMode failed 0), Shots unter `docs/loot-click/`.

## Nicht-Ziele
- Keine Rückkehr blauer (Magic) Items.
- Keine Änderung der Droptabellen oder Raritätsgewichte.

## Abhängigkeiten
- Keine.
