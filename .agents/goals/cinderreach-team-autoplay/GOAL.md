---
goal: cinderreach-team-autoplay
slug: cinderreach-team-autoplay
health_goals: [G-DIABLO06]
---

# Goal: cinderreach-team-autoplay — CI-Autoplay als 3er-Team, Kindled-Freischaltung durch Thewes

Angelegt: 2026-11-27. Herkunft: Eigner-Anfrage (setgoal-Runde 2026-11-27, alle Empfehlungen angenommen).

## Objective

Der CI-Autoplay (`.github/workflows/unity-tests.yml`, Job „Session autoplay“) ersetzt die drei Solo-Routen
(normal, kindled, lanternbearer) durch einen Teamlauf: dedizierter Server plus drei Clients, je einer pro Klasse
(fenwalker, lanternbearer, rogue). Das Team spielt die Standard-Schwierigkeit komplett bis zum Kill des
Akt-III-Endbosses Thewes. Erst dieser Kill schaltet Kindled frei. Eine zweite Teamsitzung spielt danach aus den
gespeicherten Spielständen auf Kindled durch.

## Erfolgskriterien

- K1 Spieländerung: Kindled wird beim Thewes-Kill (Akt III) freigeschaltet, nicht mehr beim Hollow Abbot
  (`DiabloNetworkPlayer.cs` um Zeile 445, `DifficultyProfile.UnlockKindled`). Das gilt solo und im Koop für jeden
  Client, der beim Kill in der Party ist. EditMode-Tests belegen: der Abbot-Kill schaltet nicht mehr frei, der
  Thewes-Kill schon.
- K2 Teamlauf Normal: `scripts/run-coop-pair.sh` mit `COOP_SERVER=1 COOP_CLIENTS=3`, Klassen fenwalker,
  lanternbearer und rogue je einmal. Client 1 führt mit `-autoplay-god`, Client 2 und 3 sind sterblich
  (`COOP_CLIENT_GOD=0`). Jeder Client schreibt `autoplay-result.json` mit Exit 0 und 0 Exceptions. Der Lead meldet
  alle Quests erledigt, Akt-Bosse 1–3 getötet und `fellLast=True`. Jeder Client meldet Kindled als freigeschaltet
  und gespeichert. Der Server beendet sich selbst mit Exit 0.
- K3 Teamlauf Kindled: Eine zweite Sitzung mit denselben drei Save-Dateien und ohne `-difficulty`-Flag läuft auf
  Kindled (im Log belegt durch Profilname und Multiplikatoren) und spielt bis Thewes, mit denselben Kriterien wie
  K2.
- K4 CI: Der Job enthält keine Solo-Route mehr, rendert per `AUTOPLAY_DISPLAY=xvfb-gpu` (RTX 3060 Ti) und bleibt
  unter `timeout-minutes` (bei Bedarf passend angehoben). Ergebnisse liegen im Artefakt und unter
  `~/unity-sandbox-autoplay/<Datum>[-<run_id>]/`, je Sitzung und Client getrennt (normal|kindled × client1..3).
- K5 Health: `G-DIABLO06` (dotfiles) prüft statt der Solo-Ordner die Teamergebnisse: drei verschiedene Klassen,
  Thewes in Normal getötet, Kindled freigeschaltet, Kindled-Sitzung grün. Die Probe ist nach dem Umbau grün.
- K6 Nachweis: mindestens ein grüner `workflow_dispatch`-Lauf (`autoplay=true`) auf `main` mit den Belegen
  K2–K5. Run-ID und Auszüge stehen in `PROGRESS.md` und `docs/coop-run-log.md`.

## Scope

- Kindled-Freischaltung im Spiel auf Thewes verlegen, plus Tests.
- `run-coop-pair.sh` erweitern, wo nötig: drei Klassen über `COOP_CLIENT<k>_CLASS`, Übernahme der Saves in die
  Folgesitzung, Ausgabeordner je Sitzung.
- `CoopAutoplay`/`SessionAutoplay` erweitern, soweit die Koop-Route die volle Kampagne (Quests, Akt III, Thewes)
  bisher nicht abdeckt.
- Workflow-Job, `docs/ci-runner.md`, `G-DIABLO06` in dotfiles.

## Nicht-Ziele

- Keine Solo-Route im CI-Autoplay.
- Kein verbindliches fps-gate. Drei Clients teilen sich eine GPU, das Gate bleibt ein Hinweis.
- Kein Relay- oder devmesh-Pod-Lauf. Lokal genügt UTP über 127.0.0.1.
- Keine Balance-Änderung an Kindled (Multiplikatoren bleiben HP/Schaden ×1.8, Unique ×2, Gold ×1.5).

## Randbedingungen und Abhängigkeiten

- Runner `unity-licensed` (WSL), editor-lock seriell, kein Desktop-Fenster (`xvfb-gpu` seit `22bc564`).
- Jeder Client hat ein eigenes `HOME`/XDG-Profil und eine eigene `-save-file`. Die Kindled-Sitzung nutzt bewusst
  dieselben Save-Dateien wie die Normal-Sitzung.
- Grundlage: `docs/coop-run-log.md` (Trio- und Server-Läufe, `CompactPacketDriver` 1200 Byte),
  `cinderreach-act-boss-gating` (Akt-Boss-Gate, Phase `boss4`), `cinderreach-party-team`.
- Annahme: Die Koop-Route deckt Quests und Akt III bisher nicht vollständig ab. Ein Koop-Lauf mit
  Thewes-Kill ist nirgends belegt.
- Abschluss je Schritt über Worktree und `agent-workspace.py land`. Die dotfiles-Änderung läuft getrennt über
  deren Worktree-Flow.
