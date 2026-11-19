# Progress: cinderreach-party-roles

Prozent: 100 %

## Erledigt

- 2026-11-18: setgoal-Runde beantwortet (alle Empfehlungen übernommen: Fenwalker=Tank, Lanternbearer=Heiler, neu Rogue; Threat-Tabelle + Tank-AoE-Stun; Ziel-/AoE-Heal cooldown-basiert; alle Klassen Basis-Set, Spezial ×1.5; Solo-KI füllt auf 3 auf; max. 3, Koop ersetzt KI 1:1; health_goals G-DIABLO01/02/03/06).
- 2026-11-18: Ist-Stand belegt (HEAD `8d0965b`): `ClassCatalog` mit `fenwalker`/`lanternbearer`, `DiabloSkills.Skill` 0–6, Faktoren, `EnemyAttack.AcquireHero` (nächster Lebender), `PlayerHealth.Heal`, `HeroLocator`, Koop über `DiabloNetworkPlayer`/`HitValidator`.
- 2026-11-18: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-party-roles/tasks.md` (S1–S6) geschrieben; Eigner-`go` erteilt.
- 2026-11-19: S1 gelandet (`1842d1f`): Rollen (`Tank`/`Healer`/`Dps`), Rogue-Katalog (Faktoren 1,5, `Rogue.fbx`), Basis-Set-Gate (`CanCast`), Spezial-Formel (`SpecialtySkillMultiplier`), Rogue-Itempool (Nightfang/Keepsake). EditMode 311/311 + PlayMode 187/187.
- 2026-11-19: S2 gelandet (`75995ce`): `ThreatTable` (Schaden 1:1, Slam +20, kein Decay), `EnemyAttack`-Stun + Threat-Zielwahl mit Solo-Fallback, `SeismicSlam = 7` (3 Schaden, 3,5 m, 8 s, 2 s Stun). `ThreatTableTests` + `TankAggroTests` neu. EditMode 318/318 + PlayMode 191/191.
- 2026-11-19: S3 gelandet (`e388d05`): `QuickShot = 8` + `BladeFlurry = 9` (Rogue ×1.5), `MendingLight = 10` + `RenewingCircle = 11` (Heiler ×1.5, Reichweiten-/Cooldown-Gates), `ThreatTable.BookHeal`, `EffectiveHeal`. `PartySkillFormulaTests` + `PartySkillTests` neu. EditMode 326/326 + PlayMode 196/196.
- 2026-11-19: S4 gelandet (`e8a8b81`): `PartyCompanion` + `PartyRoster` (Solo-Auffüllung, Threat-/Leader-Ziel, Cooldown-Skills, Tod → Camp + Wiedereinstieg), `classOverride`, `HeroLocator.AllHeroes`. `PartyRosterTests` + `SoloPartyTests` neu. EditMode 344/344 + PlayMode 201/201.
- 2026-11-19: S5 gelandet (`5eb0f32` + Fix `1dce3e3`): Heal-Relay + Server-Threat (`SubmitHealServerRpc`/`HealTargetClientRpc`, `HitValidator.ValidateHeal`), Cast-Gate auf 0–11 geöffnet, Host-Reconcile (Rollenersatz 1:1, Join-Gate max. 3, Client ohne Phantom-KI). `CoopPartyTests` neu. EditMode 345/345 + PlayMode 212/212. Koop-Smoke Host-Tank + Client-Rogue Exit 0/0 (KI-Rogue tritt zurück, KI-Heiler bleibt, 0 Exceptions).
- 2026-11-19: S6 Beweise: Autoplay je Klasse Exit 0, 8/8 Phasen, 0 Exceptions — Fenwalker 163,0 s (52 Kills), Rogue 132,6 s (46 Kills, −18,7 %), Lanternbearer 189,7 s (51 Kills, +16,3 %), Band ±25 % gehalten, FPS-Pass; Ergebnis-JSON mit `partyRoles`/`huskKills`/`healsCast`/`hpRestored`; 34 Shotcheck-PNGs (26–29 Party-Skills); Q11 + Entscheid + `docs/party-roles-run-log.md`. EditMode 346/346 + PlayMode 228/228 (inkl. Peer-Tests), Verifies grün.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
