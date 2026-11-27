---
goal: cinderreach-inventory-ui-layout-split
slug: cinderreach-inventory-ui-layout-split
branch: feature/cinderreach-inventory-ui-layout-split-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
priority: P3 (unabhängiges Refactoring, mechanisch/niedriges Risiko; parallel zur Story-Kette möglich, S2 als Nächstes)
status: done (100 %) — S1–S3 gelandet (39/39 Baseline gehalten, Voll-Suiten + Shotcheck grün)
start_condition: main sauber; kein paralleles Schreiben an DiabloInventoryUI.cs (insb. kein offener Inventory-/Gear-Worktree)
plan_refs: [.agents/goals/cinderreach-inventory-ui-layout-split/GOAL.md]
---

# Plan: cinderreach-inventory-ui-layout-split — DiabloInventoryUI Layout als Partial abtrennen

Pfad: feature, 3 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-inventory-ui-layout-split-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).
Reihenfolge ist Abhängigkeitsreihenfolge; keine Stufe lässt Suiten, Verify oder Host-Start rot zurück.

## Goal

Siehe [GOAL.md](../../goals/cinderreach-inventory-ui-layout-split/GOAL.md):
`DiabloInventoryUI.cs` analog zum `DiabloNetworkPlayer.*.cs`-Muster trennen —
der 3-Tab-/Paperdoll-UGUI-Layoutbau wandert in `DiabloInventoryUI.Layout.cs`,
Lebenszyklus, Drag-State und Item-Aktionen bleiben in der Hauptdatei.

## Success Criteria

Die drei Erfolgskriterien aus GOAL.md, je Stufe unten mit Test-Beleg verknüpft.
Gesamt-Gate S3: die 6 benannten Suites plus vollständige EditMode-/PlayMode-Suiten
(`bash scripts/run-unity-tests.sh`), `python3 scripts/check-meta.py` und
`KayKitAssets.Verify` (`repo.yaml:verify`) grün.

## Ausgangslage (Belege, HEAD `8e59d00`, 2026-11-27)

- `sandbox/Assets/Scripts/DiabloInventoryUI.cs`: 1.033 Zeilen, 43.802 Bytes
  (Audit-Basis `5bd41d3` nannte 1.014 Zeilen — Datei ist seitdem gewachsen).
  Klasse heute `public sealed class DiabloInventoryUI : MonoBehaviour` (Zeile 16),
  noch nicht `partial`.
- Vorbild im selben Verzeichnis: `DiabloNetworkPlayer.cs` ist
  `public sealed partial class` mit `.Flask.cs`, `.Items.cs`, `.Names.cs`,
  `.Story.cs` — Namens- und Partial-Muster werden übernommen, sonst nichts.
- Zu verschiebende Layout-Methoden (Ist-Zeilen): `BuildUI` (564), `MakePage` (616),
  `BuildInventoryPage` (638), `BuildCharacterColumn` (660), `BuildEquipSlot` (710),
  `MakeCard` (761, static), `Place` (772, static), `BuildBagColumn` (780),
  `BuildStatsPage` (835), `BuildSettingsPage` (873), `PaintTabs` (902),
  `PaintEquipIcon` (909), `MakeTabButton` (932), `MakeBagCell` (950).
- In der Hauptdatei verbleiben u. a.: Felder und Konstanten (Zeilen 20–69, darunter
  `canvas`, Seiten-`GameObject`s, `slotTexts`, `equipIcons`/`equipFrames`,
  `tabButtons`, `bagList`, `paperdoll`), `Awake`/`EnsureBuilt` (71/76, ruft `BuildUI`),
  `Update` (88), öffentliche Aktionen `SetOpen` (112), `ShowTab` (134),
  `EquipIndex` (153), `EquipById` (187), `DiscardById` (198), `UnequipSlot` (217),
  `DiscardIndex` (244), `DropBagOnSlot` (277), Drag-Handler (308–420),
  `Refresh` (422) sowie `PaperdollSlotPosition` (748, static).
- Geteilte Felder bleiben in der Hauptdatei; die Partial-Datei greift als Teil
  derselben Klasse darauf zu — kein Sichtbarkeits- oder API-Umbau nötig.
