---
goal: cinderreach-skills-modularization
slug: cinderreach-skills-modularization
branch: feature/cinderreach-skills-modularization-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
priority: P4 (unabhängiges Refactoring, höheres Risiko Kampf-Autorität; parallel zur Story-Kette möglich, nach inventory-ui-layout-split)
status: ready — cinderreach-party-roles ist 100 % (PROGRESS: Keine offenen Punkte), der S1-Abgleich ist damit nur noch lesende Kontrolle; Code-Basis seit Planerstellung unverändert
start_condition: Stand gegen cinderreach-party-roles abgeglichen (GOAL-Abhängigkeit, erfüllt: 100 %); kein paralleles Schreiben an DiabloSkills.cs; GOAL-Korrektur aus S1 ist gelandet
plan_refs: [.agents/goals/cinderreach-skills-modularization/GOAL.md]
---

# Plan: cinderreach-skills-modularization — DiabloSkills in BowStanceVisuals und SkillBarHud entzerren

Pfad: feature, 4 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-skills-modularization-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).
Reihenfolge ist Abhängigkeitsreihenfolge; keine Stufe lässt Suiten, Verify oder Host-Start rot zurück.

## Goal

Siehe [GOAL.md](../../goals/cinderreach-skills-modularization/GOAL.md):
UGUI-Skillbar-Rendering (`SkillBarHud`) und Bogen-/Schwert-Stance-Visuals
(`BowStanceVisuals`) aus der Kampf-Autorität `DiabloSkills.cs` herauslösen,
ohne Netz- oder Kampfverträge zu brechen.

## Success Criteria

Die vier Erfolgskriterien aus GOAL.md — mit der in S1 korrigierten Enum-Basis
(0..19 statt 0..11, s. u.) —, je Stufe unten mit Test-Beleg verknüpft.
Gesamt-Gate S4: die 7 benannten Suites (`DiabloSkillsTests`, `LanternSkillTests`,
`PartySkillTests`, `SkillRankTests`, `BuildUniqueEffectTests`,
`PartySkillFormulaTests`, `DiabloHdPlayTests`) plus vollständige
EditMode-/PlayMode-Suiten (`bash scripts/run-unity-tests.sh`),
`python3 scripts/check-meta.py` und `KayKitAssets.Verify` grün.

## Ausgangslage (Belege, HEAD `8e59d00`, 2026-11-27)

- `sandbox/Assets/Scripts/DiabloSkills.cs`: 1.414 Zeilen (Audit-Basis `5bd41d3`
  nannte 1.304). Seit der Basis hat `aaa2571` (Class-Kits/Kombos) die Datei
  erweitert — die GOAL-Zahlen sind an zwei Stellen überholt:
  - Skill-Enum heute `Fireball=0 … PowderKeg=19` (Zeilen 23–29), nicht `0..11`.
    Kriterium 3 muss auf `0..19` korrigiert werden, bevor Code angefasst wird (S1).
  - Aufrufer heute 59 Dateien mit `DiabloSkills`-Referenz (Scripts + Tests),
    nicht 42 — in S1 per `grep -rln` neu zählen und als Vertrag einfrieren.
- Stance-Block (Zeilen 221–347): Konstanten `BowStanceSeconds` (225) und
  `SwordName` (227, wird auch von `ClassAppearance.cs:313` gelesen), Felder
  `bowUntil`, `heldBow`, `bowRenderers`, `showBowModel`, `swordRenderers`;
  API `BowDrawn` (234), `SetStarterBowModelVisible` (236), `EnterBowStance` (246),
  `ExitBowStance` (255), `CreateHeldBow` (262), `HandMountRotation` (293),
  `LongAxis` (297), `SetSwordVisible` (327). `FindDeep` (338) wird nur von diesem
  Block genutzt (267, 331) und zieht mit um.
- Fassaden-Aufrufer außerhalb: `HeroEquipmentAppearance.cs:151` und
  `DiabloAnimator.cs:60` lesen `BowDrawn`; `DiabloClickController.cs:399` ruft
  `ExitBowStance`; Tests rufen `DiabloSkills.LongAxis`
  (`DiabloSkillsTests.cs:62`), `EnterBowStance`/`BowDrawn` (ebd. 59/186/193/233)
  und `PartySkillTests.cs:66` (`BowDrawn`). `DiabloSkills.cs:1203` beendet die
  Stance per Timeout in der eigenen Update-Schleife.
