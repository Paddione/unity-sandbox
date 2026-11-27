"""Story voice lines from the C# sources (cinderreach-story-voice).

Pure stdlib. Reads the dialog scripts (speakers per line) and the bark table
from ``sandbox/Assets/Scripts/Story`` and the German texts from the
localization catalog, and yields one entry per spoken line::

    {"key": "dlg.h1.offer.1", "speaker": "ottilie", "text": "..."}

Narration lines carry speaker ``narrator``.
"""
from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SCRIPTS = REPO / "sandbox" / "Assets" / "Scripts"
DIALOGS_CS = SCRIPTS / "Story" / "StoryDialogs.cs"
BARKS_CS = SCRIPTS / "Story" / "StoryBarks.cs"
LOC_DIR = SCRIPTS / "Localization"
VOICE_DIR = REPO / "sandbox" / "Assets" / "AddressableAssets" / "Voice"
VOICES_JSON = REPO / "docs" / "audio" / "voices.json"
LOCK_JSON = REPO / "docs" / "audio" / "voice-lines.lock.json"

_CONST = re.compile(r"const string ([^;]+);")
_DIALOG = re.compile(r'new StoryDialog\("([^"]+)"((?:,\s*[^,)]+)*)\)')
_BARK = re.compile(r'new StoryBark\("([^"]+)",\s*([^)]+)\)')
_ENTRY = re.compile(r'E\("((?:dlg|bark)\.[^"]+)",\s*"((?:[^"\\]|\\.)*)"')
_STRING_CONST = re.compile(r'(\w+)\s*=\s*(?:StoryIds\.(\w+)|"([^"]*)")')
_CS_ESC = re.compile(r'\\(u[0-9a-fA-F]{4}|["\\ntr])')

NPC_IDS = {"Ottilie": "ottilie", "Ilsabe": "ilsabe", "Jorin": "jorin", "Merle": "merle"}


def _unescape_cs(raw: str) -> str:
    """Decode C# string literal escape sequences (quotes, slashes, \\n/\\t/\\r, \\uXXXX)."""
    def _sub(m: re.Match[str]) -> str:
        tok = m.group(1)
        if tok.startswith("u"):
            return chr(int(tok[1:], 16))
        return {'"': '"', "\\": "\\", "n": "\n", "t": "\t", "r": "\r"}[tok]

    return _CS_ESC.sub(_sub, raw)


def _consts(src: str) -> dict[str, str]:
    out: dict[str, str] = {}
    for block in _CONST.findall(src):
        for name, ref, lit in _STRING_CONST.findall(block):
            out[name] = NPC_IDS.get(ref, lit) if ref else lit
    return out


def _speaker(token: str, consts: dict[str, str]) -> str:
    token = token.strip()
    base = token.split("+")[0].strip()
    if base.startswith('"'):
        value = base.strip('"')
    else:
        value = consts.get(base, base)
    return "narrator" if value == "n" else value


def catalog_texts() -> dict[str, str]:
    texts: dict[str, str] = {}
    for cs in sorted(LOC_DIR.glob("LocCatalog*.cs")):
        for key, de in _ENTRY.findall(cs.read_text(encoding="utf-8")):
            texts[key] = _unescape_cs(de)
    return texts


def lines() -> list[dict[str, str]]:
    texts = catalog_texts()
    out: list[dict[str, str]] = []
    dsrc = DIALOGS_CS.read_text(encoding="utf-8")
    consts = _consts(dsrc)
    for did, rest in _DIALOG.findall(dsrc):
        tokens = [t for t in rest.split(",") if t.strip()]
        for i, tok in enumerate(tokens):
            key = f"dlg.{did}.{i + 1}"
            out.append({"key": key, "speaker": _speaker(tok, consts), "text": texts.get(key, "")})
    bsrc = BARKS_CS.read_text(encoding="utf-8")
    bconsts = _consts(bsrc)
    for bid, tok in _BARK.findall(bsrc):
        key = f"bark.{bid}"
        out.append({"key": key, "speaker": _speaker(tok, bconsts), "text": texts.get(key, "")})
    return out


def tts_text(text: str) -> str:
    """Text as sent to the TTS model: catalog text with the pronunciation fixes from voices.json."""
    for word, spoken in json.loads(VOICES_JSON.read_text(encoding="utf-8")).get("pronounce", {}).items():
        text = re.sub(rf"\b{re.escape(word)}\b", spoken, text)
    return text


def line_hash(entry: dict[str, str], voices: dict) -> str:
    voice = voices[entry["speaker"]]
    blob = json.dumps([tts_text(entry["text"]), entry["speaker"], voice.get("ref_text", ""), voice.get("seed", 0),
                       voice.get("instruct", "")], ensure_ascii=False)
    return hashlib.sha1(blob.encode("utf-8")).hexdigest()[:12]


def load_voices() -> dict:
    return json.loads(VOICES_JSON.read_text(encoding="utf-8"))["voices"]


def load_lock() -> dict:
    if not LOCK_JSON.exists():
        return {}
    return json.loads(LOCK_JSON.read_text(encoding="utf-8"))["lines"]
