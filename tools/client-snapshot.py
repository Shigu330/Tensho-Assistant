"""Client map database (X-115): snapshots of the installed client's catalogues, per game version, and their comparison.

No Dalamud needed. Commands (run with `python -I -X utf8 tools/client-snapshot.py …`):
  snapshot [--label 7.58]      reads the installed client (tools/ClientSnapshot: the plugin's own catalogue loaders on Lumina, plus maps,
                               markers and aetherytes) into <root>/<game version>/.
  import-cache <folder> [--label 7.57]
                               takes files of the plugin cache (catalog-* families of DataCache-v1) as the snapshot of the game version
                               written in each file; files already stored are skipped.
  list                         stored snapshots.
  diff <v1> <v2> [--out F] [--fr] [--limit N]
                               markdown report: counts per family, then additions, removals, moves (threshold per family) and
                               changes, names in the client language. v1/v2: a game version, its start, a label, `previous` or `latest`.
  patch [--fr]                 the one command after a game patch: imports the plugin cache files of versions not stored yet, takes the
                               snapshot of the installed version if missing, compares it with the previous one, writes
                               <root>/<version>/diff-from-<old>.md and prints its path and the counts.
  --selftest                   checks import and diff on made-up files (no game needed).
Options: --root DIR (default %LOCALAPPDATA%\\TenshoAssistant\\client-snapshots, or TENSHO_SNAPSHOTS), --sqpack DIR (default: game path of
XIVLauncher's launcherConfigV3.json), --lang French|English|German|Japanese (default: XIVLauncher's game language).
The snapshots are Square Enix data: they stay on this computer, outside the repository, and are never published.
"""
import json
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOOL = ROOT / "tools" / "ClientSnapshot"
APPDATA = Path(os.environ.get("APPDATA", str(Path.home() / "AppData" / "Roaming")))
LOCAL = Path(os.environ.get("LOCALAPPDATA", str(Path.home() / "AppData" / "Local")))
XIVL = APPDATA / "XIVLauncher"
PLUGIN_CACHE = XIVL / "pluginConfigs" / "TenshoAssistant" / "DataCache-v1"
FAMILIES = ["catalog-map-positions", "catalog-npc-vendors", "catalog-gathering", "catalog-items", "catalog-recipes"]
LANGS = {0: "Japanese", 1: "English", 2: "German", 3: "French"}
VER = re.compile(r"(?:^|;)ffxiv=([0-9.]+)")


def default_root():
    return Path(os.environ.get("TENSHO_SNAPSHOTS") or LOCAL / "TenshoAssistant" / "client-snapshots")


