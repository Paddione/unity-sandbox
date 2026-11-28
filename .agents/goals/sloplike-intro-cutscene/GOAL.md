# Goal: sloplike-intro-cutscene

health_goals: none (einmalige Inhaltsarbeit, belegt durch PlayMode-Test und Shots)

## Objective
Beim Start einer neuen Kampagne läuft eine Intro-Cutscene: Die vier Overlords sichern sich mit KI-Geld je ein Reich, die Massen hängen an Bildschirmen voller Italian Brainrot, nur der Protagonist sieht die Wahrheit und schwört, zum Release eine 5070 Ti zu kaufen. Die Szene ist überspringbar und in DE/EN vertont.

## Ist-Befunde
- B1 `sandbox/Assets/Scripts/Story/CutsceneDirector.cs` spielt heute Boss-Showcases und Spawn-Cutscenes (Fernschnitt, Namensbanner).
- B2 Neue Kampagne startet über `MainMenu.cs` → `ClassSelectUI.cs`.

## Entscheidungen
- D1 (Eigner 2026-11-28) Brainrot als eigene Kreaturen im Blender-Stil (absurde Tier-Objekt-Mischwesen mit Fantasienamen und Sing-Sang-Zeilen), keine Clips aus Fremdvideos (Rechte Dritter).
- D2 Satire-Leitplanke (Eigenentscheidung 2026-11-28): Die Pointen „Actually Indians“/„Still Indians“ zielen auf Firmen, die menschliche Klickarbeit als KI verkaufen; die Arbeiter erscheinen als normale, sympathische Leute, keine Akzent-Witze, keine ethnischen Stereotype. Reale Personen nur als erkennbare Parodie, keine erfundenen Tatsachenbehauptungen; erkennbare Abbilder realer Personen sind bei öffentlicher oder kommerzieller Verbreitung rechtlich heikel (Persönlichkeitsrecht).
- D3 (Eigenentscheidung) Umsetzung im vorhandenen `CutsceneDirector` mit Spielmodellen statt vorgerendertem Video; Erzählstimme über die Voice-Pipeline.

## Erfolgskriterien
1. Mindestens drei eigene Brainrot-Kreaturen modelliert und vom Eigner freigegeben — Nachweis: Blender-Renders unter `docs/sloplike/`, Eigner-Abnahme.
2. Neue Kampagne zeigt die Intro-Cutscene vor dem Spielstart, überspringbar, Fortsetzen eines Saves zeigt sie nicht — Nachweis: PlayMode-Test.
3. Erzählung DE/EN vertont — Nachweis: Voice-Render, `qa_transcribe.py` ohne Abweichungen über Schwelle.
4. Sichtbar im Spiel — Nachweis: Windows-Shots der Schlüsselszenen unter `docs/sloplike/`; Eigner-Abnahme.

## Nicht-Ziele
- Vorgerenderte Videos, Fremdclips.

## Abhängigkeiten
- `cinderreach-hero-boss-redesign` (Menschformen), `sloplike-story-rewrite` (Text).
