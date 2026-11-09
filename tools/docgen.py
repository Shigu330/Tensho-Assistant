"""Writes the parts of the public documents that the code or the files already know (system plan 4.14, documents lot 1).

Three generators, each written between its marks and nowhere else:
  index     docs/README.md, the map of the documents (English and French): one list below, and every docs/project/*.md file
            and every public .md file must be in it, so a new document cannot be forgotten
  pages     README.md, the table of the pages (English and French), from the page list src/TenshoAssistant/MainWindow.Pages.cs
            (order, group, developer mode) and the help texts PageHelp.<Page>Title and PageHelp.<Page>Purpose of en.json and fr.json
  versions  the version the documents were produced for (inline marks), from the <Version> of the csproj, and the state of the
            translations in Localization/README.md, counted in the language files; a version number written by hand in a sentence
            such as « checked on …, version 0.971 » is refused
A block is
    <!-- docgen:KEY -->
    ...produced text...
    <!-- /docgen:KEY -->
and an inline version is <!-- docgen:version -->0.1015<!-- /docgen:version -->.

The version marks follow the csproj only when the documents are produced before a release (the other generated documents do the same);
between two releases they may be older, never newer. A test (PublicDocsTests) and the document checks refuse a document out of date.

Usage (from the repository root):
  python -I -X utf8 tools/docgen.py          print every block
  python -I -X utf8 tools/docgen.py write    write the blocks and the version marks
  python -I -X utf8 tools/docgen.py check    exit 1 if a block is out of date, a version is written by hand, or the marks disagree;
                                             a version mark older than the csproj is reported (exit 0): produce them before a release
  python -I -X utf8 tools/docgen.py check --release   the same, and a version mark older than the csproj is an error too
"""
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CSPROJ = ROOT / "src/TenshoAssistant/TenshoAssistant.csproj"
PAGES = ROOT / "src/TenshoAssistant/MainWindow.Pages.cs"
LANGUAGES = ROOT / "src/TenshoAssistant/Localization/Languages"
QA = "https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a"
VERSION_MARK = re.compile(r"<!-- docgen:version -->([^<]*)<!-- /docgen:version -->")
# A version written by hand in a sentence: « version 0.971 », « checked …, 0.971 ». Feature notes such as « (0.1000) » stay allowed.
HAND_VERSION = re.compile(r"\bversion\s+0\.\d{3,4}\b|\b(?:checked|vérifié|relu)\b[^.\n]{0,60}\b0\.\d{3,4}\b", re.I)
# Public documents scanned for versions written by hand (the CHANGELOG, the evidence and the French working documents are history).
NO_HAND_VERSION = ("README.md", "CONTRIBUTING.md", "SECURITY.md", "Localization/README.md", "docs/README.md", "docs/FAQ.md",
                   "tools/README.md", "publish/d17/PULL_REQUEST.md", "publish/github/README.md", "community-help/README.md",
                   "community-templates/README.md", "community-discoveries/README.md", ".github/pull_request_template.md")

