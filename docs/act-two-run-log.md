# Akt II, erste Zone — Laufprotokoll (cinderreach-act-two-opening)

Stand 2026-11-22. Glass Barrens (Zone 4, +600 m X) mit Shardbacks, Dune Augurs, Glass Elites und dem
Zonen-Boss The Glasswright. Route jetzt `town, zone1, gate, zone2, boss, zone3, actboss, zone4, boss3, complete`.

## Autoplay-Matrix (Linux-Player, WSLg, `-autoplay-god`, S5-Endstand)

Je Lauf: `AUTOPLAY_SKIP_BUILD=1 bash scripts/run-autoplay.sh <out> -seed N -class <id> [-difficulty kindled]`.

| Klasse | Stufe | Seed 0 | Seed 1 | Seed 2 |
|---|---|---|---|---|
| Fenwalker | Normal | 10/10, 220 s | 10/10, 218 s | 10/10, 222 s |
| Fenwalker | Kindled | 10/10, 292 s | 10/10, 304 s | 10/10, 294 s |
| Lanternbearer | Normal | 10/10, 261 s | 10/10, 259 s | 10/10, 256 s |
| Lanternbearer | Kindled | 10/10, 366 s | 10/10, 355 s | 10/10, 375 s |
| Rogue | Normal | 10/10, 220 s | 10/10, 172 s | 10/10, 180 s |
| Rogue | Kindled | 10/10, 273 s | 10/10, 268 s | 10/10, 266 s |

Alle 18 Läufe Exit 0, 0 Exceptions. Typischer Boss-Log: `phase boss3: kills=1 phaseTwo=1 lances=21 fields=4
zoneBoss=True drop=shardback-aegis overlay=True`; Kindled feuert die Shatter Nova statt der Felder.

Erster Matrix-Durchlauf (vor dem Guard-Tuning): 14/18. Zwei Lanternbearer-Kindled-Läufe blieben in den
Barrens hängen, weil der Glasschild 1-Schaden-Treffer auf 0 rundete (Kindled-Shardbacks mit 25 HP waren von
vorn unverwundbar), ein Rogue-Lauf lief ins Barrens-Budget, ein Rogue-Lauf verfehlte die Fen-Phase. Fix:
`ShardGuard` 60 % Reduktion, gerundet, mindestens 1 Schaden; Barrens-Budget 260 s; Census-Log bei nicht
geräumten Zonen.

## Koop-Paarlauf

`AUTOPLAY_SKIP_BUILD=1 bash scripts/run-coop-pair.sh <out>` (Host + Client lokal, direkt):

- Host: Exit 0, 10/10 Phasen, 225 s, 0 Exceptions.
- Client: Exit 0, Phasen `town, zone1, boss3`, 216 s, 0 Exceptions. Der Client folgt den Tor-Broadcasts bis in
  die Barrens (`hero at (601.50, 0.08, 13.00)`) und bucht den Glasswright-Kill des Hosts:
  `[Net] zone boss kill from host: booked=True defeated=True.`
- Neu im Client-Vertrag: `boss3` (`AutoplayResult.ClientPhases`). Der Client holt dabei die Akt-Kette nach
  (Kiln offen, Akt I abgeschlossen), die Clients bisher nie mitgebucht haben.

## Windows (GPU, Perf-Nachweis)

`STAGE_DEST=…/Games/cinderreach-wip`, `SHOTCHECK_SIZE=1920x1080`:

- `shotcheck-windows.sh --autoplay`: volle Route 10/10, 301 s, 0 Exceptions, 67 Kills.
- `fps-gate.py --min-fps 60 --resolution 1920x1080`: 59 Fenster, schlechtestes 131,5 fps — PASS.
- Shots: `41_barrens_arrival`, `42_shardback_pack` (Augur-Splitterfeld), `43_glasswright_phase2`
  (Lanzenfächer, Pfeiler, Bossbalken Phase II), `44_barrens_cleared`.

## Suiten und Verifies (S5-Endstand)

EditMode 412/412, PlayMode 278/278, `SandboxBuilder.VerifyDiabloZone` grün (`[BarrensBuilder] Verify:
… shardbacks=8 augurs=6 elites=2 glasswright=True`), `check-meta` grün.
