---
goal: diablo-ability-showcases
slug: diablo-ability-showcases
health_goals: [G-DIABLO02, G-DIABLO07, G-GOALS01]
plan_refs: []
---

# Goal: diablo-ability-showcases — Ability-Cutscenes für alle Boss-Encounter

Angelegt: 2026-11-27. Herkunft: Eigner-Anfrage „generate cutscenes for every endboss encounter that highlight their abilities", geklärt per `setgoal` (alle 3 Bosse, Intro + Phase-2-Showcase, echte schadlose Demo-Casts + Text-Einblendung, 8–12 s / ca. 4 s skipbar einmal pro Run, keine neuen Voice-Aufnahmen, `G-DIABLO07` erweitern). Basis: Goal `diablo-cutscene-pipeline` (100 %, In-Engine-`CutsceneDirector` mit 4,5-s-Boss-Intros).

## Objective

In `unity-sandbox` (Cinderreach, `sandbox/`) den bestehenden In-Engine-`CutsceneDirector` so erweitern, dass jede der drei Bossbegegnungen ihre tatsächlichen Fähigkeiten inszeniert vorführt: Hollow Abbot (`BossController`), Kilnmother (`KilnmotherController`, Akt-I-Endboss) und Glasswright (`GlasswrightController`, Akt-II-Endboss). Jeder Boss erhält (a) ein auf 8–12 s verlängertes Intro, das seine Phase-1-Fähigkeiten je einmal schadlos demonstriert, und (b) einen ca. 4 s langen Phase-2-Showcase beim Übergang bei ≤ 50 % HP. Zu jedem Demo-Cast erscheint eine kurze lokalisierte Text-Einblendung (Name/Tell der Fähigkeit). Die endgültige Fähigkeitenliste wird aus den drei Controllern abgeleitet, nicht erfunden.

## Erfolgskriterien

1. Intro-Showcases: Pro Boss demonstriert das Intro jede Phase-1-Fähigkeit genau einmal als echten, aber schadlosen Cast (kein Schaden, keine bleibenden Adds/Felder/Projektile nach Sequenzende); Kampf beginnt erst nach Sequenzende oder Skip.
2. Phase-2-Showcases: Beim ersten `EnterPhaseTwo` pro Boss läuft einmalig eine ca. 4 s Sequenz, die die Phase-2-Fähigkeit zeigt; die echten Phase-2-Effekte (Summons/Pillars) bleiben bestehen, der Boss hält während der Sequenz still (`HoldBosses`), der Held behält die volle Kontrolle (kein Freeze inmitten frisch beschworener Adds).
3. Vertrag unverändert: jederzeit skipbar (Space/Esc/Klick) mit Kamera-/Steuerungs-Restore, genau einmal pro Run (`ResetRun`-Seam), kein Trigger bei totem Boss, Bosse halten während Sequenzen still (`HoldBosses`).
4. Text & Audio: neue lokalisierte Einblendungs-Keys pro Fähigkeit im bestehenden Loc-Katalog; keine neuen Voice-Aufnahmen oder Voice-Keys (bestehende Roars/Barks, Voice-Gate `scripts/check-voice-lines.py` bleibt grün).
5. Belege: EditMode-Tests (Trigger, Einmaligkeit, Skip/Restore, Schadlosigkeit der Demo-Casts, Phase-2-Showcase je Boss) grün; `G-DIABLO07`-Probe (Dotfiles "scripts/health_probes/diablo_unity.py") um Ability-Showcase-Anker erweitert inkl. Tests; übrige `repo.yaml:verify`-Suites grün.

## Scope

- `sandbox/Assets/Scripts/Story/CutsceneDirector.cs`, ggf. `sandbox/Assets/Scripts/Story/StoryDirector.cs`
- `sandbox/Assets/Scripts/Enemies/BossController.cs`, `sandbox/Assets/Scripts/Enemies/KilnmotherController.cs`, `sandbox/Assets/Scripts/Enemies/GlasswrightController.cs` (nur Demo-/Showcase-Hooks, keine neuen Fähigkeiten)
- Lokalisierungs-Katalog für neue Einblendungs-Keys
- EditMode-Tests unter `sandbox/Assets/Tests/`; Health-Probe in Dotfiles-Repo "scripts/health_probes/diablo_unity.py" + Dotfiles-Test `test_diablo_unity_health`

## Nicht-Ziele

- Keine vorgerenderten Videos, kein Unity-Timeline/Cinemachine, keine neuen Packages
- Keine neuen Boss-Fähigkeiten, keine Balance-Änderungen, keine neuen Voice-Aufnahmen
- Keine Änderung an bestehenden Dialog-Highlights; keine Death-/Outro-Sequenzen

## Abhängigkeiten

- `CutsceneDirector` (Boss-Intros, `HoldBosses`, Skip-Pfad), `StoryDialogUI`, `StoryVoice`, drei Boss-Controller, Arena-Trigger
- "scripts/health_probes/diablo_unity.py" (`G-DIABLO07`), Dotfiles-Test `test_diablo_unity_health`, "ci-map.yaml" (Dotfiles-Repo `~`)

health_goals: [G-DIABLO02, G-DIABLO07, G-GOALS01]
