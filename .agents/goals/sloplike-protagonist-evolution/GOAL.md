# Goal: sloplike-protagonist-evolution

health_goals: [G-DIABLO02]

## Objective
Der Protagonist startet als ganz normaler Junge oder normales Mädchen ohne Klasse. Nach dem Sieg über den Akt-I-Boss (Musk) entwickelt er sich in einer Story-Szene zu einer der drei Klassen. Bestehende Saves spielen ohne Bruch weiter.

## Ist-Befunde
- B1 Klassenwahl heute direkt beim Start in `ClassSelectUI` (Klasse → Gender/Frisur/Haarfarbe/Build/Name), `HeroProfile` in `RunProfile.Hero`.
- B2 `SaveGame.CurrentVersion = 17` (`sandbox/Assets/Scripts/Save/SaveGame.cs`).
- B3 Klassen-Kits mit je 6 Skills (Q/E/R/Leertaste/F/G), `ClassAppearance.SkinModelPath` wählt Basismodelle je Klasse/Gender.

## Entscheidungen
- D1 (Eigner 2026-11-28) Evolution nach dem Akt-I-Boss.
- D2 (Eigenentscheidung) Vor der Evolution: eigene „Normalo“-Modelle (Junge/Mädchen, Alltagskleidung) und ein kleines Basis-Kit; Charaktererstellung ohne Klasse; Migration v17→v18 setzt bestehende Helden als bereits entwickelt.
- D3 (Eigner) Redesign der drei Klassen passend zur neuen Story folgt später als eigenes Goal.

## Erfolgskriterien
1. Neue Kampagne startet klassenlos mit Normalo-Modell (Junge/Mädchen) — Nachweis: PlayMode-Test, Windows-Shot.
2. Nach dem Akt-I-Boss öffnet sich die Klassenwahl als Story-Szene, danach gilt das Klassen-Kit — Nachweis: PlayMode-Test, Autoplay `-seed 0` Exit 0 bis `complete`.
3. v17-Saves laden als entwickelte Helden — Nachweis: EditMode-Migrationstest v17→v18.
4. Koop bleibt funktionsfähig — Nachweis: Koop-Autoplay Host+Client Exit 0.

## Nicht-Ziele
- Redesign der drei Klassenmodelle für die neue Story.

## Abhängigkeiten
- `sloplike-story-rewrite`.
