# Progress: arpg-session-loop

Prozent: 25 %

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

## Nächste Schritte

1. S3 town+vendor nach S2 (braucht Gold). dotfiles-Probe (`diablo_unity.py` auf `cinderreach`) separat landen.

## Offene Punkte

- Keine.
