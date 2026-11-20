---
goal: cinderreach-menu-flow-polish
slug: cinderreach-menu-flow-polish
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
---

# Goal: cinderreach-menu-flow-polish — Spielfluss, Menü-Komponenten und UI-Art iterieren

Angelegt: 2026-11-21. Herkunft: Eigner-Auftrag „iterate on the gameplay flow and UI Menu
Components and art elements of our unity game“. Ausgangslage: `.agents/goals/CINDERREACH.md`
und der ungelandete Branch `feature/cinderreach-menu-art-polish` (9-Slice-UI, Blender-Sprites,
Startbildschirm, Pause-Menü; Stand vor itemization S5/party-team).

## Objective

Den Menü-/Art-Branch auf aktuellen `main` bringen und landen, danach in Iterationen
Spielfluss (Start → Klasse → Spiel → Pause → Ende), Menü-Komponenten und UI-Art
sichtbar verbessern. Jede Iteration ist per Windows-Shotcheck belegt.

## Erfolgskriterien

1. Branch auf `main` rebased: Koop-Handel, Hinweise, Klick-außerhalb-Schließen, Skill-Varianten,
   Itemization-S5-Aktionen bleiben erhalten; EditMode/PlayMode grün.
2. Genau ein Startbildschirm im Spiel (kein Legacy-Titel über der Klassenwahl).
3. Alle drei Klassen haben Portrait, Akzentfarbe und korrekte Kartendaten; jeder Skill hat ein Icon
   (`DiabloMenuPolishTests.SkillIcons_ResolveEveryClassSkill`).
4. Shotcheck mit `start_screen`, `start_settings` und allen In-Game-Panels ohne Überlauf/Anschnitt.
5. `KayKitAssets.Verify`, `python3 scripts/check-meta.py`, Autoplay `-seed 0` mit FPS-Gate grün.

## Nicht-Ziele

Neue Spielmechanik, Save-Migration, Asset-Store-Käufe, Netzprotokoll-Änderungen.
