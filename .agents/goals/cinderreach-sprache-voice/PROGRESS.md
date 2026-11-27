# Progress: cinderreach-sprache-voice

Prozent: 33 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (Entscheid: verständlich-modern als eigenes Goal, Glossar + Ton bleiben).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-sprache-voice/tasks.md` geschrieben.
- 2026-11-26: Status geprüft (main `5978149`): weiter 0 % — keine S-Stufe gelandet, kein `feature/cinderreach-sprache-voice-s<N>`-Branch. Kein `sprache-inventur.md`, keine `SpracheTests.cs`, kein `sprache-voice-run-log.md`; Zeilenstand weiter ca. 115+ Bestand (deutsch, 8 Stimmen, `check-voice-lines.py` als Gate). Plan-Basis (`73ca3e2`) überholt. Reihenfolge gilt gestaffelt: S2 erst nach Textständen aus Akt-I-S1, Akt-II-S1, Akt-III-S1 — alle drei stehen noch aus, daher S2 weiter blockiert.

- 2026-11-27 (S1, Worktree `feature/cinderreach-sprache-voice-s1`): Ton-Fortschreibung in `docs/story-bible.md` (§Ton-Fortschreibung, 8 Glättungsregeln + Vorher/Nachher-Beispiele; Glossar + Ton unberührt, Akt-Tabellen unberührt) und Voll-Inventur `docs/sprache-inventur.md` neu: 238 Textzeilen (115 vertont = 107 dlg + 8 bark, 123 Story-Katalog inkl. 28 quest.* + 6 story.item.* aus `6646cff`), 115/115 Clips, `check-voice-lines.py`: 115 lines, 0 problems (nur gelesen). Offen: Akt-II-S1 + Akt-III-S1-Textstände sowie N8–N11-Folgezeilen (keine `|n8`…`|n11`-Flags) — alles S2 nach Staffelung.

- 2026-11-27: Plan auf `1ff1b95` rebased und als P6 priorisiert: Akt-I-S1-Textstand steht (`6646cff`), Akt-II-S1 und Akt-III-S1 fehlen weiter — S1 (Regeln+Inventur) startbar, S2 bleibt gestaffelt blockiert; Zeilen-/Clip-Stand (115) auf aktuellem HEAD bestätigt.

- 2026-11-27 (S1, Worktree `feature/cinderreach-sprache-voice-s1`, gelandet als `a5087c8` auf `main` — ff, Push durch, Worktree aufgeräumt): s. Eintrag oben (Ton-Fortschreibung + 238-Zeilen-Inventur). Prozent jetzt 33 %.

## Nächste Schritte

- S2: Text-Glättung (Loc + Dialogs/Barks) + Voice-Render + QA — nach den Quest-S1-Textständen (Akt-I-S1+S2 stehen mit `6646cff`/`16833a6`, Akt-II-S1 und Akt-III-S1 weiter offen).
- S3: Abnahme (Suiten, check-voice-lines, Doku).

## Offene Punkte

- S2 läuft erst nach Akt-II-S1- und Akt-III-S1-Textständen; Akt-I-Seite (inkl. S2-Dialoge) ist inventarisiert.
