"""Writes the parts of the public documents that the code or the files already know (system plan 4.14, documents lot 1).

Three generators, each written between its marks and nowhere else:
  index     docs/README.md, the map of the documents (English and French): one list below, and every docs/project/*.md file
            and every public .md file must be in it, so a new document cannot be forgotten
  pages     README.md, the table of the pages (English and French), from the page list src/TenshoAssistant/MainWindow.Pages.cs
            (order, group, developer mode) and the help texts PageHelp.<Page>Title and PageHelp.<Page>Purpose of en.json and fr.json
  versions  the version the documents were produced for (inline marks), from the <Version> of the csproj, and the state of the
            translations in src/TenshoAssistant/Localization/README.md, counted in the language files; a version number written by hand in a sentence
            such as « checked on …, version 0.971 » is refused
  settings  docs/project/SETTINGS.md (plan 4.16): every settings control of the code (BoolSetting, IntSetting, SliderSetting,
            ComboSetting, the map's SettingBox), the Configuration property it writes with its type and default, where it is and its
            label and tooltip; then the settings of « restore every setting » that no control writes; one block per language
            (settings in the English half, settings-fr in the French half)
  network   SECURITY.md (English and French): the sources the plugin contacts, from the list NETWORK checked against the code
            (every file that makes a request is in a row, every host it names is a host of a row) and the setting that turns each off
  decisions docs/project/DECISIONS.md: the index, one row per DEC with its date and problem, in each half (decisions-en reads the
            English entries « ID: DEC-NNN », decisions the French entries « DEC-ID: DEC-NNN »; both halves must list the same DECs)
  evidence  docs/evidence/README.md: one row per evidence file with the tracked files that cite it
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
  python -I -X utf8 tools/docgen.py write --blocks   write the blocks only, the version marks keep their version (each version)
  python -I -X utf8 tools/docgen.py check    exit 1 if a block is out of date, a version is written by hand, or the marks disagree;
                                             a version mark older than the csproj is reported (exit 0): produce them before a release
  python -I -X utf8 tools/docgen.py check --release   the same, and a version mark older than the csproj is an error too
"""
import functools
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
HAND_VERSION = re.compile(r"\bversion\s+0\.\d{3,4}\b|\b(?:checked|vérifié|relu|relue|relues|state at|état au)\b[^.\n]{0,60}"
                          r"\b0\.\d{3,4}\b", re.I)
