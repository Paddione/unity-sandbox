# Progress: cinderreach-hud-redesign

Prozent: 100 %

## Erledigt

- 2026-11-27: Goals angelegt (`cae286b`), Plan gestagt (`4c6ba73`, `.agents/plans/cinderreach-hud-redesign/tasks.md`, P1–P6).
- 2026-11-27/28: Umsetzung auf Branch `feature/cinderreach-hud-redesign`:
  - `35404a6` feat(hud): HUD nach Canvas-Artboard mit Cinzel/Alegreya, Gruppenrahmen und runder Minimap
    (P1 Theme: `HudChrome`, `HudLayout`, `DisplayFont`/`BodyFont`; P2 unten: Kugel 176 px, Skillplatte 604 px,
    EP-Leiste, Gold-/Menüspalte; P3 oben: Zielbalken mit Tier-Plakette, runde Minimap, Fuhrbuch-Panel;
    P4 Gruppe: `PartyFramesHud` + `heroLevel`-NetworkVariable; P5 Tests/Shots).
  - `1db3e9d` fix(hud): Lebenszahl, Leerzeichenbreite, Tier-Plakette und Begleiterzeile nach Windows-Shotcheck
    (Cinzel-Overflow, „Cinderreach Sans“ mit verbreitertem Leerzeichen + `MODIFIED-AlegreyaSans.txt`,
    unsichtbarer Elite-Renderer, Plaketten-Layout, Begleiter „St. N“).
  - `0bddacc` perf(hud): Gruppenrahmen mit 10 Hz und ohne Stencil-Masken (nach A/B-Runde 1).
- Belege (K1–K6):
  - K1: `python3 scripts/check-meta.py` OK; `HudThemeTests` grün (Fonts ≠ Builtin, Panel-Chrome, Layout-Konstanten).
  - K2: `HudLayoutPlayTests` grün (Kugel, 72-px-Slots, Flasche in der Leiste, EP, Goldkarte, Menütasten ≥ 44 px
    klickbar, Elite-Plakette, Minimap-Maske, Tracker-Panel im richtigen Quadranten, 1920×1080).
  - K3: `PartyFramesPlayTests` grün (Host + Client: Name/Klasse/Stufe, Balken folgt `hpState` nach Schaden);
    Team-Autoplay `artifacts/team-autoplay-hud/normal/team-verdict.json`: `pass:true`, 0 Exceptions,
    34/34 Quests, `actBossKills=[1:Kilnmother:-, 2:Glasswright:-, 3:Thewes:-]`, alle Clients Exit 0 (~310 s).
  - K4: `bash scripts/run-unity-tests.sh` voll grün: EditMode 762/762 + PlayMode 417/417 auf `0bddacc`
    (`docs/test-runs.jsonl`); `python3 -m pytest scripts -q` 62 passed; `KayKitAssets.Verify` Exit 0.
  - K5: Windows-Shotcheck 1920×1080: `docs/hud-redesign/hud_solo.jpg`, `hud_party.jpg`, `hud_target_elite.jpg`
    + `docs/hud-redesign/compare.jpg` (Shot ↔ Artboard „HUD im Spiel“). Hinweis: `NEW_HERO FAIL` tritt im
    Baseline-Lauf auf `origin/main` (`cae286b`) identisch auf — vorbestehend, nicht vom Branch verursacht.
  - K6: `docs/hud-redesign/perf.md`: schlechtestes Fenster Branch 67,9 fps vs. main 68,7 fps = 98,8 % (Schwelle 95 %).
- 2026-11-28: Stand in `.agents/goals/CINDERREACH.md` (HUD-Abschnitt) eingearbeitet.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