# ---------------------------------------------------------------------------------------------------------------------------- index
# One row per document or folder; paths are relative to docs/. (links, for whom en/fr, content en/fr).
INDEX = [
    ([("../README.md", None)], "everyone", "tout le monde",
     "presentation (English, then French), pages and features, installation, build, credits",
     "présentation (anglais, puis français), pages et fonctions, installation, compilation, crédits"),
    ([("../CONTRIBUTING.md", None)], "contributors", "contributeurs",
     "build, test, conventions, evidence, licences, adding a tool, versions and releases, Dalamud rules",
     "compiler, tester, conventions, preuves, licences, ajouter un outil, versions et releases, règles Dalamud"),
    ([("../SECURITY.md", None)], "everyone", "tout le monde",
     "reporting a vulnerability, sensitive surfaces of the plugin (network, local files, actions in the game)",
     "signaler une vulnérabilité, surfaces sensibles du plugin (réseau, fichiers locaux, actions dans le jeu)"),
    ([("../CHANGELOG.md", None)], "everyone", "tout le monde",
     "what changed in each version (French, then `### English`)",
     "ce qui a changé à chaque version (français, puis `### English`)"),
    ([("FAQ.md", None)], "everyone", "tout le monde",
     f"frequent questions, English then French, written by `tools/faq.py` from the plugin's list (the Help page shows the same); "
     f"new questions in [Discussions › Q&A]({QA})",
     f"questions fréquentes, en anglais puis en français, écrites par `tools/faq.py` depuis la liste du plugin (la page Aide montre les "
     f"mêmes) ; nouvelles questions dans [Discussions › Q&A]({QA})"),
    ([("project/", None)], "contributors and agent", "contributeurs et agent",
     "{core}, and the working documents listed below", "{core}, et les documents de travail listés plus bas"),
    ([("evidence/", None)], "contributors and agent", "contributeurs et agent",
     "probe outputs and log excerpts cited by the code (evidence of the data links), most named `<version>-<subject>`",
     "sorties de sondes et extraits de journaux cités par le code (preuves des liens de données), le plus souvent nommés "
     "`<version>-<sujet>`"),
    ([("../Localization/README.md", None)], "translators", "traducteurs",
     "adding or completing a language", "ajouter ou compléter une langue"),
    ([("../community-help/README.md", None), ("../community-templates/README.md", None), ("../community-discoveries/README.md", None)],
     "community", "communauté",
     "videos and guides of the Help · gallery of « My tools » · shared discoveries (exchange through GitHub, file format)",
     "vidéos et guides de l'aide · galerie de « Mes outils » · découvertes partagées (échange par GitHub, format du fichier)"),
    ([("../publish/d17/", None)], "author", "auteur",
     "draft of a submission to the official Dalamud repository, not planned (DEC-039): [PULL_REQUEST](../publish/d17/PULL_REQUEST.md), "
     "`testing/live/TenshoAssistant/manifest.toml`, icon",
     "brouillon d'une soumission au dépôt officiel Dalamud, non prévue (DEC-039) : [PULL_REQUEST](../publish/d17/PULL_REQUEST.md), "
     "`testing/live/TenshoAssistant/manifest.toml`, icône"),
    ([("../publish/github/", "../publish/github/README.md")], "maintainer", "mainteneur",
     "the 0.323 drafts of the GitHub workflows (the active ones are in `.github/`)",
     "les brouillons de la 0.323 des workflows GitHub (les actifs sont dans `.github/`)"),
    ([("../tools/", "../tools/README.md")], "contributors", "contributeurs",
     "read-only data probes (`ShopCurrencyProbe`, `ApiProbe`…) and data scripts (`discoveries-merge.py`: shared discoveries; "
     "`faq.py` and `docgen.py`: the generated parts of the documents) · `../scripts/`: `Verify.ps1` (checks), `Release.ps1` (commit and "
     "release), `New-Tool.ps1` (new page), `Update-ToolGallery.ps1` (tool gallery)",
     "sondes de données en lecture seule (`ShopCurrencyProbe`, `ApiProbe`…) et scripts de données (`discoveries-merge.py` : découvertes "
     "partagées ; `faq.py` et `docgen.py` : les parties produites des documents) · `../scripts/` : `Verify.ps1` (contrôles), "
     "`Release.ps1` (commit et release), `New-Tool.ps1` (nouvelle page), `Update-ToolGallery.ps1` (galerie d'outils)"),
]
# docs/project: the reference documents (named in the project/ row), then the working documents (the paragraph below the table).
PROJECT_CORE = [
    ("PROJECT", "scope", "périmètre"),
    ("ROADMAP", "the 17 reference plugins and the user requests R-01 to R-14", "les 17 plugins de référence et les demandes R-01 à R-14"),
    ("ARCHITECTURE", "code map", "carte du code"),
    ("PUBLISHING", "Dalamud rules kept", "règles Dalamud tenues"),
]
PROJECT_WORKING = [
    ("GLOBAL-PLAN", "global plan and current state", "plan global et état actuel"),
    ("TESTING", "in-game test protocols", "protocoles d'essai en jeu"),
    ("TESTING-ROUTE", "the in-game tests not yet ticked, sorted by activity", "les essais en jeu non cochés, rangés par activité"),
    ("SETTINGS", "inventory of the settings: default, where, real effect", "inventaire des réglages : défaut, où, effet réel"),
    ("CONTENT-TOOLS", "content tools study, X-90", "étude des outils de contenus, X-90"),
    ("BACKLOG", None, None),
    ("USER-REQUESTS", "user requests", "demandes de l'utilisateur"),
    ("DECISIONS", None, None),
    ("GAPS", "gaps against other Dalamud plugins and choices made", "manques face aux autres plugins Dalamud et choix retenus"),
    ("REFERENCES", "verified APIs and sources", "API et sources vérifiées"),
    ("PROPOSALS-TRIAGE", "sorting of the proposals", "tri des propositions"),
    ("MAP-PROPOSALS", "map", "carte"),
    ("ITEMS-PROPOSALS", "items, bank, sorting, market", "objets, banque, tri, marché"),
    ("CRAFT-GATHER-PROPOSALS", "crafting, gathering", "artisanat, récolte"),
    ("TOOLS-PROPOSALS", "alarms, free company, quests, gear, mini-games", "alarmes, compagnie libre, quêtes, équipement, mini-jeux"),
    ("G-PROPOSALS", "proposals and provisional choices of step g", "propositions et choix provisoires de l'étape g"),
    ("COMPETITIVE-OFF-PLAN", "Tensho stops in PvP and ranked content, DEC-038", "Tensho se coupe en JcJ et en contenu classé, DEC-038"),
    ("archive/", "older sections moved out of the plan and the decisions", "anciennes sections sorties du plan et des décisions"),
]
WORKING_INTRO = {
    "en": "Working documents of [project/](project/), changed with almost every version and written in French for now (their English "
          "translation is planned just before 1.0.0): ",
    "fr": "Documents de travail de [project/](project/), changés à presque chaque version et écrits en français pour l'instant (leur "
          "traduction anglaise est prévue juste avant la 1.0.0) : ",
}
# Public .md files that are not rows of the index, with the reason.
INDEX_LEFT_OUT = {
    ".github/pull_request_template.md": "the pull request form, opened by GitHub",
    "publish/d17/PULL_REQUEST.md": "linked from the publish/d17 row",
    "publish/github/README.md": "the publish/github row",
    "tools/README.md": "the tools row",
}


