# Runbook — Concept → Prototype → (optional) Vertical Slice

Chain for the mechanics testbed. Upper bounds only; each stage ends with
its exit question and the evidence listed below.

## 1. Concept (week 0–1)

- One-sentence core goal + One-Page GDD draft (MoSCoW, done-means, dates).
- Exit question: "is the concept testable as one greybox question?"
- Evidence: `docs/one-page-gdd.md` + `docs/mechanics-questions.md`.

## 2. Prototype (1–4 weeks per question)

- One prototype, one question (Q1 movement, Q2 trigger, Q3 toy/goal).
- Greybox discipline: primitives, fresh URP/Lit, code-owned rig,
  fake everything else.
- Exit question: **"is the core mechanic fun?"**
- Evidence per question: greybox scene + `VerifyGreybox` green +
  player build run log + one `docs/decision-log.md` line
  (continue / pivot / kill).

## 3. Vertical Slice — option only

- Starts only if the sandbox answers the exit question with fun
  (all three Q1–Q3 verdicts `continue`, loop readable).
- Not started; no slice work is part of this goal.
- Evidence: decision-log line explicitly opening the slice (absent).

## Server-Rebuild über das Agent-Board

- Der Unity-Tab im Agent-Board (`/unity`) baut den Dedicated Server neu und
  rollt ihn nach gpu-metal aus. Gleiches Kommando auf der Shell:

```
python3 ~/scripts/unity_ops.py rebuild --scene greybox
```

- Phasen: `stop-editor` (Dauer-Editor `unity-sandbox-editor.service` gibt die
  Projektsperre frei) → `build` (`SandboxBuilder.BuildServer -serverScene <scene>`)
  → `start-editor` (läuft auch nach einem Fehler) → `deploy` ("BUILD_INFO.json"
  plus Rsync nach `gpu-metal:~/greybox-server/`) → `restart`
  (`greybox-server.service`) → `verify` (`[Net] Server started` im frischen Log).
- Stand und Phase: "~/.local/state/unity-ops/rebuild.json", Build-Log
  "~/.local/state/unity-ops/build.log".
- `-serverScene diablo` wählt `sandbox/Assets/Scenes/Diablo.unity`, jeder andere Wert
  oder kein Argument die Greybox. Die Szene steht in der Logzeile
  `Server build result: … (scene <Datei>)`.

## Konten, Namen und Cloud-Held

Unity Authentication im UGS-Projekt „Cinderreach“ (`02e05e98-…`, Umgebung `production`), Provider „Username &
Password“ aktiv (Unity Cloud Dashboard → Player Authentication → Identity Providers).

- Identität: Jede Spielkopie meldet sich mit einem Auth-Profil an (`main`, oder `-profile NAME` für eine zweite Kopie
  auf demselben PC). Gäste behalten ihre anonyme Identität über Neustarts. Automation (`-autoplay`, `-coop-autoplay`,
  `-shotcheck`, `-save-file`) und Server bekommen Wegwerf-Profile `auto<pid>`.
- Namen: UGS Player Names, im Startbildschirm über „Playing as …“ änderbar, `-player-name` für Testläufe. Der Server
  bereinigt jeden Namen (`PlayerNames.Sanitize`), Namensschilder zeigen ihn über allen Netz-Helden.
- Konto: Startbildschirm → „Playing as …“ → Anmelden / Konto anlegen / Abmelden. „Konto anlegen“ als Gast behält die
  Spieler-ID. Kein Passwort-Zurücksetzen: ein vergessenes Passwort heißt neues Konto (Nicht-Ziel).
- Cloud-Held: Nur angemeldete Konten synchronisieren (Cloud-Save-Schlüssel `hero`). Lokale Arbeitskopie bleibt
  "cinderreach-save.json", daneben "cinderreach-save.json.meta.json" (Besitzer, letzter Abgleich). Ersetzte Helden
  liegen als `*.pre-cloud.bak`, `*.pc-hero.bak` oder `*.owner-<id>.bak` im selben Ordner
  (`%USERPROFILE%\AppData\LocalLow\Unity Technologies\Cinderreach`).
- Live-Probe: `bash scripts/account-smoke.sh [out]` legt ein Wegwerf-Konto an, prüft Anmelden, Neustart, Abmelden,
  Fehlertexte, Upload, Konflikt und Download auf einem zweiten Profil und löscht das Konto (Exit 0 = alles grün).
