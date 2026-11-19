# Lokaler Art-Pilot (Cinderreach)

Stand: S1 abgeschlossen, S2/S3 in Arbeit. Ziel: drei Assets (Loot-Truhe,
ausgefranste Banner, vierbeiniger Gegner + Loot-Icon) auf der RTX 5070 Ti
(16.303 MiB) lokal erzeugen und bis in den spielbaren Cinderreach-Build bringen.

## 1. Setup (reproduzierbar)

- ComfyUI v0.46.0 (Commit `a277b5b`), frischer Clone unter `/tmp` (nicht im Repo).
- Isoliertes venv: `torch 2.9.1+cu128`, `torchvision 0.24.1+cu128`
  (Blackwell sm_120; nightly 2.12 + stable 2.10 sahen die GPU NICHT — nur 2.9.1 ok).
- WSL-Quirk: `CUDA_VISIBLE_DEVICES` enthält eine ungültige GPU-UUID
  (Windows-Seite). Für torch/ComfyUI **unset** lassen, sonst `cuda=False`.
- ComfyUI-Port: `:8188` ist auf dieser Box blackholed → **`:18188`** nutzen.
- Start: `env -u CUDA_VISIBLE_DEVICES python main.py --port 18188`
- Treiber 620.70.03 / CUDA UMD 13.5 / System-CUDA 13.4.

## 2. Modelle (alle Hugging Face, Gewichte NICHT im Repo)

| Zweck | Repo | Datei | Größe | Lizenz |
|---|---|---|---|---|
| FLUX.2-klein 4B FP8 (base) | Comfy-Org/Flux-2-Klein-4B | split_files/diffusion_models/flux-2-klein-base-4b.safetensors | 7,3 GB | Apache-2.0 (BFL) |
| Textencoder Qwen 4B | dto. | split_files/text_encoders/qwen_3_4b.safetensors | 7,5 GB | Apache-2.0 |
| VAE FLUX.2 | dto. | split_files/vae/flux2-vae.safetensors | 0,3 GB | Apache-2.0 |
| TRELLIS.2 INT8 | Comfy-Org/TRELLIS.2 | diffusion_models/trellis_2_int8_convrot.safetensors | 4,9 GB | MIT (Microsoft) |
| Shape-VAE bf16 | dto. | vae/trellis_2_shape_vae_bf16.safetensors | 1,1 GB | MIT |
| Texture-VAE bf16 | dto. | vae/trellis_2_texture_vae_bf16.safetensors | 0,9 GB | MIT |
| DINOv3 ViT-L | dto. | clip_vision/dino_v3_vit_l.safetensors | 1,2 GB | Meta Research (CC-BY-NC? siehe HF-Karte) |
| Pixal3D INT8 (ungenutzt, nur Switch-Gegenpfad) | Comfy-Org/Pixal3D | diffusion_models/pixal3d_int8_convrot.safetensors | 5,3 GB | MIT |
| BiRefNet (Freistellen) | Comfy-Org/BiRefNet | background_removal/birefnet.safetensors | 0,4 GB | MIT |

DINOv3-Lizenz vor Produktionsnutzung prüfen (Forschungs-Encoder; Pilot ok).
Referenzen: <https://docs.comfy.org/tutorials/flux/flux-2-klein>,
<https://docs.comfy.org/tutorials/3d/trellis2>.

## 3. Workflows (wieder öffnbar, API-Format)

- `workflows/concept.json` — FLUX.2-klein-T2I exakt nach offiziellem Template
  (`image_flux2_klein_text_to_image.json`): UNETLoader + CLIPLoader(flux2) +
  CLIPTextEncode + EmptyFlux2LatentImage + CFGGuider(cfg 5) + Flux2Scheduler +
  KSamplerSelect(euler) + RandomNoise + SamplerCustomAdvanced + VAEDecode +
  SaveImage. 20 Steps. Erzeugt mit `driver.py` (siehe Runbook-Notizen).
