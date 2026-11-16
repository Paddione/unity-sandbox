---
goal: unity-diablo-d2-combat-feel
slug: unity-diablo-d2-combat-feel
branch: feature/unity-diablo-d2-combat-feel
repo_main: main
tickets: none
request: D2-Kampfgefühl für den Blood Moor (solo-first) — Sounds, Knockback, HP-Balken, Gegenangriff mit Telegraph, Schritt-in-den-Strike
health_goals: none (einmaliger Gameplay-Prototyp wie P2; Verifikation über EditMode/PlayMode-Suiten, VerifyDiabloZone und Windows-Live-Playtests)
---

# Plan: D2 Combat Feel — Blood Moor, solo-first (Sounds, Knockback, HP-Balken, Retaliation, Step-in)

## Ausgangslage (Belege)

- `sandbox/Assets/Scripts/DiabloMelee.cs` — `TryAttack` (Arc 60°, 2,2 m, `cooldown` 0,5 s), `CurrentDamage` (Equipment, Fallback `damage`), `FindArcTarget`, `Swings`-Counter; kein Step-in, kein Knockback, kein Sound.
- `sandbox/Assets/Scripts/EnemyHealth.cs` — passiv (kein Gegenangriff), `TakeDamage(int)`/`Die()`/`DropLoot`, `Flash()` (Treffer-Feedback), `SetTier`; kein Knockback, kein HP-Bruchteil.
- `sandbox/Assets/Scripts/DiabloClickController.cs` — `Strike()` (Anim + Arc, Whiffs erlaubt), Attack-Move mit `OnStrike` + Netz-Cooldown-Gate (`lastNetSwing`), `ShouldBlockClicks`.
- `sandbox/Assets/Scripts/DiabloAnimator.cs` — `Moving`/`Strike`, `strikeCount` (Präzedenz für Sound-Counter).
- `sandbox/Assets/Scripts/PlayerHealth.cs` — Max aus `Equipment`, `Heal`/`TakeDamage`/`Refresh`; **kein Tod/Respawn**.
- `sandbox/Assets/Scripts/Items/Equipment.cs` — `GetDamage`/`GetMaxHP`, Starter `Worn Short Sword` (+2, sichtbar via `StarterSword`-Prop).
- `sandbox/Assets/Scripts/DiabloInventoryUI.cs` (UGUI, `I`-Panel), `LootDrop`/`LootNetRelay` (Bag-Routing), `ZoneEntryHint` (Moor-Text), `CampfireHeal` (+1 HP/s).
- `sandbox/Assets/Editor/SandboxBuilder.cs` — `BuildDiabloZone` (Moor, 10 Husks + `Husk_Brute`, Elite, Truhe, Moor-Gate), `VerifyDiabloZone`, Prefab + Solo-Held (Inventory/Health/UI/Starter/Schwert/EventSystem mit InputSystem-Modul).
- Tests: EditMode 31/31 (`DiabloBloodMoorTests` u. a. Schwert-/EventSystem-Pins), PlayMode 21/21 (Kill→Bag→Panel, Campfire, Chase-Swing, Submit+LogAssert); Suites via `scripts/run-unity-tests.sh`, Builds via `scripts/run-unity-build.sh`.
- Prior-Art-Suche: kein `docs/adr/`, kein `tests/spec/`; `AudioSource`/`Knockback`/`Telegraph` 0 Treffer — keine Vorentscheidung, P2-Muster (lokale Stats, host-autoritative Drops) wird **erweitert, nicht ersetzt**.
- Intel-Quellen: direkte Code-Lektüre + Suiten (verfügbar). NICHT verfügbar: codebase-memory/LSP/context7/mcp-postgres/plan-intel-Helper/plan-lint (`scripts/` hat sie nicht) → als Risiko notiert statt leerer Sektion (P2-Präzedenz: `tasks.md` ohne `intel.json`).

## Brainstorming-Entscheide (gelockt, setgoal 2026-11-16 + User-Antworten)

