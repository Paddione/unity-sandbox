# Progress: arpg-session-loop

Prozent: 80 %

## Erledigt

- 2026-11-17: Eigner-Runde beantwortet (Spielschleife, eigene Identität, headless + Shotcheck,
  `G-DIABLO01,02,03,05`).
- 2026-11-17: Goal angelegt, Plan `.agents/plans/arpg-session-loop/tasks.md` (Stufen S1–S6) geschrieben.
- 2026-11-17: Eigner bestätigt Arbeitstitel „Cinderreach“ und die Namensliste. Plan um Vorbedingung
  (Fremd-Änderungen HD-UI + MenuArt), Session-Protokoll, Statustabelle und Definition of Done ergänzt.
- 2026-11-17: Vorbedingung erfüllt: HD-UI + MenuArt-Pack aus dem Hauptcheckout gelandet (Startsperre gelöst).

## Erledigt

- 2026-11-17: S1 Identität gelandet (unity-sandbox-Anteil; dotfiles-Probe läuft separat über den Orchestrator).
  Belege: EditMode 58/58, PlayMode 59/59, `VerifyDiabloZone` grün („ashen fen + loot + netcode present“),
  `KayKitAssets.Verify` grün, `check-meta.py` ohne Befund.
  `grep -rniE "blood ?moor|rogue encampment|moor gate" sandbox scripts docs` trifft nur noch bewusste
  Ausnahmen: Historie (`dev-journal.md`, `decision-log.md`, `mechanics-questions.md` bleiben unverändert),
  Asset-Pfade (`Assets/Art/BloodMoor`, `docs/world-art/bake_moor_ground.py` — GUID-gebunden) und die
  Regex-Definition in `IdentityTests.cs` selbst. Laufzeit-Strings/Builder/Tests sind leer.
  Fix nebenbei: `DiabloAudio.EnsureBuilt` baut Clips neu, wenn Szenen-Ladungen sie zerstört haben
  (latenter Bug, machte `AudioClips_AreValidMono22050` reihenfolgeabhängig rot).

- 2026-11-18: S4 progression gelandet.
  Belege: EditMode 72/72 (neu `CharacterSheetTests`: Punktezählung über Level-Sprünge,
  Vitality-Life, Rank-Cap 5 + Schadensskala, Focus-Schaden/Cooldown, Cleave-Radius,
  Save-Roundtrip), PlayMode 66/66 (neu `SkillRankTests`: Cleave trifft alle Husks im
  Radius, Rang erhöht `ApplyHit`-Schaden, Cleave-Cooldown 6 s, `CleaveArc`-Prefab),
  `VerifyDiabloZone` grün, `KayKitAssets.Verify` grün, `check-meta.py` ohne Befund.
  Punkte: 1 Attribut (Strength/Vitality/Focus) + 1 Skillpunkt (Rang 1–5, +20 %/Rang)
  pro Level, +3 Life/Level bleibt; dritter Skill Cleave (R, 360°, 6 s, Rang erweitert
  Radius); `DiabloCharacterUI` (C, HD-Scaler, Puls bei ungespendeten Punkten).

- 2026-11-18: S2 economy+save gelandet.
  Belege: EditMode 65/65 (neu `SaveGameTests`: Roundtrip aller Felder, Version/Korrupt-Pfade,
  Wallet-Add/TrySpend, Goldtabellen-Konstanten + -Rolls), PlayMode 62/62 (neu `EconomyLoopTests`:
  Gold-Pickup-Bilanz, Kill-Drop-Bilanz, Reload setzt Level/Gold fort), `VerifyDiabloZone` grün
  (Held + Netz-Prefab tragen `Wallet`), `KayKitAssets.Verify` grün, `check-meta.py` ohne Befund.
  Gold: `Wallet` (pur) am Helden wie `Inventory` (Builder + Verify), `GoldDrop`-Pickup
  (Normal 70 % 3–8, Elite 100 % 15–30, Chest 40–60, Konstanten in `DroptableDatabase`),
  unabhängiger Roll neben der Item-Kugel in `LootDrop.SpawnLoot`.
  Save: `SaveGame` (v=1: Level, XP, S4-Punktfelder, Skill-Ränge, Equipment, Bag, Gold, zoneReached),
  `SaveService` (`cinderreach-save.json`, JsonUtility, tmp+rename, Autosave bei Level-Up + Quit,
  `SaveNow`-Hooks für Zonen-Übergang S5 / Vendor-Schluss S3 als TODO-Kommentar, korrupt →
  `*.corrupt` + Neustart + Warnung). `DiabloProgression` mit GetState/SetState (Level-Up-Autosave).
  Menü: kein Menü-Szenen-Code vorhanden — Continue (nur bei Save) / New Game (mit Bestätigung) als
  TODO ans Menü übergeben (Verdrahtung über `HasSave`/`TryLoad`/`DeleteSave` dokumentiert).


