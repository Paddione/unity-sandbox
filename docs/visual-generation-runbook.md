# Visual-Generierung: In-Editor-Loop (Unity Assistant Generator)

Stand: gegen Unity 6000.0.85f2 und Assistant-Generator-Doku `2.12.0-pre.2` / `2.1.0-pre.1`
geprüft (Pre-Release — vor Standardisierung die installierte Assistant-Version checken).
Gilt für Sprites, Texture2D und PBR-Materialien aus Text-Prompts + Referenzbildern.

## Workflow (rosionssicher durch Import-Regeln)

1. **Draft per Prompt:** Kurzbeschreibung + Stil-Anker (z. B. bestehende Ordner `Art` / `Zones`-Textur nennen).
2. **Stil fixieren:** 1–3 Referenzbilder aus dem Projekt (z. B. Ordner `Art` / `LocalArtPilot`, Ordner `Resources` / `MenuArt`)
   als Variation-Input; verhindert Stil-Drift über Iterationen.
3. **Material/Lighting im Generator:** Auflösung, Output-Count, Material-/Lichtdetails direkt im
   Generator-Fenster einstellen — nicht extern nachbearbeiten und re-importieren.
4. **Nur Chosen-Preview upscalen:** Erst das gewählte Preview produktionsreif hochskalieren,
   dann als Datei unter Ordner `Assets` / `Art` (Texturen/Models) bzw. Ordner `Assets` / `Resources` (`MenuArt`, `SkillArt`)
   (Icons) ablegen. Der Import-Postprocessor (`AssetPipelineImportRules`) fixiert Sprite- vs.
   Textur-Settings automatisch; `KayKitAssets`-Packs (Ordner `ThirdParty`) bleiben unberührt.

## Ablage-Konvention

- Texturen/Models: `Assets/Art/<Zone|LocalArtPilot>/...` (+ `.meta` einchecken — `scripts/check-meta.py` ist fail-closed).
- Icons: `Assets/AddressableAssets/MenuArt|SkillArt/*.png` als Sprite/Single ohne Mipmap (lädt per
  `Resources.Load<Sprite>`; Drift repariert `MenuArtImporter.ImportAsSprites`).
- Größen: Texturen ≤ 2048 (Postprocessor cappt), keine Hi-Res-Varianten für Doku/Demo einchecken
  (siehe `scripts/asset-bloat-audit.py`).

## Nicht-Ziele

- Kein Ersatz für KayKit-Packs oder handgebaute Zonen-Models — Generator nur für Varianten/Iterationen.
- Keine externen Round-Trips mit manuellem Reimport; was den Editor verlässt, kommt als neue
  Datei mit Review zurück.
