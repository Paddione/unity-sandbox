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

## 2026-11-18 — cinderreach-act-one S5 waypoints+proof

- Done: Wegpunkt-Schreine + Reisefenster, Autoplay bis zur Kilnmother, acht Pflichtphasen, Kiln-Shots,
  Run-Log, Perf-Fix (`HeroLocator`, Leichen-Deckel).
- Belegt: EditMode 191/191, PlayMode 84/84, Verify, neun Läufe 8/8, FPS-Minimum auf finalem Code 36,5.
- Faced: FPS-Gate rot (26,7) nach dem Kiln: ~50 `GameObject.Find("Player")` pro Frame und 26 animierte
  Leichen im Massentod. Wegpunkt-Tests: `OnEnable` läuft im EditMode für `AddComponent` nicht (Szenensuche
  statt Liste). `Kill_LootLandsInBag_EquipFromPanel` war seit second-class S4 flaky (30 % fremde Waffen),
  jetzt beide Ausgänge gepinnt. Shotcheck starb im Kiln ohne Lebenssicherung.
- Learned: Ankunft im Kiln mitten zwischen zwei Packs ist ein Balance-Befund für den Playtest.

## 2026-11-18 — cinderreach-act-one 100 %

- Live-Dispatch 44288445907 grün mit drei Läufen durch den Kiln, `G-DIABLO01`–`06` achieved.
- Faced: `G-DIABLO06` meldete nach der Routenerweiterung „Dauer > 1,5× Median“, weil der Median aus Läufen der
  alten, kürzeren Route stammte. dotfiles `e6463e03`: Basis nur aus vollständigen Läufen der aktuellen Route.
- Offene Befunde für Folge-Goals (nicht act-one): Bone Wardens ohne Leine (laufen aus dem Ossuary in den Fen),
  Bone-Warden-Projektile treffen den Helden nie, Kiln-Ankunft zwischen zwei Packs (Playtest).


## 2026-11-18 — fix: Warden-Leine, feindliche Projektile, Kiln-Ankunft, ungebundene Skripte

- Done: `RangedCaster` mit Aggro-Radius 14 m und Leine 10 m um den Spawnpunkt. Verlässt der Held den Radius
  oder wirft ein Rückstoß den Warden über 12 m hinaus, geht er heim und ignoriert den Helden bis dahin. Am
  Leinenrand bleibt er stehen und schießt weiter. `DiabloProjectile` hat einen feindlichen Modus
  (`hostile`): sucht Helden mit `PlayerHealth` statt `Husk`-Ziele, fliegt durch Husks, Schaden nach
  `DifficultyProfile.ScaleDamage`. Bone Wardens und der Fächer des Hollow Abbot schießen feindlich. Vorher
  trafen beide nie den Helden, ein Warden-Feuerball konnte nur andere Wardens treffen.
  `KilnLayout.KilnArrival` von (0, 2) auf (13, 13): mindestens 9 m zu jedem Thrall- und Elite-Anker in allen
  drei Varianten (vorher 4–5 m zwischen den Packs bei 60° und 120°). Projektil-`Destroy` läuft im EditMode
  über `DestroyImmediate`.
- Ungeklärter Test-Effekt aus second-class aufgeklärt: In einem Worktree mit aus dem Hauptcheckout kopierter
  `Library` kompiliert die erste Sitzung manchmal alle Assemblies neu (Refresh 63–67 s statt 16 s). Danach
  haben die `MonoScript`-Artefakte von `Assets/Scripts` keine Klassenbindung (`GetClass()` = null), Szenen
  und Prefabs laden ihre Komponenten als null, ohne „missing script“-Warnung, und alle Folgesitzungen erben
  den Cache: 14 Szenen-Tests rot, `VerifyDiabloZone` rot, Autoplay-Player ohne Helden. Belegt mit
  Diagnose-Methode (Held mit 11 Null-Komponenten, `DiabloClickController.cs` ohne Klasse). Der
  `DestroyImmediate`-Test war im damaligen Bisect nicht die Ursache. Abhilfe: `Editor/LibraryRepair.cs`
  erkennt ungebundene Behaviour-Skripte und reimportiert `Assets/Scripts`, wirksam ab der nächsten Sitzung.
  `scripts/unity-warmup.sh` wiederholt `SandboxBuilder.CompileCheck`, bis eine Sitzung „scripts bound“
  meldet, und läuft vor jedem Testlauf (`run-unity-tests.sh`) und jedem Autoplay-Build.
- Faced: Experimente B–E4 (Compile allein, neue Skripte ohne `.meta`, frische `Library`, fehlende
  `UserSettings`, Prefab-Reimport) widerlegten je eine Hypothese, bevor die Diagnose die Bindung zeigte.
  Auch der Reimport in derselben Sitzung reicht nicht, erst die Folgesitzung ist gebunden.
- Belegt: Endprobe aus vergiftetem Ausgangszustand: Warm-up Sitzung 1 meldet 51 ungebundene Skripte, Sitzung 2
  gebunden, EditMode 201/201, PlayMode 84/84, `VerifyDiabloZone`, `check-meta`, `KayKitAssets.Verify`. Autoplay 8/8
  Phasen, 0 Exceptions, schlechtestes Fenster 39,3 fps. Drei spätere Läufe auf demselben Spielcode lagen bei
  25,0/29,8/25,6 fps, schon in Lantern Hold vor jedem Kampf, während `llama-server` (:1919) 15,6 von 16,3 GB VRAM
  hielt und die GPU zu 89–91 % auslastete. Eigner hat den 39,3-Lauf als Beleg akzeptiert.
- Nach Rebase auf `58a6977` (maschinenweite Editor-Sperre): `run-unity-tests.sh` hält die Sperre und reicht sie per
  `UNITY_EDITOR_LOCK_HELD=1` an den Warm-up weiter, sonst wartet `run-unity-build.sh` im Kind auf den eigenen Elternprozess.
  Lauf danach: EditMode 201/201, PlayMode 83/84 (`DiabloP2LoopTests.Strike_HitsHuskInFacingArc`, Nahkampf, in zwei
  Läufen davor auf identischem Spielcode grün). Wiederholung nicht abgewartet: neun parallele Sessions in der Sperr-Warteschlange.

## 2026-11-18 — cinderreach-coop-session S1 join

