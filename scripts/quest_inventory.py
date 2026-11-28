"""Render the authoring index from StoryCatalog.All and its German journal entries."""
import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "sandbox/Assets/Scripts"
OUTPUT = ROOT / "docs/quest-inventory.md"


def render():
    model = (SCRIPTS / "Story/StoryModel.cs").read_text()
    localization = (SCRIPTS / "Localization/LocCatalog.Story.cs").read_text()
    labels = dict(re.findall(r'E\("(quest\.[^"]+)",\s*"([^"]*)"', localization))
    entries = re.findall(
        r'new StoryQuest\("(\w+)", (\d+), (true|false), StoryIds\.(\w+), new\[\]\s*'
        r'\{([^}]*)\}([^)]*)\)', model)
    if not entries or len(entries) != model.count('new StoryQuest('):
        raise ValueError("Unbekanntes Format in StoryCatalog.All")
    lines = ["# Cinderreach – Questinventar", "",
             "Quelle für Änderungen an Questregeln ist `StoryCatalog.All` in "
             "`sandbox/Assets/Scripts/Story/StoryModel.cs`. Titel, Orte im Auftragstext und "
             "Zieltexte stammen aus `LocCatalog.Story.cs`. Nach Änderungen "
             "`python3 scripts/quest_inventory.py` ausführen.", "",
             "Bei den Aktbossen kommt die Freigabe durch `ActBossGate.RequiredQuests` hinzu. "
             "Die Karte bestimmt konkrete NPC-, Fund- und Gegnerpositionen aus der laufenden Szene.", ""]
    previous_act = None
    for key, act, main, giver, objectives, options in sorted(entries, key=lambda row: int(row[1])):
        if act != previous_act:
            lines.extend([f"## Akt {act}", ""])
            previous_act = act
        args = dict(re.findall(r'(\w+):\s*([^,\s]+)', options))
        prereq = args.get("prereq", "").strip('"').upper()
        requirements = []
        if prereq:
            status = args.get("prereqStatus", "StoryStatus.Done").split(".")[-1]
            requirements.append(f"{prereq} mindestens {status}")
        if "gate" in args:
            requirements.append(args["gate"].split(".")[-1])
        reward = args.get("reward", "None").split(".")[-1]
        rewards = [] if reward in ("Gold", "None") else [reward]
        if "rewardGold" in args:
            rewards.append(args["rewardGold"] + " Gold")
        kind = "Hauptquest" if main == "true" else "Nebenquest"
        start = "automatisch" if args.get("autoAccept") == "true" else f"bei {giver}"
        lines.extend([f"### {key.upper()} · {labels[f'quest.{key}.title']}", "",
                      f"{kind} · Questgeber {giver} · Annahme {start}", "",
                      "- Voraussetzungen: " + (", ".join(requirements) or "keine"),
                      "- Questbelohnung: " + (", ".join(rewards) or "keine"),
                      "- Ziele und Schauplätze:"])
        for objective in objectives.splitlines():
            if not objective.strip():
                continue
            match = re.fullmatch(r'\s*\w+\(StoryIds\.\w+, (?:([0-9]+), )?"([^"]+)"\),?\s*', objective)
            if not match:
                raise ValueError(objective)
            count, label = match.groups()
            lines.append("  - " + labels[label] + (f" (×{count})" if count and count != "1" else ""))
        finish = "automatisch nach den Zielen" if args.get("turnIn") == "false" else f"Abgabe bei {giver}"
        lines.append("- Abschluss: " + finish)
        if "catchUp" in args:
            lines.append("- Nachträglicher Abschluss über " + args["catchUp"].split(".")[-1])
        lines.append("")
    return "\n".join(lines)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    options = parser.parse_args()
    result = render()
    if options.check:
        if result != OUTPUT.read_text():
            raise SystemExit("Questinventar veraltet. scripts/quest_inventory.py ausführen.")
    else:
        OUTPUT.write_text(result)
