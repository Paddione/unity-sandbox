---
goal: cinderreach-replayability
slug: cinderreach-replayability
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-replayability/tasks.md]
---

# Goal: cinderreach-replayability — Wiederspielwert nach dem Boss

Angelegt: 2026-11-17. Herkunft: Eigner-Auftrag „come up with the next goals“ nach `arpg-session-loop`.
Folge-Goal, Reihenfolge siehe `.agents/goals/ROADMAP.md`.

## Objective

Nach dem ersten Boss-Kill gibt es Gründe weiterzuspielen: eine zweite Schwierigkeitsstufe mit besseren
Drops, variierende Zonen-Layouts, ein tieferes Affix-System und eine Truhe in Lantern Hold. Die
20-Minuten-Session wird damit zu einer wiederholbaren Schleife.

## Erfolgskriterien

1. Schwierigkeitsstufe „Kindled“ nach Boss-Kill freigeschaltet: Gegner-HP/-Schaden ×1,8, neue Affix-Tier,
   Unique-Chance verdoppelt. Gespeichert im Save. Beleg: EditMode-Skalierungstests + PlayMode-Freischalttest.
2. Ashen Fen mit ≥ 3 geseedeten Layout-Varianten (Husk-Gruppen, Elite-Positionen, Prop-Cluster). Seed pro
   Spielstart, Autoplay mit festem Seed. Beleg: Verify prüft alle Seeds auf Erreichbarkeit von Gate und Truhe.
3. Affix-System: `AffixLibrary` von 7 auf ≥ 12 Affixe mit Tier-Spannen (z. B. Lebensraub, Gold-Fund,
   Skill-Rang +1), Rare bleibt 3–6 Affixe, Unique feste Sets. Tooltip-Vergleich zum ausgerüsteten Item. Beleg: Droptable-Tests mit Gewichten.
4. Truhe (Stash) in Lantern Hold: 40 Plätze, im Save persistiert. Beleg: Roundtrip-Test.
5. Autoplay deckt einen Kindled-Lauf ab, `G-DIABLO06` bleibt grün.

## Nicht-Ziele

- Keine prozedurale Generierung von Grund auf, nur geseedete Varianten aus handgebauten Bausteinen.
- Keine dritte Zone, keine zweite Klasse (eigener Folgegoal, falls gewünscht).
- Kein Online-Handel.

## Abhängigkeiten

- Start erst nach `arpg-session-loop` 100 % und `cinderreach-loop-guard` 100 % (Autoplay als Regressionsnetz).
- Save-Format aus `arpg-session-loop` S2 (Versionsfeld erhöhen, Migration v1 → v2 testen).
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
