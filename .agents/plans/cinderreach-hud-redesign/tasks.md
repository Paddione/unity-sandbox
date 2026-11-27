---
goal: cinderreach-hud-redesign
slug: cinderreach-hud-redesign
branch: feature/cinderreach-hud-redesign
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO03, G-DIABLO05]
priority: P9 (liefert Fonts/Stilbausteine für cinderreach-inventory-redesign und cinderreach-map-overlay)
status: planned
plan_refs: [.agents/goals/cinderreach-hud-redesign/GOAL.md]
---

# Plan: cinderreach-hud-redesign — HUD nach Canvas-Artboard „HUD im Spiel“

Pfad: feature, ein Branch, sechs Partials (P1–P6) in Abhängigkeitsreihenfolge. Abschluss per
`python3 ~/scripts/agent-workspace.py land` (`land: ff`). Vorlage: Canvas
https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard `Main.dc.html` (1920×1080).

## Goal

Siehe [GOAL.md](../../goals/cinderreach-hud-redesign/GOAL.md), Erfolgskriterien 1–6, Entscheidungen D1–D6.

## Ausgangslage (HEAD `cae286b`, 2026-11-27)

- Lebenskugel, Goldkarte und Flaschenslot: `sandbox/Assets/Scripts/DiabloLifeGlobe.cs` (Kugel 120 px unten links bei (18,16), Statuskarte 310×34 bei (158,22), Flasche 56 px bei (158,62), eigenes `MakeText` mit `LegacyRuntime.ttf`). Spawnt `DiabloLootLabels` und `HeroNameplates`.
- Skillleiste: `SkillBarHud.cs` (Canvas `SkillBarCanvas`, Platte 880×100, Slots 50 px in `SkillSlotsRow`, Veil statt Sweep, Textzeile `hud.skillbar.thrust`). Slots tragen `HoverInfoSource`.
- EP-Leiste: `DiabloProgression.cs` `XpCanvas` (Sortierung 6), `xpFill` horizontal.
- Zielbalken/Bossbalken: `DiabloTargetBar.cs` (360×54 oben Mitte, `TierColor(LootTier)`, eigenes `LegacyRuntime.ttf`).
- Minimap: `Map/MinimapHud.cs` (`SizePx` 220, Panel oben rechts (-14,-40), Zonenlabel unten).
- Quest-Tracker: `Story/StoryDirector.cs` Zeile ~59 (`StoryTracker`-Text auf eigenem Canvas, `TrackerText(...)` statisch).
- Gruppe: `DiabloNetworkPlayer.Live`, `ShownName`, `classIndex` (NetworkVariable), `hpState` (Vector2 cur/max, Owner-Write). Stufe ist **nicht** repliziert (nur Join-Report `level`).
- Fonts: keine `.ttf`/`.otf` unter `sandbox/Assets`; `DiabloUIFactory.DefaultFont` = Builtin.
- Tests: `DiabloHdTests`/`DiabloHdPlayTests` (Scaler), `GearSlotsPlayTests` (Flaschen-Seams `TestFlaskSweep`/`TestFlaskText`), `GroupQuestMapPlayTests` (Minimap), `DiabloMenuPolishTests`.

## File Structure

| Partial | Dateien (disjunkt) |
|---|---|
| P1 Theme | `sandbox/Assets/Resources/Fonts/{Cinzel-Bold.ttf,AlegreyaSans-Regular.ttf,AlegreyaSans-Bold.ttf,OFL-Cinzel.txt,OFL-AlegreyaSans.txt}` (+ `.meta`), `DiabloUIFactory.cs`, `DiabloPalette.cs`, neu `HudChrome.cs` |
| P2 Unten | `DiabloLifeGlobe.cs`, `SkillBarHud.cs`, `DiabloProgression.cs` (nur XP-Build/Layout) |
| P3 Oben | `DiabloTargetBar.cs`, `Map/MinimapHud.cs`, `Story/StoryDirector.cs` (nur Tracker-Build) |
| P4 Gruppe | neu `PartyFramesHud.cs`, `DiabloNetworkPlayer.cs` (NetworkVariable `heroLevel`), Spawn in `DiabloLifeGlobe.Awake` (Ein-Zeilen-Ergänzung nach P2), `Localization/LocCatalog.Hud.cs` |
| P5 Tests/Shots | `sandbox/Assets/Tests/EditMode/HudThemeTests.cs`, `sandbox/Assets/Tests/PlayMode/HudLayoutPlayTests.cs`, `sandbox/Assets/Tests/PlayMode/PartyFramesPlayTests.cs`, `DiabloShotCheck.cs` (Posen) |
| P6 Verify | `docs/hud-redesign/*`, `docs/test-runs.jsonl`, Goal-/CINDERREACH-Doku |

