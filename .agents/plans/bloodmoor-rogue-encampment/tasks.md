---
goal: unity-diablo-bloodmoor (neu, folgt unity-diablo-p2-coop-loot-zone 100 %; health_goals: none — einmaliger Prototyp wie Vorgänger)
slug: bloodmoor-rogue-encampment
branch: feature/bloodmoor-rogue-encampment
repo_main: main
tickets: none
request: Blood-Moor-Area mit Rogue Encampment als Startstadt, Enemies über das Moor verteilt (angreifbar/killbar/lootbar), Inventar für Gear + Equipped, Starter-Schwert für den Spieler
---

# Plan: Blood Moor + Rogue Encampment (Zone-Rework, Inventar, Starter-Schwert)

## Ausgangslage (Belege)

- `sandbox/Assets/Editor/SandboxBuilder.cs` — `BuildDiabloZone()` (Z. 593) baut „Ember Crypt Approach": Halle, 6 Husks + 1 Elite (`Husk_Elite`, 15 HP), Truhe `DChestZone`, GoalPad-Extraktion, 4 Coop-Spawnpunkte; Verify `VerifyDiabloZone()` (Z. 936) pinnt Counts.
- `sandbox/Assets/Scripts/DiabloClickController.cs` — WASD-Hybrid + Maus-Aim + Strike; Klicks gehen immer durch (`HandleClick`, Z. 222).
- `sandbox/Assets/Scripts/DiabloMelee.cs` — Arc-Strike (60°, 2,2 m), `FindArcTarget` über Tag `Husk`.
- `sandbox/Assets/Scripts/EnemyHealth.cs` — passiv (kein Gegenangriff), Tod → `LootDrop.SpawnLoot` nach Tier (`LootTable`: Normal 70 % Drop, Elite/Chest 100 %).
- `sandbox/Assets/Scripts/Items/Equipment.cs` — 3 Slots, Auto-Equip bei Pickup (`LootDrop.OnTriggerEnter`), kein Bag, kein UI.
- `sandbox/Assets/Scripts/ZoneEntryHint.cs` — Onboarding-Text „Ember Crypt Approach".
- Kein `Inventory`, kein `PlayerHealth`, keine Feind-AI (grep 0 Treffer). Prior-Art: kein `docs/adr/`, kein `tests/spec/` — P2-Muster (host-autoritative Drops via `LootNetRelay`, lokales Equip) wird **erweitert, nicht ersetzt**.
- Skill-Routing: `bundled:visual-coding` gelesen (kein Owner für Unity-Standalone), `bundled:browser-game` + `bundled:browser-app-delivery` gelesen und verworfen (reine Browser-Pipelines: Playwright/Sprites/localhost-Handoff — hier gelten Unity-Batchmode + `.exe`/Server-Handoff).

## Brainstorming-Entscheide (gelockt, User-Antworten 2026-11-15)

1. **Szene:** `DiabloZone` wird **in place** zum Blood Moor umgebaut (keine neue Szene; Ember Crypt entfällt ersatzlos). Süd = Rogue Encampment (Start), Mitte/Nord = offenes Moor, West-Alkoven + Truhe + Elite bleiben, Ost-Rubble bleibt, Nord-GoalPad wird „Moor Gate" (Extraktion).
2. **Inventar:** echtes UGUI-Panel, Taste `I`: 3 Equip-Slots + Bag-Grid (Cap 20), Klick rüstet aus (Swap zurück in den Bag). Datenklasse `Inventory` pur (EditMode-testbar), View `DiabloInventoryUI` baut Canvas zur Laufzeit (kein Prefab-Binary-Churn).
3. **Camp:** Props + Lagerfeuer-Heal (keine NPCs/Vendoren — Follow-up). Palisadenring mit Tor, Spawn im Camp, Campfire heilt +1 HP/s im Radius.
4. **Kampf:** Husks bleiben passiv (kein Gegner-Gegenangriff — kein Player-Schaden nötig, kein Death-Handling; als Follow-up notiert). „Angreifbar/killbar/lootbar" = bestehender Strike→Kill→Drop-Loop, jetzt über das Moor verteilt: 10 Normals + 2 Elites.
5. **Starter:** „Worn Short Sword" (Weapon, Magic, Damage +2, fix) — beim Spawn automatisch ausgerüstet (Solo-Held + Netz-Prefab).
6. **Multiplayer:** Inventar + HP strikt lokal pro Client (P2-Muster); Drops weiter host-autoritativ. Kein Inventar-Sync in diesem Cut.

