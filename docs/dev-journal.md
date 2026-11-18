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

## 2026-11-17 — „Reset zum Spawn" nach wenigen Treffern

- Befund: kein Teleport-Bug, sondern Tod. `Equipment.BaseMaxHP` war 3, Husks treffen mit 1 (Elite 2) — 2–3 Treffer → `PlayerHealth.DieAndRespawn` an `CoopSpawn_0`, ohne HP-Anzeige oder Meldung. Vorher unsichtbar, weil die Husks den Doppelgänger-Helden angriffen (Fix 24eb42e).
- Fix: `BaseMaxHP` 20, `CampfireHeal.HealPerSecond` 5; neue `DiabloLifeGlobe` (Kugel unten links, `cur / max`, Todesmeldung 3 s), vom ersten `PlayerHealth` selbst erzeugt — kein Szenen-Rebuild.
- Belegt: EditMode 41/41, PlayMode 35/35 (`LifeGlobe_DrainsAndAnnouncesDeath`, `HeroSurvivesSeveralHuskStrikes`).

## 2026-11-17 — Ein Windows-Spielordner (SSOT)

- Befund: `C:\Users\PatrickKorczewski\Games` hatte drei Kopien desselben Spiels (`diablo-zone.exe`): `diablo-zone-strike` (15.11., Strike-Meilenstein), `diablo-bloodmoor` (17.11. früh, Stage-Ziel aus dem combat-feel-Plan) und `diablo-zone`. Jede Phase hat von Hand in einen neuen Ordner kopiert, ein Skript gab es nicht.
- Fix: `scripts/stage-windows.sh` spiegelt `builds/diablo-zone-windows` per `rsync --delete` nach `Games\diablo-zone` und schreibt `BUILD_INFO.json`; Plan-Verweis auf `diablo-bloodmoor` ersetzt; die zwei Altordner gelöscht (nur Build-Dateien, keine Spielstände, keine Verknüpfungen).

## 2026-11-17 — S1 identity (Cinderreach / Ashen Fen)

- Done: alle spielersichtbaren Strings + Kommentare + `WorldArtBuilder`-Objektnamen auf die Identitäts-Tabelle umgestellt (Ashen Fen, Lantern Hold, Fen Gate, Fen Brute, Ash Elite); `productName: Cinderreach`; `BuildDiabloZoneWindows()` → `builds/cinderreach-windows/cinderreach.exe`; `stage-windows.sh`/`shotcheck-windows.sh` → `Games\cinderreach` (Altordner `Games\diablo-zone` bleibt unangetastet, nur als Kommentar vermerkt); Art-Bible in `STARTERPACK.md`, `build_props.py`, `DiabloPalette.cs` auf Ashen Fen (`BloodBanner` bleibt); neue `EditMode/IdentityTests.cs` (Szenen-Namenskatalog + Hint-Quellen, Regex-Gate); Test-IDs `DiabloBloodMoor*` → `DiabloAshenFen*`; Setting-Absatz in `docs/one-page-gdd.md`.
- Faced: `grep`-Regex trifft auch Historie (Journal, decision-log, mechanics-questions), Asset-Pfade (`Assets/Art/BloodMoor`, `bake_moor_ground.py`) und die Regex-Definition in `IdentityTests.cs` selbst — bewusst ausgenommen (GUIDs/Verlauf bleiben), im PROGRESS dokumentiert.
- Learned: `Husk_Elite`/`Husk_Brute` sind Szenen-Objektnamen (Target-Bar-sichtbar) → mit `VerifyDiabloZone` und `DiabloP2ConfigTests` zusammen umbenennen, sonst bricht Verify.
- Belegt: EditMode 58/58, PlayMode 59/59, `VerifyDiabloZone` + `KayKitAssets.Verify` grün, `check-meta.py` OK.
- Extra-Fix: `DiabloAudio.EnsureBuilt` (latent): Szenen-Ladungen zerstörten die statisch gecachten Synth-Clips
  (`built` blieb true) → `AudioClips_AreValidMono22050` war reihenfolgeabhängig rot. Jetzt Rebuild bei totem Clip.
- Next: dotfiles-Anteile (`diablo_unity.py`-Probe) laufen NICHT hier (eigener Worktree, Orchestrator); S2 nach S1-land.

## 2026-11-18 — S2 economy+save (Cinderreach-Session, Stufe S2)

