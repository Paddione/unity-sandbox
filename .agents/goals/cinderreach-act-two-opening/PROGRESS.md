# Progress: cinderreach-act-two-opening

Prozent: 60 %

## Erledigt

- 2026-11-22: setgoal-Runde beantwortet (Akt II erste Zone, vertikaler Slice 4–5 Stufen, health_goals G-DIABLO02/06); Goal + Plan angelegt, Eigner-`go` erteilt.
- 2026-11-22: S1 (Branch `feature/cinderreach-act-two-opening-s1`, Basis `bf6d5cd`): Zone 4 Glass Barrens (+600 m X) über `BarrensBuilder` + `BarrensLayout`, Tor Kiln→Barrens (versiegelt bis Kilnmother, eigener Hinweis) + Rücktor, Wegpunkt „Glass Barrens“, `ActProgress.Barrens`/`DefeatZoneBoss`, Save v11 (`actTwoBossDefeated`, Migration öffnet Barrens für Kilnmother-Bezwinger), Licht-Grade/Ambience/Musik-Loop Zone 4, `EncounterLayout.CurrentZone` x ≥ 500. Belege: EditMode 388/388 (neu `BarrensConfigTests` 5/5, erweiterte Act/Save/Waypoint-Tests), PlayMode 273/273, `VerifyDiabloZone` (`[BarrensBuilder] Verify: OK`, Kiln-Shrines 5) grün, `check-meta` grün. Autoplay `-seed 0`: Exit 0, 8/8 Phasen, 0 Exceptions, 51 Kills.

- 2026-11-22: Eigner-Entscheid: kein 30-fps-Gate unter Linux, Performance wird auf Windows mit GPU getestet. `run-autoplay.sh` meldet das Linux-Gate nur noch (A/B `-seed 0` vorher: main 24,1 fps, S1 23,5 fps, beide unter WSLg).
- 2026-11-22: S2 (Branch `feature/cinderreach-act-two-opening-s2`, Basis `bc48d20`): Gegnerfamilie der Glass Barrens. **Shardback** ×8 (Nahkampf, 14 HP, `ShardGuard`: frontaler 110°-Bogen schluckt 75 % abgerundet, Flanke/Rücken voll, betäubt ohne Deckung, `EnemyAttack.turnRate` 2,5 statt 10, gläserne Schildplatte), **Dune Augur** ×6 (`RangedCaster`-Brain + `DuneAugur`-Payload: `ShardField` unter dem Helden, 1,0 s Warnring, 3 s Splitter, 1 Schaden/0,75 s, unblockbar, Clients sehen das Feld per `PayloadClientRpc`), **Glass Elite** ×2 (Shardback-Elite, 30 HP, 2 Schaden, 150°-Bogen). Treffer-Richtung über `EnemyHealth.TakeDamage(amount, crit, from)` + `IDamageFilter` aus Nahkampf, Skills/Projektilen und Koop-`ApplyValidated`; Ticks/Dornen/Brände ungerichtet. 3 Layout-Varianten (`LayoutSeeder` folgt dem Kiln-Zug), Arrival-Plaza wie im Kiln (`BarrensLayout.PlazaSafe`, auch für Caster), Kiln-Choke ignoriert Helden in den Barrens. Belege: EditMode 397/397 (neu `BarrensEnemyTests` 9/9), PlayMode 276/276 (neu `BarrensCombatTests` 3/3), `VerifyDiabloZone` (`shardbacks=8 augurs=6 elites=2`) grün, `check-meta` grün, Autoplay `-seed 0` Exit 0, 8/8 Phasen, 0 Exceptions, 51 Kills.
- 2026-11-22: S3 (Branch `feature/cinderreach-act-two-opening-s3`, Basis `ec14049`): Zonen-Boss **The Glasswright** (420 HP, Tier Boss, Warrior-Rig 2,2× mit Glaskrone, Nahkampf 3 über `EnemyAttack`, Aggro 11 m, Südende `BarrensLayout.GlasswrightPos` hinter den Glass Elites; Düne 4 dafür nach Nordwest). Phase 1: Splitterlanzen-Fächer (3 × 14 m × 1,2 m, 0,9 s Linien-Telegraph, 3 Schaden, richtungsabhängig blockbar) alle 3,5 s. Phase 2 ab 50 %: drei Glass Pillars (12 HP) mit `GlassWard` (gerichtete Treffer halbiert, mind. 1), Lanzen alle 2,6 s, `ShardField` unter dem Ziel alle 6 s; Kindled stattdessen Shatter Nova (1,2 s Ring, 4 Schaden außerhalb 4 m). Bossbalken-Phase, eigener Musik-Track `BossGlass` beim Engage, Zonen-Loop nach dem Kill. Kill: `ActProgress.DefeatZoneBoss` + Save, Overlay „GLASS BARRENS CLEARED“ (Press On bleibt), Koop-`NotifyZoneBossKillClientRpc` bucht und speichert auf Clients; Lanzen/Nova/Felder per ClientRpc, jeder Peer löst gegen den eigenen Helden auf (Augur-Felder treffen damit auch Clients). Belege: EditMode 405/405 (neu `GlasswrightTests` 8/8), PlayMode 278/278 (neu `GlasswrightFightTests` 2/2), `VerifyDiabloZone` (`glasswright=True`) grün, `check-meta` grün, Autoplay `-seed 0` Exit 0, 8/8 Phasen, 0 Exceptions.

## Nächste Schritte

- S4 Beute (`ItemLevelRules` Zone 4, neues Bracket, ≥ 2 Zonen-Uniques) und Koop-Paarlauf durch Tor und Boss.

## Offene Punkte

- S4–S5 offen.
- Augurs gleiten ohne Lauf-Animation (`RangedCaster` treibt `HuskAnimator` nicht); Politur bei der Windows-Sichtprüfung in S5.
