# Co-op run log (cinderreach-coop-session S5)

Zwei-Prozess-Volllauf Town → Boss → Abschluss auf beiden Seiten, Linux-Player
(`scripts/run-coop-pair.sh`). Der Client lädt beim Join sein eigenes Save,
meldet Level/Punkte/Gear per ServerRpc (Host klemmt: Level ≤ 30, nur
Katalog-Gear), und beide Seiten speichern bei Gate und Boss-Kill wie solo.

## Paar-Lauf 2026-11-18 (S5-Stand, `run-coop-pair.sh`, Exit 0)

- Host: `exit=0 phases=town,zone1,gate,zone2,boss,zone3,actboss,complete`,
  0 Exceptions, ~220 s. Client: `exit=0 phases=town,zone1`, 0 Exceptions,
  ~106 s. Artefakte: `artifacts/coop/2026-11-18/{host,client}/player.log`
  und "autoplay-result.json" je Seite.
- Join: beide Seiten loggen `loaded local save for co-op join`; der Host
  wendet die Reports auf die Spiegel an (`join stats from 0: level 5 ...`,
  `join stats from 1: level 3 ...`).
- Gate: Host-Broadcast (`host broadcasts FenGate → zone 2`), Client folgt
  per ClientRpc (`phase zone2: pulled to the Ossuary by the host broadcast`,
  Spawn-Offsets Host `(0,0,0)` / Client `(1.5,0,0)`), danach je ein eigener
  Corpse-Aufbau pro getötetem Husk auf dem Client (`remote corpse ...`, S3).
- Saves danach (`~/.config/unity3d/Unity Technologies/Cinderreach/`):
  "coop-host.json" Level 6, 380 Gold, Zone 1 (Rückweg-Tor in die Town),
  "coop-client.json" Level 3, Zone 2 (Gate-Reise gespeichert wie solo).
  Getrennte Dateien per `-save-file` ("coop-host.json" / "coop-client.json").
- Solo-Regression (`run-autoplay.sh`): Exit 0, 0 Exceptions, FPS-Pass.

## Windows-Client gegen WSL-Host (Eigner-Lauf, Vorlage)

Build und Stage sind aus diesem Stand erzeugt (kein Start vom Headless aus —
der Start gehört auf den Windows-Desktop):

1. Windows-Build (braucht das WindowsStandalone-Modul, ~2 min):
   `bash scripts/run-unity-build.sh SandboxBuilder.BuildDiabloZoneWindows "DiabloZone Windows build result: Succeeded" 1500`
   (Stand S5: Succeeded, 120 MB; der Builder schreibt fest nach
   `builds/cinderreach-windows/`, also im Hauptcheckout — "BUILD_INFO.json"
   nennt dort den Main-Stand, das Binary kam aus diesem Worktree).
2. Stagen (verweigert bei laufendem Spiel, Exit 3):
   `bash scripts/stage-windows.sh`
   → `C:\Users\PatrickKorczewski\Games\cinderreach` (Stand S5: OK, 122 MB).
3. WSL-Host starten (Mirrored-Modus, Port 7790):
   `./builds/autoplay-player/cinderreach -host -port 7790 -autoplay -autoplay-god -coop-autoplay host -save-file coop-host.json`
4. Windows-Client beitreten (PowerShell/Explorer):
   `C:\Users\PatrickKorczewski\Games\cinderreach\cinderreach.exe -join 127.0.0.1:7790 -autoplay-god -coop-autoplay client -save-file coop-client.json`
5. Erwartete Belege: beide Logs ohne Exceptions, Client sieht Kampf und Loot,
   `player.log` (Client) enthält `phase zone2`, Shotcheck-PNG vom Client.
   Der Relay-Join-Code (`-join-code`, UGS-Projekt „Cinderreach“) folgt in S6.

## S6 — Relay (Linux-Linux und WSL ↔ Windows)

Harness: `COOP_RELAY=1 bash scripts/run-coop-pair.sh artifacts/coop-relay/<datum>`
(Host `-host-relay`, Code aus `RELAY_JOIN_CODE=…`, Client `-join-code CODE`).
Windows-Läufe zusätzlich über Einmal-Harness "/tmp/coop-win.sh"
(`MODE=direct|relay`; kein Repo-Bestandteil — Befehle siehe unten).

