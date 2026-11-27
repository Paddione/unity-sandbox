# Sprache/Voice Run-Log — P6 S3 Abnahme (Ketten-Head, Basis S2 `d3d522f`)

Stand: 2026-11-27, S3-Abnahmelauf (Uhrzeiten UTC+1), Worktree `cinderreach-sprache-voice-s3` (repo unity-sandbox,
branch `cinderreach-sprache-voice-s3`, Basis `origin/main` inkl. `d3d522f`).
S3 fasst keinen Code an (read-only: alles unter `sandbox/`, `docs/sprache-inventur.md`,
`docs/story-bible.md`, `.agents/plans/cinderreach-sprache-voice/tasks.md`);
Write-Set nur diese Datei + `docs/dev-journal.md` (Anhang) + PROGRESS.

## 1. S2-Glättungsumfang (aus S2-Commits `60b8650` + `d3d522f`, hier nur verifiziert)

- Text (`60b8650` style(text)): 13 Zeilen nach Bibel-§Ton-Fortschreibung Regeln 1/3/4
  + Rechtschreibung, Glossar + Stimmen + Anrede unberührt, keine Key-/Speaker-Änderung:
  `dlg.h14.offer.3` ohne Ottilie-Ironie („Es werden mehr“), `dlg.h3.talk.jorin.1` ohne
  Kontraktion, `dlg.h4.done.2` „sehe“, `dlg.n4.offer.1`/`dlg.n13.offer.1`/`dlg.h15.ottilie.2`
  glatt (darauf/niemand/nicht lesbar), `dlg.n8.offer.2` konkret, `dlg.h9.merle.4`/`.7` +
  `dlg.n12.done.2` + `dlg.h10.merle.2` ohne obendrauf/mit drin, `dlg.h10.offer.1` +
  `quest.h11.summary` Satzstellung entzerrt; en-Fallbacks sinngetreu mitgezogen
  (h14.offer.3, jorin.1, n8.offer.2, h10.offer.1).
- Voice (`d3d522f` chore(voice)): exakt die 12 geänderten dlg-Clips CPU-neugerendert
  (Qwen3-TTS, OMP 6; GPU/5070Ti in WSL blockiert — cuInit→100, kein Versuch),
  `docs/audio/voice-lines.lock.json` aktualisiert.
- Tests: `sandbox/Assets/Tests/EditMode/SpracheTests.cs` neu (11 Fälle: Glättung,
  Glossar, Anrede, Zahlen, Struktur).

## 2. QA-Belege (S3-Rerun,PENDING — Zahlen folgen)

- `python3 scripts/check-voice-lines.py`: `voice lines: 244, problems: 0` — S3-Rerun grün, Stand bestätigt.
- `scripts/voice/qa_transcribe.py` (venv `~/models/voice/stt-server/.venv`,
  `--only` die 12 S2-Clips, `--json /tmp/opencode/qa-s3-sample.json`): S3-Stichprobe grün —
  `clips 12, mean CER 0.013, above 0.25: 0` (Einzelwerte: jorin.1 0,00, h4.done.2 0,00,
  h9.merle.4 0,02, h9.merle.7 0,01, n4.offer.1 0,06, h14.offer.3 0,00, h15.ottilie.2 0,02,
  n8.offer.2 0,00, n12.done.2 0,00, n13.offer.1 0,00, h10.offer.1 0,02, h10.merle.2 0,02).
  Exakt der S2-Beleg (mean CER 0,013). Hinweis: paralleler Voll-Transkriptionslauf über alle
  244 Clips lief zum Abnahmezeitpunkt noch — Stichprobe ist der S3-Gate-Beleg.
  Nachtrag (Vollauf nach Landung eingelaufen, S3-Worker-Beleg): `clips 244,
  mean CER 0.021, above 0.25: 3` — Ausreißer `dlg.h16.done.3` (1,065),
  `dlg.h5.done.6` (0,338), `dlg.n10.offer.2` (0,292), alle außerhalb des
  S2-Glättungsumfangs (Bestand-Notiz, kein Gate-Verstoß; Gate bleibt die grüne Stichprobe).
