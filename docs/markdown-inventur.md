# Markdown-Inventur — unity-sandbox

Stand: geprüfter Commit `58b9b41` (`58b9b41112f84882fd9549644c53f835cd996b65`, 2026-11-28).
Anlass: Ziel `unity-markdown-bestandsaufnahme` — für jede versionierte Markdown-Datei sind
Herkunft und Pflegeweg nachvollziehbar; für mehrfach gepflegte Fakten liegt eine
Konsolidierungsentscheidung vor.

## Mengenvergleich (Abnahme Kriterium 1)

| Quelle | Zählung |
|---|---|
| `git ls-files '*.md'` (geprüfter Commit) | 65 |
| `git ls-tree -r --name-only HEAD` gefiltert auf `*.md` | 65 |
| Im Bericht erfasste Pfade (Tabelle unten) | 65 |
| Differenz (fehlend / zusätzlich) | **0 / 0** |

Prüfbefehl: `diff <(git ls-files '*.md' | sort) <(git ls-tree -r --name-only HEAD | grep '\.md$' | sort)` — identisch.
Während der Untersuchung wurden keine bestehenden Inhaltsdateien verändert; einzige Mutation
dieses Berichts ist diese Datei selbst (`docs/markdown-inventur.md`).

## Bestand (65 Pfade, Byteumfang am geprüften Commit)

Pflegearten: **H** = von Hand, **G** = generiert (mit Erzeuger), **S** = Momentaufnahme/Zählung (Stichtag im Kopf).

### Root (3)

| Pfad | Bytes | Art | Zweck / Erzeuger-Verbraucher |
|---|---|---|---|
| `README.md` | 1058 | H | Repo-Einstieg, Stack, Verweis auf `CINDERREACH.md` + GDD |
| `AGENTS.md` | 4870 | H | Agenten-Regeln R1–R7, Prüf-Signale |
| `CLAUDE.md` | 11 | H | Nur Verweis `@AGENTS.md` (kein eigener Inhalt) |

### Projektstand / Spiel-Design (8)

| Pfad | Bytes | Art | Zweck |
|---|---|---|---|
| `.agents/goals/CINDERREACH.md` | 61220 | H | **SSOT Ist-Stand**: ersetzt abgeschlossene Goals + alte ROADMAP; Ausgangspunkt neuer Arbeit |
| `docs/one-page-gdd.md` | 2680 | H | Spiel-Vision (MoSCoW-Template); verweist auf `mechanics-questions.md`, `decision-log.md`, `q1-run-log.md` |
| `docs/mechanics-questions.md` | 13545 | H | Prototyp-Fragen Q1–Qn mit Exit-Kriterien; Verdicts stehen in `decision-log.md` |
| `docs/decision-log.md` | 17961 | H | Append-only Entscheidungslog (Datum, Frage, continue/pivot/kill, Evidenz) |
| `docs/dev-journal.md` | 99606 | H | Session-Tagebuch (done/faced/learned/next) gegen Scope-Creep |
| `docs/story-bible.md` | 34522 | H | **SSOT Welt/Figuren/Begriffe/Ton**; Questregeln explizit an `quest-inventory.md` delegiert |
| `docs/quest-inventory.md` | 10096 | G | Questregeln, erzeugt per `python3 scripts/quest_inventory.py` aus `StoryModel.cs` + `LocCatalog.Story.cs` (`--check` als Goal-Nachweis) |
| `docs/sprache-inventur.md` | 3296 | S | Stichtagszählung Text-/Voice-Zeilen (Stand `6bf15a6`, 238 Zeilen), Quelle `check-voice-lines.py` |

### Betrieb / Pipeline (8)

| Pfad | Bytes | Art | Zweck |
|---|---|---|---|
| `docs/runbook.md` | 6996 | H | Sandbox-Betrieb (Beweis-Schleife) |
| `docs/asset-pipeline.md` | 2508 | H | Asset-Erzeugungswege |
| `docs/audio/generation-pipeline.md` | 3622 | H | Audio-Erzeugungspipeline |
| `docs/visual-generation-runbook.md` | 2000 | H | Bildgenerierungs-Runbook |
| `docs/build-speed.md` | 4166 | H | Build-Geschwindigkeit |
| `docs/ci-runner.md` | 4957 | H | CI-Läufer-Doku |
| `docs/mcp.md` | 7426 | H | MCP-Anbindung |
| `docs/playtest-protocol.md` | 1372 | H | 5-Personen-Playtest-Protokoll |

