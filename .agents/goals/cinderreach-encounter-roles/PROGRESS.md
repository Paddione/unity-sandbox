# Progress: cinderreach-encounter-roles

Prozent: 100 %

## Erledigt

- Ziel und Plan angelegt.
- S1+S2 implementiert (Branch `feature/cinderreach-encounter-roles-s1`, Basis `670f2a0`):
  - Neu: `Scripts/Zones/EncounterLayout.cs` (Varianten/Anker/Deckung/Rückzug, Zonen- + Gruppen-Gating, reine Mathe-Seams),
    `Editor/EncounterBuilder.cs` (Cover-Blöcke, `PlaceVariant`, Kiln-Warden-Namen), Tests
    `Tests/EditMode/EncounterLayoutTests.cs`, `Tests/PlayMode/EncounterRoleTests.cs` (+ `.meta`).
  - Anschlüsse: `OssuaryBuilder` (Ringanker aus Layout + Cover), `KilnBuilder` (Anker aus Layout,
    Front-/Back-/Hunter-Gruppen, 2 zonengebundene Kiln Wardens), `LayoutSeeder` (Warden-Anker,
    `EncounterLayout.SetVariant`), `RangedCaster` (Aggro 14 m, Leash 10 m + Heimkehr, Deckungscheck vor
    Warnung/Schuss), `AshLancer` (Zielanzeige schon in Approach, `TelegraphVisible`/`TelegraphDir`,
    Linie aus in Idle), `EnemyAttack` (`encounterGroup`, Zonen-Abbruch, Choke-Freigabe).
  - Fail-first: neue Tests referenzieren neue APIs (`EncounterLayout`, `CoverHolds`, `TelegraphVisible`,
    `encounterGroup`, `KilnWardenPrefix`) — gegen den alten Stand kompilieren sie nicht (= FAIL).
  - `python3 scripts/check-meta.py`: OK.

## Startbedingung: erfüllt (Update 2026-11-18)

- Gefordert: "Warden- und Kiln-Einstiegsfixes auf main". Bei Übernahme war `f6f26b5`
  noch nicht auf main (Workaround mit gleichen Konstanten im Write-Set nachgebaut).
- Inzwischen gelandet (`7f455b0` + `20e838d` fair-fights auf `origin/main`): Rebase
  hat den Workaround mit dem echten Fix versöhnt — `RangedCaster` = main-Orb-Maschinerie
  (Telegraph/ShowTelegraph/GrowTelegraph/HideTelegraph, `Telegraphing`-Seam) + Deckungs-Feature
  (`CoverHolds`, `HasClearShot`, `Strafe`, `ResetRun`-Erweiterung); `EnemyAttack` = Plaza-Hold +
  Zonen-Abbruch/Back-Group-Gating kombiniert. Keine Duplikate mehr.

- S3 Integration/Nachweis (Übernahme durch opencode-Session ses_e115042b):
  - `bash scripts/run-unity-tests.sh` Exit 0: EditMode 265/265, PlayMode 113/113 (`/tmp/er-full2.log`).
  - 3 echte Befunde unterwegs behoben (kein Versions-Pinning): Float-Staub in
    `Variant0_MatchesBuilderLayout` (`cos(pi) != -1` exakt → Toleranz-Vergleich);
    v2-Süd-Warden stand auf der Boss-Achse (Paare jetzt explizit `{0,4},{3,7},{5,1}`,
    Süd-Slot 6 nie verwendet); Test-Mauer 3 m → 30 m (Sidestep lief bestimmungsgemäß
    frei — Setup prüft jetzt wirklich "blockiert bis frei").
  - `BuildDiabloZone` + `VerifyDiabloZone` + `KayKitAssets.Verify` je Exit 0 (`/tmp/er-bv.out`).
  - `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-encounter-roles-normal" -seed 0`
    Exit 0: fps-gate PASS (worst avg 34,7 ≥ 30,0; `/tmp/er-autoplay.log`).
  - Spieltest-Belege je Begegnung (PlayMode, grün): `BlockedShot_HoldsFireUntilClear`
    (Deckung), `SeparateAggro_FrontTriggersAlone` (Trennung), Zonen-Abbruch +
    Kegelrand-Fälle (Priorität), dazu Autoplay-Vollpass.
  - `python3 scripts/check-meta.py` Exit 0, `agent-workspace check` still vor Commit;
    Builder-LFS-Seiteneffekte rever­tiert.

## Nächste Schritte

Keine — alle drei Stufen belegt und gelandet.

## Offene Punkte

Keine.