- Done: `Wallet` (pur, `Add`/`TrySpend`/`SetGold`, `EnsureWallet`) am Helden + Netz-Prefab wie `Inventory`
  (Builder + `VerifyDiabloZone`- + Prefab-Check); `GoldDrop`-Pickup (Normal 70 % 3–8, Elite 100 % 15–30,
  Chest 40–60, Konstanten + `GetGoldTable` in `DroptableDatabase`), unabhängiger Roll in
  `LootDrop.SpawnLoot` neben der Item-Kugel (Q5-Orb-Zähler unberührt); `SaveGame` (v=1, S4-Punktfelder
  forward-kompatibel) + `SaveService` (`cinderreach-save.json`, tmp+rename, Autosave Level-Up + Quit,
  `SaveNow(hero, zoneReached)`-Hooks für S5-Gate/S3-Vendor als Kommentar, korrupt → `*.corrupt` + Warnung);
  `DiabloProgression.GetState/SetState` (Level-Up-Autosave); Tests `SaveGameTests` (7, Roundtrip/Version/
  Korrupt/Wallet/Tabellen) + `EconomyLoopTests` (3, Pickup-Bilanz/Drop-Bilanz/Reload).
- Faced: Worktree enthielt uncommittete S2-Vorarbeit (alle Produkt-/Test-Dateien bereits angelegt);
  übernommen und vervollständigt (Builder-Wallet-Verdrahtung + Verify-Pflicht + Szenen-Rebuild fehlten).
  Kein Menü-Szenen-Code im Repo (`DiabloMenuTests` = In-Game-Tab-Menü) → Continue/New-Game als TODO
  dokumentiert statt erfunden (`HasSave`/`TryLoad`/`DeleteSave` sind die Menü-Seams).
- Learned: `VerifyDiabloZone` als Pflicht-Gate fängt fehlende Builder-Verdrahtung sofort (Wallet-Test);
  Szene nach Builder-Änderung immer per `BuildDiabloZone` regenerieren, nie von Hand.
- Belegt: EditMode 65/65, PlayMode 62/62, `VerifyDiabloZone` OK (Wallet an Bord), `KayKitAssets.Verify`
  grün, `check-meta.py` OK. Nächste: S3 (Vendor gegen Gold, `SaveNow` am Vendor-Schluss).

## 2026-11-18 — S4 progression (Charakterfortschritt mit Wahl)

- Done: `CharacterSheet` (pur: 1 Attribut- + 1 Skillpunkt/Level, Ränge 1–5 je Skill +20 % Schaden/Rang,
  Strength +1 Nahkampf/Punkt, Vitality +5 Life/Punkt auf +3 Life/Level-Basis, Focus +8 % Skillschaden
  und −3 % Cooldown/Punkt mit Floor 50 %); `DiabloProgression` mit Sheet, erweitertem GetState/SetState
  (Save-Roundtrip via `SaveService`, Level-Up-Banner „+1 Attribut, +1 Skill“); `DiabloSkills.Cleave`
  (R, 360°, 6 s Cooldown, Rang erweitert Radius +0,4 m/Rang) + `SkillArtBuilder.BuildCleaveArc`;
  `DiabloCharacterUI` (Taste `C`, HD-Scaler 1920×1080, ungespendete Punkte pulsieren); `DiabloMelee`
  liest Strength-Bonus, `SaveGame`/`SaveService` persistieren alle Punktfelder; Tests
  `CharacterSheetTests` (6) + `SkillRankTests` (4).
- Faced: Worktree enthielt uncommittete S4-Vorarbeit (Produkt-, UI-, Test- und Prefab-Dateien bereits
  angelegt); übernommen und vervollständigt (Statustabelle, Journal, PROGRESS fehlten).
  `DiabloLocomotion.controller` (LFS-Pointer) war versehentlich modifiziert → revertiert (außerhalb Scope).
- Learned: Save-Roundtrip-Test deckt neue Felder sofort ab (`freeSkillPoints` fehlte zuerst in `SaveGame`).
- Belegt: EditMode 72/72, PlayMode 66/66, `VerifyDiabloZone` OK
  („player=True husks=10 … net=True“, „OK (ashen fen + loot + netcode present)“),
  `KayKitAssets.Verify` grün (Exit 0, alle Kits verified), `check-meta.py` OK.
  Nächste: S5 (Ossuary + Boss, Balancing gegen Level 5–8).
## 2026-11-18 — S3 town+vendor (Cinderreach-Session, Stufe S3)

