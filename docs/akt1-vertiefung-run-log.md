# Akt-I-Vertiefung — Run-Log (S1–S4)

Ziel: Akt I 8 → 16 Einträge (H1–H5/N1–N3 + H14–H17/N8–N11), mind. 2 Umgebungs-/Fundstück-Rätsel (R1/R2) +
1 Dialog-/Entscheidungsrätsel (R3), jede Nebenaufgabe mit `x|nK`-Folgezeile, Anker H2-Tor/H4-Abschluss/H5 erhalten.

## S1 — Bibel + Modell (`6646cff`)

- Story-Bibel §Akt I auf 16 Einträge erweitert (H14–H17, N8–N11, Rätsel R1–R3, Folgezeilen-Mapping N8→H2/N9→H3/N10→H4/N11→H5).
- `StoryModel.cs` +8 Quests / +6 Fundstück-Ids (Anker H2/H4/H5 unangetastet, `h10–h13`/`n6–n7` für Akt III freigehalten).
- Titel/Zusammenfassungen/Ziele + Fundstücknamen in `LocCatalog.Story.cs`.
- Tests: `Akt1ModellTests` (neu, 12 Fälle), +10 Fälle `QuestRuleTests`, +8 Journeys `QuestJourneyTests`.

## S2 — Moor/Beinhaus (`16833a6`)

- R1/R2 als Fundstück-Rätsel: 7 neue `StoryPickup`s (`TownBuilder.SpawnStoryPickups`, Moor paarweise ≥3,6 m entzerrt),
  per `BuildDiabloZone` eingebacken (12 Pickups in Szene, Coverage 0).
- 15 neue Skripte + 2 Folgezeilen (`h2.offer.5` O|n8, `h3.offer.8` O|n9); 28 Zeilen de/en in `LocCatalog.Dialog.cs`
  + 28 gerenderte Voice-Clips (`check-voice-lines` 143/0).
- `Akt1MoorTests` (neu, 2 PlayMode-Reisen über echte Trigger), +3 Fälle `QuestRuleTests`,
  `GroupQuestMapTests` auf 8-zeiliges `h3.offer` nachgezogen.
- State/Director-Prüfung: keine Erweiterung nötig (Collect/Kill/Talk, `SyncDoneFlags`, Catch-up tragen die Schritte).
- Belege: EditMode 673/673, PlayMode 379/379, `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK.
- Nebenbefund: `VfxSpawnTests.Boss_TelegraphAndPhaseTwoFire` war auf alter Base rot (fehlender main-Fix `a4d3dc3`), nach Rebase grün.

## S3 — Ofen/Wacht (`3a8245b`)

- H16/H17-Dialoge, Jorin-/Ilsabe-Entscheidungen, H5-Finale mit N10/N11-Folgen (`h4.done` O|n10, `h5.offer` O|n11,
  `h5.done` O|n11 / O|!n11); R3 (Rußbuch/Siegel, Fuhrliste + Rußseite als neue Pickups via `TownBuilder` +5, Szene rebuildet).
- Akt-I-Zurufe in `StoryBarks.cs` (`lancer.fall`/`thrall.fall` u. a.); 37 Zeilen de/en + 37 Voice-Clips
  (CPU-Render, `check-voice-lines` 180/0; Voice vorgezogen, S4-Voice wird Verify-only).
- `Akt1OfenTests` (neu, 4 PlayMode-Fälle: R3-Auflösung, Entscheidungen, H5-Finale, N10/N11-Sichtbarkeit).
- Belege: EditMode 673/673, PlayMode 383/383, `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK,
  Autoplay `-seed 0` Exit 0, 0 Exceptions, 12/12 Phasen bis `complete` (Phase `actboss` enthalten).
- Triage „Held läuft gegen die Wand“: kosmetisches Steuerverhalten (Town-Waypoints, keine Stuck-Zeilen),
  S3-Szene betrifft nur Kiln-Pickups (x≈403–410), Town-Geometrie unverändert.

## S4 — Abnahme (dieser Commit)

- Save v15 → v16 (`V15ToV16`): neue Schritte brauchen keine gespeicherten Felder (generische
  `storyQuests`/`storyFlags`-Listen + akt-getriebener Catch-up in `StoryState.Refresh`); Migration sichert
  nur nicht-nulle Listen und hebt die Version.
- Tests: `CurrentVersion_IsSixteen`, `V15_KeepsStoryProgressAndOpensNewSteps` (Alt-Fortschritt bleibt,
  h14–h17/n8–n11 starten locked), Koop-Max-Merge (`StoryState.Merge` trägt Host-Snapshot auf frischen Client,
  kein Regress); `GearSlotsTests`-Versionsassert auf `CurrentVersion` generalisiert.
- Folgezeilen-Abgleich Bibel ↔ Code geprüft: N8→H2-Angebot, N9→H3-Angebot, N10→H4-Abschluss, N11→H5-Angebot —
  in `StoryDialogs.cs` exakt so verdrahtet (`|n8`, `|n9`, `|n10`, `|n11`/`|!n11`), keine Bibel-Änderung nötig.
- Belege: EditMode 674/674, PlayMode 383/383, `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK,
  Autoplay `-seed 0` Exit 0, 0 Exceptions, 12/12 Phasen bis `complete` (Phase `actboss` enthalten),
  `check-voice-lines` 180/0. (Linux-fps-gate FAIL wie immer advisory; Perf-Gate ist der Windows-Shotcheck.)