## Partial-Manifest (disjunkte target_files)

| Partial | Inhalt | target_files (exklusiv) |
|---|---|---|
| P1 data | Bag-Daten + Starter-Schwert | `sandbox/Assets/Scripts/Items/Inventory.cs` (neu), `sandbox/Assets/Scripts/Items/ItemDatabase.cs`, `sandbox/Assets/Scripts/Items/Equipment.cs` |
| P2 ui | Inventar-UI + Klick-Guard | `sandbox/Assets/Scripts/DiabloInventoryUI.cs` (neu), `sandbox/Assets/Scripts/DiabloClickController.cs` |
| P3 zone | Blood-Moor-Umbau + Wiring | `sandbox/Assets/Editor/SandboxBuilder.cs`, `sandbox/Assets/Scenes/DiabloZone.unity`, `sandbox/Assets/Scenes/DiabloZone.unity.meta`, `sandbox/Assets/Scripts/ZoneEntryHint.cs` |
| P4 camp | HP + Lagerfeuer | `sandbox/Assets/Scripts/PlayerHealth.cs` (neu), `sandbox/Assets/Scripts/CampfireHeal.cs` (neu) |
| P5 proof | Tests + Docs | `sandbox/Assets/Tests/EditMode/DiabloBloodMoorTests.cs` (neu), `sandbox/Assets/Tests/PlayMode/DiabloBloodMoorLoopTests.cs` (neu), `docs/decision-log.md`, `docs/dev-journal.md`, `docs/mechanics-questions.md` |

Schnittstellen: P3 ruft `ItemDatabase.CreateStarterSword()` (P1), `Inventory` (P1), `DiabloInventoryUI` (P2), `PlayerHealth`/`CampfireHeal` (P4) — nur per `AddComponent`/Methodenaufruf, kein Builder-Code außerhalb P3. P5 testet alle. S1: kein `baseline.json` — neue Dateien ≤ ~150 Zeilen.

## P1 — Inventar-Daten + Starter-Schwert

- `Items/Inventory.cs` (neu): `List<ItemInstance> bag`, `capacity = 20`; `bool Add(item)` (voll → false); `bool EquipFromBag(int index, Equipment eq)` — equipped Item des Slots geht zurück in den Bag (Swap), Index-Validierung; `int Count`, `int FreeSlots`.
- `Items/ItemDatabase.cs`: `CreateStarterSword()` → Weapon/Magic/`"Worn Short Sword"`/Damage +2 (fix, kein RNG).
- `Items/Equipment.cs`: `UnequipTo(ItemSlot, Inventory)` (Slot → Bag, voll → false) + `ItemInstance GetEquipped(ItemSlot)` für die UI.
- Akzeptanz: Bag-Cap, Swap-Roundtrip, Starter-Stats als Konstanten.

## P2 — Inventar-UI + Klick-Guard

- `Scripts/DiabloInventoryUI.cs` (neu): baut `Canvas` (Screen Space Overlay) + Panel zur Laufzeit; `I` toggelt (Cursor unlock/lock); zeigt 3 Slot-Labels + Stats-Zeile (Damage/MaxHP aus `Equipment`) + Bag-Buttons; Button-Klick → `EquipFromBag`; `public bool IsOpen`.
- `DiabloClickController.cs`: Klicks (`HandleClick`/Strike-Pfad) entfallen, solange `DiabloInventoryUI.IsOpen` (UI frisst die Klicks); `I`-Taste darf nicht mit WASD kollidieren (kein `I` in `PollKeyboard` — prüfen).
- Headless-sicher: keine Annahmen über `Camera.main`/Render in Logikmethoden.
- Akzeptanz: Panel toggelt, Equip-per-Klick swapt, geschlossene UI ändert nichts am Strike-Verhalten.

## P3 — Blood-Moor-Umbau + Wiring (Größtes Partial)

