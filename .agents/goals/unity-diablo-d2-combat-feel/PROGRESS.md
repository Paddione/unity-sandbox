# PROGRESS — unity-diablo-d2-combat-feel

Prozent: 80 %

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

## Nächste Schritte

- Windows-GPU-Playtest mit Ein-Satz-Verdikt (Erfolgskriterium 5),
  dann erst 100 %.

## Offene Punkte

- Live-Playtest steht aus (Erfolgskriterium 5); ohne ihn kein 100-%-Abschluss.
- Sound-Asset-Frage ist entschieden (synth statt Dateien) — kein offener Punkt mehr.
- Follow-ups (nicht dieser Goal-Cut): Corpses, Vendoren/Trank/Gold,
  Damage-Numbers (explizit nicht-D2), Screenshake, Coop-Sync-Feeling,
  Idle-Anims, Gamepad.