- HUD-Block: Felder `hud`, `slotsContainer`, `hudBuiltFor`, `hudSlots`
  (206–210); `UpdateHud` (1228), `HudLine` (1249, öffentlich),
  `BuildHud` (1294, erzeugt `SkillBarCanvas` ab 1296), `RebuildHudSlots` (1345).
  Aufrufer: `UpdateHud` aus der eigenen Schleife (1206/1225); `HudLine` aus
  `PartySkillFormulaTests.cs:111` und `LanternSkillFormulaTests.cs:94`.
- Kampf-Kern bleibt: `Cast` (485), `CastToward` (497), `PlayCosmetic` (600),
  `ServerExecuteSkill` (631) mit `DiabloNetworkPlayer.SubmitSkillCastServerRpc`
  (`DiabloNetworkPlayer.cs:624`, Aufruf `DiabloSkills.cs:521`), Balance-Felder
  und Cooldown-/Schadenslogik (350–484). Diese Zeilen ändert keine Stufe inhaltlich.
- Alle 7 Kriterium-4-Suites existieren: `DiabloSkillsTests`, `LanternSkillTests`,
  `PartySkillTests`, `DiabloHdPlayTests` (PlayMode), `SkillRankTests`,
  `PartySkillFormulaTests`, `BuildUniqueEffectTests` (Edit-/PlayMode verteilt),
  dazu `LanternSkillFormulaTests` als HUD-nahe Zusatzsuite.

## Entscheide (Annahmen, im Plan gelockt)

1. GOAL-Korrektur vor Code (S1): Kriterium 3 auf Enum `0..19` und die neu
   gezählte Aufrufer-Menge berichtigen. Das ist eine Faktenkorrektur aus
   `aaa2571`, keine Scope-Änderung; ohne sie würde der Plan gegen einen
   nicht mehr existierenden Vertrag verifizieren.
2. `BowStanceVisuals` wird eine normale Klasse (kein `MonoBehaviour`), die
   `DiabloSkills` besitzt und mit `transform` des Helden füttert. Keine neue
   Szenen-Komponente, kein `SandboxBuilder`-Eingriff, kein Prefab-Umbau.
   Verworfene Alternative: eigene Komponente am Helden (Szenen-/Builder-Verdrahtung,
   Remote-Mirror-Fälle) für einen reinen Visual-Block ohne eigenen Lebenszyklus.
3. `SkillBarHud` wird ebenfalls eine besessene Klasse; sie bekommt Lese-Zugriff
   auf Cooldown-/Klassenstand über enge Delegaten (`CooldownLeft`, `EffectiveCooldown`,
   aktive Klasse), statt `DiabloSkills` als Ganzes zu referenzieren.
   Canvas-Name `SkillBarCanvas` und Child-Hierarchie bleiben byte-identisch.
4. Öffentliche Fassaden bleiben auf `DiabloSkills`: `BowDrawn`,
   `EnterBowStance`/`ExitBowStance`, `SetStarterBowModelVisible`,
   `HandMountRotation`/`LongAxis` (statisch, testgenutzt) und `HudLine`
   delegieren an die neuen Klassen. Kein Aufrufer außerhalb von
   `DiabloSkills.cs` wird angefasst.
5. Neue Dateien bekommen ihre `.meta` per Unity-Import (neue GUID), wie im
   Schwester-Plan `cinderreach-inventory-ui-layout-split` (Entscheid 3 dort).

## Approach

Reihenfolge: Vertrag einfrieren (S1) → Stance extrahieren (S2) → HUD
extrahieren (S3) → Gesamtbeweis (S4). Stance vor HUD, weil der Stance-Block
klarer abgegrenzt ist (eigener Feldsatz, nur zwei externe Leser) und der
Timeout-Pfad (`DiabloSkills.cs:1203`) danach bereits über die Fassade läuft;
die HUD braucht mit den Cooldown-Delegaten die feinere Naht und profitiert von
der in S2 eingeübten Fassaden-Form. Kampf-, Netz- und Balance-Code wird in
keiner Stufe umformuliert — verschoben werden nur Visual-/Rendering-Blöcke.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 re-baseline | GOAL K3 auf Enum 0..19 + Aufrufer-Zahl korrigieren, 7 Suites als Baseline, Abgleich `cinderreach-party-roles` | `.agents/goals/cinderreach-skills-modularization/{GOAL,PROGRESS}.md` |
| S2 stance | `BowStanceVisuals` extrahieren, Fassaden auf `DiabloSkills` delegieren | `sandbox/Assets/Scripts/DiabloSkills.cs`, `sandbox/Assets/Scripts/BowStanceVisuals.cs` (neu + `.meta`) |
| S3 hud | `SkillBarHud` extrahieren, Cooldown-Delegaten verdrahten, `HudLine`-Fassade | `sandbox/Assets/Scripts/DiabloSkills.cs`, `sandbox/Assets/Scripts/SkillBarHud.cs` (neu + `.meta`) |
| S4 proof | Voll-Suiten, `check-meta.py`, `KayKitAssets.Verify`, Koop-/Autoplay-Smoke, PROGRESS-Abschluss | `.agents/goals/cinderreach-skills-modularization/PROGRESS.md`, ggf. `docs/class-kits-run-log.md` (nur Verweis-Eintrag) |