- Direkt betroffene Suites existieren alle: `InventoryPaperdollPlayTests`,
  `GearSlotsPlayTests`, `DiabloMenuPlayTests` (PlayMode) und `ItemCompareTests`,
  `DiabloMenuTests`, `DiabloHdTests` (EditMode).
- Test-Verträge stehen im Datei-Doccomment: `MenuTab`, `IsOpen`, `CurrentTab`,
  `SetOpen`, `ShowTab`, `EquipIndex`, `UnequipSlot`, `Refresh`, `CompareSummary`,
  `DifficultyText`, `InventoryCanvas`, `BagGrid`, `BagButton{i}`, `BagEmpty` (20 Zellen).

## Entscheide (Annahmen, im Plan gelockt)

1. Genau die 14 in Kriterium 1 gelisteten Methoden wandern. `PaperdollSlotPosition`
   (748) bleibt in der Hauptdatei: Es ist eine öffentliche, testgenutzte
   Positionsberechnung und steht nicht in der Kriterium-1-Liste; ein Mitverschieben
   wäre Scope-Ausweitung ohne Vertragsgewinn.
2. `MakeCard`/`Place` bleiben `static` in der Layout-Datei; Aufrufer in der
   Hauptdatei (u. a. `Refresh`-Pfad) erreichen sie unverändert über dieselbe Klasse.
3. Die neue `.meta` wird per Unity-Import erzeugt (Batchmode-Import oder Editor),
   nicht von Hand kopiert — GUID muss neu und eindeutig sein; `check-meta.py`
   prüft nur Existenz, der Verify-Lauf prüft die Referenzintegrität.
4. Keine Methoden-Umbenennung, keine Reihenfolgen-Änderung innerhalb der
   verschobenen Blöcke, kein Aufräumen „nebenbei“ — der Diff bleibt ein reiner
   Datei-Split, damit Review und Fehlersuche trivial bleiben.

## Approach

Zuerst den Ist-Stand der 6 Suites auf `main` einfrieren (grün als Baseline),
dann in einer Stufe die Klassendeklaration auf `partial` stellen, die 14
Layout-Methoden am Stück in die neue Datei verschieben und die `.meta` sauber
erzeugen. Abschluss mit denselben 6 Suites, Voll-Suiten, Meta-Check und Verify.
Bewusst ein einziger Verschiebe-Schritt statt Methoden-Einzelstufen: Partial-Splits
sind mechanisch, und ein Zwischenschritt mit halb verschobenem Layout hätte
keinen eigenen Nutzerwert, aber doppelte Testkosten.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 baseline | 6 Suites auf `main` fahren, Ergebnis in PROGRESS.md | `.agents/goals/cinderreach-inventory-ui-layout-split/PROGRESS.md` (nur Beleg-Eintrag) |
| S2 split | `partial`-Deklaration, 14 Methoden nach `DiabloInventoryUI.Layout.cs` verschieben, `.meta` erzeugen | `sandbox/Assets/Scripts/DiabloInventoryUI.cs`, `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs` (neu), `sandbox/Assets/Scripts/DiabloInventoryUI.Layout.cs.meta` (neu) |
| S3 proof | Voll-Suiten, `check-meta.py`, `KayKitAssets.Verify`, Inventar-Sichtprüfung, PROGRESS-Abschluss | `docs/cinderreach-inventory-shots/` (nur falls Sichtprüfung neue Shots liefert), `.agents/goals/cinderreach-inventory-ui-layout-split/PROGRESS.md` |

S2 hängt an S1 (Baseline-Vergleich). S3 hängt an S2.

## Steps

### S1 — Baseline einfrieren
- `PLATFORMS=EditMode TEST_FILTER=ItemCompareTests bash scripts/run-unity-tests.sh`
  und entsprechend `DiabloMenuTests`, `DiabloHdTests` sowie PlayMode
  `InventoryPaperdollPlayTests`, `GearSlotsPlayTests`, `DiabloMenuPlayTests`
  auf unverändertem `main` ausführen; Ergebnis (Anzahl grün/rot) in `PROGRESS.md`.
