# Progress: cinderreach-team-autoplay

Prozent: 0 %

## Erledigt

- 2026-11-27: Goal angelegt nach setgoal-Runde (Kindled-Freischaltung auf Thewes, Server + 3 Clients, Kindled als
  Folgesitzung aus Saves, Solo-Routen ganz ersetzt, Lead god und die anderen sterblich, `G-DIABLO06` anpassen).
- Vorarbeit (nicht Teil der Kriterien): CI-Autoplay rendert seit `22bc564` per `xvfb-gpu` auf der RTX 3060 Ti
  (Lauf 46031151373, Median 22–32 fps statt 3–4 fps).

## Nächste Schritte

1. K1: Kindled-Freischaltung auf Thewes verlegen, EditMode-Tests für den alten und den neuen Auslöser.
2. Koop-Route prüfen und erweitern: Server + 3 Clients durch Quests, Akt III und Thewes (lokal mit
   `run-coop-pair.sh`).
3. Folgesitzung aus Saves auf Kindled (K3).
4. Workflow-Job umbauen (K4), `G-DIABLO06` anpassen (K5), CI-Lauf als Nachweis (K6).

## Offene Punkte

- Alle Kriterien K1–K6 offen.
