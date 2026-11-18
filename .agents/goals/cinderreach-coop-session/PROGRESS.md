# Progress: cinderreach-coop-session

Prozent: 100 %

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

- 2026-11-18: S4 economy landed. `Net/TradeValidator.cs` (neu, pure C#: `CanBuy` Gold/Bag/Range,
  `CanPickup` Range + Once, `CanSell` Range + Preisdeckel 75) als gemeinsame Regel für Host und Solo.
  Gold: Host spawnt Haufen mit `NetworkObject` + Relay (S1-Loot-Muster), Clients per
  `RequestGoldServerRpc` (Reichweite + Once) mit `GrantGoldClientRpc`-Gutschrift an den Owner
  (keine Client-Prediction, keine Doppelzahlung); Host/Solo direkt via `CollectLocal`.
  Item-Loot/Chest: Reichweitenprüfung im ServerRpc (Teleport-Pickup abgelehnt).
  Vendor: Bestand pro Spieler (`Vendor.StockSeed(zoneSeed, clientId)` — Host behält Zone-Seed, Clients
  leiten ab; UI rollt beim Spawn nach, `RestockForOwner`), Kauf/Verkauf per ServerRpc auf dem
  Player-Relay: Host prüft Range + Zeile + Once (pro-Spieler-Restlisten), bewilligt per ClientRpc,
  `TryBuy`/`TrySell` setzen Gold/Bag lokal durch (Aufgabenteilung wie S2-Treffer: geteilte Welt auf
  dem Host, pro-Spieler-Zustand lokal). UI routet im Coop-Fall über das Relay (mit Gold/Bag-Precheck
  gegen Divergenz), sonst direkt.
  Belege: EditMode 297/297 (neu `TradeValidatorTests` 8 Tests: Fälschungen abgelehnt, Seed-Determinismus),
  PlayMode 154/154, `VerifyDiabloZone` OK, `check-meta` OK; Paarlauf Exit 0 (Host 8/8, Client
  `town,zone1` + Zone-2-Pull, 179 Client-Hit-Rejections als Wirkungsnachweis, je 0 Exceptions);
  Solo-Autoplay Exit 0, FPS-Pass 34,2.

- 2026-11-18: S5 save landed. Join-Handshake auf dem Player-Relay: `JoinReport`
  (Level/XP/Punkte/Ränge/Gear als JSON über ServerRpc), `SanitizeJoinReport`
  (Level 1..30, Punkte ≥ 0, nur Katalog-Gear — Vertrauensmodell wie S2-Treffer),
  `ApplyJoinReport` auf den Spiegel (Max-HP/Schaden folgen aus Gear/Level).
  Owner lädt beim Spawn sein Save (`TryLoad`) und meldet danach; Boss-Kill
  löst `NotifyBossKillClientRpc` aus (Clients speichern wie solo), Gate-Reise
  speichert den eigenen Hero (`IsOwner`-Zweig in `ApplyTravel`).
  Belege: EditMode 305/305, PlayMode 173/173 (neu `CoopSaveJoinTests` 4 Tests:
  Roundtrip, JSON, Clamp-Fakes, Spiegel-Power), `VerifyDiabloZone` OK,
  `check-meta` OK; Paarlauf Exit 0 (Join-Reports beidseitig übernommen, Client
  Zone 2 per Broadcast, 12 Remote-Corpses, je 0 Exc); Saves: Host L6/380 G,
  Client L3/Zone 2; Solo-Autoplay Exit 0, FPS-Pass 36,4. `docs/coop-run-log.md`
  (neu): Volllauf beider Seiten + Windows-Vorlage (Build Succeeded 120 MB,
  Stage OK 122 MB nach `Games\cinderreach`; Live-Join + Shotcheck-PNG als
  Eigner-Lauf dokumentiert).
- 2026-11-18: S6 relay landed. Paketmigration per Client-API (kein Hand-Edit):
  `relay`/`lobby` raus, `com.unity.services.multiplayer@2.5.0` rein (Transport
  2.4.0 → 2.7.4). `Net/RelayJoin.cs` (neu): UGS-Init + anonymer Login, Host
  erstellt Session mit Relay (`CreateSessionAsync` + `WithRelayNetwork`),
  Join-Code im Log/Panel; Client joint per `JoinSessionByCodeAsync`. Sessions
  SDK owned Transport + NGO-Start (nach Main-Thread-Fix: kein `ConfigureAwait`
  — Background-Threads brechen den Transport-Allocator); Fallback direktes UTP
  mit Warnung. `JoinArgs`: `-join-code`/`-host-relay`; `JoinPanel` mit
  Code-Feld, HostRelay/JoinByCode-Buttons, Relay-Status; `run-coop-pair.sh`
  mit `COOP_RELAY=1` (Code aus Host-Log, Client per Code).
  Belege: EditMode 305/305, PlayMode 180/180 (neu `RelayJoinTests` 7 Tests),
  `VerifyDiabloZone` OK, `check-meta` OK; Linux-Linux-Relay-Paar Exit 0/0
  (Code MHP868, Client Zone-2-Pull, je 0 Exc); WSL-Host + Windows-Client direkt
  Exit 0/0 (Client Zone-2-Pull, je 0 Exc); WSL-Host + Windows-Client per Code
  Exit 0/0 (Code 6N9BHD, Client Zone-2-Pull, je 0 Exc); Shotcheck-PNGs vom
  Windows-Build (`30_session_complete.png` u.a.); Solo-Autoplay Exit 0,
  FPS-Pass; Direkt-Paar nach Transport-Upgrade weiter Exit 0/0.
  Debug-Funde unterwegs: dynamisch gespawnte Relay-Objekte replizieren nicht
  (Hash-0-Fehler → Broadcasts laufen übers Player-Objekt, S3); gleiche
  Anon-Identität kann sich nicht selbst joinen (→ frische Identität pro
  Prozess); volle Save-Bags brechen den deterministischen Trank-Kauf
  (→ Harness archiviert Stale-Saves).

## Nächste Schritte

Keine — alle Stufen S1–S6 landed; alle 7 Goal-Kriterien belegt.

## Offene Punkte

Keine.
- Befund außerhalb des Goals (bleibt bestehen, kein Goal-Blocker): Solo-Autoplay ohne `-seed` (Seed aus vorhandenem Save, 1112106611, Fen-Variante
  `husks=1 elite=0 brute=2 props=2`) blieb nach 11 Kills bis Budgetende ohne Treffer hängen. Der Nightly testet nur
  Seeds 0–2 und diese Husk-Variante nie.