- 2026-11-18: S3 town+vendor gelandet (Rebase über S4, HdTests vereint).
  Belege nach Merge: EditMode 83/83 (VendorTests 10 + CharacterSheetTests 6), PlayMode 69/69
  (VendorLoopTests 3 + SkillRankTests 4), `VerifyDiabloZone` grün
  (`palisade=15 torches=4`, neu `vendor` + `vui` + Netz-Prefab-`DiabloVendorUI`-Check),
  `KayKitAssets.Verify` grün, `check-meta.py` ohne Befund.
  Town: Camp (Palisade/Campfire/ZTorch) aus `BuildMoorField` nach `Editor/TownBuilder` verschoben
  (gleiches Ergebnis), Vendor-NPC (KayKit Adventurer `Rogue_Hooded`, Idle `Idle_A`,
  Klick/Radius 3.5 m). `VendorStock` (6 seeded Offers, Preise Magic 25 / Rare 90 / Unique 300,
  Verkauf 25 %, Heiltrank-Stack `Consumable` Taste 1). `DiabloVendorUI` (zwei Spalten Vendor/Bag,
  Gold-Anzeige, HD-Scaler, Klick-Sperre, Schluss → `SaveService.SaveNow`).

- 2026-11-18: S6 proof, Basis-Teil ohne S5 (kein Prozent, bleibt 55 %).
  `Scripts/SessionAutoplay.cs` (neu, `-autoplay` inert ohne Flag, Gott-Cheat nur
  `-autoplay-god` im Log ausgewiesen): Wegpunkte Lantern Hold, Kampf per Melee + Q/E,
  Trank-Kauf über S3-Seam `VendorStock.BuyPotion`, Fen Gate als Seam-Name; Ossuary-Root,
  `ZoneGate`, Bone Warden, The Hollow Abbot, `SessionComplete` als markierte Seams für den
  S5-Folgepass. `DiabloShotCheck` + Town/Vendor-UI/Character-UI (+ S5-Shot-TODOs).
  Docs: Q9 in `mechanics-questions.md`, `decision-log.md` (continue Basis),
  `one-page-gdd.md` (finaler Stand S1–S4), Run-Log `docs/session-loop-run-log.md`.

## Nächste Schritte

1. S5 zone2+boss nach S3+S4 (S3+S4 gelandet 2026-11-18). Danach S6-Folgepass
   (Ossuary-/Boss-Beine + Windows-Shots, dann S6 auf landed setzen).

- 2026-11-18: S5 zone2+boss gelandet.
  Belege: EditMode 89/89 (neu `OssuaryConfigTests`: Boss-Tuning 150 HP/XP 120,
  Phasenwechsel 50 %, Boss-Loot immer Rare+, Gold 150–250, Caster-Abstand 6–8 m;
  `DiabloHdTests.SessionComplete_UsesHdScaler`), PlayMode 71/71
  (neu `BossLoopTests`: Gate teleportiert + speichert Zone 2, Rückweg-Tor nach Town,
  Boss-Tod öffnet Abschluss mit Kills/Gold), `VerifyDiabloZone` grün
  (`root=True fen=True ret=True casters=8 elites=2 boss=True`, `husks=21`,
  `palisade=15 torches=4 net=True`), `KayKitAssets.Verify` grün
  (Dungeon 211, Adventurers 6, Skeletons 4), `check-meta.py` ohne Befund.
  Zone: `Editor/OssuaryBuilder` (Annex +200 m X, Sarkophage/Pfeiler/Fackeln,
  Tor-Paar FenGate↔OssuaryGate, 8 Bone Wardens + 2 Elites, Hollow Abbot 150 HP Boss-Tier).
  Gates: `ZoneGate` (Trigger → Teleport, `zoneReached`-Autosave via `SaveNow`).
  Gegner: `RangedCaster` (6–8 m Abstand, 0,6 s Telegraph, `DiabloProjectile`-Feuerball,
  host-autoritativ), `BossController` (Phase 2 ab 50 %: 3 Husks + Projektilfächer,
  Tod → `SessionComplete`). `LootTier.Boss` (Rare+ garantiert, 150–250 Gold, 120 XP),
  `SessionComplete` (Zeit/Kills/Gold/Beste Beute, Buttons Town/Menü, HD-Scaler).
  Fix nebenbei: Button-Label als Child-Text (AddComponent<Text> auf Button-Objekt
  warf NRE in EditMode). Controller-LFS-Drift revertiert (Diablo/Husk/VendorIdle).

## Offene Punkte

- Keine.