# Public documents scanned for versions written by hand (the CHANGELOG, the evidence and the French working documents are history).
NO_HAND_VERSION = ("README.md", "CONTRIBUTING.md", "SECURITY.md", "src/TenshoAssistant/Localization/README.md", "docs/README.md", "docs/FAQ.md",
                   "tools/README.md", "community-help/README.md",
                   "community-templates/README.md", "community-discoveries/README.md", ".github/pull_request_template.md",
                   # 0.1084 (audit A060): the living documents of docs/project carry a date, never a version stamp.
                   "docs/project/ARCHITECTURE.md", "docs/project/ROADMAP.md", "docs/project/PROJECT.md",
                   "docs/project/PUBLISHING.md", "docs/project/REFERENCES.md", "docs/project/G-PROPOSALS.md")

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
    ([("../CODE_OF_CONDUCT.md", None)], "everyone", "tout le monde",
     "how we behave in the project's issues, discussions and pull requests, and how to report a problem (English, then French)",
     "comment se comporter dans les tickets, discussions et demandes de fusion du projet, et comment signaler un problème (anglais, puis français)"),
    ([("../CHANGELOG.md", None), ("changelog/", None)], "everyone", "tout le monde",
     "what changed in each version (French, then `### English` from 0.219.0 on; older entries are in French only); the last release and the versions after it, older ones in `changelog/` (`tools/changelog.py archive`)",
     "ce qui a changé à chaque version (français, puis `### English` depuis 0.219.0 ; les entrées plus anciennes sont en français seulement) ; la dernière release et les versions qui suivent, les plus anciennes dans `changelog/` (`tools/changelog.py archive`)"),
    ([("FAQ.md", None)], "everyone", "tout le monde",
     f"frequent questions, English then French, written by `tools/faq.py` from the plugin's list (the Help page shows the same); "
     f"new questions in [Discussions › Q&A]({QA})",
     f"questions fréquentes, en anglais puis en français, écrites par `tools/faq.py` depuis la liste du plugin (la page Aide montre les "
     f"mêmes) ; nouvelles questions dans [Discussions › Q&A]({QA})"),
    ([("project/", None)], "contributors and agent", "contributeurs et agent",
     "{core}, and the working documents listed below", "{core}, et les documents de travail listés plus bas"),
    ([("evidence/", None)], "contributors and agent", "contributeurs et agent",
     "probe outputs and log excerpts cited by the code (evidence of the data links), most named `<version>-<subject>`; "
     "[index](evidence/README.md) of the files that cite each",
     "sorties de sondes et extraits de journaux cités par le code (preuves des liens de données), le plus souvent nommés "
     "`<version>-<sujet>` ; [index](evidence/README.md) des fichiers qui citent chacune"),
    ([("../src/TenshoAssistant/Localization/README.md", None)], "translators", "traducteurs",
     "adding or completing a language", "ajouter ou compléter une langue"),
    ([("../community-help/README.md", None), ("../community-templates/README.md", None), ("../community-discoveries/README.md", None)],
     "community", "communauté",
     "videos and guides of the Help · gallery of « My tools » · shared discoveries (exchange through GitHub, file format)",
     "vidéos et guides de l'aide · galerie de « Mes outils » · découvertes partagées (échange par GitHub, format du fichier)"),
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
    ("SETTINGS", "inventory of the settings, produced from the code: default, where, label and tooltip",
     "inventaire des réglages, produit depuis le code : défaut, où, libellé et info-bulle"),
    ("CONTENT-TOOLS", "content tools study, X-90", "étude des outils de contenus, X-90"),
    ("DECISIONS", None, None),
    ("IDS", "registry of the identifier families (X-, DEC-, frozen ones, collisions)", "registre des familles d'identifiants (X-, DEC-, familles figées, collisions)"),
    ("GAPS", "gaps against other Dalamud plugins and choices made", "manques face aux autres plugins Dalamud et choix retenus"),
    ("REFERENCES", "verified APIs and sources", "API et sources vérifiées"),
    ("PROPOSALS-TRIAGE", "sorting of the proposals", "tri des propositions"),
    ("MAP-PROPOSALS", "map", "carte"),
    ("ITEMS-PROPOSALS", "items, bank, sorting, market", "objets, banque, tri, marché"),
    ("CRAFT-GATHER-PROPOSALS", "crafting, gathering", "artisanat, récolte"),
    ("TOOLS-PROPOSALS", "alarms, free company, quests, gear, mini-games", "alarmes, compagnie libre, quêtes, équipement, mini-jeux"),
    ("G-PROPOSALS", "proposals and provisional choices of step g", "propositions et choix provisoires de l'étape g"),
    ("audits/", "audits and plans of the work (X-76 cleanup, network rules, probe coverage, community map)",
     "audits et plans du travail (nettoyage X-76, règles réseau, couverture des sondes, carte communautaire)"),
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
    "tools/README.md": "the tools row",
    "tools/PluginUiTest/README.md": "the pinned copy of the plugin UI test library (source: Shigu330/PluginUiTest), linked from tools/README.md (the tools row)",
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
    # A tracked file deleted in the working tree (not yet committed) is no longer a document to index (0.1052).
    return [line for line in out.stdout.splitlines() if line and (ROOT / line).exists()]


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


# ------------------------------------------------------------------------------------------------------------------------- settings
SOURCE = ROOT / "src/TenshoAssistant"
CONFIGURATION = SOURCE / "Configuration.cs"
SETTINGS_INDEX = SOURCE / "SettingsIndex.cs"
CONTROL = re.compile(r'\b(BoolSetting|IntSetting|SliderSetting|ComboSetting)(?:<[^>()]*>)?\(\s*"([\w.]+)"\s*,\s*"([\w.]+)"')
SETTING_BOX = re.compile(r'\bSettingBox\(\s*"([\w.]+)"\s*,\s*([\w.]+)\s*,')
PROPERTY = re.compile(r"public\s+(?:required\s+)?([\w.<>?,\[\] ]+?)\s+(\w+)\s*\{\s*get\s*(;\s*(?:set|init);\s*\}(?:\s*=\s*([^;\n]+);)?|=>[^{}]*\})")
# A property with get=>/set=> accessors reads a backing field (« ref field » or « =>field »), whose initializer is the default.
BACKING = re.compile(r"(?:\bref\s+|=>\s*)([a-z_]\w*)\b")
CLASS = re.compile(r"^[ \t]*(?:(?:public|internal|private|protected|sealed|static|partial|abstract|readonly)\s+)*(?:class|record|struct)\s+(\w+)",
                   re.M)


def source_files():
    return sorted(path for path in SOURCE.rglob("*.cs") if "obj" not in path.parts and "bin" not in path.parts)


def read_source(path):
    return path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")


