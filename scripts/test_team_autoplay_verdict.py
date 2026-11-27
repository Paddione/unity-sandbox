import json
from pathlib import Path

import importlib.util

SPEC = importlib.util.spec_from_file_location("team_autoplay_verdict", Path(__file__).with_name("team_autoplay_verdict.py"))
tav = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(tav)

KILLS = ["1:Kilnmother:-", "2:Glasswright:-", "3:Thewes:-"]


def _client(role, cls, difficulty="Normal", **over):
    d = {
        "role": role, "classId": cls, "exit": 0, "exceptions": 0, "difficulty": difficulty,
        "finalBossDefeated": True, "kindledUnlocked": True, "kindledSaved": True,
        "phases": ["town", "boss4"], "questsDone": 40, "questsTotal": 40, "questsOpen": [],
        "actBossKills": KILLS if role == "lead" else [],
    }
    d.update(over)
    return d


def _session(tmp_path: Path, difficulty="Normal", server_log=None, **client_over) -> Path:
    s = tmp_path / "normal"
    for name, role, cls in (("client1", "lead", "fenwalker"), ("client2", "follow", "lanternbearer"),
                            ("client3", "follow", "rogue")):
        (s / name).mkdir(parents=True)
        d = _client(role, cls, difficulty, **client_over.get(name, {}))
        (s / name / "autoplay-result.json").write_text(json.dumps(d))
    (s / "server").mkdir()
    (s / "server" / "player.log").write_text(server_log if server_log is not None else
                                             "[Server] session over, all players left: quitting\n")
    return s


def test_green_normal_session_passes(tmp_path):
    v = tav.judge(_session(tmp_path), "Normal")
    assert v["pass"], v["problems"]
    assert v["lead"]["fellLast"]
    assert v["classes"] == ["fenwalker", "lanternbearer", "rogue"]


def test_duplicate_class_fails(tmp_path):
    v = tav.judge(_session(tmp_path, client3={"classId": "lanternbearer"}), "Normal")
    assert not v["pass"]
    assert any("classes" in p for p in v["problems"])


def test_follower_without_kindled_save_fails(tmp_path):
    v = tav.judge(_session(tmp_path, client2={"kindledSaved": False}), "Normal")
    assert any("client2: Kindled unlock not in the save" == p for p in v["problems"])


def test_lead_open_quests_and_boss_order_fail(tmp_path):
    v = tav.judge(_session(tmp_path, client1={"questsOpen": ["h7"], "questsDone": 39,
                                              "actBossKills": ["1:Kilnmother:h3", "2:Glasswright:-"]}), "Normal")
    assert any(p.startswith("lead: quests") for p in v["problems"])
    assert any(p.startswith("lead: act bosses") for p in v["problems"])


def test_kindled_needs_server_adoption_line(tmp_path):
    s = _session(tmp_path, "Kindled")
    v = tav.judge(s, "Kindled")
    assert any(p.startswith("server: no Kindled adoption") for p in v["problems"])
    log = ("[Difficulty] server adopts Kindled from client 1: hp x1.8 damage x1.8 gold x1.5 unique x2 affixTier 2\n"
           "[Server] session over\n")
    (s / "server" / "player.log").write_text(log)
    v = tav.judge(s, "Kindled")
    assert v["pass"], v["problems"]


def test_server_must_end_itself_and_results_must_exist(tmp_path):
    s = _session(tmp_path, server_log="")
    (s / "client3" / "autoplay-result.json").unlink()
    v = tav.judge(s, "Normal")
    assert "server: did not end the session by itself" in v["problems"]
    assert "client3: no autoplay-result.json" in v["problems"]


def test_main_writes_verdict_file(tmp_path):
    s = _session(tmp_path)
    assert tav.main([str(s), "--difficulty", "Normal"]) == 0
    assert json.loads((s / "team-verdict.json").read_text())["pass"] is True
