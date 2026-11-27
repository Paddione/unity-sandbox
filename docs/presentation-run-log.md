# Presentation run log — S5 perf+proof (cinderreach-presentation)

Branch: `feature/cinderreach-presentation-s1` (Basis: origin/main mit arpg-session-loop 100 %).
Ziel: 6 Goal-Kriterien belegt, `G-DIABLO06` (Linux 30 FPS) grün, Windows 60 FPS @1080p.

## S1 light — Zonen-Lichtprofile + Überblendung

- `Settings/Volumes/{LanternHold,AshenFen,Ossuary}.asset` via `Editor/LightingBuilder.cs`
  (`Cinderreach/Lighting/Build Lighting`), Grade-Tabelle in `Scripts/ZoneAtmosphere.cs`
  (Blend 1.5 s, `ShowZone`/`Tick` headless-testbar).
- `DiabloAmbience.ApplyZone` (Bed-Volumen pro Zone), Hook in `ZoneGate.Teleport`
  (Licht + Bed + `MusicDirector.PlayZone` + `DiabloAudio.PlayGate`).
- Beleg: EditMode `ZoneAtmosphereTests` (Blend-Dauer, Grade-Differenz, Mid-Blend, Bed-Volumen).
  Shotcheck vorher/nachher je Zone: siehe Shotcheck-Abschnitt unten.

## S2 audio — Musik, SFX, Mixer-Gain, Manifest

- `DiabloAudio`: 30 Cues (`Cue`-Enum, `PlayCue`-Dispatcher), Synth-Fallback-Zähler
  (`soundedFallbacks`/`lastFallbackCue`), Override-Seam, Gain-Layer
  (`SfxGain=EffectiveSFX`, `MusicGain=Master×Music`). Alle Legacy-`Play*` unverändert.
- `Scripts/Audio/MusicDirector.cs`: `Ensure/PlayZone/PlayBoss/PlayMenu`, 2-s-Crossfade,
  Gain über `DiabloSettings`. Echter `AudioMixer`-Asset als TODO (Headless-Floor
  nach craft-game-floors game-audio).
- `docs/audio/manifest.json`: 29 SFX + 5 Musikstücke (Prompt/Seed/Synth-Rezept, frei,
  Budget ≤60 MB, `.ogg` 0.6, 22050 Hz mono, 5 ms Fade).
- Beleg: PlayMode `AudioRoutingTests` (alle Cues feuern, Fallback zählt, Override
  unterdrückt, Gain folgt `EffectiveSFX`, Director crossfadet Zone→Boss→Menü).

## S3 vfx — Particle-Prefabs, Hooks, Budget

- `Scripts/Vfx/PresentationVfx.cs`: pooled Spawn-API (FireballTrail, ArrowTrail,
  CleaveArc, BossTelegraph, PhaseTwoBurst, LevelUpPillar, RareBeam, VeilPuff),
  MaxBudget 2000, Farben aus `DiabloPalette`, 0.3–0.8 s Events / 1.2 s Milestones.
- `Editor/VfxBuilder.cs`: 7 URP-Prefabs unter `Assets/Art/Vfx` (kein VFX Graph).
- Hooks: `DiabloSkills` (alle Casts), `BossController` (Telegraph/Phase),
  `LootDrop` (Rare/Unique-Beam).
- Beleg: PlayMode `VfxSpawnTests` (jede Fähigkeit feuert, Telegraph+Phase, nur Rare+
  beamt, Budget ≤2000).

## S4 menü — Hauptmenü, Pause, Settings-Datei

- `Scripts/Menu/GameSettings.cs` ("cinderreach-settings.json", tmp+rename, Roundtrip,
  `Apply` auf `DiabloSettings`+Screen), `MainMenu.cs` (Title/Settings/Playing,
  New/Continue/Settings/Quit + Host/Join, Continue nur mit Save, HD-Scaler),
  `PauseMenu.cs` (Esc, TimeScale 0/1, Resume/Settings/Menu).
- Beleg: EditMode `GameSettingsTests` (Clamp, Save/Load-Roundtrip, Shake-Schalter
  killt `DiabloJuice.ShakeOffset`), PlayMode `MenuFlowTests`
  (Title→Settings→Title→Play→Pause→Menu).

## S5 performance + Beweis

- `scripts/shotcheck-windows.sh --autoplay`: startet `-autoplay -autoplay-out autoplay`
  bei 1920×1080, kopiert `autoplay.log` nach `artifacts/autoplay-windows.log`.
