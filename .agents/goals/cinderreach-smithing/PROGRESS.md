# Progress: cinderreach-smithing

Prozent: 100 %

## Erledigt

- Ziel und Stufenplan angelegt.
- S1: ItemInstance.affixTier/reforgeIndexPlusOne; Tier-Erfassung am Drop (LootRoll/LootDrop), Vendor (VendorStock) und Kindled-Annahme dokumentiert; Save v6 (SaveGame/RunProfile/SaveService) mit v4→v6- und v5→v6-Migration (Tier-1-Defaults, Rollwerte erhalten); SmithingService (Salvage 1/3, Reforge 3+50g, ein Slot dauerhaft, Uniques ausgenommen, Vorschau=Validierung, In-Flight-Sperre, Snapshot-Rollback); EditMode-Tests (SmithingServiceTests, SaveMigration-v6) und PlayMode-Verträge (SmithingLoopTests inkl. Normal-/Kindled-Fund bis Verbesserung).
- S2: SmithStation (Lantern Hold, gleiche Reichweite wie Vendor) + DiabloSmithUI (HD-Skalierung 1920×1080, Taschenliste mit Ertrag, Preis/Eigenschaft/Ersatz/Spanne, Rollwert + Kontostände nach Erfolg).
- S3 Integration/Nachweis (Übernahme durch opencode-Session ses_e115042b; Rebase auf v5-main 6a33815, Konflikte SaveGame/RunProfile/SaveMigrations/SaveService/SaveMigrationTests von Hand aufgelöst: Kette v4→v5→V6, V4ToV6-Direktsprung entfernt, Versionstest auf 6):
  - `bash scripts/run-unity-tests.sh` Exit 0: EditMode 244/244, PlayMode 104/104 (`/tmp/sm-full2.log`).
  - `TownBuilder.SpawnSmithStation` + Build-Hook + Verify-Gate (gespiegelt zum Brett-Muster); `BuildDiabloZone` + `VerifyDiabloZone` + `KayKitAssets.Verify` je Exit 0 (`/tmp/sm-bv.out`).
  - `bash scripts/run-autoplay.sh "artifacts/autoplay/cinderreach-smithing-normal" -seed 0` Exit 0: fps-gate PASS (worst avg 38,5 ≥ 30,0; `/tmp/sm-autoplay.log`).
  - Perf: `SmithStation.Update` ohne `Camera.main` pro Frame (statisch gecacht, Klick-Gate zuerst — gleiche Stelle wie #7, ~2 fps).
  - `python3 scripts/check-meta.py` Exit 0, `agent-workspace check` still vor Commit; vier Builder-LFS-Seiteneffekte rever­tiert.
  - S2-Spieldurchlauf je Fund: `NormalFund_PlayedThroughToImprovement` + `KindledFund_PlayedThroughToImprovement` (PlayMode, grün).

## Nächste Schritte

Keine — alle drei Stufen belegt und gelandet.

## Offene Punkte

Keine.