## P1 — Theme-Bausteine

1. Fonts holen (OFL, Google-Fonts-Repo): `Cinzel` (statische Bold-Instanz) und `AlegreyaSans-Regular/Bold` nach `sandbox/Assets/Resources/Fonts/`, Lizenztexte daneben. Ist Cinzel nur als Variable-Font verfügbar, statische Bold-Instanz per `fonttools varLib.instancer` erzeugen.
2. `DiabloUIFactory`: `DisplayFont` (Cinzel) und `BodyFont` (Alegreya Sans) über `Resources.Load<Font>("Fonts/...")`, Fallback auf `DefaultFont` mit `Debug.LogWarning`. `MakeText(..., display: false)` Parameter ergänzen (Default Body). Bestehende Aufrufer bleiben kompatibel.
3. `DiabloPalette`: `Shield` #8CB7E0, `CooldownVeil` (0.04,0.035,0.063,0.78), `MagicText` #6E9BF2, `EffectEmber` #FF9A4D, `PartyFenwalker`=FenSteel, Rarity unverändert.
4. Neu `HudChrome` (static): `Panel(parent, name, size)` = Panel #14101A 0.9 + 1 px Bronzerand + optionaler Gold-Oberkante (`goldTop`), `Badge(...)` für Plaketten, `Bar(parent, size, color, out Image fill)` mit Filled-Image. Alle Raycast-frei außer explizit.
- Verify: `unity --json recompile "$PWD/sandbox"` ohne Fehler; `python3 scripts/check-meta.py`.

## P2 — Unterer Block (Kugel, Skillleiste, EP, Gold, Menütasten)