- Done: Camp-Block (Palisade 15, Campfire + Heal-Trigger, 4 ZTorch) aus `BuildMoorField` nach
  `Editor/TownBuilder.BuildCamp()` verschoben (verbatim, gleiche Namen/Reihenfolge) + `SpawnVendor()`
  (KayKit `Adventurers/Characters/Rogue_Hooded.fbx`, CapsuleCollider, `Vendor.zoneSeed = 7`,
  Idle-Take `Idle_A` aus `Rig_Medium_General.fbx` auf geteiltem `Resources/VendorIdle.controller`);
  `SandboxBuilder` ruft `BuildCamp()` + `SpawnVendor()` und hängt `DiabloVendorUI` an Held + Netz-Prefab
  (Verify: `vendor` + `vui` + Prefab-Check). `Vendor` (Klick nur in `InteractRadius` 3.5 m, Taste V/Esc,
  Trank mit Taste 1 via `DrinkPotion`), `VendorStock` (6 seeded Offers 3×Magic/2×Rare/1×Unique,
  Preise 25/90/300, Verkauf 25 %, `BuyPotion`), `DiabloVendorUI` (UGUI Runtime wie InventoryUI,
  Spalten Vendor/Bag, Gold-Label, HD-Scaler 1920×1080/0.5, `ExternalDrive`-Lock, Schluss → `SaveNow`).
  Consumable-Seam: `ItemSlot.Consumable`, `ItemDatabase.CreateHealingPotion` (10 g, +10 HP),
  `Inventory.RemoveAt`. Tests `VendorTests` (10: Preise, 6er-Seed-Stock, Kauf-ohne-Gold, Kauf/Verkauf-Bilanz,
  Bag-voll-Block, Trank-Heilung/Konsum, Radius) + `VendorLoopTests` (3: UI frisst Klicks/Schluss speichert,
  UI-Kauf/Verkauf/Trank, Radius-Gate) + `DiabloHdTests.Vendor_UsesHdScaler`.
- Faced: Worktree enthielt uncommittete S3-Vorarbeit (Town/Scripts/Tests bereits angelegt);
  übernommen und vervollständigt (Verify-Verdrahtung vendor/vui + Prefab-Check, HD-Test, Szenen-Rebuild).
  Editor-Lauf berührte `DiabloLocomotion/HuskLocomotion.controller` (LFS-Pointer-Drift, 3–6 B) →
  revertiert; `NetworkPlayerDiablo.prefab` + `DiabloZone.unity` sind echte Builder-Änderungen (bleiben).
- Learned: Refactor-Beleg per Verify-Counts: vorher per Code (20-Log-Ring minus 5 Nord-Lücke = 15,
  4 Fackel-Spots) = nachher Verify-Log `palisade=15 torches=4` (Pflicht-Gates `palisade<12`, `torches<4` grün).
- Belegt: EditMode 76/76, PlayMode 65/65, `VerifyDiabloZone` OK
  (`player=True husks=10 ... palisade=15 torches=4 net=True`), `KayKitAssets.Verify` grün
  (Dungeon 211, Adventurers 6 Modelle), `check-meta.py` OK. Nächste: S5 (Ossuary + Boss).
- 2026-11-18 (S5 zone2+boss): Sunken Ossuary + The Hollow Abbot + Abschluss.
  Neu: `Editor/OssuaryBuilder` (Annex bei +200 m X, Bodenplatte/Pfeilerring, 4 Sarkophage,
  4 Fackeln, KayKit-Torch-Dressing wo vorhanden, Tor-Paar `FenGate`↔`OssuaryGate`, 8 Bone Wardens
  + 2 Elites, Boss am Nord-Apsis), `Scripts/Zones/ZoneGate` (Trigger → Teleport +
  `zoneReached`-Autosave), `Scripts/Enemies/RangedCaster` (6–8 m Abstand, 0,6 s Telegraph,
  `DiabloProjectile`-Wiederverwendung, host-autoritativ), `Scripts/Enemies/BossController`
  (150 HP, Phase 2 ab 50 % mit 3 Husks + Projektilfächer, Tod via `EnemyHealth` + Poll →
  `SessionComplete`), `Scripts/SessionComplete` (Zeit/Kills/Gold/bester Drop, Town-/Menü-Buttons,
  HD-Scaler 1920×1080/0,5), `LootTier.Boss` (Rare+ garantiert, 150–250 Gold, 120 XP).
  `SandboxBuilder` ruft `BuildOssuary` + `VerifyOssuary` (Root/Gate-Paar/≥8 Caster/Boss).
  Tests: `OssuaryConfigTests` (5: Tuning, 50-%-Schwelle + Idempotenz, Rare+-Garantie über 50 Rolls,
  Gold 150–250, Caster-Abstand) + `BossLoopTests` (2: Gate teleportiert + speichert Zone 2/Rückweg,
  Boss-Tod öffnet Abschluss) + HD-Scaler-Test für SessionComplete.
- Faced: `SessionComplete_UsesHdScaler` rot (NRE: `AddComponent<Text>` direkt auf dem Button-Objekt);
  Label als Child-Text angelegt (Standard-Pattern) → grün. Editor-Lauf berührte die drei
  Locomotion/Idle-Controller (LFS-Drift, je 2 B) → revertiert wie in S3; `DiabloZone.unity` +
  Szenen-Rebuild sind echte Builder-Änderungen (bleiben).
- Learned: Boss-Tod gehört `EnemyHealth` (Loot/XP-Seams bleiben konsistent); der Controller pollt
  nur die Fraktion im selben Frame vor dem Deferred-Destroy und öffnet den Abschluss — kein
  Doppel-Loot, kein Eingriff in fremde Dateien.
