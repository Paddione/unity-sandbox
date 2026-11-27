# Progress: cinderreach-sprache-voice

Prozent: 100 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (Entscheid: verständlich-modern als eigenes Goal, Glossar + Ton bleiben).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Ausführungsplan `.agents/plans/cinderreach-sprache-voice/tasks.md` geschrieben.
- 2026-11-26: Status geprüft (main `5978149`): weiter 0 % — keine S-Stufe gelandet, kein `feature/cinderreach-sprache-voice-s<N>`-Branch. Kein `sprache-inventur.md`, keine "SpracheTests.cs", kein "sprache-voice-run-log.md"; Zeilenstand weiter ca. 115+ Bestand (deutsch, 8 Stimmen, `check-voice-lines.py` als Gate). Plan-Basis (`73ca3e2`) überholt. Reihenfolge gilt gestaffelt: S2 erst nach Textständen aus Akt-I-S1, Akt-II-S1, Akt-III-S1 — alle drei stehen noch aus, daher S2 weiter blockiert.

- 2026-11-27 (S1, Worktree `feature/cinderreach-sprache-voice-s1`): Ton-Fortschreibung in `docs/story-bible.md` (§Ton-Fortschreibung, 8 Glättungsregeln + Vorher/Nachher-Beispiele; Glossar + Ton unberührt, Akt-Tabellen unberührt) und Voll-Inventur `docs/sprache-inventur.md` neu: 238 Textzeilen (115 vertont = 107 dlg + 8 bark, 123 Story-Katalog inkl. 28 quest.* + 6 story.item.* aus `6646cff`), 115/115 Clips, `check-voice-lines.py`: 115 lines, 0 problems (nur gelesen). Offen: Akt-II-S1 + Akt-III-S1-Textstände sowie N8–N11-Folgezeilen (keine `|n8`…`|n11`-Flags) — alles S2 nach Staffelung.

- 2026-11-27: Plan auf `1ff1b95` rebased und als P6 priorisiert: Akt-I-S1-Textstand steht (`6646cff`), Akt-II-S1 und Akt-III-S1 fehlen weiter — S1 (Regeln+Inventur) startbar, S2 bleibt gestaffelt blockiert; Zeilen-/Clip-Stand (115) auf aktuellem HEAD bestätigt.

- 2026-11-27 (S1, Worktree `feature/cinderreach-sprache-voice-s1`, gelandet als `a5087c8` auf `main` — ff, Push durch, Worktree aufgeräumt): s. Eintrag oben (Ton-Fortschreibung + 238-Zeilen-Inventur). Prozent jetzt 33 %.

- 2026-11-27 (S2, Worktree `cinderreach-sprache-voice-s2`, Basis `892299c`): Text-Glättung gelandet-kommittet (`bd0d9f1` style(text)): 12 Zeilen nach Bibel-§Ton-Fortschreibung Regeln 1/3/4 + Rechtschreibung (Glossar + Stimmen + Anrede unberührt, keine Key-/Speaker-Änderung): h14.offer.3 ohne Ottilie-Ironie, h3.talk.jorin.1 ohne Kontraktion, h4.done.2 „sehe“, n4/n13/h15 glatt (darauf/niemand/nicht lesbar), n8.offer.2 konkret, h9.merle.4/.7 + n12.done.2 + h10.merle.2 ohne obendrauf/mit drin, h10.offer.1 + quest.h11.summary Satzstellung entzerrt; en-Fallbacks sinngetreu mitgezogen (h14.offer.3, jorin.1, n8.offer.2, h10.offer.1). `SpracheTests.cs` neu (11 Fälle: Glättung, Glossar, Anrede, Zahlen, Struktur). Voice-Re-Render CPU (Qwen3-TTS, kein GPU-Versuch — 5070Ti in WSL blockiert, dokumentiert): exakt die 12 geänderten dlg-Clips neu, `voice-lines.lock.json` aktualisiert. Gates: `check-voice-lines` 244/0, `qa_transcribe` 12 Clips mean CER 0,013 (0 über 0,25), `check-audio-manifest` OK (43 cues, 8 tracks), EditMode 727/727 + PlayMode 410/410 (ein flaky `GameSettingsTests.MainMenu_NewContinueValidation` im Vollauf durch Save-State fremder Tests, gezielter + voller Re-Run grün, keine Code-Änderung), `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK, `check-meta` OK, Quest-Coverage 0 uncovered, workspace-check still. Lücke (nicht erfunden, Bestand only): `dlg.h11–h13` + `bark.thewes.*` fehlen — an S4-Nachfolge/P6-S3 gemeldet. Prozent jetzt 66 %.


## Nächste Schritte

- S2: erledigt (s. oben) — Text-Commit + Clip-Commit getrennt.
- S3: erledigt (s. unten) — Abnahme grün, Run-Log + PROGRESS 100 %.

- 2026-11-27 (S3, Worktree `cinderreach-sprache-voice-s3`, Basis S2 `d3d522f`, kein Code angefasst):
  Abnahme GRÜN. Volle `run-unity-tests.sh` (Unity 6000.0.85f2): EditMode 727/727 + PlayMode
  410/410 (Save-State-Hygiene vorher, kein Flake, kein Code-Fix). Gates S3-Rerun:
  `check-voice-lines` 244/0, QA-Stichprobe 12 S2-Clips mean CER 0,013 (0 über 0,25),
  `check-audio-manifest` OK (43 cues, 8 tracks), `check-meta` OK, Quest-Coverage 0 uncovered,
  `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK, workspace-check still.
  Regler (`DiabloSettings.VoiceVol`/`EffectiveVoice`, Slider `DiabloUIFactory`+`MainMenu`) +
  Koop (`StoryBarks.Relay`, `StoryDialogShare`) via Bestandssuite gedeckt (StoryVoice-,
  AudioRouting-, GameSettings-, Coop-/GroupQuestMap-Tests in 727/410); kein dedizierter
  VoiceVol-Test im Bestand — als Lücke benannt, kein neuer Test (Tests nicht im Write-Set).
  Belege: `docs/sprache-voice-run-log.md` (neu). Prozent jetzt 100 %.

## Offene Punkte

- Keine (S3): `dlg.h11–h13` + `bark.thewes.*` fehlen im Bestand — als Lücke an S4-Nachfolge
  gemeldet, nicht erfunden; Abnahme bezieht sich auf den Bestand (244 Zeilen).
