---
goal: arpg-session-loop
slug: arpg-session-loop
branch: feature/arpg-session-loop-s<N> (eine Branch pro Stufe, je `land: ff`)
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO05]
request: "substantially improve our unity game" → Spielschleife + eigene Identität (setgoal 2026-11-17)
---

# Plan: arpg-session-loop — Session-Schleife mit eigener Identität

Pfad: feature, 6 Stufen. **Andere Sessions: zuerst Abschnitt „Session-Protokoll“ lesen.** Jede Stufe ist einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/arpg-session-loop-sN`,
Abschluss `land`) und lässt Suiten, Verify und Host-Start grün. Reihenfolge ist Abhängigkeitsreihenfolge.

## Ausgangslage (Belege, HEAD `ca19e68`)

- `sandbox/Assets/Editor/SandboxBuilder.cs` (1397 Z.): `BuildDiabloZone()` Z. 596, `BuildMoorField()` Z. 845,
  `SpawnZoneHusk()` Z. 1158, `BuildDiabloZonePlayerPrefab()` Z. 1185, `VerifyDiabloZone()` Z. 1221,
  `BuildDiabloZoneWindows()` Z. 1342. Monolith; neue Bauteile kommen in eigene Builder-Dateien.
- Vorhanden: `DiabloProgression` (Level bis 30, nur +3 Life/Level, keine Wahl, „not saved between runs“),
  `DiabloSkills` (`enum Skill { Fireball, Arrow }`, Q/E, Cooldowns, `ApplyHit`), `Items/Inventory`,
  `Items/Equipment`, `Loot/LootDrop.SpawnLoot/CollectInto`, `LootTier { Normal, Elite, Chest }`,
  `PlayerHealth`, `CampfireHeal`, `EnemyAttack` (Anmarsch, Telegraph, Steering), `HuskAnimator`,
  `DiabloCorpse`, `DiabloShotCheck` (`-shotcheck`, feste Kamerawinkel), `DiabloMenuArt`.
- Fehlt (grep 0 Treffer): Gold/Währung, Vendor, Save/Load (`JsonUtility`/`PlayerPrefs`), zweite Zone,
  Boss, Abschluss-Screen, Punktevergabe.
- D2-Namen in 15 Dateien (Scripts, Editor, Tests, `WorldArtBuilder`), `productName:
  com.unity.template.urp-blank`, Build-/Stage-Name `diablo-zone` in `scripts/stage-windows.sh`,
  `scripts/shotcheck-windows.sh` und dotfiles `scripts/health_probes/diablo_unity.py` (Z. 18, 176, 373–395).
- Tests: EditMode 12 Dateien, PlayMode 12 Dateien inkl. `DiabloTestFixture`.

## Entscheide (gelockt, Eigner 2026-11-17 + Planannahmen)

1. Fokus Spielschleife; Kampf-Feel, Präsentation und Co-op nur regressionsfrei halten.
2. Zone 2 liegt **in `DiabloZone.unity`** als abgesetzter Bereich (Übergang = Tor-Trigger + Fade + Teleport).
   Kein NGO-Szenenwechsel, Co-op-Host-Pfad bleibt unberührt. Planannahme.
3. Session-Zustand (Gold, Save, Punkte) bleibt lokal pro Client wie Inventar und HP.
4. Interne Bezeichner (`Diablo*`, Szenendateien) bleiben. Nur spielersichtbare Strings und Build-Artefakte
   werden umbenannt.
5. Identität (Eigner-bestätigt 2026-11-17):

   | Alt | Neu |
   |---|---|
   | Spiel / productName / Exe | **Cinderreach** / `cinderreach.exe` / `builds/cinderreach-windows` / `Games\cinderreach` |
   | Blood Moor (Zone 1) | Ashen Fen |
   | Rogue Encampment (Town) | Lantern Hold |
   | Moor Gate | Fen Gate |
   | Zone 2 | Sunken Ossuary |
   | Husk / Husk_Brute / Husk_Elite | Husk / Fen Brute / Ash Elite (Husk bleibt generisch) |
   | Zone-2-Gegner | Bone Warden (Fernkampf-Kaster) |
   | Boss | The Hollow Abbot |

## Vorbedingung: Fremd-Änderungen „HD-UI + MenuArt-Pack“

**Erfüllt 2026-11-17** durch Commit `feat(ui): HD UI scaling and MenuArt starter pack` (nachgeholt aus dem
Hauptcheckout, da die Ursprungs-Session sie nicht gelandet hatte). Die Prüfung in Protokollschritt 1 bleibt
als Absicherung.

- Inhalt: CanvasScaler aller Laufzeit-UIs auf `ScaleWithScreenSize`, 1920×1080, `MatchWidthOrHeight`
  0.5 (`DiabloInventoryUI`, `DiabloJuice`, `DiabloLifeGlobe`, `DiabloProgression`, `DiabloSkills`,
  `DiabloTargetBar`). Dazu `ProjectSettings.asset`, neue Tests `EditMode/DiabloHdTests.cs` und
  `PlayMode/DiabloHdPlayTests.cs`, MenuArt-Sprites `BloodBanner`, `LootChest`, `TorchSconce`
  (`Resources/MenuArt/`) samt `docs/menu-art/STARTERPACK.md` und `build_props.py`.
- Folgen für diesen Plan:
  - Neue UIs (Vendor, Character, SessionComplete, Menü-„Continue“) übernehmen exakt diese
    Scaler-Konvention. `DiabloHdTests` wird je Stufe um die neuen Canvas-Erzeuger erweitert.
  - S1 benennt die Art-Bible („blood-moor dark fantasy“) in `STARTERPACK.md`, `build_props.py` und im
    Kommentar von `DiabloPalette.cs` auf Ashen Fen um. `BloodBanner` bleibt als generischer Spritename.
  - Liegen danach noch `InitTestScene*.unity` oder `mono_crash.mem.*.blob` ungetrackt im Hauptcheckout,
    fasst sie keine Stufe an (Testlauf-Reste der anderen Session).

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 identity | Rename, productName, Build-/Stage-Name, Probe-Vertrag | alle Treffer-Dateien der Altnamen-Regex (Stand Plan: 15 + `docs/menu-art/STARTERPACK.md`, `docs/menu-art/build_props.py`, `Scripts/DiabloPalette.cs`; vor Start neu greppen), `ProjectSettings/ProjectSettings.asset`, `scripts/stage-windows.sh`, `scripts/shotcheck-windows.sh`, `docs/one-page-gdd.md`, `Tests/EditMode/IdentityTests.cs` (neu); dotfiles: `scripts/health_probes/diablo_unity.py`, `scripts/test_diablo_unity_health.py` |
| S2 economy+save | Gold, Wallet, SaveGame | `Scripts/Items/Wallet.cs` (neu), `Scripts/Loot/GoldDrop.cs` (neu), `Scripts/Loot/LootDrop.cs`, `Scripts/Loot/DroptableDatabase.cs`, `Scripts/Save/SaveGame.cs` (neu), `Scripts/Save/SaveService.cs` (neu), `Scripts/DiabloProgression.cs` (State-Export/Import), `Tests/EditMode/SaveGameTests.cs`, `Tests/PlayMode/EconomyLoopTests.cs` |
| S3 town+vendor | Lantern-Hold-Rework, Vendor + UI | `Scripts/Town/Vendor.cs`, `Scripts/Town/VendorStock.cs`, `Scripts/Town/DiabloVendorUI.cs` (neu), `Editor/TownBuilder.cs` (neu, aus `BuildDiabloZone` ausgelagerter Camp-Teil), `Editor/SandboxBuilder.cs`, `Tests/EditMode/VendorTests.cs`, `Tests/PlayMode/VendorLoopTests.cs` |
| S4 progression | Punkte, Attribute, Skill-Ränge, 3. Skill | `Scripts/DiabloProgression.cs`, `Scripts/Progression/CharacterSheet.cs` (neu), `Scripts/Progression/DiabloCharacterUI.cs` (neu, Taste `C`), `Scripts/DiabloSkills.cs` (+ `Skill.Cleave` auf `R`), `Editor/SkillArtBuilder.cs`, `Tests/EditMode/CharacterSheetTests.cs`, `Tests/PlayMode/SkillRankTests.cs` |
| S5 zone2+boss | Ossuary, Bone Warden, Boss, Abschluss | `Editor/OssuaryBuilder.cs` (neu), `Scripts/Zones/ZoneGate.cs` (neu), `Scripts/Enemies/RangedCaster.cs` (neu), `Scripts/Enemies/BossController.cs` (neu), `Scripts/SessionComplete.cs` (neu), `Scripts/Loot/LootTable.cs` (`LootTier.Boss`), `Editor/SandboxBuilder.cs` (Aufruf + Verify), `Tests/EditMode/OssuaryConfigTests.cs`, `Tests/PlayMode/BossLoopTests.cs` |
| S6 proof | Autoplay-Lauf, Shotcheck-Szenen, Docs | `Scripts/SessionAutoplay.cs` (neu, Flag `-autoplay`), `Scripts/DiabloShotCheck.cs` (+ Town/Vendor/Ossuary/Boss-Shots), `docs/session-loop-run-log.md`, `docs/decision-log.md`, `docs/dev-journal.md`, `docs/mechanics-questions.md` |

S2 hängt an nichts. S3 braucht S2 (Gold). S4 braucht S2 (Save der Punkte). S5 braucht S2 und S4
(Boss-Balancing gegen Level 5–8). S6 braucht alles. S1 läuft zuerst, weil jede spätere Stufe neue
Strings schreibt.

## S1 — Identität

- Strings ersetzen laut Tabelle (Hint-Texte, Banner, Gegnernamen, Menü-Art-Titel, Kommentare).
  `WorldArtBuilder`-Objektnamen mitziehen, Asset-GUIDs nicht anfassen.
- `productName: Cinderreach`, `BuildDiabloZoneWindows()` Ausgabe `builds/cinderreach-windows/cinderreach.exe`.
- `stage-windows.sh`/`shotcheck-windows.sh` auf `Games/cinderreach`. Einmalige Migration: alten Ordner
  `Games\diablo-zone` nach erfolgreichem Stage löschen (nur Build-Dateien, vorher listen).
- dotfiles-Worktree: `diablo_unity.py` (G-DIABLO05 Pfade `cinderreach`, Altordner-Erkennung auch für
  `diablo-zone.exe`), Tests anpassen, `land`. Memory `reference_windows_games_deploy` aktualisieren.
- `IdentityTests`: lädt alle Szenen-Hint-Texte und den Namenskatalog, kein Treffer auf die Altnamen-Regex.
- Akzeptanz: grep-Beleg aus Erfolgskriterium 1 leer, Suiten grün, `VerifyDiabloZone` grün.

## S2 — Gold + Save

- `Wallet` (pure C#, `int Gold`, `Add`, `TrySpend`), am Helden wie `Inventory`. `GoldDrop`-Pickup
  (Normal 70 % 3–8 Gold, Elite 100 % 15–30, Chest 40–60), Konstanten in `DroptableDatabase`.
- `SaveGame` (`[Serializable]`, Versionsfeld `v=1`): Level, XP, freie Punkte, Attribute, Skill-Ränge,
  Equipment, Bag, Gold, `zoneReached`. `SaveService`: `Application.persistentDataPath/cinderreach-save.json`,
  `JsonUtility`, atomar (tmp + rename), Autosave bei Zonen-Übergang, Level-Up, Vendor-Schluss und Quit.
  Kaputte Datei → Backup `*.corrupt` + Neustart, Log-Warnung (kein stiller Verlust).
- Menü: „Continue“ nur bei vorhandenem Save, „New Game“ überschreibt nach Bestätigung.
- Tests: Roundtrip aller Felder, Versions-/Korrupt-Pfad, Gold-Pickup-Bilanz, Reload setzt Level/Gold fort.

## S3 — Lantern Hold + Vendor

- Camp-Teil aus `BuildDiabloZone` nach `TownBuilder` verschieben (gleiches Ergebnis, Verify unverändert grün
  = Refactor-Beleg), dann Vendor-NPC (KayKit-Adventurer-Modell, Idle-Take) + Interaktion per Klick/Radius.
- `VendorStock`: 6 Items, Seed pro Zonenbesuch, Preise nach Rarity (Magic 25, Rare 90, Unique 300),
  Verkauf 25 % des Preises, Heiltrank-Stack (neuer Item-Typ `Consumable`, Taste `1`).
- `DiabloVendorUI`: zur Laufzeit gebaut wie `DiabloInventoryUI`, zwei Spalten (Vendor / Bag), Gold-Anzeige,
  sperrt Klick-Bewegung solange offen (gleicher Guard wie Inventar).
- Tests: Kauf ohne Gold scheitert, Kauf/Verkauf-Bilanz, Bag voll blockiert Kauf, UI frisst Klicks.

## S4 — Charakterfortschritt mit Wahl

- Pro Level 1 Attributpunkt (Strength → Nahkampfschaden, Vitality → Life, Focus → Skill-Schaden/Cooldown)
  und 1 Skillpunkt (Rang 1–5 je Skill, +20 % Schaden pro Rang). Bestehendes `+3 Life/Level` bleibt als
  Basis.
- Dritter Skill `Cleave` (R, 360°-Nahkampf, 6 s Cooldown, Rang erweitert Radius). Icon über
  `SkillArtBuilder`.
- `DiabloCharacterUI` (Taste `C`), Level-Up-Banner zeigt „+1 Attribut, +1 Skill“, ungespendete Punkte
  pulsieren.
- Tests: Punktezählung über Level-Sprünge, Rang-Effekt auf `ApplyHit`-Schaden, Cleave trifft alle Husks im
  Radius, Punkte überleben Save-Roundtrip.

## S5 — Sunken Ossuary + Boss + Abschluss

- `OssuaryBuilder`: abgesetzter Bereich (z. B. Offset +200 m X), KayKit-Dungeon-Kit + Blender-Props über
  bestehende `WorldArt`-Pipeline. `ZoneGate` am Fen Gate: Trigger → Fade → Teleport, setzt
  `zoneReached = 2`, Autosave. Rückweg-Tor zur Town.
- `RangedCaster` (Bone Warden): hält 6–8 m Abstand, telegrafierter Projektilschuss (Wiederverwendung
  `DiabloProjectile`), 8 Stück + 2 Elites.
- `BossController` (The Hollow Abbot, ~150 HP, skaliert für Level 6): Phase 1 Nahkampf-Combo +
  Bodenkreis-Telegraph, Phase 2 ab 50 % beschwört 3 Husks + Projektilfächer. `LootTier.Boss` garantiert
  Rare+ und 150–250 Gold.
- `SessionComplete`: Boss-Tod → Overlay mit Spielzeit, Kills, Gold, bester Drop, Buttons „Weiter
  (zurück in die Town)“ / „Menü“.
- Verify: `VerifyDiabloZone` prüft zusätzlich Ossuary-Root, Gate-Paar, ≥ 8 Caster, Boss-Präsenz.
  dotfiles-`G-DIABLO02`-Vertrag um die neuen Prefabs/Objekte erweitern (eigener Worktree, `land`).
- Tests: Phasenwechsel bei 50 %, Boss-Drop ist Rare+, Gate teleportiert und speichert, Boss-Tod öffnet
  Abschluss.

## S6 — Beweis

- `SessionAutoplay` (`-autoplay`, inert ohne Flag): skriptgesteuerter Held läuft Wegpunkte, kämpft per
  `DiabloMelee`/Skills, kauft einen Trank, passiert beide Gates, tötet den Boss (Cheat nur: Gottmodus-Flag
  `-autoplay-god` für Determinismus, im Log ausgewiesen). Loggt Phasen + Exceptions-Zähler, Exit-Code 0/1.
- Linux-Player-Lauf mit `-autoplay` über `scripts/run-unity-build.sh`, Log ins Run-Log.
- `DiabloShotCheck` um Town, Vendor-UI, Character-UI, Ossuary, Boss Phase 2, Abschluss erweitern. Windows:
  `scripts/stage-windows.sh` → `scripts/shotcheck-windows.sh`, PNGs lesen und im Run-Log verlinken.
- Docs: `decision-log.md` (Q9 „trägt die Session-Schleife?“ → continue/pivot/kill), `dev-journal.md`,
  `mechanics-questions.md`, `one-page-gdd.md`.
- dotfiles: `PROGRESS.md` auf 100 % nur mit allen 8 Kriterien belegt.

## Session-Protokoll (für jede ausführende Session)

Eine Session übernimmt genau **eine** Stufe. Sie arbeitet sie in einem Durchlauf bis `land` ab und
meldet dann Done (Single-pass, kein Stopp vor dem Merge).

1. **Lage prüfen:** `git -C ~/unity-sandbox fetch && git -C ~/unity-sandbox status --short`. Ist der
   Hauptcheckout nicht sauber (Ausnahme: die oben genannten Testlauf-Reste), nicht starten: fremde Arbeit
   läuft. Die Vorbedingung gilt als erfüllt, wenn `git -C ~/unity-sandbox log origin/main --oneline --
   sandbox/Assets/Tests/EditMode/DiabloHdTests.cs` einen Commit zeigt.
2. **Stufe wählen:** In der Statustabelle unten die erste Stufe mit Status `offen`, deren Abhängigkeiten
   `landed` sind. Dann `python3 ~/scripts/agent-workspace.py lock list` prüfen und nur nehmen, wenn kein
   Lock `task arpg-session-loop-sN` existiert.
3. **Anlegen + claimen:**
   `python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/arpg-session-loop-sN`.
   Danach im Worktree-Root: `python3 ~/scripts/agent-workspace.py lock claim task arpg-session-loop-sN
   --label "<harness/session>"`.
4. **Bauen** nach dem Stufenabschnitt. Kontext liefern dieser Plan, `GOAL.md`, `docs/dev-journal.md`
   (letzte 3 Einträge) und die genannten Dateien. Die Szene nur über Builder-Methoden regenerieren, nie
   `DiabloZone.unity` von Hand editieren.
5. **Verify** nach dem Abschnitt unten, alles im Worktree-Root. `run-unity-tests.sh` stoppt den
   `unity-sandbox-editor.service` und hält den Unity-Lock: Exit 3 heißt, eine andere Session testet
   gerade. Dann warten und erneut versuchen, nicht parallel erzwingen.
6. **Doku im selben Commit:** Zeile der Statustabelle auf `landed <YYYY-MM-DD>`, Eintrag in `docs/dev-journal.md`, in
   `.agents/goals/arpg-session-loop/PROGRESS.md` „Erledigt“ mit Belegen ergänzen und Prozent laut
   Gewichtung setzen.
7. **Commit + land:** `git commit` (Conventional Commits, `feat(sN-...)`), dann im Worktree
   `python3 ~/scripts/agent-workspace.py land`. Exit 1 nur wegen `getcwd` nach entferntem Worktree ist
   harmlos. Danach `git -C ~/unity-sandbox log -1 origin/main` prüfen. Den Lock gibt `land` frei,
   sonst `lock release task arpg-session-loop-sN`.
8. **dotfiles-Anteile** (S1, S5: `scripts/health_probes/diablo_unity.py` + Test) laufen in einem eigenen
   dotfiles-Worktree: `create --repo ~ fix/diablo-probe-sN`, dann `python3 scripts/agent-workspace.py
   check`, `python3 -m pytest scripts/test_diablo_unity_health.py -q`, `land`. Landen **nach** der
   unity-sandbox-Stufe, im selben Durchlauf.
9. **Blocker:** Ist eine Stufe nicht abschließbar, Lock freigeben, Status auf `blocked: <Grund>` setzen
   (eigener `chore(plan)`-Commit + `land`) und den Grund in `PROGRESS.md` unter „Offene Punkte“
   eintragen.

Parallelität: S3 und S4 dürfen nach S2 parallel laufen. Ihre `target_files` sind disjunkt bis auf
`Scripts/DiabloProgression.cs` (S4) bzw. Save-Felder (S2, bereits gelandet). Unity-Testläufe
serialisieren sich über den Projekt-Lock (Exit 3). Alle anderen Stufen laufen seriell.

### Statustabelle

| Stufe | Abhängig von | Gewicht | Status |
|---|---|---|---|
| S1 identity | Vorbedingung | 10 % | landed 2026-11-17 |
| S2 economy+save | S1 | 15 % | offen |
| S3 town+vendor | S2 | 15 % | offen |
| S4 progression | S2 | 15 % | offen |
| S5 zone2+boss | S3, S4 | 25 % | offen |
| S6 proof | S5 | 20 % | offen |

### Definition of Done je Stufe

- Akzeptanzpunkte des Stufenabschnitts durch Tests belegt (Testnamen im Journal).
- Verify 1–4 grün, bei dotfiles-Anteil auch 5. Testzahlen (EditMode x/x, PlayMode y/y) im Journal.
- Keine Altnamen neu eingeführt (`grep -rniE "blood ?moor|rogue encampment|moor gate"` ab S1 leer).
- Neue Laufzeit-UIs nutzen die HD-Scaler-Konvention, `DiabloHdTests` erweitert.
- `.meta` zu jeder neuen Datei (G-DIABLO01).

## Verify (je Stufe, vor Commit)

1. `scripts/run-unity-tests.sh` (EditMode + PlayMode, NUnit-XML `Passed`).
2. `Unity -batchmode -executeMethod SandboxBuilder.VerifyDiabloZone` grün, `KayKitAssets.Verify` grün
   (`repo.yaml:verify`).
3. `python3 scripts/check-meta.py` ohne Befund (G-DIABLO01).
4. Host-Start-Lauf des Zonen-Builds: 0 Exceptions.
5. Bei dotfiles-Änderungen: `python3 scripts/agent-workspace.py check` + `python3 -m pytest
   scripts/test_diablo_unity_health.py -q` im dotfiles-Worktree.
6. Nach `land`: `python3 ~/scripts/repo_health_collect.py` → `G-DIABLO01/02/03/05` grün.

## Risiken

- R1 Builder-Monolith: Auslagerungen (Town/Ossuary) ändern Szenen-Diffs stark. Gegenmittel: Verify-Counts
  vor/nach Refactor vergleichen, Szene nur per Builder regenerieren.
- R2 Warm-Editor-Library: Library nie bei laufendem `unity-sandbox-editor.service` kopieren
  (dev-journal 2026-11-17), sonst rote Szenentests.
- R3 Solo = NGO-Host: neue Systeme über `Diablo.IsLocalAuthority`/`IsSolo` gaten, Boss-Summons
  host-autoritativ spawnen wie Loot.
- R4 Autoplay-Determinismus: feste RNG-Seeds für Loot/Vendor im Autoplay, God-Flag nur dort.
- R5 Rename bricht G-DIABLO05 zwischen Stage und Probe-Update. Gegenmittel: Probe-Änderung in dotfiles
  im selben S1-Durchlauf landen.
- R6 Fremdarbeit im Hauptcheckout: `land` merged per ff im Hauptcheckout und kollidiert mit ungestagten
  Änderungen an denselben Dateien. Gegenmittel: Protokollschritt 1. Nie fremde Änderungen stashen,
  committen oder verwerfen.