- `python3 scripts/check-audio-manifest.py`: `check-audio-manifest: OK (43 cues, 8 tracks)` — S3-Rerun grün.
- `python3 scripts/check-meta.py`: `check-meta: OK` — S3-Rerun grün.
- `python3 scripts/check-quest-coverage.py`: `quest coverage: uncovered entries: 0` — S3-Rerun grün.
- `bash scripts/run-unity-tests.sh` (voll, Unity 6000.0.85f2): EditMode **727/727 passed**
  (35,2 s), PlayMode **410/410 passed** (172,1 s), 0 failed/0 skipped — S3-Rerun grün,
  Erwartung (727/410) exakt getroffen. Save-State-Hygiene: `~/.config/unity3d/*/cinderreach-save.json`
  vor Lauf gelöscht (bekannte Flakiness `GameSettingsTests.MainMenu_NewContinueValidation`,
  S1+S2-Präzedenz) — kein Flake aufgetreten, kein Code-Fix.
- Verifies via `bash scripts/run-unity-build.sh`: `SandboxBuilder.VerifyDiabloZone` →
  `run-unity-build: OK (SandboxBuilder.VerifyDiabloZone)` (Marker `VerifyDiabloZone: OK`);
  `KayKitAssets.Verify` → `run-unity-build: OK (KayKitAssets.Verify)` — beide S3-Rerun grün.
- `python3 ~/scripts/agent-workspace.py check`: Exit 0 (still) — S3-Rerun grün.

## 3. Regler-/Koop-Regression (kein neuer Code — Bestandspfad, Suite deckt)

- Voice-Regler: `DiabloSettings.VoiceVol` (default 0,9) + `EffectiveVoice = MasterVol × VoiceVol`
  (`sandbox/Assets/Scripts/DiabloSettings.cs`); UI-Slider in `DiabloUIFactory` (Voice-Row,
  live `sVoice.value`-Sync + Prozent-Label) und `MainMenu` (`settings.voice` = „Stimmen“/„Voices“,
  `GameSettings.voice` → `Apply()`); Persistenz via PlayerPrefs (`Prefix + "Voice"`).
  Smoke: Suite grün (§2) — `StoryVoiceTests` (Clip spielt, `played`-Zähler, EN stumm),
  `AudioRoutingTests` (Gain-Layer), `GameSettingsTests` (kein Flake nach Save-Hygiene),
  Koop-Tests (`CoopGateTests`, `CoopPartyTests`, `CoopSaveJoinTests`,
  `GroupQuestMapTests` inkl. Bark-Relay, `GroupQuestMapPlayTests` inkl.
  `hostRelay.BroadcastBark`) alle in 727/410 enthalten und bestanden.
  KEIN dedizierter VoiceVol-Test in `sandbox/Assets/Tests` (grep `VoiceVol`/`settings.voice`:
  nur Produktionscode) — als Lücke benannt statt neu geschrieben (Tests nicht im Write-Set).
  Indirekt decken `StoryVoiceTests.Play_GermanCountsClip_EnglishStaysSilent` (Clip spielt,
  `played`-Zähler) und `AudioRoutingTests.SfxGain_MatchesSettingsLayer` (Gain = Master × SFX)
  den Pfad.
- Koop (Voice-/Text-Pfad Host-Relay): `StoryBarks.Relay` (Host-Relay, Client stumm),
  `StoryDialogShare` (gleicher Clip zeilenweise auf allen Screens); Regressionstests im Bestand:
  `GroupQuestMapTests.Barks_LocalSayRelaysRemoteSayDoesNot`, `GroupQuestMapPlayTests`
  (`hostRelay.BroadcastBark("abbot.phase2")`), `CoopGateTests`, `CoopPartyTests`,
  `CoopSaveJoinTests` — alle in der Vollsuite (§2) enthalten. S2 hat keine dieser Pfade
  angefasst (nur dlg-Texte + Clips), daher reine No-Break-Regression.

## 4. Lücke (aus S2 dokumentiert, nichts erfunden)

- `dlg.h11–h13` + `bark.thewes.*` fehlen im Bestand — kein Render, kein QA, keine Tests dafür.
  An S4-Nachfolge gemeldet; PROGRESS 100 % bezieht sich auf den Bestand (244 Zeilen, s. §2).

## 5. Ergebnis

GRÜN — alle S3-Gates bestanden: Suite 727/727 + 410/410, `check-voice-lines` 244/0,
QA-Stichprobe 12 Clips mean CER 0,013 (0 über 0,25), `check-audio-manifest` OK,
`check-meta` OK, Coverage 0 uncovered, beide Verifies OK, workspace-check still.
Regler-/Koop-Regression über Bestandssuite gedeckt (dedizierter VoiceVol-Test fehlt —
Lücke benannt, s. §3). Bestands-Lücke `dlg.h11–h13`/`bark.thewes.*` bleibt an S4-Nachfolge
gemeldet. Abnahme erteilt, PROGRESS → 100 %.
