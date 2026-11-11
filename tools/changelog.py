"""CHANGELOG archive (0.1093, audit A070): CHANGELOG.md keeps the last released version and every version after it; older entries
live in docs/changelog/CHANGELOG-0.<a>-0.<b>.md, one file per hundred of the minor number, newest first.

Usage (from the repository root):
  python -I -X utf8 tools/changelog.py archive   move the entries older than the released version (repo.json) to docs/changelog/
  python -I -X utf8 tools/changelog.py check     CHANGELOG.md still holds the released version and every later one; no entry twice
The release script needs every version since the last release in CHANGELOG.md (its notes cover them); Verify.ps1 -Release runs check.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MAIN = ROOT / "CHANGELOG.md"
FOLDER = ROOT / "docs" / "changelog"
HEADING = re.compile(r"^## (\d+)\.(\d+)\.(\d+)\s*$", re.M)
# Links of the moved entries were relative to the repository root; from docs/changelog/ they need « ../../ ».
LINK = re.compile(r"\]\((?!https?:|mailto:|#|\.\./\.\./)([^)]+)\)")


def read(path):
    raw = path.read_text(encoding="utf-8")
    return raw.replace("\r\n", "\n"), "\r\n" in raw


def write(path, text, crlf):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes((text.replace("\n", "\r\n") if crlf else text).encode("utf-8"))


def released():
    """The version on the custom repository (repo.json, x.y.z.w), as (x, y, z)."""
    data = json.loads((ROOT / "repo.json").read_text(encoding="utf-8-sig"))
    entry = data[0] if isinstance(data, list) else data
    return tuple(int(n) for n in entry["AssemblyVersion"].split(".")[:3])


def split(text):
    """(head before the first entry, [(version, entry text)]) in file order."""
    found = list(HEADING.finditer(text))
    if not found:
        return text, []
    head = text[:found[0].start()]
    entries = []
    for i, m in enumerate(found):
        end = found[i + 1].start() if i + 1 < len(found) else len(text)
        entries.append(((int(m.group(1)), int(m.group(2)), int(m.group(3))), text[m.start():end].rstrip("\n") + "\n"))
    return head, entries


def bucket(version):
    low = version[1] // 100 * 100
    return FOLDER / f"CHANGELOG-0.{low}-0.{low + 99}.md"


def archive_head(path):
    low, high = re.findall(r"0\.(\d+)", path.name)
    return (f"# Journal des versions / Changelog, 0.{low} à 0.{high}\n\n"
            "Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par "
            "`tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine "
            "réécrits depuis ce dossier.\n\n")


def archive():
    text, crlf = read(MAIN)
    head, entries = split(text)
    cut = released()
    keep = [(v, e) for v, e in entries if v >= cut]
    move = [(v, e) for v, e in entries if v < cut]
    if not move:
        print("changelog: nothing to move")
        return 0
    groups = {}
    for v, e in move:
        groups.setdefault(bucket(v), []).append((v, LINK.sub(r"](../../\1)", e)))
    for path, new in sorted(groups.items()):
        old = split(read(path)[0])[1] if path.exists() else []
        known = {v for v, _ in old}
        merged = sorted(old + [(v, e) for v, e in new if v not in known], key=lambda x: x[0], reverse=True)
        write(path, archive_head(path) + "\n".join(e for _, e in merged), crlf)
    write(MAIN, head + "\n".join(e for _, e in keep), crlf)
    print(f"changelog: {len(move)} entries moved to {len(groups)} archive file(s); CHANGELOG.md keeps {len(keep)} (from "
          f"{'.'.join(map(str, cut))} on)")
    return 0


def check():
    problems = []
    _, entries = split(read(MAIN)[0])
    cut = released()
    versions = [v for v, _ in entries]
    if not versions or min(versions) > cut:
        problems.append(f"CHANGELOG.md must keep the released version {'.'.join(map(str, cut))} and every later one")
    seen = set(versions)
    for path in sorted(FOLDER.glob("CHANGELOG-*.md")) if FOLDER.exists() else []:
        for v, _ in split(read(path)[0])[1]:
            if v in seen:
                problems.append(f"{'.'.join(map(str, v))} is twice (CHANGELOG.md or another archive, and {path.name})")
            if v >= cut:
                problems.append(f"{path.name} holds {'.'.join(map(str, v))}, not older than the released version")
            if bucket(v).name != path.name:
                problems.append(f"{'.'.join(map(str, v))} is in {path.name}, expected {bucket(v).name}")
            seen.add(v)
    for p in problems:
        print("FAIL changelog:", p)
    print("changelog: ok" if not problems else f"changelog: {len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    if command == "archive":
        sys.exit(archive())
    if command == "check":
        sys.exit(check())
    print(__doc__)
    sys.exit(2)