S2 und S3 berühren beide `DiabloSkills.cs`, sind aber block-disjunkt
(Stance 221–347 vs. HUD 206–210/1228–1412); Reihenfolge S2 → S3 ist trotzdem
Pflicht, damit kein Merge zwischen zwei Extraktionen an derselben Datei entsteht.

## Steps

### S1 — Re-Baseline: GOAL-Vertrag und Teststand einfrieren
- GOAL.md Kriterium 3 korrigieren: Enum `0..19`, Aufrufer per
  `grep -rln "DiabloSkills" sandbox/Assets/Scripts sandbox/Assets/Tests` neu
  zählen und Zahl + Zähldatum eintragen; Änderung in `PROGRESS.md` als
  Faktenkorrektur aus `aaa2571` begründen.
- Stand gegen `cinderreach-party-roles` abgleichen (GOAL-Abhängigkeit):
  prüfen, ob dessen gelandete Stufen `DiabloSkills.cs` berühren; offene
  Berührungspunkte in `PROGRESS.md` nennen, Start sonst freigeben.
- Die 7 Suites auf unverändertem `main` fahren (gefiltert:
  `PLATFORMS=PlayMode TEST_FILTER=DiabloSkillsTests bash scripts/run-unity-tests.sh` usw.),
  Ergebnis in `PROGRESS.md`.
- Verify: korrigiertes GOAL lint-grün (`node ~/scripts/goal-lint.mjs --repo .`), Baseline dokumentiert.

### S2 — `BowStanceVisuals` extrahieren
- Neue Klasse `BowStanceVisuals` (`sandbox/Assets/Scripts/BowStanceVisuals.cs`):
  übernimmt Felder `bowUntil`, `heldBow`, `bowRenderers`, `showBowModel`,
  `swordRenderers` sowie `CreateHeldBow`, `HandMountRotation`, `LongAxis`,
  `SetSwordVisible`, `FindDeep` und die Stance-Zeitlogik (`BowStanceSeconds`).
  Konstruktor bekommt den Helden-`Transform`; Bogen-Prefab
  (`Resources.Load("SkillArt/HeroBow")`) und Mount-Namen (`handslot.l`/`hand.l`) unverändert.
- `DiabloSkills` hält eine `BowStanceVisuals`-Instanz; Fassaden `BowDrawn`,
  `EnterBowStance`, `ExitBowStance`, `SetStarterBowModelVisible` delegieren,
  statische `HandMountRotation`/`LongAxis` delegieren an die Klasse.
  `SwordName` bleibt Konstante auf `DiabloSkills` (`ClassAppearance.cs:313`).
  Timeout-Pfad (Zeile 1203) ruft nur noch die Fassade.
- `.meta` per Unity-Import erzeugen, `python3 scripts/check-meta.py` grün,
  Compiler-Beleg per `unity --json recompile` oder Batchmode ohne `data.errors`.
- Verify: `DiabloSkillsTests` (inkl. `HeldBow_TurnsAboutItsLongAxis`,
  Stance-Timeout-Fälle) und `PartySkillTests` unverändert grün; kein Test angefasst.

### S3 — `SkillBarHud` extrahieren
- Neue Klasse `SkillBarHud` (`sandbox/Assets/Scripts/SkillBarHud.cs`):
  übernimmt Felder `hud`, `slotsContainer`, `hudBuiltFor`, `hudSlots` sowie
  `BuildHud`, `RebuildHudSlots`, `UpdateHud` und die Text-Erzeugung hinter
  `HudLine`. `SkillBarCanvas`-Name und Child-Hierarchie bleiben identisch
  (Bau-Code wird wortgleich verschoben).
