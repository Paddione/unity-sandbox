---
goal: cinderreach-coop-session
slug: cinderreach-coop-session
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-coop-session/tasks.md]
---

# Goal: cinderreach-coop-session — Co-op-Parität der Session-Schleife

Angelegt: 2026-11-17. Herkunft: Eigner-Auftrag „come up with the next goals“ nach `arpg-session-loop`.
Folge-Goal, Reihenfolge siehe `.agents/goals/ROADMAP.md`.

## Objective

Die komplette Session-Schleife aus `arpg-session-loop` funktioniert für 2–4 Spieler im NGO-Host-Modell:
gemeinsamer Zonen-Übergang, replizierter Boss, host-autoritative Vendor- und Gold-Transaktionen, Leichen
auf Clients. Dazu kommt ein belegter Relay-Join zwischen zwei Maschinen.

## Erfolgskriterien

1. Zonen-Gate: Übergang nach Sunken Ossuary bringt alle Spieler mit (Host entscheidet, Clients folgen per
   ClientRpc). Beleg: PlayMode-Multi-Instance-Test oder 2-Prozess-Lauf (Host + Client, Linux-Player) mit Log.
2. Boss und Bone Wardens host-autoritativ: HP, Phasenwechsel und Summons repliziert. Schaden von Clients per
   validiertem ServerRpc. Beleg: Test „Client-Treffer senkt Boss-HP auf Host und Client gleich“.
3. Gold, Vendor-Kauf/-Verkauf und Loot-Pickup per ServerRpc mit Validierung (Gold reicht, Bag frei,
   Reichweite). Inventar und Gold bleiben pro Spieler. Beleg: Tests für abgelehnte Fälschungen.
4. `DiabloCorpse` auch auf Clients (heute bewusst nur Host).
5. Save: jeder Client speichert seinen eigenen Charakter. Join mit vorhandenem Save übernimmt Level und Gear.
   Beleg: Roundtrip-Test im Client-Pfad.
6. Join zwischen WSL-Host und Windows-Client (`Games\cinderreach`) per direktem UTP (IP/Port, WSL
   `mirrored`): beide sehen Kampf und Loot. Beleg: Logs beider Seiten + Shotcheck-PNG vom Client.
   Zusätzlich Relay-Join per Code über das verknüpfte UGS-Projekt „Cinderreach“ (Multiplayer-Services-Paket).
7. Solo-Pfad und `G-DIABLO06`-Autoplay bleiben grün.

## Nicht-Ziele

- Keine dedizierten Server, kein Matchmaking, keine Lobby-UI über den Join-Code hinaus.
- Kein Cheat-Schutz über Server-Validierung hinaus.
- Keine neuen Inhalte.

## Abhängigkeiten

- Start erst nach `arpg-session-loop` 100 %. Sinnvoll nach `cinderreach-loop-guard`, weil der Autoplay den
  Solo-Regressionsschutz liefert.
- `unity-multiplayer-demo` (NGO 2.5 + Relay/Lobby, dotfiles).
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