- Öffentliche Schnittstelle aus Kriterium 2 gegen den Code abgleichen
  (`IsOpen`, `CurrentTab`, `Paperdoll`, `SetOpen`, `ShowTab`, `EquipIndex`,
  `EquipById`, `DiscardById`, `UnequipSlot`, `DiscardIndex`, `DropBagOnSlot`,
  `DropSlotOnBag`, `RightClickBag`, `RightClickSlot`, `PaperdollSlotPosition`,
  `Notice`) — jede fehlende oder umbenannte Signatur stoppt den Plan hier.
- Verify: Baseline grün dokumentiert; rote Vorbefunde werden benannt und nicht dem Split angelastet.

### S2 — Layout in `DiabloInventoryUI.Layout.cs` auslagern
- `DiabloInventoryUI.cs`: Klassendeklaration auf `public sealed partial class`
  umstellen; die 14 Layout-Methoden (Ist-Zeilen oben) inklusive ihrer
  XML-Doccomments und der nur von ihnen genutzten Hilfs-Konstanten entfernen.
- `DiabloInventoryUI.Layout.cs` (neu): `public sealed partial class DiabloInventoryUI`
  mit denselben 14 Methoden, unverändertem Wortlaut; Datei-Doccomment nennt Zweck
  (UGUI-Aufbau/Styling) und Schwesterdatei (Lebenszyklus/Drag/Aktionen).
- Prüfen, dass kein verschobener Block Hauptdatei-`private`-Helfer braucht, die
  nicht Felder sind; falls doch, Helfer mit verschieben und Umzug in `PROGRESS.md` begründen.
- `DiabloInventoryUI.Layout.cs.meta` per Unity-Import erzeugen (Editor/Batchmode
  auf den Worktree), danach `python3 scripts/check-meta.py` grün.
- Erster Compiler-Beleg: `unity --json recompile "$PWD/sandbox"` (laufender Editor)
  oder Batchmode-Testlauf; `data.errors` leer.
- Verify: die 6 Suites aus S1 erneut grün, ohne Teständerung.

### S3 — Vollverifikation und Abschluss
- `bash scripts/run-unity-tests.sh` (EditMode + PlayMode komplett),
  `python3 scripts/check-meta.py`, Verify aus `repo.yaml:verify`
  (`KayKitAssets.Verify`), `python3 ~/scripts/agent-workspace.py check` grün.
- Sichtprüfung Inventar/Paperdoll (bestehende Shots unter
  `docs/cinderreach-inventory-shots/` als Vergleich): 3 Tabs, Paperdoll,
  Bag-Zellen und Drag-Ghost unverändert; Abweichung = Blocker, nicht „Kosmetik“.
- `PROGRESS.md`: Zeilenzahl vorher/nachher je Datei, Suite-Belege, dann Goal auf
  100 % mit `Offene Punkte: Keine.`

## Validation Plan

- Kernvergleich S1↔S2: identische Suite-Ergebnisse vor und nach dem Split;
  ein einziger neuer Fehlschlag gilt als Split-Regression, bis das Gegenteil belegt ist.
- Höchstrisiko-Check: Hierarchie-/Namensvertrag (`InventoryCanvas`, `BagGrid`,
  `BagButton{i}`, `BagEmpty`) — er wird von PlayMode-Tests über die Szene geprüft;
  `InventoryPaperdollPlayTests` und `GearSlotsPlayTests` müssen unverändert grün sein.
- Compiler- und Meta-Gate vor jedem Unity-Lauf, damit ein GUID-/Partial-Fehler
  nicht als Spielfehler fehlinterpretiert wird.

## Risks / Open Questions

- Risiken: Doppelte GUID in handkopierter `.meta` (Entscheid 3 verhindert das);
  versteckte Kopplung über `static`-Helfer (`MakeCard`/`Place`, Entscheid 2);
  `.meta` für das neue Verzeichnis ist nicht nötig (gleiches Verzeichnis).
- Rollback: S2 ist ein mechanischer Move in einer Stufe — bei rotem Gate wird
  die Stufe nicht gelandet, `main` behält die ungeteilte Datei; kein Save-,
  Netz- oder Verhaltens-Risiko, weil keine Logik geändert wird.
- Open Questions: Keine. Ob `PaperdollSlotPosition` später mitzieht, ist eine
  eigene Aufräum-Entscheidung außerhalb dieses Goals (Entscheid 1).