- Linux-Linux: Host `RELAY_JOIN_CODE=MHP868`, beide Exit 0, Client
  `phase zone2: pulled to the Ossuary by the host broadcast`, je 0 Exceptions
  (`artifacts/coop-relay/2026-11-18/`).
- WSL-Host + Windows-Client, direkt: beide Exit 0, Client
  `phase zone2: pulled to the Ossuary`, je 0 Exceptions
  (`artifacts/coop-win-direct/2026-11-18/`).
- WSL-Host + Windows-Client, per Code: Host `RELAY_JOIN_CODE=6N9BHD`,
  Client `joined session by code 6N9BHD` + `phase zone2`, beide Exit 0,
  je 0 Exceptions (`artifacts/coop-win-relay/2026-11-18/`).
- Shotcheck-PNGs vom Windows-Build (`artifacts/shotcheck/`,
  z. B. `30_session_complete.png`: Session-Complete-Overlay in Lantern Hold).
- Windows-Befehle (Staging s. S5, S6-Build 121 MB): Host wie oben; Client
  direkt `cinderreach.exe -join 127.0.0.1:7790 -coop-autoplay client ...`
  oder per Code `cinderreach.exe -join-code <CODE> -coop-autoplay client ...`.
  Shotcheck: `bash scripts/shotcheck-windows.sh` vom Repo-Root.

## Dreiergruppe Akt I + II (coop-trio, 2026-11-23)

Harness: `COOP_CLIENTS=2 [COOP_CLIENT_GOD=0] COOP_HOST_CLASS=… COOP_CLIENT_CLASS=… COOP_CLIENT2_CLASS=… bash scripts/run-coop-pair.sh <out>`.
Host wartet per `-coop-peers 3` auf beide Clients. Clients kämpfen in jeder Zone mit Rollen-Skills und
buchen jede Boss-Meldung des Hosts. Artefakte unter `artifacts/coop-trio/run<N>/{host,client,client2}/`.

| Lauf | Klassen (Host/C1/C2) | Clients | Ergebnis | Befund |
|---|---|---|---|---|
| 1 | Fenwalker/Rogue/Lanternbearer | god | Host 10/10, C1 8/8, C2 8/8, 0 Exceptions | Saves aller drei: Kiln offen, Akt I, Glasswright, Level 5 |
| 2 | dto. | sterblich | Host ohne `zone1`, Clients ohne `zone1` | F1/F2 Gate, Heiler ohne `zone4` (Bolts zählten nicht) |
| 3 | dto. | sterblich | Clients 8/8, Host ohne `zone1` | F3 Host 120 s hinter stehendem Client blockiert |
| 4 | dto. | sterblich | Host 10/10, C1 8/8, C2 8/8, 0 Exceptions, 194 s | Heals 8 / 24 HP, Rogue fällt am Glasswright und steigt wieder ein |
| 5 | Lanternbearer/Fenwalker/Rogue | sterblich | Host 10/10, C1 8/8, C2 8/8, 0 Exceptions, 199 s | Rollen getauscht grün |
| Paar | Standard (1 Client) | god | Host 10/10, Client 8/8 | Regression des Harness-Umbaus grün |

Behobene Fehler:

- B1: Clients buchten den Abbot-Kill nicht (Kiln im Client-Stand und -Save gesperrt, kein Kindled).
  Jetzt `NotifyBossKillClientRpc` → `BookAbbotKill`.
- B2: Der Kilnmother-Kill erreichte Clients gar nicht (Akt I nie abgeschlossen bis zum Glasswright).
  Jetzt `BroadcastActBossKill` → `BookKilnmotherKill` + Autosave.
- F1: Gruppentor feuerte ohne Verweilzeit, sobald alle Lebenden im Tor-Bereich standen. Das FenGate (0,1,−6)
  liegt am Weg in den Fen, die Gruppe wurde im Vorbeigehen ins Ossuary gezogen. Jetzt 0,6 s wie solo.
- F2: Nach einem Broadcast sendete der Host bis zum eigenen Teleport jeden Frame erneut (13× pro Tor,
  Clients teleportierten mehrfach). Jetzt 2 s Sperre (`ZoneGate.BroadcastLockSeconds`), 3 Broadcasts je Lauf.
- F3: Helden blockierten sich per `CharacterController`, ein stehender Mitspieler sperrte den Weg dauerhaft.
  Jetzt `DiabloNetworkPlayer.IgnoreAllyCollisions` bei jedem Netz-Spawn. Gegner und Wände blocken weiter.

Offen:

- ~~Mitspieler-HP nicht repliziert~~ und ~~Host-Lanternbearer heilt nie~~: erledigt, siehe HP-Sync unten.
- Boss-Overlays (`SessionComplete`) erscheinen nur beim Host, Clients sehen keinen Akt-Abschluss-Bildschirm.

## HP-Sync der Mitspieler (coop-hp-sync, 2026-11-23)

Gegnerschaden wird weiter auf jedem Prozess simuliert, maßgeblich sind aber nur die HP des Besitzers.
`DiabloNetworkPlayer.hpState` (Owner-schreibend, `(current, max)`) wird bei echten Änderungen
gesendet (`NeedsHpSync`: erste Meldung, > 0,05 HP, Tod oder Volle-HP-Wechsel). Mirrors übernehmen
den Wert (`PlayerHealth.SetMirrored`), lokale Treffer, Heilungen und Tod/Respawn bleiben auf
Mirrors wirkungslos. `PlayerHealth.Heal` gibt die (bei Mirrors vorhergesagte) Heilmenge zurück, die
das Heil-Relay zum Besitzer trägt. Der Besitzer loggt `[Net] heal from <id>: +N → cur/max`.
Host-Autoplay als Lanternbearer heilt jetzt zuerst (Renewing ab 2 Verletzten, sonst Mending).

| Lauf | Klassen (Host/C1/C2) | Ergebnis | Heilung |
|---|---|---|---|
| hp1 | Lanternbearer/Fenwalker/Rogue, Clients sterblich | Host 10/10, C1 8/8, C2 8/8, 0 Exceptions | Host 21 Heals, beim Besitzer angekommen 14 + 11, Client-Tode 0 |
| hp2 | Fenwalker/Rogue/Lanternbearer, Clients sterblich | Host 10/10, C1 8/8, C2 8/8, 0 Exceptions | Client-Heiler → Rogue 10 Heals angekommen |

Vorher (Lauf 5 oben, gleicher Host-Heiler ohne Sync): `heals=0`, ein Client-Tod.
Solo-Lanternbearer Seeds 0 und 1: 10/10 (ein erster Seed-1-Lauf lief im Barrens-Budget mit 2 Shardbacks
aus, die Wiederholung war grün). EditMode 417/417, PlayMode 280/280.

## Dedizierter Server (cinderreach-dedicated-server S1, 2026-11-23)

`COOP_SERVER=1 COOP_CLIENTS=2 bash scripts/run-coop-pair.sh artifacts/coop-server/<lauf>`: headless
`-batchmode -nographics -server -port N` ohne eigenen Helden, Client 1 `-coop-autoplay lead` (Fen-Kampf,
Gruppenreise durch das FenGate, Ossuary-Kampf), Client 2 `-coop-autoplay follow` (folgt dem Lead).

| Lauf | Ergebnis |
|---|---|
| s1-1 | Lead 4/4 (`town,zone1,gate,zone2`, 21 + 18 Treffer), Follow 3/3 (+ `boss`: Abbot-Kill vom Server gebucht), Server stellt Solo-Helden ab, `[Zone] host broadcasts FenGate → zone 2`, nach dem Verlassen nach 20 s Exit 0, je 0 Exceptions |

Regeln: Leader = lebender Spieler mit kleinster ClientId (`DedicatedServer.Leader`), Broadcasts über ein
beliebiges gespawntes Spielerobjekt (`DedicatedServer.AnyPlayer`), Server speichert nicht, keine KI-Begleiter,
60-fps-Deckel. Ein Server, der Spieler hatte und 20 s leer bleibt, beendet sich (Neustart = frischer Run).

### Relay-Server und Auffinden (S2)

`COOP_SERVER=1 COOP_RELAY=1 COOP_CLIENTS=2 bash scripts/run-coop-pair.sh artifacts/coop-server/<lauf>`: Server
`-server -server-relay -server-name "Cinderreach Server test-<pid>"`, Clients `-join-server -server-name …`.
Die Sessions-API startet nur sessionlose (server-authentifizierte) Sessions als Server, ein anonym angemeldeter
Prozess würde Host. Deshalb direkt Relay + Lobby (`ServerDirectory`): Allokation für 4 Spieler, NGO-Server darauf,
öffentliche Lobby mit Join-Code und Build, Heartbeat alle 15 s, Löschen vor dem Beenden.

