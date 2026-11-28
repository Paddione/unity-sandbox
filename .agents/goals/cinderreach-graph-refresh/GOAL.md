# Goal: cinderreach-graph-refresh

health_goals: none (einmaliger Abgleich des Architekturstands mit dem bestehenden Runbook)
plan_refs: [.agents/plans/cinderreach-graph-refresh/tasks.md]

## Objective
Der Live-ADR beschreibt die aktuellen Architekturverträge. Die projektseitigen Lücken sind geprüft und die Dokumentverweise aufgelöst.

## Ist-Befunde
- B1 Der ADR belegt c0aad39. Der Index zu 6089d9b meldet 21 fehlende Ziele.

## Entscheidungen
- D1 Projektseitiger Abgleich mit vollständigem Index und Live-Rücklesen gemäß Eignerwahl.

## Erfolgskriterien
1. Fehlende Verweise und partielle Parsergebnisse jeweils null — Nachweis durch index_status.
2. Subsystemabdeckung und Grenzen geprüft — Nachweis durch Indexdiagnose und Pfadprüfung.
3. Architekturverträge und Revision im Live-ADR aktualisiert — Nachweis durch manage_adr im Lesemodus.

## Nicht-Ziele
- Änderungen am zentralen Indexer.

## Abhängigkeiten
- Graph-Hub und Hauptcheckout.
