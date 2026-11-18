# Progress: cinderreach-build-uniques

Prozent: 100 %

## Erledigt

- Ziel und Plan angelegt.
- S1 Loot/Anzeige/Spielstand (Übernahme durch opencode-Session ses_e115042b; Rebase auf Finale-main 79a5153):
  - `Items/ItemDatabase.cs`: `lancer-ward` (HeavyAxe/Fenwalker) + `wick-of-the-halo` (EmberCenser/Lanternbearer) in `CreateUnique`, `UniqueWeaponForClass()` (50/50 Build- vs. Legacy-Uniques pro Klasse, klassenkorrekt).
  - `Loot/DroptableDatabase.cs`: `BuildUniqueWeaponShare = 0.5` dokumentiert; reguläre Unique-Gewichte unverändert. `Loot/LootDrop.cs`: Waffen-Unique-Zweig routet über die Klassenwahl.
  - Tooltip-Effekttext aus der Definition (`DiabloInventoryUI`). Tests: `Tests/EditMode/BuildUniqueLootTests.cs` (Klassenfilter, 50/50-Auswahl, Drop-Routing, JSON-/Stash-Roundtrip, Schmiede-Regel, Tooltip, Dropchancen).
- S2 Effekte (selber Branch):
  - Neu: `Items/BuildUniqueEffects.cs` (EffectText, HasLancerWard/HasWickOfHalo, TryInterruptCharge max 1/Cast → Exhausted, Halo-Fenster NotifyHalo/HaloActiveAt/ResetRun, PreferHaloTarget), `Tests/PlayMode/BuildUniqueEffectTests.cs` (max-1-Interrupt, ohne Unique kein Interrupt, Erholung, Ablegen, Halo-Prio, ohne Halo/Laterne nächste, Ablauf, Reload).
  - Anschlüsse `DiabloSkills` (DoCleave-Interrupt variantenunabhängig, DoHalo-Notify + Ward-Spawning kombiniert — Doppel-Spawning aus Merge behoben), `AshLancer.InterruptToExhausted`, Wisp-Zielwahl (Halo-Prio über vorgefilterten Kandidaten, Reichweite/Sicht unverändert).
- S3 Integration/Nachweis:
  - `bash scripts/run-unity-tests.sh` Exit 0: EditMode 290/290, PlayMode 145/145 (`/tmp/bu-full.log` + `/tmp/bu-play.log`; Halo-Doppelspawn aus Rebase-Merge gefunden+behoben).
  - `SandboxBuilder.VerifyDiabloZone` Exit 0 (`/tmp/bu-verify.log`; keine Szenenänderung nötig).
  - `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-build-uniques-normal" -seed 0` Exit 0: Vollpass (9 Phasen) + fps-gate PASS (worst avg 37,2 ≥ 30,0; `/tmp/bu-autoplay3.log`). Erster Versuch: Player-Build starb im Unity-Asset-Importer mit SIGFPE (Infra-Flake, Retry grün); zweiter Versuch 19,6 fps bei Load 8,67 + fremden Editoren (Umgebung, kein Code-Effekt — dritter Versuch 37,2 auf identischem Build).
  - Klassenvergleich je Klasse mit/ohne Waffe: `BuildUniqueEffectTests` (PlayMode, grün — beide Skill-Varianten, Ablegen beendet Effekt, Halo-Ablauf, Reload erhält Effekt).
  - `python3 scripts/check-meta.py` Exit 0, `agent-workspace check` still vor Commit.

## Nächste Schritte

Keine — alle drei Stufen belegt und gelandet.

## Offene Punkte

Keine.
