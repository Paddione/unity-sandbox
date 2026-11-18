# Progress: cinderreach-party-roles

Prozent: 0 %

## Erledigt

- 2026-11-18: setgoal-Runde beantwortet (alle Empfehlungen übernommen: Fenwalker=Tank, Lanternbearer=Heiler, neu Rogue; Threat-Tabelle + Tank-AoE-Stun; Ziel-/AoE-Heal cooldown-basiert; alle Klassen Basis-Set, Spezial ×1.5; Solo-KI füllt auf 3 auf; max. 3, Koop ersetzt KI 1:1; health_goals G-DIABLO01/02/03/06).
- 2026-11-18: Ist-Stand belegt (HEAD `8d0965b`): `ClassCatalog` mit `fenwalker`/`lanternbearer`, `DiabloSkills.Skill` 0–6 (Fireball/Arrow/Cleave, LanternBolt/Wisps/SearingHalo/VeilStep), `ClassDefinition.focusSkillFactor`/`strengthMeleeFactor`, `EnemyAttack.AcquireHero` wählt nächsten lebenden Helden, `PlayerHealth.Heal` vorhanden, `HeroLocator` Solo/Hero, Koop über `DiabloNetworkPlayer`/`HitValidator`.
- 2026-11-18: Goal persistent unter `.agents/goals/cinderreach-party-roles/` angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 % deklariert) und Ausführungsplan `.agents/plans/cinderreach-party-roles/tasks.md` geschrieben (Stufen S1–S6). Noch kein Feature-Code geändert; Ausführung startet erst mit Eigner-`go`.

## Nächste Schritte

1. S1 Rollen-Daten + Basis-Set (Plan `tasks.md`): Rogue in `ClassCatalog`, Rollen-/Faktor-Felder, Basis-Skills für alle Klassen, Faktor-Test.
2. Danach S2–S6 in Plan-Reihenfolge (je eigener Worktree, `land` pro Stufe).

## Offene Punkte

- Ausführung (S1–S6) steht aus — Start erst mit Eigner-`go`; Goal + Plan liegen ungelandet im Worktree `feature/cinderreach-party-roles-plan` zur Sichtung.
- Rogue-Modell/Key-Belegung und genaue Skill-Zahlen (Schaden/Cooldown/Threat-Multiplikator) werden in S1/S3 aus dem Bestand abgeleitet und per Tests gepinnt, nicht vorab erfunden.