def text_of(texts, key):
    """A text of a language file: a flat key at the root (« Home.… ») or Section.Key."""
    if key in texts and isinstance(texts[key], str):
        return texts[key]
    section, _, rest = key.partition(".")
    value = texts.get(section, {}).get(rest) if isinstance(texts.get(section), dict) else None
    return value if isinstance(value, str) else None


def call_body(text, start):
    """The arguments of the call opened just before start (parentheses balanced, strings skipped)."""
    depth, i, quote = 1, start, None
    while i < len(text) and depth:
        ch = text[i]
        if quote:
            if ch == "\\":
                i += 1
            elif ch == quote:
                quote = None
        elif ch in "\"'":
            quote = ch
        elif ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
        i += 1
    return text[start:i - 1]


# Local names of a part of Configuration in the files that draw its controls: (file, name) → property of Configuration.
ALIASES = {("MainWindow.MapView.cs", "settings"): "MapView", ("MainWindow.MapView.cs", "contextSettings"): "MapView",
           ("MainWindow.Settings.cs", "search"): "CraftSearch", ("MainWindow.FashionWeek.cs", "options"): "FashionOptions",
           ("MainWindow.GearWear.cs", "WearSettings"): "GearWear"}


def setting_path(expression, file):
    """configuration.A.B → A.B; a local name of ALIASES → its property; anything else: None."""
    expression = expression.strip().lstrip("!").replace("?.", ".")
    if expression.startswith("configuration."):
        return expression[len("configuration."):]
    head, _, rest = expression.partition(".")
    return f"{ALIASES[(file, head)]}.{rest}" if (file, head) in ALIASES and rest else None


@functools.cache
def controls():
    """Every settings control of the code: (path or None, label key, tip key or None, file name)."""
    out = []
    for path in source_files():
        text = read_source(path)
        for found in CONTROL.finditer(text):
            body = call_body(text, text.index("(", found.start()) + 1)
            body = re.sub(r"\((configuration\.[\w.]+)\s*\?\?=\s*new\(\)\)", r"\1", body)  # (configuration.X??=new()).Y=value
            write =re.search(r"\b(\w+)\s*=>\s*\{?\s*([\w.?]+)\s*=\s*\1\b", body)
            out.append((setting_path(write.group(2), path.name) if write else None, found.group(2), found.group(3), path.name))
        for found in SETTING_BOX.finditer(text):
            out.append((setting_path(found.group(2), path.name), found.group(1), found.group(1) + "Help", path.name))
    return out


@functools.cache
def declarations():
    """{class name: {property: (type, initializer or None)}} for every class of the plugin."""
    classes = {}
    for path in source_files():
        text = read_source(path)
        heads = [(found.start(), found.group(1)) for found in CLASS.finditer(text)]
        for found in PROPERTY.finditer(text):
            owner = None
            for at, name in heads:
                if at < found.start():
                    owner = name
            if not owner:
                continue
            initial = (found.group(4) or "").strip() or None
            if found.group(3).startswith("=>"):
                # « private bool a=true,b; »: the declarator of the field, and its initializer if any.
                field = BACKING.search(found.group(3))
                line = field and re.search(rf"^\s*private\s+(?:volatile\s+|static\s+)*[\w.<>?\[\] ]+?\s+([^;]*\b{field.group(1)}\b[^;]*);", text, re.M)
                declared = line and re.search(rf"\b{field.group(1)}\s*(?:=\s*([^,]+))?(?:,|$)", line.group(1))
                initial = (declared.group(1) or "").strip() or None if declared else None
            classes.setdefault(owner, {}).setdefault(found.group(2), (found.group(1).strip(), initial))
    return classes


def resolve(classes, path):
    """(type, default text) of a path from Configuration, or None when a part is not a property."""
    owner, found = "Configuration", None
    for part in path.split("."):
        found = classes.get(owner, {}).get(part)
        if not found:
            return None
        owner = found[0].rstrip("?").split(".")[-1]
    kind, initial = found
    if initial is None:
        initial = "null" if kind.endswith("?") or kind in ("string",) else "false" if kind == "bool" else \
            "0" if kind in ("int", "uint", "long", "float", "double", "byte", "ushort") else "(empty)"
    elif initial in ("new()", "[]") or initial.startswith("new "):
        initial = "(empty)" if initial in ("[]",) or "List<" in initial or "Dictionary<" in initial or "HashSet<" in initial else "(object)"
    return kind.split(".")[-1] if "<" not in kind else kind, initial.replace("TenshoAssistant.", "")


