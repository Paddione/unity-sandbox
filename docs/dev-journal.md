# Dev-journal — unity-sandbox

Per session: done / faced / learned / next. Against scope creep:
anything outside Q1–Q3 greybox goes to won't-have, not into the scene.

## 2026-11-14 — Greybox scene + code-owned controller (foundation)

- Done: Universal 3D (URP 17.0.4) project, `SandboxBuilder` greybox
  (ground, 6 blocks, 4 walls, light, camera, capsule player),
  hand-rolled `ThirdPersonController`; Linux player build (84 s, 91 MB)
  runs with zero errors (audio-only noise headless).
- Faced: Asset Store has no headless fetch — Starter Assets dropped in
  favor of the code-owned rig; template materials predate URP 17.
- Learned: headless batchmode builds the full loop without GUI.
- Next: trigger toy + toy/goal loop, questions written down.

## 2026-11-14 — MCP relay + keep-alive editor (agentic loop)

- Done: relay `unity-ai-relay v1.0.12` handshake (7 tools), `CommandScript`
  proof (`McpProvenCube` persists), fanned out to all harnesses;
  user service `unity-sandbox-editor.service` keeps the editor alive.
- Faced: stale sockets after editor PID died — cleaned, restarted.
- Learned: `Unity_RunCommand` needs `internal class CommandScript :
  IRunCommand` exactly; no GUI approval headless.
- Next: credit forecast, gateway check (other goal, not this one).

## 2026-11-14 — Q2/Q3 greybox + goal closeout (this goal)

- Done: `ToyTrigger` (counter/pickup/goal, `Needed = 3`) + scene wiring
  (`TriggerZone`, `Orb_0..2`, `GoalPad`) via `SandboxBuilder`;
  `VerifyGreybox` inventory proof; docs: GDD dates, questions,
  decision log, runbook, playtest protocol, run log.
- Faced: keep-alive editor holds the project lock — service stopped for
  the rebuild, restarted afterwards.
- Learned: one headless `BuildAll` covers scene + player proof for all
  three questions at once.
- Next: SFX placeholder (M3, 2026-12-05), share with friends (M4).

## 2026-11-14 — Q4 multiplayer-testable co-op (goal unity-multiplayer-demo)

- Done: NGO 2.5.0 + transport/lobby/relay pinned; `MultiplayerAutoStarter` (path-based clone detect, no ParrelSync dep); `ThirdPersonController` owner gate (solo unaffected); `SandboxBuilder.BuildNetworkLayer` (NetworkManager + transport assignment + NetworkPlayer prefab + prefabs list); `VerifyMultiplayer`; `MultiplayerConfigTests` (3 tests); `BuildAll` green (15.9s, 95MB); EditMode 6/6; host + `--net-client` run logs clean.
- Faced: keep-alive editor locked the project (stopped service for batch work); services lobby/relay 2.0.0 don't exist (used 1.3.0/1.2.0); `NetworkConfig.NetworkTransport` must be assigned explicitly ("No transport has been selected" + NRE in StartHost until fixed); Sandbox.Runtime needed `Unity.Networking.Transport` asmref.
- Learned: base manifest already fails Unity resolve when the wrong project is open — always pass `-projectPath`; `NetworkTransform` (not ClientNetworkTransform) ships in NGO 2.5 Components.
- Next: ParrelSync 2-editor live join, lag pass, silent playtest triage; commit + push + restart editor service.

## 2026-11-15 — Q5 Diablo click-to-move kill-loot (goal unity-diablo-prototype)

- Done: Q5 mechanics question; `DiabloClickController` (iso click + attack-move) + `DiabloMelee` (range/cooldown) + `EnemyHealth` (HP/flash/death orb-drop); `ToyTrigger` activator widened to tag fallback; `Husk` tag; `SandboxBuilder.BuildDiabloScene/VerifyDiablo/BuildDiablo` (Knight hero, 3 Skeleton husks, tile floor 25, wall ring 24, torches, chest GoalPad); `KayKitAssets` extended to 5 packs (Adventurers 6, Skeletons 4, Rig_Medium 8 anim rigs); EditMode 9/9 + PlayMode 4/4; diablo player build Succeeded (36.6s, 98MB), 45s run clean (0 exceptions).
- Faced: `-quit` kills `-runTests` before execution (omit `-quit`; runner exits itself); controller cached sibling `DiabloMelee` in `Awake` before it existed (lazy resolve fix, test-found); KayKit figure packs use `Characters/` not `Models/` + need URP check without atlas remap; type is `EditorBuildSettingsScene`.
- Learned: PlayMode `InputTestFixture` virtual devices work headless; `VerifyDiablo` inventory pattern scales; owner GUI import mapped URP materials correctly (Verify green first try).
- Next: visual-fit check in editor, hero accessories, Rig_Medium animation wiring, one human feel session (follow-ups, not criteria).

