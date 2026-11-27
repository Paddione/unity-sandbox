# AGENTS.md für Unity Sandbox

Die gemeinsame Arbeitsweise steht in `~/AGENTS.md` und `~/agentic-resources/AGENTS.md`. Der lokale Vertrag für Branch, Prüfungen und Fast-Forward-Abschluss steht in `repo.yaml`.

## Unity-Arbeit

- Für C#-Compilerdiagnosen im Terminal nutze `unity --json recompile "$PWD/sandbox"` aus dem Repo-Root. Prüfe den Exitcode sowie `data.errors` und `data.warnings`. Der Befehl braucht einen laufenden Editor für genau diesen Projektpfad. `unity status` zeigt die verbundenen Editoren. Ohne Editor für den Worktree nutze den Batchmode der in `sandbox/ProjectSettings/ProjectVersion.txt` angegebenen Unity-Version über den Build- oder Testrunner.
- Für Editor-Konsole und kleine C#-Abfragen nutze `Unity_GetConsoleLogs` und `Unity_RunCommand` im MCP-Server `unity-sandbox`. Die globale MCP-Verbindung zeigt auf den Hauptcheckout. Verwende für Prüfungen von Worktree-Code den Unity-CLI-Aufruf mit dem Worktree-Projektpfad.
- Arbeite in einem Worktree unter `~/.worktrees`. Anlage und Abschluss laufen über `python3 ~/scripts/agent-workspace.py create --repo unity-sandbox <branch>` und `python3 ~/scripts/agent-workspace.py land`.
- Lese vor Änderungen den Ist-Stand in `.agents/goals/CINDERREACH.md` sowie die aktiven Ziele und Stufenpläne in `.agents/`. Abgeschlossene Ziele werden dort eingearbeitet und danach entfernt. Plane Save-Migrationen auf Basis von aktuellem `main`.
- Nutze `bash scripts/run-unity-tests.sh` für EditMode und PlayMode (beim Iterieren gefiltert: `PLATFORMS=EditMode TEST_FILTER=<Klasse> bash scripts/run-unity-tests.sh`; Fehlschläge und langsamste Tests druckt `scripts/unity_test_report.py`, getestet via `python3 -m pytest scripts -q`), `python3 scripts/check-meta.py` für Asset-Metadaten und `bash scripts/run-unity-build.sh` für Editor-Builds und Szenenverifikation. Das Spielprojekt liegt unter `sandbox/`. Die Editor-Version steht in `sandbox/ProjectSettings/ProjectVersion.txt`.
- Installiere die Repository-Hooks bei einem frischen Checkout mit `python3 ~/scripts/agent-workspace.py install --repo ~/unity-sandbox`. Erzeuge die lokalen Skill-Links mit `python3 ~/scripts/agent-resources.py link-consumers`. Unity-spezifische Skills stehen im Home-Katalog. Die lokale Skill-Verknüpfung bleibt unversioniert.

## Agenten-Regeln R1–R7

Diese Regeln gelten für jede Arbeit an diesem Repo. Die Projektstand-Ampel auf
`/unity` wertet sie aus; jede Regel nennt das prüfbare Signal. Quellen sind der
Repo-Zustand und die versionierten Belege unter `docs/`.

- **R1 Worktree-Pflicht.** Gearbeitet wird ausschließlich in einem Worktree unter
  `~/.worktrees`, nie im Hauptcheckout. Signal: `git -C ~/unity-sandbox status
  --porcelain` ist leer (gitignorierte Pfade zählen nicht). Ein Commit verrät
  seine Herkunft nicht; die leere Statusausgabe ist die Näherung.
- **R2 Mess-Pflicht.** Jeder Commit, der `sandbox/Assets/Editor/SandboxBuilder.cs`,
  ein `*.asmdef`, `sandbox/Packages/manifest.json`, `sandbox/ProjectSettings/*`
  oder `scripts/run-unity-build.sh` ändert, braucht eine Zeile in
  `docs/build-metrics.jsonl`, deren `commit` dieser Commit oder ein Nachfolger
  ist (der Build lief nach der Änderung). Signal: letzter Commit je Pfadmenge,
  danach Abgleich mit der neuesten Metrikzeile.
- **R3 PROGRESS-Pflege.** Ein aktives Ziel (< 100 %) mit Code-Commits nach der
  letzten PROGRESS-Änderung, die älter als 7 Tage ist, wird gelb. Signal:
  Prozent aus `PROGRESS.md`, `git log -1 --format=%ct -- PROGRESS.md`, danach
  Code-Commits im selben Repo seit diesem Zeitpunkt.
- **R4 Test-Gate.** Der letzte Eintrag je Plattform in `docs/test-runs.jsonl`
  hat `failed == 0` und ein `commit`, das nicht älter ist als der letzte Commit
  unter `sandbox/Assets/Scripts`. Signal: JSONL-Zeile plus `git log -1
  --format=%ct -- sandbox/Assets/Scripts`.
- **R5 Abschluss per `land` (ff).** Kein Merge-Commit auf `main` seit
  2026-11-27. Signal: `git log --merges --oneline main` ist leer.
- **R6 Accelerator opt-in.** Kein Accelerator-Endpunkt steht fest in
  `sandbox/ProjectSettings` oder `sandbox/Assets/Editor`. Signal: grep auf
  `10080|cacheServerEndpoint|cacheServerNamespace` in beiden Pfaden findet
  nichts; die Nutzung läuft nur über `UNITY_ACCELERATOR` in
  `scripts/run-unity-build.sh`.
- **R7 Addressables nur über `AddressablesSetup.Ensure`.** Außerhalb von
  `sandbox/Assets/Editor/AddressablesSetup.cs` mutiert niemand
  `AddressableAssetSettings`. Signal: grep `AddressableAssetSettings` unter
  `sandbox/Assets` findet nur Treffer in dieser Klasse.

Ampel: Grün = Signal erfüllt. Gelb = Beleg veraltet (R2 Zeile älter als der
Pflicht-Commit, R3 älter als 7 Tage, R4 ohne Eintrag oder älter als der letzte
Script-Commit). Rot = Regel verletzt (R1 Status nicht leer, R2 Zeile fehlt,
R4 letzter Lauf mit Fehlern, R5 Merge-Commit, R6 Endpunkt, R7 Fremdzugriff).