def reset_names():
    text = read_source(CONFIGURATION)
    block = re.search(r"Reset\s*=\s*new HashSet<string>\s*\{(.*?)\};", text, re.S).group(1)
    return re.findall(r'"(\w+)"', re.sub(r"//[^\n]*", "", block))


def settings_tabs():
    return dict(re.findall(r'\("([\w.]+)","(\w+)"\)', read_source(SETTINGS_INDEX)))


def settings_rows():
    """[(path or label key, kind, default, places, en text, fr text, has path)] sorted by path."""
    classes, tabs = declarations(), settings_tabs()
    en, fr = language("en"), language("fr")
    rows = {}
    for path, label, tip, file in controls():
        key = path or f"({label})"
        tab = tabs.get(label)
        page = file[len("MainWindow."):-3] if file.startswith("MainWindow.") else file
        place = (tab, page, file)
        row = rows.setdefault(key, {"path": path, "places": [], "label": label, "tip": tip, "settings": False})
        if place not in row["places"]:
            row["places"].append(place)
        if tab and not row["settings"]:  # the texts of the Settings page win over a page's own copy
            row.update(label=label, tip=tip, settings=True)
    out = []

    def where(lang, code):
        def one(place):
            tab, page, file = place
            if tab:
                return (f"Settings › {tab}" if code == "en" else
                        f"{(text_of(lang, 'PageHelp.SettingsTitle') or 'Paramètres').strip()} › "
                        f"{(text_of(lang, 'SettingsSections.' + tab) or tab).strip()}")
            title = text_of(lang, f"PageHelp.{page}Title")
            return f"page {title.strip()}" if title else f"`{file}`"
        return one
    for key in sorted(rows, key=str.lower):
        row = rows[key]
        kind, default = (resolve(classes, row["path"]) or ("?", "?")) if row["path"] else ("—", "—")

        def texts(lang):
            label = text_of(lang, row["label"]) or row["label"]
            tip = text_of(lang, row["tip"]) if row["tip"] else None
            return f"**{cell(label)}**" + (f" — {cell(tip)}" if tip else "")
        places = {"en": [where(en, "en")(p) for p in row["places"]], "fr": [where(fr, "fr")(p) for p in row["places"]]}
        out.append((key, kind, default, places, texts(en), texts(fr), bool(row["path"])))
    return out


SETTINGS_WORDS = {
    "en": ("| Setting | Type | Default | Where | Label and tooltip |",
           "Settings reset by « Restore every setting » without a control in the interface ({n}): state of the interface, choices "
           "written by a page or migration marks.",
           "| Setting | Type | Default |",
           "Counted: {rows} controls ({linked} linked to `Configuration`, {other} to another file), {hidden} settings without a control."),
    "fr": ("| Réglage | Type | Défaut | Où | Libellé et info-bulle |",
           "Réglages remis par « Tout remettre par défaut » sans case dans l'interface ({n}) : état de l'interface, choix "
           "écrits par une page ou marques de migration.",
           "| Réglage | Type | Défaut |",
           "Compté : {rows} cases ({linked} reliées à `Configuration`, {other} à un autre fichier), {hidden} réglages sans case."),
}


def settings_block(code):
    """The settings inventory in one language (English block « settings », French block « settings-fr »)."""
    rows = settings_rows()
    covered = {row[0].split(".")[0] for row in rows if row[6]}
    classes = declarations()
    head, hidden_line, hidden_head, counted_line = SETTINGS_WORDS[code]
    lines = [head, "|---|---|---|---|---|"]
    for key, kind, default, places, en, fr, _ in rows:
        lines.append(f"| `{key}` | {cell(kind)} | {cell(default)} | {cell(' · '.join(places[code]))} | {en if code == 'en' else fr} |")
    hidden = [name for name in sorted(reset_names(), key=str.lower) if name not in covered]
    lines += ["", hidden_line.format(n=len(hidden)), "", hidden_head, "|---|---|---|"]
    for name in hidden:
        kind, default = resolve(classes, name) or ("?", "?")
        lines.append(f"| `{name}` | {cell(kind)} | {cell(default)} |")
    counted = sum(1 for row in rows if row[6])
    lines += ["", counted_line.format(rows=len(rows), linked=counted, other=len(rows) - counted, hidden=len(hidden))]
    return "\n".join(lines)


def settings_problems():
    problems = []
    classes = declarations()
    for key, kind, default, places, en, fr, has_path in settings_rows():
        if has_path and kind == "?":
            problems.append(f"settings: {key} is written by a control but is not a property path of Configuration")
    for name in reset_names():
        if name not in classes.get("Configuration", {}):
            problems.append(f"settings: {name} of ConfigurationReset.Reset is not a property of Configuration")
    return problems


