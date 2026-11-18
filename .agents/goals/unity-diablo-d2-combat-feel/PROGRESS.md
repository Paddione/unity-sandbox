# PROGRESS — unity-diablo-d2-combat-feel

Prozent: 100 %

## Erledigt

- Plan gestagt, P1–P5 implementiert und gelandet (`ec7d503`, ff, gepusht).
- Alle Suiten grün: EditMode 36/36, PlayMode 27/27
  (`DiabloD2FeelTests`, `DiabloD2FeelLoopTests`), `VerifyDiabloZone` OK.
- Belege: DiabloAudio (7 Clips, sounded-Counter), Step-in 0,5 m, Knockback
  0,6/0,2 m (Pop statt Punt — Arc-Kill-Vertrag intakt), EnemyAttack
  (2,0 m / 0,4 s / 1,5 s / 1–2 Schaden), DieAndRespawn am Camp,
  DTargetBar mit 3-s-Fade, UIClick-Hooks.
- 3 Targets via Wrapper gebaut, Server deployed (Join-Smoke: connect +
  clean disconnect, 0 Exceptions, Log ruhig), Windows-Build gestaged.

- 2026-11-18: Eigner hat den Playtest als Erfolg abgeschlossen
  (Anweisung „als Erfolg abschließen") — Erfolgskriterium 5 damit erfüllt,
  keine erfundenen Feel-Details protokolliert. Stand beim Close: HEAD
  `6a33815` (zone-contracts S1+S2, Szenen-Regen mit Contract-Board;
  D2-Feel-Dateien unberührt), Unity-Tests-Run `44343327121` lief,
  Vor-HEAD `0eccf67` CI-grün.

- Follow-ups außerhalb dieses Goal-Cuts: Corpses, Vendoren/Trank/Gold,
  Damage-Numbers (explizit nicht-D2), Screenshake, Coop-Sync-Feeling,
  Idle-Anims, Gamepad.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
