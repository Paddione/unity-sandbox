#!/usr/bin/env python3
"""Health-Probe G-QUESTCOVER01: ungetestete Cinderreach-Inhalte (stdlib only).

Das Inventar wird aus den Spielinhalten gewonnen, nicht aus einer Handliste:

- Quests: ``new StoryQuest("<id>", ...)`` in
  ``sandbox/Assets/Scripts/Story/StoryModel.cs``.
- Platzierungen: Szenenobjekte in ``sandbox/Assets/Scenes/DiabloZone.unity``
  (``m_Name:`` plus Prefab-Override ``propertyPath: m_Name / value:``), die
  entweder zu einer bekannten Interaktions-Konstante aus den C#-Quellen
  gehoeren (``*ObjectName`` / ``*GateName`` / ``*ShrineObjectName`` /
  ``*BoardObjectName``) oder zu einer Platzierungsfamilie desselben Prefabs
  (``StoryNpc_``, ``StoryPickup_``, ``WaypointShrine_``). Dadurch erfassen
  neue Quests und einzelne neue Platzierungen desselben Prefabs automatisch.

Jeder Inventareintrag muss in mindestens einer Testdatei unter
``sandbox/Assets/Tests/`` ueber eine explizite Marke referenziert sein:

- ``quest:<id>`` (z. B. ``quest:h1``) in ``QuestRuleTests.cs``
  (EditMode) und ``QuestJourneyTests.cs`` (PlayMode),
- ``placement:DiabloZone:<ObjektName>`` in ``WorldInteractionTests.cs``.

Regelmaessiger Lauf: gibt die Anzahl ungedeckter Eintraege aus, Exit 1 bei
jedem Problem (ungedeckt, baumelnd, doppelt). Bei unlesbarer Quelle mit
Fehler abbrechen (fail-closed). Leere Bestaende duerfen kein Gruen erzeugen.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

QUEST_RE = re.compile(r'new\s+StoryQuest\s*\(\s*"([^"]+)"')
CONST_RE = re.compile(
    r'(?:const\s+string\s+)(\w*(?:ObjectName|GateName|ShrineObjectName|BoardObjectName))\s*=\s*"([^"]+)"'
)
GAMEOBJECT_RE = re.compile(r'new\s+GameObject\s*\(\s*"([^"]+)"')
SCENE_NAME_RE = re.compile(r"^[\s\-]*m_Name:\s*(.+?)\s*$", re.MULTILINE)
SCENE_OVERRIDE_RE = re.compile(
    r"propertyPath:\s*m_Name\s*\n\s*value:\s*(.+?)\s*$", re.MULTILINE
)
QUEST_MARK_RE = re.compile(r"quest:([A-Za-z0-9_]+)")
PLACEMENT_MARK_RE = re.compile(r"placement:([A-Za-z0-9_:\-]+)")

PLACEMENT_FAMILIES = ("StoryNpc_", "StoryPickup_", "WaypointShrine_")
SCENE = "DiabloZone"

MODEL_REL = Path("sandbox/Assets/Scripts/Story/StoryModel.cs")
SCENE_REL = Path("sandbox/Assets/Scenes/DiabloZone.unity")
TEST_ROOTS = [
    Path("sandbox/Assets/Tests/EditMode"),
    Path("sandbox/Assets/Tests/PlayMode"),
]


def quests_from_model_text(text: str) -> list[str]:
    """Alle Quest-Ids aus StoryModel.cs, in Quellreihenfolge."""
    return QUEST_RE.findall(text)


def exact_names_from_sources(root: Path) -> set[str]:
    """Interaktions-Objektnamen aus C#-Konstanten (Vendor, Smith, Stash,
    ContractBoard, RiskShrine, ZoneGate) plus bekannte Literale."""
    names: set[str] = set()
    scripts = root / "sandbox/Assets/Scripts"
    if scripts.is_dir():
        for path in sorted(scripts.rglob("*.cs")):
            try:
                text = path.read_text(encoding="utf-8", errors="strict")
            except (OSError, UnicodeError):
                continue
            for match in CONST_RE.finditer(text):
                names.add(match.group(2))
    town_builder = root / "sandbox/Assets/Editor/TownBuilder.cs"
    try:
        builder_text = town_builder.read_text(encoding="utf-8", errors="strict")
    except (OSError, UnicodeError):
        builder_text = ""
    for lit in GAMEOBJECT_RE.findall(builder_text):
        if lit == "CampfireHeal":
            names.add(lit)
    names.add("CampfireHeal")
    return names


def scene_names_from_text(text: str) -> list[str]:
    """Alle Objektnamen aus der Szenendatei (m_Name + Prefab-Overrides)."""
    names = SCENE_NAME_RE.findall(text)
    names.extend(m.group(1).strip() for m in SCENE_OVERRIDE_RE.finditer(text))
    return [n.strip() for n in names if n and n.strip()]


def placements_from_scene_text(scene_text: str, exact_names: set[str]) -> list[str]:
    """Interaktions-Platzierungen aus der Szene, in Szenenreihenfolge ohne
    Duplikate. Erfasst einzelne Platzierungen desselben Prefabs ueber die
    Familienpraefixe sowie neue exakte Objektnamen aus den C#-Konstanten."""
    out: list[str] = []
    seen: set[str] = set()
    for name in scene_names_from_text(scene_text):
        if name in seen:
            continue
        if name in exact_names or name.startswith(PLACEMENT_FAMILIES):
            seen.add(name)
            out.append(f"placement:{SCENE}:{name}")
    return out