- Next: S6-Folgepass nach S5-Rebase (Ossuary-/Boss-Beine, S5-Shots, Q9 erneut belegen).
## 2026-11-18 — S6 proof, Basis-Teil ohne S5 (SessionAutoplay + Shotcheck + Docs)

- Done: `Scripts/SessionAutoplay.cs` (neu, `-autoplay` inert ohne Flag; Wegpunkte
  Campfire/Vendor/Tor-Pfeiler, Kampf per `DiabloMelee` + Q/E-Skills, Trank-Kauf über
  S3-Seam `VendorStock.BuyPotion`, Fen-Gate-Marsch; Ossuary-Root, `ZoneGate`, Bone
  Warden, The Hollow Abbot, `SessionComplete` als benannte S5-Seams mit SKIPPED-Log;
  Cheat nur `-autoplay-god`, im Log ausgewiesen; Phasen- + Exception-Zähler, Exit 0/1).
  `DiabloShotCheck` + `town_lantern_hold`, `vendor_open`, `character_open`
  (+ S5-Shot-TODOs); Docs: Q9 (`mechanics-questions.md`), Q9-`continue (Basis)`
  (`decision-log.md`), `one-page-gdd.md` (S1–S4-Stand), `docs/session-loop-run-log.md`;
  `tasks.md` → S6 `in Arbeit (Basis ohne S5)`, PROGRESS bei 55 % + Basis-Eintrag.
- Faced: erster Autoplay-Lauf meldete `kills~0` bei 12 echten Kills — der Zähler las eine
  bereits zerstörte Husk-Referenz statt Vorher/Nachher zu vergleichen; auf
  `CountHusks`-Differenz umgestellt, neu gebaut, Neulauf `kills=12`. Windows-Shots
  zeigten das Vendor-Panel schon im Town-Shot (Öffnungszustand aus dem Shot-Ablauf,
  kein Produktfehler).
- Learned: `BuildDiabloZoneBuild` schreibt absolut nach `~/unity-sandbox/builds/` —
  aus dem Worktree gebaut landet das Artefakt im Hauptcheckout (git-ignored, ok);
  `xvfb-run` trägt den Linux-Player headless mit 0 Exceptions.
- Belegt: EditMode 83/83, PlayMode 69/69, `VerifyDiabloZone` OK
  (`player=True husks=10 … palisade=15 torches=4 net=True`), `KayKitAssets.Verify`
  grün, `check-meta.py` OK; Linux-`-autoplay`-Lauf Exit 0, 0 Exceptions;
  Windows stage + shotcheck OK (18 Shots, 3 neue visuell geprüft).
- Next: Orchestrator rebased S5 → Folgepass (Ossuary-/Boss-Beine, S5-Shots,
  Q9-Verdikt erneut belegen, S6 auf landed).

## 2026-11-18 — S6 proof Vollpass (Ossuary/Boss-Beine, Orchestrator-Übernahme)

- Done: S5-Folgepass aus dem S6-Worktree (Muse-Harness verweigerte 2× den
  Follow-up mit `tool_choice`-Fehler; Übernahme nach Retry-Budget): `SessionAutoplay`
  spielt die ganze Schleife (Fen → FenGate → Ossuary → Boss → Rückweg),
  `DiabloShotCheck` mit 4 S5-Shots, Boss-Tod-Race gefixt (`OnBossDeath` im
  `EnemyHealth`-Tod statt Poll — Deferred-Destroy gewann sonst), Gate-Bounce
  gefixt (Ankunft 8 m frei vom Rückweg-Tor), `D2FeelTests` Boss-Tier-3-Schaden.
- Belegt: Linux-`-autoplay`-Vollpass Exit 0, 0 Exceptions,
  `DONE phases=7 huskKills=13 bossKills=1 phaseTwo=1 overlay=1`;
  Windows stage OK + 22 Shots (19–22 neu, echte Renders);
  EditMode 89/89, PlayMode 71/71, beide Verifies grün, `check-meta.py` OK.
- Faced: Controller-LFS-Drift nach jedem Editor-Lauf (revertiert wie S3/S5);
  Windows-Exe unverändert (nur `cinderreach_Data` frisch — normal, Launcher stabil);
  Ossuary-Leash findet keine Ziele (kills=0 dort, Wardens fallen im Boss-Kampf) —
  ehrlich im Run-Log vermerkt, Follow-up Leash/Anmarsch außerhalb dieses Plans.
- Learned: Retry-Budget diszipliniert einhalten (2 Fehlversuche → Übernahme
  statt drittem Dispatch); Riesen-Logzeilen (`grep -a`, Größen statt `cat`).
- Nächste: dotfiles G-DIABLO02-Vertrag (Ossuary/Boss-Prefade), dann 100 %.

## 2026-11-18 — cinderreach-loop-guard S1 result (Ergebnis-JSON + run-autoplay.sh)