## 2026-11-15 — Q6 Diablo P2 co-op loot-zone (plan unity-diablo-p2-multiplayer-loot-zone)

- Done: P1 host-authoritative netcode (`DiabloNetworkPlayer` owner-gate + validated `SubmitAttackServerRpc`, `DiabloNetworkBootstrap` Resources-prefab wiring, `MultiplayerAutoStarter` scene log); P2 item data (`ItemDefinition/AffixLibrary/ItemDatabase/Equipment`, 3x3 combos + 3 fixed uniques); P3 loot (`LootTable/DroptableDatabase/LootDrop/LootNetRelay`, `EnemyHealth` tier + `RequestDamage` + item-drop hook, Q5 orb compat kept); P4 `BuildDiabloZone/VerifyDiabloZone/BuildDiabloZoneBuild` + `NetworkPlayerDiablo` prefab + scene (6 husks, elite 15 HP, chest Rare+, 4 coop spawns); P5 `DiabloP2ConfigTests` + `DiabloP2LoopTests` + Q6 docs; EditMode 14/14 + PlayMode 10/10; zone build Succeeded (84.7s, 99MB), host-start run 0 exceptions.
- Faced: `NetworkPrefabs` not enumerable (dedup via PlayerPrefab-compare); `StandaloneBuildSubtotal` typo (fixed to `Subtarget`); `Destroy` applies at frame end — kill asserts need one yielded frame (Q5 waited 6s; mine checked immediately).
- Learned: `NetworkManager.Singleton` safe-guards (`null`/`IsListening`/`IsServer`) keep solo path exact; `.meta` files generate on import and must be committed; `DefaultNetworkPrefabs.asset` dirties on Prefabs.Add (same as Q4 commit).
- Next: land via `agent-workspace.py land` (ff); follow-ups (not criteria): Relay cross-machine join, server cooldown on relay swings, client-pickup convergence pass, 5-person playtest.

## 2026-11-15 — Q6 entry + run animation (goal unity-diablo-prototype follow-up)

- Done: ghost-hero yield (`DiabloClickController.ShouldYieldToNetcode` + Awake gate — scene hero stands down when netcode runs, pure rule is EditMode-pinned); prefab spawn moved to the entry stairs; `ZoneEntry` trigger with one-shot onboarding log; Mecanim locomotion (`DiabloLocomotion.controller`: Idle_A/Running_A retargeted, "Moving" bool) via `DiabloAnimator` on hero + network prefab; `FpsReporter` already landed for perf proof.
- Faced: KayKit Knight ships with an auto-Animator (Humanoid import) — legacy Animation plan caught by the new config test (double-drive), switched to Mecanim; `AddCondition` does NOT create the controller param (explicit `AddParameter`); building the controller twice orphans the first ref (per-session cache).
- Learned: EditMode tests can assert AnimatorController states/params; PlayMode `IsMoving` pins driver input without frames; take names live in the FBX (`Idle_A`, `Running_A`), not in the .meta.
- Next: Windows + server rebuilds with this branch, retest with the user (run anim + entry hint), husk idle as follow-up.

## 2026-11-15 — Twin-stick hybrid controls (WASD + mouse aim/strike)

- Done: WASD/arrows move camera-relative with priority (`PollKeyboard`, `MapInput`), hero faces the cursor every frame (`SetAimPoint`, snap), left-click strikes in a 60° arc (`Strike` → `FindArcTarget`/`TryAttackDirection`; clicked husk keeps Q5 attack-move). Hint text + Q6 setup updated. Tests: WASD via `InputTestFixture` (repo precedent), aim/arc/anim pure + in-scene.
- Faced: `Keyboard.current` exists but idle in headless runners — `SetMoveInput` got wiped each frame; programmatic `SetMoveInput` only survives without a keyboard, so the movement test uses synthetic key presses instead. Same-frame `== null` after `Destroy` bit again in the strike test (yield one frame). `WaitSeconds` helper lives only in the Q5 file.
- Learned: no existing test clicked through the real mouse path — repurposing left-click broke nothing; Q5 APIs untouched and green.
- Next: rebuild Windows + server builds, redeploy, user retest (WASD feel, strike arc); gamepad stick as follow-up.

