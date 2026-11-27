# Progress: cinderreach-skills-modularization

Prozent: 0 %

## Erledigt

- 2026-11-26: Aus Code-Analyzer-Befund `CA-02` und `/setgoal`-Klärungsrunde als Goal auf Basis `main` (`5bd41d3`) angelegt.
- 2026-11-27: Stufenplan [tasks.md](../../plans/cinderreach-skills-modularization/tasks.md) angelegt (4 Stufen, Basis `8e59d00`); `plan_refs` in GOAL.md ergänzt. Prozent bleibt 0 %.
- 2026-11-27: Drift seit Basis belegt: `DiabloSkills.cs` heute 1.414 Zeilen, Skill-Enum `0..19` (statt `0..11` in Kriterium 3), 59 referenzierende Dateien (statt 42) — Korrektur ist als S1 im Plan verankert, GOAL.md selbst noch unverändert.

- 2026-11-27: Plan gegen `1ff1b95` geprüft (Code unverändert, 1.414 Zeilen, Enum 0..19) und als P4 priorisiert; `cinderreach-party-roles` ist 100 %, der S1-Abgleich ist damit nur noch lesende Kontrolle. Prozent bleibt 0 %.

## Nächste Schritte

- S1 aus dem Stufenplan: GOAL-Kriterium 3 auf Enum `0..19` und neue Aufrufer-Zahl korrigieren, Stand gegen `cinderreach-party-roles` abgleichen und die 7 Skill-Suites als Baseline einfrieren, danach S2 (`BowStanceVisuals`), S3 (`SkillBarHud`), S4 (Gesamtverifikation).

## Offene Punkte

- Umsetzung und Testnachweis noch offen.
