# Goal: cinderreach-sound-audit-fix

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
health_probe: {"id":"G-DIABLO07","title":"Cinderreach audio completeness: manifest cues/tracks match code, every zone has its own theme, no silent heal/quest/shrine/block event","cmd":"python3 scripts/check-audio-manifest.py >/dev/null 2>&1; echo $?","target":0}

## Objective

Auf neuester Version (`unity-sandbox main`, Basis `c2b14bb`, vor Start per Fetch auf `origin/main`-Gleichstand geprüft) die komplette Sound-Implementierung von Cinderreach prüfen und alle Lücken schließen: Jedes spielrelevante Ereignis hat einen passenden, hörbaren Soundeffekt und jedes Gebiet ein eigenes, zum Setting passendes Hintergrundthema. Ausgangslage: [CINDERREACH.md](../CINDERREACH.md).

## Erfolgskriterien (messbar)

1. Vollinventar Ereignis → Cue → Synth-Rezept in `docs/audio/manifest.json`: Kampf/Skills aller drei Klassen, Treffer/Crit/Tod (Spieler und Gegner), Heilung, Block, Loot/Items/Schmieden, UI/Menüs, Quests/Dialoge, Schreine/Wegpunkte, Bosse, Zonen-Ambiente. Kein stilles Ereignis, kein Cue ohne Aufrufer, kein Aufruf ohne existierenden Cue (Probe `scripts/check-audio-manifest.py` grün).
2. Pro Gebiet ein eigenes loopendes Thema mit Crossfade beim Zonenwechsel: Lantern Hold, Ashen Fen, Sunken Ossuary, Cinder Kiln, Glass Barrens — plus Menü-Thema und Boss-Thema inkl. Glasswright. Jedes Thema mit eigenem Synth-Charakter (nicht nur anderer Grundton), im Manifest dokumentiert.
3. Alle neuen/ersetzten Sounds lokal synthetisiert (Manifest-Regel „no third-party assets, license: free"), Budget ≤ 60 MB, Format laut Manifest (22050 Hz mono).
4. Tests grün: bestehende EditMode/PlayMode-Suiten plus Audio-Vertrag (Cue-Existenz, Zonen-Musik-Mapping, Heal/Quest/Shrine/Block-Counter) und `G-DIABLO07`-Probe ohne Fehlschlag.
5. Eigner-Hörabnahme auf dem Windows-Build (Passung zum Setting je Zone/Cue) als letzter Schritt vor 100 % in `PROGRESS.md` belegt. Bis dahin bleibt das Goal < 100 % mit diesem konkreten Blocker unter „Offene Punkte“.

## Scope

- `sandbox/Assets/Scripts/DiabloAudio.cs`, `sandbox/Assets/Scripts/Audio/MusicDirector.cs`, Aufrufer in Skills/Story/Zones/Block, `docs/audio/manifest.json`, zugehörige EditMode/PlayMode-Tests, `scripts/check-audio-manifest.py`.
- Szenenänderungen nur per Builder, nie von Hand (in diesem Goal voraussichtlich keine nötig).

## Nicht-Ziele

- Keine Kauf-, Asset-Store-, Fremd-IP- oder KI-generierten Assets.
- Kein Umbau von Netz/Save/Quest-Systemen; interne `Diablo*`-Bezeichner bleiben.
- Kein Ersatz des Audio-Systems durch ein neues Framework; kein AudioMixer-Asset (bestehender Gain-Layer über `DiabloSettings` bleibt).

## Abhängigkeiten

- Unity 6000.0.85f2 (`sandbox/ProjectSettings/ProjectVersion.txt`), Testlauf `bash scripts/run-unity-tests.sh`, Metadaten `python3 scripts/check-meta.py`.
- Health-Vertrag Dotfiles: `G-DIABLO01/02/03/06` in `scripts/health_probes/diablo_unity.py`; neue Probe `G-DIABLO07` läuft repo-lokal über `scripts/check-audio-manifest.py`.
