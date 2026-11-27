# Progress: cinderreach-team-autoplay

Prozent: 70 %

## Erledigt

- 2026-11-27: Goal angelegt nach setgoal-Runde (Kindled-Freischaltung auf Thewes, Server + 3 Clients, Kindled als
  Folgesitzung aus Saves, Solo-Routen ganz ersetzt, Lead god und die anderen sterblich, `G-DIABLO06` anpassen).
- Vorarbeit (nicht Teil der Kriterien): CI-Autoplay rendert seit `22bc564` per `xvfb-gpu` auf der RTX 3060 Ti
  (Lauf 46031151373, Median 22–32 fps statt 3–4 fps).

- 2026-11-27 K1: Kindled schaltet beim Thewes-Kill frei (`StoryDirector.BookFinalBossKill`, Koop per
  `DiabloNetworkPlayer.BroadcastFinalBossKill` an jeden Client), nicht mehr beim Abbot. Tests
  `CoopActBookingTests` (Abbot schaltet nicht frei, Thewes einmal, Client-Buchung) und `KindledUnlockTests`
  (PlayMode). Volle Suite EditMode 741/741, PlayMode 411/411.
- 2026-11-27 K2/K3 (lokal): Teamkampagne auf dediziertem Server (Story-Autorität auf dem Server, Weiterleitung von
  Funden/Zonen, Lead = `SessionAutoplay` als Client, Follower durch die ganze Kampagne, Akt-Bosse auf Clients
  netzwerk-gespawnt, Gegner-Positionskorrektur, Server übernimmt die Party-Schwierigkeit, Kindled-Folgelauf per
  `RunProfile.StartCampaign`). Lauf `run7` (`docs/coop-run-log.md`): normal PASS (Lead 34/34,
  `1:Kilnmother:- 2:Glasswright:- 3:Thewes:-`, Follower 9/9, alle Kindled freigeschaltet und gespeichert, Server Exit 0)
  und kindled PASS (`[Difficulty] server adopts Kindled from client 1: hp x1.8 damage x1.8 gold x1.5 unique x2`,
  Lead 34/34, je 0 Exceptions).
- 2026-11-27 K4 (Code): Workflow-Job `autoplay` ruft `scripts/run-team-autoplay.sh` (keine Solo-Route, `xvfb-gpu`,
  `timeout-minutes: 300`), Host-Ablage `~/unity-sandbox-autoplay/<Datum>[-<run_id>]/{normal,kindled}/{server,client1..3}/`,
  `docs/ci-runner.md` aktualisiert.
- 2026-11-27 K5 (Code): `G-DIABLO06` bewertet die Team-Verdicts (dotfiles-Worktree `fix/diablo06-team-autoplay`).

## Nächste Schritte

1. Landen (unity-sandbox, dann dotfiles `G-DIABLO06`).
2. K6: `gh workflow run unity-tests.yml -f autoplay=true` auf `main`, Run-ID und Auszüge hier und in
   `docs/coop-run-log.md`, `G-DIABLO06` grün.

## Offene Punkte

- K4 im CI unbelegt, K5 nach dem CI-Lauf zu prüfen, K6 offen.