- `BuildDiabloZone()` wird „Blood Moor": Bodenplane moor-grün statt Krypta-Grau; Hallenwände raus, dafür Palisadenring (Holz-Primitives) um das Süd-Camp mit Tor nach Norden; Camp: 4 `CoopSpawn_*` im Ring, `Campfire`-Objekt (P4-Component), `ZoneEntry`-Trigger mit neuem Text; Moor: 10 Husks verstreut (Mitte/Nord, mind. 3 m Abstand), 2. Elite („Moor Brute", 12 HP, Elite-Tier) im Nordosten; West-Alkoven + `DChestZone` + `Husk_Elite` unverändert; Ost-Rubble bleibt; Nord-GoalPad → „Moor Gate" (Name + Farbe, Q3-Semantik bleibt).
- Held + `NetworkPlayerDiablo`-Prefab: `Inventory` + `PlayerHealth` (P4) + `DiabloInventoryUI` (P2) dazu; `ItemDatabase.CreateStarterSword()` per `Equipment.Equip` sofort ausrüsten (Helper für beide, kein Doppelcode).
- `VerifyDiabloZone()`: Counts anheben (husks ≥ 10, Elites = 2, Campfire, Palisade, `Inventory`-/`Health`-Presence, Starter Damage ≥ 2); `ZoneEntryHint`-Text: „Rogue Encampment — WASD … Tor nach Norden, Moor säubern, Loot einsammeln (I = Inventar), Extraktion am Moor Gate".
- Akzeptanz: Verify grün, Szene lädt Solo + als Server.

## P4 — HP + Lagerfeuer

- `Scripts/PlayerHealth.cs` (neu): `maxHP` aus `Equipment.GetMaxHP()` (Refresh bei Equip), `currentHP`, `Heal(amount)` (clamped), `TakeDamage(amount)` (nur Residual-Logik für künftige Gegner-AI, kein Tod-Handling in diesem Cut — dokumentieren).
- `Scripts/CampfireHeal.cs` (neu): Trigger-Radius 4 m, `healPerSecond = 1f`; heilt `PlayerHealth` aller Helden im Radius (Solo + Coop-Mirrors lokal).
- Akzeptanz: Heilen clamp am Max, außerhalb kein Heal, Konstanten als `const`.

## P5 — Tests + Docs (Beweis)

- `Tests/EditMode/DiabloBloodMoorTests.cs`: Inventory-Cap, Equip-Swap-Roundtrip, Starter-Schwert-Stats, Campfire-Konstanten, Loot-Gewichte unangetastet (Regression zu P2-Pins).
- `Tests/PlayMode/DiabloBloodMoorLoopTests.cs` (Präzedenz `DiabloWASDTests`: `InputTestFixture` + synthetische Keys): Spawn mit Starter ausgerüstet → Husk killen → Drop → Pickup landet im Bag (nicht direkt am Körper) → per UI-Methode equippen → Campfire heilt.
- Docs-Anhänge: `decision-log.md` (Blood-Moor-Entscheide), `dev-journal.md`, `mechanics-questions.md` (Q7: „Trägt der Moor-Camp-Loop mit Inventar?").
- Akzeptanz: EditMode + PlayMode grün, Verify grün.

## Finaler Verify-Task (Pflicht vor Commit)

1. `scripts/run-unity-tests.sh` (EditMode + PlayMode, XML-Ergebnis `Passed`).
2. `Unity -batchmode -nographics -quit -projectPath sandbox -executeMethod SandboxBuilder.BuildDiabloZone -logFile /tmp/bloodmoor-build.log`, danach `-executeMethod SandboxBuilder.VerifyDiabloZone`.
3. `python3 ~/scripts/agent-workspace.py check` im Worktree-Root (still = ok).
4. Builds: `BuildDiabloZoneBuild` (Linux), `BuildDiabloZoneWindows`, `BuildDiabloZoneServer`; Deploy nach `ops/gpu-metal.md`-Konvention (rsync + service restart), Windows-Build ins Staging; Join-Proof + kurzer Smoke (Strike → Kill → Loot → `I`-Panel).
5. Commit `feat(diablo): blood moor rogue encampment inventory starter` (gezielte Pfade, kein `-A`), `land` (ff) + Push (Dauerfreigabe), Lock releasen.

## Übergabe an dev-flow-execute

Stand: Branch `feature/bloodmoor-rogue-encampment` gepusht, Plan committiert, kein PR. Nächster Schritt: `dev-flow-execute` liest diesen Plan aus `.agents/plans/bloodmoor-rogue-encampment/tasks.md` (kein Ticket-Backend — Pfad per Branch-Konvention). Follow-ups (nicht in diesem Cut): Gegner-Gegenangriff + Player-Tod, NPCs/Vendoren, Inventar-Sync im Coop, 60 FPS-Messung auf GPU.