- Done: `AutoplayResult` (schema 1: commit, started, duration_s, phases, exceptions, fps_windows, exit;
  Pflichtphasen town/zone1/gate/zone2/boss/complete). `SessionAutoplay` trägt Phasen nur bei Erfolg ein
  und schreibt das JSON immer, auch bei Fehlschlag (`-autoplay-out`, `-autoplay-commit`).
  `FpsReporter.Windows` (read-only). `scripts/run-autoplay.sh` baut nach `builds/autoplay-player/`
  (neu: `-buildOut` in `BuildDiabloZoneBuild`, `UNITY_BUILD_EXTRA_ARGS` in `run-unity-build.sh`),
  startet den Lauf, Timeout 900 s = Exit 2, danach `fps-gate.py`.
- Belegt: EditMode 97/97 (8 neu `AutoplayResultTests`), PlayMode 71/71, `VerifyDiabloZone` OK,
  `KayKitAssets.Verify` OK, `check-meta` OK. `run-autoplay.sh` Exit 0, `result_phases=town,zone1,gate,zone2,boss,complete`,
  0 Exceptions, 75 s Spielzeit.
- Faced: Xvfb-Lauf rendert auf llvmpipe, FPS-Gate rot (5,6 fps, R1 aus dem Plan). Library-Kopie brach ab,
  weil der Warm-Editor gerade startete; Dienst kurz gestoppt, sauber kopiert, wieder gestartet.
- Learned: WSLg-Display + `GALLIUM_DRIVER=d3d12` gibt dem Linux-Player die echte GPU
  (`Renderer: D3D12 (NVIDIA GeForce RTX 5070 Ti)`), 47,5 fps schlechtestes Fenster. Kill-Zahl schwankt
  mit der Bildrate (Fen 12 vs. 9 Kills), Phasen bleiben stabil.
- Next: S2 nightly — Runner-Zugriff auf WSLg-Display prüfen.

## 2026-11-18 — cinderreach-loop-guard S2 nightly (Workflow + Host-Ablage)

- Done: `.github/workflows/autoplay-nightly.yml` (02:30 UTC + Dispatch, `unity-licensed`, 45 min),
  Ergebnis als Artefakt (14 Tage) und Kopie nach `~/unity-sandbox-autoplay/`, `docs/ci-runner.md`.
- Belegt: 3 Dispatch-Läufe grün (44219811822, 44220861693, 44222093327), je 131 s, 0 Exceptions,
  6/6 Phasen, FPS-Minimum 48,0–48,5 auf D3D12 aus dem Runner-Dienst.
- Learned: `workflow_dispatch` braucht den Workflow auf `main`, daher Workflow zuerst gelandet, Belege im
  zweiten Commit. Die Concurrency-Gruppe hält nur einen wartenden Lauf, Dispatches daher seriell.
- Next: S3 `G-DIABLO06` in dotfiles.

## 2026-11-18 — cinderreach-loop-guard S3 probe + Fehlertest, Goal 100 %

- Done: dotfiles `G-DIABLO06` (neuestes `autoplay-result.json` aus `~/unity-sandbox-autoplay`: ≤ 48 h,
  exit 0, 6 Phasen, 0 Exceptions, Dauer ≤ 1,5× Median der vorigen 5, FPS-Fenster ohne erstes ≥ 30).
- Belegt: Probe-Tests 20 passed, Live `G-DIABLO06 achieved`. Fehlertest mit unverwundbarem Boss auf
  Wegwerf-Branch → `critical 3`. Danach Lauf auf `main` → achieved.
- Faced: `agent-workspace.py finish` verweigert ungemergte Branches, rohes `git push --delete` blockt der
  Guard. Der Wegwerf-Branch `test/g06-invulnerable-boss` (lokal + remote) und sein Worktree bleiben
  deshalb stehen, bis der Eigner sie löscht. Der Plan-Satz „verwerfen über `finish --branch`“ trifft
  nicht zu.
- Learned: Ein Probe mit `baseline 1` meldet einen Einzelverstoß nur `at_risk`. Für G-DIABLO06 Baseline 0.

## 2026-11-18 — cinderreach-replayability S1 save-v2 (Migration + Stash-Daten)

- Done: `SaveGame` v2, `SaveMigrations.Migrate` (Outcome Current/Migrated/FutureVersion/Corrupt),
  `Stash` (40), `RunProfile` (Schwierigkeit, Freischaltung, Seed, Stash), Backup `*.v1.bak` beim Laden
  und vor jedem Überschreiben einer älteren Datei.
- Belegt: EditMode 106/106, PlayMode 71/71, beide Verifies, `check-meta`, Autoplay 6/6 Phasen.
- Faced: Neue Save-Felder wären bei jedem Autosave verloren gegangen, weil `CaptureState` nur Helden-Komponenten
  liest. Gelöst über `RunProfile`.
