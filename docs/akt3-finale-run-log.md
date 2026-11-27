# Akt-III-Finale — Laufprotokoll (cinderreach-akt3-finale S4)

Stand 2026-11-27 (S4-Final, Worktree `cinderreach-akt3-finale-s4`, Commit-Basis S3c
`9865355` = H11-Kanten-Hook mit Thewes-Respawn + Item-Namen auf S3b-Szene `468e35b`).
Kette H9→H10→H11→H12→H13 + N6/N7 (Bibel §Akt III „Was nach Hause will"), 34 Katalog-Quests.

## S4-Final (Linux-Player, WSLg, `-autoplay -autoplay-god -seed 0`)

`bash scripts/run-autoplay.sh artifacts/autoplay-s4.log -seed 0` (Neubau, Ergebnis-Commit
`a0c74d5` + S3c `9865355` per Rebase im Branch):

- Player-Exit **0**, `duration_s` 284,4 s, `exceptions` 0, `huskKills` 70, `navPlans` 126, `navWarps` 2.
- `result_phases`: `town,zone1,gate,zone2,boss,zone3,actboss,zone4,boss3,complete,quests` —
  `complete` erreicht (boss3: kills=1, Phase 2, 13 Lanzen, 5 gewardete Felder), danach
  `quests`-Phase: **34/34, `open=[]`, `caughtUp=[]`** → `ComputeExit` 0.
- Sweep-Beleg (Player-Log): N6/N7-Angebote + H10-Talks `walked=True`, `TurnIn h10`;
  4× `lanzenspitze` + 3× `glutspur` + N7-`spendenseite` `taken=True`; `hunt Thewes:
  killed=True` (Auftritt via S3c-Hook nach H11); `Talk h13`, `TurnIn n6`, `TurnIn n7`.
- fps-gate **PASS** (worst avg 31,7 fps vs. 30,0 bei 56 Fenstern, 1280×720, RTX 5070 Ti D3D12).
- Blocker aus dem Rerun (Thewes-Vorkill → H12 unerfüllbar) ist durch S3c aufgelöst:
  H11-Kante spawnt Thewes neu (voll HP, Boss-Tier), H12-Kill bucht, H13-Entscheidung läuft.

## S4-Rerun (historisch: 32/34, Thewes-Vorkill)

`bash scripts/run-autoplay.sh artifacts/autoplay-s4.log -seed 0` (Neubau, Commit `d4183bf`):

- Player-Exit 1, `duration_s` 261,6 s, `exceptions` 0, `huskKills` 70, `navPlans` 134, `navWarps` 3.
- `result_phases`: `town,zone1,gate,zone2,boss,zone3,actboss,zone4,boss3,complete` —
  die `complete`-Phase ist erreicht (inkl. `boss3: kills=1 phaseTwo=1 lances=13
  novas=0 fields=3 warded=5 zoneBoss=True drop=glasswright-prism overlay=True`).
- Quests: `32/34`, `open=[h12,h13]`, `caughtUp=[]` → `quests`-Phase fehlt, `ComputeExit` 1.
- Fortschritt ggü. S4-Erstlauf (29/34, offen n6/n7/h11/h12/h13): H11-Rätsel löst aus
  (3× `glutspur`-Finds am Moortor, `h11` autoAccept/turnIn-frei → Done ohne Talk),
  N6 (4× `lanzenspitze`) und N7 (2. `spendenseite`) via Sweep + TurnIn sauber.
  H9-Talks, H10-Angebot/Talks, N6-/N7-Angebote: alle `walked=True`.
- Nebenbefunde: fps-gate FAIL (worst 29,1 fps vs. 30,0) — advisory (Eigner-Entscheid:
  Performance wird per Windows-GPU-Shotcheck belegt), kein Gate. Neu: `[Loc] missing
  key 'story.item.lanzenspitze'` + `'story.item.glutspur'` im Player-Log
  (`LocCatalog.Story.cs` kennt nur `story.item.spendenseite`) — Anzeige-Keys fehlen,
  kein Quest-Blocker.

## Blocker: Thewes fällt vor H12 — Kill zählt nicht, kein Respawn

H12 (`Kill(Thewes,1)`, `prereq: h11`, `autoAccept: true`, `turnIn: false`) bleibt offen,
H13 hängt an H12. Ablauf aus `artifacts/autoplay-s4.log/autoplay-player.log`:

- Zeilen ~1235–1349: Die Gruppe bekämpft Thewes (420 HP) während des Akt-II-Barrens-Sweeps
  (Sehglas-/Speichenrad-Funde) und tötet ihn (`Thewes died (kills=76)`) — zu diesem
  Zeitpunkt sind nicht einmal die H9-Talks geführt (Zeile ~1530), H10-TurnIn erst
  Zeile 1544. Der Kill wird auf keine Quest gebucht (H12 weder angeboten noch aktiv).
- Nach `TurnIn h10` bietet niemand H11 per Talk an (per Design: `autoAccept`), die Finds
  lösen H11 direkt ein; H12 wird danach aktiv — sein Ziel ist bereits tot, und der
  S3b-Spawner (`BarrensBuilder.SpawnMoortor`, einmalig nach Glasswright-Muster) lässt
  Thewes nicht wiedererscheinen. H12 ist in einem einzigen Autoplay-Pass unerfüllbar.
- Kein Autoplay-/Sweep-Fix möglich: Retro-Buchung (`BookKill` nachträglich) oder Respawn
  wären Code-/Szene-Änderungen und liegen außerhalb des S4-Write-Sets (Code/Szene/Tests
  sind read-only). Folge-Arbeit (eigener Branch): z. B. Thewes erst mit H12-Aktivierung
  aggressiv/spawnbar machen, Kill-Rückwirkung gelten lassen oder Einmal-Respawn —
  Design-Entscheid des Story-Eigners, nicht S4.

## Vorlauf: S4-Erstlauf (Basis ohne Moortor-Präsenz, historisch)

Player-Exit 1, `duration_s` 213 s, `exceptions` 0, `huskKills` 69, `bossKills` 1:
`complete` erreicht, Quests `29/34`, `open=[n6,n7,h11,h12,h13]`. Ursache damals: keine
physische Moortor-Präsenz in `DiabloZone.unity` (kein Tor-Objekt, keine Finds, kein
Thewes-Gegner; einzige Spendenseite verbrauchte N15 vor N7). S3b (`468e35b`) hat das
behoben (Tore, 8 Finds, Thewes-Spawner, Presence-Journey) — der Rerun oben belegt den
neuen Stand (S3b-Belege: EditMode 716/716, PlayMode 409/409, `VerifyDiabloZone` OK,
`KayKitAssets.Verify` OK, `check-meta` OK, Coverage 0 uncovered, `check-voice-lines`
244/0, workspace-check still).

## Save/Koopf-Befunde aus S3 (für die Abnahme vorgemerkt)

- Save: `Save_Roundtrip_FinaleBleibtBestehen` — nach `BookDecision(true)` Roundtrip
  über `SaveNow`/`TryLoad`: h10–h13 Done, `MoortorOpen`, `FinalBossDefeated`,
  `Decision=Shatter`, Save-Version 16 (keine Migration).
- Koop: `Koop_Snapshot_TraegtFinaleSchritte` — Host-Snapshot Max-Merge
  (P2-S3-Präzedenz, kein neuer Netzwerkcode): Client sieht h11/h12 Done, h13 Active.
- S3-Suiten (Branch-Beleg): EditMode 716/716, PlayMode 408/408,
  `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK, `check-meta` OK,
  `check-quest-coverage` 0 uncovered, `check-voice-lines` 244/0.

## Voice-Stand S4 (Rerun-bestätigt)

`check-voice-lines`: 244 Zeilen, 0 Probleme (Worktree-Stand, 2026-11-27).
`dlg.h11–h13` existieren nicht und werden NICHT erfunden — Glättung macht P6 S2
nach dem S4-Drop (Handoff). `docs/audio/voice-lines.lock.json` unverändert.
