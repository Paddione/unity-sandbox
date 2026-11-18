---
goal: cinderreach-loop-guard
slug: cinderreach-loop-guard
branch: feature/cinderreach-loop-guard-s<N> (unity-sandbox), fix/diablo-probe-g06 (dotfiles)
repo_main: main
tickets: none
health_goals: [G-DIABLO06]
start_condition: arpg-session-loop Stufe S6 = landed (SessionAutoplay mit -autoplay existiert)
---

# Plan: cinderreach-loop-guard — Autoplay nächtlich, G-DIABLO06 in /health

Pfad: feature, 3 Stufen. Ausführung nach dem **Session-Protokoll** aus
`.agents/plans/arpg-session-loop/tasks.md` (Schritte 1–9), Lock-Name `task cinderreach-loop-guard-sN`.
Protokollschritt 1 prüft hier zusätzlich die Startbedingung:
`git -C ~/unity-sandbox log origin/main --oneline -- sandbox/Assets/Scripts/SessionAutoplay.cs` zeigt einen
Commit.

## Ausgangslage (Belege, Stand 2026-11-17)

- CI: `.github/workflows/unity-tests.yml` läuft auf dem Self-Hosted-Runner `[self-hosted, Linux, X64,
  unity-licensed]` (`github-runner-unity.service`) bei Push/Dispatch. Eigene Checkout- und Library-Kopie,
  `docs/ci-runner.md`. Artefakte als Upload, 14 Tage.
- `scripts/run-unity-build.sh` (gehärteter Build mit Exit 2 = Hang), `scripts/fps-gate.py` (liest
  `[Perf] FPS avg …`-Zeilen von `FpsReporter`, Flags `--min-fps`, `--resolution`, `--skip-first`).
- dotfiles `scripts/health_probes/diablo_unity.py`: Probes G-DIABLO01..05, Muster für Unknown-Anker
  außerhalb des Hosts in `G-DIABLO04`. Registrierung über die Liste in der Funktion „Liefert G-DIABLO01..05“
  (Z. ~430). `scripts/health_remediate.py:NON_AGENT_GOALS`, Tests `scripts/test_diablo_unity_health.py`.
- Nach arpg-session-loop S6: `SessionAutoplay` (`-autoplay`, `-autoplay-god`), Exit 0/1, Phasen-Log.

## Entscheide

1. Ausführungsort ist der Self-Hosted-Runner per `schedule:` im GitHub-Workflow, kein lokaler
   systemd-Timer. Der Runner hat eine eigene Library und kollidiert nicht mit dem Editor-Dienst. Nächtlich
   03:30 Europe/Berlin (cron `30 2 * * *` UTC im Winter).
2. Ergebnisablage lokal **und** als Artefakt. Der Runner schreibt `autoplay-result.json` zusätzlich nach
   `~/unity-sandbox-autoplay/<YYYY-MM-DD>/` (außerhalb des Repos, Runner-User = Host-User). Die Probe
   liest nur dieses Verzeichnis, kein GitHub-API-Zugriff nötig.
3. Ergebnisformat (Vertrag zwischen Spiel und Probe):
   `{"schema":1,"commit":"<sha>","started":"<iso>","duration_s":<f>,"phases":["town","zone1","gate","zone2","boss","complete"],"exceptions":<n>,"fps_windows":[<f>...],"exit":<n>}`.

4. Anzeige des Autoplay-Laufs (Nachtrag S1, 2026-11-18): Unter Xvfb rendert Unity mit llvmpipe
   (gemessen 5,6 fps schlechtestes Fenster, FPS-Gate rot, R1 eingetreten). Über das WSLg-X-Display mit
   Mesa d3d12 (`DISPLAY=:0 GALLIUM_DRIVER=d3d12`, `/dev/dxg`) rendert die RTX 5070 Ti: 47,5 fps
   schlechtestes Fenster, Gate grün. `run-autoplay.sh` wählt per `AUTOPLAY_DISPLAY=auto` WSLg, wenn
   Socket und `/dev/dxg` existieren, sonst Xvfb. Schwelle 30 bleibt unverändert.

## Partial-Manifest

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 result | Ergebnis-JSON aus dem Autoplay | `sandbox/Assets/Scripts/SessionAutoplay.cs`, `sandbox/Assets/Scripts/FpsReporter.cs` (Fenster sammeln), `sandbox/Assets/Tests/EditMode/AutoplayResultTests.cs` (neu), `scripts/run-autoplay.sh` (neu) |
| S2 nightly | Workflow + Ablage | `.github/workflows/autoplay-nightly.yml` (neu), `docs/ci-runner.md` |
| S3 probe | G-DIABLO06 in dotfiles | dotfiles: `scripts/health_probes/diablo_unity.py`, `scripts/test_diablo_unity_health.py`, `scripts/health_remediate.py` |

