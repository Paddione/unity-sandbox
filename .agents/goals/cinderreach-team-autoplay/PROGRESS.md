# Progress: cinderreach-team-autoplay

Prozent: 15 %

## Erledigt

- 2026-11-27: Goal angelegt nach setgoal-Runde (Kindled-Freischaltung auf Thewes, Server + 3 Clients, Kindled als
  Folgesitzung aus Saves, Solo-Routen ganz ersetzt, Lead god und die anderen sterblich, `G-DIABLO06` anpassen).
- Vorarbeit (nicht Teil der Kriterien): CI-Autoplay rendert seit `22bc564` per `xvfb-gpu` auf der RTX 3060 Ti
  (Lauf 46031151373, Median 22–32 fps statt 3–4 fps).

- 2026-11-27 K1: Kindled schaltet beim Thewes-Kill frei (`StoryDirector.BookFinalBossKill`, Koop per
  `DiabloNetworkPlayer.BroadcastFinalBossKill` an jeden Client), nicht mehr beim Abbot. Tests
  `CoopActBookingTests` (Abbot schaltet nicht frei, Thewes einmal, Client-Buchung) und `KindledUnlockTests`
  (PlayMode). Volle Suite EditMode 741/741, PlayMode 411/411.

## Nächste Schritte

1. Koop-Route prüfen und erweitern: Server + 3 Clients durch Quests, Akt III und Thewes (lokal mit
   `run-coop-pair.sh`).
2. Folgesitzung aus Saves auf Kindled (K3).
3. Workflow-Job umbauen (K4), `G-DIABLO06` anpassen (K5), CI-Lauf als Nachweis (K6).

## Offene Punkte

- K2–K6 offen. Lücken für die Server-Lead-Route über die volle Kampagne: Sammelziele (`StoryPickup` bucht auf
  dem Client nur lokal), Boss-Nachweise (`BossController.BossesKilled`/`SessionComplete.Shown` zählen nur auf dem
  Server), Wegpunkt-Reise ohne Gruppenmitnahme.
