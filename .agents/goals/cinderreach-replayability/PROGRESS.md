# Progress: cinderreach-replayability

Prozent: 40 %

## Erledigt

- 2026-11-17: Goal angelegt (Folge-Goal nach `arpg-session-loop`).
- 2026-11-17: Plan `.agents/plans/cinderreach-replayability/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: Startbedingung erfüllt (`arpg-session-loop` und `cinderreach-loop-guard` 100 %).
- 2026-11-18: S1 save-v2 gelandet. `SaveGame` v2 (`difficultyUnlocked`, `difficulty`, `runSeed`, `stash`),
  `SaveMigrations` (v1 → v2, Backup `*.v1.bak` vor dem ersten v2-Schreiben, neuere Version wird nicht
  geladen und bleibt unberührt), `Stash` (40 Plätze), `RunProfile` als Laufzeit-Halter der v2-Felder.
  Belege: EditMode 106/106 (9 neu in `SaveMigrationTests`, `FutureVersion_NotLoadedAndLeftUntouched`
  ersetzt den alten Corrupt-Pfad), PlayMode 71/71, `VerifyDiabloZone` + `KayKitAssets.Verify` grün,
  `check-meta` OK, `run-autoplay.sh` Exit 0 (6/6 Phasen, 0 Exceptions, FPS-Minimum 49,1).

- 2026-11-18: S2 affix gelandet. `AffixDef` + `AffixLibrary` mit 14 Affixen (7 alte, Tier-1-Spannen
  unverändert, dazu LifeSteal, GoldFind, SkillRank, CooldownReduction, FireDamage, Thorns, MagicFind),
  Tier 1/Tier 2, Rare 3–6. Effekte: Lebensraub (Nahkampf + Projektile, Deckel 25 %), Gold Find am
  Gold-Pickup, Magic Find in `LootTable.Roll` (Rare ×(1+mf/200), Unique ×(1+mf/100), mf 0 = exakter
  P2-Pfad), SkillRank +N auf alle Skills, Cooldown-Reduktion (Deckel 40 %), Feuerschaden im Waffenschaden,
  Dornen in `EnemyAttack`. Vergleichs-Tooltip `DiabloInventoryUI.CompareSummary`. Belege: EditMode 120/120
  (14 neu `AffixTableTests`), PlayMode 71/71, Verifies grün, Autoplay 6/6 Phasen, FPS-Minimum 49,0.

## Nächste Schritte

1. S3 layouts, danach S4 kindled.

## Offene Punkte

- S3 bis S5 offen.