## 2026-11-15 — Strike feedback + self-raycast fix (click felt dead)

- Done: every click swings (`Strike` → `DiabloAnimator.OnStrike`, `strikeCount`; `Melee_1H_Attack_Chop` on a "Strike" trigger, Any-State in/out); hero + prefab on Ignore Raycast (recursive — layer is per-object, meshes live on children); whiff deals no damage but animates. Tests: config pins layer recursion + controller states/params, PlayMode pins whiff (swing, no kill) vs arc hit.
- Faced: KayKit hero ships with an auto-Animator (Humanoid import) — legacy Animation would double-drive; Mecanim retargeting is the intended path. `AddCondition` does not create the parameter; rebuilding the controller orphans earlier refs (per-build cache).
- Learned: silent misses are the worst feedback — a whiff must always be visible. Aim rays must ignore the shooter.
- Next: rebuild Windows + server, redeploy, user retest (whiff swing, close-range aim); husk idle anims as follow-up.

## 2026-11-15 — Blood Moor + Rogue Encampment (inventory, starter sword)

- Done: zone reworked in place (encampment, 10 husks, brute, campfire heal, starter auto-equip); `Inventory` + `DiabloInventoryUI` (I panel, click-equip, `ShouldBlockClicks` guard); `PlayerHealth` + `CampfireHeal`; pickup routing (`CollectInto`) with full-bag equip-overflow so `ToyTrigger` pickup counts stay consistent. Tests: new EditMode `DiabloBloodMoorTests` + PlayMode `DiabloBloodMoorLoopTests` (kill → bag → panel-equip → campfire heal).
- Faced: `edit_file` near-miss on `Equipment.Equip` (log-line wording) — rewrote the file cleanly instead of stacking edits; `ItemInstance` needed `[Serializable]` or the builder-equipped starter would vanish on scene save; palisade log count depends on the angle skip (20 logs → 15 built, verify pins ≥ 12).
- Learned: bag-full must never strand loot — overflow-equip keeps the old pickup-count contract intact.
- Next: run suites + VerifyDiabloZone, rebuild Linux/Windows/server, redeploy, user retest (I-panel, campfire heal, moor clear).

## 2026-11-15 — Blood Moor test failures (gear damage + trigger sync)

- Faced: PlayMode kill test failed — `DiabloMelee.damage` (flat 1) ignored `Equipment` entirely (zero gameplay consumers of `GetDamage`); fixed in product via `DiabloMelee.CurrentDamage` (weapon stats, fallback flat), also used by `DiabloNetworkPlayer.Attack`. No existing test changed meaning: bare heroes still swing 1.
- Faced: PlayMode campfire test healed 0 — project sets `m_AutoSyncTransforms: 0`, so test teleports (`transform.position`) never reach the physics world; fixed in test via `Physics.SyncTransforms()` after teleports (product untouched — the game walks via `CharacterController.Move`, always synced).
- Learned: no trigger callback in this project was ever observed firing before; headless trigger tests must sync transforms after any teleport.

## 2026-11-16 — D2 combat feel (Q8, solo-first)

- Done: `DiabloAudio` synth (Application.isPlaying-guard für DontDestroyOnLoad — EditMode wirft sonst); Step-in via `CharacterController.Move`; Knockback als Positions-Shift (keine Physik, kein Trigger-Kontakt); `EnemyAttack` per Distanz-Math (autosync-sicher); Target-Bar mit Test-Seam; `LogAssert`-Lehre: Headless-Audio läuft auf nosound und warnt pro Frame — Umgebungsrauschen, keine Produkt-Aussage (Submit-Test prüft Dispatch + Modultyp statt Logs).
- Faced: 1,2-m-Knockback hat `Strike_HitsHuskInFacingArc` gebrochen (Normals flogen aus der 2,2-m-Arc) — kein Test-Fix, sondern Produkt-Tuning auf 0,6/0,2 m (Combos müssen connecten, D2-Pop statt Punt). Zweitens: `GetComponent<InputSystemUIInputModule>` nach `AddComponent` war null, weil ich die Add-Zeile beim Umschreiben verloren hatte — der Test hat es gefangen.
- Learned: EditMode-`AddComponent` + `DontDestroyOnLoad` = sofortiger Throw; `Physics.SyncTransforms` allein teleportiert keinen aktiven CharacterController (disable/move/enable nötig — aus Blood-Moor-Tests bestätigt); `LogAssert.Expect(string)` matcht exakt, nicht per Regex, und die Reihenfolge zählt.
- Next: committen, landen, 3 Targets via Wrapper bauen, Server deployen + Join-Smoke, Windows-Build stagen; dann Goal-PROGRESS fortschreiben (< 100 %, Playtest offen).

