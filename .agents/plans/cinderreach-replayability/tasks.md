---
goal: cinderreach-replayability
slug: cinderreach-replayability
branch: feature/cinderreach-replayability-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: arpg-session-loop 100 % und cinderreach-loop-guard 100 %
---

# Plan: cinderreach-replayability — Wiederspielwert nach dem Boss

Pfad: feature, 5 Stufen. Ausführung nach dem **Session-Protokoll** aus
`.agents/plans/arpg-session-loop/tasks.md` (Schritte 1–9), Lock-Name `task cinderreach-replayability-sN`.
Protokollschritt 1 prüft zusätzlich: `PROGRESS.md` von `arpg-session-loop` **und** `cinderreach-loop-guard`
zeigen `Prozent: 100 %`.

## Ausgangslage (Belege, Stand 2026-11-17)

- `Items/AffixLibrary.cs`: 7 Affix-Schlüssel (`Damage`, `AttackSpeed`, `Crit`, `MaxHP`, `Armor`,
  `MoveSpeed`, `XPMul`), Pools je Slot mit 3 Einträgen, Magic 1–2 / Rare 3–6 Affixe, Werte per
  `RollValue`. Affixe als String-Schlüssel, kein Typ.
- `Loot/LootTable.cs` + `DroptableDatabase.cs`: Gewichte je `LootTier` (Normal 80/18/2, Elite 50/40/10,
  Chest Rare+), Unique-Items fest in `ItemDatabase`.
- Zone 1 entsteht im Builder (`SandboxBuilder.BuildMoorField()` Z. 845) aus festen Positionen.
- Aus arpg-session-loop: `SaveGame` v1 (`JsonUtility`), `BossController`, `SessionComplete`,
  `TownBuilder`, `SessionAutoplay` mit festen Seeds. Aus loop-guard: nächtlicher Autoplay + `G-DIABLO06`.

## Entscheide

1. Layout-Varianten werden **zur Laufzeit** aus Bausteinen gesetzt (Husk-Gruppen, Elite-Spots,
   Prop-Cluster als Anker-Sets im Builder), nicht als 3 Szenen. Seed kommt aus dem Save (neuer Lauf =
   neuer Seed), Autoplay setzt `-seed`.
2. Affixe bleiben String-Schlüssel (Save-Kompatibilität), bekommen aber eine Tabelle mit Tier-Spannen
   (`AffixDef`: Schlüssel, Slots, Tier-1/Tier-2-Spanne, Effekt-Hook). Rare-Anzahl bleibt 3–6 wie heute.
   Goal-Kriterium 3 daran angepasst.
3. Kindled-Stufe als Multiplikator-Profil (`DifficultyProfile`), kein zweiter Content-Satz.
4. Save v2: neue Felder `difficultyUnlocked`, `difficulty`, `runSeed`, `stash`. Migration v1 → v2 ist
   Pflicht und getestet.

## Partial-Manifest

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 save-v2 | Save-Migration + Stash-Daten | `Scripts/Save/SaveGame.cs`, `Scripts/Save/SaveMigrations.cs` (neu), `Scripts/Items/Stash.cs` (neu), `Tests/EditMode/SaveMigrationTests.cs` |
| S2 affix | Affix-Tabelle, ≥ 12 Affixe, Effekte, Vergleichs-Tooltip | `Scripts/Items/AffixLibrary.cs`, `Scripts/Items/AffixDef.cs` (neu), `Scripts/Items/Equipment.cs` (Effekt-Hooks), `Scripts/DiabloInventoryUI.cs` (Tooltip-Vergleich), `Tests/EditMode/AffixTableTests.cs` |
| S3 layouts | Geseedete Layouts Ashen Fen | `Editor/SandboxBuilder.cs` (`BuildMoorField` → Anker-Sets), `Scripts/Zones/LayoutSeeder.cs` (neu), `Scripts/SessionAutoplay.cs` (`-seed`), `Tests/PlayMode/LayoutSeedTests.cs` |
| S4 kindled | Schwierigkeitsstufe + Freischaltung | `Scripts/Difficulty/DifficultyProfile.cs` (neu), `Scripts/EnemyHealth.cs`, `Scripts/EnemyAttack.cs`, `Scripts/Loot/DroptableDatabase.cs`, `Scripts/SessionComplete.cs` (Freischalt-Hinweis), `Scripts/Menu/MainMenu.cs` (Auswahl), `Tests/EditMode/DifficultyTests.cs`, `Tests/PlayMode/KindledUnlockTests.cs` |
| S5 stash+proof | Truhe in Lantern Hold, Kindled-Autoplay, Docs | `Scripts/Town/StashChest.cs` (neu), `Scripts/Town/DiabloStashUI.cs` (neu), `Editor/TownBuilder.cs`, `.github/workflows/autoplay-nightly.yml` (Kindled-Lauf), `docs/replayability-run-log.md` (neu) |

