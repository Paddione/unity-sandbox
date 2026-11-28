# Map-Overlay: Windows-Performance (cinderreach-map-overlay, Kriterium 6)

Messung: `bash scripts/shotcheck-windows.sh --autoplay` mit `SHOTCHECK_SIZE=1920x1080` auf dem WIP-Stage
(`Games\cinderreach-wip`), Windows-GPU. Paar-Messung derselben Binärdatei (Branch nach `128b32a`):
Lauf A ohne Karte (`map closed`), Lauf B mit dauerhaft offener Overlay-Karte (`-autoplay-map`).
Der Autoplay im Windows-Modus nimmt keinen Seed an; Läufe streuen deshalb (siehe „Rauschen“).

## Ergebnis (Paar 3, 2026-11-28, Binärdatei nach `128b32a`)

Beide Läufe liefen denselben Pfad (je `DONE phases=11 huskKills=50 bossKills=1 quests=13/34`,
Fensterzahl je Phase praktisch identisch) — ein direkt vergleichbares Paar.

| Wert | Karte zu (A) | Overlay offen (B) | B/A |
|---|---|---|---|
| fps-gate schlechtestes Fenster | 70,0 fps | 69,0 fps | **0,99** |
| fps-gate | PASS | PASS | |
| Median aller Fenster | 151,0 fps | 170,0 fps | 1,13 |

Ergebnis: schlechtestes Fenster mit offener Overlay-Karte **98,6 %** des Laufs ohne Karte
(Schwelle 95 %), erfüllt. Der Median liegt mit Karte sogar über dem ohne Karte
(Phasenrauschen; beide Läufe weit über der 60-fps-Schwelle).

Phasen-Mediane (identische Phasen, nahezu identische Fensterzahlen):

| Phase | Karte zu | Overlay offen | B/A |
|---|---|---|---|
| town_waypoints | 73,0 | 71,0 | 0,97 |
| flask | 82,0 | 76,0 | 0,93 |
| combat_fen | 80,0 | 80,0 | 1,00 |
| gate_FenGate | 94,5 | 90,0 | 0,95 |
| combat_ossuary | 103,0 | 100,0 | 0,97 |
| boss | 112,0 | 92,5 | 0,83 |
| gate_KilnGate | 131,0 | 94,0 | 0,72 |
| combat_kiln | 135,5 | 112,0 | 0,83 |
| gate_BarrensGate | 141,5 | 159,0 | 1,12 |
| combat_barrens | 137,0 | 170,0 | 1,24 |
| actboss | 174,0 | 174,0 | 1,00 |

Einzelne Phasen (boss, gate_KilnGate, combat_kiln) liegen mit Karte 17–28 % niedriger,
andere (Barrens) deutlich höher; im schlimmsten gemessenen Fenster beträgt der Abstand
1,4 % — das ist die Kriterium-Metrik (fps_windows ≥ 95 %), und sie ist erfüllt.

## Rauschen

- Vorläufer-Paar 1 (Vorbau vor `9508874`/`128b32a`): worst 58,0 (offen) vs. 63,0 (zu) = 0,92 —
  aber der offene Lauf nahm einen längeren Weg (13 Phasen, 66 Kills, actboss+boss4,
  Tracker noch sichtbar über der Karte). Kein direkt vergleichbares Paar.
- Vorläufer-Paar 2 (nach `9508874`, vor `128b32a`): 0,62/0,79 bei stark abweichenden Pfaden
  (zu: 11 Phasen/13 Quests, offen: 13 Phasen/34 Quests inkl. `complete`) — Pfad-Streuung,
  keine Aussage über die Overlay-Kosten.
- Paar 3 (finale Binärdatei, identische Pfade) ist der gültige Beleg. Die Overlay-Kosten
  wurden vorab zusätzlich gesenkt (`128b32a`): Redraw-Takt 0,3 s statt 0,15 s,
  Bild-/Quest-Text-Updates nur bei Änderung, Icon-Pool skippt unveränderte Eigenschaften.
