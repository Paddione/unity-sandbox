# Progress: cinderreach-party-team

Prozent: 100 %

## Erledigt

- 2026-11-19: Goal + Plan angelegt (GOAL.md, tasks.md); Worktree `fix/party-team-spawn-item-level`.
- 2026-11-19: S1 gelandet: `PartyRoster.ApplyRemoteRoles` (ein Join/Leave-Pfad für Host und
  Tests, `-1` bei Join-Gate), Spawn mit `Equipment` vor `PlayerHealth` + Level-Mirror,
  `SyncCompanionLevels` im `Update`, `PartyCompanion.Gear`/`Equip` (Klassenbindung),
  `DiabloProgression.MirrorHeroProgress` + Companion-Guards, `PlayerHealth.Refresh` pflegt
  `Equipment` nach.
- 2026-11-19: Tests neu: `PartyTeamLifecycleTests` 5/5 (Spawn kämpft/heilt/hält Threat;
  Despawn genau der Beitrittsrolle, Rest spielt weiter; Leave füllt ganz nach; Overfull `-1`
  ohne Mutation; Tod hält Slot bis Rejoin) + `PartyGearLevelTests` 4/4 (Gear-HP ab Spawn,
  Klassenbindung, Schaden/Heal-Boost + echter Ziel-Heal, Mirror-Mathe ohne Begleiter-XpCanvas).
  `.meta`-GUIDs als 32 Hex ohne Bindestriche (Bindestrich-Form ignoriert der Importer).
- 2026-11-19: Beweise: EditMode 358/358 + PlayMode 262/262 (Voll-Gate, inkl. aller
  Party-Bestands-Tests), `KayKitAssets.Verify` Exit 0 / 0 Exceptions,
  `python3 scripts/check-meta.py` OK, `python3 ~/scripts/agent-workspace.py check` still.
  Doku: Q12 in `docs/mechanics-questions.md`, Entscheid in `docs/decision-log.md`, Absatz in
  `docs/dev-journal.md`.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
