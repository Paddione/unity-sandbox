# Akt-II-Vertiefung — Run-Log (S1–S3)

Ziel: Akt II 6 → ca. 12 Einträge (H6–H9/N4–N5 + H18/H19/N12–N15), mind. 2 Umgebungsrätsel (R4/R5) +
1 Entscheidungsrätsel (R6), neue H9-`x|nK`-Varianten, Anker H6/H8/H9 erhalten.

## S1 — Bibel + Modell

- Bibel §Akt II auf 12 Einträge (H18/H19 + N12–N15, Rätsel R4–R6, Wendung 3 geschärft, Anker H6/H8/H9 unangetastet).
- `StoryModel.cs` +6 Quests / +4 Items (`h10–h13`/`n6–n7` für Akt III freigehalten), Save v16 unangetastet.
- Tests: `Akt2ModellTests` (12/12).
- Belege: EditMode 686/686, PlayMode 383/383, `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK.

## S2 — Brachen-Strang (`5fd7a8c`)

- H18/H19/N12–N15-Dialoge (29 Zeilen + H9-Folgen merle.7/ottilie.7-8) + 2 Wendung-3-Barks in
  `StoryBarks.cs`/`StoryDirector.cs` (R4/R5 beim Aufwachen), Loc-Texte de/en, 9 Brache-Pickups in
  `TownBuilder` + Szene rebuildet.
- `Akt2BrachenTests` (6 PlayMode-Reisen: H18/H19/N12/N13-N15/N14-Folge/Surfacing, 43 Schlüssel-Asserts),
  +4 Regel-Fälle in `QuestRuleTests` (H18-AutoAccept, N12-Sperre/150-Gold, R6-Pfadtrennung, N13-200-Gold),
  31 Voice-Clips gerendert.
- Belege: EditMode 690/690, PlayMode 389/389, `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK,
  `check-voice-lines` 211/0, `check-meta` OK, Coverage 0 uncovered.

## S3 — Abnahme (dieser Commit)

- Save: KEINE Migration. S2 (H18/H19/N12–N15, R4/R5, 9 Pickups) braucht keine persistierten Felder —
  neue Schritte reiten die generischen `storyQuests`/`storyFlags`-Listen und öffnen via akt-getriebenem
  Catch-up in `StoryState.Refresh` beim ersten Laden. Exakt die V15→V16-Präzedenz (Akt-I-S4, `e66c5f0`):
  dort sicherte `V15ToV16` nur nicht-nulle Listen und hob die Version; hier entfällt selbst das, weil
  v16-Listenform unverändert trägt. `SaveGame.CurrentVersion` bleibt 16, kein EditMode-Migrationstest nötig
  (kein Migrations-Code ohne Bedarf — kein toter Code).
- Koop: neue S2-Schritte (R4/R5-Pickups via `BookCollect`, N12–N15-Turn-ins via `BookTalk`) laufen über den
  generischen Buchungspfad — kein Quest-spezifischer Netzwerkcode. Clients leiten Talks per
  `SubmitStoryTalkServerRpc` an den Host (`DiabloNetworkPlayer.cs`, Ownership-unabhängig), der Host bucht via
  `BookTalkLocal` und verteilt per `BroadcastStory`/Snapshot-`Merge`; Barks (R4/R5-Aufwach-Zurufe
  `sehglas.find`/`scherben.find`) erreichen Clients über `StoryBarks.Relay` → `BroadcastBark` (Host-Relay-
  Präzedenz). Code-Verifikation, kein Lauf nötig — Pfad ist quest-agnostisch.
- Voice: verify-only — `check-voice-lines.py` → 211 Zeilen, 0 Probleme. S2 hat 31 Clips gerendert
  (Stand 211/0); neue S3-Zeilen: keine. Kein Render nötig (CPU-Pfad-Vermerk: GPU/5070Ti in WSL ist
  blockiert, cuInit→100 — kein GPU-Versuch, Blocker nur dokumentiert).
- Autoplay `bash scripts/run-autoplay.sh artifacts/autoplay-s3.log -seed 0` (Build `Succeeded in 350.6s`;
  Player-Lauf nach Harness-Timeout manuell fortgesetzt, identische Args): Exit 0, Exceptions 0,
  `duration_s=178.7`, 12 Lauf-Phasen bis `boss3` + `complete`
  (`town,zone1,gate,zone2,boss,zone3,actboss,zone4,boss3,complete`), huskKills=69, bossKills=1,
  boss3 phaseTwo=1/lances=12/fields=3/warded=3, Renderer D3D12 (RTX 5070 Ti).
  Linux-fps-gate advisory FAIL (worst 28.1 fps < 30) — wie immer advisory, Perf-Gate ist der Windows-Shotcheck.
- Suites + Verifies: EditMode 690/690, PlayMode 389/389 (beide `Passed`, 0 failed/skipped);
  `VerifyDiabloZone` OK (husks=67, netcode present), `KayKitAssets.Verify` OK
  (CharacterAnimations, 8 Modelle, URP Lit + Atlas), `check-meta` OK,
  `check-quest-coverage` 0 uncovered, `agent-workspace.py check` still.
  Unity-Churn (`ProjectSettings/TimeManager.asset`) via `git checkout --` revertiert, nicht committet.
