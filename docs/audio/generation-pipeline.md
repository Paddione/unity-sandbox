# Audio-Generierung: Cinderreach Pipeline (GPU 0 + Addressables)

Alle lokalen Audio-Workflows laufen ausschließlich auf **GPU 0 (`RTX 3060 Ti`, 8 GB)** via
`CUDA_DEVICE_ORDER=PCI_BUS_ID`; GPU 1 (`RTX 5070 Ti`, `inkling2-mini-llamacpp.service`) bleibt unberührt.

## 1. Stimmen (Qwen3-TTS auf GPU 0)

- **Stack:** Host-PyTorch (`~/unsloth-boxes/voice-tts/.venv/bin/python`) mit `Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign`
  (Referenzstimmen unter `docs/audio/voices/<id>/ref.ogg`) und `Qwen/Qwen3-TTS-12Hz-1.7B-Base` (Clone, ~4,4 GB VRAM,
  alle 244 Dialog-/Bark-Zeilen).
- **Render-Einstieg:** `~/unsloth-boxes/voice-tts/.venv/bin/python scripts/voice/render_voices.py --device cuda:0`
  (Standard `--device auto` wählt GPU 0 bei verfügbarem CUDA, sonst CPU; `--only KEY ...` / `--force` / `--seed-offset N`).
- **Katalog-Parsing & Gates:**
  - `scripts/voice/voice_lines.py` dekodiert `\uXXXX`-Escapes aus `LocCatalog*.cs` und bindet `clone_model` in den Lock-Hash.
  - `python3 scripts/check-voice-lines.py` prüft Vollständigkeit aller 244 Clips und `docs/audio/voice-lines.lock.json`.
  - `~/models/voice/stt-server/.venv/bin/python scripts/voice/qa_transcribe.py --max-cer 0.25` misst Verständlichkeit gegen Whisper Deutsch (Abnahme aller 244 Clips `<= 0,25` CER).
- **Ablage & Import:** `sandbox/Assets/AddressableAssets/Voice/<key>.ogg` (Mono Vorbis 0.6, `CompressedInMemory`).

## 2. HQ-SFX (Unsloth Studio GPU 0 + Deterministisches Mastering)

- **Stack:** Unsloth Studio (`http://127.0.0.1:8888`) mit `audio-cpp/audio.cpp-gguf/Stable-Audio-3-Small-SFX-GGUF`
  (Q8_0 auf GPU 0, ~1,7 GB VRAM, ~4 s pro Variation) via `scripts/audio/render_sfx.py` (73 Clips: 43 Manifest-Cues
  plus 30 Oberflächen-/Varianten-Pools `Swing_0..2`, `Hit_<Surface>_0..2`, `Footstep_<Ground>_0..2`).
- **Mastering (`master_sfx`):** DC-Entfernung, Stille-Trimming (`< 5 ms` Lead, 2 ms Fade-in / 10 ms Fade-out),
  Equal-Power-Loop-Crossfade für `Ambient`, Mono-Downmix, Kategorie-RMS-Normalisierung (`ui`/`combat`/`event`/`step`/`ambient`),
  True-Peak-Decke `<= -1,0 dBFS` (0 Clipping-Samples) → 44,1 kHz 16-bit Mono PCM WAV.
- **Ablage, Gate, Lock & Parität:** `sandbox/Assets/AddressableAssets/Sfx/<key>.wav`, Lockfile `docs/audio/sfx.lock.json`,
  Vollständigkeits- und Mastering-Gate `python3 scripts/check-sfx-clips.py` (analog zu `check-voice-lines.py`),
  Manifest-Vertrag `scripts/check-audio-manifest.py` (`docs/audio/manifest.json`).

## 3. Import-Regeln, Addressables & Runtime-Fallback

- `AssetPipelineImportRules` (`1.1`) erzwingt beim Import:
  - `Assets/AddressableAssets/Sfx/`: Mono, Vorbis `0.75`, `DecompressOnLoad` für One-Shots (`CompressedInMemory` für `Ambient*`).
  - `Assets/AddressableAssets/{Voice,Music}/`: Mono, Vorbis `0.6`, `CompressedInMemory`.
- Neue Dateien über Unity `AddressablesSetup.Ensure` in `game-content` registrieren (Adressen „Sfx/<key>“, „Voice/<key>“).
- Laufzeit (`DiabloAudio`, ResolveAmbient in `sandbox/Assets/Scripts/DiabloAmbience.cs`): `Res.TryGet<AudioClip>("Sfx/...")` lädt vorhandene Clips logfrei
  (Priorität: Test-Override → Oberfläche+Variante → Oberfläche → Variante → Basis-Cue); ohne Asset greift
  nahtlos die deterministische 22050-Hz-Laufzeitsynthese.

## Verworfene / blockierte Pfade

- `unity ai` CLI (`0.3.0-beta.1`): ohne Anmeldung/Credits (`AUTH_NOT_LOGGED_IN` / `NO_SUBSCRIPTION`) nicht nutzbar.
- `MOSS-SoundEffect-v2-GGUF` in Unsloth Studio: ~10,8 GB VRAM (OOM auf GPU 0, CPU-Fallback > 3,5 min pro Clip);
  nur optional auf GPU 1 bei explizit pausiertem LLM-Dienst.
- `Qwen3-TTS-GGUF` in Unsloth Studio: ~8,2 GB VRAM (OOM auf GPU 0), daher Host-PyTorch auf GPU 0.
