# Progress: cinderreach-graph-adr-hygiene

Prozent: 100 %

## Erledigt

- 2026-11-26: Aus A2/A3-Graph- und ADR-Audit (`5bd41d3`) sowie `/setgoal`-Klärungsrunde als Goal angelegt.
- 2026-11-27: Stufenplan [tasks.md](../../plans/cinderreach-graph-adr-hygiene/tasks.md) angelegt (5 Stufen, Basis `8e59d00`); `plan_refs` in GOAL.md ergänzt.

- 2026-11-27: Plan gegen `1ff1b95` geprüft (unverändert) und als P7 priorisiert (unabhängig, jederzeit als Füller).

- 2026-11-27 (S1, Worktree `feature/cinderreach-graph-adr-hygiene-s1`): frische `index_status`-Messung nach Re-Index auf `main`-HEAD (s. S1-Baseline unten) — gelandet als `8e781a7` (ff, Push durch, Worktree aufgeräumt). Prozent jetzt 20 %.
- 2026-11-27 (S2, Worktree `feature/cinderreach-graph-adr-hygiene-s2`): `start_client()` in `scripts/run-coop-pair.sh` auf explizites Array-Sammeln (`args=(...)`) und `if [ -n "$cls" ]` statt `${cls:+(class $cls)}` / `${arr[@]+...}` umgestellt (Laufzeitverhalten identisch); als `8e3eef0` gelandet. Belege: `bash -n scripts/run-coop-pair.sh` Exit 0; `scripts/test_run_coop_pair.py` (4 Matrixfälle) + `python3 -m pytest scripts -q` (34/34) grün; `home-patrick-unity-sandbox` meldet `parse_partial.count == 0`.
- 2026-11-27 (S3 + S4 + S5-Runbook, Worktree `feature/cinderreach-graph-adr-hygiene-s3`, gelandet als `dcb2367`):
  - S3 (`missing` 63 → 0): alle fehlenden `doc_links` über Diagnose-Index bereinigt — `<see cref="RiskShrineProgress.Reset"/>` in `sandbox/Assets/Scripts/Zones/RiskShrineProgress.cs` korrigiert (`RiskShrine` EditMode 9/9 grün), G4-Lint-Rest in `.agents/goals/cinderreach-akt1-vertiefung/PROGRESS.md` behoben (`Keine.`), Tippfehler `scripts/test_check_hotpath_caching.py` korrigiert, Ordner- und externe/Laufzeit-Namen (z. B. Dotfiles-Proben, geplante Run-Logs, entfernte historische Methodennamen) ohne irreführende Repo-Code-Links formatiert.
  - S4 (`ambiguous` 260 bzw. 309 vor S3 → 229): Bare-Filenames in `.agents/goals/CINDERREACH.md`, aktiven Goal-Dateien (ohne gesperrtes `cinderreach-akt2-vertiefung`) und lebenden Doku-Dateien unter `docs` durch eindeutige repo-relative Pfade ersetzt; historische Verläufe blieben unangetastet.
  - S5-Runbook: Abschnitt `## Code-Graph und ADR-Pflege` in `docs/runbook.md` ergänzt (Auslöser, `index_repository` → `manage_adr` → `index_status`, Zuständigkeit am landenden Goal).
  - Belege nach Landung von `dcb2367` auf `main`: Re-Index `home-patrick-unity-sandbox` (`indexed_at 2026-11-27T06:15:47Z`, `10543` nodes, `57834` edges) liefert `parse_partial: 0`, `doc_links.unresolved.missing: 0`, `ambiguous: 229` (`1236` mentions).
- 2026-11-27 (S5 ADR-Update + Abschluss, Worktree `feature/cinderreach-graph-adr-hygiene-s5`): ADR für `home-patrick-unity-sandbox` per `manage_adr(mode='update')` aktualisiert (6 Abschnitte, < 8.000 Zeichen; `dcb2367`, `DiabloSkills.Skill` `0..19`, Teilklassen-/Fassaden-Splits, `CA-01`–`CA-03` abgeschlossen, Graph-Zähler `0/229/0`); `manage_adr(mode='outline')` und `adr_present: true` bestätigt; `node ~/scripts/goal-lint.mjs --repo .`, `python3 scripts/check-meta.py`, `python3 -m pytest scripts -q` (34/34) und `python3 ~/scripts/agent-workspace.py check` grün.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.

## S1-Baseline (Messung, 2026-11-27)

- Frischer `index_status(project="home-patrick-unity-sandbox", diagnostics="full")` nach Re-Index auf `main`-HEAD `6bf15a6` (Index `indexed_at 2026-11-27T01:14:45Z`, `nodes 10342`, `edges 56447`): `missing 63 / ambiguous 260 / parse_partial 1` (zuvor Audit `45/179` auf Basis `5bd41d3` — alte Werte ersetzt).
- `parse_partial`: genau 1 Datei, `scripts/run-coop-pair.sh`, `error_ranges 92-92`. Konstrukte in Zeile 92: `"${LAUNCH[@]}"` / `"${COMMON[@]}"`, `${CLIENT_GOD[@]+"${CLIENT_GOD[@]}"}`, `${SERVER_PEERS[@]+"${SERVER_PEERS[@]}"`, `${cls:+-class "$cls"}` — S2-Gegenstand, Skript unverändert.
- `ambiguous`-Top-Träger (grep über `.agents/goals/**` + `docs/**`): `check-meta.py` (42), `tasks.md` (39), `GOAL.md` (22), `CINDERREACH.md` (20), `run-coop-pair.sh` (19), "agent-workspace.py" (19), `run-unity-tests.sh` (18), `decision-log.md` (16), `PROGRESS.md` (16). Aufteilung: aktiv (GOAL.md-Bare-Filenames, ca. 92 Fundstellen; lebende `docs/*.md`) vs. historisch (PROGRESS-Verläufe, `*-run-log.md`-Referenzen aus `CINDERREACH.md` + 5 PROGRESS-Dateien + 6 Ordner `docs`-Dateien — per Entscheid 2 unangetastet).
- `missing`-Charakter (Hub liefert nur Aggregate + gedeckelte Samples, exakte Liste ist S3-Arbeit): Stichprobe zeigt intakte Ziele (`scripts/check-meta.py`, `docs/story-bible.md`, `docs/runbook.md` existieren) — Hauptverdacht Pfad-Kontext-Fehler (Bare-Filename aus fremdem Verzeichnis, z. B. `CINDERREACH.md` ohne Ordner `.agents/goals`-Präfix; Suffixe wie `run-coop-pair.sh:92`).
- Stale-Audit `45/179` aus `5bd41d3` hiermit als Arbeitsbasis ersetzt; keine Code-/Doku-Fixes in S1 (reine Messung).