1. `DiabloLifeGlobe`: Kugel 176 px mit 5 px Bronzerand und Gold-Ring, Glanzfleck oben links, Zahl in `DisplayFont` 26 px + Unterzeile „LEBEN“ (neuer Loc-Key `hud.globe.life`). Position: links der Skillleiste, unten Mitte-Block. Schild-Chip über der Kugel (sichtbar wenn `PlayerHealth` Schild > 0; Seam `TestShieldChipShown`). Goldkarte rechts der Skillleiste (Label `GOLD`, Betrag `DisplayFont` 22, Gold #FACC47) plus 2×2 Menütasten (I Tasche, C Figur, M Karte, Esc Pause) als echte `Button`s ≥ 44 px, die die vorhandenen Toggle-Pfade aufrufen. Punkte-Hinweis `hud.globe.points` bleibt als Zeile unter dem Gold. Flaschenslot wandert als letzter Slot in die Skillleiste (siehe 2), Seams `TestFlaskSweep`/`TestFlaskText` bleiben auf `DiabloLifeGlobe` erhalten.
2. `SkillBarHud`: Slots 72 px, Taste oben links (Gold, Bold), Skillname darunter (13 px, ParchmentDim), Cooldown als radialer Sweep (`Image.Type.Filled`, Radial360) + Restsekunden groß in `DisplayFont`; Akzentrand für den Primärskill in Klassenfarbe. Platte: `HudChrome.Panel` mit Gold-Oberkante, Breite aus Slotanzahl. Textzeile `SkillBar` bleibt als unsichtbarer Test-/Fallback-Text (bestehende Tests lesen `Line()`). Flaschen-Slot-Anker `FlaskSlotAnchor` als letztes Kind, in den `DiabloLifeGlobe` sein Flaschen-Frame umhängt.
3. `DiabloProgression` XP: Leiste 8 px innerhalb der Skillplatte unter den Slots, Beschriftung links `Stufe N`, rechts `x / y EP` (Loc-Keys `hud.xp.level`, `hud.xp.amount`).
- Verify: `PLATFORMS=PlayMode TEST_FILTER=GearSlotsPlayTests bash scripts/run-unity-tests.sh` grün (Flaschen-Seams).

## P3 — Oberer Block (Ziel, Minimap, Tracker)

1. `DiabloTargetBar`: Name in `DisplayFont` 22 px in Tier-Farbe, Plakette rechts daneben mit Tier-Text (`Elite`/`Boss`/`Aktboss`, Loc `hud.tier.*`, nicht bei Normal), Balken 600×18 mit Prozentzahl mittig, Statuszeile darunter (verschlammt/vergiftet aus `EnemyStatus`, falls vorhanden; sonst leer). Bossbalken analog im selben Stil.
2. `MinimapHud`: runder Rahmen (Kreismaske über `Mask` + Kreissprite, Bronzering + Goldlinie), Zonenname in `DisplayFont` versal unter der Karte. `SizePx` bleibt 220 (Tests).
3. `StoryDirector`-Tracker: `HudChrome.Panel` 300 px breit unter der Minimap, Kopf „FUHRBUCH“ + Tastenhinweis J, Inhalt weiter aus `TrackerText(...)`.
- Verify: `TEST_FILTER=GroupQuestMapPlayTests` grün.

## P4 — Gruppenrahmen

1. `DiabloNetworkPlayer`: `NetworkVariable<int> heroLevel` (Everyone read, Owner write), Owner setzt bei Spawn und `DiabloProgression`-Level-Up.
2. Neu `PartyFramesHud` (MonoBehaviour, Canvas Sortierung 4, raycast-frei): eigener Rahmen oben links (Portrait-Kreis in Klassenfarbe mit `DiabloMenuArt`-Klassenportrait, Name, Klasse, `St. N`, Lebensbalken + Schildanteil), darunter je weiterem `DiabloNetworkPlayer.Live` ein kleiner Rahmen (Name, Klasse · St., Lebensbalken aus `hpState`), max. 4. Solo: nur eigener Rahmen (Level aus `DiabloProgression`). Tote Mitspieler gedimmt. Pure Funktion `PartyFramesHud.Rows(...)` liefert die Zeilen (testbar).
3. Spawn in `DiabloLifeGlobe.Awake` analog `HeroNameplates`.
- Verify: `TEST_FILTER=PartyFramesPlayTests` grün.

## P5 — Tests und Shot-Posen

1. `HudThemeTests` (EditMode): `DisplayFont`/`BodyFont` ≠ Builtin, `HudChrome.Panel` liefert Rand + Hintergrund, `PartyFramesHud.Rows` sortiert eigenen Helden zuerst und begrenzt auf 4.
2. `HudLayoutPlayTests` (PlayMode, 1920×1080): Kugel, Skillslots (≥ 72 px), Flaschenslot in der Leiste, EP-Leiste, Goldkarte, Menütasten (≥ 44 px, klickbar), Zielbalken-Plakette bei Elite, Minimap-Maske, Tracker-Panel existieren und liegen in den erwarteten Bildschirmquadranten.
3. `PartyFramesPlayTests` (PlayMode, Host + Client wie bestehende Netz-PlayTests): zweiter Held erscheint mit Name/Klasse/Stufe, Lebensbalken folgt `hpState` nach Schaden.
4. `DiabloShotCheck`: Posen `hud_solo`, `hud_party` (Begleiter/Mitspieler), `hud_target_elite`.
- Verify: gefilterte Läufe je Testklasse grün.

## P6 — Verifikation und Abschluss

1. `bash scripts/run-unity-tests.sh` (EditMode + PlayMode) grün, Zeilen in `docs/test-runs.jsonl`.
2. `python3 scripts/check-meta.py`, `python3 -m pytest scripts -q`, `KayKitAssets.Verify` (repo.yaml verify).
3. Windows-Build + Stage WIP + `SHOTCHECK_SIZE=1920x1080 bash scripts/shotcheck-windows.sh`; Shots `hud_*` nach `docs/hud-redesign/`, Vergleichsbild Shot ↔ Artboard (nebeneinander, `docs/hud-redesign/compare.png`).
4. Autoplay A/B `-seed 0` main vs. Branch auf Windows, `fps_windows` Branch ≥ 95 % main; Werte in `docs/hud-redesign/perf.md`.
5. Team-Autoplay ohne neue Fehler (G-DIABLO06).
6. `PROGRESS.md` auf 100 % mit Belegen, Stand in `.agents/goals/CINDERREACH.md` (HUD-Abschnitt) einarbeiten, `land`.
