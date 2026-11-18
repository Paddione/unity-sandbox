# Progress: cinderreach-coop-session

Prozent: 40 %

## Erledigt

- 2026-11-17: Goal angelegt (Folge-Goal nach `arpg-session-loop`).
- 2026-11-17: Plan `.agents/plans/cinderreach-coop-session/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: S1 join landed. `-host [-port N]` / `-join ip[:port]` (`Net/JoinArgs.cs`, P2-Flags bleiben gültig),
  Co-op-Fenster `JoinPanel` (F7, Button in der Klassenwahl), `-save-file` für getrennte Saves, `CoopAutoplay`
  (Client folgt dem Host, greift per ServerRpc an, Ergebnis mit `role`), `scripts/run-coop-pair.sh`.
  Belege: EditMode 210/210, PlayMode 84/84, `VerifyDiabloZone`, `check-meta`; Paarlauf Exit 0: Client
  `town,zone1` 0 Exceptions (104 s), Host 8/8 Phasen 0 Exceptions (179 s); Solo-Autoplay `-seed 0` 8/8, 36,8 fps.

- 2026-11-18: S2 enemies landed. Host-autoritative Treffer: `Net/HitValidator.cs` (Reichweite +
  Toleranz, Cooldown, Ziel lebt, Schaden 1..50; `NearestLivingPlayer`), `EnemyHealth` als
  `NetworkBehaviour` mit `NetHP` (Server schreibt, Clients spiegeln; ungespawnt lokal wie zuvor),
  `EnemyAttack`-Zielwahl nächste lebende Heldin, `BossController.Phase` (1/2, Server schreibt) +
  Summons per `NetworkObject.Spawn` + Slam-/Phase-Telegraph per ClientRpc, `RangedCaster`-Telegraph
  per ClientRpc + nächste lebende Heldin, `DiabloNetworkPlayer`-ServerRpc mit HitValidator-Gate
  (Melee 4 m/0,5 s, Ranged 24 m/0,3 s) + PlayMode `HostEnemyTests` (Validator-Ablehnungen, Server-Treffer
  senkt HP, Boss-Phase summont einmal, Nearest-Living). `Sandbox.Tests.PlayMode` um
  `Unity.Netcode.Runtime`-Referenz ergänzt (NetworkBehaviour-Basis).
  Belege: EditMode 257/257, PlayMode 113/113, `VerifyDiabloZone` OK (husks=48), `KayKitAssets.Verify`
  grün, `check-meta` OK; Paarlauf Exit 0: Host 8/8 Phasen 0 Exceptions (181 s), Client `town,zone1`
  0 Exceptions hits=232 (108 s); Solo-Autoplay Exit 0, 0 Exceptions, FPS-Pass 34,6 (Schwelle 30).

## Nächste Schritte

1. S3 world: Zonen-Gate für alle, Client-Leichen.

## Offene Punkte

- Kriterien 1–7 offen bis S2–S6.
- Befund außerhalb des Goals: Solo-Autoplay ohne `-seed` (Seed aus vorhandenem Save, 1112106611, Fen-Variante
  `husks=1 elite=0 brute=2 props=2`) blieb nach 11 Kills bis Budgetende ohne Treffer hängen. Der Nightly testet nur
  Seeds 0–2 und diese Husk-Variante nie.