Seriell: S1 → S2 → S3.

## S1 — Ergebnis-JSON

- `SessionAutoplay` schreibt beim Beenden `autoplay-result.json` nach `-autoplay-out <dir>`
  (Default `<game>/autoplay`), Felder laut Entscheid 3. Exceptions zählt ein `Application.logMessageReceived`-Hook.
- `FpsReporter` stellt die gemessenen Fenster als Liste bereit (statische Property, nur lesen).
- `scripts/run-autoplay.sh`: baut den Linux-Player über `run-unity-build.sh`, startet ihn mit
  `-autoplay -autoplay-god -autoplay-out <dir> -screen-width 1280 -screen-height 720` unter Xvfb (kein
  `-batchmode`, sonst rendert nichts und das FPS-Gate misst Unsinn; Muster aus `unity-build-matrix-perf`). Timeout 900 s (Exit 2 = Hang),
  danach `python3 scripts/fps-gate.py <log> --min-fps 30 --resolution 1280x720 --skip-first 1`.
  Exit = max(Autoplay, FPS-Gate).
- Tests: JSON-Schema-Felder, Exception-Zähler, Phasenliste vollständig nur bei Boss-Kill.
- Akzeptanz: `bash scripts/run-autoplay.sh` lokal Exit 0, JSON vorhanden.

## S2 — Nachtlauf

- `autoplay-nightly.yml`: `on: schedule + workflow_dispatch`, gleiches Runner-Label, `concurrency:
  unity-autoplay`, Schritte Checkout (LFS) → `bash scripts/run-autoplay.sh` → Kopie nach
  `~/unity-sandbox-autoplay/$(date +%F)/` → Artefakt-Upload (14 Tage). Retention lokal: Ordner älter als
  30 Tage löschen.
- Konflikt mit `unity-tests.yml`: eigene `concurrency`-Gruppe reicht nicht, beide nutzen dieselbe
  Runner-Library. Ein Runner führt Jobs ohnehin seriell aus, das ist der Schutz. Im Plan-Journal belegen.
- Akzeptanz: 3 aufeinanderfolgende `workflow_dispatch`-Läufe grün (Run-URLs im Journal).

## S3 — G-DIABLO06

- Neue Probe „Cinderreach Session-Autoplay (nächtlich)“, Kategorie `Diablo-Game`, Priority `A`:
  - `achieved`: neuestes `autoplay-result.json` ≤ 48 h, `exit == 0`, alle 6 Phasen, `exceptions == 0`,
    `duration_s ≤ 1.5 × median(letzte 5)`, kein FPS-Fenster (ohne erstes) < 30.
  - `critical`: eine Bedingung verletzt, Wert = Anzahl verletzter Bedingungen. Detail nennt jede.
  - `unknown`: Verzeichnis fehlt (nicht dieser Host), Muster aus `G-DIABLO04`.
- `NON_AGENT_GOALS` um `G-DIABLO06` erweitern.
- Tests: achieved, je ein critical-Fall pro Bedingung, unknown, zu alt.
- Akzeptanz: Live `python3 ~/scripts/repo_health_collect.py` zeigt `G-DIABLO06` achieved. Danach den
  Fehlertest aus Goal-Kriterium 5: Branch mit unverwundbarem Boss per `workflow_dispatch` auf dem Branch
  laufen lassen, Probe gegen dessen Ergebnis → critical, Branch verwerfen über `finish --branch`.

### Statustabelle

| Stufe | Abhängig von | Gewicht | Status |
|---|---|---|---|
| S1 result | Startbedingung | 35 % | landed 2026-11-18 |
| S2 nightly | S1 | 30 % | offen |
| S3 probe | S2 | 35 % | offen |

## Verify (je Stufe)

1. `scripts/run-unity-tests.sh` grün (S1).
2. `bash scripts/run-autoplay.sh` Exit 0 (S1, S2).
3. dotfiles (S3): `python3 scripts/agent-workspace.py check`, `python3 -m pytest
   scripts/test_diablo_unity_health.py -q`, danach Live-Collector.

## Risiken

- R1 Xvfb/llvmpipe-FPS liegt bei ~2 fps fullscreen (`unity-build-matrix-perf`). Gegenmittel: 1280×720
  fenstert, Schwelle 30 erst nach einem Messlauf bestätigen. Liegt der Messwert darunter, die Schwelle
  im Goal begründet senken (Eigner informieren, nicht still ändern).
- R2 Nachtlauf belegt den Runner: Ein Push um 03:30 wartet. Akzeptiert.
- R3 Autoplay-Flakiness: feste Seeds (arpg S6 R4). Ein Flake gilt als Bug, nicht als Retry-Fall.
