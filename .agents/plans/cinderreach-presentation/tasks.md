---
goal: cinderreach-presentation
slug: cinderreach-presentation
branch: feature/cinderreach-presentation-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO05, G-DIABLO06]
start_condition: arpg-session-loop 100 %
---

# Plan: cinderreach-presentation — Licht, Audio, VFX, Menü, 60 FPS

Pfad: feature, 5 Stufen. Ausführung nach dem **Session-Protokoll** aus
`.agents/plans/arpg-session-loop/tasks.md` (Schritte 1–9), Lock-Name `task cinderreach-presentation-sN`.
Protokollschritt 1 prüft zusätzlich: `.agents/goals/arpg-session-loop/PROGRESS.md` zeigt `Prozent: 100 %`.
Vor S1 den Skill `craft-game-floors` laden (Art-Stil, Licht, Juice, Sound, Menüs, Look-Scoring).

## Ausgangslage (Belege, Stand 2026-11-17)

- URP mit `Settings/PC_RPAsset` + `PC_Renderer`, `DefaultVolumeProfile.asset`. Post-Processing nutzt
  heute nur `DiabloAmbience.cs` (Moor-Haze, Fackeln).
- Audio: `DiabloAudio.cs` (310 Z.) erzeugt 7+ Synth-Clips zur Laufzeit (22050 Hz mono). Keine
  `AudioMixer`-Assets im Projekt.
- VFX: `DiabloJuice` (Impact-Bursts, Schadenszahlen, Screenshake), `DiabloFire` (Feuer), `DiabloProjectile`.
  Keine Particle-Prefabs pro Skill.
- Menü: `DiabloMenuArt` + MenuArt-Sprites (`Resources/MenuArt/`, Starter-Pack inkl. `BloodBanner`,
  `LootChest`, `TorchSconce`). HD-Scaler-Konvention 1920×1080, match 0.5 (`DiabloHdTests`).
- Perf: `FpsReporter` loggt `[Perf] FPS avg …`, `scripts/fps-gate.py`. Windows-Start über
  `scripts/shotcheck-windows.sh` (startet die exe, sammelt PNGs).
- Lokale Generatoren: `studio_generate_audio` (Fleet-MCP) für Musik/SFX, Blender-MCP für Props,
  `docs/menu-art/build_*.py` für Sprites.

## Entscheide

1. Audio-Assets werden **lokal generiert** (`studio_generate_audio`) und als `.ogg` unter
   `sandbox/Assets/Audio/{Music,Sfx}/` abgelegt (LFS). Prompt, Modell und Seed je Datei in
   `docs/audio/manifest.json`, damit sie reproduzierbar bleiben. Freie Fremd-Assets nur mit Lizenzeintrag
   im Manifest, nichts Bezahltes.
2. Ein `AudioMixer` mit Gruppen Master/Music/SFX/UI. `DiabloAudio` spielt bevorzugt Asset-Clips und
   fällt auf Synth zurück, wenn ein Clip fehlt (bestehende Tests bleiben gültig).
3. Lighting per Zone über **Volume-Profile + Lichtrig-Prefabs**, die beim Zonen-Wechsel überblenden.
   Kein Light-Baking (Builder regeneriert Szenen, Baking würde jeden Lauf verlängern).
4. Die Performance-Messung auf Windows nutzt den Autoplay. `shotcheck-windows.sh` bekommt einen
   `-autoplay`-Modus, der Log zurückkopiert.

## Partial-Manifest

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 light | Volume-Profile + Lichtrigs je Zone, Überblendung | `Settings/Volumes/{LanternHold,AshenFen,Ossuary}.asset` (neu), `Editor/LightingBuilder.cs` (neu), `Scripts/ZoneAtmosphere.cs` (neu), `Scripts/DiabloAmbience.cs`, `Tests/EditMode/ZoneAtmosphereTests.cs` |
| S2 audio | Musik, SFX, Mixer, Settings-Hook | `Assets/Audio/**` (neu), `Audio/CinderreachMixer.mixer` (neu), `Scripts/DiabloAudio.cs`, `Scripts/Audio/MusicDirector.cs` (neu), `docs/audio/manifest.json` (neu), `Tests/PlayMode/AudioRoutingTests.cs` |
| S3 vfx | Particle-Prefabs Skills/Boss/Level-Up/Rare-Beam | `Assets/Art/Vfx/**` (neu), `Editor/VfxBuilder.cs` (neu), `Scripts/DiabloSkills.cs` (VFX-Hooks), `Scripts/Enemies/BossController.cs` (Telegraph-VFX), `Scripts/Loot/LootDrop.cs` (Beam), `Tests/PlayMode/VfxSpawnTests.cs` |
| S4 menu | Hauptmenü, Pause, Settings-Datei | `Scripts/Menu/MainMenu.cs`, `Scripts/Menu/PauseMenu.cs`, `Scripts/Menu/GameSettings.cs` (neu), `Scripts/DiabloMenuArt.cs`, `Scripts/DiabloJuice.cs` (Shake-Schalter), `Tests/EditMode/GameSettingsTests.cs`, `Tests/PlayMode/MenuFlowTests.cs` |
| S5 perf+proof | Windows-60-FPS-Messung, Optimierung, Run-Log | `scripts/shotcheck-windows.sh` (`-autoplay`-Modus), `Settings/PC_RPAsset.asset` (Qualitätsstufen), `docs/presentation-run-log.md` (neu), dotfiles `scripts/health_probes/diablo_unity.py` (G-DIABLO02-Vertrag) |