# -------------------------------------------------------------------------------------------------------------------------- network
# One row per source the plugin contacts: (name, hosts, setting that turns it off or None, code files, what en, what fr).
# The hosts and files are checked against the code: every file that makes a request is in a row (or NETWORK_LEFT_OUT), every host it
# names is a host of a row (or NOT_CONTACTED), and every host of a row is named by one of its files.
NETWORK = [
    ("Universalis", ["universalis.app"], "CommunityUniversalis",
     ["Market/MarketService.cs", "Market/MarketBoardService.cs", "Market/RetainerSales.cs"],
     "prices, listings and history, by world and item id; with the wishlist sale alert (off by default), one request every 30 minutes at "
     "most for up to 20 wishes that have a price",
     "prix, annonces et historique, par monde et numéro d'objet ; avec l'alerte de vente des souhaits (désactivée par défaut), une requête "
     "toutes les 30 minutes au plus pour 20 souhaits au plus qui ont un prix"),
    ("Garland Tools", ["www.garlandtools.org"], "CommunityGarland", ["Community/GarlandClient.cs", "Duties/DutyLootService.cs"],
     "item sources, duty and monster loot", "sources d'objets, butin des contenus et des monstres"),
    ("FFXIV Console Games Wiki", ["ffxiv.consolegameswiki.com"], "CommunityWiki",
     ["Community/WikiItemAcquisition.cs", "Duties/WikiDutyGuide.cs", "Maps/WikiVistas.cs", "Monsters/BeastWikiIndex.cs",
      "Monsters/BlueSpellWikiIndex.cs", "Monsters/WikiMonsterSources.cs", "Island/IslandAnimals.Dalamud.cs", "Island/IslandAnimals.cs"],
     "item acquisition, monster drops, duty guides, sightseeing log, blue magic spells, Beastmaster and island pasture pages",
     "acquisition des objets, butin des monstres, guides des contenus, carnet d'exploration, sorts de mage bleu, pages du dresseur et du "
     "pâturage de l'île"),
    ("Teamcraft", ["gubal.ffxivteamcraft.com"], "CommunityTeamcraftBites", ["Community/FishBiteTimes.cs"],
     "public reports: one GraphQL POST query carrying the fishing spot id",
     "rapports publics : une requête GraphQL POST portant le numéro du lieu de pêche"),
    ("LuminaSupplemental", ["raw.githubusercontent.com"], "CommunityLumina", ["Community/LuminaSupplemental.cs"],
     "community data files (CSV) hosted on GitHub", "fichiers de données de la communauté (CSV) hébergés sur GitHub"),
    ("GatherBuddy", ["raw.githubusercontent.com", "api.github.com"], "CommunityGatherBuddy", ["Community/GatherBuddyFish.cs"],
     "fish data (baits, mooches, windows) hosted on GitHub", "données des poissons (appâts, remises à l'eau, fenêtres) hébergées sur GitHub"),
    ("Fashion Report (FFXIVGachaSpreadsheet)", ["raw.githubusercontent.com"], "CommunityFashion", ["Fashion/FashionDataService.cs"],
     "the week's Fashion Report data hosted on GitHub", "les données de la Revue de mode de la semaine, hébergées sur GitHub"),
    ("PaissaDB", ["paissadb.zhu.codes"], "CommunityPaissa", ["Community/PaissaDb.cs"],
     "open housing plots, by world id, on a click or with its optional watch",
     "terrains libres, par numéro de monde, sur un clic ou avec sa surveillance facultative"),
    ("This repository", ["raw.githubusercontent.com"], None, ["MyTools/ToolGallery.cs", "Help/CommunityHelp.cs"],
     "community tool gallery and help videos, on a click, data only, never code",
     "galerie d'outils de la communauté et vidéos d'aide, sur un clic, des données seulement, jamais de code"),
    ("Shared discoveries exchange", ["raw.githubusercontent.com", "api.github.com", "github.com", "gist.github.com"], "Discovery.AutoExchange",
     ["Discoveries/GitHubExchange.cs", "Discoveries/CommunityDiscoveries.cs"],
     "the common file of this repository; after « Connect GitHub » only, the player's own sightings to a secret gist and one issue "
     "(described below); on « Exchange now » or with « Automatic exchange »",
     "le fichier commun de ce dépôt ; seulement après « Connecter GitHub », les observations du joueur vers un gist secret et un ticket "
     "(décrit plus bas) ; sur « Échanger maintenant » ou avec « Échange automatique »"),
    ("Discord webhook", ["discord.com", "discordapp.com", "ptb.discord.com", "canary.discord.com"], "Discord.Enabled",
     ["Alarms/DiscordNotifier.cs", "Alarms/DiscordWebhook.cs"],
     "HTTPS POST of alarms and alerts to the player's own webhook (described below)",
     "envoi HTTPS POST des alarmes et alertes vers le webhook du joueur (décrit plus bas)"),
    # X-121: the desktop application (not the plugin): its own files are named « TenshoAssistant.Desktop/… », its switch is a Desktop.* text.
    ("Desktop app update", ["api.github.com", "github.com"], "Desktop.UpdateAutoOn",
     ["TenshoAssistant.Desktop/DesktopUpdater.cs", "TenshoAssistant.Desktop/DesktopUpdate.cs"],
     "desktop app only: at its start and on « Check for update », this repository's release of the installed plugin's version (else the "
     "latest Stable one), then TenshoAssistant.Desktop.zip and its SHA-256 from that release; GET only, no player data (limit described below)",
     "application de bureau seulement : à son démarrage et sur « Chercher une mise à jour », la release de ce dépôt qui a la version du plugin "
     "installé (sinon la dernière Stable), puis TenshoAssistant.Desktop.zip et son SHA-256 de cette release ; GET seulement, aucune donnée du "
     "joueur (limite décrite plus bas)"),
]
# X-121: the desktop application's sources, checked as the plugin's (a file of it that makes a request is in a row).
DESKTOP_SOURCE = ROOT / "src/TenshoAssistant.Desktop"


