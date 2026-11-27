import re
import subprocess
import tempfile
from pathlib import Path

import pytest

SCRIPT = Path(__file__).with_name("run-coop-pair.sh")


def _extract_functions() -> str:
    text = SCRIPT.read_text()
    helpers = "\n".join(
        line for line in text.splitlines()
        if re.match(r"^(cdir|csave|cclass|crole)\(\)", line)
    )
    m = re.search(r"(?ms)^start_client\(\)\s*\{.*?^\}", text)
    assert m is not None, "start_client() not found in run-coop-pair.sh"
    return helpers + "\n" + m.group(0)


def _run_start_client(
    *,
    server: str,
    client_god: str,
    client_class: str,
    k: int,
    target_args: list[str],
    extra_env: dict[str, str] | None = None,
) -> tuple[str, list[str]]:
    funcs = _extract_functions()
    quoted_targets = " ".join(repr(a) for a in target_args)
    with tempfile.NamedTemporaryFile("w+b", suffix=".bin") as tf:
        bash_code = f"""
set -euo pipefail
TARGET_ARGS=({quoted_targets})
{funcs}
OUT="/tmp/coop-out"
TIMEOUT="1200"
PLAYER="/tmp/player"
LAUNCH=(xvfb-run -a)
COMMON=(-screen-width 960)
SERVER="${{COOP_SERVER:-0}}"
CLIENT_GOD=(-autoplay-god)
[ "${{COOP_CLIENT_GOD:-1}}" = 0 ] && CLIENT_GOD=()
SERVER_PEERS=()
[ "$SERVER" = 1 ] && SERVER_PEERS=(-coop-peers 2)
CLIENT_PIDS=()
timeout() {{
  printf '%s\\0' "$@" > "$CAPTURE_FILE"
}}
start_client "$CLIENT_K" "${{TARGET_ARGS[@]}}"
wait "${{CLIENT_PIDS[0]}}"
"""
        env = {
            "PATH": "/usr/bin:/bin",
            "CAPTURE_FILE": tf.name,
            "COOP_SERVER": server,
            "COOP_CLIENT_GOD": client_god,
            "COOP_CLIENT_CLASS": client_class,
            "CLIENT_K": str(k),
        }
        if extra_env:
            env.update(extra_env)
        res = subprocess.run(
            ["bash", "-c", bash_code],
            env=env,
            capture_output=True,
            text=True,
        )
        assert res.returncode == 0, res.stderr
        raw = Path(tf.name).read_bytes()
        captured = [item.decode("utf-8") for item in raw.split(b"\0")[:-1]]
        return res.stdout.strip(), captured


@pytest.mark.parametrize(
    ("server", "client_god", "client_class", "k", "target_args", "expected_role", "expected_dir", "expected_save", "extra_env"),
    [
        ("0", "1", "", 1, ["-join", "127.0.0.1:7790"], "client", "client", "coop-client.json", None),
        ("0", "0", "rogue", 2, ["-join", "127.0.0.1:7790"], "client", "client2", "coop-client2.json", {"COOP_CLIENT2_CLASS": "rogue"}),
        ("1", "1", "fenwalker", 1, ["-join-server", "-server-name", "Cinderreach Test"], "lead", "client", "coop-client.json", None),
        ("1", "0", "", 2, ["-join-server", "-server-name", "Cinderreach Test"], "follow", "client2", "coop-client2.json", None),
    ],
)
def test_start_client_arg_assembly(
    server,
    client_god,
    client_class,
    k,
    target_args,
    expected_role,
    expected_dir,
    expected_save,
    extra_env,
):
    stdout, captured = _run_start_client(
        server=server,
        client_god=client_god,
        client_class=client_class,
        k=k,
        target_args=target_args,
        extra_env=extra_env,
    )
    effective_cls = (extra_env or {}).get("COOP_CLIENT2_CLASS", client_class)
    if effective_cls:
        assert stdout == f"run-coop-pair: client {k} -> {' '.join(target_args)} (class {effective_cls})"
    else:
        assert stdout == f"run-coop-pair: client {k} -> {' '.join(target_args)}"

    expected = [
        "--kill-after=30",
        "1200",
        "xvfb-run",
        "-a",
        "/tmp/player",
        "-screen-width",
        "960",
    ]
    if client_god != "0":
        expected.append("-autoplay-god")
    expected.extend(target_args)
    expected.extend(["-coop-autoplay", expected_role])
    if server == "1":
        expected.extend(["-coop-peers", "2"])
    if effective_cls:
        expected.extend(["-class", effective_cls])
    expected.extend([
        "-save-file",
        expected_save,
        "-player-name",
        f"Client{k}",
        "-autoplay-out",
        f"/tmp/coop-out/{expected_dir}",
        "-logFile",
        f"/tmp/coop-out/{expected_dir}/player.log",
    ])
    assert captured == expected
