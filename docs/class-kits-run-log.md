# Klassen-Kits: Laufprotokoll (cinderreach-class-kits-parity)

Stand 2026-11-27, Branch `feat/class-kits-parity` auf `5978149`.

## Suiten

- EditMode volle Suite 627/628, PlayMode 359/362 im ersten Volllauf. Die 4 Fehler stammten aus der Änderung
  (Save-Rückerstattung im Roundtrip-Test, Netz-Enumtest bis 11, konkave Gear-Outline, Bulwark-Text ohne „Bedrohung“)
  und sind behoben; Nachlauf der betroffenen Klassen EditMode 51/51, PlayMode 19/19.
- Nach dem Moorgänger-Balancing (Moorsturm 4, Sumpfgrund 2/Tick, Moorhaken 5 s): Kit- und Party-Suiten
  EditMode 42/42, PlayMode 16/16.
- `check-meta` OK, `check-audio-manifest` OK (43 Cues), `pytest scripts` 18/18, Quest-Coverage 0 offen,
  `agent-workspace.py check` still.

## Autoplay (Linux-Player, xvfb, `-seed 0`)

WSLg lieferte an diesem Abend keinen GLX-Kontext (`X_GLXCreateContext BadValue`), daher xvfb. Laufzeiten auf
xvfb (~2 fps, CPU) sind renderbegrenzt und kein Balance-Maß.

| Klasse | Exit | Phasen | Kills | Exceptions | Combos |
|---|---|---|---|---|---|
| Moorgänger | 0 | 12, complete | 69 | 0 | Moorfessel 13, Zerschmettern 13, Vergeltung 11 |
| Schmuggler | 0 | 12, complete | 66 | 0 | Giftbrand 12, Hinterhalt 11, Zündschuss 10, Kehlenschnitt 8 |
| Laternenträgerin | 0 | 12, complete | 68 | 0 | (keine Kit-Combos, Referenz) |

## Windows (GPU, 1920×1080)

- Shotcheck Exit 0, 0 Exceptions. Neue Posen: `47_fenwalker_mire_ground`, `48_fenwalker_hook_slam`,
  `49_fenwalker_shatter_whirl`, `50_fenwalker_shatter_frontal` (beide Spalten-Varianten),
  `51_rogue_venom_smoke`, `52_rogue_powder_keg`, `53_rogue_fan_spark_shot`, `54_rogue_single_shot`
  (beide Schnellschuss-Varianten), `55_rogue_shadow_roll`. HUD zeigt je 6 Slots mit Icons.
  Shotcheck-Log: Moorfessel ausgelöst (Fass-Combos zünden erst nach der Lunte, nach dem Log-Zeitpunkt).
- Autoplay `--autoplay`: fps-gate PASS, schlechtestes Fenster 63,0 fps (Schwelle 60), Mittel ~225 fps.
