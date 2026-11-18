---
goal: cinderreach-second-class
slug: cinderreach-second-class
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-second-class/tasks.md]
---

# Goal: cinderreach-second-class — zweite spielbare Klasse

Angelegt: 2026-11-18. Herkunft: Eigner-Auftrag „come up with new milestones for our unity game“ (setgoal).
Folge-Goal #6, Reihenfolge siehe `.agents/goals/ROADMAP.md`.

## Objective

Eine zweite spielbare Klasse, der **Lanternbearer** (Licht-Caster mit Wisp-Begleitern), mit eigenem Skill-Baum, über dieselbe
Attribut- und Skillpunkt-Progression wie die bestehende Klasse, die als **Fenwalker** unverändert bleibt.

## Erfolgskriterien

1. Klassenwahl-Screen bei neuem Charakter, Klasse im Save geführt. Ein Save mit alter Version wird als
   Nahkampfklasse migriert. Beleg: EditMode-Migrationstest + PlayMode-Auswahltest.
2. Die neue Klasse hat ≥ 4 Skills mit eigenen VFX und eigenem Sound sowie eigene Attribut-Skalierung.
   Beleg: EditMode-Tests auf Schadens- und Kostenformeln.
3. Gegenstände klassenspezifisch nutzbar, Drops beachten die Klasse. Beleg: EditMode-Test.
4. Autoplay läuft mit beiden Klassen bis zum letzten verfügbaren Boss. Die Clear-Zeit der neuen Klasse
   liegt innerhalb ±25 % der Nahkampfklasse. Beleg: Autoplay-Log unter `G-DIABLO06`.
5. Co-op mit gemischten Klassen: Join, Kampf, Disconnect ohne Exception. Beleg: Join-Smoke wie in
   `unity-diablo-d2-combat-feel`.
6. Windows-Build gestaged, Shotcheck-PNG der neuen Klasse im Kampf.

## Nicht-Ziele

- Keine dritte Klasse, kein Respec-Händler, kein PvP.
- Keine neuen Zonen (eigenes Goal `cinderreach-act-one`), kein Steam-Build.

## Abhängigkeiten

- Start erst nach `cinderreach-loop-guard` 100 % (Autoplay als Regressionsnetz). Kann parallel zu
  `cinderreach-act-one` laufen; Balance-Abnahme in Zone 3 setzt dessen Abschluss voraus.
- Bestehende Nahkampf-Balance (Cleave, Knockback-Vertrag) bleibt unverändert, Beleg: `DiabloD2Feel*`-Tests grün.
- Session-Protokoll aus `.agents/plans/arpg-session-loop/tasks.md`, Unity 6000.0.85f2, `land: ff`.
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
