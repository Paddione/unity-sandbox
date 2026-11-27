# Progress: diablo-ability-showcases

Prozent: 100 %

## Erledigt

- 2026-11-27: Goal angelegt (Worktree `feat/diablo-ability-showcases`); Eigner-Entscheide per `setgoal` übernommen (alle 3 Bosse, Intro + Phase-2, schadlose Demo-Casts + Text, 8–12 s / ca. 4 s skipbar 1× pro Run, keine neue Voice, `G-DIABLO07` erweitern); keine Überlappung mit aktiven Goals < 100 % (Akt-Vertiefungen und Voice-Goals berühren keine Cutscene-Ability-Inszenierung).
- 2026-11-27: Umsetzung — `CutsceneDirector` mit Showcase-Daten je Boss (Phase-1-/Phase-2-Ability-Keys, Phase-2-Id/Dauer 4 s, Intro 9 s), Step-Sequencer (`ShowcaseCasts`/`ShowcaseCastCount`/`CurrentAbilityKey`), schadlosen Demo-Casts (Telegraph/Vfx, Glasswright-Lanzen vom Helden weg) und `TryStartPhaseTwoShowcase` (Boss hält still, Held behält Kontrolle, Kamera bleibt); Verdrahtung in allen drei `EnterPhaseTwo`, gegated auf echte Story-Szenen (sonst leckt `HoldBosses` in Kampftests ohne tickenden Director); 10 neue Loc-Keys `cutscene.ability.*` (DE/EN).
- 2026-11-27: Belege Unity — gefilterte EditMode-Suite `CutsceneDirectorTests` 13/13 grün (5 neue Tests: Daten/Captions, Intro-Casts schadlos, Glasswright-Lanzen, Phase-2 einmalig/Kontrolle, Phase-2-Skip); volle Suiten EditMode 619/620 und PlayMode 362/363 — die zwei Rest-Fehlschläge sind Basis-Befunde, nicht diese Änderung: `LocalizationTests.NoUnlistedPlayerVisibleLiterals` ist auf `origin/main` bereits per Allowlist-Fix behoben (Rebase bei `land` übernimmt ihn), `VfxSpawnTests.Boss_TelegraphAndPhaseTwoFire` ist laut CI-Artefakt (Run 45913365512 auf `main`, EditMode dort 0 Fehlschläge) der einzige, vorbestehende PlayMode-Fehlschlag; `check-voice-lines.py` 115 Zeilen 0 Probleme, `check-meta.py` OK, `goal-lint` still.
- 2026-11-27: Dotfiles — `G-DIABLO07`-Probe in `scripts/health_probes/diablo_unity.py` um Showcase-Anker erweitert (Ability-Keys in Director + Loc-Katalog, Phase-2-Wires in allen drei Controllern, neue Test-Symbole), `scripts/test_diablo_unity_health.py` nachgezogen: 30 pytest grün, Probe gegen den Unity-Worktree `achieved` (current 0).

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
