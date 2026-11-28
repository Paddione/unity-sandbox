import json

from quest_inventory import JSON_OUTPUT, OUTPUT, load_quests, render, render_json


def test_document_matches_game_catalog():
    assert OUTPUT.read_text() == render()


def test_json_matches_game_catalog():
    assert JSON_OUTPUT.read_text() == render_json()


def test_json_and_markdown_share_records():
    quests = load_quests()
    data = json.loads(render_json(quests))
    # Quest-ID als Schlüssel, vollständig und eindeutig.
    assert set(data["quests"]) == set(quests)
    assert len(data["quests"]) == len(quests)
    # Jeder Eintrag trägt die auflösbaren Felder, die auch die Markdown-Ansicht zeigt.
    for quest_id, record in data["quests"].items():
        assert record["id"] == quest_id
        assert record["title"]
        assert isinstance(record["objectives"], list) and record["objectives"]
        for objective in record["objectives"]:
            assert objective["label"]
