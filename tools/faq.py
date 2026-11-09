"""Writes docs/FAQ.md from the plugin's one FAQ list (system plan 4.19).

The list is src/TenshoAssistant/Help/Faq.cs (the order and the keys); the texts are Help.Question<Key> and Help.Answer<Key> in the English
and French language files. The page is English first, then the line « Français : », then French (the format of tools/bilingual.py).
A test (FaqTests) and check-docs refuse a docs/FAQ.md that is out of date.

Usage (from the repository root):
  python -I -X utf8 tools/faq.py          print the page
  python -I -X utf8 tools/faq.py write    write docs/FAQ.md
  python -I -X utf8 tools/faq.py check    exit 1 if docs/FAQ.md is out of date
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LIST = ROOT / "src/TenshoAssistant/Help/Faq.cs"
LANGUAGES = ROOT / "src/TenshoAssistant/Localization/Languages"
OUT = ROOT / "docs/FAQ.md"
QA = "https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a"
MARKER = "Français :"

HEAD = {
    "en": ("# Frequent questions",
           "The same questions are in the plugin, on the Help page (`/tensho`, then Help), in your language, and its search finds them. "
           f"Ask a new question in [Discussions › Q&A]({QA}); a question that comes back joins this list. A problem or an idea goes in "
           "[a ticket](https://github.com/Shigu330/Tensho-Assistant/issues/new/choose)."),
    "fr": ("# Questions fréquentes",
           "Les mêmes questions sont dans le plugin, sur la page Aide (`/tensho`, puis Aide), dans votre langue, et sa recherche les trouve. "
           f"Posez une nouvelle question dans [Discussions › Q&A]({QA}) ; une question qui revient rejoint cette liste. Un problème ou une "
           "idée va dans [un ticket](https://github.com/Shigu330/Tensho-Assistant/issues/new/choose)."),
}


def keys():
    found = re.findall(r'new\("(\w+)"(?:,\s*"\w+")?\)', LIST.read_text(encoding="utf-8"))
    if not found or len(found) != len(set(found)):
        raise SystemExit(f"faq: no list, or a key twice, in {LIST.relative_to(ROOT)}")
    return found


def texts(code):
    help_ = json.loads((LANGUAGES / f"{code}.json").read_text(encoding="utf-8-sig"))["Help"]
    pairs = []
    for key in keys():
        question, answer = help_.get(f"Question{key}", "").strip(), help_.get(f"Answer{key}", "").strip()
        if not question or not answer:
            raise SystemExit(f"faq: {code}: Help.Question{key} or Help.Answer{key} is missing")
        pairs.append((question, answer))
    return pairs


def page():
    lines = ["<!-- Written by tools/faq.py from src/TenshoAssistant/Help/Faq.cs and the English and French language files. "
             "Do not edit by hand: change the list or the texts, then run « python -I -X utf8 tools/faq.py write ». -->", ""]
    for n, code in enumerate(("en", "fr")):
        if n:
            lines += [MARKER, ""]
        title, intro = HEAD[code]
        lines += [title, "", intro, ""]
        for question, answer in texts(code):
            lines += [f"## {question}", "", answer, ""]
    return "\n".join(lines)


def main(argv):
    text = page()
    if not argv:
        sys.stdout.write(text)
        return 0
    if argv == ["write"]:
        OUT.write_text(text, encoding="utf-8", newline="\n")
        print(f"faq: {OUT.relative_to(ROOT)} written ({len(keys())} questions)")
        return 0
    if argv == ["check"]:
        now = OUT.read_text(encoding="utf-8") if OUT.exists() else ""
        if now.replace("\r\n", "\n") != text:
            print("faq: docs/FAQ.md is out of date: python -I -X utf8 tools/faq.py write")
            return 1
        return 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
