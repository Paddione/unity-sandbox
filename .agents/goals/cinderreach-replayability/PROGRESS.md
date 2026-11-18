# Progress: cinderreach-replayability

Prozent: 85 %

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

- 2026-11-18: S3 layouts gelandet. `LayoutSeeder`: je 3 Varianten für Husk-Gruppe, Ash Elite, Fen Brute,
  Prop-Cluster (81 Kombinationen, Variante 0 = bisherige Aufstellung). Seed aus `-seed N` oder
  `RunProfile.RunSeed`, Co-op-Peers bleiben auf Variante 0. Builder legt `LayoutProps_0..2` an,
  `VerifyDiabloZone` prüft `layouts=3`. Nachtlauf rotiert Seed 0..2 nach Wochentag, Ergebnis-JSON trägt
  `seed`. Belege: EditMode 125/125 (5 neu `LayoutSeedTests`, Erreichbarkeit von Fen Gate, Truhe und allen
  12 Gegner-Ankern für alle 81 Kombinationen), PlayMode 71/71, Verifies grün, Autoplay `-seed 1`
  und `-seed 2` je 6/6 Phasen, 0 Exceptions, FPS-Minimum 50,3/50,4.

- 2026-11-18: S4 kindled gelandet. `DifficultyProfile` (Normal ×1; Kindled HP/Schaden ×1,8, Affix-Tier 2,
  Unique-Gewicht ×2, Gold ×1,5), Freischaltung beim ersten Hollow-Abbot-Kill (sofort gespeichert,
  Overlay-Zeile „KINDLED FREIGESCHALTET“), Auswahl über „New run (Normal/Kindled)“ in den Einstellungen
  (lädt die Zone mit frischem Seed neu, nur solo), `-difficulty kindled` für Autoplay. Belege: EditMode 131/131
  (6 neu `DifficultyTests`), PlayMode 74/74 (3 neu `KindledUnlockTests`), Verifies grün, Autoplay Normal und
  Kindled je 6/6 Phasen, 0 Exceptions. Kindled-Lauf: Abbot 270 statt 150 HP, Husk 9 statt 5 HP.

## Nächste Schritte

1. S5 stash+proof.

## Offene Punkte

- S5 offen.
- Nebenbefund (nicht dieses Goal): Bone-Warden-Projektile treffen den Helden nie, weil
  `RangedCaster.Fire` über `DiabloProjectile`/`DiabloSkills.ApplyHit` nur `EnemyHealth` trifft. Kindled
  skaliert deshalb nur Husk-Nahkampf und Boss-Slam. Kandidat für `cinderreach-coop-session` S2 oder einen Fix.
