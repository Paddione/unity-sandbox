# Fortschritt – cinderreach-dedicated-server

Prozent: 100 %

## Erledigt

- S1 Server-Modus (`46ea3d6`): Lauf s1-1 Lead 4/4, Follow 3/3, Server Exit 0.
- S2 Relay + Lobby, „Join Server“ (`ff43095`): Lauf s2-relay-1 grün, Region europe-west4.
- S3 Container und devmesh (`a18b55b`, dotfiles `7ec4da97`, Bachelorprojekt #7331): Pod `workspace/cinderreach-server`
  bereit, Container-Rauchtest s3-docker-4 mit 0 Server-Exceptions.
- S4 Abnahme: Paketlimit 1200 Byte (`6036e68`, dotfiles `56764d3a`) nach Clientabbrüchen gegen den Pod (WireGuard-MTU
  1420). Produktivlauf s4-prod-1 gegen „Cinderreach Server“: Windows-Client (installierter Build) Lead 4/4, Linux-Client
  Follow 3/3, 0 Exceptions, Pod danach selbst neu gestartet und neu veröffentlicht. Installer
  `Cinderreach-Setup-0.1.0-ae2222d.exe` neu gebaut, Runbook `docs/runbook.md` „Dedizierter Server (devmesh)“.
  `G-CRSERVER01`: `readyReplicas` = 1.
- EditMode 426/426, PlayMode 280/280.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