def network_path(file):
    """A file of a NETWORK row: « TenshoAssistant.Desktop/… » under src/, else under src/TenshoAssistant."""
    return ROOT / "src" / file if file.startswith("TenshoAssistant.Desktop/") else SOURCE / file


def network_files():
    """(row name of the file, path) of every source file that may make a request: the plugin's and the desktop application's."""
    plugin = [(path.relative_to(SOURCE).as_posix(), path) for path in source_files()]
    desktop = [("TenshoAssistant.Desktop/" + path.relative_to(DESKTOP_SOURCE).as_posix(), path)
               for path in sorted(DESKTOP_SOURCE.rglob("*.cs")) if "obj" not in path.parts and "bin" not in path.parts]
    return plugin + desktop
# Files that hold an HTTP client or a request without contacting a source of their own, with the reason.
NETWORK_LEFT_OUT = {
    "Community/BoundedHttp.cs": "the bounded reader every request goes through (timeout, size limit)",
    "Credits/CreditCatalog.cs": "the credits page: names and links only",
    "Diagnostics/PageSweepWindow.Dalamud.cs": "the developer page sweep: draws the pages, whose own requests are listed here",
}
# Hosts named by request files that are never contacted (links shown or sent as text).
NOT_CONTACTED = {"creativecommons.org": "the CC0 licence link in the exchange's texts",
                 "www.youtube.com": "help videos, opened in the player's browser on a click",
                 "youtube.com": "help videos, opened in the player's browser on a click",
                 "youtu.be": "help videos, opened in the player's browser on a click"}
REQUEST = re.compile(r"\bHttpClient\b|\bBoundedHttp\b|\bHttpRequestMessage\b")
HOST = re.compile(r'https?://([a-z0-9.-]+\.[a-z]{2,})|"([a-z0-9-]+(?:\.[a-z0-9-]+)*\.(?:com|org|net|app|codes|gg|io|co|be|wiki|tv))"')


def network_block(code):
    en = code == "en"
    texts = language(code)
    labels = {path: label for path, label, _, _ in controls() if path}
    rows = ["| Source | Hosts | Off with | Code |" if en else "| Source | Hôtes | Coupée par | Code |", "|---|---|---|---|"]
    for name, hosts, setting, files, what_en, what_fr in NETWORK:
        if setting:
            label = text_of(texts, setting if setting.startswith("Desktop.") else labels.get(setting, "")) or setting
            switch = f"« {cell(label)} »"
        else:
            switch = "— (on a click)" if en else "— (sur un clic)"
        code_cell = ", ".join(f"`{file}`" for file in files)
        rows.append(f"| **{name}**: {what_en if en else what_fr} | {', '.join(f'`{host}`' for host in hosts)} | {switch} | {code_cell} |"
                    .replace("**:", "**:" if en else "** :"))
    return "\n".join(rows)