- Done: Startparameter `-host [-port N]`, `-join ip[:port]` (`JoinArgs`, P2-Flags und Klon-Erkennung bleiben),
  `MultiplayerAutoStarter.Restart` für den Wechsel zur Laufzeit, `JoinPanel` (F7 / Klassenwahl, HD-Scaler),
  `SaveService -save-file`, `AutoplayResult.role` mit Client-Pflichtphasen `town,zone1`, `CoopAutoplay` (Client),
  Host-Autoplay wartet bis 90 s auf den Client, `scripts/run-coop-pair.sh` (zwei Player auf WSLg-GPU).
- Belegt: EditMode 210/210, PlayMode 84/84, Verify, Paarlauf Exit 0 (Client 2/2, Host 8/8, je 0 Exceptions),
  Solo-Autoplay `-seed 0` 8/8 bei 36,8 fps.
- Faced: erster Solo-Lauf ohne `-seed` las Seed 1112106611 aus einem vorhandenen Save; in dieser Fen-Variante blieb
  der Held nach 11 Kills ohne Treffer bis Budgetende hängen (Befund in PROGRESS, nicht S1).
- Next: S2 host-autoritative Gegner.

## 2026-11-18 — faire Kämpfe Akt I: Plaza, Wind-up-Orb, PlayMode-Belege

- Done: Auf `7f455b0` aufgesetzt (Leine, hostile Projektile, NO-Ankunft bleiben). Dazu: Ankunfts-Plaza
  (Tor- und Wegpunkt-Scheibe je r=5, gelatcht, Tor/Wegpunkt/Reset re-armen), sichtbarer Warden-Wind-up
  (wachsende Kugel + Licht, `Telegraphing`-Seam), Plaza-Guards in `EnemyAttack`/`AshLancer`/`Kilnmother`,
  PlayMode-Suiten (`WardenRetreatTests`, `WardenShotTests`, `KilnArrivalTests` alle 3 Layouts) plus
  `KilnPlazaTests` (EditMode) und `docs/fair-fights-playtest.md` (Fenwalker/Lanternbearer, Windows, offen).
- Belegt: EditMode 206/206, PlayMode 91/91, Akt-I-Autoplay 9/9 Phasen, 49 Husks, Kilnmother tot,
  0 Exceptions, FPS-Tor 33,6 (Schwelle 30).
- Learned: Nahkampf- und Caster-Komponente auf einem Warden-Körper pattieren sich an der inneren Bandkante
  (6 m − 0,025 m pro Frame, Wind-up startet nie); der Retreat-Test löst den Wind-up deshalb deterministisch
  an der äußeren Bandkante (8 m) aus. Reine Sim in Python hat den Patt vor dem Re-Run bewiesen.
- Faced: Erster eigener Szenen-Rebuild schrieb 32 eingebettete MonoScripts mit nackten `fileID`-Refs
  (14 rote EditMode-Tests); YAML-Patch nur des Gate-Ziels verworfen, Szene aus Main übernommen, Library per
  `unity-warmup.sh` geheilt (Sitzung 1 gebunden).
- Offen: zwei manuelle Windows-Läufe laut Protokoll (Eigner).

## 2026-11-18 — fix: Editor-Sperre überspringt hängende Läufe

- Anlass: Ein EditMode-Lauf (`skill-variants-s1`) fror um 14:35 im Startup ein (0 % CPU, kein Log-Byte in 16 min)
  und überlebte seine Session. Er hielt die maschinenweite Editor-Sperre, rund zehn Läufe standen dahinter.
- Done: `scripts/lib/unity-editor-lock.sh`, von `run-unity-build.sh` und `run-unity-tests.sh` genutzt. Der Halter
  trägt seinen Unity-Prozess und sein Log in `<lock>.owner` ein. Ein Lauf, dessen Log `UNITY_STALL_S` (600 s)
  still ist, endet selbst mit Exit 2 und gibt die Sperre sofort frei (Tests: ohne die nächste Plattform zu
  starten). Wartende pollen alle 30 s und beenden einen hängenden, auch verwaisten Halter samt Prozessbaum. Eine
  wiederverwendete PID wird nicht getroffen (die Kommandozeile muss den Log-Pfad tragen). Hängt ein Build erst
  nach dem Erfolgsmarker (bekannter Shutdown-Hänger), gilt er als OK.
- Belegt: `scripts/test-unity-editor-lock.sh` 6/6 mit Fake-Editor (34 s). Echter Lauf durch die neuen Wrapper:
  Warm-up, EditMode 201/201, PlayMode 84/84.
- Faced: `pkill -f` auf ein Muster aus der eigenen Kommandozeile beendete die eigene Shell. Der Test räumt nur
  per PID-Baum auf.

## 2026-11-18 — coop-session S2: host-autoritative Gegner + HitValidator

- Done: `Net/HitValidator.cs` (neu, pure C# + Unity-Math: Reichweite + 0,5 m, Cooldown, Ziel lebt,
  Schaden 1..50, `NearestLivingPlayer`), `EnemyHealth` → `NetworkBehaviour` mit `NetHP`
  (Everyone/Server; Server schreibt, Clients spiegeln per OnValueChanged, ungespawnt lokal),
  `EnemyAttack.AcquireHero` (public, nächste lebende Heldin), `BossController` → `NetworkBehaviour`
  mit `Phase` (1/2) + `PhaseTwo`/`SlamTelegraph`-ClientRpc + Summons per `NetworkObject.Spawn`
  (Server; solo lokal), `RangedCaster` → `NetworkBehaviour` mit `TelegraphClientRpc` + nächste
  lebende Heldin, `DiabloNetworkPlayer`-Validierung (Melee 4 m/0,5 s, Ranged 24 m/0,3 s, je Sender),
  `DiabloSkills.ApplyHit`-Kommentar (HitValidator), PlayMode `HostEnemyTests` (5 Tests) +
  `Sandbox.Tests.PlayMode`-Asmdef um `Unity.Netcode.Runtime` ergänzt.
- Belegt: EditMode 257/257, PlayMode 113/113, `VerifyDiabloZone` OK (husks=48, armed),
  `KayKitAssets.Verify` grün, `check-meta` OK; Paarlauf Exit 0 (Host 8/8, 0 Exc, 181 s; Client
  town,zone1, 0 Exc, hits=232, 108 s); Solo-Autoplay Exit 0, 0 Exc, FPS-Pass 34,6 (≥30).
