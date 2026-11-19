"""Tests for unity_test_report.py (run: python3 -m pytest scripts -q)."""
import importlib.util
from pathlib import Path

SPEC = importlib.util.spec_from_file_location(
    "unity_test_report", Path(__file__).with_name("unity_test_report.py"))
report = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(report)


def write(tmp_path, body, total, passed, failed, result):
    xml = (f'<test-run total="{total}" passed="{passed}" failed="{failed}" skipped="0" '
           f'result="{result}" duration="3.5">{body}</test-run>')
    path = tmp_path / "r.xml"
    path.write_text(xml, encoding="utf-8")
    return path


PASS = '<test-case fullname="A.Fast" result="Passed" duration="0.1"/>' \
       '<test-case fullname="A.Slow" result="Passed" duration="2.0"/>'
FAIL = ('<test-case fullname="B.Broken" result="Failed" duration="0.4"><failure>'
        '<message><![CDATA[  Expected: 38\n  But was:  40\n]]></message>'
        '<stack-trace><![CDATA[at B.Broken () [0x00001] in <x>:0\n'
        '(at Assets/Tests/EditMode/B.cs:42)]]></stack-trace></failure></test-case>')


def test_passing_run_lists_slowest_first(tmp_path):
    text, ok = report.summarize(write(tmp_path, PASS, 2, 2, 0, "Passed"), "EditMode", 5, False)
    assert ok
    assert "EditMode: 2/2 passed, 0 failed" in text
    assert text.index("A.Slow") < text.index("A.Fast")


def test_failure_shows_name_frame_and_message(tmp_path):
    text, ok = report.summarize(write(tmp_path, PASS + FAIL, 3, 2, 1, "Failed(Child)"), "EditMode", 0, False)
    assert not ok
    assert "FAIL B.Broken (Assets/Tests/EditMode/B.cs:42)" in text
    assert "Expected: 38" in text and "But was:  40" in text
    assert "slowest" not in text


def test_empty_run_fails_unless_filtered(tmp_path):
    path = write(tmp_path, "", 0, 0, 0, "Passed")
    assert not report.summarize(path, "PlayMode", 5, False)[1]
    assert report.summarize(path, "PlayMode", 5, True)[1]


def test_missing_xml_fails(tmp_path):
    text, ok = report.summarize(tmp_path / "none.xml", "PlayMode", 5, False)
    assert not ok and "missing" in text


def test_main_appends_step_summary(tmp_path, monkeypatch):
    summary = tmp_path / "summary.md"
    monkeypatch.setenv("GITHUB_STEP_SUMMARY", str(summary))
    path = write(tmp_path, PASS, 2, 2, 0, "Passed")
    assert report.main([str(path), "EditMode"]) == 0
    assert "EditMode: 2/2 passed" in summary.read_text(encoding="utf-8")
