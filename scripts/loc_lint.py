"""Python mirror of LocLint (sandbox/Assets/Tests/EditMode/LocalizationTests.cs) for fast iteration.

Usage: python3 scripts/loc_lint.py sandbox/Assets/Scripts  (prints <path>\t<literal> findings)
"""
import re, sys, os
Literal = re.compile(r'(?P<pre>\$@|@\$|\$|@)?"(?P<body>(?:[^"\\\n]|\\.)*)"')
Hole = re.compile(r'\{[^{}]*\}')
Word = re.compile(r'[A-Za-zÄÖÜäöüß]{2,}')
Fragment = re.compile(r'[.:!?—]')
Sink = re.compile(r'(return\s*$|\.text\s*=\s*$|[?:]\s*$|MakeSmallButton\(\s*$|label\s*[:=]\s*$|title\s*[:=]\s*$)')
Skip = ["Debug.Log","Debug.Assert","Debug.LogWarning","Debug.LogError","[Header(","[Tooltip(","[MenuItem(","new GameObject(","Resources.Load",".Find(","CompareTag","Shader.Find","Animator.StringToHash","PlayerPrefs.","Console.Write","[assembly","FindGameObjectsWithTag","SetTrigger(","SetBool(","SetFloat(","SetInteger(",'Play("',"LogWarning(","LogError("]
def is_player(body, before):
    text = Hole.sub(" ", body).replace("\\n"," ")
    words = Word.findall(text)
    if not words: return False
    if "/" in text and " " not in text: return False
    if "_" in text and " " not in text: return False
    if len(words)>=2 and " " in text.strip(): return True
    w0=words[0]
    if len(w0)>=3 and w0.upper()==w0 and text.strip()==w0: return True
    if w0[0].isupper() and text.strip()==w0 and Sink.search(before.rstrip()): return True
    if " " not in text.strip() and re.fullmatch(r'[\w.-]+\.\w{2,4}', text.strip()): return False  # file names
    if "_" not in text and "/" not in text and Fragment.search(text) and any(len(w)>=3 and w[0].isupper() for w in words): return True
    return False
def scan_text(src):
    inblock=False
    for line in src.split("\n"):
        if inblock:
            e=line.find("*/")
            if e<0: continue
            line=line[e+2:]; inblock=False
        t=line.lstrip()
        if t.startswith("//") or t.startswith("#"): continue
        if t.startswith("/*"):
            if "*/" not in t: inblock=True
            continue
        if any(m in line for m in Skip): continue
        for m in Literal.finditer(line):
            if is_player(m.group("body"), line[:m.start()]): yield m.group("body")
root=sys.argv[1]
out=[]
for d,_,fs in os.walk(root):
    for f in sorted(fs):
        if not f.endswith(".cs"): continue
        p=os.path.join(d,f); rel=os.path.relpath(p,root).replace("\\","/")
        if rel.startswith("Localization/"): continue
        for h in scan_text(open(p,encoding="utf-8-sig").read()): out.append(rel+"\t"+h)
for x in sorted(set(out)): print(x)
