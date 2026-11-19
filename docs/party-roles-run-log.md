# Party roles run log (cinderreach-party-roles S6, 2026-11-19)

Linux player on WSLg GPU, `bash scripts/run-autoplay.sh <out> -class <id> -save-file party-s6-<id>.json`,
`-autoplay -autoplay-god`. Every run fields a full 3-role party: the hero plus two AI companions
(`PartyRoster` boot, `CoopSpawn_0` marker). Fresh save per class — a stale default save from an earlier
smoke overrode `-class` in the first attempt (all runs played fenwalker, town phase failed on a full bag);
isolated saves fixed it. Lesson: autoplay class proofs always need `-save-file`.

## Clear time per hero class (Normal, full route to the Kilnmother)

| Class | Duration | Kills | Party | Heals | HP restored | Phases | Exceptions | FPS worst |
|---|---|---|---|---|---|---|---|---|
| Fenwalker | 163.0 s | 52 | fenwalker, lanternbearer, rogue | 0 | 0 | 8/8 | 0 | 38.3 |
| Rogue | 132.6 s | 46 | rogue, fenwalker, lanternbearer | 0 | 0 | 8/8 | 0 | 34.0 |
| Lanternbearer | 189.7 s | 51 | lanternbearer, fenwalker, rogue | 0 | 0 | 8/8 | 0 | 34.7 |

Rogue is 18.7 % faster and Lanternbearer 16.3 % slower than the Fenwalker party, inside the ±25 %
acceptance band (party-vs-party; the second-class solo band compared a shorter route). FPS gate passes
on all three (worst judged window ≥ 30). Result JSON carries `partyRoles`, `huskKills`, `healsCast`,
`hpRestored` (schema 1, backward compatible).

Heals stayed 0 on all god runs: the godded hero holds top threat (slam 3 + 20 bonus, quick shot
3 per 0.3 s) and takes every hit, so the AI healer never found an injured ally. Heal mechanics are
proven by `SoloPartyTests` (fight + mend in the live loop) and `PartySkillTests` (target + group +
threat); a non-god endurance run stays a follow-up (real difficulty balance).

## Co-op smoke, Tank host + Rogue client

`COOP_HOST_CLASS=fenwalker COOP_CLIENT_CLASS=rogue bash scripts/run-coop-pair.sh` (direct UTP):

- Host log: `[Party] Lanternbearer joins` + `[Party] Rogue joins` at solo boot, `[Party] Rogue steps
  aside for a joining hero` on client join, `[Party] Rogue joins` again after the client leaves
  (leaver's role refills). The healer stays throughout.
- Client log: zero `[Party]` lines — no phantom AI on the joiner (first attempt spawned client-side
  AI because boot ran before the join; fixed via `-join`/`-join-code`/`--net-client` detection).
- Verdicts: host exit 0, full 8 phases, 0 exceptions; client exit 0, town + zone1, 0 exceptions.
  Host mirror shows Rogue, client mirror shows Fenwalker.

## Windows build and shotcheck

`BuildDiabloZoneWindows` OK, `stage-windows.sh` OK (`Games\cinderreach`, 123 MB),
`shotcheck-windows.sh` OK, 34 shots. New: `26_tank_seismic_slam.png` (slam ring, HUD
`[Space] Seismic Slam` on cooldown), `27_rogue_quick_shot.png`, `28_rogue_blade_flurry.png`,
`29_healer_renewing_circle.png`. The class select (`22_class_select.png`) now shows all three cards.

## Suites at land time

EditMode 346/346, PlayMode 228/228 (includes tests landed on main by peer sessions while S6 ran),
`KayKitAssets.Verify` green, `python3 scripts/agent-workspace.py check` still.
