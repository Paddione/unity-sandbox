# Asset-Pipeline-Baseline (Unity 6000.0.85f2)

Ziel: Jede visuelle oder Audio-Änderung landet reproduzierbar im Build, ohne Reimport-Regression.

## 1. Auf Import Pipeline v2 bauen

Asset Import Pipeline v2 ist Default (seit 2019.3): umgeschriebene Asset-Datenbank,
schneller Plattformwechsel, skalierbare Imports, robustes Dependency-Tracking.
Konsequenz: Import-Regeln, Caching und CI auf v2-Verhalten auslegen, keine v1-Workarounds.

## 2. Keine Komplettpakete importieren

Unity öffnet das Import-Fenster mit allen Items selektiert — das zieht Demo-Szenen,
Varianten, Hi-Res-Texturen, Doku und Beispielcode mit. Richtwert: 497 MB Download
vs. 33,7 MB für den tatsächlich gebrauchten Prefab (Meshes + Materialien + Texturen).

Prozess:

1. Nur Prefab + Dependencies importieren.
2. Regelmäßig `python3 scripts/asset-bloat-audit.py` laufen lassen (Budgets: 500 MB total,
   100 MB pro Datei, ThirdParty-Anteil < 60 %).
3. Unused-Reste löschen: Jede Datei unter `Assets` wird auf frischen Clones in den
   `Library`-Cache re-importiert (Import-/Clone-/Disk-Kosten), auch wenn Unity
   Ungenutztes beim Build stript — außer Ordner `Resources`, das immer mit ausliefert.

## 3. Erst messen: Editor.log-Aggregat

Fühlt sich die Iteration langsam an, zuerst den Import messen:

```sh
python3 scripts/parse-editor-import-log.py ~/Library/Logs/Unity/Editor.log
# oder:  python3 scripts/parse-editor-import-log.py /tmp/Editor.log
```

`Start Importing ... / Done Importing ...`-Zeilen liefern Pfad + Dauer; gruppiert nach
Extension sieht man, ob Texturen, Models oder Prefabs dominieren (Unity-Beispiel-Ranking
ist nur ein Startpunkt — das eigene Log ist maßgeblich). Langsamste Treffer anfassen,
nicht für den Build gedachte Texturen entfernen.

## 4. Regeln in Code erzwingen

`sandbox/Assets/Editor/AssetPipelineImportRules.cs` (AssetPostprocessor) fixiert beim Import:

- Audio (Ordner `Assets` / `Resources` / `Voice` und `Music`): mono, Vorbis, 0.6 —
  passend zu `docs/audio/manifest.json` (22050 Hz, mono, oggQuality 0.6, Budget ≤ 60 MB).
  SFX brauchen keine Dateien (`DiabloAudio` synthetisiert zur Laufzeit).
- UI-Icons (Ordner `Resources` (`MenuArt`, `SkillArt`)): Sprite/Single, Alpha, kein Mipmap
  (lädt per `Resources.Load<Sprite>`; Batch-Reparatur: `MenuArtImporter.ImportAsSprites`).
- Neues Art (Ordner `Assets` / `Art`): sRGB, max 2048, `ImportStandard`-Materialien (URP Lit).
- `KayKit`-Bestand unter `Assets` / `ThirdParty` ist ausgenommen — dort gilt `KayKitAssets`.
