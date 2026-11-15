---
goal: unity-diablo-p2-coop-loot-zone (neu, folgt unity-diablo-prototype 100 %; health_goals: none — einmaliger Prototyp wie Vorgänger)
slug: unity-diablo-p2-multiplayer-loot-zone
branch: feature/unity-diablo-p2-multiplayer-loot-zone
repo_main: main
tickets: none
request: Multiplayer + Magic/Rare/Unique-Items (Armor/Weapon/Helmet) mit Droptables + erste erkundbare Zone für das Unity-Diablo
---

# Plan: Diablo P2 — Co-op Loot-Zone (Multiplayer, Items, Droptables, Zone 1)

## Ausgangslage (Belege)

- `sandbox/Assets/Scripts/DiabloClickController.cs` — Solo P1, kein NetworkObject-Gate (Kommentar Zeile 9).
- `sandbox/Assets/Scripts/DiabloMelee.cs` / `EnemyHealth.cs` — lokaler Damage, `DropOrb()` spawnt gelbe Kugel mit `ToyTrigger` Pickup-Modus.
- `sandbox/Assets/Scripts/MultiplayerAutoStarter.cs` — Q4 Co-op-Muster: Host im Main, Client via `_clone_*` oder `--net-client`, `--net-address/--net-port`, Listen auf `0.0.0.0`.
- `docs/decision-log.md` — Q4 NGO + Relay/Lobby (continue), Q5 solo-first click-to-move kill-loot (continue headless).
- `docs/multiplayer-plan.md` — Pakete NGO 2.5.0 + Transport 2.4.0 + Lobby 1.3.0 + Relay 1.2.0 (bereits im Manifest).
- `sandbox/Assets/Editor/SandboxBuilder.cs` — `BuildDiabloScene` (16x16-Tile-Raum, 3 Husks, GoalPad), `VerifyDiablo`, `BuildDiablo`, `BuildDiabloServer`.
- Prior-Art-Suche: kein `docs/adr/`, kein `tests/spec/` (geprüft, 0 Treffer) — Entscheidungen stehen nur in `decision-log.md` + `multiplayer-plan.md` (oben zitiert). Bestehende Lösung (NGO-Direkt-IP + Solo-Diablo) wird **erweitert, nicht ersetzt**.

## Brainstorming-Entscheide (gelockt)

1. **Multiplayer:** Host-autoritativ NGO, 2–4 Spieler, DiabloZone-Szene. Direkt-IP zuerst (`MultiplayerAutoStarter`-Muster), Relay/Lobby als Option ohne neue Pakete. Kein PvP, kein Dedicated Server in P2 (Follow-up).
2. **Items:** 3 Slots (Armor, Weapon, Helmet) x 3 Raritäten (Magic 1–2 Affixe blau, Rare 3–6 Affixe gelb, Unique fix orange mit Flavor). Daten als C#-Klassen + `ItemDatabase` (kein `.asset`-Binary im Repo, Seed per Builder-Code). Kein Inventar-UI in P2 (Equip-Log + Verify).
3. **Droptables:** Jeder Drop ist mind. Magic (kein White-Tier in P2). Gewichtete Tabellen pro Gegner-Tier, seeded RNG für Tests. Ersetzt `DropOrb`.
4. **Zone 1 „Ember Crypt Approach“:** Süd-Treppe → Krypta-Halle (Diablo-Raum als Kern) → West-Alkoven (Elite + Chest) → Ost-Durchgang eingestürzt (Deko) → Nord-Extraktion (GoalPad als Krypta-Tor). 6 Husks + 1 Elite, 4 Coop-Spawnpunkte, KayKit Dungeon/adventurers/skeletons (bereits importiert).
5. **Q6:** „Ist der Coop-Kill-Loot-Extract-Loop mit Rarity-Chase in der Krypta fun?“ Exit: Verify grün + Linux-Build läuft + PlayMode-Loot-Test grün; Human-5er-Playtest = Follow-up (Q1–Q5-Präzedenz).

## Partial-Manifest (disjunkte target_files, max 9)

| Partial | Inhalt | target_files (exklusiv) |
|---|---|---|
| P1 net | Coop-Netcode Diablo | `sandbox/Assets/Scripts/DiabloClickController.cs`, `sandbox/Assets/Scripts/DiabloMelee.cs`, `sandbox/Assets/Scripts/DiabloNetworkBootstrap.cs`, `sandbox/Assets/Scripts/DiabloNetworkPlayer.cs`, `sandbox/Assets/Scripts/MultiplayerAutoStarter.cs` |
| P2 items | Item-Daten + Equip | `sandbox/Assets/Scripts/Items/ItemDefinition.cs`, `sandbox/Assets/Scripts/Items/AffixLibrary.cs`, `sandbox/Assets/Scripts/Items/Equipment.cs`, `sandbox/Assets/Scripts/Items/ItemDatabase.cs` |
| P3 loot | Droptables + Drop | `sandbox/Assets/Scripts/EnemyHealth.cs`, `sandbox/Assets/Scripts/Loot/LootTable.cs`, `sandbox/Assets/Scripts/Loot/LootDrop.cs`, `sandbox/Assets/Scripts/Loot/DroptableDatabase.cs` |
| P4 zone | Zone 1 Builder + Szene | `sandbox/Assets/Editor/SandboxBuilder.cs`, `sandbox/Assets/Scenes/DiabloZone.unity`, `sandbox/Assets/Scenes/DiabloZone.unity.meta` |
| P5 proof | Tests + Docs | `sandbox/Assets/Tests/EditMode/DiabloP2ConfigTests.cs`, `sandbox/Assets/Tests/PlayMode/DiabloP2LoopTests.cs`, `docs/mechanics-questions.md` (Q6-Anhang), `docs/decision-log.md` (Q6-Anhang), `docs/dev-journal.md` (Anhang) |