- Learned: PlayMode-Tests brauchen `Unity.Netcode.Runtime` in der Asmdef, sobald Produktionscode
  `NetworkBehaviour` als Basis nutzt (CS0012); EditMode hatte die Referenz bereits.
- Faced: `DiabloNetworkPlayer.cs` steht nicht in der S2-target_files-Liste (dort nur `DiabloSkills.cs`),
  trägt aber beide Treffer-ServerRpcs — Validierung landet dort, `DiabloSkills` delegiert.
- Next: S3 world (Gate für alle, Client-Leichen).

## 2026-11-18 — coop-session S3: Gate für alle, Client-Leichen

- Done: `Net/ZoneTravelRelay.cs` (neu, statisch): `GroupReady` (alle Lebenden oder 3 s Host-Hold,
  nie locked), `SpawnOffset` (Index 0 bleibt, Follower fächern ≤ 3 m), Fade-Overlay (0,3 s, non-blocking),
  `ApplyLocalTravel` (eigenen Hero per Offset teleportieren), `ApplyRemoteCorpse` (visual aus eigener
  Husk-Kopie). Broadcasts auf dem Host-Player-Relay (`BroadcastTravel/BroadcastCorpse`, ClientRpcs,
  Muster ShareKillXP): dynamisch gespawnte Relay-Objekte replizieren NICHT (Client-Hash-0-Fehler),
  deshalb kein separates Relay-Objekt. `ZoneGate`: `HasClientMirrors` (Spiegel zählen statt
  `IsSolo` — `ConnectedClientsIds` enthält den Server, solo wäre sonst mit Client true),
  `CoopTick` (nur Host mit Clients; Clients dwellen lokal nicht), `HostHero` (Owned-Player-Objekt —
  zwei Objekte heißen „Player“), `TeleportViaHost` (Harness: Broadcast + async, solo: lokal),
  `ApplyTravel` (gemeinsamer Reisekörper). `DiabloCorpse.SpawnFrom(husk, remote)` + Death-Broadcast
  vor Despawn. `SessionAutoplay`-Gate wartet 15 s auf Broadcast-Reise; `CoopAutoplay`-Client bleibt
  bis Zone 2 (60 s Grace, Log `phase zone2`).
- Belegt: EditMode 264/265 (einziger Fail `LayoutSeedTests`, vorbestehend auf Main),
  PlayMode 127/127 (neu `CoopGateTests` 8 Tests), `VerifyDiabloZone` OK, `KayKitAssets.Verify` grün,
  `check-meta` OK; Paarlauf Exit 0: Host `host broadcasts FenGate → zone 2`, Host-Offset (0,0,0),
  Client-Offset (1,5,0,0), Client `phase zone2: pulled to the Ossuary`, 12 Remote-Corpses, je 0 Exc;
  Solo-Autoplay Exit 0, FPS-Pass 31,2 (≥ 30).
- Learned: Autoplay umgeht Dwell per direktem `zg.Teleport` (SessionAutoplay.cs:309) — organischer
  Dwell/Hold greift bei kämpfenden Heroes nie (Hold resettet bei Bewegung), deshalb braucht der
  Harness-Pfad einen eigenen Broadcast-Einstieg.
- Faced: `DiabloNetworkPlayer.cs`/`SessionAutoplay.cs`/`CoopAutoplay.cs` stehen nicht in der
  S3-target_files-Liste, tragen aber die Treffer-/Harness-Seite (ServerRpc-Hub, Gate-Fallback,
  Client-Grace) — wie S2 dokumentiert.
- Next: S4 economy (Gold/Pickup/Vendor validiert).

## 2026-11-18 — coop-session S4: validierte Wirtschaft