- Learned: Ein Save neuerer Version wanderte bisher nach `*.corrupt`. Jetzt bleibt er liegen, damit ein
  Downgrade keinen Spielstand zerstört.
- Next: S2 Affixe.

## 2026-11-18 — cinderreach-replayability S2 affix (14 Affixe, Tiers, Effekte, Vergleich)

- Done: Affix-Tabelle mit Tier 1/2, sieben neue Affixe samt Effekt-Hooks, Vergleichs-Tooltip im Bag.
- Belegt: EditMode 120/120, PlayMode 71/71, `VerifyDiabloZone`, `KayKitAssets.Verify`, `check-meta`,
  Autoplay exit 0 (6/6 Phasen, 49,0 fps Minimum).
- Faced: `CharacterSheet` ist reines C# ohne Helden-Bezug; der SkillRank-Bonus kommt daher als Parameter
  aus `DiabloSkills.EffectiveDamage`.
- Learned: Ein neuer Gleitkomma-Loot-Pfad hätte geseedete Tests verschoben; bei Magic Find 0 bleibt der
  alte Ganzzahl-Wurf.
- Next: S3 Layouts.

## 2026-11-18 — cinderreach-replayability S3 layouts (geseedete Ashen-Fen-Varianten)

- Done: `LayoutSeeder` (3×3×3×3 Varianten), `LayoutProps_0..2` im Builder, Verify-Zähler `layouts`,
  `-seed` im Autoplay und Seed-Rotation im Nachtlauf, `seed` im Ergebnis-JSON.
- Belegt: EditMode 125/125, PlayMode 71/71, `VerifyDiabloZone` (`layouts=3`), `KayKitAssets.Verify`,
  `check-meta`, Autoplay Seed 1 und 2 grün.
- Faced: Erster Erreichbarkeitslauf komplett rot: Spawn steht auf `ZStep_1` (0,3 m), die Prüfkapsel
  begann bei 0,15 m. Diagnose über den Warm-Editor (MCP `Unity_RunCommand`), Kapsel auf Stufenhöhe
  angehoben. Controller-Drift nach dem Szenen-Neubau revertiert.
- Learned: Gegnerpositionen sind im Netz nicht repliziert. Pro-Client-Seeds hätten die Co-op-Welt
  gespalten, daher Variante 0 für Peers.
- Next: S4 Kindled.

## 2026-11-18 — cinderreach-replayability S4 kindled (Schwierigkeitsstufe + Freischaltung)

- Done: `DifficultyProfile`, Freischaltung beim ersten Boss-Kill mit Sofort-Save, Overlay-Zeile,
  „New run (Normal/Kindled)“ in den Einstellungen, `-difficulty kindled`, Skalierung von HP, Schaden,
  Unique-Gewicht, Affix-Tier und Gold.
- Belegt: EditMode 131/131, PlayMode 74/74, Verifies, Autoplay Normal (Abbot 150 HP) und Kindled
  (Abbot 270 HP, Husk 9 HP) je exit 0.
- Faced: Ein Schwierigkeitswechsel mitten im Lauf hätte HP (beim Aufwachen skaliert) und Schaden (beim
  Treffer skaliert) gemischt; Wechsel daher nur mit neuem Lauf.
- Learned: Bone-Warden-Projektile treffen den Helden nicht (vorhandener Fehler, als Offener Punkt notiert).
- Next: S5 Truhe + Kindled-Nachtlauf.

## 2026-11-18 — cinderreach-replayability S5 stash + proof

- Done: `StashChest` + `DiabloStashUI` in Lantern Hold (Builder, Verify `stash=True`), `difficulty` im
  Autoplay-Ergebnis, Nachtlauf Normal + Kindled, Run-Log `docs/replayability-run-log.md`.
- Belegt: EditMode 137/137 (6 neu `StashChestTests`), PlayMode 74/74, Verifies, sechs Autoplay-Läufe
  (Seeds 0–2 × Normal/Kindled) exit 0, 0 Exceptions, Abbot 150 bzw. 270 HP.
- Faced: Drei Läufe je Stufe reichen nicht, um den Unique-Bonus zu zeigen (Kindled 2/32 vs. Normal 5/56).
  Beleg der Rate bleibt der Tabellen-Test.
- Next: dotfiles G-DIABLO06 je Schwierigkeit landen, Dispatch, Goal 100 %.

## 2026-11-18 — cinderreach-replayability 100 %, G-DIABLO03-Regression behoben

- Done: Live-Dispatch mit Normal + Kindled, `G-DIABLO06` je Schwierigkeit grün. Goal auf 100 %.
- Faced: `G-DIABLO03` fiel auf Testdaten vom 17.11. zurück. Ursache: Der Autoplay-Job (loop-guard S2)
  teilt den Runner-Workspace mit `Unity Tests`, und `actions/checkout` löschte `artifacts/*.xml`. Fix:
  `clean: false` im Autoplay-Checkout. Run-Log-Zeile für Seed 0 korrigiert (Layout 2/2/2/1, aus
  `System.Random(0)` nachgerechnet; vorher falsch eingetragen).