- Qualitätsstufen: `GameSettings` (Auflösung/Vollbild) + `DiabloSettings.QualityLevel`
  (Low/Med/High); URP-Asset bleibt unangetastet (Shadow-Distanz 50, 4 Kaskaden —
  Optimierungshebel dokumentiert: Schatten-Distanz → Partikelbudget → Licht-Anzahl
  pro Objekt → MSAA, je mit Vorher/Nachher-FPS).
- Linux-Gate (`G-DIABLO06`, 30 FPS): `run-autoplay.sh` + `fps-gate.py --skip-first 1`.
- Windows-Gate (60 FPS @1080p): `stage-windows.sh` →
  `SHOTCHECK_SIZE=1920x1080 bash scripts/shotcheck-windows.sh --autoplay` →
  `python3 scripts/fps-gate.py artifacts/autoplay-windows.log --min-fps 60
  --resolution 1920x1080 --skip-first 1`.

### Läufe (Stand 2026-11-18, Worktree `feature/cinderreach-presentation-s1`)

- [x] `scripts/run-unity-tests.sh`: EditMode **298/298** Passed (2026-11-18T19:23Z),
  PlayMode **160/160** Passed (2026-11-18T19:30Z). Fix: `VfxSpawnTests` Setup
  bekam `PlayerHealth` (AcquireHero braucht lebenden Spieler für Telegraph).
- [x] `SandboxBuilder.VerifyDiabloZone`: OK (`ashen fen + loot + netcode present`).
  `KayKitAssets.Verify`: OK. `python3 scripts/check-meta.py`: OK.
- [x] Linux `-autoplay`: **PASS** 2026-11-18 (`artifacts/autoplay/presentation-s1`,
  wslg D3D12 RTX 5070 Ti). `DONE phases=9 huskKills=52 bossKills=1 phaseTwo=1 overlay=2
  exceptions=0` (town → zone1 → gate → zone2 → boss → zone3 → actboss → complete,
  inkl. `waypoint_home travelled=True`). `fps-gate --min-fps 30 --skip-first 1`:
  34 Windows, worst avg **32.7 fps** → PASS. "autoplay-result.json" exit 0.
- [x] Windows-Build + Stage: **OK** 2026-11-18 (`BuildDiabloZoneWindows` Succeeded,
  `stage-windows.sh` OK, Commit `ec711e8`, 122 MB → `Games\cinderreach`).
- [x] Windows `-autoplay` @1080p: **PASS** (game exit 0, `bossKills=1 phaseTwo=1`,
  `artifacts/autoplay-windows.log`). `fps-gate --min-fps 60 --skip-first 1`:
  46 Windows, worst avg **139.2 fps** → PASS. Fix nebenbei: `fps-gate.py` akzeptiert
  deutsche Dezimalkommas (Windows-Locale `381,8` statt `381.8`).
- [x] Shotcheck PNGs: **30 Shots** → `artifacts/shotcheck` (`DONE 30 shots`), u. a. neu
  `22_class_select`, `23–25 Lanternbearer`, `26_kiln_entrance`, `27_thrall_pack`,
  `28_kilnmother_phase2`, `29_act_one_complete`, `30_session_complete`.
  Look-Bewertung nach `craft-game-floors` (visuell gesichtet):
  Town warm mit Fackeln/Vendor-Panel (HD-Scaler ok), Ossuary dunkel mit Caster-Runde
  + Telegraph-Säulen lesbar, Kiln sehr dunkel aber Held + XP-Balken lesbar, keine
  Magenta-Shader, keine Console-Errors im Build-Log. Restkosmetik (Vendor-Tooltip
  eng, Kiln fast schwarz) als Follow-up notiert, kein Gate.
- [x] Menü-Sound-Tuning (Eigner-Wunsch 2026-11-18): UIClick tiefer + leiser
  (Sweep 1200 Hz → 520→380 Hz, 0.05 s → 0.07 s, Vol 1.0 → 0.35, jetzt mit
  Gain-Layer + Fallback), VendorBuy/Sell eine Oktave tiefer (660/990 → 330/495 Hz,
  0.15 s → 0.18 s, Vol 0.8 → 0.5). Manifest-Einträge synchronisiert.
  EditMode nach Tuning: **298/298** grün.
- [ ] Shotcheck PNGs: ausstehend (Vorher/Nachher je Zone,
  Look-Bewertung nach `craft-game-floors`).

## Regression

- Suiten grün, `G-DIABLO02`-Vertrag in dotfiles um neue Audio-/VFX-/Menü-/Atmosphäre-
  Assets erweitert (`Scripts/Menu/*`, `Scripts/Audio/MusicDirector.cs`,
  `Scripts/Vfx/PresentationVfx.cs`, `Scripts/ZoneAtmosphere.cs`).
- `G-DIABLO06` (Linux 30 FPS) weiter grün.
