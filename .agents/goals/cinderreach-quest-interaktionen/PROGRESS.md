# Fortschritt: cinderreach-quest-interaktionen

Prozent: 100 %

## Erledigt

- Inventar aus den Spielinhalten (`scripts/check-quest-coverage.py`):
  14 Quests aus `StoryModel.cs` (h1–h9, n1–n5) + 24 Platzierungen aus
  `DiabloZone.unity` (5 StoryPickup-Einzelplatzierungen inkl. 3× Grabtafel,
  2 StoryNpc, 5 WaypointShrine, 6 ZoneGate, Vendor, Smith, StashChest,
  ContractBoard, CampfireHeal, RiskShrine). Jede Zeile verweist per
  `quest:`-/`placement:`-Marke auf einen ausführbaren Testfall.
- PlayMode: `QuestJourneyTests` (14/14) spielt jede Quest über den echten
  Auslöser (BookTalk/BookCollect/BookKill/BookEnter) bis zum sichtbaren
  Abschluss (Fuhrbuch-✓, Abgabe-Dialog); `WorldInteractionTests` (24/24)
  prüft je Platzierung Erreichbarkeit, richtiges Ziel und beobachtbare
  Wirkung. Volle PlayMode-Suite: 341/341 grün.
- EditMode: `QuestRuleTests` (26 Fälle) deckt jeden Verlauf, gesperrte
  Schritte (Voraussetzung, Akt-Gate, falsches Ziel), einmalige Belohnungen
  über den produktiven Vergabepfad und Save/Load-Wiederherstellung
  (ToSave/FromSave + produktiver SaveService-Dateipfad) ab. Volle
  EditMode-Suite: 565/565 grün.
- Health-Probe G-QUESTCOVER01 meldet null (`quest coverage: uncovered
  entries: 0`); Fixture-Suite (8 Fälle: neuer Inhalt, gelöschter Test,
  doppelte Kennung, fehlendes Inventar) grün; `check-meta.py` ok.
  Negativkontrollen belegt: fehlende n5-Zuordnung → `uncovered: quest:n5`;
  vertauschte Schrein-Zielbindung → zugehöriger PlayMode-Fall rot.
  Beide zurückgenommen, danach wieder grün.

## Fallzahlen je Akt (Stand Branch-HEAD)

- Akt I: 5 Hauptquests (h1–h5) + 3 Nebenquests (n1–n3); 20 Platzierungen
  (Vendor, Smith, StashChest, ContractBoard, CampfireHeal, RiskShrine,
  StoryNpc_ottilie, 5 StoryPickup, WaypointShrine_Town/Fen/Ossuary/Kiln,
  FenGate, OssuaryGate, KilnGate, KilnReturnGate).
- Akt II: 4 Hauptquests (h6–h9) + 2 Nebenquests (n4–n5); 4 Platzierungen
  (StoryNpc_merle, WaypointShrine_Barrens, BarrensGate, BarrensReturnGate).

## Offene Punkte

Keine.
