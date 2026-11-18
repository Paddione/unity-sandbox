---
goal: cinderreach-skill-variants
slug: cinderreach-skill-variants
health_goals: [G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-skill-variants/tasks.md]
---

# Skill-Varianten mit spürbaren Trade-offs

## Objective

Fenwalker und Lanternbearer erhalten je eine Wahl, die Positionierung und Timing im Kampf verändert.

## Erfolgskriterien

1. Fenwalker wählt Rundumschlag oder einen weiter reichenden Frontalschlag für Cleave. Lanternbearer wählt eine mitlaufende Aura oder eine länger anhaltende ortsfeste Feuerzone für Searing Halo.
2. Der Charakterbogen erklärt Trefferfläche, Reichweite und Dauer. Ein Wechsel im Lantern Hold bleibt nach Reload aktiv, und alte Spielstände starten mit der bisherigen Variante.
3. PlayMode-Tests belegen Zielauswahl und räumliches Verhalten. Ein manueller Lauf pro Klasse vergleicht beide Varianten mit derselben Ausrüstung an zwei passenden Kampfsituationen.
4. Unity-Suiten und der vollständige Akt-I-Autoplay sind grün. Die neuen Systemtests belegen die Ziele unabhängig vom regulären Akt-I-Lauf.

## Scope und Nicht-Ziele

Ein vorhandener Skill pro Klasse, bestehende Ränge und Tasten. Keine zusätzlichen Klassen, kein neuer Skill-Baum und keine Co-op-Replikation der Varianten.

## Abhängigkeiten

Empfohlener Start nach `cinderreach-smithing`. Der [Stufenplan](../../plans/cinderreach-skill-variants/tasks.md) beschreibt die Integrationspunkte und Nachweise.
