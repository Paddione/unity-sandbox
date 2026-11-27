---
goal: cinderreach-graph-adr-hygiene
slug: cinderreach-graph-adr-hygiene
branch: feature/cinderreach-graph-adr-hygiene-s<N>
repo_main: main
tickets: none
health_goals: [G-GOALS01]
priority: P7 (unabhängig, nur Doku/Hygiene, keine Entblockung; jederzeit als Füller parallel möglich)
status: in_progress — S1 gelandet (8e781a7, 20 %), nächster Schritt S2 parse-fix
start_condition: main sauber; fleet-workspace-Hub erreichbar (index_repository/index_status/manage_adr); kein paralleles Schreiben an .agents/goals/** durch ein anderes Goal
plan_refs: [.agents/goals/cinderreach-graph-adr-hygiene/GOAL.md]
---

# Plan: cinderreach-graph-adr-hygiene — Graph- und ADR-Hygiene schließen

Pfad: feature, 5 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-graph-adr-hygiene-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).
Reihenfolge ist Abhängigkeitsreihenfolge; keine Stufe lässt Lint, Meta-Check oder Skript-Suiten rot zurück.

## Goal

Siehe [GOAL.md](../../goals/cinderreach-graph-adr-hygiene/GOAL.md): 45 `missing`- und
179 `ambiguous`-`doc_links` aus dem A2/A3-Audit (Revision `5bd41d3`) bereinigen,
Bash-`parse_partial` in `scripts/run-coop-pair.sh:92` ohne Verhaltensänderung glätten und
einen verbindlichen ADR-Aktualisierungsprozess in `docs/` verankern.

## Success Criteria

Die vier Erfolgskriterien aus GOAL.md, je Stufe unten mit Mess-Beleg verknüpft.
Gesamt-Gate S5: Re-Index mit `parse_partial.count == 0` und
`doc_links.unresolved.missing == 0`, `bash -n scripts/run-coop-pair.sh` grün,
`node ~/scripts/goal-lint.mjs --repo .`, `python3 scripts/check-meta.py` und
`python3 -m pytest scripts -q` ohne Fehler.

## Ausgangslage (Belege, HEAD `8e59d00`, 2026-11-27)

- Audit-Basis ist `5bd41d3`; seitdem u. a. `aaa2571` (Class-Kits), `443d7c0`
  (Hot-Path-Caching) und `8e59d00` (Goal in `CINDERREACH.md` eingearbeitet).
  Die Audit-Zahlen (45/179) sind daher Startwerte, keine aktuelle Messung — S1 misst neu.
- `scripts/run-coop-pair.sh` (185 Zeilen): `bash -n` ist heute grün, die Datei ist
  also lauffähig; `parse_partial` ist eine Indexer-Grenze, kein Shell-Fehler.
  Auffällige Konstrukte um Zeile 92: Array-Expansionen `"${LAUNCH[@]}"`,
  `"${COMMON[@]}"`, das gesetzte-Array-Idiom `${CLIENT_GOD[@]+"${CLIENT_GOD[@]}"}`
  bzw. `${SERVER_PEERS[@]+"${SERVER_PEERS[@]}"}` und `${cls:+...}` im `start_client`-Aufruf.
- `docs/runbook.md` existiert, ist aber das Gameplay-Runbook (Concept → Prototype →
  Vertical Slice, Server-Rebuild, Konten). Keine Graph-/ADR-Pflege darin.
- Zentraler Hub `fleet-workspace` stellt `index_repository`, `index_status`
  (`diagnostics="full"`) und `manage_adr` bereit; Projektname `home-patrick-unity-sandbox`.
- Nicht-Ziele aus GOAL.md bleiben: kein Upstream-Indexer-Eingriff, kein
  `SandboxBuilder.cs`-/`DiabloShotCheck.cs`-Refactoring, keine Gameplay-Änderung.

## Entscheide (Annahmen, im Plan gelockt)

1. ADR-Prozess kommt als eigener Abschnitt in `docs/runbook.md` (Kriterium 3 nennt
   `docs/runbook.md` zuerst); ein dediziertes code-graph-adr (optional unter `docs`) nur, wenn der
   Abschnitt das Gameplay-Runbook sprengt — dann verlinkt `docs/runbook.md` darauf.
   Verworfene Alternative: still ein neues Doku-File ohne Runbook-Verweis (auffindbar schlechter).
2. `ambiguous` wird nur in aktiven Goals und lebender Doku (`docs/*.md` ohne
   Run-Logs/Journale als Historie) auf eindeutige relative Pfade umgestellt;
   historische Einträge in `PROGRESS.md`-Verläufen und Run-Logs bleiben unangetastet,
   soweit sie nicht selbst `missing` erzeugen. Das Kriterium verlangt „messbar reduziert“,
   nicht null.
3. `missing`-Ziele werden je Fund entschieden: Tippfehler korrigieren, umgezogene
   Datei auf echten Pfad umstellen oder Verweis berechtigt entfernen — kein
   pauschales Löschen und kein Anlegen von Dummy-Zielen.
4. Das `parse_partial`-Konstrukt wird minimal umformuliert (gleiches Laufzeitverhalten),
   nicht das Skript umgebaut: Kandidat ist das `${arr[@]+...}`-Idiom; Ersatz über
   explizites Befüllen/Erweitern der Argument-Arrays vor dem Aufruf.

## Approach

Erst neu messen und die Funde klassieren (Ursache je Fund), dann den einzelnen
Parser-Fund (`run-coop-pair.sh:92`) isoliert glätten und per Re-Index beweisen,
danach `missing` (hartes Kriterium, Ziel 0) und `ambiguous` (Reduktion in aktiven
Dateien) in getrennten, reviewbaren Stufen bereinigen. Zuletzt den ADR-Prozess
dokumentieren, einmal real durchlaufen (`manage_adr`) und mit Gesamt-Re-Index,
Lint und Suiten abschließen. Jede Stufe endet auf frischem `index_status`-Stand,
damit Zähler nicht auf veraltetem Index beruhen.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 inventur | Frische Messung, Klassifikation, Baseline-Zähler in PROGRESS.md | `.agents/goals/cinderreach-graph-adr-hygiene/PROGRESS.md` (nur Beleg-Eintrag) |
| S2 parse-fix | `run-coop-pair.sh:92`-Konstrukt glätten, Verhaltensnachweis | `scripts/run-coop-pair.sh`, ggf. `scripts/test_*.py` für Argument-Konstruktion |
| S3 missing | 45 `missing`-`doc_links` korrigieren/umstellen/entfernen | `.agents/goals/**/*.md`, `docs/**/*.md` (nur Fundstellen) |
| S4 ambiguous | Bare-Filenames in aktiven Goals/Doku durch relative Pfade ersetzen | `.agents/goals/*/GOAL.md`, `docs/*.md` (nur aktive Fundstellen) |
| S5 adr-prozess | ADR-Aktualisierungsprozess dokumentieren, `manage_adr`-Lauf, Gesamtverifikation | `docs/runbook.md` (neuer Abschnitt) oder code-graph-adr (optional unter `docs`) (neu) + Runbook-Verweis, `.agents/goals/cinderreach-graph-adr-hygiene/{GOAL,PROGRESS}.md` |

S1 zuerst (Baseline). S2 ist unabhängig von S3/S4, aber vor S5, damit der
Abschluss-Re-Index alle drei Messwerte gleichzeitig beweist. S3 vor S4, weil
`missing == 0` das harte Kriterium ist und Pfad-Korrekturen aus S3 Mehrdeutigkeiten
aus S4 mit auflösen können. S5 zuletzt (Prozess beschreibt den erreichten Ist-Stand).

## Steps

### S1 — Frische Inventur und Klassifikation
- `index_status(project="home-patrick-unity-sandbox", diagnostics="full")` erheben;
  falls der Index älter als `main`-HEAD ist, `index_repository` voranstellen.
- Funde exportieren und klassieren: `missing` nach Ursache (Tippfehler / verschoben /
  gelöscht / nie existiert), `ambiguous` nach aktiv vs. historisch, `parse_partial`
  mit Datei und Zeile. Zähler und Top-Ursachen in `PROGRESS.md` festhalten
  (ersetzt die Audit-Zahlen von `5bd41d3` als Arbeitsbasis).
- Verify: Baseline-Eintrag in `PROGRESS.md`; keine Code- oder Doku-Änderung außer diesem Eintrag.

### S2 — `parse_partial` in `run-coop-pair.sh` glätten
- Das gemeldete Konstrukt (Zeile 92, Argument-Übergabe in `start_client`) isolieren:
  per Minimaländerung testen, welches Idiom (`${arr[@]+...}`, `${cls:+...}` oder
  Array-Expansion) den Indexer aus dem Takt bringt — jeweils eine Änderung, dann Probe-Re-Index.
- Ersatz ohne Verhaltensänderung: Argumente vor dem `timeout`-Aufruf explizit in
  Arrays sammeln (z. B. optionale Blöcke per `if`-Anhängen statt `+`-Idiom).
- Verhaltensnachweis: `bash -n scripts/run-coop-pair.sh` grün; Argumentliste vor/nach
  Änderung für die Fälle Server an/aus, `COOP_RELAY` an/aus und Client mit/ohne
  Klasse vergleichen (Trockenlauf über gesetzte `PLAYER`-/`LAUNCH`-Stubs oder
  `bash -x`-Trace ohne echten Start); bestehende `python3 -m pytest scripts -q` grün.
- Verify: Re-Index, `index_status` meldet für diese Datei kein `parse_partial` mehr.

### S3 — `missing`-`doc_links` auf 0 bringen
- Je Fund aus S1: Ziel korrigieren, auf echten relativen Pfad umstellen oder den
  Verweis mit Begründung im Commit entfernen. Betroffen sind nur Fundstellen in
  `.agents/goals/**` und `docs/**`; keine inhaltlichen Goal-Änderungen.
- Nach jedem Teilbatch (pro Goal/Doc-Gruppe) `node ~/scripts/goal-lint.mjs --repo .`
  laufen lassen, damit relative Links und Goal-Struktur gültig bleiben.
- Verify: Re-Index; `doc_links.unresolved.missing == 0` im `index_status`-Beleg in `PROGRESS.md`.

### S4 — `ambiguous` in aktiven Dateien reduzieren
- Bare-Filenames (z. B. `(GOAL.md)`, `(PROGRESS.md)`, nackte Doc-Namen) in aktiven
  `GOAL.md` und lebenden `docs/*.md` durch eindeutige relative Pfade ersetzen.
- Historische Verläufe (`PROGRESS.md`-Einträge, Run-Logs, Journale) nur anfassen,
  wenn sie selbst als aktive Fundstelle gemeldet werden; Entscheidung je Fund in
  `PROGRESS.md` kurz begründen.
- Verify: Re-Index; `ambiguous`-Zähler gegenüber S1-Baseline messbar gesunken
  (Vorher/Nachher-Zahlen in `PROGRESS.md`), `goal-lint` weiter grün.

### S5 — ADR-Prozess verankern und Gesamtverifikation
- Neuer Abschnitt in `docs/runbook.md`: Auslöser für ein ADR-Update (neues oder
  geteiltes Subsystem, Netz-/Save-Vertragsänderung, Klassen-/Skill-Umbau),
  Ablauf `index_repository` → `manage_adr(mode='set_sections'|'update')` →
  Re-Index-Kontrolle (`adr_present: true`), Verantwortlichkeit beim landenden Goal.
- Den Prozess einmal real durchlaufen: ein `manage_adr`-Update für den aktuellen
  Stand (u. a. Class-Kits `aaa2571`) ausführen und das Ergebnis im Runbook-Abschnitt
  als Beispiel verlinken.
- Gesamt-Gate: Re-Index mit `parse_partial.count == 0` und
  `doc_links.unresolved.missing == 0`; `bash -n scripts/run-coop-pair.sh`,
  `node ~/scripts/goal-lint.mjs --repo .`, `python3 scripts/check-meta.py`,
  `python3 -m pytest scripts -q` und `python3 ~/scripts/agent-workspace.py check` grün.
- Verify: Alle vier GOAL-Erfolgskriterien mit Mess-Beleg in `PROGRESS.md`, dann
  Goal auf 100 % mit `Offene Punkte: Keine.`

## Validation Plan

- Je Stufe: frischer `index_status`-Beleg (kein Zähler ohne Re-Index nach
  Dateiänderungen), `goal-lint` nach jeder Doku-Stufe, `check-meta.py` und
  Skript-Suiten in S2/S5.
- Höchstrisiko-Check: S2-Verhaltensvergleich der Argumentliste — ein still
  verändertes Coop-Startverhalten würde erst im echten Koop-Lauf auffallen;
  Regression fängt der Vorher/Nachher-Vergleich der expandierten Argumente
  über die vier Schalter-Kombinationen, plus ein Koop-Smoke
  (`scripts/run-coop-pair.sh`), falls ein Player-Build verfügbar ist, sonst
  als offener manueller Beleg in `PROGRESS.md` benennen, nicht behaupten.
- Massenhafte Link-Edits (S3/S4) je Goal/Doc-Gruppe als eigene Commits, damit ein
  Fehlgriff per Stufe zurückrollbar bleibt; `main` bleibt nach jeder Stufe lint-grün.

## Risks / Open Questions

- Risiken: Index-Zähler driften durch parallele Goal-Arbeit (S1-Baseline veraltet) —
  Mitigation: S5 misst abschließend neu und nennt die gemessene Revision.
  Hub-Ausfall blockiert jede Messung — dann ist die Stufe nicht land-bar, kein
  Abschluss auf Schätzung.
- Rollback: Jede Stufe eigener Branch/Worktree; Link-Batches sind reine Doku-Edits
  und per Commit isoliert revertierbar. Der S2-Eingriff ist auf ein Konstrukt begrenzt.
- Open Questions: Keine für S1–S4. Ob der ADR-Abschnitt in `docs/runbook.md`
  bleibt oder als code-graph-adr (optional unter `docs`) ausgelagert wird, entscheidet S5 an der
  Länge (Entscheid 1); beides erfüllt Kriterium 3.