### Kataloge / Inventare (3)

| Pfad | Bytes | Art | Zweck |
|---|---|---|---|
| `docs/cinderreach-asset-inventory.md` | 4302 | H/S | Asset-Bestand (+ `.tsv`-Schwester, keine `.md`) |
| `docs/kaykit-assets.md` | 1519 | H | KayKit-Assetbezug |
| `docs/kaykit-characters.md` | 1839 | H | KayKit-Figurenbezug |

### Design-Specs + Perf (6)

| Pfad | Bytes | Art | Zweck |
|---|---|---|---|
| `docs/hud-redesign/design-spec.md` | 3199 | H | HUD-Design-SSOT (Canvas-Ablage, außerhalb nicht abrufbar) |
| `docs/hud-redesign/perf.md` | 1751 | H | Windows-Perf-Messung (Kriterium 6) |
| `docs/map-overlay/design-spec.md` | 2220 | H | Karten-Overlay-Design-SSOT (verweist für Stil auf HUD-Spec D4) |
| `docs/map-overlay/perf.md` | 2744 | H | Windows-Perf-Paarmessung (Kriterium 6) |
| `docs/inventory-redesign/design-spec.md` | 1660 | H | Inventar-Design-Spec |
| `docs/menu-art/STARTERPACK.md` | 2125 | H | Menü-Art-Starterpaket |

### Weitere Themen-Docs (6)

| Pfad | Bytes | Art | Zweck |
|---|---|---|---|
| `docs/multiplayer-plan.md` | 2160 | H | Alter Mehrspieler-Plan (verweist auf entferntes Goal-Verz.) |
| `docs/skill-vfx/README.md` | 2730 | H | Skill-VFX-Doku |
| `docs/world-art/local-art-pilot/README.md` | 8236 | H | Welt-Kunst-Pilot |
| `docs/world-art/model-realism/README.md` | 4206 | H | Modell-Realismus |
| `docs/fair-fights-playtest.md` | 1598 | H | Fair-Fights-Playtest |
| `docs/replayability-run-log.md` | 2274 | H | Wiederspielbarkeits-Protokoll (inhaltlich Run-Log-Charakter) |

### Run-Logs / Belegprotokolle (19)

Alle **H**, append-only Belege je Stufe; werden in `CINDERREACH.md` („Belege und Doku“) zusammengefasst,
nicht doppelt gepflegt.

| Pfad | Bytes |
|---|---|
| `docs/act-one-run-log.md` | 2494 |
| `docs/act-two-run-log.md` | 2884 |
| `docs/akt1-vertiefung-run-log.md` | 3776 |
| `docs/akt2-vertiefung-run-log.md` | 3777 |
| `docs/akt3-finale-run-log.md` | 5663 |
| `docs/class-kits-run-log.md` | 3035 |
| `docs/coop-run-log.md` | 16265 |
| `docs/diablo-p2-run-log.md` | 3725 |
| `docs/diablo-run-log.md` | 2784 |
| `docs/party-roles-run-log.md` | 3094 |
| `docs/presentation-run-log.md` | 6392 |
| `docs/q1-run-log.md` | 1375 |
| `docs/q4-run-log.md` | 2547 |
| `docs/second-class-run-log.md` | 2176 |
| `docs/session-loop-run-log.md` | 4824 |
| `docs/sprache-voice-run-log.md` | 6296 |
| `docs/story-run-log.md` | 1813 |
| `docs/voice-batch-preflight.md` | 2485 |
| `docs/voice-batch-run-log.md` | 2325 |

### Goals (12)

