---
goal: cinderreach-act-one
slug: cinderreach-act-one
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: []
---

# Goal: cinderreach-act-one — Akt I mit dritter Zone und Akt-Boss

Angelegt: 2026-11-18. Herkunft: Eigner-Auftrag „come up with new milestones for our unity game“ (setgoal).
Folge-Goal #5, Reihenfolge siehe `.agents/goals/ROADMAP.md`.

## Objective

Die 20-Minuten-Session wächst zu einem abgeschlossenen Akt I mit drei Zonen: eine neue dritte Zone mit
eigenem Akt-Boss, Lantern Hold bleibt das Hub.

## Erfolgskriterien

1. Zone 3 als eigene Szene mit eigenem Namen und eigenen Assets, 2 neue Gegnertypen und ein Akt-Boss mit
   ≥ 2 Phasen. Beleg: `VerifyDiabloZone` + PlayMode-Test für Spawn und Boss-Kill.
2. Akt-Struktur steuert den Fortschritt: Boss Ossuary → Zone 3 frei → Akt-Boss → „Akt I abgeschlossen“ im
   Save. Beleg: EditMode-Test der Freischaltkette + PlayMode-Test für Save/Load über alle 3 Zonen.
3. Wegpunkt- oder Portal-System verbindet Lantern Hold mit jeder freigeschalteten Zone. Beleg: PlayMode-Test.
4. Zone 3 hält Seeds, Affixe und Kindled-Skalierung aus `cinderreach-replayability` ein.
   Beleg: Skalierungstest auf Zone-3-Gegner.
5. Nächtlicher Autoplay (`G-DIABLO06`) spielt bis zum Akt-Boss-Kill durch, ohne Exception, FPS-Gate grün.
6. Windows-Build per `scripts/stage-windows.sh` gestaged, Shotcheck-PNG aus Zone 3 und vom Akt-Boss als Sichtbeleg.

## Nicht-Ziele

- Kein Akt II, keine Story-Cutscenes, kein Quest-Dialogsystem.
- Keine neue Klasse (eigenes Goal `cinderreach-second-class`).
- Keine Co-op-Änderungen über die bestehende Parität hinaus.

## Abhängigkeiten

- Start erst nach `cinderreach-replayability` 100 % (Seeds, Affixe, Kindled gelten auch in Zone 3).
- Session-Protokoll aus `.agents/plans/arpg-session-loop/tasks.md`, Unity 6000.0.85f2, `land: ff`.
- Assets aus der Blender-/KayKit-Pipeline wie bei der Blood Moor, keine Fremd-IP.
- EditMode- und PlayMode-Suiten bleiben grün; Save-Version erhöhen und Migration testen.
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
