"""Fixtures fuer scripts/check-quest-coverage.py (G-QUESTCOVER01).

Jede Regel bekommt eine kleine Inventar-/Zuordnungsfixture: neuer Inhalt
ohne Fall, geloeschter Test (baumelnde Marke), doppelte Kennung und
fehlendes Inventar muessen auffallen. Der letzte Fall prueft den echten
Repo-Stand (vollstaendig gedeckt).
"""
import importlib.util
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "check_quest_coverage", HERE / "check-quest-coverage.py"
)
check = importlib.util.module_from_spec(spec)
sys.modules["check_quest_coverage"] = check
spec.loader.exec_module(check)

MODEL_TWO_QUESTS = '''
public static class StoryCatalog {
    public static readonly StoryQuest[] All = {
        new StoryQuest("h1", 1, true, StoryIds.Ottilie, new[] { Talk(StoryIds.Jorin, "k") }),
        new StoryQuest("n1", 1, false, StoryIds.Ilsabe, new[] { Collect(StoryIds.Lieferbuch, 1, "k2") }),
    };
}
'''

SCENE_TWO_PLACEMENTS = """
--- !u!1 &1
GameObject:
  m_Name: StoryPickup_fuhrbrief_0
--- !u!1 &2
GameObject:
  m_Name: WaypointShrine_Town
"""

EXACT = {"StoryPickup_fuhrbrief_0", "WaypointShrine_Town"}


def _inv(model=MODEL_TWO_QUESTS, scene=SCENE_TWO_PLACEMENTS, exact=EXACT):
    return check.inventory_from_texts(model, scene, set(exact))


def test_new_quest_without_case_is_uncovered():
    quests, placements = _inv()
    cov = check.coverage_from_test_texts({"A.cs": "// quest:h1"})
    problems = check.find_problems(quests, placements, cov)
    assert any(p == "uncovered: quest:n1 (no test references it)" for p in problems), problems


def test_new_placement_without_case_is_uncovered():
    quests, placements = _inv()
    assert "placement:DiabloZone:StoryPickup_fuhrbrief_0" in placements
    assert "placement:DiabloZone:WaypointShrine_Town" in placements
    cov = check.coverage_from_test_texts(
        {"A.cs": "// quest:h1 // quest:n1 // placement:DiabloZone:StoryPickup_fuhrbrief_0"}
    )
    problems = check.find_problems(quests, placements, cov)
    assert any(
        p == "uncovered: placement:DiabloZone:WaypointShrine_Town (no test references it)"
        for p in problems
    ), problems


def test_same_prefab_second_placement_needs_own_case():
    scene = SCENE_TWO_PLACEMENTS + "--- !u!1 &3\nGameObject:\n  m_Name: StoryPickup_fuhrbrief_1\n"
    exact = set(EXACT) | {"StoryPickup_fuhrbrief_1"}
    quests, placements = check.inventory_from_texts(MODEL_TWO_QUESTS, scene, exact)
    assert "placement:DiabloZone:StoryPickup_fuhrbrief_1" in placements
    cov = check.coverage_from_test_texts(
        {
            "A.cs": "// quest:h1 // quest:n1 // placement:DiabloZone:StoryPickup_fuhrbrief_0"
            " // placement:DiabloZone:WaypointShrine_Town"
        }
    )
    problems = check.find_problems(quests, placements, cov)
    assert any("StoryPickup_fuhrbrief_1" in p and p.startswith("uncovered:") for p in problems), problems


def test_deleted_test_dangling_reference_is_problem():
    quests, placements = _inv()
    cov = check.coverage_from_test_texts({"A.cs": "// quest:h1 // quest:h2"})
    problems = check.find_problems(quests, placements, cov)
    assert any(p.startswith("dangling: quest:h2") for p in problems), problems


def test_duplicate_key_is_problem():
    quests = ["quest:h1", "quest:h1"]
    problems = check.find_problems(quests, [], {"quest:h1": ["A.cs"]})
    assert any(p.startswith("duplicate inventory key: quest:h1") for p in problems), problems


def test_missing_inventory_never_green():
    problems = check.find_problems([], [], {})
    assert any(p.startswith("missing inventory: no quests") for p in problems), problems
    assert any(p.startswith("missing inventory: no placements") for p in problems), problems


def test_prefab_override_value_counts_as_placement():
    scene = "    - target: {fileID: 1}\n      propertyPath: m_Name\n      value: Vendor\n"
    _, placements = check.inventory_from_texts("", scene, {"Vendor"})
    assert placements == ["placement:DiabloZone:Vendor"], placements


def test_repository_is_fully_covered():
    assert check.problems() == []
