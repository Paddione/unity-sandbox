# Build-Speed-Runbook (Goal unity-build-speed-5x)

Stand: 2026-11-27. Baseline (Commit `369186f`, UTP-`PlayerBuildInfo`):
linux-player 26,9 s / 194 MB, windows-player 100,3 s / 190 MB,
linux-server 94,8 s / 112 MB. Größter Anteil überall Script-Assemblies
(14,6 s / 62,0 s / 36,4 s), danach Postprocess (windows 23,5 s / server 27,1 s /
linux 3,9 s). Import kostet bei warmer `Library/` ~0 (0 `Start Importing`-Zeilen).

## 1. Incremental ist Default, Clean ist Opt-in (Task 2)

- Build-Outputs werden nie gelöscht: `run-unity-build.sh` fasst `builds/` nicht an.
- Expliziter Clean-Build: `UNITY_CLEAN_BUILD=1 UNITY_CLEAN_DIRS="builds/diablo-zone:…"`
  (repo-relativ, Doppelpunkt-getrennt). Verweigert werden: absolute Pfade, `..`,
  `Library/`, `Assets/`, `ProjectSettings/`, `Packages/`, `sandbox/`, `scripts/`.
- Matrix-Messung: `python3 scripts/measure-build.py [--note TEXT] [--targets …]`
  hängt JSON-Zeilen an `builds/build-metrics.jsonl` an (ignored, Arbeitsdatei;
  belegte Zahlen zusätzlich in die Goal-`PROGRESS.md` übernehmen).
- Worktree-Builds landen per `-buildOut` im eigenen Worktree (`measure-build`
  setzt ihn automatisch); `SandboxBuilder.{BuildDiabloZoneBuild,BuildDiabloZoneWindows,
  BuildDiabloZoneServer}` akzeptieren `-buildOut <pfad>`.

## 2. Backend-Matrix (Task 3)

- Alle drei Matrix-Targets (Linux/Windows/Server) bauen mit **Mono**
  (`ScriptingImplementation.Mono2x`, in den Build-Methoden explizit gepinnt):
  schnellste Iteration und schnellste Release-Builds auf Standalone.
- **IL2CPP** nur dort, wo Plattform-Policy es verlangt (Konsolen/Mobile/WebGL):
  dann immer in denselben Output-Ordner bauen (inkrementelle C++-Kompilierung),
  nie löschen. Befund 2026-11-27: `scriptingBackend` stand nur für Android;
  alle Matrix-Artefakte enthalten `Managed/Assembly-CSharp.dll` (Mono-Beleg).
- Abweichung vom Setgoal-Votum (`IL2CPP-Release`) begründet in Goal-`PROGRESS.md`:
  Releases sind heute Mono und laufen (u. a. Dedicated Server auf gpu-metal);
  IL2CPP würde nur die Buildzeit vervielfachen.

## 3. Assembly-Split (Task 4)

- Audit 2026-11-27 (`/tmp/depgraph2.py`, Kommentare/Strings gestrippt): Alle 20
  Ordner (`Scripts/*` + Root) bilden EINE Strongly Connected Component — ein
  Per-Folder-`.asmdef`-Schnitt ist ohne Code-Umbau unmöglich (Zyklen garantiert).
- Stattdessen dateigranular: 13 Pure-Leaf-Dateien (keine projektinternen Referenzen,
  keine `partial`s) → `Scripts/Foundation/` (`Sandbox.Foundation`, keine Refs);
  2 Dateien, die nur Foundation referenzieren (`HitStop.cs`, `HuskAnimator.cs`) →
  `Scripts/Core/` (`Sandbox.Core` → Foundation). Compiler verifiziert Kreisfreiheit
  (`CompileCheck` + Tests). Tests-`.asmdef`s referenzieren beide explizit.
- Gewinn: Edits an diesen 15 Dateien (u. a. `Diablo.cs`, `Wallet.cs`,
  `HeroAppearance.cs`) kompilieren nur die kleine Assembly statt alles.
- Nicht-Ziel bleibt: kein Namespace-/Layer-Refactor des Rests (Follow-up).

## 4. Accelerator + Library (Task 5)

- Betriebsort: diese Maschine als Docker-Container (`unitytechnologies/accelerator`,
  persistent in `~/.local/share/unity-accelerator`); Steuerung:
  `bash scripts/unity-accelerator.sh up|status|down` (Cache-Port 127.0.0.1:10080,
  Dashboard 127.0.0.1:10081).
- Nutzung per Env (unset = unverändert): `UNITY_ACCELERATOR=127.0.0.1:10080
  [UNITY_ACCELERATOR_NAMESPACE=unity-sandbox]` — `run-unity-build.sh` hängt dann
  `-cacheServerEndpoint/-cacheServerNamespacePrefix` + Download/Upload-Flags an.
  Beleg: `Using cacheserver namespaces - metadata:unity-build-speedmetadata,
  artifacts:unity-build-speedartifacts` im Build-Log (CompileCheck-Probe).
- `Library/` (Worktree: 4,3 GB, warm) wird nie gelöscht: kein Skript fasst sie an,
  `UNITY_CLEAN_DIRS` verweigert sie explizit. Import kostet warm ~0 (0
  `Start Importing`-Zeilen in allen Baseline-Builds); der Accelerator sichert
  kalte Checkouts/CI ab. Builds laufen ohne Accelerator lauffähig (Cache ist
  Beschleuniger, keine harte Abhängigkeit).

## 5. Addressables (Task 6)

- (Wird hier ergänzt: Gruppen, Content-Update-Ablauf.)
