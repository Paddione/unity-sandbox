---
title: "Cinderreach dedizierter Server"
domains: [unity, networking, infra]
status: in_progress
goal: cinderreach-dedicated-server
slug: cinderreach-dedicated-server
branch: feature/cinderreach-dedicated-server-sN
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03]
---

# cinderreach-dedicated-server — Plan

[Ziel](../../goals/cinderreach-dedicated-server/GOAL.md). Planbasis `bb87532`.

## S1 · Server-Modus ohne Host-Helden

- `Scripts/Net/JoinArgs.cs`: `-server` (Alias `--net-server`) → `NetMode.Server`.
- Neu `Scripts/Net/DedicatedServer.cs`: `Active`, Boot deaktiviert den Solo-Helden, Leader = lebender Spieler mit
  kleinster ClientId, Leer-Exit nach der ersten Sitzung, Speichern aus.
- `Scripts/Zones/ZoneGate.cs` (`HostHero` → Leader), `Scripts/DiabloNetworkPlayer.cs` (`HostRelay` → beliebiges
  gespawntes Spielerobjekt), `Scripts/Save/SaveService.cs` (Server speichert nicht), `Scripts/Party/PartyRoster.cs`
  (KI füllt fehlende Rollen hinter dem Leader).
- `Scripts/Net/CoopAutoplay.cs`: Rolle `lead` (ein Client läuft die Fen-Route bis ins Ossuary), `client` folgt dem Leader.
- `scripts/run-coop-pair.sh`: `COOP_SERVER=1` startet headless Server + Clients.
- Tests: EditMode `DedicatedServerTests` (Args, Leader-Wahl, Exit-Regel), PlayMode-Suite grün, Paarlauf mit Server.

## S2 · Relay-Server und Auffinden

- `Scripts/Net/RelayJoin.cs`: Server erstellt öffentliche Session (Name „Cinderreach Server“, Relay, StartServer),
  Client sucht per Query und tritt bei (`-join-server`). `ClassSelectUI`/`JoinPanel`: Knopf „Join server“.
- Paarlauf `COOP_SERVER=1 COOP_RELAY=1`.

## S3 · Container und devmesh

- `SandboxBuilder.BuildDiabloZoneLinuxServer` (StandaloneLinux64, Server-Subtarget), `docker/server/Dockerfile`,
  `scripts/publish-server-image.sh` → `ghcr.io/paddione/cinderreach-server:<commit>`.
- dotfiles `apps/cinderreach-server/k8s` (Namespace, Deployment amd64, keine Service-Ports), Bachelorprojekt
  `flux/clusters/devmesh/ks-cinderreach-server.yaml`, dotfiles Health-Probe `G-CRSERVER01`.

## S4 · Abnahme

- Windows-Client gegen Pod, zweiter Client, Installer neu, `docs/runbook.md` Abschnitt Server, CINDERREACH.md einarbeiten.