- Offen außerhalb dieses Goals: Bone-Warden-Projektile treffen den Helden nie (`RangedCaster.Fire` →
  `DiabloSkills.ApplyHit` trifft nur `EnemyHealth`). Kindled skaliert deshalb nur Husk-Nahkampf und Slam.

## 2026-11-18 — cinderreach-second-class S1 class-data (Katalog + Save v3)

- Done: Klassenkatalog, feste Skill-Enum-Werte, Save v3 `classId`, Migration v2 → v3.
- Belegt: EditMode 143/143, PlayMode 74/74, Verifies, Autoplay exit 0.
- Faced: Autoplay-Lauf mit Zufalls-Seed (Husk-Variante 1) meldete den Fen nach einem Kill als clear.
  Befund betrifft replayability S3, Untersuchung folgt als eigener Fix.
- Next: S2 Lanternbearer-Skills.

## 2026-11-18 — fix: Fen Gate fired mid-fight, autoplay hid it, builds lost scripts

- Symptom: Autoplay mit Zufalls-Seed meldete den Fen nach einem Kill als clear (`huskKills=2`).
- Ursache 1 (gemessen über eine Census-Zeile und Dwell-Logs): Der Held lief im Kampf über bzw. entlang des
  FenGate-Triggers (0, 1, -6) und wurde sofort ins Ossuary teleportiert. Danach lagen alle Fen-Husks außerhalb
  der 40-m-Suche, der Kampf galt als geräumt. Trifft echte Spieler genauso.
- Fix: `ZoneGate` feuert erst, wenn der Held 0,6 s still auf dem Tor steht. Bewegung über 0,3 m oder ein
  gelandeter Treffer startet die Zeit neu, und das Tor prüft selbst, ob der Held noch in seinen Bounds steht
  (Unstick-Versätze lösen kein `OnTriggerExit` aus). Entscheid: Tor-Reise braucht jetzt einen bewussten Halt.
- Ursache 2: `SessionAutoplay` hielt einen Zonenwechsel mitten im Kampf für „clear“. Jetzt FAIL mit Census,
  die Phase fehlt im Ergebnis und `G-DIABLO06` wird rot.
- Ursache 3: Ein Build in derselben Batch-Sitzung, die geänderte Skripte erst kompilierte, lieferte eine Szene
  mit 124 „missing script“-Behaviours (kein Held). `run-autoplay.sh` kompiliert jetzt vorher separat
  (`SandboxBuilder.CompileCheck`).
- Belegt: EditMode 149/149 (6 neu `ZoneGateDwellTests`), PlayMode 74/74, Verifies, Autoplay Seeds 1, 769869018,
  0, 2 je Fen clear mit 22 Kills, 6/6 Phasen, 0 missing scripts, FPS-Minimum 47,3–53,0.
- Offener Nebenbefund: Bone Wardens laufen ohne Leine aus dem Ossuary (x 200) bis in den Fen und zählen dort
  zu den Kills. Kandidat für `cinderreach-coop-session` S2 oder `cinderreach-act-one`.

## 2026-11-18 — cinderreach-second-class S2 skills (Lanternbearer)

- Done: vier Lanternbearer-Skills, `Wisp`, Durchdringen im `DiabloProjectile`, Klassen-Gate, HUD/Tasten aus
  dem Katalog, Chime/Hum/Shimmer-Klänge.
- Belegt: EditMode 157/157, PlayMode 78/78, Verifies, Autoplay Seed 0 (Fenwalker) grün.
- Faced: EditMode-Halo traf Husks einer offenen Zonenszene aus anderen Suites (eigene leere Szene im Test);
  `CleaveArc` trägt schon ein Licht (`AddComponent` lieferte null); Projektiltests müssen Spielzeit statt
  Frames warten (Test-Frames ≈ 1 ms).
- Next: S3 Ränge und Charakter-UI je Klasse.

## 2026-11-18 — cinderreach-second-class S3 sheet+ui

- Done: Lanternbearer-Ränge im Charakterbogen und Save, Klassen-Focus-Faktor, Rang-Zeilen aus dem Katalog.
- Belegt: EditMode 163/163, PlayMode 78/78, Verifies, Autoplay Seed 1 grün.
- Faced: Feldname `panel` kollidierte mit einer lokalen Variable in `BuildUI` (Kompilierfehler); Rechenfehler
  im eigenen Test (7 statt 8 Punkte).
- Next: S4 Klassenwahl und Items.

## 2026-11-18 — cinderreach-second-class S4 select+items

- Done: Klassen-Optik per Re-Skin, Co-op-Klassen-Sync, Start-Overlay zur Klassenwahl, klassengebundene Waffen,
  gewichtete Waffen-Drops, Laternen-Unique.