## 2026-11-16 — Handschwert + Husk-Anmarsch (Playtest-Follow-up)

- Befund: Schwert hing an der Hüfte (Avatar nicht humanoid → GetBoneTransform wirft); Normals starben im ersten Hit vor dem 0,4-s-Windup; Husks standen statisch herum.
- Fix: Schwert an `handslot.r` (KayKit-Waffenhalterung, per-Namen-Suche, Hip nur Fallback + Test pinnt Hand-Mount); Normal-HP 3→5 (zwei Starter-Hits, Rache trifft einmal); Husks laufen im 8-m-Radius an (1,5 m/s, Stopp 1,8 m) und wenden sich zu.
- Belegt: EditMode 36/36 (Hand-Mount, Aggro-Konstanten), PlayMode 28/28 (Anmarsch-Test), VerifyDiabloZone OK.

## 2026-11-17 — Feel-Polish (Husk-Rig, Leichen, Umweg, Hit-Stop)

- Done: `HuskAnimator` + Builder-`WireHuskAnimator` (Skeletons teilen Rig_Medium, gleiche Takes wie der Held); `DiabloCorpse`; Steering in `EnemyAttack` (Fächer 0/±35/±70/±105°, Husks und Helden blockieren nie, Separation per Distanz); `HitStop`; `Diablo`-Prädikate (`IsLocalAuthority`, `IsSolo`). Tests: EditMode `DiabloFeelPolishTests`, PlayMode `DiabloFeelPolishLoopTests` (Flash am Kind-Renderer, Leiche inert + versinkt, Umweg um Wand, Attack-Take, Hit-Stop stellt timeScale wieder her).
- Faced: Hit-Flash suchte den Renderer am Root — KayKit-Meshes hängen an Kindern, der Flash lief seit Q5 ins Leere. Solo-DiabloZone läuft als NGO-Host, ein „nur ohne Netcode"-Gate hätte Leichen und Hit-Stop im echten Spiel abgeschaltet → Leiche aus `EnemyHealth.corpseModel` (FBX-Referenz) statt Klon. `BuildDiabloScene` wirft DiabloZone aus den Build-Settings (vorbestehend; Datei auf HEAD zurückgesetzt).
- Learned: Testhusks aus Primitiven verdecken Rig-Annahmen — neue Tests nutzen einen leeren Root mit Mesh-Kind.
- Next: Windows-GPU-Playtest (deckt auch das offene Kriterium 5 von `unity-diablo-d2-combat-feel` ab).

## 2026-11-17 — Doppelter Held im Host-Modus

- Befund: DiabloZone enthält einen Solo-Helden `Player` am selben Spawn (0, 0.05, 11) wie `NetworkPlayerDiablo`. Solo läuft als NGO-Host, der Solo-Held schaltete nur seinen Click-Controller ab und blieb sichtbar stehen. Folgefehler: Husks, Target-Bar und Pickups suchen den Helden per Namen `Player` und trafen den stehenden Doppelgänger.
- Fix: `DiabloNetworkPlayer.OnNetworkSpawn` (nur Owner) deaktiviert den Solo-Helden und übernimmt lokal den Namen `Player` (Namen werden nicht repliziert, Remote-Mirrors behalten ihren Clone-Namen).
- Belegt: EditMode 41/41 (`DiabloHeroTakeoverTests`), PlayMode 33/33, Linux-Zonen-Build Host-Lauf: `[Net] Owned hero took over 'Player' (solo hero stood down=True)`, 0 Exceptions.
- Faced: aus dem laufenden Warm-Editor kopierte Library band Szenen-MonoBehaviours nicht (9 Szenentests rot bei korrekten GUIDs) — frische Library behebt es. Worktree-Library nie bei laufendem Editor kopieren.
