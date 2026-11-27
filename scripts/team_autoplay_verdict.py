#!/usr/bin/env python3
"""Team-autoplay verdict (cinderreach-team-autoplay K2/K3).

Reads one session directory written by run-team-autoplay.sh:
  <session>/server/player.log
  <session>/client1..3/autoplay-result.json (+ player.log)
and checks the team criteria:
  - every client: exit 0, 0 exceptions, Thewes down, Kindled unlocked and saved;
  - three different classes (fenwalker, lanternbearer, rogue), client 1 is the lead;
  - the lead: every quest Done, act bosses 1-3 fell last, boss4 reached;
  - difficulty: all clients on the expected profile; on Kindled the server log
    proves the adopted profile with its multipliers;
  - the server ended the session by itself.
Writes <session>/team-verdict.json and exits 0 (pass) or 1 (fail).

Usage: team_autoplay_verdict.py <session_dir> --difficulty Normal|Kindled
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

CLASSES = {"fenwalker", "lanternbearer", "rogue"}
CLIENTS = ("client1", "client2", "client3")
SERVER_OVER = "[Server] session over"
SERVER_ADOPTS = "[Difficulty] server adopts"


def fell_last(kills: list[str]) -> bool:
    acts = set()
    for k in kills or []:
        if not k or not k.endswith(":-"):
            return False
        acts.add(k.split(":")[0])
    return {"1", "2", "3"} <= acts


def judge(session: Path, difficulty: str) -> dict:
    problems: list[str] = []
    clients: dict[str, dict] = {}
    for name in CLIENTS:
        path = session / name / "autoplay-result.json"
        if not path.is_file():
            problems.append(f"{name}: no autoplay-result.json")
            continue
        try:
            d = json.loads(path.read_text())
        except (OSError, json.JSONDecodeError) as e:
            problems.append(f"{name}: unreadable result ({e})")
            continue
        clients[name] = d
        if d.get("exit") != 0:
            problems.append(f"{name}: exit {d.get('exit')} phases={','.join(d.get('phases', []))}")
        if d.get("exceptions", 1) != 0:
            problems.append(f"{name}: {d.get('exceptions')} exceptions")
        if not d.get("finalBossDefeated"):
            problems.append(f"{name}: Thewes not down")
        if not d.get("kindledUnlocked"):
            problems.append(f"{name}: Kindled not unlocked")
        if not d.get("kindledSaved"):
            problems.append(f"{name}: Kindled unlock not in the save")
        if str(d.get("difficulty", "")).lower() != difficulty.lower():
            problems.append(f"{name}: difficulty {d.get('difficulty')} (want {difficulty})")

    classes = {d.get("classId") for d in clients.values()}
    if len(clients) == len(CLIENTS) and classes != CLASSES:
        problems.append(f"classes {sorted(c or '?' for c in classes)} (want {sorted(CLASSES)})")

    lead = clients.get("client1")
    if lead is not None:
        if lead.get("role") != "lead":
            problems.append(f"client1: role {lead.get('role')} (want lead)")
        if lead.get("questsOpen") or lead.get("questsDone", 0) < lead.get("questsTotal", 1):
            problems.append(f"lead: quests {lead.get('questsDone')}/{lead.get('questsTotal')} open={lead.get('questsOpen')}")
        if not fell_last(lead.get("actBossKills", [])):
            problems.append(f"lead: act bosses not proven last {lead.get('actBossKills')}")
        if "boss4" not in lead.get("phases", []):
            problems.append("lead: boss4 not reached")
    for name in CLIENTS[1:]:
        d = clients.get(name)
        if d is not None and d.get("role") != "follow":
            problems.append(f"{name}: role {d.get('role')} (want follow)")

    server_log = session / "server" / "player.log"
    text = server_log.read_text(errors="replace") if server_log.is_file() else ""
    if SERVER_OVER not in text:
        problems.append("server: did not end the session by itself")
    adopted = [line.strip() for line in text.splitlines() if SERVER_ADOPTS in line]
    if difficulty.lower() == "kindled":
        if not any("Kindled" in line and "hp x1.8" in line for line in adopted):
            problems.append(f"server: no Kindled adoption with multipliers in the log ({adopted[:1]})")

    return {
        "schema": 1,
        "session": session.name,
        "difficulty": difficulty,
        "pass": not problems,
        "problems": problems,
        "classes": sorted(c for c in classes if c),
        "serverDifficulty": adopted[0] if adopted else "",
        "lead": {
            "quests": f"{lead.get('questsDone')}/{lead.get('questsTotal')}" if lead else "",
            "actBossKills": lead.get("actBossKills", []) if lead else [],
            "fellLast": fell_last(lead.get("actBossKills", [])) if lead else False,
        },
        "clients": {
            n: {k: d.get(k) for k in ("role", "classId", "exit", "exceptions", "difficulty",
                                      "finalBossDefeated", "kindledUnlocked", "kindledSaved",
                                      "nextRunKindled", "duration_s")}
            for n, d in clients.items()
        },
    }


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("session", type=Path)
    ap.add_argument("--difficulty", default="Normal", choices=["Normal", "Kindled"])
    args = ap.parse_args(argv)
    verdict = judge(args.session, args.difficulty)
    (args.session / "team-verdict.json").write_text(json.dumps(verdict, indent=2) + "\n")
    state = "PASS" if verdict["pass"] else "FAIL"
    print(f"team-verdict {args.session.name} ({args.difficulty}): {state}")
    for p in verdict["problems"]:
        print(f"  - {p}")
    return 0 if verdict["pass"] else 1


if __name__ == "__main__":
    sys.exit(main())
