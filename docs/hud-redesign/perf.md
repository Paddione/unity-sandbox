# HUD-Redesign: Windows-Performance (cinderreach-hud-redesign, Kriterium 6)

Messung: `bash scripts/shotcheck-windows.sh --autoplay` mit `SHOTCHECK_SIZE=1920x1080` auf dem WIP-Stage
(`Games\cinderreach-wip`), Windows-GPU, danach `python3 scripts/fps-gate.py <log> --min-fps 60 --resolution 1920x1080 --skip-first 1`.
Der Autoplay im Windows-Modus nimmt keinen Seed an; Läufe derselben Codebasis streuen deshalb stark.

## Rauschen

Zwei Läufe desselben `main` (`cae286b`) weichen je Phase um 10–20 % voneinander ab
(z. B. `gate_FenGate` 98,2 → 78,4 fps, `combat_kiln` 139,5 → 112,3 fps). Gültig ist daher nur der Vergleich
eines direkt nacheinander gemessenen Paars.

## Paar 2026-11-28 (Branch `0bddacc` direkt vor `main` `cae286b`)

| Wert | main | Branch | Branch/main |
|---|---|---|---|
| fps-gate schlechtestes Fenster | 68,7 fps | 67,9 fps | 0,99 |
| fps-gate | PASS | PASS | |
| Median aller Fenster | 166,1 fps | 190,8 fps | 1,15 |

Phasen-Mediane (gleiche Phasen beider Läufe):

| Phase | main | Branch | Branch/main |
|---|---|---|---|
| town_waypoints | 72,9 | 70,7 | 0,97 |
| flask | 71,1 | 69,8 | 0,98 |
| combat_fen | 74,0 | 75,4 | 1,02 |
| gate_FenGate | 78,4 | 80,7 | 1,03 |
| combat_ossuary | 85,7 | 84,3 | 0,98 |
| boss | 93,7 | 98,4 | 1,05 |
| gate_KilnGate | 99,5 | 104,7 | 1,05 |
| combat_kiln | 112,3 | 120,9 | 1,08 |

Ergebnis: schlechtestes Fenster des Branches 98,8 % von `main` (Schwelle 95 %), erfüllt.

Vorlauf (Paar 1, vor `0bddacc`): Branch-Phasen 8–16 % unter `main`, aber der `main`-Lauf brach früh ab
(`boss4: FAIL Thewes not found`) und nahm andere Wege (navWarps 55 statt 13). Daraufhin wurden die
Gruppenrahmen auf 10 Hz gedrosselt und ihre Stencil-Masken entfernt (`0bddacc`), dann Paar 2 gemessen.
