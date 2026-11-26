# Goal: cinderreach-hotpath-caching

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
health_probe: {"id":"G-DIABLO08","title":"No per-frame Camera.main or transform.Find in LootDropView.LateUpdate and DiabloClickController.Update/UpdateCamera","cmd":"cd ~/unity-sandbox && python3 scripts/check-hotpath-caching.py >/dev/null 2>&1; echo $?","target":0}

## Objective

Per-Frame-Lookups (`Camera.main`, `transform.Find`, `GetComponent<DiabloInventoryUI>()`) in `LootDropView.LateUpdate` (`sandbox/Assets/Scripts/Loot/LootDropView.cs:98-105`) und `DiabloClickController.Update`/`UpdateCamera` (`sandbox/Assets/Scripts/DiabloClickController.cs:255-263,402-409`) auf Basis von `main` (`5bd41d3`) durch sauber invalidiertes Referenz-Caching ersetzen und dauerhaft über `scripts/check-hotpath-caching.py` (`G-DIABLO08`) absichern. Ausgangslage: [CINDERREACH.md](../CINDERREACH.md).

## Erfolgskriterien

1. `LootDropView` cacht `cachedBall`, `cachedIcon` (in `Show()` nach dem Aufräumen aktualisiert) und `cachedCam` (mit Null-Fallback bei Kamerawechsel); `LootDropView.LateUpdate` ruft bei gültigen Referenzen weder `Camera.main` noch `transform.Find` auf.
2. `DiabloClickController` cacht `cachedCam` (mit Null-Fallback bei Kamerawechsel) und `cachedInventoryUi`; `Update` und `UpdateCamera` rufen bei gültigen Referenzen weder `Camera.main` noch `GetComponent<DiabloInventoryUI>()` pro Klick oder Frame auf.
3. Statische Probe `scripts/check-hotpath-caching.py` (inkl. pytest unter `scripts/`) prüft beide Dateien, liefert bei sauberem Stand Exit 0 und schlägt bei Regressionen fehl (`G-DIABLO08`).
4. Öffentliche Signaturen (`LootDropView.ShowOn`, `Show`, `LootBall`/`ItemIcon`-Hierarchienamen, `DiabloClickController`-API) bleiben unverändert; `ItemPresentationTests`, vollständige Unity EditMode/PlayMode-Suiten (`bash scripts/run-unity-tests.sh`), `python3 scripts/check-meta.py` und `python3 -m pytest scripts -q` sind grün.

## Scope

- `sandbox/Assets/Scripts/Loot/LootDropView.cs`
- `sandbox/Assets/Scripts/DiabloClickController.cs`
- `scripts/check-hotpath-caching.py` und zugehöriger pytest unter `scripts/`

## Nicht-Ziele

- Keine Änderungen an Loot-Drop-Raten, Kamera-Winkeln, Eingabebindungen oder Netz-/Save-Verträgen.
- Keine Änderungen an anderen Skripten außerhalb der zwei Hot-Path-Dateien.

## Abhängigkeiten

- `unity-sandbox main` (Basis `5bd41d3`), Unity `6000.0.85f2`, bestehende Gates `G-DIABLO01/02/03/06`.
