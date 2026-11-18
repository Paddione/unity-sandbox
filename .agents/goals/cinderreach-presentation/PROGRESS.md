# Progress: cinderreach-presentation

Prozent: 100 %

## Erledigt

- 2026-11-17: Goal angelegt (Folge-Goal nach `arpg-session-loop`).
- 2026-11-17: Plan `.agents/plans/cinderreach-presentation/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: Startbedingung erfüllt (`arpg-session-loop` 100 %), Branch
  `feature/cinderreach-presentation-s1` angelegt.
- 2026-11-18: S1 light gelandet (Worker): `Settings/Volumes/{LanternHold,AshenFen,Ossuary}.asset`,
  `Editor/LightingBuilder.cs`, `Scripts/ZoneAtmosphere.cs` (Blend 1.5 s), `DiabloAmbience.ApplyZone`,
  `ZoneGate.Teleport`-Hook (Licht + Bed + Musik + Gate-Sound). Tests `ZoneAtmosphereTests`.
- 2026-11-18: S2 audio gelandet (Worker): `DiabloAudio` 30 Cues + Fallback-Zähler + Gain-Layer,
  `Scripts/Audio/MusicDirector.cs` (2-s-Crossfade), `docs/audio/manifest.json` (29 SFX + 5 Musik).
  Tests `AudioRoutingTests`.
- 2026-11-18: S3 vfx gelandet (Worker + 1 Fix): `Scripts/Vfx/PresentationVfx.cs` (Budget 2000, pooled),
  `Editor/VfxBuilder.cs`, Hooks in `DiabloSkills`/`BossController`/`LootDrop`. Tests `VfxSpawnTests`
  (Fix: `PlayerHealth` im Setup für Telegraph).
- 2026-11-18: S4 menü gelandet (Hauptspur): `Menu/GameSettings.cs` (JSON-Roundtrip),
  `Menu/MainMenu.cs`, `Menu/PauseMenu.cs`. Tests `GameSettingsTests` + `MenuFlowTests`.
- 2026-11-18: S5 perf+proof: `shotcheck-windows.sh --autoplay`, `fps-gate.py` Komma-Fix,
  `docs/presentation-run-log.md`. Menü-Sound-Tuning (UIClick 520→380 Hz/0.35,
  Vendor 330/495 Hz/0.5).
- 2026-11-18: Belege — EditMode **298/298**, PlayMode **160/160**, `VerifyDiabloZone` OK,
  `KayKitAssets.Verify` OK, `check-meta` OK, Linux-Autoplay `DONE phases=9 exceptions=0`
  + `fps-gate --min-fps 30` PASS (worst 32.7 fps), Windows-Autoplay @1080p
  `fps-gate --min-fps 60` PASS (worst 139.2 fps), Shotcheck **30 Shots** gesichtet,
  `G-DIABLO02`-Vertrag in dotfiles erweitert (eigener Worktree `health-probe-presentation`,
  Probe-Tests 24/24).

## Nächste Schritte

Keine — alle 6 Erfolgskriterien belegt, alle 5 Stufen gelandet.

## Offene Punkte

Keine.
