"""Render the authoring index from StoryCatalog.All and its German journal entries.

Markdown-Übersicht und JSON-Export für das Agent-Board lesen dieselben
strukturierten Datensätze (`load_quests`), mit der Quest-ID als Schlüssel.
"""
import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "sandbox/Assets/Scripts"
STORY_MODEL = SCRIPTS / "Story/StoryModel.cs"
LOCALIZATION = SCRIPTS / "Localization/LocCatalog.Story.cs"
OUTPUT = ROOT / "docs/quest-inventory.md"
JSON_OUTPUT = ROOT / "docs/quest-inventory.json"

OBJECTIVE_RE = re.compile(
    r'\s*(\w+)\(StoryIds\.(\w+), (?:([0-9]+), )?"([^"]+)"\),?\s*'
)


def parse_quests(model_text, localization_text):
    """Quest-ID -> strukturierter Datensatz, in Katalogreihenfolge.

    Liest `new StoryQuest(...)` aus StoryCatalog.All und löst Titel sowie
    Zieltexte über LocCatalog.Story.cs auf. Bei unbekanntem Format oder
    fehlendem Lokalisierungstext bricht die Funktion mit ValueError ab
    (fail-closed).
    """
    labels = dict(re.findall(r'E\("(quest\.[^"]+)",\s*"([^"]*)"', localization_text))
    entries = re.findall(
        r'new StoryQuest\("(\w+)", (\d+), (true|false), StoryIds\.(\w+), new\[\]\s*'
        r'\{([^}]*)\}([^)]*)\)', model_text)
    if not entries or len(entries) != model_text.count('new StoryQuest('):
        raise ValueError("Unbekanntes Format in StoryCatalog.All")

    quests = {}
    for key, act, main, giver, objectives_text, options_text in entries:
        args = dict(re.findall(r'(\w+):\s*([^,\s]+)', options_text))
        prereq = args.get("prereq", "").strip('"').upper() or None
        objectives = []
        for objective in objectives_text.splitlines():
            if not objective.strip():
                continue
            match = OBJECTIVE_RE.fullmatch(objective)
            if not match:
                raise ValueError(objective)
            obj_type, target, count, label_key = match.groups()
            objectives.append({
                "type": obj_type,
                "target": target,
                "count": int(count) if count else 1,
                "label": labels[label_key],
            })
        quests[key] = {
            "id": key,
            "act": int(act),
            "main": main == "true",
            "giver": giver,
            "title": labels[f"quest.{key}.title"],
            "prereq": prereq,
            "prereqStatus": (
                args.get("prereqStatus", "StoryStatus.Done").split(".")[-1]
                if prereq else None
            ),
            "gate": args.get("gate", "StoryGate.None").split(".")[-1],
            "autoAccept": args.get("autoAccept") == "true",
            "turnIn": args.get("turnIn", "true") != "false",
            "catchUp": args.get("catchUp", "StoryGate.None").split(".")[-1],
            "reward": args.get("reward", "StoryReward.None").split(".")[-1],
            "rewardGold": int(args.get("rewardGold", "0")),
            "objectives": objectives,
        }
    return quests


def load_quests():
    """Strukturierte Datensätze aus den Spielquellen im Repo."""
    return parse_quests(STORY_MODEL.read_text(), LOCALIZATION.read_text())


def render(quests=None):
    """Markdown-Übersicht aus den strukturierten Datensätzen."""
    quests = load_quests() if quests is None else quests
    lines = ["# Cinderreach – Questinventar", "",
             "Quelle für Änderungen an Questregeln ist `StoryCatalog.All` in "
             "`sandbox/Assets/Scripts/Story/StoryModel.cs`. Titel, Orte im Auftragstext und "
             "Zieltexte stammen aus `LocCatalog.Story.cs`. Nach Änderungen "
             "`python3 scripts/quest_inventory.py` ausführen.", "",
             "Bei den Aktbossen kommt die Freigabe durch `ActBossGate.RequiredQuests` hinzu. "
             "Die Karte bestimmt konkrete NPC-, Fund- und Gegnerpositionen aus der laufenden Szene.", ""]
    previous_act = None
    for quest in sorted(quests.values(), key=lambda row: row["act"]):
        if quest["act"] != previous_act:
            lines.extend([f"## Akt {quest['act']}", ""])
            previous_act = quest["act"]
        kind = "Hauptquest" if quest["main"] else "Nebenquest"
        start = "automatisch" if quest["autoAccept"] else f"bei {quest['giver']}"
        requirements = []
        if quest["prereq"]:
            requirements.append(f"{quest['prereq']} mindestens {quest['prereqStatus']}")
        if quest["gate"] != "None":
            requirements.append(quest["gate"])
        rewards = [] if quest["reward"] in ("Gold", "None") else [quest["reward"]]
        if quest["rewardGold"]:
            rewards.append(f"{quest['rewardGold']} Gold")
        lines.extend([f"### {quest['id'].upper()} · {quest['title']}", "",
                      f"{kind} · Questgeber {quest['giver']} · Annahme {start}", "",
                      "- Voraussetzungen: " + (", ".join(requirements) or "keine"),
                      "- Questbelohnung: " + (", ".join(rewards) or "keine"),
                      "- Ziele und Schauplätze:"])
        for objective in quest["objectives"]:
            suffix = f" (×{objective['count']})" if objective["count"] != 1 else ""
            lines.append("  - " + objective["label"] + suffix)
        finish = "automatisch nach den Zielen" if not quest["turnIn"] else f"Abgabe bei {quest['giver']}"
        lines.append("- Abschluss: " + finish)
        if quest["catchUp"] != "None":
            lines.append("- Nachträglicher Abschluss über " + quest["catchUp"])
        lines.append("")
    return "\n".join(lines)


def render_json(quests=None):
    """JSON-Export mit Quest-ID als Schlüssel, aus denselben Datensätzen."""
    quests = load_quests() if quests is None else quests
    payload = {
        "source": "StoryCatalog.All + LocCatalog.Story.cs",
        "quests": quests,
    }
    return json.dumps(payload, ensure_ascii=False, indent=2) + "\n"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    options = parser.parse_args(argv)
    quests = load_quests()
    if options.check:
        for target, text in ((OUTPUT, render(quests)), (JSON_OUTPUT, render_json(quests))):
            if text != target.read_text():
                raise SystemExit(f"{target.name} veraltet. scripts/quest_inventory.py ausführen.")
    else:
        OUTPUT.write_text(render(quests))
        JSON_OUTPUT.write_text(render_json(quests))


if __name__ == "__main__":
    main()
