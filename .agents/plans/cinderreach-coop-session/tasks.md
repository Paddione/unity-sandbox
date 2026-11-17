---
goal: cinderreach-coop-session
slug: cinderreach-coop-session
branch: feature/cinderreach-coop-session-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: arpg-session-loop 100 % (cinderreach-loop-guard 100 % empfohlen)
---

# Plan: cinderreach-coop-session — Co-op-Parität der Session-Schleife

Pfad: feature, 6 Stufen (S6 optional). Ausführung nach dem **Session-Protokoll** aus
`.agents/plans/arpg-session-loop/tasks.md` (Schritte 1–9), Lock-Name `task cinderreach-coop-session-sN`.
Protokollschritt 1 prüft zusätzlich: `.agents/goals/arpg-session-loop/PROGRESS.md` zeigt `Prozent: 100 %`.

## Ausgangslage (Belege, Stand 2026-11-17)

- NGO 2.5 + UTP 2.4 + Lobby 1.3 + Relay 1.2 im Manifest (decision-log Q4). Im Code gibt es **keine**
  Relay- oder Lobby-Nutzung. `MultiplayerAutoStarter.cs:63` ruft `StartHost()` direkt (UTP direkt).
- `cloudProjectId`/`organizationId` in `ProjectSettings.asset` leer: kein UGS-Projekt verknüpft, Relay
  ist ohne Eigner-Login nicht nutzbar.
- WSL läuft mit `networkingMode = Mirrored` (`.wslconfig`): Windows-Client erreicht den WSL-Host direkt
  über `127.0.0.1`/LAN-IP.
- RPC-Bestand (25 Stellen): `DiabloNetworkPlayer` 8, `LootNetRelay` 4, `DiabloSkills` 3, `LootDrop` 2,
  `EnemyHealth` 1. Muster: Owner-only-Input, validierter ServerRpc, host-autoritative Drops (decision-log Q6).
- Bewusst lokal laut decision-log: Inventar, HP, Husk-Retaliation (client-seitig), Leichen nur Host,
  Hit-Stop nur solo. Aus arpg-session-loop kommen hinzu: Gold, Vendor, Save, Punkte, `ZoneGate`,
  `BossController`, `RangedCaster` (alle solo-lokal gebaut, Entscheid 3 dort).

## Entscheide

1. Transport: **direkter UTP-Join** (IP + Port, Default 7777) ist Pflichtumfang. Relay ist Stufe S6 und
   nur dann, wenn der Eigner das UGS-Projekt verknüpft. Sonst bleibt S6 `blocked: UGS-Projekt fehlt`, und
   das Goal-Kriterium 6 gilt mit direktem Join als erfüllt. Goal-Text entsprechend angepasst.
2. Autorität: Gegner (Husks, Wardens, Boss) werden **host-autoritativ** (heute Retaliation client-seitig).
   Clients senden nur Treffer-Absichten per ServerRpc, der Host validiert Reichweite und Cooldown.
3. Pro-Spieler-Zustand (Inventar, Gold, Punkte, Save) bleibt beim Client. Der Host validiert nur
   Transaktionen, die geteilte Welt betreffen (Pickup, Vendor-Bestand, Gate).
4. Testgerüst: In-Process-Multi-Instance-Tests sind mit NGO schwer. Stattdessen ein **2-Prozess-Lauf**
   (Linux-Player-Host + Linux-Player-Client, `-coop-autoplay host|client`) als Hauptbeleg, dazu
   PlayMode-Tests für die Validierungslogik über reine C#-Validatoren.

