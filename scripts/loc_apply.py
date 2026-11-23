"""Apply a localisation spec to Cinderreach runtime code (cinderreach-deutsch-story S2).

Spec lines (UTF-8, '#' comments, fields separated by ' | '):

    <path under Assets/Scripts> | <literal as in code, quotes included> | <key> | <German text>
    <path under Assets/Scripts> | <literal> | allow
    - | - | <key> | <German text> | <English text>      (catalog entry only)

A plain literal becomes Loc.T("key"); an interpolated $"..." literal becomes
Loc.F("key", hole1, hole2, ...) with the English template built from the holes
("{secs:0}" -> "{0:0}"). The German text uses the same {n[:fmt]} placeholders.
Entries land in sandbox/Assets/Scripts/Localization/LocCatalog.<Domain>.cs, the
domain being the key's first segment. Matching lines leave loc-pending.txt;
'allow' lines move there to loc-allowlist.txt. A key that already exists is
reused (its texts must match). Every literal must occur in its file.

Usage: python3 scripts/loc_apply.py <spec> [--dry-run]
"""
import re
import sys
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPTS = ROOT / "sandbox/Assets/Scripts"
LOCDIR = SCRIPTS / "Localization"
TESTS = ROOT / "sandbox/Assets/Tests/EditMode"
HOLE = re.compile(r"\{((?:[^{}]|\{[^{}]*\})*)\}")


def split_hole(h):
    """'secs:0' -> ('secs', ':0'); respects ?: and nested parens/quotes."""
    depth, q = 0, False
    for i, c in enumerate(h):
        if c == '"':
            q = not q
        elif not q:
            if c in "([":
                depth += 1
            elif c in ")]":
                depth -= 1
            elif c == "?" and depth == 0:
                return h, ""  # ternary: no format suffix possible after it safely
            elif c in ":," and depth == 0:
                return h[:i], h[i:]
    return h, ""


def cs_escape(s):
    return s.replace("\\", "\\\\").replace('"', '\\"')


def parse_literal(lit):
    """Return (is_interpolated, english_template, [hole expressions])."""
    m = re.fullmatch(r'(\$?)"(.*)"', lit, re.S)
    if not m:
        raise SystemExit(f"bad literal {lit!r}")
    interp, body = bool(m.group(1)), m.group(2)
    body = body.replace('\\"', '"').replace("\\\\", "\\")
    if not interp:
        return False, body, []
    args, out, i = [], [], 0
    pos = 0
    for hm in HOLE.finditer(body):
        out.append(body[pos:hm.start()])
        expr, fmt = split_hole(hm.group(1))
        out.append("{%d%s}" % (len(args), fmt))
        args.append(expr.strip())
        pos = hm.end()
    out.append(body[pos:])
    tmpl = "".join(out)
    return True, tmpl, args


def map_holes(de, args, errors, n):
    """German may repeat the code's holes ({expr[:fmt]}); map them to {index[:fmt]}."""
    used = set()

    def one(m):
        expr, fmt = split_hole(m.group(1))
        expr = expr.strip()
        if re.fullmatch(r"\d+", expr):
            return m.group(0)
        hits = [i for i, a in enumerate(args) if a == expr]
        if hits:
            free = [i for i in hits if i not in used]
            idx = free[0] if free else hits[0]
            used.add(idx)
            return "{%d%s}" % (idx, fmt)
        errors.append(f"{n}: German hole {{{expr}}} not in code holes {args}")
        return m.group(0)
    return HOLE.sub(one, de)


def domain_file(domain):
    name = domain[:1].upper() + domain[1:]
    return LOCDIR / f"LocCatalog.{name}.cs", name


def load_catalog():
    entries = {}
    pat = re.compile(r'E\("([^"]+)",\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"\)')
    for f in LOCDIR.glob("LocCatalog.*.cs"):
        for m in pat.finditer(f.read_text(encoding="utf-8")):
            entries[m.group(1)] = (m.group(2), m.group(3))
    return entries


