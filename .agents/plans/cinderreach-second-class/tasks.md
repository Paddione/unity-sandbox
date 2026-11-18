---
goal: cinderreach-second-class
slug: cinderreach-second-class
branch: feature/cinderreach-second-class-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: cinderreach-loop-guard 100 %
---

# Plan: cinderreach-second-class — Lanternbearer als zweite Klasse

Pfad: feature, 5 Stufen. Ausführung nach dem **Session-Protokoll** aus
`.agents/plans/arpg-session-loop/tasks.md` (Schritte 1–9), Lock-Name `task cinderreach-second-class-sN`.
Protokollschritt 1 prüft zusätzlich: `PROGRESS.md` von `cinderreach-loop-guard` zeigt `Prozent: 100 %`.
S5-Teil „Balance-Abnahme Zone 3“ wartet zusätzlich auf `cinderreach-act-one` 100 %.

## Ausgangslage (Belege, Stand 2026-11-18, HEAD `c70ca4a`)

- `Scripts/DiabloSkills.cs` (445 Z.): `enum Skill { Fireball, Arrow, Cleave }`, Q/E/R, `BaseCooldown`,
  `EffectiveCooldown`, `EffectiveDamage`, `CastToward`, `DoCleave`, `ShowRemoteCast` (Co-op-Kosmetik über
  `DiabloNetworkPlayer` Z. 121, Skill als int übertragen), `ApplyHit`.
- `Scripts/Progression/CharacterSheet.cs` (147 Z.): Attribute `Strength/Vitality/Focus`, Rang je
  `DiabloSkills.Skill`, `SkillMultiplier`, `CooldownMultiplier`, `CleaveRadius`, `ToEntries`/`FromEntries`.
  `DiabloCharacterUI` (224 Z.) hat feste Buttons `SpendFireball/SpendArrow/SpendCleave`.
- `Scripts/Items/ItemDefinition.cs`: `ItemSlot { Armor, Weapon, Helmet, Consumable }`, `ItemInstance` mit
  `baseType`, keine Klassenbindung.
- `Scripts/Save/SaveGame.cs`: keine Klassen-Angabe, `skillRanks` als String-Schlüssel.
- Held-Prefab entsteht in `SandboxBuilder.BuildDiabloZonePlayerPrefab()`. Menü „New Game/Continue“
  liegt heute in `DiabloProgression.cs`; nach presentation S4 in `Scripts/Menu/MainMenu.cs`.
- Aus loop-guard erwartet: `SessionAutoplay` mit Ergebnis-JSON, nächtlicher Lauf, `G-DIABLO06`.

## Entscheide

1. Identität (Eigner 2026-11-18): Bestehende Klasse heißt **Fenwalker** und bleibt unverändert
   (Fireball, Arrow, Cleave, Balance). Neue Klasse **Lanternbearer**, Licht-Caster mit vier Skills:
   **Lantern Bolt** (Projektil, Q), **Wisps** (2 Begleiter, E), **Searing Halo** (Ring um den Helden, R),
   **Veil Step** (Kurzteleport 6 m, Leertaste).