def network_problems():
    problems, listed, hosts = [], {}, set()
    for name, row_hosts, setting, files, *_ in NETWORK:
        hosts.update(row_hosts)
        named = set()
        for file in files:
            path = network_path(file)
            if not path.exists():
                problems.append(f"network: {name}: {path.relative_to(ROOT).as_posix()} does not exist")
                continue
            listed[file] = name
            named.update(group for found in HOST.finditer(read_source(path)) for group in found.groups() if group)
        for host in row_hosts:
            if host not in named:
                problems.append(f"network: {name}: no file of the row names {host}")
        if setting and setting.startswith("Desktop."):
            if not all(text_of(language(code), setting) for code in ("en", "fr")):
                problems.append(f"network: {name}: {setting} is not a text of en.json and fr.json")
        elif setting and setting not in {path for path, *_ in controls()}:
            problems.append(f"network: {name}: {setting} is not a setting of a control")
    for file, path in network_files():
        text = read_source(path)
        shown = path.relative_to(ROOT).as_posix()
        if not REQUEST.search(text) or file in NETWORK_LEFT_OUT:
            continue
        if file not in listed:
            problems.append(f"network: {shown} makes requests but is in no row (tools/docgen.py, NETWORK)")
        for found in HOST.finditer(text):
            host = found.group(1) or found.group(2)
            if host not in hosts and host not in NOT_CONTACTED:
                problems.append(f"network: {shown} names {host}, in no row (tools/docgen.py, NETWORK or NOT_CONTACTED)")
    return problems


# ------------------------------------------------------------------------------------------------------------------------ decisions
DECISIONS = ROOT / "docs/project/DECISIONS.md"


def decisions():
    """[(DEC-NNN, date, problem)] in the order of the file."""
    text = DECISIONS.read_text(encoding="utf-8").replace("\r\n", "\n")
    out = []
    for block in re.finditer(r"^DEC-ID: (DEC-\d+)\n(.*?)(?=^DEC-ID: |\Z)", text, re.M | re.S):
        date = re.search(r"^Timestamp: (\d{4}-\d{2}-\d{2})", block.group(2), re.M)
        problem = re.search(r"^Problem: (.+)$", block.group(2), re.M)
        out.append((block.group(1), date.group(1) if date else "?", problem.group(1).strip() if problem else "?"))
    return out


def decisions_en():
    """[(DEC-NNN, date, problem)] of the English half (entries « ID: DEC-NNN », before the French half)."""
    text = DECISIONS.read_text(encoding="utf-8").replace("\r\n", "\n")
    out = []
    for block in re.finditer(r"^ID: (DEC-\d+)\n(.*?)(?=^ID: |^---$|^DEC-ID: |\Z)", text, re.M | re.S):
        date = re.search(r"^Timestamp: (\d{4}-\d{2}-\d{2})", block.group(2), re.M)
        problem = re.search(r"^Problem: (.+)$", block.group(2), re.M)
        out.append((block.group(1), date.group(1) if date else "?", problem.group(1).strip() if problem else "?"))
    return out


def decisions_block(code="fr"):
    """The index of the DECs: « decisions » in the French half, « decisions-en » in the English half."""
    rows = ["| DEC | Date | Problème |" if code == "fr" else "| DEC | Date | Problem |", "|---|---|---|"]
    rows += [f"| {dec} | {date} | {cell(problem)} |" for dec, date, problem in (decisions() if code == "fr" else decisions_en())]
    return "\n".join(rows)


def decisions_problems():
    seen, problems = set(), []
    for dec, date, problem in decisions():
        if dec in seen:
            problems.append(f"decisions: {dec} is written twice in docs/project/DECISIONS.md")
        seen.add(dec)
        if "?" in (date, problem):
            problems.append(f"decisions: {dec} has no Timestamp or no Problem line")
    french, english = [row[:2] for row in decisions()], [row[:2] for row in decisions_en()]
    if english != french:  # a translated document changes in both halves (phase 7)
        missing = [dec for dec, _ in french if dec not in {d for d, _ in english}]
        problems.append("decisions: the English half of docs/project/DECISIONS.md (« ID: DEC-NNN » entries) does not have the same DECs, "
                        "dates and order as the French half" + (f"; missing: {', '.join(missing[:5])}" if missing else ""))
    problems += [f"decisions: {dec} has no Timestamp or no Problem line in the English half" for dec, date, problem in decisions_en()
                 if "?" in (date, problem)]
    return problems


# ------------------------------------------------------------------------------------------------------------------------- evidence
EVIDENCE = ROOT / "docs/evidence"
CODE_ROOTS = ("src/", "tests/", "tools/", "scripts/", ".github/")


def evidence_files():
    return sorted(path.name for path in EVIDENCE.iterdir() if path.is_file() and path.name != "README.md")


