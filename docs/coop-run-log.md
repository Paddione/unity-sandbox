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
