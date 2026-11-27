---
goal: cinderreach-graph-adr-hygiene
slug: cinderreach-graph-adr-hygiene
health_goals: [G-GOALS01]
plan_refs: [.agents/plans/cinderreach-graph-adr-hygiene/tasks.md]
---

# Goal: cinderreach-graph-adr-hygiene

## Objective

Die im Code-Graph- und ADR-Audit (A2/A3, Revision `5bd41d3`) festgestellten repo-eigenen Graph- und Doku-Hygiene-Lücken schließen: fehlende (`45`) und mehrdeutige (`179`) `doc_links`-Referenzen in `.agents/goals/**` und `docs/**` bereinigen, das Bash-`parse_partial`-Konstrukt in `scripts/run-coop-pair.sh:92` ohne Verhaltensänderung glätten und einen verbindlichen ADR-Aktualisierungsprozess für `home-patrick-unity-sandbox` in `docs/` verankern. Ausgangslage: [CINDERREACH.md](../CINDERREACH.md).

## Erfolgskriterien

1. `index_status(project="home-patrick-unity-sandbox", diagnostics="full")` meldet nach Re-Index `parse_partial.count == 0` (`scripts/run-coop-pair.sh:92` parse-fehlerfrei; `bash -n scripts/run-coop-pair.sh` grün).
2. Alle 45 bisher als `missing` gemeldeten `doc_links` sind bereinigt (korrigiert, auf echte Pfade umgestellt oder berechtigt entfernt) und `ambiguous` Bare-Filenames in aktiven Goals und Doku sind durch eindeutige relative Pfade ersetzt, sodass `doc_links.unresolved.missing == 0` und `ambiguous` messbar reduziert ist.
3. `docs/runbook.md` (oder ein dediziertes Architektur-Runbook unter `docs/`) dokumentiert den kompakten Prozess zum Re-Index und zur ADR-Aktualisierung via `manage_adr(mode='set_sections'|'update')` bei größeren Subsystem-Änderungen; der Graph-ADR bleibt aktuell (`adr_present: true`).
4. `node ~/scripts/goal-lint.mjs --repo .`, `python3 scripts/check-meta.py` und `python3 -m pytest scripts -q` bestehen ohne Fehler.

## Scope

- `.agents/goals/**/*.md`
- `docs/**/*.md`
- `scripts/run-coop-pair.sh` (Zeile 92)
- Graph-Re-Index und ADR-Update via `fleet-workspace`

## Nicht-Ziele

- Keine Änderungen am Upstream-Indexer `codebase-memory` (C#-`TESTS`-Kanten-Erkennung, Short-Name-Heuristiken wie `.Add`/`.Contains` und das `python-resources-2`-Gap an `scripts/voice/voice_lines.py:91` bleiben dokumentierte Tool-Grenzen).
- Kein Refactoring von `sandbox/Assets/Editor/SandboxBuilder.cs` oder `sandbox/Assets/Scripts/DiabloShotCheck.cs` und keine Gameplay-Code-Änderungen.

## Abhängigkeiten

- `unity-sandbox main` (Basis `5bd41d3`), zentraler MCP-Hub `fleet-workspace` (`index_repository`, `index_status`, `manage_adr`).
