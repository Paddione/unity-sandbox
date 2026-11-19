---
goal: cinderreach-party-team
slug: cinderreach-party-team
branch: fix/party-team-spawn-item-level
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: cinderreach-party-roles 100 % auf main
plan_refs: [.agents/goals/cinderreach-party-team/GOAL.md]
---

# Plan: cinderreach-party-team — eine Stufe

Pfad: fix, eine Stufe, ein Worktree (`fix/party-team-spawn-item-level`),
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`.

## S1 — Seams + Team-Tests + Gear/Level (einzige Stufe)

- `Party/PartyRoster.cs`: `ApplyRemoteRoles` (Join/Leave-Seam, `-1` bei Join-Gate),
  `ServerReconcile` delegiert dorthin; Spawn-Reihenfolge `Equipment` vor `PlayerHealth`
  (+ sofortiger Level-Mirror); `SyncCompanionLevels` im `Update` (Key-gecacht).
- `Party/PartyCompanion.cs`: `Gear`/`Equip` (Klassenbindung via `Equipment.CanEquip`),
  Doku auf Ausrüstung + Level-Mirror nachgezogen.
- `DiabloProgression.cs`: `MirrorHeroProgress` (Level/XP/Attribute/Ränge, quiet),
  Companion-Guards (kein Ambience-Target, keine XP-Leiste, kein Save, keine Fanfare);
  `PlayerHealth.Refresh` pflegt `Equipment` nach (spät hinzugefügtes Gear zählt).
- Tests: `PartyTeamLifecycleTests` (5 UnityTests: Spawn+Teamplay, Despawn+Weiterspielen,
  Nachfüllen, Overfull, Tod-Slot) + `PartyGearLevelTests` (4 UnityTests: Gear-HP,
  Klassenbindung, Schaden/Heal-Boost + echter Heal, Mirror-Mathe + kein XP-Balken).
- Verify: `PLATFORMS=EditMode TEST_FILTER=PartyRosterTests` + PlayMode-Filter
  (`PartyTeamLifecycleTests`, `PartyGearLevelTests`, `SoloPartyTests`, `CoopPartyTests`,
  `PartySkillTests`) grün, dann Voll-Gate `bash scripts/run-unity-tests.sh`,
  `KayKitAssets.Verify`, `python3 scripts/check-meta.py`,
  `python3 ~/scripts/agent-workspace.py check`.
- Doku: Q12 in `docs/mechanics-questions.md`, Entscheid in `docs/decision-log.md`,
  Absatz in `docs/dev-journal.md`, PROGRESS auf Belege fortschreiben.

## Validation Plan

- Höchstrisiko: Spawn-Reihenfolge bricht `PlayerHealth`-Cache (MaxHP ohne Gear) —
  gefangen durch `Companions_SpawnWithGearAwareHealth` (Gear ab Start + nach Equip).
- Zweitrisiko: Mirror/Sync schreibt Save oder spornt XP-Balken —
  gefangen durch Guards + `Companions_MirrorHeroLevelAndAttributes` (kein `XpCanvas`).
- Regression: unverändertes Solo-Verhalten ohne `DiabloProgression` am Leader
  (`SyncCompanionLevels` no-op) — gefangen durch bestehende `SoloPartyTests`.

## Risks / Open Questions

- Risiko: Begleiter-`DiabloProgression.Update` lief bisher mit HUD — Guard ändert nur
  Begleiter (Namens-/Komponenten-Check), Heldenpfad unverändert.
- Keine offenen Fragen: kein Bag/Vendor für Begleiter, Build-Spiegel statt eigener
  Skillung (Eigner-Vorgabe „auf dem Level des Protagonisten").
