---
goal: cinderreach-companion-rendering
slug: cinderreach-companion-rendering
health_goals: [G-DIABLO02, G-DIABLO03, G-GOALS01]
---

# Goal: cinderreach-companion-rendering — Begleiter-Rendering, Gender-Modelle und Erstellungs-Vorschau

Angelegt: 2026-11-24. Herkunft: Eigner-Anfrage mit Screenshot vom 2026-11-24 („CPU-assisted teammates and some purple entity are not properly rendered, female characters have the same model as male ones, add a Vorschau characterview in the creation", setgoal-Runden 2026-11-24, alle Punkte mit Empfehlung entschieden; Persistenz + Verfolgung auf Eigner-Wunsch).

## Objective

In `unity-sandbox` (`sandbox/`, Unity 6000.0.85f2, URP, Cinderreach) Begleiter und Platzhalter-Entities als echte Charaktere rendern und mit dem Helden mitlaufen lassen; weibliche Helden bekommen eigene Modelle aus KayKit-Bestand; die Erstellung erhält eine eingebettete 3D-Vorschau. Ausgangslage: `Party/PartyRoster.cs` (`SpawnCompanion`) erzeugt getönte `Capsule`-Primitives ohne Animator (Screenshot: grüne/gelbe Kapseln); primitive Feind-Spawns mit URP-Default-Material wirken lila/magenta (u. a. `Enemies/BossController.cs`-Kommentar); `Classes/HeroAppearance.cs` (`GetProportions`) skaliert nur Knochen (0,85–1,25); `Menu/ClassSelectUI.cs` (`PreviewDraft`) zeigt den Draft nur am Welt-Helden hinter dem Modal.

## Erfolgskriterien

1. CPU-Begleiter rendern als volle Klassen-Helden: `PartyRoster.SpawnCompanion` spawnt pro Begleiter das KayKit-Modell seiner Klasse aus `Classes/ClassCatalog.cs` (Fenwalker `Knight.fbx`, Lanternbearer `Mage.fbx`, Rogue `Rogue.fbx`), mit `ClassAppearance`-Look, Ausrüstung, Animator und Namenslabel; keine `Capsule`-Primitives mehr (PlayMode-Beleg je Klasse).
2. Begleiter laufen mit dem Helden: bestehende `Party/PartyCompanion.cs`-Logik (`FormationSlots`, `FollowStep`, `LeashRange`, `RegroupRange`, `PortWithLeader`, `RejoinDelay`) bleibt erhalten und wird mit Locomotion (stehen/gehen/rennen via `DiabloAnimator`/`LocomotionTwist`) sichtbar; Rejoin nach Tod am Leader.
3. Lila Platzhalter ist weg: alle primitiven Feind-/Effekt-Spawns (Boss-Summons in `Enemies/BossController.cs`, `RiskShrine`-Elite in `Zones/RiskShrine.cs`, `DuneAugur`-/`BurningGround`-Zylinder) nutzen KayKit-Skelett-Modelle bzw. korrektes URP-Lit-Material statt Magenta/Lila-Default (Screenshot-Stelle belegt).
4. Weibliche Modelle sind eigenständig: pro Klasse eigene weibliche Basis aus dem vorhandenen Pack (`Assets/ThirdParty/KayKit/Adventurers/Characters/`, z. B. `Ranger.fbx`/`Rogue_Hooded.fbx` statt `Knight.fbx`), plus `HeroAppearance`-Haar (6 Stile, 10 Farben) und Build-Proportionen; weiblich/männlich/nicht-binär sind am Modell unterscheidbar.
5. Erstellungs-Vorschau (`Vorschau CharacterView`): das Customize-Modal in `Menu/ClassSelectUI.cs` bekommt einen eingebetteten 3D-Viewport (RenderTexture + Drehteller), der jede Wahl (Klasse, Gender, Haar, Farbe, Build, Name) sofort am Modell zeigt; `CancelCustomize` stellt den alten Look wieder her.
6. Gates grün: neue/aktualisierte Tests in `sandbox/Assets/Tests/{EditMode,PlayMode}` (Begleiter-Spawns, Gender-Modelle, Vorschau), `KayKitAssets.Verify` (`repo.yaml:verify`), `python3 scripts/check-meta.py`, `python3 ~/scripts/agent-workspace.py check`; Eintrag in `docs/decision-log.md`. Vor 100 % steht in `PROGRESS.md` unter `## Offene Punkte`: `Keine.`

## Scope

- `sandbox/Assets/Scripts/Party/` (`sandbox/Assets/Scripts/Party/PartyRoster.cs`, `sandbox/Assets/Scripts/Party/PartyCompanion.cs`)
- `sandbox/Assets/Scripts/Classes/` (`sandbox/Assets/Scripts/Classes/ClassCatalog.cs`, `sandbox/Assets/Scripts/Classes/ClassAppearance.cs`, `sandbox/Assets/Scripts/Classes/HeroAppearance.cs`)
- `sandbox/Assets/Scripts/Menu/ClassSelectUI.cs` (Customize-Modal, Vorschau-Viewport, `Loc`-Texte deutsch mit Englisch-Fallback)
- Primitive Feind-Spawns (`Enemies/BossController.cs`, `Zones/RiskShrine.cs`, `Enemies/DuneAugur.cs`, `Enemies/BurningGround.cs`)
- KayKit-Bestand unter `Assets/ThirdParty/KayKit/` (keine neuen Imports)
- Tests: `sandbox/Assets/Tests/{EditMode,PlayMode}`

## Nicht-Ziele

- Keine neuen kostenpflichtigen KayKit-Imports, keine neue Blender-/Asset-Pipeline, kein Asset-Store-Kauf
- Kein Umstyling nach der Erstellung, kein Friseur/NPC, kein Respec
- Kein Gameplay-Effekt von Gender/Build (Stats, Hitbox, Tempo), keine Änderung an Klassen, Skills oder `DiabloSkills.Skill`-Netz-Int-Vertrag
- Keine Szenen-/Netzwerk-Architekturänderung, keine Änderung an `PlayerAccount`/Cloud-Sync, kein Windows-Staging

## Abhängigkeiten / Referenzen

- Ist-Stand: `.agents/goals/cinderreach-character-creation/` (Erstellung mit Gender/Haar/Build/Name, Save v14, Koop-Profil; dort als Nicht-Ziel: keine echten Gender-Körpermodelle — dieses Goal erweitert genau diesen Punkt, bleibt aber beim KayKit-Bestand ohne neue Pipeline), `.agents/goals/cinderreach-party-roles/`, `.agents/goals/cinderreach-party-team/`, `.agents/goals/CINDERREACH.md`
- Ausführung: Worktree `feature/cinderreach-companion-rendering`, Abschluss per `agent-workspace.py land` (`land: ff`)
- health_goals: [G-DIABLO02, G-DIABLO03, G-GOALS01]
