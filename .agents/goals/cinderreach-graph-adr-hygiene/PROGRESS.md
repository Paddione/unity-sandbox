# Progress: cinderreach-graph-adr-hygiene

Prozent: 20 %

## Erledigt

- 2026-11-26: Aus A2/A3-Graph- und ADR-Audit (`5bd41d3`) sowie `/setgoal`-Klärungsrunde als Goal angelegt.
- 2026-11-27: Stufenplan [tasks.md](../../plans/cinderreach-graph-adr-hygiene/tasks.md) angelegt (5 Stufen, Basis `8e59d00`); `plan_refs` in GOAL.md ergänzt.

- 2026-11-27: Plan gegen `1ff1b95` geprüft (unverändert) und als P7 priorisiert (unabhängig, jederzeit als Füller).

- 2026-11-27 (S1, Worktree `feature/cinderreach-graph-adr-hygiene-s1`): frische `index_status`-Messung nach Re-Index auf `main`-HEAD (s. S1-Baseline unten) — gelandet als `8e781a7` (ff, Push durch, Worktree aufgeräumt). Prozent jetzt 20 %.

## Nächste Schritte

- S2 aus dem Stufenplan: `parse_partial` in `scripts/run-coop-pair.sh:92` glätten (Verhaltensnachweis + Re-Index-Beleg), danach S3 (`missing` → 0) und S4 (`ambiguous` reduzieren), S5 (ADR-Prozess + Gesamtverifikation).

## Offene Punkte

- S2–S5 stehen aus; S1-Baseline (63/260/1) ist die Arbeitsbasis für alle Folgestufen.

## S1-Baseline (Messung, 2026-11-27)

- Frischer `index_status(project="home-patrick-unity-sandbox", diagnostics="full")` nach Re-Index auf `main`-HEAD `6bf15a6` (Index `indexed_at 2026-11-27T01:14:45Z`, `nodes 10342`, `edges 56447`): `missing 63 / ambiguous 260 / parse_partial 1` (zuvor Audit `45/179` auf Basis `5bd41d3` — alte Werte ersetzt).
- `parse_partial`: genau 1 Datei, `scripts/run-coop-pair.sh`, `error_ranges 92-92`. Konstrukte in Zeile 92: `"${LAUNCH[@]}"` / `"${COMMON[@]}"`, `${CLIENT_GOD[@]+"${CLIENT_GOD[@]}"}`, `${SERVER_PEERS[@]+"${SERVER_PEERS[@]}"`, `${cls:+-class "$cls"}` — S2-Gegenstand, Skript unverändert.
- `ambiguous`-Top-Träger (grep über `.agents/goals/**` + `docs/**`): `check-meta.py` (42), `tasks.md` (39), `GOAL.md` (22), `CINDERREACH.md` (20), `run-coop-pair.sh` (19), `agent-workspace.py` (19), `run-unity-tests.sh` (18), `decision-log.md` (16), `PROGRESS.md` (16). Aufteilung: aktiv (GOAL.md-Bare-Filenames, ca. 92 Fundstellen; lebende `docs/*.md`) vs. historisch (PROGRESS-Verläufe, `*-run-log.md`-Referenzen aus `CINDERREACH.md` + 5 PROGRESS-Dateien + 6 `docs/`-Dateien — per Entscheid 2 unangetastet).
- `missing`-Charakter (Hub liefert nur Aggregate + gedeckelte Samples, exakte Liste ist S3-Arbeit): Stichprobe zeigt intakte Ziele (`scripts/check-meta.py`, `docs/story-bible.md`, `docs/runbook.md` existieren) — Hauptverdacht Pfad-Kontext-Fehler (Bare-Filename aus fremdem Verzeichnis, z. B. `CINDERREACH.md` ohne `.agents/goals/`-Präfix; Suffixe wie `run-coop-pair.sh:92`).
- Stale-Audit `45/179` aus `5bd41d3` hiermit als Arbeitsbasis ersetzt; keine Code-/Doku-Fixes in S1 (reine Messung).