- Belegt: EditMode 168/168, PlayMode 80/80, Verify `classLook=True`, Autoplay Fenwalker grün, Lanternbearer
  ohne Exceptions.
- Faced: Overlay am Szenen-Helden wäre beim Netzwerk-Takeover verschwunden und hätte die Steuerung gesperrt;
  das Overlay ist ein eigenes Objekt und löst den Helden beim Klick auf.
- Next: S5 Beweis.

## 2026-11-18 — cinderreach-second-class S5 proof

- Done: Lanternbearer-Kampfprofil im Autoplay (Fenwalker-Regeln unverändert), `classId` im Ergebnis, dritter
  Nachtlauf als Lanternbearer, Shotcheck-Aufnahmen Klassenwahl + Wisps/Halo/Bolt, Mirror-Klassen-Log,
  `docs/second-class-run-log.md`.
- Belegt: EditMode 168/168, PlayMode 80/80, je drei Seeds Fenwalker 111,1 s und Lanternbearer 122,0 s im Mittel
  (+9,8 %), alle 6/6 Phasen, 0 Exceptions. Co-op-Smoke gemischt: Mirror zeigt Lanternbearer, Join/Disconnect
  sauber, 0 Exceptions. Windows-Stage + 26 Shots.
- Faced: relativer `-logFile`-Pfad wird vom Player nicht geschrieben; absolute Pfade nutzen.
- Next: dotfiles G-DIABLO06 je Klasse landen, Dispatch, Goal 100 %.

## 2026-11-18 — fix: Abbot summons magenta, cinderreach-second-class 100 %

- Done: `BossController.SpawnHusk` gibt den Beschwörungs-Kapseln ein URP-Lit-Material (aschgrau). Vorher Default-
  Material ohne URP-Shader, im Player magenta (sichtbar in `21_abbot_phase2.png` seit arpg-session-loop S5).
- Belegt: EditMode 168/168 (Material-Assert in `OssuaryConfigTests.Boss_PhaseTwoAtHalfHp`), PlayMode 80/80,
  Windows-Shotcheck `21_abbot_phase2.png` zeigt graue Kapseln.
- Faced: Ein `DestroyImmediate` der Beschwörungen im EditMode-Test ließ in derselben Testsitzung alle später
  geöffneten Szenen ohne `Sandbox.Runtime`-Komponenten laden (11 Szenen-Tests rot). Per Bisect belegt
  (ohne Teständerung 168/168, ohne Fix-Änderung im Skript ebenfalls rot nur mit der Teständerung), Mechanismus
  nicht gefunden. Der Test lässt die Beschwörungen wie früher stehen. Library-Kopien aus Hauptcheckout und
  Runner waren nicht die Ursache.
- second-class auf 100 %: alle sechs Kriterien belegt, `G-DIABLO06` live grün inkl. Normal/lanternbearer.

## 2026-11-18 — cinderreach-act-one S1 act-save

- Done: `ActProgress`, Save v4 + Migration v3 → v4, Kiln-Freischaltung beim Abbot-Kill mit Sofort-Save.
- Belegt: EditMode 174/174, PlayMode 80/80, Verifies, Autoplay grün mit `[Act] Cinder Kiln unlocked.`
- Next: S2 Cinder Kiln (Welt).

## 2026-11-18 — cinderreach-act-one S2 kiln-world

- Done: Cinder Kiln als Szenenbereich, gesperrtes Kiln-Tor mit Hinweis, Rücktor, Verify.
- Belegt: EditMode 177/177, PlayMode 80/80, `VerifyDiabloZone` inkl. Kiln, Autoplay 6/6.
- Faced: Tests referenzierten `KilnBuilder` (Editor-Assembly) → Konstanten nach `KilnLayout`; `TextMesh` im
  Szenenbau brach den URP-Lit-Vertrag → Text zur Laufzeit.
- Next: S3 Gegner, S4 Kilnmother.

## 2026-11-18 — cinderreach-act-one S3 kiln-enemies + S4 kilnmother

- Done: Cinder Thrall, Ash Lancer, Kiln-Aufstellung in drei Varianten, The Kilnmother mit Feuerwellen, brennendem
  Boden und Beschwörung, Akt-I-Abschluss mit Save und Overlay, `LootTier.ActBoss`.
- Belegt: EditMode 186/186, PlayMode 82/82, Verify inkl. Kiln-Inventar, Autoplay 6/6.
- Faced: `Skeleton_Brute.fbx` fehlt im Pack (Fallback); Lancer verletzten den „armed“-Vertrag; zwei eigene Tests
  griffen auf den `Player` einer offenen Zonenszene zu bzw. kippten an einer Float-Zeitgrenze.
- Next: S5 Wegpunkte, Autoplay-Route bis zur Kilnmother, Shotcheck, Probe.

