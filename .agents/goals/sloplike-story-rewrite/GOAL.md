# Goal: sloplike-story-rewrite

health_goals: none (Inhaltsumbau, belegt durch Story- und Autoplay-Tests; kein Dauerzustand)

## Objective
Die Kampagne erzählt die Sloplike-Geschichte: Musk, Altman, Amodei und Huang haben mit Geld und Einfluss aus dem KI-Rennen je ein Reich errichtet und halten die Massen mit Italian Brainrot ruhig. Ein ganz normaler Junge oder ein normales Mädchen sieht als Einzige(r) die Wahrheit und schwört, zum Release eine 5070 Ti zu kaufen und der Welt die Wahrheit über KI und Superintelligenz zu zeigen. Am Ende von Akt II enthüllt sich: AI war „Actually Indians“. In Akt III überzeugt Amodei den Protagonisten, die Welt hätte statt AI die SI (Superintelligenz) jagen sollen.

## Ist-Befunde
- B1 Story-SSOT `docs/story-bible.md`, Quests in `sandbox/Assets/Scripts/Story/StoryModel.cs`, Texte DE/EN in `LocCatalog.Story.cs`, Katalog `docs/quest-inventory.json`.
- B2 Akt-Bosse heute: Akt I Kilnmother (H4), Akt II Glasswright (H8), Akt III Thewes (H12); `ActBossGate` sperrt Bosse bis zur Quest-Pflichtmenge.
- B3 Boss-Karikaturen: Goal `cinderreach-hero-boss-redesign` (A1 Musk, A2 Altman, A3 Amodei, A4 Huang).
- B4 Vertonung über `scripts/voice/` (`voice_lines.py`, `render_voices.py`, `qa_transcribe.py`).

## Entscheidungen
- D1 (Eigner 2026-11-28) Akt I Musk, Akt II Altman, Akt III Amodei, Akt IV Huang; Wendungen wie im Objective; Ton: Memekultur feiern, die Jagd nach Superintelligenz als sinnlos zeigen.
- D2 Satire-Leitplanke (Eigenentscheidung 2026-11-28): Die Pointen „Actually Indians“/„Still Indians“ zielen auf Firmen, die menschliche Klickarbeit als KI verkaufen; die Arbeiter erscheinen als normale, sympathische Leute, keine Akzent-Witze, keine ethnischen Stereotype. Reale Personen nur als erkennbare Parodie, keine erfundenen Tatsachenbehauptungen; erkennbare Abbilder realer Personen sind bei öffentlicher oder kommerzieller Verbreitung rechtlich heikel (Persönlichkeitsrecht).
- D3 (Eigenentscheidung) Quest-Mechanik, Zonen und Save-Struktur bleiben; umgeschrieben werden Namen, Dialoge, Questtexte, Story-Bibel und Boss-Bezüge. Die Schlusspointe „Still Indians“ gehört zu `sloplike-act-four`.

## Erfolgskriterien
1. Story-Bibel beschreibt den neuen Bogen Akt I–III inkl. beider Wendungen — Nachweis: `docs/story-bible.md` Diff, Eigner-Abnahme der Texte.
2. Quest- und Dialogtexte DE/EN tragen die neue Geschichte — Nachweis: EditMode-Tests `StoryModel`/`LocCatalog` grün, keine fehlenden Schlüssel.
3. „Actually Indians“-Enthüllung spielt am Ende von Akt II, Amodeis SI-Wende in Akt III — Nachweis: PlayMode-Test der Story-Flags, Autoplay `-seed 0` Exit 0 bis `complete`.
4. Neue Zeilen sind vertont — Nachweis: Voice-Render ohne Fehler, `qa_transcribe.py` ohne Abweichungen über Schwelle.

## Nicht-Ziele
- Intro-Cutscene (`sloplike-intro-cutscene`), Akt IV (`sloplike-act-four`), Klassen-Evolution (`sloplike-protagonist-evolution`).

## Abhängigkeiten
- `cinderreach-hero-boss-redesign` (Boss-Identitäten, Voicelines).