## Partial-Manifest

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 join | Direkt-Join-UI + Startparameter, 2-Prozess-Harness | `Scripts/DiabloNetworkBootstrap.cs`, `Scripts/Net/JoinPanel.cs` (neu), `Scripts/Net/CoopAutoplay.cs` (neu), `scripts/run-coop-pair.sh` (neu), `Tests/EditMode/JoinArgsTests.cs` |
| S2 enemies | Host-autoritative Gegner + Boss-Replikation | `Scripts/EnemyHealth.cs`, `Scripts/EnemyAttack.cs`, `Scripts/Enemies/BossController.cs`, `Scripts/Enemies/RangedCaster.cs`, `Scripts/DiabloSkills.cs` (Treffer-ServerRpc), `Scripts/Net/HitValidator.cs` (neu), `Tests/PlayMode/HostEnemyTests.cs` |
| S3 world | Zonen-Gate für alle, Client-Leichen | `Scripts/Zones/ZoneGate.cs`, `Scripts/DiabloCorpse.cs`, `Scripts/Net/ZoneTravelRelay.cs` (neu), `Tests/PlayMode/CoopGateTests.cs` |
| S4 economy | Gold/Pickup/Vendor per validiertem ServerRpc | `Scripts/Loot/LootNetRelay.cs`, `Scripts/Loot/GoldDrop.cs`, `Scripts/Town/Vendor.cs`, `Scripts/Net/TradeValidator.cs` (neu), `Tests/EditMode/TradeValidatorTests.cs` |
| S5 save | Client-Save beim Join, Level/Gear übernehmen | `Scripts/DiabloNetworkPlayer.cs`, `Scripts/Save/SaveService.cs`, `Tests/PlayMode/CoopSaveJoinTests.cs`, `docs/coop-run-log.md` (neu) |
| S6 relay (optional) | Relay-Join-Code | `Scripts/Net/RelayJoin.cs` (neu), `Scripts/Net/JoinPanel.cs`, `sandbox/ProjectSettings/ProjectSettings.asset` |

Seriell S1 → S2 → S3 → S4 → S5. S6 nur nach Eigner-Freigabe (UGS-Login), dann unabhängig.

## S1 — Direkt-Join + 2-Prozess-Harness

- Startparameter `-host [-port N]`, `-join <ip[:port]>`. Ohne Parameter bleibt Solo = Host (heutiges
  Verhalten). Hauptmenü bekommt „Host“/„Join (IP)“ in einem `JoinPanel` (HD-Scaler-Konvention).
- `CoopAutoplay`: Host spielt die Autoplay-Route, Client folgt dem Host-Helden in 3 m Abstand und greift
  dessen Ziel an. Beide loggen Phasen + Exceptions in `autoplay-result.json` (Schema aus loop-guard, Feld
  `role`).
- `scripts/run-coop-pair.sh`: startet Host und Client unter Xvfb, wartet auf beide, Exit = max.
- Akzeptanz: Client verbindet, beide Logs 0 Exceptions bis Town → Zone 1 (Rest folgt in S2–S3).

## S2 — Gegner host-autoritativ

- `EnemyHealth`/`EnemyAttack`: Schaden und Zielwahl nur auf dem Server. Zielwahl nimmt den nächsten
  lebenden Spieler (heute Name `Player`, siehe dev-journal Doppelheld). HP als `NetworkVariable`.
