#!/usr/bin/env python3
"""One bilingual format for everything the scripts write on GitHub: English first, then French (the project's choice of 2026-11-09).

  python -I -X utf8 tools/bilingual.py compose <english-file> <french-file>   prints the bilingual text
  python -I -X utf8 tools/bilingual.py check <file>                           exit 1 when a half is missing or in the wrong language
  python -I -X utf8 tools/bilingual.py --selftest

Format: the English text, a blank line, the marker line below, a blank line, the French text. It reads the same in a commit body and in
Markdown (issue comments, release notes, announcements). scripts/Release.ps1 builds commit bodies with the same marker: the self-test
reads it there, so the two cannot drift apart. Short titles, labels and milestones stay in English only.
"""
import re
import sys
from pathlib import Path

MARKER = "Français :"
ROOT = Path(__file__).resolve().parents[1]

# A few very common words of each language; enough to tell a half written in the wrong language, not a language detector.
EN_WORDS = frozenset("the and of to is in for with are from this that not when now by be it on into each".split())
FR_WORDS = frozenset("le la les des et du est pour dans une un avec pas sur qui que au aux ce cette sont par plus ne en".split())
WORD = re.compile(r"[a-zàâäçéèêëîïôöùûüÿœæ']+", re.I)


def compose(english, french):
    """The bilingual text; both halves are required (a missing translation is an error, never an empty half)."""
    english, french = english.strip(), french.strip()
    if not english or not french:
        raise ValueError("bilingual text needs an English and a French half")
    if MARKER in english.splitlines() or MARKER in french.splitlines():
        raise ValueError("a half already contains the marker line")
    return f"{english}\n\n{MARKER}\n\n{french}"


def split(text):
    """(english, french), or None when the marker line is missing."""
    lines = text.replace("\r\n", "\n").split("\n")
    if MARKER not in lines:
        return None
    at = lines.index(MARKER)
    return "\n".join(lines[:at]).strip(), "\n".join(lines[at + 1:]).strip()


def counts(text):
    words = [w.lower() for w in WORD.findall(text)]
    return sum(w in EN_WORDS for w in words), sum(w in FR_WORDS for w in words)


def problems(text):
    """What is wrong with a bilingual text: [] when it follows the format."""
    halves = split(text)
    if halves is None:
        return [f"no « {MARKER} » line: the French half is missing"]
    english, french = halves
    found = []
    if not english:
        found.append("the English half is empty")
    if not french:
        found.append("the French half is empty")
    if text.replace("\r\n", "\n").split("\n").count(MARKER) > 1:
        found.append("the marker line appears more than once")
    en, fr = counts(english)
    if en + fr >= 4 and fr > 2 * en:
        found.append("the English half reads as French")
    en, fr = counts(french)
    if en + fr >= 4 and en > 2 * fr:
        found.append("the French half reads as English")
    return found


def release_marker():
    """The marker scripts/Release.ps1 uses for commit bodies (the self-test compares it with MARKER)."""
    text = (ROOT / "scripts/Release.ps1").read_text(encoding="utf-8")
    found = re.search(r"\$bilingualMarker\s*=\s*'([^']*)'", text)
    return found.group(1) if found else None


def selftest():
    cases = [
        (compose("Progress in 0.1.0: the guide lists every page.", "Avancée en 0.1.0 : le guide liste toutes les pages."), []),
        ("Progress in 0.1.0: the guide lists every page.", ["no « Français : » line: the French half is missing"]),
        (f"The list of tools is produced from the pages.\n\n{MARKER}\n\nThe list of tools is produced from the pages.",
         ["the French half reads as English"]),
        (f"La liste des outils est tirée des pages.\n\n{MARKER}\n\nLa liste des outils est tirée des pages.", ["the English half reads as French"]),
        (f"\n\n{MARKER}\n\nTexte.", ["the English half is empty"]),
        ("## 0.2.0\n- One.\n\n## 0.1.0\n- Two.\n\n" + MARKER + "\n\n## 0.2.0\n- Un.\n\n## 0.1.0\n- Deux.", []),
    ]
    ok = 0
    for text, expected in cases:
        got = problems(text)
        if got == expected:
            ok += 1
        else:
            print(f"bilingual selftest: expected {expected}, got {got} for {text[:60]!r}")
    total = len(cases) + 3
    try:
        compose("English.", " ")
        print("bilingual selftest: compose accepted an empty half")
    except ValueError:
        ok += 1
    if split(compose("A.", "B.")) == ("A.", "B."):
        ok += 1
    else:
        print("bilingual selftest: split(compose()) does not give the halves back")
    marker = release_marker()
    if marker == MARKER:
        ok += 1
    else:
        print(f"bilingual selftest: scripts/Release.ps1 uses {marker!r}, tools/bilingual.py uses {MARKER!r}")
    print(f"bilingual selftest: {ok}/{total} ok")
    return 0 if ok == total else 1


def main(argv):
    if argv[:1] == ["--selftest"]:
        return selftest()
    if argv[:1] == ["compose"] and len(argv) == 3:
        print(compose(Path(argv[1]).read_text(encoding="utf-8"), Path(argv[2]).read_text(encoding="utf-8")))
        return 0
    if argv[:1] == ["check"] and len(argv) == 2:
        found = problems(Path(argv[1]).read_text(encoding="utf-8"))
        for line in found:
            print(line)
        return 1 if found else 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