- Naht: `SkillBarHud` erhält beim Bau Delegaten für `CooldownLeft`,
  `EffectiveCooldown` und die aktive Klasse (`ActiveClass`/`ActiveVariantId`),
  statt auf `DiabloSkills`-Interna zuzugreifen. `DiabloSkills.HudLine()`
  bleibt öffentliche Fassade und delegiert.
- Eigene Schleife: Die bisherigen `UpdateHud`-Aufrufe (1206/1225) rufen die
  HUD-Instanz; Remote-Mirror-Verhalten (`ExternalDrive`-Zweig) bleibt gleich.
- `.meta` per Unity-Import, `check-meta.py` grün, Compiler-Beleg wie S2.
- Verify: `PartySkillFormulaTests` (`HudLine_NamesNewSkills`),
  `LanternSkillFormulaTests` (`HudLine_FollowsTheClass`), `LanternSkillTests`
  und `DiabloSkillsTests` unverändert grün.

### S4 — Gesamtverifikation und Abschluss
- Alle 7 Kriterium-Suites plus `LanternSkillFormulaTests` gezielt, dann
  `bash scripts/run-unity-tests.sh` vollständig (EditMode + PlayMode),
  `python3 scripts/check-meta.py`, Verify aus `repo.yaml:verify`
  (`KayKitAssets.Verify`), `python3 ~/scripts/agent-workspace.py check` grün.
- Verhaltens-Smoke: Autoplay (`scripts/run-autoplay.sh`) Exit 0 ohne Exception
  und Koop-Paar (`scripts/run-coop-pair.sh`), falls Player-Build verfügbar —
  der Netz-Pfad (`SubmitSkillCastServerRpc`, Skill-Int-Vertrag 0..19) ist
  unverändert, ein roter Smoke gilt bis zum Gegenbeweis als Extraktions-Regression.
  Ohne Build den Smoke als offenen manuellen Beleg benennen, nicht behaupten.
- Sichtprüfung Skillbar + Bogen-Stance im Shotcheck (Bogen gezeichnet/ablegt,
  Cooldown-Veil), Zeilenzahl vorher/nachher je Datei in `PROGRESS.md`,
  dann Goal auf 100 % mit `Offene Punkte: Keine.`

## Validation Plan

- Kernvergleich S1↔S4: identische Suite-Ergebnisse; jeder neue Fehlschlag ist
  bis zum Gegenbeweis eine Extraktions-Regression.
- Höchstrisiko-Checks: (a) Netz-Vertrag — Enum-Werte 0..19 und
  `SubmitSkillCastServerRpc(int, …)` bleiben unangetastet, `grep`-Beleg im
  S4-Eintrag; (b) Hierarchie-Vertrag — `SkillBarCanvas`-Aufbau wortgleich
  verschoben, HUD-Tests prüfen Namen/Hierarchie über die Szene;
  (c) Stance-Timing — `BowStanceSeconds`-Timeout läuft weiter über die
  Fassade in der `DiabloSkills`-Schleife (Testfall Zeile 193).
- Je Stufe Compiler-Beleg vor Unity-Läufen, damit GUID-/Partial-Fehler nicht
  als Spielfehler fehlinterpretiert werden.

## Risks / Open Questions

- Risiken: Versteckte Kopplung der HUD an Cooldown-Interna (Entscheid 3
  begrenzt die Naht auf drei Delegaten); Stance-Renderer-Caches
  (`bowRenderers`/`swordRenderers`) müssen mit der Instanz wandern, nicht in
  `DiabloSkills` zurückbleiben; parallele Arbeit an `DiabloSkills.cs` aus
  `cinderreach-party-roles` (Startbedingung + S1-Abgleich).
- Rollback: Jede Stufe eigener Branch/Worktree; S2/S3 sind reine
  Verschiebungen hinter Fassaden — rote Stufe wird nicht gelandet, `main`
  behält die monolithische Klasse. Kein Save-/Netz-Risiko, weil Verträge
  (Enum, RPC, Balance-Felder) in keiner Stufe geändert werden.
- Open Questions: Keine für S1/S3/S4. In S2 ist zu entscheiden, ob
  `BowStanceVisuals` zusätzlich eine eigene `Tick`-Methode für den Timeout
  bekommt oder `DiabloSkills` weiter `BowDrawn` pollt — Empfehlung: Poll über
  die Fassade behalten (heutiges Verhalten, kleinster Diff); Entscheidung
  fällt beim Implementieren und steht dann in `PROGRESS.md`.
