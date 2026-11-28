from quest_inventory import OUTPUT, render


def test_document_matches_game_catalog():
    assert OUTPUT.read_text() == render()