def project_link(name):
    return f"project/{name}" if name.endswith("/") else f"project/{name}.md"


def index_block(code):
    en = code == "en"
    head = ("| Folder / file | For whom | Content |" if en else "| Dossier / fichier | Pour qui | Contenu |")
    core = ", ".join(f"[{name}]({project_link(name)}) ({desc_en if en else desc_fr})" for name, desc_en, desc_fr in PROJECT_CORE)
    rows = [head, "|---|---|---|"]
    for links, who_en, who_fr, what_en, what_fr in INDEX:
        cell = " · ".join(f"[{label}]({target or label})" for label, target in links)
        rows.append(f"| {cell} | {who_en if en else who_fr} | {(what_en if en else what_fr).format(core=core)} |")
    working = []
    for name, desc_en, desc_fr in PROJECT_WORKING:
        label = name.rstrip("/") + ("/" if name.endswith("/") else "")
        desc = desc_en if en else desc_fr
        working.append(f"[{label}]({project_link(name)})" + (f" ({desc})" if desc else ""))
    return "\n".join(rows) + "\n\n" + WORKING_INTRO[code] + ", ".join(working) + "."


def git_files(pattern):
    out = subprocess.run(["git", "ls-files", pattern], cwd=ROOT, capture_output=True, text=True, encoding="utf-8")
    return [line for line in out.stdout.splitlines() if line]


def index_problems():
    """Every docs/project document and every public .md is in the index, and every path of the index exists."""
    problems = []
    listed = {name for name, _, _ in PROJECT_CORE + PROJECT_WORKING if not name.endswith("/")}
    for path in sorted((ROOT / "docs/project").glob("*.md")):
        if path.stem not in listed:
            problems.append(f"index: docs/project/{path.name} is not in the index (tools/docgen.py, PROJECT_WORKING)")
    for name in listed | {name for name, _, _ in PROJECT_CORE + PROJECT_WORKING if name.endswith("/")}:
        if not (ROOT / "docs" / project_link(name)).exists():
            problems.append(f"index: docs/{project_link(name)} does not exist")
    rows = set()
    for links, *_ in INDEX:
        for label, target in links:
            path = (ROOT / "docs" / (target or label)).resolve()
            if not path.exists():
                problems.append(f"index: {target or label} does not exist")
            rows.add(path.relative_to(ROOT).as_posix())
    skip = ("src/", "tests/", "docs/project/", "docs/evidence/", "docs/README.md")
    for path in git_files("*.md"):
        if path.startswith(skip) or path in rows or path in INDEX_LEFT_OUT:
            continue
        if path.count("/") and path.rsplit("/", 1)[0] in rows:
            continue
        problems.append(f"index: {path} is not in docs/README.md (tools/docgen.py, INDEX or INDEX_LEFT_OUT)")
    return problems


