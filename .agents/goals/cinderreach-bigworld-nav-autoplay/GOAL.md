# Goal: cinderreach-bigworld-nav-autoplay

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]

plan_refs: [.agents/plans/cinderreach-bigworld-nav-autoplay/tasks.md]

## Objective
Helden, Begleiter und Autoplay kommen in der 4× Welt überall hin. Alle Layout-Varianten bleiben erreichbar, Begleiter bleiben auf dem Boden, und die Solo-Autoplay-Route läuft von Lantern Hold bis Thewes ohne Hänger durch.

## Ist-Befunde
- B1 Erreichbarkeit läuft über 0,5-m-Kapsel-Flood ab Spawn zu Fen-Tor, Truhe und allen Gegnerankern für 81 Varianten (`sandbox/Assets/Tests/EditMode/LayoutSeedTests.cs:60-120`, `LayoutSeeder.VariantCount = 3`, `HuskCount = 10` in `sandbox/Assets/Scripts/Zones/LayoutSeeder.cs:22`).
- B2 Bewegung nutzt Grid-A* mit Kapsel-Probe, Stall-Replan und Warp-Fallback (`AutoplayMover`, Zähler `navPlans`/`navWarps` in `autoplay-result.json`); Pflichtphasen `town, zone1, gate, zone2, boss, zone3, actboss, zone4, boss3, complete` plus `actBossKills`-Feld.
- B3 Begleiter clampen auf `WorldBounds` (`PartyRoster.PortWithLeader` bei `ZoneTravel`), Tore verlangen 0,6 s Verweilzeit; Solo-Autoplay ohne `-seed` kann außerhalb Seed 0–2 in seltener Fen-Variante hängen (bekannte Grenze).
- B4 Vorgänger-Goal `cinderreach-bigworld-encounters` ist gelandet (Encounter-Positionen stabil, Voll-Suiten EditMode 791/791 + PlayMode 432/432 in `docs/test-runs.jsonl`); Autoplay-Beleg für die 4× Welt mit `-seed 0` steht aus.

## Entscheidungen
- D1 Modus: Anlegen + planen + umsetzen (Eigner: Empfehlung übernommen).
- D2 Zuschnitt: eigenes Nav-/Autoplay-Goal nach den Encounter-Sicherungen (Eigner: Empfehlung übernommen).
- D3 Vorrang: Erreichbarkeit und Autoplay gleichrangig mit Safe-Abständen (Eigner: Empfehlung übernommen).
- D4 Abnahme: Voll-Suiten + Verify + Autoplay seed 0 grün (Eigner: Empfehlung übernommen).

## Erfolgskriterien
1. Alle 81 Layout-Varianten erreichen vom Spawn Fen-Tor, Truhe und alle Gegneranker — Nachweis: `PLATFORMS=EditMode TEST_FILTER=LayoutSeedTests bash scripts/run-unity-tests.sh` grün, bei Flood-Lücken in neuen Außenflächen Fix in Test oder Builder mit Begründung.
2. Solo-Autoplay `-seed 0` läuft in der 4× Welt bis `complete` mit Exit 0, alle Story-Quests `Done` ohne Catch-up (`questsCaughtUp == 0`), `actBossKills` zeigt `-` für alle drei Akte — Nachweis: `bash scripts/run-autoplay.sh <out> -seed 0` plus `autoplay-result.json` im Beleg-Commit.
3. Kein Nav-Regress und keine Stranded-Begleiter: Companion-Clamp und Tor-Reise bleiben auf den neuen Böden — Nachweis: Voll-Suiten `bash scripts/run-unity-tests.sh` grün (EditMode ≥ 791, PlayMode ≥ 432), `SandboxBuilder.VerifyDiabloZone` + `KayKitAssets.Verify` + `python3 scripts/check-meta.py` grün.

## Nicht-Ziele
- Keine neuen Routen/Quests und keine Autoplay-KI-Umstellung; nur robust machen, was existiert.
- Kein Windows-Shotcheck und keine FPS-Wertung (nur Linux-Hinweis-Gate).
- Keine Encounter-Umstellung (Vorgänger-Goal).

## Abhängigkeiten
- `cinderreach-bigworld-encounters` (gelandet `b426472`; stabile Encounter-Positionen als Basis für Nav-/Autoplay-Nachweise).