def add_entry(key, de, en, dry):
    domain = key.split(".")[0]
    path, name = domain_file(domain)
    line = f'        E("{key}", "{cs_escape(de)}", "{cs_escape(en)}"),\n'
    if dry:
        return
    if not path.exists():
        path.write_text(
            f"/// <summary>Player text, domain '{domain}' (cinderreach-deutsch-story).</summary>\n"
            "public static partial class LocCatalog\n{\n"
            f"    static readonly Entry[] {name} =\n    {{\n    }};\n}}\n", encoding="utf-8")
        (path.parent / (path.name + ".meta")).write_text(
            f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n")
        core = LOCDIR / "LocCatalog.cs"
        text = core.read_text(encoding="utf-8")
        anchor = "        yield return Names;\n"
        text = text.replace(anchor, anchor + f"        yield return {name};\n")
        core.write_text(text, encoding="utf-8")
    text = path.read_text(encoding="utf-8")
    idx = text.rindex("    };")
    path.write_text(text[:idx] + line + text[idx:], encoding="utf-8")


def main():
    spec, dry = Path(sys.argv[1]), "--dry-run" in sys.argv
    catalog = load_catalog()
    pending_path, allow_path = TESTS / "loc-pending.txt", TESTS / "loc-allowlist.txt"
    pending = pending_path.read_text(encoding="utf-8").splitlines()
    allow = allow_path.read_text(encoding="utf-8").splitlines()
    errors, done = [], 0
    for n, raw in enumerate(spec.read_text(encoding="utf-8").splitlines(), 1):
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        parts = raw.split(" | ")
        parts = [p.strip() for p in parts[:3]] + parts[3:]  # texts keep their spaces
        rel, lit = parts[0], parts[1]
        if rel == "-":  # catalog-only entry: - | - | key | de | en
            key, de, en = parts[2], parts[3], parts[4]
            if key in catalog and catalog[key] != (cs_escape(de), cs_escape(en)):
                errors.append(f"{n}: key {key} exists with other text {catalog[key]}")
            elif key not in catalog:
                add_entry(key, de, en, dry)
                catalog[key] = (cs_escape(de), cs_escape(en))
            done += 1
            continue
        path = SCRIPTS / rel
        src = path.read_text(encoding="utf-8")
        if lit not in src:
            errors.append(f"{n}: literal not in {rel}: {lit}")
            continue
        interp, en, args = parse_literal(lit)
        body = lit[lit.index('"') + 1:-1]
        lint_line = f"{rel}\t{body}"
        if parts[2] == "allow":
            if lint_line in pending:
                pending.remove(lint_line)
            if lint_line not in allow:
                allow.append(lint_line)
            done += 1
            continue
        key, de = parts[2], parts[3]
        if key in catalog and catalog[key] != (cs_escape(de), cs_escape(en)):
            errors.append(f"{n}: key {key} exists with other text {catalog[key]}")
            continue
        if interp:
            de = map_holes(de, args, errors, n)
        holes_de = sorted(set(re.findall(r"\{(\d+)", de)))
        holes_en = sorted(set(re.findall(r"\{(\d+)", en)))
        if holes_de != holes_en:
            errors.append(f"{n}: placeholder mismatch de {holes_de} en {holes_en} ({key})")
            continue
        call = f'Loc.F("{key}", {", ".join(args)})' if args else f'Loc.T("{key}")'
        if key not in catalog:
            add_entry(key, de, en, dry)
            catalog[key] = (cs_escape(de), cs_escape(en))
        if not dry:
            path.write_text(src.replace(lit, call), encoding="utf-8")
        while lint_line in pending:
            pending.remove(lint_line)
        done += 1
    if not dry:
        pending_path.write_text("\n".join(pending) + "\n", encoding="utf-8")
        allow_path.write_text("\n".join(allow) + "\n", encoding="utf-8")
    for e in errors:
        print("ERROR", e)
    print(f"applied {done}, errors {len(errors)}, pending {sum(1 for l in pending if l and not l.startswith('#'))}")
    sys.exit(1 if errors else 0)


if __name__ == "__main__":
    main()