# ---------------------------------------------------------------------------------------------------------------------------- pages
def language(code):
    return json.loads((LANGUAGES / f"{code}.json").read_text(encoding="utf-8-sig"))


def navigation():
    """(group key or None, page, developer mode only) of every page with a navigation entry, in the order of the navigation."""
    groups = {}
    for line in PAGES.read_text(encoding="utf-8").splitlines():
        found = re.match(r'\s*new\(Page\.(\w+),(null|"(\w+)"),(.*)$', line)
        if not found or found.group(4).startswith("null,"):
            continue  # not a page line, or a page reached from the tabs of its parent
        groups.setdefault(found.group(3), []).append((found.group(1), "Visible:" in found.group(4)))
    if not groups:
        raise SystemExit(f"docgen: no page found in {PAGES.relative_to(ROOT)}")
    return [(group, page, dev) for group, pages in groups.items() for page, dev in pages]


def cell(text):
    return " ".join(text.split()).replace("|", "\\|")


def pages_block(code):
    texts = language(code)
    help_, nav = texts["PageHelp"], texts["Navigation"]
    en = code == "en"
    rows = ["| Group | Page | What it is for |" if en else "| Groupe | Page | À quoi elle sert |", "|---|---|---|"]
    last = object()
    for group, page, dev in navigation():
        title, purpose = help_.get(f"{page}Title", "").strip(), help_.get(f"{page}Purpose", "").strip()
        if not title or not purpose:
            raise SystemExit(f"docgen: {code}: PageHelp.{page}Title or PageHelp.{page}Purpose is missing")
        name = "—" if group is None else nav.get(f"Category{group}", group).title()
        dev_note = (" *(developer mode only)*" if en else " *(mode développeur seulement)*") if dev else ""
        rows.append(f"| {name if group != last else ''} | **{cell(title)}**{dev_note} | {cell(purpose)} |")
        last = group
    return "\n".join(rows)


# ------------------------------------------------------------------------------------------------------------------------- versions
def csproj_version():
    full = re.search(r"<Version>([^<]+)</Version>", CSPROJ.read_text(encoding="utf-8")).group(1)
    return full[:-2] if full.count(".") == 2 and full.endswith(".0") else full


def as_tuple(version):
    return tuple(int(part) for part in re.findall(r"\d+", version))


def keys(node, prefix=""):
    for key, value in node.items():
        if key == "_meta":
            continue
        if isinstance(value, dict):
            yield from keys(value, prefix + key + ".")
        else:
            yield prefix + key


FRENCH_NAMES = {"en": "l'anglais", "fr": "le français", "de": "l'allemand", "ja": "le japonais"}


def joined(items, word):
    return items[0] if len(items) == 1 else ", ".join(items[:-1]) + f" {word} " + items[-1]


def languages_block(code):
    reference = set(keys(language("en")))
    complete, partial = [], []
    for path in sorted(LANGUAGES.glob("*.json")):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        name = data.get("_meta", {}).get("name", path.stem) if code == "en" else FRENCH_NAMES.get(path.stem, path.stem)
        count = len(set(keys(data)) & reference)
        (complete if count == len(reference) else partial).append((name, path.name, count))
    complete.sort(key=lambda row: row[1] != "en.json")
    version = f"<!-- docgen:version -->{csproj_version()}<!-- /docgen:version -->"
    if code == "en":
        text = (f"Current state (counted in the language files, version {version}): "
                + joined([name for name, _, _ in complete], "and") + f" are complete ({len(reference)} keys; French must stay complete)")
        if partial:
            text += "; " + joined([f"{name} (`{file}`) translates {count} keys" for name, file, count in partial], "and")
            text += ", not reviewed by native speakers"
    else:
        text = (f"État actuel (compté dans les fichiers de langue, version {version}) : "
                + joined([name for name, _, _ in complete], "et") + f" sont complets ({len(reference)} clés ; le français doit le rester)")
        if partial:
            text += " ; " + joined([f"{name} (`{file}`) traduit {count} clés" for name, file, count in partial], "et")
            text += ", sans relecture par des locuteurs natifs"
    return text + "."