Schnittstellen: P3 definiert `EnemyHealth.RequestDamageServerRpc(int, ulong)` — P1 ruft nur diese auf. P4 ruft `DiabloNetworkBootstrap.EnsureZoneNetcode()` (P1) — kein Builder-Code in P1. P2 `ItemDatabase.GetBaseTypes(slot)` wird von P3 `LootDrop` konsumiert. S1-Richtwert: keine Datei gebaselined (kein `docs/code-quality/baseline.json`) — neue Dateien ≤ ~150 Zeilen, kein Split nötig (Neuanlage).

## P1 — Coop-Netcode (Ziel: 2–4 Spieler hosten/joinen)

- `DiabloNetworkPlayer.cs` (neu): `NetworkBehaviour`, Owner-Gate (`IsOwner`), hält `NetworkTransform` + `Equipment`-Ref, spawnt Kamera nur für Owner.
- `DiabloClickController.cs`: Early-Return wenn `NetworkManager.Singleton != null && !IsOwner` (per `DiabloNetworkPlayer`), Kamera-Update nur Owner.
- `DiabloMelee.cs`: `TryAttack` bleibt lokal für Solo; neu `TryAttackServerRpc(targetNetId)` — Server prüft Range/Cooldown, ruft `EnemyHealth.RequestDamageServerRpc` auf.
- `DiabloNetworkBootstrap.cs` (neu): `EnsureZoneNetcode()` — NetworkManager + UnityTransport (127.0.0.1:7777) + `MultiplayerAutoStarter` in DiabloZone, `NetworkPlayerDiablo`-Prefab als PlayerPrefab (Knight-Optik, CharacterController, NetworkObject, NetworkTransform, Click/Melee/NetworkPlayer/Equipment).
- `MultiplayerAutoStarter.cs`: `--net-server`-Pfad + DiabloZone-Szenenname loggen, sonst unverändert (Q4-Semantik bleibt).
- Akzeptanz: 2. Host startet ohne Exception, `--net-client` loggt Client-Pfad (Q4-Muster).

## P2 — Items (Ziel: Magic/Rare/Unique je Slot)

- `ItemDefinition.cs`: `Slot {Armor, Weapon, Helmet}`, `Rarity {Magic, Rare, Unique}`, `baseType`, `List<Affix>`, `uniqueId` (nur Unique).
- `AffixLibrary.cs`: Pools pro Slot — Weapon: +Damage, +AttackSpeed, +Crit; Armor: +MaxHP, +Armor, +MoveSpeed; Helmet: +XP-Mul, +Crit, +MaxHP. Ranges pro Affix, Roll mit `System.Random(seed)`.
- `ItemDatabase.cs`: Basistypen (Weapon: ShortSword/HeavyAxe/Dagger; Armor: ClothTunic/LeatherVest/Breastplate; Helmet: Hood/Helm/Circlet) + 3 Uniques fix: `Emberbrand Blade` (Weapon, +Fire-Damage), `Cryptwarden Plate` (Armor, +HP/+Armor), `Seer's Circlet` (Helmet, +XP/+Crit) inkl. Flavor-Text.
- `Equipment.cs`: `NetworkBehaviour`, 3 Slots, `Equip(ItemInstance)` ersetzt + recalc (`damage, maxHP, moveSpeed, crit, xpMul`), `Debug.Log("[Loot] Equipped ...")` für Headless-Beleg.
- Akzeptanz: Alle 9 Slot×Rarity-Kombinationen erzeugbar, Uniques fix (kein Roll).

## P3 — Droptables (Ziel: gewichteter Loot statt Orb)

