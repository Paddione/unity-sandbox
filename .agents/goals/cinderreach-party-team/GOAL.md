---
goal: cinderreach-party-team
slug: cinderreach-party-team
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-party-team/tasks.md]
---

# Goal: cinderreach-party-team — Spawn/Despawn-Beweise und ausrüstbare Begleiter auf Heldenniveau

Angelegt: 2026-11-19. Herkunft: Eigner-Nachfrage auf `cinderreach-party-roles` (100 %):
Spawn/Despawn der Computer-Teambegleiter soll zusammen mit dem Teamplay getestet sein,
und das Team soll ausrüstbar sein und auf dem Level des Protagonisten bleiben.

## Objective

In `unity-sandbox` (`sandbox/`, Unity 6 URP, Cinderreach) die Solo-KI-Gruppe härten:
(1) Spawn und Despawn der Begleiter sind durch Teamplay-Integrationstests belegt
(kämpfen + heilen + Threat nach Spawn, nach Despawn und nach Nachfüllen),
(2) Begleiter tragen eigene Ausrüstung (`Equipment`, Klassenbindung wie beim Helden,
kein Loot-Bag/kein Vendor — Nicht-Ziel aus party-roles bleibt),
(3) Begleiter spiegeln Level, XP, Attribute und Skillränge des Protagonisten
(quiet mirror: keine eigene XP-Leiste, kein Autosave, keine Fanfare).

## Erfolgskriterien

1. `PartyTeamLifecycleTests` (PlayMode, neu): Spawn-Gruppe kämpft/heilt/hält Threat in einem
   Durchlauf; Join despawnt genau die Beitrittsrolle und der Rest spielt weiter (Heal + Schaden);
   Leave füllt die Rolle nach (ganz, voll HP, `AllHeroes` wieder 3); Overfull-Join (`-1`) ändert
   nichts; Tote blockieren ihren Slot (kein Doppel-Spawn) bis zum Rejoin — grün.
2. `PartyGearLevelTests` (PlayMode, neu): Begleiter spawnen mit eigener `Equipment`-Komponente
   (MaxHP zählt Gear ab Start, `PlayerHealth.Refresh` pflegt nach); klassenfremde Waffen werden
   abgewiesen; Waffenschaden skaliert Begleiter-Pfeile, Skillrang den Begleiter-Heal (Formel +
   echter Ziel-Heal); Spawn- und Sync-Pfad spiegeln Heldenlevel/Attribute (MaxHP-Mathe), Begleiter
   ohne XP-Leiste, Held mit — grün.
3. Host-Pfad (`ServerReconcile`) nutzt dieselbe `ApplyRemoteRoles`-Logik wie die Tests
   (kein Solo-Sonderpfad im Join/Leave-Verhalten); Koop-Smoke aus dem party-roles-Run-Log
   bleibt gültig (Logik identisch refaktoriert, keine Netzänderung).
4. EditMode-/PlayMode-Suiten, `KayKitAssets.Verify`, `python3 scripts/check-meta.py`,
   `python3 ~/scripts/agent-workspace.py check` grün; Eintrag in `docs/mechanics-questions.md`
   + `docs/decision-log.md`.

## Scope

- `sandbox/Assets/Scripts/Party/PartyRoster.cs` (Test-Seam `ApplyRemoteRoles`, Spawn-Reihenfolge
  mit `Equipment`, Level-Sync im `Update`)
- `sandbox/Assets/Scripts/Party/PartyCompanion.cs` (`Gear`/`Equip`, Doku)
- `sandbox/Assets/Scripts/DiabloProgression.cs` (`MirrorHeroProgress`, Companion-Guards)
- `sandbox/Assets/Scripts/PlayerHealth.cs` (`Refresh` pflegt Equipment nach)
- `sandbox/Assets/Tests/PlayMode/PartyTeamLifecycleTests.cs` + `sandbox/Assets/Tests/PlayMode/PartyGearLevelTests.cs` (neu)

## Nicht-Ziele

- Kein Begleiter-Bag/Vendor/Schmiede-UI, kein Begleiter-Loot, keine Begleiter-Dialoge
- Keine eigene Begleiter-Skillung (Build wird vom Helden gespiegelt), kein Mana
- Keine Netz-/Save-Formatänderung (Spiegel ist Runtime-only), kein Final-Art

## Abhängigkeiten / Referenzen

- `cinderreach-party-roles` (100 %): `PartyRoster`/`PartyCompanion`, `HeroLocator.AllHeroes`,
  `ReconcileRoles`, `NeedsJoinGate`, `CoopPartyTests`/`SoloPartyTests`
- `cinderreach-itemization` (100 %): `Equipment`/`ItemDatabase`/`AffixLibrary`, Klassenbindung
- Ausgangslage: `.agents/goals/CINDERREACH.md`