- `HitValidator` (pure C#): Reichweite ≤ Skill-Reichweite + 0,5 m Toleranz, Cooldown, Ziel lebt.
- Boss: Phase als `NetworkVariable<int>`, Summons per Server-Spawn (NetworkObject-Prefab), Telegraphe per
  ClientRpc.
- Tests: Validator-Ablehnungen (zu weit, Cooldown, totes Ziel), PlayMode „Treffer über ServerRpc senkt
  HP“, Phasenwechsel synchron (2-Prozess-Log).

## S3 — Gate + Leichen

- `ZoneGate`: nur der Host löst aus, wenn alle lebenden Spieler im Trigger stehen oder der Host 3 s
  hält. Dann ClientRpc Fade + Teleport mit Spawn-Offsets.
- `DiabloCorpse` auf Clients aus `EnemyHealth.corpseModel` per ClientRpc beim Tod (Muster aus Feel-Polish,
  kein Klon des NetworkObjects).
- Tests: Gate-Bedingung (pure Logik), 2-Prozess-Lauf erreicht Zone 2 auf beiden Seiten.

## S4 — Wirtschaft validiert

- `TradeValidator` (pure C#): Kauf nur mit genug Gold, Bag frei, Reichweite zum Vendor ≤ 4 m. Pickup nur
  in Reichweite ≤ 2,5 m und nur einmal (Doppel-Pickup-Race).
- Gold-Drops: Host spawnt, Pickup-ServerRpc, Gutschrift per ClientRpc an den Owner.
- Vendor-Bestand pro Spieler (Seed + ClientId), keine geteilten Lagerbestände.
- Tests: gefälschter Kauf (zu wenig Gold), Doppel-Pickup, Reichweite.

## S5 — Save beim Join

- Client lädt beim Join sein lokales Save. Level, Punkte und Gear werden per ServerRpc gemeldet, Host
  übernimmt sie für Max-HP und Schaden (Vertrauensmodell: Koop unter Freunden, Grenzen klemmen: Level ≤ 30).
- Autosave des Clients bei Gate und Boss-Kill wie solo.
- 2-Prozess-Volllauf Town → Boss → Abschluss auf beiden Seiten, Log in `docs/coop-run-log.md`.
- Windows-Client gegen WSL-Host: `Games\cinderreach\cinderreach.exe -join 127.0.0.1`, Shotcheck-PNG vom
  Client. Belegt Goal-Kriterium 6 (direkter Join).
- Akzeptanz: alle 7 Goal-Kriterien belegt (6 mit direktem Join), Solo-Autoplay `G-DIABLO06` grün.

## S6 — Relay (optional)

- Voraussetzung: Eigner verknüpft das UGS-Projekt (Editor-Login, `cloudProjectId` gesetzt). Ohne das
  Status `blocked: UGS-Projekt fehlt` und nicht weiter verfolgen.
- `RelayJoin`: anonyme Auth, Allocation (max. 3 Clients), Join-Code im `JoinPanel`.

### Statustabelle

| Stufe | Abhängig von | Gewicht | Status |
|---|---|---|---|
| S1 join | Startbedingung | 15 % | offen |
| S2 enemies | S1 | 25 % | offen |
| S3 world | S2 | 15 % | offen |
| S4 economy | S3 | 20 % | offen |
| S5 save | S4 | 25 % | offen |
| S6 relay | Eigner-Freigabe | 0 % (optional) | blocked: UGS-Projekt fehlt |

## Verify (je Stufe)

1. `scripts/run-unity-tests.sh` grün.
2. `VerifyDiabloZone` grün (`repo.yaml:verify` + Builder-Verify).
3. `bash scripts/run-coop-pair.sh` Exit 0 (ab S1, Umfang wächst je Stufe).
4. `bash scripts/run-autoplay.sh` Exit 0 (Solo-Regression, aus loop-guard; fehlt es noch, den
   Solo-Player-Lauf aus arpg S6 nutzen).
5. `python3 scripts/check-meta.py` ohne Befund.

## Risiken

- R1 Host-Autorität ändert das Kampfgefühl (Latenz bei Clients). Gegenmittel: Treffer-Feedback
  (Flash, Zahl) clientseitig vorhersagen, HP-Korrektur vom Host. Solo bleibt ohne Latenz (Host).
- R2 Zielwahl über Namen `Player` bricht mit mehreren Spielern. In S2 auf Spielerliste umstellen.
- R3 Windows-Firewall blockt den Join zum WSL-Port. Gegenmittel: Mirrored-Modus nutzt localhost.
  Sonst Firewall-Regel als Eigner-Schritt dokumentieren, nicht selbst ändern.
- R4 2-Prozess-Läufe unter Xvfb sind langsam (llvmpipe). Timeouts großzügig (1200 s), Inhalt testen, nicht FPS.