@functools.cache
def evidence_citations():
    """{evidence file: (code files, documents)} from the tracked files that name it (another evidence file counts as a document;
    the evidence index and the file itself do not, 0.1085)."""
    names = evidence_files()
    cited = {name: ([], []) for name in names}
    for path in git_files("*"):
        if path == "docs/evidence/README.md" or not path.endswith((".cs", ".py", ".ps1", ".md", ".json", ".yml", ".csproj", ".txt")):
            continue
        try:
            text = (ROOT / path).read_text(encoding="utf-8-sig")
        except (OSError, UnicodeDecodeError):
            continue
        for name in names:
            if name in text and path != "docs/evidence/" + name:
                cited[name][0 if path.startswith(CODE_ROOTS) else 1].append(path)
    return cited


def evidence_block():
    cited = evidence_citations()
    rows = ["| Evidence · Preuve | Code and tests · Code et tests | Documents |", "|---|---|---|"]
    for name, (code, docs) in cited.items():
        rows.append(f"| [{name}]({name}) | {', '.join(f'`{path}`' for path in code) or '—'} | {', '.join(f'`{path}`' for path in docs) or '—'} |")
    by_code = sum(1 for code, _ in cited.values() if code)
    nowhere = sum(1 for code, docs in cited.values() if not code and not docs)
    return ("\n".join(rows) + f"\n\nCounted · compté : {len(cited)} files · fichiers ; {by_code} cited by the code or the tests · citées par le "
            f"code ou les tests ; {nowhere} cited nowhere · citées nulle part.")


# ---------------------------------------------------------------------------------------------------------------------------- write
BLOCKS = [
    ("docs/README.md", "index-en", lambda: index_block("en")),
    ("docs/README.md", "index-fr", lambda: index_block("fr")),
    ("README.md", "pages-en", lambda: pages_block("en")),
    ("README.md", "pages-fr", lambda: pages_block("fr")),
    ("src/TenshoAssistant/Localization/README.md", "languages-en", lambda: languages_block("en")),
    ("src/TenshoAssistant/Localization/README.md", "languages-fr", lambda: languages_block("fr")),
    ("docs/project/SETTINGS.md", "settings", lambda: settings_block("en")),
    ("docs/project/SETTINGS.md", "settings-fr", lambda: settings_block("fr")),
    ("SECURITY.md", "network-en", lambda: network_block("en")),
    ("SECURITY.md", "network-fr", lambda: network_block("fr")),
    ("docs/project/DECISIONS.md", "decisions-en", lambda: decisions_block("en")),
    ("docs/project/DECISIONS.md", "decisions", decisions_block),
    ("docs/evidence/README.md", "evidence", evidence_block),
]


def problems_of_sources():
    return index_problems() + settings_problems() + network_problems() + decisions_problems()


def produce(text, key, body):
    start, end = f"<!-- docgen:{key} -->\n", f"\n<!-- /docgen:{key} -->"
    i = text.find(start)
    j = text.find(end, i + len(start)) if i >= 0 else -1
    if i < 0 or j < 0 or text.count(start) != 1:
        raise SystemExit(f"docgen: the marks of {key} are missing or doubled")
    return text[: i + len(start)] + body + text[j:]


def documents(marks=True):
    """{file: (text on disk with \\n, text it should have)}; marks=False leaves the version marks as they are."""
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
        kept = VERSION_MARK.findall(now)
        version = csproj_version() if marks or not kept else kept[0]  # a block's own mark keeps the file's version too
        new = VERSION_MARK.sub(f"<!-- docgen:version -->{version}<!-- /docgen:version -->", new)
        out[name] = (now, new)
    return out


def main(argv):
    if not argv:
        for file, key, body in BLOCKS:
            print(f"## {file} · {key}\n\n{body()}\n")
        return 0
    if argv[:1] == ["write"] and set(argv[1:]) <= {"--blocks"}:
        changed = []
        for name, (now, new) in documents("--blocks" not in argv).items():
            if new != now:
                path = ROOT / name
                crlf = b"\r\n" in path.read_bytes()  # keep the line endings of the file
                path.write_bytes((new.replace("\n", "\r\n") if crlf else new).encode("utf-8"))
                changed.append(name)
        print(f"docgen: {len(changed)} document(s) written{': ' + ', '.join(changed) if changed else ''} (version {csproj_version()})")
        errors, _ = version_problems(False)
        problems = problems_of_sources()
        for line in problems + errors:
            print("docgen:", line)
        return 1 if problems or errors else 0
    if argv[:1] == ["check"] and set(argv[1:]) <= {"--release"}:
        release = "--release" in argv
        problems = problems_of_sources()
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