| Pfad | Bytes | Art | Zweck |
|---|---|---|---|
| `.agents/goals/cinderreach-questkatalog/GOAL.md` | 853 | H | Questkatalog-Ziel (100 %) |
| `.agents/goals/cinderreach-questkatalog/PROGRESS.md` | 316 | H | Abschlussbelege (`quest_inventory.py --check`, Coverage 0 offen) |
| `.agents/goals/cinderreach-bag-quest-upgrades/GOAL.md` | 2209 | H | Taschenquest-Ziel (0 %) |
| `.agents/goals/cinderreach-bag-quest-upgrades/PROGRESS.md` | 207 | H | Stub (angelegt 2026-11-28, alles offen) |
| `.agents/goals/cinderreach-inventory-cursor-item/GOAL.md` | 1883 | H | Cursor-Item-Ziel (0 %) |
| `.agents/goals/cinderreach-inventory-cursor-item/PROGRESS.md` | 210 | H | Stub (angelegt 2026-11-28, alles offen) |
| `.agents/goals/cinderreach-loot-click-filter/GOAL.md` | 3015 | H | Loot-Klick-Ziel (100 %, Belege in PROGRESS) |
| `.agents/goals/cinderreach-loot-click-filter/PROGRESS.md` | 1464 | H | Abschlussbelege (Tests grün, Shots) |
| `.agents/goals/cinderreach-town-npc-identity/GOAL.md` | 1612 | H | NPC-Identitäts-Ziel (0 %) |
| `.agents/goals/cinderreach-town-npc-identity/PROGRESS.md` | 206 | H | Stub (angelegt 2026-11-28, alles offen) |
| `.agents/goals/cinderreach-vendor-sell-all/GOAL.md` | 1547 | H | Händler-Ziel (0 %) |
| `.agents/goals/cinderreach-vendor-sell-all/PROGRESS.md` | 204 | H | Stub (angelegt 2026-11-28, alles offen) |

Summe: 3 + 8 + 8 + 3 + 6 + 6 + 19 + 12 = **65 Pfade** (Differenz 0).

Suchumfang Mehrfachpflege (Abnahme Kriterium 2): `grep`/`rg` über alle 65 `.md` nach
Quest-IDs/Titeln (`H1`, `dunkle Tür`), Projektstand-Angaben (`34` Quests, `Cinderreach`,
`Lantern Hold`/`Ashen Fen`), Goal-Slugs (6 Verzeichnisse) sowie Querverweisen
(`CINDERREACH.md`, `quest-inventory`, `story-bible`, `repo.yaml`); Questregeln zusätzlich
gegen Erzeuger `scripts/quest_inventory.py` und Kataloge `StoryModel.cs`/`LocCatalog.Story.cs`
(nur lesend) abgeglichen.

## Befunde (priorisiert)

### B1 — README-Status veraltet (bestätigt, Aufwand klein)

- Fundstelle 1: `README.md:## Status` — „Skeleton only. Unity 6 project + first greybox scene still open (see foundation goal).“
- Fundstelle 2: `.agents/goals/CINDERREACH.md` (Stand 2026-11-28) — Akt-I–III-Story-Kette, Sprache/Voice, HUD, Inventar, Karte gelandet; `README.md`-Aussage ist seit `8cc8452` überholt.
- Maßgebliche Quelle: `CINDERREACH.md`. Aktualisierungsweg: von Hand, ein Satz + Verweis.
- Vorschlag: Status-Absatz auf „Cinderreach Akt I–III spielbar, Einzelheiten siehe `CINDERREACH.md`“ kürzen; Leser: neue Einsteiger. Aufwand: Minuten, 1 Datei.

### B2 — Sechs Goal-Verzeichnisse nicht in CINDERREACH-Übersicht (bestätigt, Aufwand mittel)

- Fundstelle 1: `.agents/goals/CINDERREACH.md:## Aktive Goals` — „Keine — alle zuletzt aktiven Goals stehen in ihren `PROGRESS.md` auf 100 %“.
- Fundstelle 2: sechs Verzeichnisse am geprüften Commit: `cinderreach-questkatalog` (100 %, PROGRESS 2026-11-28 belegt),
  `cinderreach-loot-click-filter` (100 %, nur in Fließtext-Zeile 380 erwähnt, nicht in der Goal-Tabelle),
  `cinderreach-bag-quest-upgrades` / `-inventory-cursor-item` / `-town-npc-identity` / `-vendor-sell-all` (je 0 %, Stub-PROGRESS ~200 Bytes „Goal angelegt nach /setgoal-Runde“).
- Maßgebliche Quelle: je `PROGRESS.md`. Aktualisierungsweg: von Hand beim Goal-Abschluss (Hausregel: Stand einarbeiten, Verzeichnis + Plan entfernen).
- Vorschlag: die zwei 100 %-Goals (`questkatalog`, `loot-click-filter`) in `CINDERREACH.md` einarbeiten und die Verzeichnisse entfernen;
  die vier 0 %-Goals bei Abschluss ebenso. Leser: alle Agenten (Einstiegspunkt neue Arbeit). Aufwand: je Goal Minuten.

