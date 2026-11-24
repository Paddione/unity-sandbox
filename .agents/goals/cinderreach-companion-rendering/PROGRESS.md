# Fortschritt – cinderreach-companion-rendering

Prozent: 100 %

## Erledigt

- 2026-11-24: setgoal-Runden 1+2 beantwortet (alle Empfehlungen übernommen), Goal persistent angelegt.
- 2026-11-24: Begleiter rendern als Helden (`Classes/CompanionFactory.cs` neu: Klon des Live-Helden-Rigs + Gameplay-Strip + frischer State; `Party/PartyRoster.cs` ohne Kapsel-Primitive; `Party/PartyCompanion.cs` mit `Live`-Registry, `DisplayName` und `Moving`-Antrieb; `Net/HeroNameplates.cs` zeigt Begleiter-Namen; `Classes/ClassAppearance.cs` `autoApply=false` für Klone).
- 2026-11-24: Platzhalter ersetzt (`Enemies/HuskModelLibrary.cs` neu: Builder-Modell, sonst Szenen-Template, sonst URP-Kapsel, nie bare): `Zones/RiskShrine.cs`-Elite, `Enemies/BossController.cs`-Summons (+`summonModel`, `Editor/OssuaryBuilder.cs` weist Skeleton_Minion zu), `Enemies/KilnmotherController.cs`-Thralls; Schrein als Pfeiler + Fackel + Runenscheibe (`Editor/RiskShrineBuilder.cs`).
- 2026-11-24: Gender-Basen (`Classes/ClassAppearance.cs`: `SkinModelPath` Fenwalker w→Ranger, Rogue w→Rogue_Hooded, Rogue m/nb→Rogue, Lanternbearer→Mage, sonst Knight-Basis; stabile Skin-Wurzel mit `ClearSkin`/`MapSkin`; `Editor/SandboxBuilder.cs` verdrahtet alle vier Modell-Refs, alte Szenen fallen auf die Basis zurück).
- 2026-11-24: Erstellungs-Vorschau (`Menu/HeroPreviewView.cs` neu: Bühnen-Kamera + RenderTexture + Drehteller weit außerhalb der Welt; `Menu/ClassSelectUI.cs` zeigt/versteckt sie mit dem Draft; `menu.create.preview` in `LocCatalog.Menu.cs` DE/EN).
- 2026-11-24: Tests neu: `CompanionLookTests` (Mapping, Profile, Skin-Wechsel), `HuskModelLibraryTests` (Modell-Präferenz, Template-Fallback, nie bare), `CompanionRenderTests` (Klon statt Kapsel, Folgen, Fallback-URP), `HeroPreviewTests` (Vorschau lebt/folgt/schließt). Bestehender `ClassItemTests`-Skin-Fall durch stabile Skin-Wurzel geheilt (statt Test anzufassen).
- 2026-11-24: `DiabloZone.unity` + `NetworkPlayerDiablo.prefab` kanonisch neu gebaut (`SandboxBuilder.BuildDiabloZone` headless): Schrein-Prop, `eliteModel`/`summonModel`, alle vier `ClassAppearance`-Refs auf Held und Prefab belegt; Blob-Delta 0,2 %, kein Churn.
- 2026-11-24: Gates grün auf dem Endstand: `agent-workspace.py check` still, EditMode 539/539, PlayMode 303/303, `check-meta.py` OK, `KayKitAssets.Verify` Exit 0, `VerifyDiabloZone` OK (67 Husks bewaffnet, Schrein auf Anker, classLook). Entscheid in `docs/decision-log.md`.

## Nächste Schritte

- Keine (offen nur als Follow-up, kein Kriterium): visueller Windows-GPU-Playtest mit Screenshot (Schrein, Begleiter, weibliche Modelle, Vorschau) per `shotcheck-windows.sh --autoplay`.

## Offene Punkte

Keine.
