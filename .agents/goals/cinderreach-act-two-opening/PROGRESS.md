# Progress: cinderreach-act-two-opening

Prozent: 20 %

## Erledigt

- 2026-11-22: setgoal-Runde beantwortet (Akt II erste Zone, vertikaler Slice 4–5 Stufen, health_goals G-DIABLO02/06); Goal + Plan angelegt, Eigner-`go` erteilt.
- 2026-11-22: S1 (Branch `feature/cinderreach-act-two-opening-s1`, Basis `bf6d5cd`): Zone 4 Glass Barrens (+600 m X) über `BarrensBuilder` + `BarrensLayout`, Tor Kiln→Barrens (versiegelt bis Kilnmother, eigener Hinweis) + Rücktor, Wegpunkt „Glass Barrens“, `ActProgress.Barrens`/`DefeatZoneBoss`, Save v11 (`actTwoBossDefeated`, Migration öffnet Barrens für Kilnmother-Bezwinger), Licht-Grade/Ambience/Musik-Loop Zone 4, `EncounterLayout.CurrentZone` x ≥ 500. Belege: EditMode 388/388 (neu `BarrensConfigTests` 5/5, erweiterte Act/Save/Waypoint-Tests), PlayMode 273/273, `VerifyDiabloZone` (`[BarrensBuilder] Verify: OK`, Kiln-Shrines 5) grün, `check-meta` grün. Autoplay `-seed 0`: Exit 0, 8/8 Phasen, 0 Exceptions, 51 Kills.

- 2026-11-22: Eigner-Entscheid: kein 30-fps-Gate unter Linux, Performance wird auf Windows mit GPU getestet. `run-autoplay.sh` meldet das Linux-Gate nur noch (A/B `-seed 0` vorher: main 24,1 fps, S1 23,5 fps, beide unter WSLg).

## Nächste Schritte

- S2 Gegnerfamilie (Schildträger, Flächenbeschwörer, Elite), Encounter-Layout mit 3 Varianten, Arrival-Plaza.

## Offene Punkte

- S2–S5 offen.
