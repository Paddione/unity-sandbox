---
goal: cinderreach-loop-guard
slug: cinderreach-loop-guard
health_goals: [G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-loop-guard/tasks.md]
---

# Goal: cinderreach-loop-guard — Session-Schleife dauerhaft gegen Regression bewachen

Angelegt: 2026-11-17. Herkunft: Eigner-Auftrag „come up with the next goals“ nach `arpg-session-loop`.
Folge-Goal, Reihenfolge siehe `.agents/goals/ROADMAP.md`.

## Objective

Der in `arpg-session-loop` S6 gebaute `-autoplay`-Durchlauf (Town → Ashen Fen → Sunken Ossuary → Boss →
Abschluss) läuft regelmäßig automatisch und wird als neues Health Goal `G-DIABLO06` in `/health` überwacht.
Dazu gehören FPS-Gate und Laufzeit, damit jede spätere Änderung an Cinderreach sofort sichtbar macht, ob die
Session noch durchspielbar ist.

## Erfolgskriterien

1. Wiederkehrender Lauf (systemd-Timer oder Self-Hosted-Runner-Workflow, nächtlich) baut den Linux-Player
   und führt `-autoplay -autoplay-god` aus. Log + `autoplay-result.json` (Phasen, Exceptions, Dauer, FPS-Fenster)
   unter `artifacts/autoplay/<datum>/`. Beleg: 3 aufeinanderfolgende Läufe grün.
2. `G-DIABLO06` in dotfiles `scripts/health_probes/diablo_unity.py`: neuestes Ergebnis ≤ 48 h alt, alle Phasen
   erreicht, 0 Exceptions, Durchlaufzeit ≤ 1,5 × Median der letzten 5 Läufe. Priority `A`. Unknown-Anker
   außerhalb des Hosts wie `G-DIABLO04`.
3. FPS-Gate: `scripts/fps-gate.py <log> --min-fps 30 --resolution 1280x720 --skip-first 1` als Teil des
   Laufs; ein Unterschreiten macht `G-DIABLO06` `critical`.
4. Unit-Tests für `G-DIABLO06` (achieved, critical bei Exception/fehlender Phase/zu alt/FPS, unknown) in
   `scripts/test_diablo_unity_health.py`. `G-DIABLO06` in `NON_AGENT_GOALS` von `scripts/health_remediate.py`.
5. Ein bewusst eingebauter Fehler (Boss unverwundbar) färbt `G-DIABLO06` rot. Beleg im Journal, Fehler wieder
   entfernt.

## Nicht-Ziele

- Kein Unity-Batchmode im 5-Minuten-Collector. Die Probe liest nur Artefakte.
- Keine neuen Spielinhalte.
- Kein Windows-Lauf im Timer (bleibt manueller Shotcheck).

## Abhängigkeiten

- Start erst, wenn `arpg-session-loop` Stufe S6 `landed` ist (`SessionAutoplay` existiert).
- dotfiles-Goal `diablo-unity-health-goals` (Probe-Modul, Collector-Verdrahtung).
- health_goals: [G-DIABLO06]