- `workflows/prop.json` — TRELLIS.2-INT8 exakt nach offiziellem Template
  (`3d_pixal3d_trellis2_image_to_model.json`), konvertiert mit
  `trellis_convert.py` (UI→API, Switch=true-Pfad, Pixal3D/MoGe-Zweige
  entfernt, Finale SaveGLB). Besonderheiten: DynamicCombo-Subinputs brauchen
  dotted keys (`sign_mode.fill`), verlinkte Widget-Inputs belegen trotzdem
  Widget-Slots, `control_after_generate` als API-Input mitschicken.

## 4. Messwerte S1 (RTX 5070 Ti, 16303 MiB)

VRAM-Logging: `nvidia-smi ... -lms 200` von Idle bis Export (logs/vram-s1.csv).
Idle nach llama-Stopp: 1463 MiB.

| Asset | Seed | Auflösung | Gen-Zeit | VRAM-Peak | Versuche |
|---|---|---|---|---|---|
| Truhe-Konzept (FLUX) | 7 | 768² | 55,6 s (Kaltstart) | 15517 MiB | 1 |
| Banner-Konzept (FLUX) | 11 | 768² | 12,8 s | 15517 MiB | 1 |
| Hund-Konzept (FLUX) | 21 | 768² | 13,6 s | 15517 MiB | 1 |
| Loot-Icon (FLUX) | 31 | 512² | 6,8 s | 15517 MiB | 1 (+ PIL-Freistellen) |
| Truhe-GLB (TRELLIS.2 INT8) | 101 | 1536 Upsample / 4K-Bake | 1391 s (~23 min) | 15970 MiB | 2 (1× Pfadfehler) |
| Hund-GLB (TRELLIS.2 INT8) | 202 | dto. | 272 s (~4,5 min, warm) | 11833 MiB | 1 |

Alle Läufe passten in 16 GB (Peak 15.970 MiB). Kaltstart kostet ~15 min extra
(Modell-Load + Shader-Kompilate).

## 5. Produktionsentscheidung (pro Assetklasse)

- **Statische Props (Truhe): lokale Produktion GENÜGT.** TRELLIS.2-GLB
  (699k Tris) → Blender-Decimate auf 3.000 Tris, Texturen 1K, 4,3 MB.
  Nacharbeit ~30 min (Decimate + Pivot/Maßstab + Prüfung).
- **Stoff/Alpha (Banner): lokale Produktion GENÜGT, ohne 3D-Modell.**
  FLUX-Konzept → PIL-Alpha-Maske → Blender-Stoffplane (475 Tris) mit
  Alpha-Maske (MASK, Cutoff 0,5). Nacharbeit ~45 min. TRELLIS.2 für Banner
  nicht nötig (dünner Stoff → Plane ist die bessere Produktionswahl).
- **Quadruped: lokale Produktion GENÜGT mit Retopo/Rig.**
  TRELLIS.2-Entwurf (700k Tris) → Decimate 12k → FK-Rig + 2 Clips (Run/Attack).
  Nacharbeit ~2 h (Rig-Kontrolle + Clip-Tuning). Reine Cloud-Alternative
  (Modellierung beauftragen) wäre ein Mehrfaches an Zeit/Kosten.
- **Loot-Icon: lokale Produktion GENÜGT.** FLUX + Freistellen, ~10 min.

Cloud-Einsparung (Schätzung): vergleichbare Stock-/Auftrags-Assets
(Truhe/Banner/Hund-animiert/Icon) ca. 150–400 € Marktplatz- bzw.
Auftragskosten; lokale Stromkosten pro Pilotlauf vernachlässigbar
(< 1 kWh GPU-Zeit für 2D, ~0,3 kWh pro 3D-Lauf warm). Einmaliges Setup
(~4 h inkl. Toolchain-Diagnose) amortisiert sich ab dem zweiten Asset-Set.

## 6. Unity-Einbindung (S2/S3)

- `sandbox/Assets/Art/LocalArtPilot/`: `LootChest.glb`, `TornBanner.glb`
  (glTFast-Import), `Quadruped.fbx` (Generic-Rig), `LootIcon.png`,
  Materialien + Wrapper-Prefabs unter `Prefabs/`.
