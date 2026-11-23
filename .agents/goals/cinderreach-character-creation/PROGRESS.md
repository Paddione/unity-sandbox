# Fortschritt – cinderreach-character-creation

Prozent: 100 %

## Erledigt

- 2026-11-23: setgoal-Runde beantwortet (alle Empfehlungen übernommen: Gender als Profil + Silhouette ohne neues FBX; Frisuren + Farbpalette; 3 Build-Stufen; DE-Namenspools + Würfel + Edit, Heldenname getrennt vom Accountnamen; nur Erstellung, Save v14 + Koop; health_goals G-DIABLO01/02/03/06).
- 2026-11-23: Ist-Stand belegt (HEAD `f0ce4aa`): `ClassSelectUI` mit `ChooseNew`/`Continue`/`ShouldShow`, `ClassAppearance` (Knight/Mage-Re-Skin, ein Rig), Save v13, `DiabloNetworkPlayer.PublishClass`, `HeroNameplates`, `PlayerAccount` getrennt.
- 2026-11-23: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und Worktree `feature/cinderreach-character-creation` erstellt; Eigner-Auftrag „persistent machen und verfolgen“ erteilt.
- Profil & Namen (`Classes/HeroAppearance.cs`): `HeroProfile` (Name/Gender/Frisur/Haarfarbe/Build), DE-Namenspools (16 weiblich/16 männlich/16 neutral, Würfel je Gender), `HeroNames.Clean` (2–16 Zeichen), 6 Frisuren, 10 Haarfarben, 3 Build-Stufen, `GetProportions` (Faktoren 0,85–1,25, rein kosmetisch), Netz-Packung `Pack`/`Unpack`.
- Save v14: `SaveGame.CurrentVersion` 13→14, `V13ToV14`-Migration (Default-Look + deterministischer Zufallsname aus Run-Seed/Level, Accountname unberührt), `RunProfile.Hero`, Roundtrip in `SaveService` (Capture/Apply inkl. Look-Anwendung), Reload-Pfad über `SaveService.NewHeroProfile`.
- Aussehen (`ClassAppearance.Apply(cls, profile)`): Gender-/Build-Knochenproportionen (chest/spine, shoulder/upperarm), Haar-Aufsätze am `head`-Knochen je Frisur mit Farb-Tint, Helm verdeckt das Haar (`LateUpdate`), Klassen-Re-Skin unverändert.
- Koop: `DiabloNetworkPlayer.appearanceCode` (Owner, gepackt) + `heroName` (Server, `HeroNames.Clean`), Spiegel wenden Klasse + Profil an (`ApplyClassIndex`), `ShownName` zeigt den Heldennamen vor dem Accountnamen, Late-Join über die NetworkVariables.
- UI: Erstellungs-Schritt `HeroCustomizePanel` im `ClassSelectUI` (Klasse → Gender/Frisur/Haarfarbe/Build/Name → Start) mit Zyklus-Buttons, Live-Vorschau am Helden (`PreviewDraft`), Würfel-Button, Namensfeld mit Regel-Hinweis bei ungültigem Namen; `-class`/Autoplay/Shotcheck/Koop-Peers weiter ohne Schritt (Defaults), `ChooseNew` als Direkt-Seam unverändert.
- Tests: EditMode 504/504 (neu `HeroAppearanceTests` 6/6, `SaveMigrationTests` v13→v14 + Held-Roundtrip), PlayMode 290/290 (neu `ClassSelectTests.Customize_*`, Continue mit Heldenprofil), `KayKitAssets.Verify` Exit 0, `python3 scripts/check-meta.py` OK, `pytest scripts` 6/6, `agent-workspace.py check` still, `goal-lint` ohne Befund für dieses Goal (Fremdbefund `cinderreach-equipment-art: G2` besteht auf `main`).
- Doku: Entscheid in `docs/decision-log.md`, Ist-Stand in `.agents/goals/CINDERREACH.md` nachgezogen.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
