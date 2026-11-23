# Co-op run log (cinderreach-coop-session S5)

Zwei-Prozess-Volllauf Town → Boss → Abschluss auf beiden Seiten, Linux-Player
(`scripts/run-coop-pair.sh`). Der Client lädt beim Join sein eigenes Save,
meldet Level/Punkte/Gear per ServerRpc (Host klemmt: Level ≤ 30, nur
Katalog-Gear), und beide Seiten speichern bei Gate und Boss-Kill wie solo.

## Paar-Lauf 2026-11-18 (S5-Stand, `run-coop-pair.sh`, Exit 0)

- Host: `exit=0 phases=town,zone1,gate,zone2,boss,zone3,actboss,complete`,
  0 Exceptions, ~220 s. Client: `exit=0 phases=town,zone1`, 0 Exceptions,
  ~106 s. Artefakte: `artifacts/coop/2026-11-18/{host,client}/player.log`
  und `autoplay-result.json` je Seite.
- Join: beide Seiten loggen `loaded local save for co-op join`; der Host
  wendet die Reports auf die Spiegel an (`join stats from 0: level 5 ...`,
  `join stats from 1: level 3 ...`).
- Gate: Host-Broadcast (`host broadcasts FenGate → zone 2`), Client folgt
  per ClientRpc (`phase zone2: pulled to the Ossuary by the host broadcast`,
  Spawn-Offsets Host `(0,0,0)` / Client `(1.5,0,0)`), danach je ein eigener
  Corpse-Aufbau pro getötetem Husk auf dem Client (`remote corpse ...`, S3).
- Saves danach (`~/.config/unity3d/Unity Technologies/Cinderreach/`):
  `coop-host.json` Level 6, 380 Gold, Zone 1 (Rückweg-Tor in die Town),
  `coop-client.json` Level 3, Zone 2 (Gate-Reise gespeichert wie solo).
  Getrennte Dateien per `-save-file` (`coop-host.json` / `coop-client.json`).
- Solo-Regression (`run-autoplay.sh`): Exit 0, 0 Exceptions, FPS-Pass.

## Windows-Client gegen WSL-Host (Eigner-Lauf, Vorlage)

Build und Stage sind aus diesem Stand erzeugt (kein Start vom Headless aus —
der Start gehört auf den Windows-Desktop):

1. Windows-Build (braucht das WindowsStandalone-Modul, ~2 min):
   `bash scripts/run-unity-build.sh SandboxBuilder.BuildDiabloZoneWindows "DiabloZone Windows build result: Succeeded" 1500`
   (Stand S5: Succeeded, 120 MB; der Builder schreibt fest nach
   `builds/cinderreach-windows/`, also im Hauptcheckout — `BUILD_INFO.json`
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
Windows-Läufe zusätzlich über Einmal-Harness `/tmp/coop-win.sh`
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
