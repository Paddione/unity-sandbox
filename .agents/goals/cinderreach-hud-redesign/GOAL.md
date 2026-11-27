# Goal: cinderreach-hud-redesign

health_goals: [G-DIABLO01, G-DIABLO03, G-DIABLO05]
plan_refs: [.agents/plans/cinderreach-hud-redesign/tasks.md]

Ausgangslage: [`.agents/goals/CINDERREACH.md`](../CINDERREACH.md). Design-Vorlage: Canvas
https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard „HUD im Spiel“ (`Main.dc.html`).

## Objective
Das Spiel-HUD sieht aus wie das Artboard „HUD im Spiel“: Lebenskugel mit Flüssigkeitsstand und Glanz, Skillleiste mit
Tastenbeschriftung, Namen, Cooldown-Sweep und Flaschenladungen, EP-Leiste, Goldfeld, Zielbalken mit Tier-Plakette, Minimap
mit Zonenname und Fuhrbuch-Tracker im Panelstil. Neu sind Gruppenrahmen: eigener Rahmen oben links (Klassenfarbe, Stufe,
Leben, Schild), darunter je Mitspieler Name, Klasse, Stufe und Leben. Schriften und Rahmen sind als gemeinsame Bausteine
nutzbar, damit Inventar und Karte sie übernehmen.

## Ist-Befunde
- B1 UI ist uGUI mit Legacy-`Text`; `DiabloUIFactory.DefaultFont` lädt nur `LegacyRuntime.ttf`, unter `sandbox/Assets` liegt keine `.ttf`/`.otf`.
- B2 Farben stehen zentral in `sandbox/Assets/Scripts/DiabloPalette.cs` (Panel #14101A, PanelEdge #3A2E22, TrimGold #F2B134, Parchment #E8D9BE, Blood #B31217, Klassenakzente, Raritätsfarben).
- B3 HUD-Teile: `DiabloLifeGlobe.cs` (Kugel, Todesschleier, Goldzeile `hud.globe.purse`), `SkillBarHud.cs`, Flaschenslot mit Cooldown-Sweep, `DiabloTargetBar.cs` (Tier-Farbe, Bossbalken), `Map/MinimapHud.cs`, `Story/StoryQuestUI.cs` (Tracker bis 4 Aufgaben).
- B4 Gruppenrahmen existieren nicht (kein `PartyFrame`/`PartyHud` unter `sandbox/Assets/Scripts`).
- B5 Laufzeit-UIs nutzen den HD-Scaler 1920×1080, match 0,5 (`DiabloHdTests`).
- B6 Sichtprüfung nur per Windows-Shotcheck (`scripts/shotcheck-windows.sh`, WIP-Stage `Games\cinderreach-wip`); WSL rendert nicht (`-nographics`).

## Entscheidungen
- D1 Drei Goals (HUD → Inventar → Karte); dieses zuerst, es liefert die gemeinsamen Stilbausteine (Eigner, 2026-11-27).
- D2 Gruppenrahmen gehören in dieses Goal (Eigner, 2026-11-27).
- D3 Stil übernehmen, Bewährtes behalten: bestehende Logik (Cooldowns, Tier-Farben, Tracker-Inhalte, Todesschleier) bleibt, nur Darstellung und Anordnung ändern sich (Eigner, 2026-11-27).
- D4 Schriften Cinzel (Überschriften, Zahlen) und Alegreya Sans (Fließtext) als OFL-Fonts unter `sandbox/Assets/Resources/Fonts` mit `OFL.txt`; `DiabloUIFactory` bekommt `DisplayFont`/`BodyFont`. Begründung: Legacy-`Text` bleibt, kein TMP-Umstieg nötig.
- D5 Neue Farben aus dem Mockup (Schild #8CB7E0, Abklingzeit-Abdunklung, aufgehelltes Magisch #6E9BF2, Effektorange #FF9A4D) nur in `DiabloPalette.cs`.
- D6 Gruppenrahmen lesen bereits replizierte Werte (Leben, Stufe, Klasse, Name über `LootDrop.PrettyName`); keine neue Save-Migration.

## Erfolgskriterien
1. Fonts eingebunden, `.meta` vollständig — Nachweis: `python3 scripts/check-meta.py` grün, EditMode-Test `HudThemeTests` prüft, dass `DisplayFont`/`BodyFont` nicht der Builtin-Font sind.
2. HUD-Elemente nach Artboard (Kugel, Skillleiste inkl. Namen/Ladungen/Sweep, EP-Leiste, Goldfeld, Zielbalken mit Plakette, Minimap-Rahmen mit Zonenname, Tracker-Panel) — Nachweis: PlayMode-Test `HudLayoutPlayTests` prüft Existenz, Anker und Mindestgrößen (Buttons ≥ 44 px) bei 1920×1080.
3. Gruppenrahmen zeigen im Koop jeden Mitspieler mit Name, Klasse, Stufe und Lebensanteil und aktualisieren bei Schaden — Nachweis: PlayMode-Test `PartyFramesPlayTests` (Host + Client), Team-Autoplay `scripts/run-autoplay.sh` ohne neue Fehler (G-DIABLO06).
4. Voll-Suiten grün — Nachweis: `bash scripts/run-unity-tests.sh` (EditMode + PlayMode), Zeilen in `docs/test-runs.jsonl` mit `failed == 0`.
5. Windows-Shots `hud_solo`, `hud_party`, `hud_target_elite` zeigen das neue HUD — Nachweis: `bash scripts/shotcheck-windows.sh` bei 1920×1080, PNGs in `artifacts/shotcheck`, Vergleichsbild Shot ↔ Artboard unter `docs/hud-redesign/`.
6. Keine Performance-Regression — Nachweis: `fps_windows` aus `autoplay-result.json` auf Branch ≥ 95 % von `main` (gleicher Seed).

## Nicht-Ziele
- Inventar und Karte (eigene Goals `cinderreach-inventory-redesign`, `cinderreach-map-overlay`).
- Umstieg auf TextMeshPro oder UI Toolkit.
- Neue Spielmechaniken (Ressourcenkugel, neue Buffs).

## Abhängigkeiten
- Keine.