- glTFast-Eintrag in `sandbox/Packages/manifest.json`.
- Platzierung über `SandboxBuilder`/`OssuaryBuilder`-Hooks (Loot-State und
  Collider bleiben an der vorhandenen Spielobjektwurzel).
- Truhe (DChestZone): KayKit-Visuals deaktiviert (Wurzel aktiv!), Pilot-
  Wrapper als Kind; Icon-Billboard (URP/Lit + AlphaClip) schwebt darüber.
- Banner: WorldArt-AlcoveBanner ersetzt (gleiche Position), zweites Banner
  am Ossuary-Tor. URP/Lit-Materialien aus GLB-Texturen erzeugt.
- Gegner (Fen Brute): Skeleton-Renderer deaktiviert, PilotHound-Visual als
  Kind (Yaw -90°, Scale 1,15), eigener Animator (Run/Attack, kein Root Motion),
  `QuadrupedMimic` spiegelt Host-State (moving/attackCount/IsAlive).
- Diagnosen unterwegs (alle behoben, Tests wieder 344/344):
  HideVisuals deaktivierte versehentlich die Chest-Wurzel (Renderer auf
  Wurzel) → Wurzel ausgenommen; GLB-Materialien nutzten den glTF-Shader
  statt URP/Lit → persistente URP/Lit-Material-Assets (nested Prefabs
  verwerfen In-Memory-Materialien!); Sprite-Import stand auf Multiple →
  Single erzwungen.

## 7. Build + Spielkamera (S4)

- Linux-Player: `SandboxBuilder.BuildDiabloZoneBuild`,
  Marker `DiabloZone build result: Succeeded`, 140 MB, ~184 s
  (Build-Revision: Branch `feature/cinderreach-local-art`).
- Spielkamera-Aufnahmen (`-pilotshots -class fenwalker`, Exit 0):
  `shots/00_pilot_chest` (Truhe + Icon-Billboard),
  `01_pilot_icon` (Billboard-Close-up), `02_pilot_banner_alcove` +
  `03_pilot_banner_ossuary` (ausgefranste Alpha-Kante vor dunkel),
  `04_pilot_hound_idle`, `05/07_pilot_hound_run_*` (Trab),
  `09/11_pilot_hound_attack_*` (Ansprung), `13/14_pilot_hound_combat_*`
  (echter Aggro-Take: Fen-Brute-Zielleiste, Trefferzahlen, Loot-Orb).
- Diagnose unterwegs: Menü (Klassenauswahl) lag anfangs über den Shots →
  `-class <id>`-Flag + Dismiss pro Shot; Rig fand erst den falschen
  (Skelett-)Animator → Auswahl über Mimic; Controller band
  `__preview__`-Takes → exakter Take-Match; Banner hing unter dem Boden
  (Blender-Pivot am Stab) → +1,9 m Bracket-Höhe; deterministische Frames
  brauchen `Animator.Update(0)` nach `Play`.

## 8. Gesamtbilanz (Nacharbeit + Gesamtzeit)

| Asset | Gen-Zeit | Nacharbeit | Gesamt |
|---|---|---|---|
| Truhe | 55 s + 1391 s (GLB) | ~40 min (Decimate 699k→3k, Pivot/Maß, Re-Import-Fixes) | ~1,5 h |
| Banner | 13 s | ~1 h (Alpha-Maske, Stoffmesh, Pivot-Diagnose) | ~1,2 h |
| Hund | 14 s + 272 s (GLB) | ~3 h (Retopo 12k, FK-Rig, 2 Clips, Mimic, Take-Fix) | ~3,5 h |
| Icon | 7 s | ~15 min (Freistellen, Billboard) | ~0,5 h |

Einmal-Setup (ComfyUI-Toolchain, Template-Konvertierung, WSL-Quirks): ~5 h.
Cloud-Einsparung (Schätzung): vergleichbare Stock-/Auftrags-Assets
(Truhe/Banner/animierter Hund/Icon) ca. 150–400 €; lokale Stromkosten
vernachlässigbar (< 1 kWh GPU für 2D, ~0,3 kWh pro 3D-Lauf warm).