def launcher():
    try:
        return json.loads((XIVL / "launcherConfigV3.json").read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def client_language(cfg):
    """The game client's language: XIVLauncher's « Language » (a name, or a number in older files), else English."""
    value = cfg.get("Language", cfg.get("ClientLanguage"))
    if isinstance(value, int):
        return LANGS.get(value, "English")
    return value if value in LANGS.values() else "English"


def game_version(sqpack):
    try:
        return (Path(sqpack).parent / "ffxivgame.ver").read_text(encoding="utf-8").strip()
    except OSError:
        return ""


def head(path, size=4096):
    """Family, scope and version of a cache envelope, read from its first bytes (the files reach 20 MB)."""
    with open(path, "rb") as f:
        text = f.read(size).decode("utf-8", "replace")
    m = re.match(r'\{"Schema":1,"Family":"([^"]+)","Scope":"([^"]*)","Version":"([^"]*)"', text)
    return (m.group(1), m.group(2), m.group(3)) if m else None


def opt(argv, name, default=None):
    if name in argv:
        i = argv.index(name)
        if i + 1 < len(argv):
            return argv[i + 1]
    return default


# ---------- store ----------

def snapshots(root):
    out = []
    if root.is_dir():
        for d in root.iterdir():
            try:
                meta = json.loads((d / "meta.json").read_text(encoding="utf-8"))
            except (OSError, ValueError):
                continue
            out.append((d, meta))
    out.sort(key=lambda x: [int(p) for p in re.findall(r"\d+", x[1].get("game", ""))])
    return out


def write_meta(d, game, label=None, **fields):
    path = d / "meta.json"
    try:
        meta = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        meta = {"game": game}
    if label:
        meta["label"] = label
    for k, v in fields.items():
        if isinstance(v, list):
            meta[k] = sorted(set(meta.get(k, [])) | set(v))
        elif v is not None:
            meta[k] = v
    d.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(meta, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")


def import_cache(folder, root, label=None, quiet=False):
    """Copies catalog-* envelopes into <root>/<game version>/cache. Returns {version: [families added]}."""
    added = {}
    for f in sorted(Path(folder).glob("*.json")):
        h = head(f)
        if not h or h[0] not in FAMILIES:
            continue
        m = VER.search(h[2])
        if not m:
            continue
        version = m.group(1)
        dest = root / version / "cache"
        if (dest / f.name).exists():
            continue
        present = {head(g)[0] for g in dest.glob("*.json") if head(g)} if dest.is_dir() else set()
        if h[0] in present:  # one file per family and version: a snapshot already holds it
            continue
        dest.mkdir(parents=True, exist_ok=True)
        shutil.copy2(f, dest / f.name)
        added.setdefault(version, []).append(h[0])
        write_meta(root / version, version, label, sources=["import-cache"], language=h[1], families=[h[0]])
    if not quiet:
        for v, fams in added.items():
            print(f"imported {v}: {', '.join(sorted(fams))}")
        if not added:
            print("nothing new to import")
    return added


def build_tool():
    dotnet = os.environ.get("DOTNET") or "dotnet"
    local = Path.home() / ".codex" / "dotnet-10.0.403" / "dotnet.exe"
    if not os.environ.get("DOTNET") and local.exists():
        dotnet = str(local)
    r = subprocess.run([dotnet, "build", str(TOOL), "-c", "Release", "-nologo", "-v", "q"], capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print((r.stdout + r.stderr)[-3000:])
        raise SystemExit("FAIL ClientSnapshot did not build")
    return dotnet, TOOL / "bin" / "Release" / "net10.0-windows" / "ClientSnapshot.dll"


def snapshot(root, sqpack, lang, label=None):
    version = game_version(sqpack)
    if not version:
        raise SystemExit(f"FAIL no ffxivgame.ver next to {sqpack}")
    dotnet, dll = build_tool()
    dest = root / version
    tmp = root / (version + ".tmp")
    shutil.rmtree(tmp, ignore_errors=True)
    r = subprocess.run([dotnet, str(dll), str(sqpack), str(tmp), f"--lang={lang}"], text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0 or game_version(sqpack) != version:
        shutil.rmtree(tmp, ignore_errors=True)
        raise SystemExit("FAIL snapshot not taken (see the lines above)")
    if (dest / "cache").is_dir():  # a snapshot replaces imported files of the same version
        shutil.rmtree(dest / "cache")
    dest.mkdir(parents=True, exist_ok=True)
    for item in tmp.iterdir():
        shutil.move(str(item), str(dest / item.name))
    tmp.rmdir()
    write_meta(dest, version, label, sources=["snapshot"], language=lang, families=FAMILIES + ["maps"])
    print(f"snapshot {version} in {dest}")
    return version


def resolve(root, name):
    snaps = snapshots(root)
    if not snaps:
        raise SystemExit("FAIL no snapshot stored")
    if name == "latest":
        return snaps[-1]
    if name == "previous":
        if len(snaps) < 2:
            raise SystemExit("FAIL only one snapshot stored")
        return snaps[-2]
    for d, meta in snaps:
        if name in (meta.get("label"), meta.get("game")) or meta.get("game", "").startswith(name):
            return d, meta
    raise SystemExit(f"FAIL no snapshot named {name}")


def load(d):
    """{family: value} of one snapshot; 'maps' from client-maps.json."""
    out = {}
    for f in (d / "cache").glob("*.json") if (d / "cache").is_dir() else []:
        h = head(f)
        if h and h[0] in FAMILIES:
            out[h[0]] = json.loads(f.read_text(encoding="utf-8"))["Value"]
    if (d / "client-maps.json").exists():
        out["maps"] = json.loads((d / "client-maps.json").read_text(encoding="utf-8"))
    return out


# ---------- diff ----------

KINDS = ["QuestIssuer", "GatheringPoint", "FishingSpot", "Sightseeing", "AetherCurrent"]


def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1]) if a and b else 0.0


def specs(names):
    """(family, list, title, key, label, position, threshold, compared fields)."""
    item = lambda i: names.get(i) or f"#{i}"
    first_loc = lambda v: (v["Locations"][0]["MapId"], v["Locations"][0]["X"], v["Locations"][0]["Y"]) if v.get("Locations") else None
    return [
        ("catalog-map-positions", "Positions", "map positions", lambda r: (r["Kind"], r["Id"], r["MapId"], r["Data"]),
         lambda r: f"{KINDS[r['Kind']] if r['Kind'] < len(KINDS) else r['Kind']} {r['Id']} {r['Name']}".strip() + f" (map {r['MapId']})",
         lambda r: (r["MapId"], r["X"], r["Z"]), 1.0, ["Radius", "Name", "MinTime", "MaxTime", "Extra"]),
        ("catalog-npc-vendors", "Vendors", "NPC vendors", lambda r: r["NpcId"],
         lambda r: f"{r['Name']} ({r['NpcId']}" + (f", {r['Locations'][0]['Zone']}" if r.get("Locations") else "") + ")",
         first_loc, 0.3, ["Name", "ShopIds"]),
        ("catalog-npc-vendors", "Sales", "shop sales", lambda r: (r["ShopId"], r["ItemId"], r["Quantity"], r["Hq"], r["Kind"]),
         lambda r: f"{item(r['ItemId'])} — shop {r['ShopId']}" + (f" {r['ShopName']}" if r["ShopName"] else ""), None, 0,
         ["Costs", "RequiredRank", "QuestRequired", "AchievementRequired"]),
        ("catalog-gathering", "Sources", "gathering sources", lambda r: (r["ItemId"], r["PointId"]),
         lambda r: f"{item(r['ItemId'])} — {r['TypeName']} {r['Level']}, point {r['PointId']}" + (f", {r['Zone']}" if r["Zone"] else ""),
         None, 0, ["Level", "TerritoryId", "MapId", "Zone", "Place", "IsHidden", "Schedule"]),
        ("catalog-gathering", "Fishing", "fish", lambda r: r["FishId"], lambda r: f"{item(r['ItemId'])} (fish {r['FishId']})", None, 0,
         ["ItemId", "NotebookSpotId", "Hidden", "TimeRestricted", "WeatherRestricted"]),
        ("catalog-items", "Items", "items", lambda r: r["Id"], lambda r: f"{r['Name']} ({r['Id']})", None, 0,
         ["Name", "ItemLevel", "EquipLevel", "IsMarketable", "UiCategoryId", "SellPrice"]),
        ("catalog-recipes", "Rows", "recipes", lambda r: r["RecipeId"],
         lambda r: f"{item(r['ResultItemId'])} — {r['CraftType']} {r['Level']} (recipe {r['RecipeId']})", None, 0,
         ["ResultItemId", "ResultAmount", "Level", "Ingredients"]),
        ("maps", "Maps", "maps", lambda r: r["Id"], lambda r: f"{r['Name']}" + (f" / {r['Sub']}" if r["Sub"] else "") + f" ({r['Id']}, {r['Key']})",
         None, 0, ["Territory", "SizeFactor", "OffsetX", "OffsetY", "Name", "Sub"]),
        ("maps", "Markers", "map markers", lambda r: (r["Range"], r["Index"]),
         lambda r: (r["Label"].replace("\r\n", " / ").replace("\n", " / ") or f"icon {r['Icon']}") + f" (range {r['Range']})",
         lambda r: (r["Range"], r["X"], r["Y"]), 4.0, ["Icon", "Label", "DataType", "DataKey"]),
        ("maps", "Aetherytes", "aetherytes", lambda r: r["Id"], lambda r: f"{r['Name'] or r['Aethernet']} ({r['Id']})", None, 0,
         ["Name", "Aethernet", "Territory", "Map", "IsAetheryte"]),
    ]


def keyed(rows, key):
    """{key: row}; rows sharing a key (same item twice at one point, one sale at two prices) get a rank after a stable sort."""
    groups = {}
    for r in rows:
        groups.setdefault(key(r), []).append(r)
    out = {}
    for k, group in groups.items():
        if len(group) == 1:
            out[k] = group[0]
        else:
            for i, r in enumerate(sorted(group, key=lambda r: json.dumps(r, sort_keys=True, ensure_ascii=False))):
                out[(k, i)] = r
    return out


def compare(old, new, spec):
    fam, lst, title, key, label, pos, thr, fields = spec
    a, b = keyed(old.get(fam, {}).get(lst, []), key), keyed(new.get(fam, {}).get(lst, []), key)
    res = {"title": title, "family": fam, "before": len(a), "after": len(b), "present": fam in old and fam in new,
           "added": [label(b[k]) for k in b if k not in a], "removed": [label(a[k]) for k in a if k not in b], "moved": [], "changed": []}
    for k in a.keys() & b.keys():
        x, y = a[k], b[k]
        if pos:
            p, q = pos(x), pos(y)
            if (p is None) != (q is None) or (p and (p[0] != q[0] or dist(p[1:], q[1:]) > thr)):
                res["moved"].append(f"{label(y)}: {fmt(p)} → {fmt(q)}")
        diff = [f for f in fields if x.get(f) != y.get(f)]
        if diff:
            res["changed"].append(f"{label(y)}: " + ", ".join(f"{f} {short(x.get(f))} → {short(y.get(f))}" for f in diff))
    for part in ("added", "removed", "moved", "changed"):
        res[part].sort()
    return res


def fmt(p):
    return "none" if p is None else f"{p[0]}@{p[1]:.1f},{p[2]:.1f}"


def short(v):
    s = json.dumps(v, ensure_ascii=False) if not isinstance(v, str) else v
    return s if len(s) <= 60 else s[:57] + "…"


LABELS = {
    "en": {"title": "Client data comparison", "family": "Family", "before": "Before", "after": "After", "added": "Added",
           "removed": "Removed", "moved": "Moved", "changed": "Changed", "missing": "not in both snapshots", "more": "… and {n} more",
           "intro": "Read by the plugin's own catalogue loaders (tools/ClientSnapshot) or copied from its cache; names in the client language ({lang}).",
           "thresholds": "Move thresholds: map positions 1 unit, vendors 0.3 map coordinate, markers 4 texture pixels; a change of map counts as a move."},
    "fr": {"title": "Comparaison des données du client", "family": "Famille", "before": "Avant", "after": "Après", "added": "Ajouts",
           "removed": "Retraits", "moved": "Déplacements", "changed": "Changements", "missing": "absent d'un des deux instantanés",
           "more": "… et {n} de plus",
           "intro": "Lu par les chargeurs de catalogues du plugin (tools/ClientSnapshot) ou copié de son cache ; noms dans la langue du client ({lang}).",
           "thresholds": "Seuils de déplacement : positions 1 unité, marchands 0,3 coordonnée de carte, marqueurs 4 pixels de texture ; un changement de carte compte comme un déplacement."},
}


def diff(root, v1, v2, lang_ui="en", limit=40):
    (d1, m1), (d2, m2) = resolve(root, v1), resolve(root, v2)
    old, new = load(d1), load(d2)
    names = {i["Id"]: i["Name"] for i in old.get("catalog-items", {}).get("Items", [])}
    names.update({i["Id"]: i["Name"] for i in new.get("catalog-items", {}).get("Items", [])})
    results = [compare(old, new, s) for s in specs(names)]
    return report(results, m1, m2, LABELS[lang_ui], limit), results


def tag(meta):
    return meta["game"] + (f" ({meta['label']})" if meta.get("label") else "")


def report(results, m1, m2, L, limit):
    out = [f"# {L['title']} : {tag(m1)} → {tag(m2)}", "", L["intro"].format(lang=m2.get("language", "?")), L["thresholds"], "",
           f"| {L['family']} | {L['before']} | {L['after']} | {L['added']} | {L['removed']} | {L['moved']} | {L['changed']} |",
           "|---|---|---|---|---|---|---|"]
    for r in results:
        if not r["present"]:
            out.append(f"| {r['title']} | {r['before'] or '—'} | {r['after'] or '—'} | {L['missing']} | | | |")
        else:
            out.append(f"| {r['title']} | {r['before']} | {r['after']} | {len(r['added'])} | {len(r['removed'])} | {len(r['moved'])} | {len(r['changed'])} |")
    for r in results:
        if not r["present"] or not any(r[p] for p in ("added", "removed", "moved", "changed")):
            continue
        out += ["", f"## {r['title']}"]
        for part in ("added", "removed", "moved", "changed"):
            if r[part]:
                out += ["", f"### {L[part]} ({len(r[part])})", *[f"- {x}" for x in r[part][:limit]]]
                if len(r[part]) > limit:
                    out.append(f"- {L['more'].format(n=len(r[part]) - limit)}")
    return "\n".join(out) + "\n"


def counts_line(results):
    return "; ".join(f"{r['title']} +{len(r['added'])} -{len(r['removed'])} ~{len(r['moved'])} Δ{len(r['changed'])}"
                     for r in results if r["present"])


def patch(root, sqpack, lang, lang_ui):
    if PLUGIN_CACHE.is_dir():
        import_cache(PLUGIN_CACHE, root)
    version = game_version(sqpack)
    snaps = {m["game"]: (d, m) for d, m in snapshots(root)}
    if version not in snaps or "snapshot" not in snaps[version][1].get("sources", []):
        snapshot(root, sqpack, lang)
    games = [m["game"] for _, m in snapshots(root)]
    older = [g for g in games if g != version and games.index(g) < games.index(version)]
    if not older:
        print(f"only {version} is stored: nothing to compare yet")
        return 0, None, None
    text, results = diff(root, older[-1], version, lang_ui)
    out = root / version / f"diff-from-{older[-1]}.md"
    out.write_text(text, encoding="utf-8")
    print(f"report: {out}")
    print(counts_line(results))
    return 0, out, results


# ---------- selftest ----------

def selftest():
    ok, total = 0, 0

    def check(cond, what):
        nonlocal ok, total
        total += 1
        ok += bool(cond)
        if not cond:
            print("FAIL", what)

    def env(fam, ver, value):
        return {"Schema": 1, "Family": fam, "Scope": "French", "Version": f"ex1=1;ffxiv={ver};schema=7|copy=3", "RecordedAt": "2026-01-01T00:00:00+00:00", "Value": value}

    with tempfile.TemporaryDirectory() as t:
        t = Path(t)
        cache, root = t / "cache", t / "store"
        cache.mkdir()
        items_a = {"Items": [{"Id": 1, "Name": "Gil", "ItemLevel": 1}, {"Id": 2, "Name": "Ancien", "ItemLevel": 1}]}
        items_b = {"Items": [{"Id": 1, "Name": "Gil", "ItemLevel": 1}, {"Id": 3, "Name": "Nouveau", "ItemLevel": 5}]}
        pos_a = {"Positions": [{"Kind": 0, "Id": 9, "MapId": 2, "Data": 0, "X": 0.0, "Z": 0.0, "Radius": 1, "Name": "Q", "MinTime": 0, "MaxTime": 0, "Extra": ""}]}
        pos_b = {"Positions": [{"Kind": 0, "Id": 9, "MapId": 2, "Data": 0, "X": 3.0, "Z": 4.0, "Radius": 1, "Name": "Q", "MinTime": 0, "MaxTime": 0, "Extra": ""}]}
        for name, e in (("a.json", env("catalog-items", "2026.01.01.0000.0000", items_a)), ("b.json", env("catalog-map-positions", "2026.01.01.0000.0000", pos_a)),
                        ("c.json", env("catalog-items", "2026.02.01.0000.0000", items_b)), ("d.json", env("catalog-map-positions", "2026.02.01.0000.0000", pos_b)),
                        ("e.json", {"Schema": 1, "Family": "quests", "Scope": "French", "Version": "ffxiv=2026.03.01.0000.0000", "Value": {}})):
            (cache / name).write_text(json.dumps(e, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
        added = import_cache(cache, root, quiet=True)
        check(sorted(added) == ["2026.01.01.0000.0000", "2026.02.01.0000.0000"], "import groups by game version, skips other families")
        check(import_cache(cache, root, quiet=True) == {}, "second import adds nothing")
        check(head(cache / "a.json") == ("catalog-items", "French", "ex1=1;ffxiv=2026.01.01.0000.0000;schema=7|copy=3"), "head reads the envelope")
        check(resolve(root, "latest")[1]["game"] == "2026.02.01.0000.0000" and resolve(root, "2026.01")[1]["game"] == "2026.01.01.0000.0000", "resolve")
        text, results = diff(root, "previous", "latest", "fr")
        items = next(r for r in results if r["title"] == "items")
        check(items["added"] == ["Nouveau (3)"] and items["removed"] == ["Ancien (2)"], "items added and removed")
        pos = next(r for r in results if r["title"] == "map positions")
        check(len(pos["moved"]) == 1 and "2@0.0,0.0 → 2@3.0,4.0" in pos["moved"][0], "move over the threshold")
        check("| maps | — | — |" in text and "### Ajouts (1)" in text, "report: missing family and French labels")
        check(counts_line(results).startswith("map positions +0 -0 ~1"), "counts line")
    print(f"client-snapshot selftest: {ok}/{total} ok")
    return 0 if ok == total else 1


def main(argv):
    if not argv or argv[0] in ("-h", "--help"):
        print(__doc__)
        return 2
    if argv[0] == "--selftest":
        return selftest()
    root = Path(opt(argv, "--root") or default_root())
    cfg = launcher()
    sqpack = Path(opt(argv, "--sqpack") or Path(cfg.get("GamePath") or "") / "game" / "sqpack")
    lang = opt(argv, "--lang") or client_language(cfg)
    ui = "fr" if "--fr" in argv else "en"
    cmd = argv[0]
    if cmd == "snapshot":
        snapshot(root, sqpack, lang, opt(argv, "--label"))
        return 0
    if cmd == "import-cache" and len(argv) > 1:
        import_cache(Path(argv[1]), root, opt(argv, "--label"))
        return 0
    if cmd == "list":
        for d, m in snapshots(root):
            fams = len(m.get("families", []))
            print(f"{tag(m)}  {m.get('language', '?')}  {', '.join(m.get('sources', []))}  {fams} families  {d}")
        return 0
    if cmd == "diff" and len(argv) > 2:
        text, results = diff(root, argv[1], argv[2], ui, int(opt(argv, "--limit", "40")))
        out = opt(argv, "--out")
        if out:
            Path(out).write_text(text, encoding="utf-8")
            print(f"report: {out}")
            print(counts_line(results))
        else:
            print(text)
        return 0
    if cmd == "patch":
        return patch(root, sqpack, lang, ui)[0]
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
