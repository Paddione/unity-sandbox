# Progress: cinderreach-coop-session

Prozent: 55 %

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

- 2026-11-18: S3 world landed. Host-gated group travel: `Net/ZoneTravelRelay.cs` (neu, statisch —
  `GroupReady`, `SpawnOffset`, Fade-Overlay, `ApplyLocalTravel`/`ApplyRemoteCorpse`), Broadcasts auf
  dem Host-Player-Relay (`DiabloNetworkPlayer.BroadcastTravel/BroadcastCorpse`, Muster wie ShareKillXP —
  dynamisch gespawnte Relay-Objekte replizieren nicht, Hash-0-Fehler), `ZoneGate.CoopTick` (alle
  Lebenden auf dem Gate oder 3 s Host-Hold; `HasClientMirrors` statt `IsSolo`; Clients dwellen lokal
  nicht), `TeleportViaHost` für den Autoplay-Harness (Broadcast + 15 s Wait statt sync), `DiabloCorpse`
  remote-Overload + Death-Broadcast vor Despawn, `CoopAutoplay`-Client bleibt bis Zone 2 (60 s Grace).
  Eigner-Funde: `Diablo.IsSolo` zählt den Server nicht mit (conn==1 solo trotz Client — deshalb
  Mirror-Zählung); Autoplay umgeht Dwell per `zg.Teleport` (SessionAutoplay.cs:309). `HostHero` nutzt
  das Owned-Player-Objekt (zwei Objekte heißen „Player“).
  Belege: EditMode 264/265 (1 vorbestehender `LayoutSeedTests`-Bruch, auch auf Main), PlayMode 127/127
  (neu `CoopGateTests` 8 Tests), `VerifyDiabloZone` OK, `KayKitAssets.Verify` grün, `check-meta` OK;
  Paarlauf Exit 0: Host `host broadcasts FenGate → zone 2`, Client `phase zone2: pulled to the Ossuary`
  + Offsets (0,0,0)/(1,5,0,0), 12 Remote-Corpses, je 0 Exceptions; Solo-Autoplay Exit 0, FPS-Pass 31,2.

## Nächste Schritte

1. S4 economy: Gold/Pickup/Vendor per validiertem ServerRpc.

## Offene Punkte

- Kriterien 1–7 offen bis S2–S6.
- Befund außerhalb des Goals: Solo-Autoplay ohne `-seed` (Seed aus vorhandenem Save, 1112106611, Fen-Variante
  `husks=1 elite=0 brute=2 props=2`) blieb nach 11 Kills bis Budgetende ohne Treffer hängen. Der Nightly testet nur
  Seeds 0–2 und diese Husk-Variante nie.
