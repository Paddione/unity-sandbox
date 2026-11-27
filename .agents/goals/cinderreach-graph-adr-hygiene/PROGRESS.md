# Progress: cinderreach-graph-adr-hygiene

Prozent: 0 %

## Erledigt

- 2026-11-26: Aus A2/A3-Graph- und ADR-Audit (`5bd41d3`) sowie `/setgoal`-Klärungsrunde als Goal angelegt.
- 2026-11-27: Stufenplan [tasks.md](../../plans/cinderreach-graph-adr-hygiene/tasks.md) angelegt (5 Stufen, Basis `8e59d00`); `plan_refs` in GOAL.md ergänzt. Prozent bleibt 0 %.

- 2026-11-27: Plan gegen `1ff1b95` geprüft (unverändert) und als P7 priorisiert (unabhängig, jederzeit als Füller). Prozent bleibt 0 %.

## Nächste Schritte

- S1 aus dem Stufenplan: frische `index_status`-Messung (Audit-Zahlen 45/179 auf Basis `5bd41d3` neu erheben) und Funde klassieren, danach S2 (`run-coop-pair.sh:92`), S3/S4 (`doc_links`), S5 (ADR-Prozess).

## Offene Punkte

- Bereinigung, Doku-Update und Re-Index-Nachweis noch offen.

## S1-Baseline (Messung, 2026-11-27)

- Frischer `index_status(project="home-patrick-unity-sandbox", diagnostics="full")` nach Re-Index auf `main`-HEAD `6bf15a6` (Index `indexed_at 2026-11-27T01:14:45Z`, `nodes 10342`, `edges 56447`): `missing 63 / ambiguous 260 / parse_partial 1` (zuvor Audit `45/179` auf Basis `5bd41d3` — alte Werte ersetzt).
- `parse_partial`: genau 1 Datei, `scripts/run-coop-pair.sh`, `error_ranges 92-92`. Konstrukte in Zeile 92: `"${LAUNCH[@]}"` / `"${COMMON[@]}"`, `${CLIENT_GOD[@]+"${CLIENT_GOD[@]}"}`, `${SERVER_PEERS[@]+"${SERVER_PEERS[@]}"`, `${cls:+-class "$cls"}` — S2-Gegenstand, Skript unverändert.
- `ambiguous`-Top-Träger (grep über `.agents/goals/**` + `docs/**`): `check-meta.py` (42), `tasks.md` (39), `GOAL.md` (22), `CINDERREACH.md` (20), `run-coop-pair.sh` (19), `agent-workspace.py` (19), `run-unity-tests.sh` (18), `decision-log.md` (16), `PROGRESS.md` (16). Aufteilung: aktiv (GOAL.md-Bare-Filenames, ca. 92 Fundstellen; lebende `docs/*.md`) vs. historisch (PROGRESS-Verläufe, `*-run-log.md`-Referenzen aus `CINDERREACH.md` + 5 PROGRESS-Dateien + 6 `docs/`-Dateien — per Entscheid 2 unangetastet).
- `missing`-Charakter (Hub liefert nur Aggregate + gedeckelte Samples, exakte Liste ist S3-Arbeit): Stichprobe zeigt intakte Ziele (`scripts/check-meta.py`, `docs/story-bible.md`, `docs/runbook.md` existieren) — Hauptverdacht Pfad-Kontext-Fehler (Bare-Filename aus fremdem Verzeichnis, z. B. `CINDERREACH.md` ohne `.agents/goals/`-Präfix; Suffixe wie `run-coop-pair.sh:92`).
- Stale-Audit `45/179` aus `5bd41d3` hiermit als Arbeitsbasis ersetzt; keine Code-/Doku-Fixes in S1 (reine Messung).