def inventory_from_texts(
    model_text: str, scene_text: str, exact_names: set[str]
) -> tuple[list[str], list[str]]:
    """Reine Funktion fuer Fixtures: (quests, placements) als Keys."""
    quests = [f"quest:{qid}" for qid in quests_from_model_text(model_text)]
    placements = placements_from_scene_text(scene_text, exact_names)
    return quests, placements


def coverage_from_test_texts(files: dict[str, str]) -> dict[str, list[str]]:
    """Marke -> Dateiliste, fuer Fixtures und den Repo-Abgleich.

    Nur geformte Marken zaehlen (quest:h1 / placement:DiabloZone:Name);
    ein Tippfehler erzeugt keine Deckung und faellt als uncovered auf.
    """
    cov: dict[str, list[str]] = {}
    for path, text in files.items():
        for key in QUEST_MARK_RE.findall(text):
            if re.fullmatch(r"[a-z]+\d+", key):
                cov.setdefault(f"quest:{key}", []).append(path)
        for key in PLACEMENT_MARK_RE.findall(text):
            if re.fullmatch(r"[A-Za-z0-9_]+\:[A-Za-z0-9_]+", key):
                cov.setdefault(f"placement:{key}", []).append(path)
    return cov


def find_problems(
    quests: list[str], placements: list[str], coverage: dict[str, list[str]]
) -> list[str]:
    """Neue Inhalte ohne Fall, geloeschte Tests (baumelnde Marken),
    doppelte Kennungen und fehlendes Inventar muessen auffallen."""
    problems: list[str] = []
    inventory = list(quests) + list(placements)
    if not quests:
        problems.append("missing inventory: no quests derived from StoryModel.cs")
    if not placements:
        problems.append(
            "missing inventory: no placements derived from DiabloZone.unity"
        )
    seen: set[str] = set()
    for key in inventory:
        if key in seen:
            problems.append(f"duplicate inventory key: {key}")
        seen.add(key)
    for key in sorted(seen):
        if key not in coverage:
            problems.append(f"uncovered: {key} (no test references it)")
    known = seen
    for key in sorted(coverage):
        if key not in known and (
            key.startswith("quest:") or key.startswith("placement:")
        ):
            refs = ", ".join(sorted(set(coverage[key])))
            problems.append(f"dangling: {key} (referenced by {refs}, not in inventory)")
    return sorted(problems)


def read_test_files(root: Path) -> dict[str, str]:
    files: dict[str, str] = {}
    for test_root in TEST_ROOTS:
        base = root / test_root
        if not base.is_dir():
            continue
        for path in sorted(base.glob("*.cs")):
            try:
                files[path.name] = path.read_text(encoding="utf-8", errors="strict")
            except (OSError, UnicodeError) as exc:
                raise RuntimeError(f"unreadable test file: {path}: {exc}")
    return files


def problems(root: Path | None = None) -> list[str]:
    repo = root or Path(__file__).resolve().parent.parent
    try:
        model_text = (repo / MODEL_REL).read_text(encoding="utf-8", errors="strict")
    except (OSError, UnicodeError) as exc:
        raise RuntimeError(f"unreadable quest source {MODEL_REL}: {exc}")
    try:
        scene_text = (repo / SCENE_REL).read_text(encoding="utf-8", errors="strict")
    except (OSError, UnicodeError) as exc:
        raise RuntimeError(f"unreadable scene source {SCENE_REL}: {exc}")
    exact = exact_names_from_sources(repo)
    quests, placements = inventory_from_texts(model_text, scene_text, exact)
    coverage = coverage_from_test_texts(read_test_files(repo))
    return find_problems(quests, placements, coverage)


def main() -> int:
    try:
        found = problems()
    except RuntimeError as exc:
        print(f"check-quest-coverage: ERROR {exc}", file=sys.stderr)
        return 2
    for line in found:
        print(line)
    print(f"quest coverage: uncovered entries: {len(found)}")
    return 1 if found else 0


if __name__ == "__main__":
    raise SystemExit(main())
