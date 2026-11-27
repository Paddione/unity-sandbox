# Audio-Generierung: Tweaken + lokal generieren

Zwei Loops, je nach Änderungsart: **Audity** für nicht-destruktives Tweaken bestehender Sounds,
**lokale KI** (MOSS / ACE-Step / VoxCPM2) für neue SFX, Musik und Stimmen. Finish immer gleich:
trimmen, loopen, als Einzeldatei importieren — mit erzwungenen Import-Settings.

## 4a. Tweaken ohne den Editor zu verlassen (Audity)

Node-Graph-Audio-Middleware: Sound aus Sources, Effekten, Modulation und Logik auf einem Canvas
bauen und hören, ohne den Editor zu verlassen. Graph ist ein Projekt-Asset (liegt neben der Szene,
wird mit versioniert), läuft im eigenen Fenster auch ohne Play-Mode, Änderungen klingen sofort —
in Audition wie im Play-Mode. Einsatz: Effektketten, Modulation, Logik an bestehenden Sounds ändern,
statt Dateien neu zu generieren.

## 4b. Generieren, dann lokal finishen (MOSS / ACE-Step / VoxCPM2)

Bewährtes Muster für alle drei Audio-Klassen:

- **SFX/Ambience:** MOSS SoundEffect generieren.
- **Musik:** ACE-Step (Instrumente, Tempi, Stile).
- **Stimmen:** VoxCPM2 via VoiceStudio — Textbeschreibung → 8 NPC-Auditions → 1 gewählte Stimme
  als Referenz für weitere Dialoge.

Finish-Schritte (aus der Referenz-Implementierung übernommen):

1. Späte Impacts trimmen, Abschnitte per Blend in Loops überführen.
2. Jede Dialogzeile als eigene Datei speichern → `Assets/AddressableAssets/Voice/*.ogg`
   (Per-Line-Import; `check-voice-lines.py` + `voice-lines.lock.json` bleiben maßgeblich).
3. Import-Settings kommen aus `AssetPipelineImportRules.OnPreprocessAudio` (mono, Vorbis, 0.6),
   passend zu `docs/audio/manifest.json` (22050 Hz, mono, oggQuality 0.6, Budget ≤ 60 MB);
   `scripts/check-audio-manifest.py` prüft Cue↔Manifest-Parität (`DiabloAudio.Cue`,
   `MusicDirector`-SynthLoops, ZoneTracks inkl. KilnLoop).
4. Studio-`.ogg`-Renders ersetzen Synth-Clips 1:1 per Cue-Name (Lizenz-Hinweis im Manifest beachten).

## Was nicht gilt

- SFX brauchen keine Binärdateien: `DiabloAudio` synthetisiert zur Laufzeit (22050 Hz);
  Import-Regeln greifen nur für Voice-/Musik-Dateien.
- Keine Enterprise-Cache-/CI-Tuning-Aussagen (`-consistencyCheck` o. ä.) in diesem Dokument.