- `DroptableDatabase.cs`: Tabellen pro Tier — Normal-Husk Dropchance 70 %: 80 Magic / 18 Rare / 2 Unique; Elite 100 %: 50 / 40 / 10; Chest/Boss 100 %: 0 / 80 / 20. Gewichte summieren je 100 (Test-pinned). Elite = 5x HP.
- `LootTable.cs`: `Roll(tier, rng)` → `(rarity, slot)`; seeded `System.Random` für deterministische Tests.
- `LootDrop.cs`: `SpawnLoot(pos, rarity, slot)` — Farb-Cube (blau/gelb/orange, URP/Lit) + `ToyTrigger` Pickup + `NetworkObject.Spawn()` wenn netcode aktiv; Pickup ruft `Equipment.Equip` auf dem einsammelnden Spieler.
- `EnemyHealth.cs`: `maxHP` → `NetworkVariable<int>`-Sync (Server schreibt), Tod nur Server (`Die` via `RequestDamageServerRpc`), `DropOrb()` ersetzt durch `LootDrop.SpawnLoot(LootTable.Roll(...))`; `HusksKilled/OrbsDropped`-Zähler bleiben für Q5-Kompatibilität (+ `ItemsDropped`).
- Akzeptanz: 1000 seeded Rolls treffen Verteilungs-Toleranz ±5 %, Chest garantiert Rare+.

## P4 — Zone 1 „Ember Crypt Approach“ (Ziel: erkundbar)

- `BuildDiabloZone()`: Süd-Treppe (3 Stufen, Primitives) → Halle 16x16 (wiederverwendet `floor_tile_large`, `wall`, `wall_corner`, `torch_lit` x4) → West-Alkoven 6x6 (Elite-Spot + `chest` als `LootChest` mit `DroptableDatabase` Chest-Tier) → Ost-Durchgang mit Geröll (Deko-Cubes, blockiert) → Nord-Krypta-Tor (`GoalPad` reskinned, `ToyTrigger` Goal-Modus, `Needed = 6` Pickups ≙ 6 Husks).
- Spawns: 6 Husks (2 Warrior/2 Minion/2 Rogue-Modelle, Positionen Halle + Korridor) + 1 Elite (`Husk_Elite`, Minion-Modell skaliert 1.3x, +HP, roter Tint via Material-Instanz) + 4 `CoopSpawn_{0..3}` (Süd-Treppe, Kreis-Formation).
- `VerifyDiabloZone()`: Spieler-Spawns=4, Husks≥6 (mit `EnemyHealth`), Elite=1, GoalPad=1 (Goal-Modus), Torches≥4, Tiles≥25, Walls≥4, nur URP/Lit-Materialien, NetworkManager + Transport vorhanden.
- Build-Settings: Greybox bleibt Index 0 (Pin aus `UrpPipelineTests`), DiabloZone nach Diablo angehängt; `BuildDiabloZoneBuild()` analog `BuildDiablo` nach `builds/diablo-zone/diablo-zone`.
- Akzeptanz: `VerifyDiabloZone` OK, Szene öffnet fehlerfrei.

## P5 — Proof (Ziel: Q6 + grüne Suites)

- `DiabloP2ConfigTests.cs` (EditMode): Build-Settings-Reihenfolge (Greybox 0, Diablo + DiabloZone enthalten), DiabloZone-Inventar (Spawns/Husks/Elite/Goal/Netcode), Droptable-Gewichte = 100, URP/Lit-only, ItemDB (9 Kombis + 3 Uniques fix).
- `DiabloP2LoopTests.cs` (PlayMode): seeded Loot-Roll-Verteilung, `Equipment.Equip` recalc (Damage/HP), Melee→Damage→Tod→Loot-Kette (solo, ohne Netz), Elite-HP-Skalierung.
- Docs: Q6 in `mechanics-questions.md`, Q6-Entscheid in `decision-log.md`, Session in `dev-journal.md`.
- Finaler Verify-Task (aus `repo.yaml:verify` + `scripts/run-tests.sh`): `KayKitAssets.Verify` + EditMode + PlayMode grün, `BuildDiabloZone` Succeeded, 40-s-Run 0 Exceptions, `python3 ~/scripts/agent-workspace.py check` im Dotfiles-Checkout still.

## File Structure (Zielbaum)

```text
sandbox/Assets/
  Editor/SandboxBuilder.cs            # +BuildDiabloZone/VerifyDiabloZone/BuildDiabloZoneBuild (P4)
  Scenes/DiabloZone.unity             # Zone 1 (P4, neu)
  Scripts/
    DiabloClickController.cs          # +Owner-Gate (P1)
    DiabloMelee.cs                    # +ServerRpc (P1)
    MultiplayerAutoStarter.cs         # +Zone-Log (P1)
    DiabloNetworkBootstrap.cs         # neu (P1)
    DiabloNetworkPlayer.cs            # neu (P1)
    Items/ItemDefinition.cs           # neu (P2)
    Items/AffixLibrary.cs             # neu (P2)
    Items/Equipment.cs                # neu (P2)
    Items/ItemDatabase.cs             # neu (P2)
    EnemyHealth.cs                    # Netz-Sync + Loot-Hook (P3)
    Loot/LootTable.cs                 # neu (P3)
    Loot/LootDrop.cs                  # neu (P3)
    Loot/DroptableDatabase.cs         # neu (P3)
  Tests/EditMode/DiabloP2ConfigTests.cs  # neu (P5)
  Tests/PlayMode/DiabloP2LoopTests.cs    # neu (P5)
docs/mechanics-questions.md docs/decision-log.md docs/dev-journal.md  # Q6-Anhänge (P5)
```