| Lauf | Ergebnis |
|---|---|
| s2-relay-1 | `[Server] published … relay region europe-west4`, beide Clients `joining Cinderreach server via relay`, Lead 4/4, Follow 3/3, Server Exit 0, 0 Exceptions, keine Heartbeat-/Löschfehler |

### Container (S3)

Image aus `scripts/publish-server-image.sh` lokal per `docker run … -server-name "<test>"`, zwei Linux-Clients
`-join-server` über Relay.

| Lauf | Ergebnis |
|---|---|
| s3-docker-2 | Server-Subtarget: Lead/Follow grün, aber 32 Server-Exceptions (`Material..ctor` ohne Shader in `EnemyHealth.DropOrb`, `GoldDrop`, `PaintGoal` in `ToyTrigger`, mitten in `Die()`) |
| Player-Subtarget im Container | Segfault in `PlayerMain` nach dem Szenenstart, auch mit X11/GL/GTK-Bibliotheken: verworfen |
| s3-docker-4 | Server-Subtarget mit Shader-Null-Schutz: Lead 4/4, Follow 3/3 (+ `boss`), Container-Exit 0, 0 Server-Exceptions |

### Abnahme gegen den devmesh-Pod (S4)

| Lauf | Aufbau | Ergebnis |
|---|---|---|
| s4-windows-2 | Pod `k3s-tablet`, Windows-Lead, Linux-Follow | beide verbunden, Windows nach ~40 s vom Server getrennt (kein Datenfluss nach dem Handshake), Lead-NRE am Tor |
| s4-windows-3 | Pod `k3s-tablet`, Linux-Lead, Windows-Follow | Windows sieht den Lead nie bewegen, getrennt |
| s4-local-win2 | lokaler Container (MTU 1500), Linux-Lead, Windows-Follow | grün: Follow `town,zone1,zone2,boss` |
| s4-nodetest | Test-Pod `gpu-cluster2`, Linux-Lead, Windows-Follow | diesmal Linux getrennt: nicht plattform-, sondern pfadabhängig |
| s4-pkt-1 | Test-Pod `k3s-tablet` mit Paketlimit 1200, Windows-Lead, Linux-Follow | grün: Lead 4/4, Follow 3/3, Gruppenreise, 0 Exceptions auf allen drei Prozessen |

Ursache: Unity Transport sendet UDP-Nutzlasten bis 1400 Byte (1428 auf der Leitung), die Pods hängen an Flannel
WireGuard mit MTU 1420. Volle Pakete gingen verloren, sobald eine fragmentierte zuverlässige Nachricht lief, der
Client lief nach 30–60 s in den Timeout. `CompactPacketDriver` begrenzt auf 1200 Byte für alle Netzmodi.
Nebenbei: per `timeout` abgebrochene Windows-Läufe lassen `cinderreach.exe` weiterlaufen (sperrt Logs/Staging),
danach `taskkill /IM cinderreach.exe /F`.

## Spielernamen und Konten (cinderreach-accounts, 2026-11-23)

Namen per `-player-name`, Konten über `scripts/account-smoke.sh` (Wegwerf-Konto, danach gelöscht).

| Lauf | Aufbau | Ergebnis |
|---|---|---|
| s1-relay-trio | `COOP_RELAY=1 COOP_CLIENTS=2`, Host + 2 Clients | volle Route Exit 0, 0 Exceptions; jeder Prozess meldet „Client1/Client2/Host joined the party“ |
| s4-server-relay | `COOP_SERVER=1 COOP_RELAY=1 COOP_CLIENTS=2` | Lead `town,zone1,gate,zone2`, Follow `town,zone1,zone2`, Server Exit 0; Server und Clients kennen beide Namen |
| s4-smoke-linux | `account-smoke.sh` (Linux-Player) | create/restore/pc2 je 0 Fehler: Gast behält ID, Neustart stellt Konto her, Upload, Konflikt, Download |
| s4-smoke-windows | gleiche Probe mit `Games\cinderreach\cinderreach.exe` (`dc62621`) | create/restore/pc2 je 0 Fehler, Konto-Held (Stufe 7, 123 Gold) kommt auf dem zweiten Profil an |
| s4-prod-names | devmesh-Pod (Image `sha256:a6f619ab…`), Windows `-join-server` Lead „WinTester“, Linux Follow „LinuxTester“ | Lead `town,zone1,gate,zone2`, Follow `town,zone1,zone2,boss`, je 0 Exceptions, beide sehen den Namen des anderen |