- Spieler löschen oder sperren: Unity Cloud Dashboard → Player Management.

## Dedizierter Server (devmesh)

Spieler klicken auf dem Startbildschirm „Join Server“. Der Client sucht die öffentliche Lobby „Cinderreach Server“
und tritt über deren Relay-Code bei. Der Server läuft als Pod `workspace/cinderreach-server` im devmesh-Heimcluster,
nur mit ausgehenden Verbindungen (Unity Relay/Lobby, Projekt „Cinderreach“). Kein Port, kein Service, kein DNS.

- Update: `bash scripts/publish-server-image.sh` (Server-Build, Image `ghcr.io/paddione/cinderreach-server:<commit>`,
  gibt den Digest aus). Digest in dotfiles "apps/cinderreach-server/k8s/deployment.yaml" eintragen, dotfiles landen;
  Flux (`cinderreach-server`, Quelle dotfiles, Definition in Bachelorprojekt "flux/clusters/devmesh/") rollt aus.
  Client und Server sollten aus demselben Commit stammen (NGO prüft Netzwerk-Hashes). Danach Installer neu bauen.
- Status: `kubectl --context devmesh -n workspace get pod -l app=cinderreach-server`, Logs mit
  `kubectl --context devmesh -n workspace logs deploy/cinderreach-server | grep '\[Server\]'`.
- Neustart (frischer Run): `kubectl --context devmesh -n workspace rollout restart deploy/cinderreach-server`.
  Ein Server beendet sich 20 s nach dem letzten Spieler selbst. Kubernetes startet ihn neu (kurzer Backoff).
- Proben: `/tmp/cinderreach-ready` wird alle 5 s erneuert, solange NGO lauscht und der Lobby-Heartbeat frisch ist.
  Älter als 5 min → Liveness startet den Container neu.
- Lokal testen ohne Cluster: `COOP_SERVER=1 COOP_RELAY=1 COOP_CLIENTS=2 bash scripts/run-coop-pair.sh <out>`
  (eigener Lobby-Name, berührt den echten Server nicht).
- Server-Build = Server-Subtarget ohne Shader: Laufzeit-Material-Code muss `Shader.Find` = null tolerieren.

## Code-Graph und ADR-Pflege

Der Code-Graph (`home-patrick-unity-sandbox`) und das Architektur-Entscheidungsprotokoll (ADR) liegen im zentralen Hub `fleet-workspace` (`codebase-memory`).

- **Auslöser für ein ADR-Update:** neues oder geteiltes Subsystem (z. B. Partial-/Klassen-Extraktion), Änderung an Netz- oder Save-Verträgen (`DiabloSkills.Skill`, RPC-Signaturen, `SaveMigrations`), Klassen-/Skill-Umbau sowie abgeschlossene Punkte aus dem Refactoring-Backlog (`CA-*`).
- **Ablauf vor Goal-Abschluss (Verantwortung beim landenden Goal):**
  1. Graph aktualisieren: `index_repository(repo_path="/home/patrick/unity-sandbox", mode="full")`.
  2. Betroffene ADR-Abschnitte gezielt pflegen: `manage_adr(project="home-patrick-unity-sandbox", mode="set_sections", section_updates={...})` (oder `mode="update"` bei Vollerneuerung).
  3. Stand prüfen: `index_status(project="home-patrick-unity-sandbox", diagnostics="full")` — Pflichtwerte `adr_present: true`, `parse_partial.count == 0` und `doc_links.unresolved.missing == 0`.
- **Doku-Konvention:** Repo-Dateien in aktiven Goals und Doku möglichst mit eindeutigem repo-relativem Pfad referenzieren; Laufzeitartefakte oder Dateien aus anderen Repos (`"cinderreach-save.json"`, `"autoplay-result.json"`, Dotfiles-Pfade) nicht als repo-lokale Code-Links formatieren.
- **Nachweis:** Live-ADR-Update inkl. Abschluss des Refactoring-Backlogs (`CA-01`–`CA-03`) in der Git-Historie des Goals cinderreach-graph-adr-hygiene dokumentiert. Die konsolidierten Ergebnisse stehen in `.agents/goals/CINDERREACH.md`.
