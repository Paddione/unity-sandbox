# Progress: cinderreach-encounter-roles

Prozent: 0 %

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

## Startbedingung: Blocker (notiert, nicht blockierend)

- Gefordert: "Warden- und Kiln-Einstiegsfixes auf main". Befund 2026-11-18:
  `git merge-base --is-ancestor f6f26b5 HEAD` → NOT-IN-HEAD; die Fixes
  (`fix(fights): warden leash, hostile enemy projectiles, clear Kiln arrival`)
  liegen nur auf Branch `fix/warden-leash-hostile-projectiles`, nicht auf main (`3e25496`).
  Konkret fehlend auf main: Warden-Leash (14 m/10 m), hostile Projektile (`DiabloProjectile`,
  ausser Write-Set), Kiln-Ankunft in der NO-Ecke (`KilnLayout.KilnArrival`, ausser Write-Set).
- Umgang: S1 trotzdem implementiert. Leash + Deckungscheck sind in `RangedCaster` im Write-Set
  nachgebaut (gleiche Konstanten); `KilnLayout.KilnArrival` unangetastet (HEAD-Tests pinnen
  `gate.destination == KilnArrival`), Trennbarkeit daher per Gruppen-Gating (Choke + `BackGroup`),
  nicht per Distanz. Echter Fix-Branch muss zuerst landen (fremde Session).

## Nächste Schritte

- EditMode/PlayMode-Suiten grün, Szene regenerieren + `VerifyDiabloZone` grün.
- Autoplay `cinderreach-encounter-roles-normal -seed 0` (8/8, FPS-Gate), manueller Spieltest-Beleg.
- Danach S3 bzw. Prozente nach Beleglage setzen (keine 100 % ohne S3).

## Offene Punkte

- Umsetzung und Abnahme stehen aus (Suiten/Autoplay/Spieltest laufen noch).
- Windows-Spieltest Deckung/Trennung/Priorität: nur manuell möglich, noch unbelegt.