### B3 — Alter Multiplayer-Plan verweist ins Leere (bestätigt, Aufwand klein)

- Fundstelle 1: `docs/multiplayer-plan.md:4` — „Assumptions (repo has no `repo.yaml`)“ und Goal-Pfad `.agents/goals/unity-multiplayer-demo/GOAL.md`.
- Fundstelle 2: `repo.yaml` existiert im Root; das Goal-Verzeichnis existiert nicht mehr (Netz-Entscheid steht in `decision-log.md`, Q4, fortgeführt in `coop-run-log.md`).
- Maßgebliche Quelle: `decision-log.md` + `coop-run-log.md`. Aktualisierungsweg: von Hand.
- Vorschlag: Datei als historisch kennzeichnen (Kopfzeile „archiviert, ersetzt durch …“) oder entfernen; nichts daraus wird mehr gepflegt. Leser: Koop-/Netz-Arbeit. Aufwand: Minuten, 1 Datei.

### B4 — Questregeln vs. Story-Bibel: bewusste Ableitung, kein Handlungsbedarf

- Fundstelle 1: `docs/quest-inventory.md` (generiert, Kopf nennt Erzeuger + Nach-Erzeugungs-Befehl).
- Fundstelle 2: `docs/story-bible.md` (SSOT Welt/Begriffe/Ton, delegiert Questregeln explizit ans Questinventar: „Die aktuellen Questregeln stehen im Questinventar, das aus dem Spielkatalog erzeugt wird“).
- Entscheidung: Arbeitsteilung beibehalten — Regeln nur im Code ändern + Generator laufen lassen; Erzählhintergrund nur in der Bibel. Keine Doppelpflege, da die Ableitungsrichtung dokumentiert ist.

### B5 — Kennzahlen in Run-Logs und CINDERREACH-Belegtabelle: bewusste Ableitung, kein Handlungsbedarf

- Fundstelle 1: 19 Run-Logs unter `docs/` (z. B. `akt3-finale-run-log.md`: „34/34, `open=[]`“, EditMode-/PlayMode-Zahlen).
- Fundstelle 2: `.agents/goals/CINDERREACH.md` fasst dieselben Zahlen pro Goal zusammen (z. B. „EditMode 716/716, PlayMode 410/410, Quests 34/34“).
- Entscheidung: beibehalten — Protokoll (Beleg) vs. Übersicht (Zusammenfassung) ist die dokumentierte Form („Laufprotokolle bleiben unter `docs/`“); Aktualisierungsweg ist der Goal-Abschluss, keine laufende Synchronpflege.

### B6 — Sprachzahlen an drei Stellen: Momentaufnahme + Belege, kein Handlungsbedarf

- Fundstellen: `docs/sprache-inventur.md` (Stichtag `6bf15a6`: 115/123/238 Zeilen), `docs/voice-batch-run-log.md` + `docs/sprache-voice-run-log.md` (Erzeugungsbelege: `check-voice-lines.py` „115, problems: 0“), `CINDERREACH.md` (Übernahme „238 Zeilen“).
- Entscheidung: beibehalten — Inventur ist datierte Zählung, Run-Logs sind Erzeugungsbelege; bei neuen Zeilen neue Inventur statt Nachpflegen alter Zahlen.

### Leere Teilmengen (mit Suchumfang belegt)

- Keine zwei `.md` pflegen dieselbe ausführbare Regel: einzige generierte `.md` ist `quest-inventory.md` (Erzeuger `scripts/quest_inventory.py`); kein zweiter Generator, keine Hand-Kopie der Regeltabellen gefunden (`H1`-Titel nur in `quest-inventory.md` + erzählerisch in `story-bible.md`, siehe B4).
- Keine widersprüchlichen Pläne: „.agents/plans/“ existiert am geprüften Commit nicht (Gegenprobe `ls`); einzige Plan-Datei ist das archivierte `docs/multiplayer-plan.md` (B3).
- `CLAUDE.md` (11 Bytes) ist reiner Verweis, keine Inhaltsdopplung zu `AGENTS.md`.