- Done: `Net/TradeValidator.cs` (neu, pure C#, keine Szene): `CanBuy` (Gold/Bag/4 m),
  `CanPickup` (2,5 m + Once), `CanSell` (4 m + Deckel 75). `Tests/EditMode/TradeValidatorTests.cs`
  (8 Tests: Fälschungen abgelehnt, Seed-Determinismus Host/Client).
- Gold: `GoldDrop` spawnt auf dem Host mit `NetworkObject` + `LootNetRelay` (wie S1-Loot);
  Clients fragen per `RequestGoldServerRpc` an (Range + Once), Gutschrift per `GrantGoldClientRpc`
  nur an den Owner (GoldFind lokal) — kein Predict, keine Doppelzahlung. Host/Solo direkt
  (`CollectLocal`, Despawn). Item-Loot und Truhen prüfen im ServerRpc zusätzlich die Reichweite
  (`SenderHero`-Lookup, planar) — Teleport-Pickup wird abgewiesen.
- Vendor: `Vendor.StockSeed`/`StockFor` (Host id 0 → Zone-Seed; Client → abgeleitet; UI rollt
  beim Spawn nach, `RestockForOwner`). Kauf/Verkauf laufen im Coop über das Player-Relay
  (`RequestVendorBuy/Sell`, `TryServerBuy/TryServerSell`, `Approve*ClientRpc`, Restlisten pro
  Sender, `ResetCoopEconomy`); `TryBuy`/`TrySell` setzen Gold/Bag lokal durch. UI routet im
  Coop-Fall mit Gold/Bag-Precheck (keine Divergenz), sonst direkt wie bisher. Trank bleibt lokal
  (Fixpreis, unendlicher Bestand, kein Shared State).
- Belegt: EditMode 297/297, PlayMode 154/154, `VerifyDiabloZone` OK, `KayKitAssets.Verify` grün,
  `check-meta` OK; Paarlauf Exit 0 (Host 8/8, Client town,zone1 + Zone-2-Pull, 179 verworfene
  Client-Treffer als Validator-Wirkungsnachweis, je 0 Exc); Solo-Autoplay Exit 0, FPS-Pass 34,2.
- Learned: Worktree ohne Commits wurde von fremder Session gereapt — nach `create` sofort claimen
  und zügig committen. `LayoutSeedTests`-Rot war vorbestehend auf Main (inzwischen dort gefixt).
- Faced: `DiabloNetworkPlayer.cs`/`DiabloVendorUI.cs`/`SessionAutoplay`-Umfeld stehen nicht in der
  S4-target_files-Liste, tragen aber Relay-Hub, UI-Routing und Harness (wie S2/S3 dokumentiert).
  Verkauf serverseitig nur Range + Preisdeckel prüfbar (kein Shadow-Inventar — bewusst, kein
  Cheat-Schutz über Validierung hinaus).
- Next: S5 save (Client-Save, Level/Gear-Meldung, Windows-Client, coop-run-log).

## 2026-11-18 — coop-session S5: Client-Save und Join-Handshake

- Done: Join-Handshake auf `DiabloNetworkPlayer` (S2/S3-Hub): `JoinReport`
  (JSON über `SubmitJoinStatsServerRpc`), `SanitizeJoinReport` (Level 1..30,
  Punkte ≥ 0, nur Katalog-Gear/Unique-IDs), `ApplyJoinReport` (Spiegel bekommt
  Level/Punkte/Gear — Max-HP und Schaden folgen nativ). Owner lädt beim Spawn
  sein Save und meldet danach. `BossController.OnBossDeath` ruft
  `BroadcastBossKill` (Clients speichern wie solo); `ZoneGate.ApplyTravel`
  speichert zusätzlich den eigenen Hero (`IsOwner`), Solo-Pfad unverändert.
- Belegt: EditMode 305/305, PlayMode 173/173 (neu `CoopSaveJoinTests` 4 Tests:
  Roundtrip Level/Gear/Zone, JSON-Roundtrip, Clamp-Fakes (99→30, negativ→0,
  Fake-Gear→null), Spiegel-Power), `VerifyDiabloZone` OK, `KayKitAssets.Verify`
  grün, `check-meta` OK; Paarlauf Exit 0, Join-Reports beidseitig übernommen
  (Host L5 + Client L3 auf Spiegeln), Client Zone 2 per Broadcast, Saves
  Host L6/380 G + Client L3/Zone 2, je 0 Exc; Solo-Autoplay Exit 0, FPS 36,4.
- `docs/coop-run-log.md` (neu): Volllauf-Nachweis beider Seiten + Windows-Vorlage.
  Windows-Build Succeeded (120 MB, Builder-Ziel im Hauptcheckout), Stage OK
  (122 MB nach `Games\cinderreach`). Kein Start vom Headless aus (Desktop des
  Eigners) — Live-Join + Shotcheck-PNG als Eigner-Lauf dokumentiert.
- Faced: `BossController.cs` steht nicht in der S5-target_files-Liste (3-Zeilen-Hook,
  wie S2–S4 dokumentiert). `RunProfile`/`DifficultyProfile` liegen unter
  `Save/` bzw. `Difficulty/`, nicht wie vermutet direkt unter `Scripts/`.
- Next: S6 relay (Join-Code, Multiplayer-Services-Migration, Eigner-Läufe).

## 2026-11-18 — coop-session S6: Relay-Join-Code (FINAL)

- Done: Paketmigration per Client-API (kein Hand-Edit): `relay`/`lobby` raus,
  `com.unity.services.multiplayer@2.5.0` rein (Transport 2.4.0 → 2.7.4 in einer
  UPM-Resolution). `Net/RelayJoin.cs` (neu): UGS-Init + anonymer Login, Host
  erstellt die Session mit Relay (`CreateSessionAsync` + `WithRelayNetwork`,
  max 4), Join-Code im Log (`RELAY_JOIN_CODE=`) und im JoinPanel; Client joint
  per `JoinSessionByCodeAsync`. Das Sessions SDK owned Transport + NGO-Start;
  Fallback direktes UTP mit Warnung (nie still). `JoinArgs -join-code` /
  `-host-relay`, JoinPanel mit Code-Feld + HostRelay/JoinByCode + Relay-Status,
  `run-coop-pair.sh` mit `COOP_RELAY=1`.
- Belegt: EditMode 305/305, PlayMode 180/180 (neu `RelayJoinTests` 7 Tests:
  Code-Normierung, Flag-Parsing, Offline-Fehlerpfade), `VerifyDiabloZone` OK,
  `check-meta` OK; Linux-Linux-Relay-Paar Exit 0/0 (Code MHP868, Client
  Zone-2-Pull, je 0 Exc); WSL-Host + Windows-Client direkt Exit 0/0 (Client
  Zone-2-Pull, je 0 Exc); WSL-Host + Windows-Client per Code Exit 0/0
  (Code 6N9BHD, Client Zone-2-Pull, je 0 Exc); Shotcheck-PNGs vom Windows-Build
  (`30_session_complete.png` u.a.); Solo-Autoplay Exit 0, FPS-Pass;
  Direkt-Paar nach Transport-Upgrade weiter Exit 0/0.
- Debug-Funde (alle live belegt, keine Theorie): Sessions-SDK braucht den
  Unity-Main-Thread (kein `ConfigureAwait` — sonst NRE im Transport-Allocator
  bei `StartHost`); gleiche Anon-Identität kann sich nicht selbst joinen (pro
  Prozess frisch via `SignOut(clearCredentials:true)`); volle Save-Bags brechen
  den deterministischen Trank-Kauf (Harness archiviert Stale-Saves seit S6).
- Faced: `MultiplayerAutoStarter.cs`/`run-coop-pair.sh`/`Sandbox.Runtime.asmdef`
  (Assembly-Referenzen) stehen nicht in der S6-target_files-Liste, tragen aber
  Relay-Boot, Harness und Kompilierbarkeit (wie S2–S5 dokumentiert).
  `ugs-relay-probe.sh` blieb unverändert (Vorab-Check, Exit 0).
- Next: Goal-Audit und 100 %.

## 2026-11-19 — cinderreach-party-roles S6 (Gruppen-Beweis, Goal 100 %)

- Done: S6 gelandet (Autoplay-Profil Rogue, Ergebnis-Felder `partyRoles`/`huskKills`/`healsCast`/`hpRestored`,
  4 Party-Shots, Q11 + Entscheid + Run-Log). Belege: je Klasse Exit 0, 8/8 Phasen, 0 Exceptions
  (163,0 s / 132,6 s / 189,7 s, Band ±25 %); Koop-Paar Tank+Rogue Exit 0/0 mit Rollenersatz und Join-Gate;
  EditMode 346/346 + PlayMode 228/228; 34 Shotcheck-PNGs.
- Faced: Stale-Default-Save überschrieb `-class` (alle Autoplay-Läufe spielten Fenwalker, Town-Phase
  scheiterte am vollen Beutel) — isolierte `-save-file` pro Klasse als Fix, seitdem Pflicht für
  Klassen-Beweise. Client-Boot spannte Phantom-KI vor dem Join (IsSolo lag) — `-join`-Erkennung als Fix,
  per Re-Smoke belegt (Client-Log ohne `[Party]`).
- Learned: God-Held hält alle Aggro (Slam +20, Quick-Shot-DPS), Begleiter bleiben unverletzt, Heiler
  bleibt stumm — Heals 0 ist erklärter Befund, kein Defekt (Mechanik über PlayMode-Kampf belegt).
- Next: Non-God-Ausdauerlauf (echte Balance), 4. Spieler-Join-Gate live prüfen, Rogue-Look (Knight-Platzhalter).

## 2026-11-19 — Watchdog-Kalibrierung: CPU-aware Stall-Verdict

- Anlass: Unity-Tests-Run auf `main` starb mit Exit 2 (Log-Stille ≥ 600 s) auf einem 1-Zeilen-Plan-Commit;
  Re-Run grün. Datengrundlage: EditMode 344 Tests ≈ 13 s, PlayMode 220 Tests ≈ 94 s Testzeit (Run 01:18) —
  gesunde Phasen liegen weit unter 600 s, aber Maschinen-Contention streut die Laufzeit (5–21 min),
  und Stille allein unterscheidet langsam (CPU-busy) nicht von hängt (CPU-idle).
- Done: `scripts/lib/unity-editor-lock.sh` — Stall-Verdict per `unity_tree_cpu_s` (kumulative Prozessbaum-CPU
  via /proc, comm-feldsicher): still + idle → Kill (Exit 2, unverändert), still + busy (≥
  `UNITY_STALL_CPU_MIN_S` in `UNITY_STALL_CPU_PROBE_S`) → frisches Stall-Fenster, begrenzt durch den harten
  Timeout. Gilt für Holder (`unity_run_watched`) und Waiter (`unity_lock_reap_stale`, mit Cmdline-Recheck
  nach dem Probe-Sleep). Jede Verdict-Zeile trägt ihre CPU-Evidenz (`+Ns/Ms`). Kein /proc → Legacy-Verhalten.
  Header von `run-unity-build.sh`/`run-unity-tests.sh` nachgezogen.
- Belegt: `scripts/test-unity-editor-lock.sh` 8/8 (2 neue Cases: Waiter schont CPU-busy-Halter, busy Run
  erreicht Timeout statt STALL), `python3 -m pytest scripts -q` 5 passed, shellcheck ohne neue Findings.
- Faced: `wooden-crate.glb` steht im Index ungefiltert (27020 B), `.gitattributes` verlangt lfs — jeder frische
  Checkout zeigt es dirty. Pre-existing, bewusst nicht angefasst (explizite Pathspecs beim Commit).
- Next: Post-Merge-CI-Run auf `main` als Live-Beleg beobachten.

## 2026-11-19 — LFS-Heilung: wooden-crate.glb als Pointer

- Anlass: `sandbox/Assets/ThirdParty/Meshy/crate/wooden-crate.glb` lag roh (27020 B) im Index, `.gitattributes`
  verlangt `*.glb filter=lfs` — jeder frische Checkout zeigte den Worktree dirty (reine Clean-Filter-Artefakt,
  Inhalt bit-identisch). Scan: einziger Violator von 509 Pattern-Dateien (508 sauber per `git lfs ls-files`).
- Done: `git add` (Clean-Filter → Pointer `e53ae62d`, 130 B), Objekt liegt im gemeinsamen LFS-Store,
  `git lfs push` vor dem Land, damit CI-Checkout (`lfs: true`) und frische Klone materialisieren.
- Belegt: Pointer im Index, `git lfs status` Git→LFS-Mapping ok, Post-Merge-CI-Run als Roundtrip-Beleg.

## 2026-11-19 — Party-Team-Härtung: Spawn/Despawn-Beweise + ausrüstbare Begleiter auf Heldenniveau

- Anlass: Eigner-Nachfrage auf party-roles (100 %) — Spawn/Despawn der KI-Begleiter zusammen mit
  Teamplay testen; Team ausrüstbar machen und auf Protagonisten-Level halten. Goal
  `.agents/goals/cinderreach-party-team/` (Q12), Plan `.agents/plans/cinderreach-party-team/tasks.md`.
- Done: `PartyRoster.ApplyRemoteRoles` (Join/Leave-Seam, `-1` bei Join-Gate; `ServerReconcile`
  delegiert dorthin — ein Pfad für Host und Tests); Spawn-Reihenfolge `Equipment` vor
  `PlayerHealth` + sofortiger Level-Mirror; `SyncCompanionLevels` im `Update` (Key-gecacht);
  `PartyCompanion.Gear`/`Equip` (Klassenbindung via `Equipment.CanEquip`); `DiabloProgression`
  `.MirrorHeroProgress` (Level/XP/Attribute/Ränge, quiet) + Companion-Guards (kein Ambience-Target,
  keine XP-Leiste, kein Save, keine Fanfare); `PlayerHealth.Refresh` pflegt `Equipment` nach.
- Belegt: `PartyTeamLifecycleTests` 5/5 + `PartyGearLevelTests` 4/4 neu (je `.meta` mit
  Unity-GUID, 32 Hex ohne Bindestriche — Bindestrich-Form wird vom Importer ignoriert);
  EditMode 358/358 + PlayMode 262/262; `scripts/check-meta.py` OK;
  `python3 ~/scripts/agent-workspace.py check` still.
- Falle: Erster gefilterter Lauf meldete 0/0 — nicht der Filter (`;`-Liste funktioniert), sondern die
  ungültigen `.meta`-GUIDs (Assets wurden ignoriert). Log-Signal: „does not have a valid GUID".
- Grenze: Build-Spiegel statt eigener Begleiter-Skillung; keine Begleiter-UI (Equip-API belegt,
  UI Follow-up). Kein Save-/Netz-Format angerührt.

## 2026-11-19 — Party-Team-Builds: Server nach gpu-metal, Windows-Client, Coop- + E2E-Beleg

- Anlass: Eigner-„ja" auf cinderreach-party-team (`a4e8aaf`) — Server-Build, Windows-Client,
  Coop-Smoke und E2E-Join gegen gpu-metal auf dieser Maschine.
- Done: `SandboxBuilder.BuildDiabloZoneServer` Succeeded (49,1 s, 92 MB), Smoke lokal
  `[Net] Server started`; neuer Code im Build belegt (`MirrorHeroProgress` in
  `Sandbox.Runtime.dll`); `rsync` nach `gpu-metal:~/diablo-server/` + `BUILD_INFO.json`
  (`a4e8aaf`, `diablo-zone`); `diablo-server.service` restart, active, `[Net] Server started`.
  `SandboxBuilder.BuildDiabloZoneWindows` Succeeded (40,7 s, 139 MB), `stage-windows.sh` OK
  (`Games\cinderreach`, neuer Code in staged DLL belegt, Tests korrekterweise absent).
- Belegt: Coop-Paar Host-Tank + Client-Rogue Exit 0/0 — Host 8/8 Phasen, 49 Kills, 1 Heal
  (`exceptions=0`); Host-Log `[Party] Lanternbearer/Rogue joins`, `Rogue steps aside`,
  `Rogue joins` (Leave-Refill), Client 0 `[Party]`-Zeilen. E2E: frischer Linux-Player
  `-join 100.74.227.118:7778` → `[Net] Client connected: 1 (total=1)` auf beiden Seiten.
- Grenze: 2 `ArgumentNullException` im Server-Log sind Vorbestand (`EnemyHealth.DropOrb`
  baut Material mit gestripptem Shader — Server ohne Rendering), unberührt von diesem Commit.

## 2026-11-21 — Menü-/Art-Branch gelandet, Layout-Fix und Spielfluss-Iteration 1

- Rebase: `feature/cinderreach-menu-art-polish` (9-Slice-UI, Blender-Sprites, Startbildschirm,
  Pause-Menü) lag seit 2026-11-18 ungelandet und kollidierte in 14 Dateien mit itemization S5,
  party-roles/-team und skill-variants. Aufgelöst mit beiden Seiten: Vendor behält Koop-Handel
  über den Host, Notice, Klick-außerhalb-Schließen und den „Close (saves)“-Knopf im neuen Layout;
  Inventar behält Equip/Discard-Aktionen und `AreaLine`; Charakterblatt behält Skill-Varianten.
- Layout-Ursache: Fast alle Panels laufen mit `childControlHeight = false`; `DiabloUIFactory.SetLayout`
  setzte nur `LayoutElement`, die Gruppen positionierten deshalb jedes Kind mit 100 px Default-Höhe
  (Divider als Goldklecks, Inhalte unter dem Panelrand). `SetLayout` spiegelt die Wunschgröße jetzt
  auf die `RectTransform`. Item-Karten schrumpfen per `FitToBox` in ihre Box, das Charakterblatt
  wächst per `ContentSizeFitter`.
- Spielfluss: genau ein Startbildschirm (`MainMenu.ClassSelectOwnsStart`), Continue nennt Klasse,
  Level, Gold und Stufe per nebenwirkungsfreiem `SaveService.TryPeek`; Solo-Pause friert die Zeit
  ein (Hit-Stop-sicher); Todesschleier „YOU HAVE FALLEN“ statt Textzeile; „Press On“ nach dem Abbot;
  Skill-Varianten haben erstmals einen Bedienpfad (Umschalter im Charakterblatt, solo im Hold).
- Art: Rogue-Portrait (Spielmodell `Rogue.fbx`) und fünf fehlende Skill-Icons (Seismic Slam,
  Quick Shot, Blade Flurry, Mending Light, Renewing Circle) per `build_menu_pack.py`; Kartentexte
  aus Rolle/Starter/echter Skillzahl; Rogue-Akzentfarbe; HUD-Bronzeplatte unter Skill- und XP-Leiste;
  Item-Namen lesbar (`LootDrop.PrettyName`: „Bulwark Carrier“, „Cloth Tunic“), Varianten englisch.
- Perf-Regression gefunden (A/B, gleiche Maschine, Autoplay `-seed 0` 1280×720): `main` `0619770`
  Stadt ~110–120 fps, worst 34,6; Branch worst 20,5 (Gate rot), Stadt ~42. Ursache:
  `DiabloSettings.BootApply` setzte beim Start immer `Screen.SetResolution(1920×1080, Vollbild)`
  und VSync und überstimmte damit `-screen-*` und Automation. Jetzt: Boot fasst die Anzeige nur
  ohne Automation/`-screen-*` an und die Auflösung nur, wenn der Spieler eine gespeichert hat
  (`BootMayTouchDisplay`). Zusätzlich laufen Modal-Checks im Klick-Controller nur noch in
  Klick-Frames, `ClassSelectUI`/`PauseMenuUI` melden sich statt per Szenen-Scan über `AnyOpen`.

## 2026-11-21 — Menü-Flow Iteration 3: leere Slots, Startbild, Übergang

- Leere Ausrüstungsslots: Unity-Szenenserialisierung und `JsonUtility` halten `[Serializable]`-Felder
  nie `null`, ein leerer Slot kam als leere `ItemInstance` zurück („[Magic] No bonuses“ im Inventar,
  Klick darauf legte ein Phantom-Item in die Tasche). `Equipment.IsBlank`/`NormalizeEmptySlots`
  (Awake, `GetEquipped`, nach `SaveService.ApplyState`), `HasProgress` prüft `IsBlank`.
- Startbild `StartupBackdrop` neu: 1920×1080 statt hochskalierter 1024×576, Rogue ergänzt, Gruppe
  im rechten Drittel, damit die Klassenkarten (linke 64 %) niemanden verdecken.
- Übergang: Der Startbildschirm blendet nach der Wahl 0,45 s aus (Eingabe sofort frei).

## 2026-11-21 — Spawn-Load nur für beitretende Clients (Nightly-Lanternbearer rot)

- Befund: Nightly `0619770` Lanternbearer-Lauf Exit 1 (Normal/Kindled grün). Solo läuft als Host
  ohne Mitspieler; `DiabloNetworkPlayer.OnNetworkSpawn` lud für den Owner immer den lokalen
  Spielstand. Im Nightly lag dort der Fenwalker Lv 6 aus dem Kindled-Lauf: `-class lanternbearer`
  wurde überschrieben (`party=fenwalker,fenwalker,rogue`), der Trankkauf ging über den Host-Relay
  (`bought=False`), Phase `town` fehlte. Für Spieler hieß das: „Begin as <Klasse>“ behielt Level,
  Gold und Ausrüstung des alten Helden.
- Fix: `ShouldLoadSaveOnSpawn(isServer)` — nur beitretende Clients bringen ihren Helden mit.
  Host und Solo wählen auf dem Startbildschirm Continue (lädt) oder einen neuen Helden.
- Nachtrag Iteration 4: „Return to Town“ im Abschluss-Overlay landet am Town-Schrein statt im
  Lagerfeuer, synchronisiert die Physik und speichert. Belege: EditMode 379/379, PlayMode 268/268,
  Autoplay `-seed 2` Normal und danach Lanternbearer beide Exit 0 (Lanternbearer `bought=True`,
  alle 8 Phasen).

## 2026-11-21 — „New run“ setzt den gespeicherten Helden fort

- Folge aus der Spawn-Load-Regel: `DiabloInventoryUI.StartNewRun` speichert und lädt die Szene neu;
  `RuntimeInitializeOnLoad`-Boots laufen danach nicht erneut, und der Host lädt beim Spawn nicht mehr.
  Ohne Gegenmaßnahme stünde ein frischer Level-1-Held da, den der nächste Autosave über den Spielstand
  schreibt. Fix: `SaveService.ContinueAfterReload` vor dem Reload, `ClassSelectUI` hängt sich an
  `SceneManager.sceneLoaded` und setzt den Helden fort, sobald der (übernommene) Held steht.
- Beleg im Player: Shotcheck-Schritt `RELOAD_CONTINUE` vergleicht Level und Gold vor/nach dem Reload.

## 2026-11-21 — Neuer Held mit Rückfrage und frischer Welt, Automation-Saves isoliert

- „Begin as <Klasse>“ fragt jetzt, wenn ein Held auf dem Spiel steht („BEGIN ANEW?“ mit dem
  ersetzten Helden: gespeichert oder aktuell). Mitten in der Sitzung (solo) startet der neue Held per
  Szenen-Reload mit `RunProfile.Reset()` (Welt, Akt, Truhe frisch; `SaveService.NewHeroAfterReload`),
  Koop behält den Klassenwechsel an Ort und Stelle. Belege im Player: Shotcheck `NEW_HERO`.
- Fund: `shotcheck-windows.sh` startete Shotcheck und Windows-Autoplay ohne `-save-file`; gleicher
  `productName` heißt gleicher `persistentDataPath`, die Läufe schrieben also den echten
  `cinderreach-save.json` des installierten Spiels (zuletzt 2026-11-21 02:05). Jetzt
  `-save-file automation-save.json`. Der überschriebene Stand ist ohne Backup nicht wiederherstellbar.

## 2026-11-21 — Gegnernamen und Bossbalken

- Zielbalken zeigte Objektnamen mit Builder-Indizes („Ash Lancer_0“, beschworene Thralls als
  „Kiln Summon_1_1“). `DiabloTargetBar.DisplayName` entfernt Indizes und bildet interne Namen ab,
  `TierColor` färbt Elites stahlblau und Bosse gold.
- Bossbalken: breiter Balken oben mit Name und Phase (I/II), solange ein lebender Boss (Tier Boss
  oder ActBoss) im Umkreis von 24 m steht; Szenen-Scan mit 2 Hz, nie pro Frame. Der Zielbalken rückt
  darunter und blendet aus, wenn das Ziel der Boss selbst ist (per Alpha, damit Namenssuchen das
  Panel weiter finden). Belege: EditMode 381/381, PlayMode 272/272, Autoplay `-seed 0` PASS,
  Shotcheck `abbot_phase2`/`kilnmother_phase2` mit Bossbalken.

## 2026-11-21 — Loot-Namen am Boden (Alt)

- Alt halten zeigt über jedem Item-Drop im Umkreis von 18 m den Namen in Seltenheitsfarbe
  (`DiabloLootLabels`, gepoolte Schilder, überlappende Schilder rücken nach oben). Grundlage ist das
  neue Register `LootDrop.Live` statt eines Szenen-Scans. Steuerhinweise nennen „Alt Loot names“.
  Belege: EditMode 381/381, PlayMode 273/273, Autoplay `-seed 0` PASS, Shotcheck `loot_labels_alt`.

## 2026-11-22 — Akt II eröffnet: Glass Barrens (act-two-opening S1)

- Zone 4 **Glass Barrens** bei +600 m X: verglaste Ascheebene mit Dünen, zehn Glas-Spitzen in einem
  Ost- und einem West-Bogen (Nord- und Südspur bleiben frei), kalte Glow-Lichter, KayKit-Ruinen.
  Gebaut nur über `BarrensBuilder`, abgenommen durch `VerifyBarrens` in `VerifyDiabloZone`.
- Akt-Kette: `ActProgress.Barrens = 4`. `CompleteAct` (Kilnmother) öffnet die Barrens mit,
  `Unlock(Barrens)` ohne Akt I wird abgewiesen, `FromSave` schließt handeditierte Saves ohne Akt I.
  `DefeatZoneBoss` hält den künftigen Zonen-Boss-Kill.
- Save v11: `actTwoBossDefeated`. Migration v10 → v11 öffnet die Barrens für Kilnmother-Bezwinger.
- Tor Kiln → Barrens südlich des Kuppelrings mit eigenem Hinweis („The glass stays sealed“),
  Rücktor, Wegpunkt „Glass Barrens“ (an `KilnLayout.Shrines` angehängt, Index 3 bleibt Kiln),
  eigener Licht-Grade, Ambience-Pegel und Musik-Loop.
- Falle: `BarrensLayout` las `KilnLayout.KilnOffset`, während `KilnLayout.Shrines` `BarrensLayout`
  liest — zyklischer statischer Initialisierer mit genullten Feldern. `BarrensLayout` hält den
  Kiln-Offset jetzt selbst, `BarrensConfigTests` sperrt den Zyklus.

## 2026-11-22 — Glass-Barrens-Gegner (act-two-opening S2)

- **Shardback**: Nahkämpfer mit gläsernem Frontschild (`ShardGuard`). Treffer aus dem 110°-Bogen verlieren
  75 % (abgerundet, ein 1er-Stich prallt ab), Flanke und Rücken treffen voll, ein betäubter Shardback hat keine
  Deckung. Er dreht sich langsam (`turnRate` 2,5), damit die Gruppe ihn über den Tank binden und von hinten treffen kann.
- **Dune Augur**: Caster mit dem Bone-Warden-Brain, ruft statt eines Feuerballs ein Splitterfeld unter den Helden
  (1 s Warnring, dann 3 s Schnitte). Gegenmittel ist Herausgehen, Blocken hilft nicht.
- **Glass Elite**: Shardback-Elite mit 150°-Bogen und 30 HP.
- Trefferrichtung: neuer Overload `EnemyHealth.TakeDamage(amount, crit, from)` mit `IDamageFilter`. Nur Treffer mit
  bekanntem Ursprung (Nahkampf, Skills, Projektile, Koop-Validierung) laufen durch Filter.
- Fair fights: Ankunfts-Plaza der Barrens wie im Kiln, gilt auch für Caster. Der Kiln-Choke wertete Helden in
  den Barrens (x > 300) als „durch“ — jetzt auf 300–500 m begrenzt.

## 2026-11-22 — The Glasswright (act-two-opening S3)

- Zonen-Boss der Glass Barrens. Phase 1 wirft Splitterlanzen im Fächer: drei leuchtende Linien warnen
  0,9 s, dann schneidet jede einmal. Seitlich heraustreten ist die Antwort, frontales Blocken fängt sie.
- Phase 2 hebt drei Glaspfeiler, die den Boss schützen (gerichtete Treffer halbiert), bis die Gruppe sie
  zerschlägt. Dazu schnellere Lanzen und Splitterfelder. Auf Kindled ersetzt die Shatter Nova die Felder:
  wer bei der Entladung weiter als 4 m vom Boss steht, wird getroffen — die Mechanik zieht in den Nahkampf,
  die Lanzen strafen das Stehen in der Linie.
- Enemy→Hero in Koop: Lanzen, Nova und Splitterfelder werden auf jedem Peer gespawnt und gegen den
  eigenen Helden aufgelöst (wie das per-Client-HP-Modell). Das betrifft rückwirkend auch die Augur-Felder.

## 2026-11-22 — Beute der Glass Barrens (act-two-opening S4)

- Die Barrens droppen auf Itemlevel 15 (Elite 16, Boss 17) und öffnen Bracket 4 mit ×1,75 Wertebereich.
- Zwei Zonen-Uniques nur aus Bracket-4-Quellen: Shardback Aegis (Rüstung) und Glasswright Prism (Helm).
  Der erste Glasswright-Kill garantiert eines davon; der Abschlussbildschirm nennt es als besten Fund.

## 2026-11-22 — Akt-II-Nachweis (act-two-opening S5)

- Autoplay läuft jetzt bis zum Glasswright (Phasen `zone4`, `boss3`); der Koop-Client bleibt bis zum Kill und
  bucht ihn. Matrix 18/18, Koop-Paar grün, Windows 1080p schlechtestes Fenster 131,5 fps.
- Balance aus der Matrix: Der Glasschild rundete schwache Treffer auf 0 und machte Kindled-Shardbacks für den
  Lanternbearer von vorn unverwundbar. Jetzt 60 % Reduktion, gerundet, mindestens 1 Schaden.
- Sichtprüfung: Augurs gleiten zwar ohne Laufanimation, im Kampf fällt das zwischen Splitterfeldern kaum auf.
  Kein Blocker, als Politurpunkt notiert.

## 2026-11-23 — Welt-Texturen

- Bisher trug nur der Fen-Boden eine Textur. Ossuary, Kiln, Barrens und die Siedlungsobjekte waren
  einfarbige Greybox-Würfel, die Blender-Props Flachfarben ohne UVs. Jetzt 16 prozedurale, nahtlose
  Textursätze mit Normal-Maps: Schieferplatten und Quader im Ossuary, Basaltplatten mit glimmenden Fugen und
  Brandziegel im Kiln, windgeriffelter Aschesand und geschichteter Sandstein in den Barrens, Moor mit Kieseln,
  Rinde, Holz, Stein, Eisen und Knochen an den Props.
- Welt-UVs statt Objekt-UVs: gestreckte Würfel behalten die Texeldichte, benachbarte Blöcke wiederholen nicht
  dieselbe Kachelphase.
- Erster Kiln-Entwurf leuchtete zu stark und überstrahlte Lancer-Telegraphen, erster Barrens-Sand wirkte wie
  Zebrastreifen. Beides nach Windows-Shotcheck gedämpft.
- Nebenfund: `GlassWard` lag in `GlasswrightController.cs` und wurde als eingebettetes MonoScript serialisiert.
  Nach einem Neuaufbau mit frischer Library fand `VerifyBarrens` die Komponente nicht mehr. Eigene Datei
  `GlassWard.cs` behebt die fragile Bindung.

## 2026-11-23 — Dedizierter Server, Stufe 1

- Ziel: Spieler aus dem Internet treten einem dauerhaft laufenden Server bei. Eigner-Entscheidung: Pod im
  devmesh-Heimcluster, erreichbar über Unity Relay (keine offenen Ports).
- Der Server hat keinen eigenen Helden. Wo das Spiel den Host-Helden fragte (Gruppenreise am Tor), führt jetzt
  der erste lebende Spieler. ClientRpcs laufen über irgendein Spielerobjekt, weil der Server keines besitzt.
- Ein geleerter Server beendet sich nach 20 s. Der Supervisor (später Kubernetes) startet einen frischen Run,
  so bleibt kein halber Weltzustand zurück.

## 2026-11-23 — Dedizierter Server, Stufe 2

- Spieler klicken auf dem Startbildschirm „Join Server“ und landen ohne Code im laufenden Server. Der Server
  steht in einer öffentlichen Lobby „Cinderreach Server“, die seinen Relay-Code trägt.
- Die bequeme Sessions-API ließ sich nicht nutzen: Sie macht jeden anonym angemeldeten Ersteller zum Host. Relay
  und Lobby direkt anzusprechen kostet etwa 150 Zeilen und hält den Server heldenlos.
- Testläufe veröffentlichen unter eigenem Namen (`-server-name`), damit sie nie Spieler vom echten Server abziehen.