S1 zuerst (Save-Format). Danach S2 und S3 parallel (disjunkt). S4 nach S2 (Affix-Tiers). S5 zuletzt.

## S1 — Save v2 + Stash-Daten

- `SaveMigrations.Migrate(json)`: liest `v`, hebt v1 auf v2 (Defaults: Kindled gesperrt, Normal,
  zufälliger Seed, leerer Stash). Unbekannte höhere Version → kein Laden, Warnung, Save bleibt unberührt.
- `Stash` (pure C#, 40 Plätze), API analog `Inventory`.
- Tests: v1-Fixture (echte Datei aus arpg-session-loop-Tests) migriert verlustfrei, v3 wird abgelehnt.

## S2 — Affixe

- 12+ Affixe: bestehende 7 plus `LifeSteal`, `GoldFind`, `SkillRank` (+1 auf zufälligen Skill),
  `CooldownReduction`, `FireDamage`, `Thorns`, `MagicFind`. Je Affix Tier-1 (Normal) und Tier-2 (Kindled).
- Effekt-Hooks in `Equipment` aggregieren Werte, `DiabloSkills.ApplyHit` und `PlayerHealth` lesen sie.
- Tooltip im Inventar: Wert-Differenz zum ausgerüsteten Item (grün/rot).
- Tests: jede Affix-Spanne, Aggregation, LifeSteal heilt, SkillRank wirkt auf Rang, Gewichte summieren
  korrekt.

## S3 — Layouts

- Builder legt 3 Anker-Sets je Kategorie an (inaktiv). `LayoutSeeder` aktiviert per Seed eine Kombination
  und platziert Husks/Elites daran.
- Erreichbarkeit: Für jeden Seed 0..2 prüft ein PlayMode-Test per NavMesh-freiem Raycast-Korridor oder
  SphereCast-Pfad (Steering aus `EnemyAttack`), dass Fen Gate und Truhe vom Spawn erreichbar sind.
- Autoplay: `-seed N`, nächtlicher Lauf rotiert Seed 0..2 nach Wochentag.

## S4 — Kindled

- `DifficultyProfile`: Normal (×1), Kindled (HP ×1,8, Schaden ×1,8, Affix-Tier 2, Unique-Chance ×2,
  Gold ×1,5). Freischaltung durch den ersten Boss-Kill (Save-Feld). Auswahl im Menü.
- `SessionComplete` zeigt „Kindled freigeschaltet“ beim ersten Kill.
- Tests: Skalierung, Droptable-Gewichte Kindled, Freischaltung persistiert.

## S5 — Stash + Beweis

- `StashChest` in Lantern Hold (Interaktion wie Vendor), `DiabloStashUI` (HD-Scaler-Konvention).
- Nachtlauf erweitert: zweiter Autoplay mit `-difficulty kindled -autoplay-god`. Das Ergebnis-JSON trägt
  `difficulty`, `G-DIABLO06` bewertet beide Läufe (dotfiles-Probe: neueste Ergebnisse je Schwierigkeit).
- Run-Log: Seeds, Kindled-Lauf, Drop-Statistik über 3 Läufe (Unique-Rate Normal vs. Kindled).
- Akzeptanz: alle 5 Goal-Kriterien belegt.

### Statustabelle

| Stufe | Abhängig von | Gewicht | Status |
|---|---|---|---|
| S1 save-v2 | Startbedingung | 15 % | offen |
| S2 affix | S1 | 25 % | offen |
| S3 layouts | S1 | 25 % | offen |
| S4 kindled | S2 | 20 % | offen |
| S5 stash+proof | S3, S4 | 15 % | offen |

## Verify (je Stufe)

1. `scripts/run-unity-tests.sh` grün.
2. `VerifyDiabloZone` grün, bei S3 für alle Seeds.
3. `bash scripts/run-autoplay.sh` Exit 0 (bei S3 mit `-seed 0..2`, ab S4 auch Kindled).
4. `python3 scripts/check-meta.py` ohne Befund.
5. dotfiles-Anteil (S5): `check` + `pytest scripts/test_diablo_unity_health.py -q` + Live-Collector.

## Risiken

- R1 Save-Migration zerstört Spielstände. Gegenmittel: Backup `*.v1.bak` vor dem ersten v2-Schreiben.
- R2 Layout-Seeds blockieren Wege. Gegenmittel: Erreichbarkeitstest pro Seed ist Pflicht-Gate.
- R3 Kindled ist mit Autoplay-God-Modus nicht aussagekräftig für Balance. Akzeptiert: Balance-Urteil
  bleibt Eigner-Playtest (Nicht-Ziel), Autoplay belegt nur Durchspielbarkeit.