def version_problems(release):
    """Hand-written versions, marks that disagree, marks newer than the csproj; (errors, notes)."""
    errors, notes, seen = [], [], set()
    for name in NO_HAND_VERSION:
        path = ROOT / name
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8")
        seen.update(VERSION_MARK.findall(text))
        for number, line in enumerate(VERSION_MARK.sub("", text).splitlines(), 1):
            for found in HAND_VERSION.finditer(line):
                errors.append(f"versions: {name}:{number}: « {found.group(0)} » is written by hand: use a version mark or no version")
    now = csproj_version()
    if len(seen) > 1:
        errors.append(f"versions: the version marks disagree ({', '.join(sorted(seen))}): python -I -X utf8 tools/docgen.py write")
    for version in seen:
        if as_tuple(version) > as_tuple(now):
            errors.append(f"versions: a version mark says {version}, newer than the csproj ({now})")
        elif version != now:
            (errors if release else notes).append(f"versions: the documents were produced for {version}, the csproj is at {now}: "
                                                  "python -I -X utf8 tools/docgen.py write before a release")
    return errors, notes


# ---------------------------------------------------------------------------------------------------------------------------- write
BLOCKS = [
    ("docs/README.md", "index-en", lambda: index_block("en")),
    ("docs/README.md", "index-fr", lambda: index_block("fr")),
    ("README.md", "pages-en", lambda: pages_block("en")),
    ("README.md", "pages-fr", lambda: pages_block("fr")),
    ("Localization/README.md", "languages-en", lambda: languages_block("en")),
    ("Localization/README.md", "languages-fr", lambda: languages_block("fr")),
]


def produce(text, key, body):
    start, end = f"<!-- docgen:{key} -->\n", f"\n<!-- /docgen:{key} -->"
    i = text.find(start)
    j = text.find(end, i + len(start)) if i >= 0 else -1
    if i < 0 or j < 0 or text.count(start) != 1:
        raise SystemExit(f"docgen: the marks of {key} are missing or doubled")
    return text[: i + len(start)] + body + text[j:]


def documents():
    """{file: (text on disk with \\n, text it should have)}."""
    out = {}
    for name in dict.fromkeys([name for name, _, _ in BLOCKS] + list(NO_HAND_VERSION)):
        path = ROOT / name
        if not path.exists():
            continue
        now = path.read_text(encoding="utf-8").replace("\r\n", "\n")
        new = now
        for file, key, body in BLOCKS:
            if file == name:
                new = produce(new, key, body())
        new = VERSION_MARK.sub(f"<!-- docgen:version -->{csproj_version()}<!-- /docgen:version -->", new)
        out[name] = (now, new)
    return out


def main(argv):
    if not argv:
        for file, key, body in BLOCKS:
            print(f"## {file} · {key}\n\n{body()}\n")
        return 0
    if argv == ["write"]:
        changed = []
        for name, (now, new) in documents().items():
            if new != now:
                path = ROOT / name
                crlf = b"\r\n" in path.read_bytes()  # keep the line endings of the file
                path.write_bytes((new.replace("\n", "\r\n") if crlf else new).encode("utf-8"))
                changed.append(name)
        print(f"docgen: {len(changed)} document(s) written{': ' + ', '.join(changed) if changed else ''} (version {csproj_version()})")
        errors, _ = version_problems(False)
        for line in index_problems() + errors:
            print("docgen:", line)
        return 1 if index_problems() or errors else 0
    if argv[:1] == ["check"] and set(argv[1:]) <= {"--release"}:
        release = "--release" in argv
        problems = index_problems()
        for name, (now, new) in documents().items():
            # The version marks are compared apart (they follow the csproj only before a release).
            if VERSION_MARK.sub("", now) != VERSION_MARK.sub("", new) or (release and now != new):
                problems.append(f"{name} is out of date: python -I -X utf8 tools/docgen.py write")
        errors, notes = version_problems(release)
        for line in problems + errors:
            print("docgen:", line)
        for line in notes:
            print("docgen: note:", line)
        return 1 if problems or errors else 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
