# AGENTS.md für Unity Sandbox

Die gemeinsame Arbeitsweise steht in `~/AGENTS.md` und `~/agentic-resources/AGENTS.md`. Der lokale Vertrag für Branch, Prüfungen und Fast-Forward-Abschluss steht in `repo.yaml`.

## Unity-Arbeit

- Für C#-Compilerdiagnosen im Terminal nutze `unity --json recompile "$PWD/sandbox"` aus dem Repo-Root. Prüfe den Exitcode sowie `data.errors` und `data.warnings`. Der Befehl braucht einen laufenden Editor für genau diesen Projektpfad. `unity status` zeigt die verbundenen Editoren. Ohne Editor für den Worktree nutze den Batchmode der in `sandbox/ProjectSettings/ProjectVersion.txt` angegebenen Unity-Version über den Build- oder Testrunner.
- Für Editor-Konsole und kleine C#-Abfragen nutze `Unity_GetConsoleLogs` und `Unity_RunCommand` im MCP-Server `unity-sandbox`. Die globale MCP-Verbindung zeigt auf den Hauptcheckout. Verwende für Prüfungen von Worktree-Code den Unity-CLI-Aufruf mit dem Worktree-Projektpfad.
- Arbeite in einem Worktree unter `~/.worktrees`. Anlage und Abschluss laufen über `python3 ~/scripts/agent-workspace.py create --repo unity-sandbox <branch>` und `python3 ~/scripts/agent-workspace.py land`.
- Lese vor Änderungen den Ist-Stand in `.agents/goals/CINDERREACH.md` sowie die aktiven Ziele und Stufenpläne in `.agents/`. Abgeschlossene Ziele werden dort eingearbeitet und danach entfernt. Plane Save-Migrationen auf Basis von aktuellem `main`.
- Nutze `bash scripts/run-unity-tests.sh` für EditMode und PlayMode (beim Iterieren gefiltert: `PLATFORMS=EditMode TEST_FILTER=<Klasse> bash scripts/run-unity-tests.sh`; Fehlschläge und langsamste Tests druckt `scripts/unity_test_report.py`, getestet via `python3 -m pytest scripts -q`), `python3 scripts/check-meta.py` für Asset-Metadaten und `bash scripts/run-unity-build.sh` für Editor-Builds und Szenenverifikation. Das Spielprojekt liegt unter `sandbox/`. Die Editor-Version steht in `sandbox/ProjectSettings/ProjectVersion.txt`.
- Installiere die Repository-Hooks bei einem frischen Checkout mit `python3 ~/scripts/agent-workspace.py install --repo ~/unity-sandbox`. Erzeuge die lokalen Skill-Links mit `python3 ~/scripts/agent-resources.py link-consumers`. Unity-spezifische Skills stehen im Home-Katalog. Die lokale Skill-Verknüpfung bleibt unversioniert.
