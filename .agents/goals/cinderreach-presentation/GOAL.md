---
goal: cinderreach-presentation
slug: cinderreach-presentation
health_goals: [G-DIABLO02, G-DIABLO05, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-presentation/tasks.md]
---

# Goal: cinderreach-presentation — Präsentation: Licht, Audio, VFX, Menü, Performance

Angelegt: 2026-11-17. Herkunft: Eigner-Auftrag „come up with the next goals“ nach `arpg-session-loop`.
Folge-Goal, Reihenfolge siehe `.agents/goals/ROADMAP.md`.

## Objective

Cinderreach sieht und klingt nach einem fertigen Spiel: zonenspezifisches Licht und Nebel,
generierte statt synthetischer Audio-Assets (Musik pro Zone, SFX), Skill- und Boss-VFX, Hauptmenü mit
Einstellungen. Dabei hält es 60 FPS bei 1080p auf der Windows-GPU.

## Erfolgskriterien

1. Licht: eigenes Lighting-Profil je Zone (Lantern Hold warm, Ashen Fen kalt-dunstig, Ossuary düster mit
   Fackel-Inseln), Post-Processing-Volumes (URP), Look-Bewertung mit dem Skill `craft-game-floors`.
   Beleg: Shotcheck-PNGs vorher/nachher je Zone im Run-Log.
2. Audio: Musik-Loop je Zone + Boss-Track, ≥ 20 SFX (Treffer, Skills, UI, Vendor, Gold), generiert lokal
   (`studio_generate_audio`) oder mit freier Lizenz dokumentiert. Mixer-Gruppen Music/SFX/UI mit
   Lautstärke-Settings. `DiabloAudio`-Synth bleibt nur als Fallback.
3. VFX: Partikel für Fireball, Arrow, Cleave, Boss-Telegraph und Phasenwechsel, Level-Up und Rare-Drop-Beam.
4. Hauptmenü + Pause: New/Continue/Settings/Quit, Settings für Auflösung, Vollbild, Lautstärken, Kamera-Shake
   an/aus. Persistiert in eigener Settings-Datei.
5. Performance: Windows-Lauf mit `FpsReporter` über eine komplette Autoplay-Session bei 1920×1080, danach
   `scripts/fps-gate.py --min-fps 60 --resolution 1920x1080` grün. Linux-Gate (`G-DIABLO06`, 30 FPS) bleibt grün.
6. Regression: Suiten grün, `G-DIABLO02`-Vertrag um neue Audio-/VFX-Assets erweitert.

## Nicht-Ziele

- Kein Pipeline-Wechsel (URP bleibt), kein HDRP.
- Keine neuen Gameplay-Systeme.
- Keine bezahlten Asset-Store-Käufe ohne Eigner-Freigabe.

## Abhängigkeiten

- Start erst nach `arpg-session-loop` 100 %. Kann parallel zu `cinderreach-coop-session` laufen (andere
  Dateien: Audio/VFX/Lighting vs. Netcode). Kollisionen über `target_files` im jeweiligen Plan ausschließen.
- HD-UI-Konvention (CanvasScaler 1920×1080, match 0.5) aus dem HD/MenuArt-Pack.
- health_goals: [G-DIABLO02, G-DIABLO05, G-DIABLO06]