S1, S2, S3 sind disjunkt und dürfen parallel laufen. S4 nach S2 (Lautstärke-Settings brauchen den Mixer).
S5 zuletzt.

## S1 — Licht pro Zone

- Lantern Hold: warmes Key-Light (2700 K), Laternen-Punktlichter, leichte Bloom. Ashen Fen: kühles
  Mondlicht, Exponential-Fog, gedämpfte Sättigung. Ossuary: fast dunkel, Fackel-Inseln, Vignette.
- `ZoneAtmosphere` blendet beim `ZoneGate`-Teleport das Profil über 1,5 s um.
- Beleg: Shotcheck vorher/nachher je Zone, Look-Bewertung nach `craft-game-floors` im Run-Log.

## S2 — Audio

- Musik: 3 Zonen-Loops (60–90 s, nahtlos) + Boss-Track + Menü-Thema. SFX ≥ 20: Schwert-Treffer (3
  Varianten), Fireball/Arrow/Cleave, Husk-/Warden-/Boss-Laute, Gold, Rare-Drop, Level-Up, UI-Klick,
  Vendor-Kauf/-Verkauf, Gate, Tod.
- `MusicDirector` crossfadet bei Zonen-Wechsel und Boss-Phase.
- Tests: jeder `DiabloAudio`-Cue löst einen Clip aus der richtigen Mixer-Gruppe aus; fehlender Clip →
  Synth-Fallback (Zähler).

## S3 — VFX

- URP-Particle-Prefabs (kein VFX Graph, damit Linux-llvmpipe-Läufe tragbar bleiben): Fireball-Trail +
  Explosion, Arrow-Trail, Cleave-Bogen, Boss-Bodenkreis + Phase-2-Ausbruch, Level-Up-Säule,
  Rare/Unique-Lichtsäule über Drops.
- Partikelbudget: max. 2000 aktiv gleichzeitig, Prüfung im PlayMode-Test.

## S4 — Menü + Settings

- Hauptmenü: Continue (nur mit Save), New Game, Host/Join (falls coop-session schon gelandet, sonst
  ausgeblendet), Settings, Quit. Pause (Esc): Resume, Settings, Menü.
- `GameSettings` (JSON in `persistentDataPath/cinderreach-settings.json`): Auflösung, Vollbild,
  Master/Music/SFX/UI-Lautstärke, Kamera-Shake an/aus, Schadenszahlen an/aus.
- Tests: Settings-Roundtrip, Shake-Schalter wirkt auf `DiabloJuice`, Menü-Fluss New → Spiel → Pause → Menü.

## S5 — Performance + Beweis

- Windows-Lauf: `stage-windows.sh` → `shotcheck-windows.sh --autoplay` bei 1920×1080 Vollbild, Log zurück,
  `fps-gate.py --min-fps 60 --resolution 1920x1080 --skip-first 1`.
- Unter 60: Profiler-Lauf (Development-Build), Gegenmittel in dieser Reihenfolge: Schatten-Distanz,
  Partikelbudget, Licht-Anzahl pro Objekt, MSAA. Jede Maßnahme mit Vorher/Nachher-FPS im Run-Log.
- `G-DIABLO02`-Vertrag in dotfiles um Audio/VFX/Volume-Assets erweitern (eigener Worktree, `land`).
- Akzeptanz: alle 6 Goal-Kriterien belegt, `G-DIABLO06` (Linux 30 FPS) weiter grün.

### Statustabelle

| Stufe | Abhängig von | Gewicht | Status |
|---|---|---|---|
| S1 light | Startbedingung | 20 % | gelandet 2026-11-18 |
| S2 audio | Startbedingung | 25 % | gelandet 2026-11-18 |
| S3 vfx | Startbedingung | 20 % | gelandet 2026-11-18 |
| S4 menu | S2 | 15 % | gelandet 2026-11-18 |
| S5 perf+proof | S1–S4 | 20 % | gelandet 2026-11-18 |

## Verify (je Stufe)

1. `scripts/run-unity-tests.sh` grün.
2. `VerifyDiabloZone` + `KayKitAssets.Verify` grün.
3. `python3 scripts/check-meta.py` ohne Befund, neue Binärdateien über LFS (`git lfs ls-files` zeigt sie).
4. Windows-Shotcheck der betroffenen Szene, PNG gesichtet (Memory-Referenz: Unity-Shotcheck).
5. Ab S5: Windows-FPS-Gate 60 @1080p.

## Risiken

- R1 Generierte Musik loopt hörbar. Gegenmittel: Crossfade-Loop im `MusicDirector`, Längen ≥ 60 s.
- R2 LFS-Volumen durch Audio. Budget: ≤ 60 MB Audio gesamt, `.ogg` Qualität 0.6.
- R3 Parallel-Läufe S1–S3 regenerieren beide die Szene über Builder. Gegenmittel: S1–S3 hängen nur
  Prefabs/Components an, die Szene wird einmal pro Stufe beim Landen über den Builder neu gebaut. Bei
  Konflikt im `land`-Rebase die Szene neu bauen, nie Szenen-YAML von Hand mergen.
- R4 Windows-GPU nicht erreichbar (Rechner aus). Dann S5 `blocked: Windows-Host offline`, kein Ersatzbeleg.