## Teamkampagne auf dediziertem Server (cinderreach-team-autoplay, 2026-11-27)

`bash scripts/run-team-autoplay.sh artifacts/coop-team/<lauf>`: headless `-server` (UTP 127.0.0.1) plus drei
Linux-Clients unter `xvfb-gpu`, je eigenes HOME/XDG-Profil. Client 1 fenwalker führt mit `-autoplay -autoplay-god
-coop-autoplay lead` (volle Solo-Route mit allen Quests und Thewes), Client 2 lanternbearer und Client 3 rogue sind
sterblich (`-coop-autoplay follow`). Sitzung `normal` aus frischen Saves, danach `kindled` aus denselben Saves ohne
`-difficulty`. Auswertung `scripts/team_autoplay_verdict.py` (`<sitzung>/team-verdict.json`).

| Lauf | Ergebnis | Befund und Fix |
|---|---|---|
| run1 | Lead 11/34 Quests, Follower hängen | Lead plante das nächste Gespräch vor dem Story-Broadcast des Servers (Jorin vergab h16/n10 nicht), Follower folgten nach dem Abgang des Leads einander: Lead wartet auf die Buchung, Follower binden sich an die Lead-ClientId |
| run2 | Lead 13/34, Akt 2/3 fehlen | Clients versteckten die Akt-Bosse vor dem Szenen-Sync, NGO spawnt inaktive In-Scene-Objekte nicht: jeder Client tötete eine lokale Kilnmother. `ActBossGate.MayHide` versteckt auf Clients erst nach dem Spawn |
| run3 | kein Spiel | Editor-SIGFPE beim Beenden nach erfolgreichem Build: `run-unity-build.sh` wertet Signal-Exit nach Marker und Batchmode-Quit als OK |
| run4 | Lead 32/34 (h12, h13) | Thewes-RPC vor dem Story-Broadcast: Lead wartet nach Thewes auf H12 Done |
| run5 | Lead 13/34 | Questrunden vor dem Akt-Boss aufgebraucht, bevor n3 beim Lead ankam: 10 Runden mit 1,5 s Sync-Pause |
| run6 | Lead ohne `zone1` | Gegner haben keine NetworkTransform, die Client-Kopie driftete (~1200 Reichweiten-Ablehnungen): Server schickt nach einer Ablehnung die Server-Position an den Absender (`SnapEnemyClientRpc`, 0,5 s Drossel) |
| run7 | normal PASS, kindled PASS | Lead 34/34, `1:Kilnmother:- 2:Glasswright:- 3:Thewes:-`, Follower 9/9 Phasen, alle drei Kindled freigeschaltet und gespeichert, Server Exit 0; Kindled: `[Difficulty] server adopts Kindled from client 1: hp x1.8 damage x1.8 gold x1.5 unique x2 affixTier 2, 70 enemies rescaled`, Lead 34/34, je 0 Exceptions (Normal 307 s, Kindled 414 s) |
| run8 (nach Rebase auf `c0aad39`) | normal PASS, kindled Lead exit 1 | Lead 34/34, aber `caughtUp=[h19]`: der Client schloss H19 per lokalem Catch-up, bevor der Server-Broadcast H19 als gestartet zeigte (gespielt haben es die Follower). Koop-Clients wenden keinen Story-Catch-up mehr an (`StoryDirector.CatchUpHere`) |
| CI 46058725563 (`369186f`) | normal PASS, kindled PASS | Workflow-Job 21 min; normal: Lead 34/34, fellLast, Clients 320/317/314 s, alle Kindled freigeschaltet und gespeichert; kindled: Server adoptiert Kindled (x1.8, 70 Gegner umgerechnet), Lead 34/34, alle exit 0 (405/402/399 s) |

Regeln: Der dedizierte Server ist Story-Autorität (`StoryDirector.ServerTick`: Refresh, Moortor, Akt-Boss-Gate,
Broadcast bei Änderung, Log `[Server] story gate`). Clients leiten Funde und Zonenbetreten weiter
(`SubmitStoryEventServerRpc`). Der erste Join bestimmt die Schwierigkeit des Servers (`DedicatedServer.AdoptPartyDifficulty`,
lebende Gegner werden umgerechnet). Ein Neuer Lauf auf anderer Schwierigkeit startet die Kampagne neu und behält den Helden
(`RunProfile.StartCampaign`, auch der Button „Neuer Lauf“).
