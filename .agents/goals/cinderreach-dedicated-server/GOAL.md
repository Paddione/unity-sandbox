---
goal: cinderreach-dedicated-server
slug: cinderreach-dedicated-server
health_goals: [G-DIABLO02, G-DIABLO03]
health_probe: {"id":"G-CRSERVER01","title":"Cinderreach-Server-Pod im devmesh bereit","cmd":"kubectl --context devmesh -n cinderreach get deploy cinderreach-server -o jsonpath='{.status.readyReplicas}' | grep -qx 1; echo $?","target":0}
plan_refs: [.agents/plans/cinderreach-dedicated-server/tasks.md]
---

# Cinderreach – Dedizierter Server für Internet-Koop

Ausgangslage: [CINDERREACH.md](../CINDERREACH.md). Eigner-Entscheidungen 2026-11-23: dedizierter Server statt
Spieler-Host, Betrieb im k3s-Heimcluster (devmesh), Erreichbarkeit über Unity Relay (kein offener Port, keine
Router-Änderung).

## Objective

Ein headless laufender Cinderreach-Server im devmesh-Cluster nimmt Spieler aus dem Internet auf. Der Windows-Client
findet ihn über „Join server“ ohne Code oder IP. Der Server besitzt selbst keinen Helden, die Spieler führen den Run.

## Erfolgskriterien

1. Server-Modus (`-server`): NGO-Server ohne eigenen Helden, kein Speichern, keine UI. Der erste verbundene Spieler
   führt Gruppenreisen, Broadcasts laufen über ein beliebiges Spielerobjekt, Boss-Kills werden in die Clients gebucht.
   Leert sich der Server nach einer Sitzung, beendet er sich (Neustart = frischer Run). Ein lokaler Lauf mit
   Linux-Server und zwei Clients belegt Beitritt, Kampf in der Fen und Gruppenreise in das Ossuary.
2. Relay-Server und Auffinden: Der Server registriert eine öffentliche Relay-Session „Cinderreach Server“. Clients
   treten per `-join-server` oder Startbildschirm-Knopf bei, ohne Code. Lokaler Relay-Lauf mit zwei Clients grün.
3. Betrieb: Linux-Server-Build als Container-Image unter `ghcr.io/paddione/cinderreach-server`, Manifeste unter
   dotfiles `apps/cinderreach-server/k8s`, Flux-Kustomization im devmesh, Pod bereit, `G-CRSERVER01` grün.
4. Abnahme: Windows-Client (Installer-Build) tritt dem Pod über das Internet-Relay bei und spielt mit einem zweiten
   Client; Installer neu gebaut; Runbook dokumentiert Update, Neustart und Logs.

## Nicht-Ziele

Persistente Welt über Neustarts, mehrere Server-Instanzen, Matchmaking, Server-seitige Charakterspeicherung,
Anti-Cheat jenseits der bestehenden host-autoritativen Validierung.
