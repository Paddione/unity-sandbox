# Akt-III-Finale — Laufprotokoll (cinderreach-akt3-finale S4)

Stand 2026-11-27 (S4, Worktree `cinderreach-akt3-finale-s4`, Basis `eb1c8e2` = S3).
Kette H9→H10→H11→H12→H13 + N6/N7 (Bibel §Akt III „Was nach Hause will"), 34 Katalog-Quests.

## S4-Abnahmelauf (Linux-Player, WSLg, `-autoplay -autoplay-god -seed 0`)

`bash scripts/run-autoplay.sh artifacts/autoplay-s4.log -seed 0` (Neubau, kein Skip):

- Player-Exit 1, `duration_s` 213 s, `exceptions` 0, `huskKills` 69, `bossKills` 1.
- `result_phases`: `town,zone1,gate,zone2,boss,zone3,actboss,zone4,boss3,complete` —
  die `complete`-Phase ist erreicht (inkl. `boss3: kills=1 phaseTwo=1 lances=14
  novas=0 fields=3 warded=4 zoneBoss=True drop=shardback-aegis overlay=True`).
- Quests: `29/34`, `open=[n6,n7,h11,h12,h13]`, `caughtUp=[]` → `quests`-Phase fehlt,
  `ComputeExit` 1. Ablauf bis `TurnIn h10` sauber (H9-Talks, H10-Angebot/Talks,
  N6-/N7-Angebote), danach findet der Sweep keine Arbeit mehr.
- Nebenbefund: fps-gate FAIL (worst 29,7 fps vs. 30,0) — advisory (Eigner-Entscheid:
  Performance wird per Windows-GPU-Shotcheck belegt), kein Gate.

## Blocker: Moortor hat keine physische Präsenz in der Szene

Die 5 offenen Quests brauchen Inhalte, die in `DiabloZone.unity` nicht existieren
(grep-Beleg, je 0 Treffer im Szenenfile):

- H11 (Rätsel R7): 3× `glutspur`-Finds am Moortor — kein `StoryPickup_glutspur_*`.
- H12: Killziel `Thewes` — kein Thewes-Gegner (nur Kill-String + Director-Logik).
- H13: hängt an H12 (Talk Ottilie nach Thewes' Fall).
- N6: 4× `lanzenspitze` — keine Quelle (kein Pickup, kein Drop).
- N7: 1× `spendenseite` — das einzige Exemplar (`StoryPickup_spendenseite_0`)
  verbraucht N15, bevor N7 angeboten wird (kein Respawn: `StoryPickup` despawnt
  erst, wenn ein aktiver Task zählt).

S3 (`eb1c8e2`) lieferte Modell/Kette (`h10→h11→h12→h13`, N6/N7 ab H10),
Director-Finale (`BookFinalBossPhase` genau 1×, `BookDecision` genau 1×),
Moortor-Gate-*Logik* (`ZoneGate.IsMoortorGate`, `Locked` via `Finale.MoortorOpen`),
Save ohne Migration (`RestoreFinale`, v16) und 10 PlayMode-Reisen — aber kein
Tor-Objekt, keine Finds, keinen Boss-Spawn (S3-Stat: keine Szene/Builder-Datei).
`SessionAutoplay` kann die Moortor-Kette daher nicht spielen: `QuestSweep` sucht
Finds nur in der aktuellen Zone, `ZoneWithWork` reist nur per Wegpunkt
(Town/Ossuary/Kiln/Barrens) — das Moortor ist über keinen Pfad erreichbar.
Direktes `BookCollect`/`BookKill` aus Autoplay-Code wäre fabrizierter Nachweis,
kein Beweis, und bleibt deshalb verboten.

Folge-Arbeit (außerhalb S4-Write-Set, braucht Szene/Builder-Zugriff):
`MoortorGate` + `MoortorReturnGate` platzieren (Tor-Seam wie Fen-/Kiln-/BarrensGate),
3 Glutspur-Pickups + 4 Lanzenspitzen-Quellen + 2. Spendenseite (N7 nach N15) legen,
Thewes-Gegner spawnen (Hüllen-Gefolge, Bossbalken/Phase-2-Muster Bestand).
Erst danach kann S4 `34/34, 0 Catch-up, Exit 0` belegen.

## Save/Koop-Befunde aus S3 (für die Abnahme vorgemerkt)

- Save: `Save_Roundtrip_FinaleBleibtBestehen` — nach `BookDecision(true)` Roundtrip
  über `SaveNow`/`TryLoad`: h10–h13 Done, `MoortorOpen`, `FinalBossDefeated`,
  `Decision=Shatter`, Save-Version 16 (keine Migration).
- Koop: `Koop_Snapshot_TraegtFinaleSchritte` — Host-Snapshot Max-Merge
  (P2-S3-Präzedenz, kein neuer Netzwerkcode): Client sieht h11/h12 Done, h13 Active.
- S3-Suiten (Branch-Beleg): EditMode 716/716, PlayMode 408/408,
  `VerifyDiabloZone` OK, `KayKitAssets.Verify` OK, `check-meta` OK,
  `check-quest-coverage` 0 uncovered, `check-voice-lines` 244/0.

## Voice-Stand S4

`check-voice-lines`: 244 Zeilen, 0 Probleme. `dlg.h11–h13` existieren nicht und
werden NICHT erfunden — Glättung macht P6 S2 nach dem S4-Drop (Handoff).
`docs/audio/voice-lines.lock.json` unverändert.