1. **Vorbild D2** (methodisch, düster), nicht D3/D4: kein Screenshake, **keine Damage-Numbers** (hat D2 nicht), kein Hit-stop.
2. **Combat-Feel zuerst**; Loot-Tiefe und Welt-Leben sind Folge-Goals.
3. **Solo-first**; Coop bleibt kompatibel (Join/Kill/Loot-Beweise), keine neuen Netzsysteme. Retaliation client-lokal (wie `PlayerHealth`).
4. **Sound voll synthetisiert** (`DiabloAudio`, zur Laufzeit generiert, keine Binär-Assets — P2-Regel „kein .asset-Binary im Repo").
5. **Knockback klein** (1,2 m normal, 0,4 m Elite/Brute), Positions-Shift ohne Physik (Boden-Clamp y=0).
6. **HP-Balken**: ein geteilter Ziel-Balken (D2-Boss-Bar-Stil, oben mittig), gebunden an zuletzt getroffenen Husk, 3 s Fade.
7. **Schritt in den Strike** (0,5 m Richtung Aim, vor Arc-Auflösung, auch bei Whiff — Commitment). Owner bewegt sich selbst (Owner-Authority ✓).
8. **Keine Corpses** (Destroy+Orb behält Q3-Loop) — Follow-up. **Keine Vendoren/Tränke/Gold** — Folge-Goal.
9. Sounds (22050 Hz mono, alle synth): Swing (0,12 s Noise-Burst), Hit (0,15 s 180→90 Hz Thump), Death (0,4 s 300→60 Hz), LootBlip, UIClick, Hurt, PlayerDeath.

## Partial-Manifest (disjunkte target_files, max 9; Tests separat in P5)

| Partial | Inhalt | target_files (exklusiv) |
|---|---|---|
| P1 audio | Synth-Clips + Zähler-API | `sandbox/Assets/Scripts/DiabloAudio.cs` (neu) |
| P2 strike-feel | Step-in, Knockback-Auslösung, Sound-Hooks, LastStruck | `sandbox/Assets/Scripts/DiabloMelee.cs`, `sandbox/Assets/Scripts/EnemyHealth.cs` |
| P3 retaliation | Telegraph/Windup/Cooldown, Tod + Respawn | `sandbox/Assets/Scripts/EnemyAttack.cs` (neu), `sandbox/Assets/Scripts/PlayerHealth.cs` |
| P4 wiring+bar | Ziel-Balken, Builder-Verdrahtung, Verify-Update, Hinweis-Text, UI-Klick-Sound | `sandbox/Assets/Scripts/DiabloTargetBar.cs` (neu), `sandbox/Assets/Editor/SandboxBuilder.cs`, `sandbox/Assets/Scenes/DiabloZone.unity`, `sandbox/Assets/Scenes/DiabloZone.unity.meta`, `sandbox/Assets/Scripts/ZoneEntryHint.cs`, `sandbox/Assets/Scripts/DiabloInventoryUI.cs` |
| P5 proof | Tests + Docs | `sandbox/Assets/Tests/EditMode/DiabloD2FeelTests.cs` (neu), `sandbox/Assets/Tests/PlayMode/DiabloD2FeelLoopTests.cs` (neu), `docs/decision-log.md`, `docs/dev-journal.md`, `docs/mechanics-questions.md` |

Schnittstellen: P1 definiert `DiabloAudio.Play{Swing,Hit,Death,Loot,UIClick,Hurt,PlayerDeath}()` + Zähler (`soundedSwings/Hits/Deaths`, `strikeCount`-Präzedenz) — P2/P3 rufen nur diese auf. P2 setzt `DiabloMelee.LastStruck` (static, nullbar) + `EnemyHealth.ApplyKnockback(Vector3)` + `EnemyHealth.HPFraction()` — P4-Bar pollt nur diese. P3 nutzt `PlayerHealth.TakeDamage` (bestehend) + neu `PlayerHealth.DieAndRespawn()` (P3-eigen). P4 aktualisiert `VerifyDiabloZone` (EnemyAttack auf Husks, Bar, Audio → `CountZoneRecursive`-Signatur folgt); P5 nutzt die neuen Asserts. S1: kein `baseline.json` — neue Dateien ≤ ~150 Zeilen.

## P1 — Audio-Synth (Ziel: hörbare Schläge ohne Assets)

- `Scripts/DiabloAudio.cs` (neu, static): `AudioClip`-Factory (`AudioClip.Create` + `SetData`, 22050 Hz mono) für Swing/Hit/Death/Loot/UIClick/Hurt/PlayerDeath (Dauern/Freqs s. o.); lazy `AudioSource` auf hidden GO (headless: stiller No-op, kein Crash); `Play*()`-Methoden + `sounded*`-Zähler.
- Akzeptanz: alle 7 Clips non-null, `samples > 0`, `frequency == 22050` (EditMode, ohne Playback).

## P2 — Strike-Feel (Ziel: Wucht + Schritt)

- `DiabloMelee.cs`: `Strike`-naher Step-in (0,5 m Richtung `AimDirection` vor Arc-Auflösung, y=0-Clamp); nach Treffer `ApplyKnockback` (1,2 m / 0,4 m Elite+) vom Angreifer weg; `PlaySwing` bei jedem Schwung, `PlayHit` bei Treffer; `LastStruck` setzen/löschen.
- `EnemyHealth.cs`: `ApplyKnockback(Vector3 dir)` (distanzgewichtet, y=0, kein Rigidbody nötig), `HPFraction()` (hp/max, clamped).
- Solo-Pfad UND Attack-Move-Pfad (beide `TryAttack`-Stellen); Netz-Client-Pfad nutzt weiter das Controller-Gate (kein Doppel-Sound: Server spielt nichts, Client spielt lokal).
- Akzeptanz: PlayMode misst Versatz ≥ 0,8 m nach Treffer; Step-in 0,5 m auch bei Whiff; Zähler steigen.

## P3 — Retaliation (Ziel: Moor wehrt sich, Tod hat Folgen)

- `Scripts/EnemyAttack.cs` (neu): Reichweite 2,0 m, Windup 0,4 s (Rot-Tönung als Telegraph, `Flash`-Muster), Schaden 1 (normal) / 2 (Elite/Brute), Cooldown 1,5 s; trifft nur Helden mit `PlayerHealth`; spielt `PlayHurt`, bei Tod `PlayPlayerDeath`.
- `Scripts/PlayerHealth.cs`: `DieAndRespawn()` (HP 0 → Teleport `CoopSpawn_0`, Full-Heal; Husks behalten HP — kein Corpse-Run).
- Akzeptanz (headless, keine Timing-Abhängigkeit im Assert außer Wait-Fenster): Husk neben Held → HP sinkt nach Windup; Tod → Respawn am Camp mit vollem HP.

## P4 — Wiring + Ziel-Balken (Ziel: sichtbar, was man jagt)

- `Scripts/DiabloTargetBar.cs` (neu, UGUI top-center): Name + Balken aus `LastStruck`/`HPFraction()` in 12 m, Fade 3 s nach letztem Hit; leer = versteckt.
- `SandboxBuilder.cs`: `EnemyAttack` in `SpawnZoneHusk` (Schaden nach Tier), `VerifyDiabloZone`+`CountZoneRecursive` erweitern (EnemyAttack je Husk, Bar-Objekt, `DiabloAudio`-Erwähnung nicht nötig — kein Szenen-Objekt), Szene + Prefab regenerieren (Schwert/Verify-Pins bleiben grün).
- `ZoneEntryHint.cs`: Retaliation-Warnung anhängen („Husks schlagen zurück — weiche aus, Camp heilt.").
- `DiabloInventoryUI.cs`: 1-Zeilen-Hook `PlayUIClick` im Panel-Toggle.
- Akzeptanz: Verify grün, Bar folgt Treffer, Hint-Text aktuell.

## P5 — Tests + Docs (Beweis)

- `Tests/EditMode/DiabloD2FeelTests.cs`: Clip-Specs (7× non-null/`samples`/`frequency`), Knockback-Konstanten, Retaliation-Konstanten (Range/Windup/CD/Schaden), Step-in-Konstante, Szene-Pins (EnemyAttack je Husk, Bar-Objekt, `StarterSword` intakt).
- `Tests/PlayMode/DiabloD2FeelLoopTests.cs` (InputTestFixture-Präzedenz wo Tasten nötig): Kill→Knockback-Versatz; Retaliation senkt HP; Tod→Respawn am Camp voll; Bar-Bruchteil == Husk-HP; Step-in 0,5 m; `sounded*`-Zähler steigen (LogAssert für Sound-Pfade ohne Log-Spam).
- Docs: `decision-log.md` (Q8-Entscheid), `dev-journal.md`, `mechanics-questions.md` (Q8: „Fühlt sich der Moor-Kampf wie D2 Act 1 an?").
- Akzeptanz: EditMode + PlayMode grün, `VerifyDiabloZone` grün.

## Finaler Verify-Task (Pflicht vor Commit)

1. `bash scripts/run-unity-tests.sh` (EditMode + PlayMode, XML `Passed`).
2. `run-unity-build.sh SandboxBuilder.BuildDiabloZone "Saved Assets/Scenes/DiabloZone.unity"` + `-executeMethod SandboxBuilder.VerifyDiabloZone` (OK-Zeile) — Wrapper-Pflicht (Service-Stop, Heartbeat, Timeout).
3. `python3 ~/scripts/agent-workspace.py check` im Worktree-Root (still = ok).
4. Builds via Wrapper: Server + Windows + Linux (`DiabloZone* build result: Succeeded`); Deploy Server (rsync + restart, Join-Smoke); Windows-Stage nach `/mnt/c/.../diablo-bloodmoor/`.
5. Commit `feat(diablo): d2 combat feel` (gezielte Pfade, kein `-A`), `land` (ff) + Push (Dauerfreigabe), `.agents/goals/unity-diablo-d2-combat-feel/PROGRESS.md` belegte Prozente (NICHT 100 % — Umsetzung folgt; `Offene Punkte` aktualisieren).
6. Goal-Datei-Änderung (PROGRESS.md) läuft über denselben Land mit (gehört zum Plan), GOAL.md bleibt unverändert.

## Übergabe an dev-flow-execute

Stand bei STOPP: Branch `feature/unity-diablo-d2-combat-feel` gepusht, Plan committiert, kein PR, Branch-Lock aktiv. Nächster Schritt: `dev-flow-execute` liest diesen Plan (kein Ticket-Backend — Pfad per Branch-Konvention). Follow-ups (nicht in diesem Cut): Corpses, Vendoren/Tränke/Gold, Damage-Numbers (explizit nicht-D2), Screenshake, Coop-Sync-Feeling, Idle-Anims, Gamepad.