2. Klassen als Daten: `ClassDefinition` (ScriptableObject-frei, pure C#-Katalog `ClassCatalog`) mit Id,
   Anzeigename, Skill-Liste, Tastenbelegung, Attribut-Skalierung, Basis-Modell. `DiabloSkills.Skill` wird um
   die 4 neuen Werte **am Ende** erweitert (int-Übertragung im Netz bleibt für alte Werte stabil).
3. Attribute bleiben `Strength/Vitality/Focus`. Lanternbearer skaliert Skill-Schaden über Focus stärker
   (Faktor im `ClassDefinition`), Strength wirkt nur auf Grundangriff. Kein neues Attribut.
4. Item-Bindung über optionales `classRestriction` am Item-Basistyp (leer = alle Klassen). Neue Basistypen:
   Laterne (Weapon, Lanternbearer). Drops: 70 % passend zur Klasse des Finders, Rest frei.
5. Save: nächste freie Version (nach replayability und ggf. act-one) mit `classId`. Migration: fehlendes
   `classId` → `fenwalker`. Pro Save genau ein Charakter, kein Charakter-Slot-System.
6. Lanternbearer-Modell aus dem vorhandenen KayKit-Adventurer-Satz (`Editor/KayKitCharacters.cs`), Laterne
   als Blender-Prop über `WorldArtBuilder`. Keine Fremd-IP.

7. Nachtrag S2 (2026-11-18): Keine Icons über `SkillArtBuilder`, das HUD ist textbasiert. Bolt-, Wisp-, Halo-
   und Veil-Art entstehen prozedural (Licht + Primitive, Halo als gold getönter `CleaveArc`). Bis S3 liefert
   `CharacterSheet.GetRank` für Lanternbearer-Skills 0 und `SpendSkill` lehnt sie ab.

8. Nachtrag S4 (2026-11-18): Kein zweites Held-Prefab. Die KayKit-Adventurers teilen ein Rig, `ClassAppearance`
   bindet die Mage-Meshes an die Knochen des vorhandenen Helden; Szene, NGO-Prefabliste und Netzwerk-Prefab
   bleiben eins. Laterne prozedural (Würfel + Punktlicht) statt Blender-Prop. Kein Hauptmenü: Klassenwahl als
   Start-Overlay. „Lantern of the Hold“ nicht in `GetUniques` (P2-Vertrag drei Uniques, Vendor-Seeds).

## Partial-Manifest

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 class-data | Klassenkatalog, Save-Feld, Migration | `Scripts/Classes/ClassDefinition.cs` (neu), `Scripts/Classes/ClassCatalog.cs` (neu), `Scripts/Save/SaveGame.cs`, `Scripts/Save/SaveMigrations.cs`, `Scripts/Save/SaveService.cs`, `Tests/EditMode/ClassCatalogTests.cs` (neu), `Tests/EditMode/SaveMigrationTests.cs` |
| S2 skills | Vier Lanternbearer-Skills, Wisp-Begleiter, Netz-Kosmetik | `Scripts/DiabloSkills.cs` (Enum-Erweiterung, Dispatch über Klasse), `Scripts/Classes/LanternSkills.cs` (neu), `Scripts/Classes/Wisp.cs` (neu), `Scripts/DiabloNetworkPlayer.cs` (Remote-Cast neuer Skills), `Editor/SkillArtBuilder.cs` (4 Icons), `Tests/EditMode/LanternSkillFormulaTests.cs` (neu), `Tests/PlayMode/LanternSkillTests.cs` (neu) |
| S3 sheet+ui | Klassenabhängige Ränge und Character-UI | `Scripts/Progression/CharacterSheet.cs`, `Scripts/Progression/DiabloCharacterUI.cs` (Buttons aus Klassenkatalog statt fest), `Tests/EditMode/CharacterSheetTests.cs`, `Tests/EditMode/DiabloHdTests.cs` |
| S4 select+items | Klassenwahl, Held-Prefab, Item-Bindung, Drops | `Scripts/Menu/ClassSelectUI.cs` (neu), `Scripts/Menu/MainMenu.cs` bzw. Menü-Teil in `Scripts/DiabloProgression.cs`, `Editor/SandboxBuilder.cs` (`BuildDiabloZonePlayerPrefab` je Klasse), `Editor/KayKitCharacters.cs`, `Scripts/Items/ItemDatabase.cs` (Laternen-Basistypen, `classRestriction`), `Scripts/Items/Equipment.cs` (Anlege-Prüfung), `Scripts/Loot/DroptableDatabase.cs` (Klassen-Gewichtung), `Tests/EditMode/ClassItemTests.cs` (neu), `Tests/PlayMode/ClassSelectTests.cs` (neu) |
| S5 proof | Autoplay beider Klassen, Co-op gemischt, Shotcheck, Docs | `Scripts/SessionAutoplay.cs` (`-class`, Lanternbearer-Kampfprofil), `Scripts/DiabloShotCheck.cs` (Lanternbearer-Shots), `docs/second-class-run-log.md` (neu), `docs/one-page-gdd.md`, `docs/dev-journal.md`; dotfiles: `scripts/health_probes/diablo_unity.py`, `scripts/test_diablo_unity_health.py` |

S1 zuerst. S2 und S3 nach S1 parallel (disjunkt). S4 nach S2 und S3. S5 zuletzt.

## S1 — Klassendaten + Save

- `ClassCatalog.Fenwalker` beschreibt exakt den heutigen Stand (Skills, Tasten, Faktoren 1,0).
  `ClassCatalog.Lanternbearer` mit den Werten aus S2. `ClassCatalog.Get(id)` wirft bei unbekannter Id nicht,
  sondern liefert Fenwalker mit Log-Warnung.
- Save-Feld `classId`, Migration fehlendes Feld → `fenwalker`, Backup vor erstem Schreiben.
- Tests: Fenwalker-Katalog entspricht den heutigen Konstanten aus `DiabloSkills`/`CharacterSheet`
  (Regressionsanker), Migration der vollen Kette mit v1-Fixture, unbekannte Id fällt zurück.

## S2 — Lanternbearer-Skills

- Lantern Bolt: Projektil über `DiabloProjectile`, 1,2 s Cooldown, Basis 4 Schaden, durchdringt 1 Ziel.
- Wisps: 2 Begleiter für 20 s, umkreisen den Helden, greifen Ziele im Umkreis 6 m an (1 Schaden / 0,8 s),
  14 s Cooldown, Rang +1 Wisp ab Rang 3. Wisps sind Kosmetik auf Clients, Schaden nur bei lokaler Autorität.
- Searing Halo: Ring Radius 3,5 m, 3 Schaden + 0,6 m Knockback (Pop-Vertrag aus d2-combat-feel), 7 s Cooldown.
- Veil Step: Teleport 6 m in Blickrichtung, prüft Kollision per CapsuleCast, 5 s Cooldown.
- `DiabloSkills` wählt Skills und Tasten aus `ClassDefinition`. Fenwalker-Pfad unverändert.
- Tests: Schadens-/Cooldown-Formeln mit Rang und Focus, Wisp-Anzahl je Rang, Halo trifft im Radius,
  Veil Step stoppt vor Wand, Fenwalker-Formeln unverändert (`DiabloD2Feel*` grün).

## S3 — Charakterbogen + UI

- `CharacterSheet` führt Ränge für die Skills der aktiven Klasse, `SkillMultiplier` nutzt den
  Focus-Faktor aus `ClassDefinition`. Ränge fremder Klassen werden beim Laden ignoriert, nicht gelöscht.
- `DiabloCharacterUI` erzeugt Skill-Buttons aus dem Katalog statt `SpendFireball/SpendArrow/SpendCleave`.
- Tests: Punkte-Vergabe je Klasse, Save-Roundtrip mit Lanternbearer-Rängen, UI zeigt 3 bzw. 4 Skills,
  HD-Scaler-Test erweitert.

## S4 — Klassenwahl + Items

- `ClassSelectUI` bei „New Game“: zwei Karten mit Name, Kurzbeschreibung, Skill-Icons. „Continue“ lädt
  die gespeicherte Klasse ohne Auswahl.
- Held-Prefab je Klasse aus dem Builder (`Player_Fenwalker`, `Player_Lanternbearer`), Spawn wählt nach
  `classId`. Co-op-Host spawnt das Prefab der jeweiligen Client-Klasse (NGO-Prefab-Liste erweitern).
- Item-Bindung: `Equipment.CanEquip(item, classId)`, Tooltip zeigt Klassen-Hinweis rot. Drop-Gewichtung
  70/30 laut Entscheid 4.
- Tests: Auswahl setzt `classId`, Continue überspringt Auswahl, Laterne nicht für Fenwalker anlegbar,
  Drop-Verteilung über 1000 Würfe im Toleranzband.

## S5 — Beweis

- `SessionAutoplay -class lanternbearer` mit eigenem Kampfprofil (Halo bei ≥ 3 Gegnern, sonst Bolt, Wisps
  auf Cooldown). Nachtlauf führt beide Klassen, Ergebnis-JSON trägt `classId` und Clear-Zeit.
- Abnahme: Clear-Zeit Lanternbearer innerhalb ±25 % Fenwalker bis zum letzten verfügbaren Boss (Hollow
  Abbot, nach act-one zusätzlich Kilnmother). Abweichung → Zahlen in S2 justieren, nicht Fenwalker.
- Co-op-Smoke: Host Fenwalker + Client Lanternbearer, Join, Kampf, Disconnect, 0 Exceptions (Muster aus
  `unity-diablo-d2-combat-feel`).
- Shotcheck: Klassenwahl, Lanternbearer mit Wisps und Halo im Kampf. Windows über `stage-windows.sh` →
  `shotcheck-windows.sh`, PNGs im Run-Log.
- dotfiles-Worktree: `G-DIABLO02`-Vertrag um beide Held-Prefabs, `G-DIABLO06` bewertet Läufe je Klasse.
  `check`, Test, `land`.
- Akzeptanz: alle 6 Goal-Kriterien belegt, `PROGRESS.md` auf 100 %.

### Statustabelle

| Stufe | Abhängig von | Gewicht | Status |
|---|---|---|---|
| S1 class-data | Startbedingung | 15 % | landed 2026-11-18 |
| S2 skills | S1 | 30 % | landed 2026-11-18 |
| S3 sheet+ui | S1 | 15 % | landed 2026-11-18 |
| S4 select+items | S2, S3 | 20 % | landed 2026-11-18 |
| S5 proof | S4 (Zone-3-Abnahme: act-one 100 %) | 20 % | landed 2026-11-18 |

## Verify (je Stufe)

1. `scripts/run-unity-tests.sh` grün, `DiabloD2FeelTests` und `DiabloD2FeelLoopTests` unverändert grün.
2. `VerifyDiabloZone` grün (ab S4 beide Held-Prefabs), `KayKitAssets.Verify` (`repo.yaml:verify`).
3. `bash scripts/run-autoplay.sh` Exit 0, ab S5 je Klasse.
4. `python3 scripts/check-meta.py` ohne Befund.
5. dotfiles-Anteil (S5): `check` + `python3 -m pytest scripts/test_diablo_unity_health.py -q`, nach `land`
   `python3 ~/scripts/repo_health_collect.py` → `G-DIABLO02/03/06` grün.

## Risiken

- R1 Enum-Erweiterung verschiebt Netz-Werte. Gegenmittel: neue Werte nur am Ende, Test auf feste
  int-Werte der drei Fenwalker-Skills.
- R2 `DiabloSkills.cs` wird zum zweiten Monolithen. Gegenmittel: Lanternbearer-Logik in `LanternSkills`,
  `DiabloSkills` dispatcht nur.
- R3 Wisps verursachen Co-op-Desync. Gegenmittel: Schaden nur bei lokaler Autorität, Clients zeigen Kosmetik.
- R4 Kollision mit coop S2 (`DiabloSkills.cs` Treffer-ServerRpc) und presentation S3 (VFX-Hooks in
  `DiabloSkills.cs`). Gegenmittel: S2 erst starten, wenn keine Lock-Claims dieser Stufen offen sind
  (`agent-workspace.py lock list`).
- R5 Balance mit `-autoplay-god` nur grob. Clear-Zeit ist Durchsatz-Indiz, Balance-Urteil bleibt
  Eigner-Playtest.
