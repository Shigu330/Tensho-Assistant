"""Client map database (X-115): snapshots of the installed client's catalogues, per game version, and their comparison.

No Dalamud needed. Commands (run with `python -I -X utf8 tools/client-snapshot.py …`):
  snapshot [--label 7.58]      reads the installed client (tools/ClientSnapshot: the plugin's own catalogue loaders on Lumina, plus maps,
                               markers and aetherytes) into <root>/<game version>/.
  import-cache <folder> [--label 7.57]
                               takes files of the plugin cache (catalog-* families of DataCache-v1) as the snapshot of the game version
                               written in each file; files already stored are skipped.
  list                         stored snapshots.
  diff <v1> <v2> [--out F] [--fr] [--limit N]
                               markdown report: counts per family (X-116: with the layout placements, client-placements.json: NPCs,
                               objects, FATE and CE ranges, aetherytes, coffers, exits), then additions, removals, moves (threshold per family) and
                               changes, names in the client language. v1/v2: a game version, its start, a label, `previous` or `latest`.
  patch [--fr]                 the one command after a game patch: imports the plugin cache files of versions not stored yet, takes the
                               snapshot of the installed version if missing, compares it with the previous one, writes
                               <root>/<version>/diff-from-<old>.md and changes-from-<old>.json (every changed place, for
                               discoveries-merge.py recheck), prints their paths and the counts.
  check [facts.json …] [--snapshot V]
                               X-116: the facts' places (community base and this PC's Discoveries.json by default) against the client's
                               placements: NPC places (19) and FATEs (3) against the layout files, fishing (10, 23, 29, 30) against the
                               fishing spots, spearfishing (28) against its points; never monsters (the client does not place them).
                               SUSPECT past a threshold per family (THRESHOLDS); read-only.
  discover <v1> <v2>           X-116: what the patch added with no place in the client and no fact known (French lines).
  --selftest                   checks import, diff, check and discover on made-up files (no game needed).
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
    write_meta(dest, version, label, sources=["snapshot"], language=lang, families=FAMILIES + ["maps", "placements"])
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


PLACEMENT_FIELDS = {"Family": "", "Bg": 0, "File": "", "Instance": 0, "Base": 0, "X": 0.0, "Y": 0.0, "Z": 0.0, "Festival": 0}


def load(d):
    """{family: value} of one snapshot; 'maps' from client-maps.json; 'placements' (X-116) from client-placements.json, split per placement
    family, each row with its layout path in Bg (the file keeps an index and leaves default values out)."""
    out = {}
    for f in (d / "cache").glob("*.json") if (d / "cache").is_dir() else []:
        h = head(f)
        if h and h[0] in FAMILIES:
            out[h[0]] = json.loads(f.read_text(encoding="utf-8"))["Value"]
    if (d / "client-maps.json").exists():
        out["maps"] = json.loads((d / "client-maps.json").read_text(encoding="utf-8"))
    if (d / "client-placements.json").exists():
        raw = json.loads((d / "client-placements.json").read_text(encoding="utf-8"))
        bgs = raw.get("Bgs", [])
        split = {fam: [] for fam, _, _ in PLACEMENTS}
        for r in raw.get("Placements", []):
            row = {**PLACEMENT_FIELDS, **r}
            row["Bg"] = bgs[row["Bg"]]
            split.setdefault(row["Family"], []).append(row)
        split["_territories"] = raw.get("Territories", [])
        split["_names"] = raw.get("Names", {})
        out["placements"] = split
    return out


# ---------- diff ----------

KINDS = ["QuestIssuer", "GatheringPoint", "FishingSpot", "Sightseeing", "AetherCurrent"]
# X-116: placement families of client-placements.json, their titles and move thresholds in yalms (world units). A layout row that moves less is
# float noise or a nudge nobody sees; FATE and CE ranges are wide areas whose centre matters at a few yalms (docs/evidence/x116-client-placements.md).
PLACEMENTS = [("enpc", "NPC placements", 1.0), ("eobj", "object placements", 1.0), ("fate", "FATE ranges", 5.0), ("ce", "CE ranges", 5.0),
              ("aetheryte", "aetheryte placements", 1.0), ("treasure", "coffers", 1.0), ("exit", "zone exits", 2.0)]


def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1]) if a and b else 0.0


def context(*snaps):
    """Names and places both snapshots share (the later one wins): item names, placement names, layout → territories and zone name,
    map → territory and scale, marker range → map, gathering point → territory."""
    ctx = {"items": {}, "names": {}, "bg_zone": {}, "bg_territories": {}, "maps": {}, "range_map": {}, "point_territory": {}}
    for snap in snaps:
        ctx["items"].update({i["Id"]: i["Name"] for i in snap.get("catalog-items", {}).get("Items", [])})
        pl = snap.get("placements", {})
        for fam, names in pl.get("_names", {}).items():
            ctx["names"].setdefault(fam, {}).update(names)
        for t in pl.get("_territories", []):
            ctx["bg_territories"].setdefault(t["Bg"], set()).add(t["Id"])
            if t.get("Name"):
                ctx["bg_zone"].setdefault(t["Bg"], t["Name"])
        for m in snap.get("maps", {}).get("Maps", []):
            ctx["maps"][m["Id"]] = m
            if m.get("MarkerRange"):
                ctx["range_map"].setdefault(m["MarkerRange"], m["Id"])
        for s in snap.get("catalog-gathering", {}).get("Sources", []):
            if s.get("TerritoryId"):
                ctx["point_territory"][s["BaseId"]] = s["TerritoryId"]
    return ctx


def world_of_pixel(m, px, py):
    """Map texture pixel → world X/Z (the inverse of DiscoveryCheckProbe's Pixel: (x + offset) × scale + 1024)."""
    scale = max(1, m.get("SizeFactor") or 100) / 100
    return (px - 1024) / scale - m.get("OffsetX", 0), (py - 1024) / scale - m.get("OffsetY", 0)


def specs(ctx):
    """(family, list, title, key, label, position, threshold, compared fields, where, subject). where(row): [(territory, x, z)] for the report of
    changed places (x, z None: the whole zone); subject(row): (family, id) a fact can be about. X-116."""
    if not isinstance(ctx, dict) or "items" not in ctx:  # X-115 callers passed the item names alone
        ctx = {**context(), "items": ctx or {}}
    names = ctx["items"]
    item = lambda i: names.get(i) or f"#{i}"
    first_loc = lambda v: (v["Locations"][0]["MapId"], v["Locations"][0]["X"], v["Locations"][0]["Y"]) if v.get("Locations") else None
    map_territory = lambda m: ctx["maps"].get(m, {}).get("Territory", 0)

    def pos_where(r):
        territory = ctx["point_territory"].get(r["Id"], 0) if r["Kind"] == 1 else map_territory(r["MapId"])
        return [(territory, r["X"], r["Z"])] if territory else []

    def pos_subject(r):
        return {1: "gathering", 2: "fishing"}.get(r["Kind"], "position"), r["Id"]

    def marker_where(r):
        m = ctx["maps"].get(ctx["range_map"].get(r["Range"], -1))
        if not m:
            return []
        x, z = world_of_pixel(m, r["X"], r["Y"])
        return [(m["Territory"], x, z)]

    def placement_spec(fam, title, thr):
        zone = lambda bg: ctx["bg_zone"].get(bg, bg.rsplit("/", 1)[-1])
        label = lambda r: (f"{ctx['names'].get(fam, {}).get(str(r['Base'])) or fam} ({r['Base']}) — {zone(r['Bg'])}"
                           + (f", festival {r['Festival']}" if r["Festival"] else ""))
        return ("placements", fam, title, lambda r: (r["Bg"], r["Instance"], r["Base"]), label, lambda r: (r["Bg"], r["X"], r["Z"]), thr,
                ["Festival", "File"], lambda r: [(t, r["X"], r["Z"]) for t in sorted(ctx["bg_territories"].get(r["Bg"], ()))],
                lambda r: (fam, r["Base"]))

    return [
        ("catalog-map-positions", "Positions", "map positions", lambda r: (r["Kind"], r["Id"], r["MapId"], r["Data"]),
         lambda r: f"{KINDS[r['Kind']] if r['Kind'] < len(KINDS) else r['Kind']} {r['Id']} {r['Name']}".strip() + f" (map {r['MapId']})",
         lambda r: (r["MapId"], r["X"], r["Z"]), 1.0, ["Radius", "Name", "MinTime", "MaxTime", "Extra"], pos_where, pos_subject),
        ("catalog-npc-vendors", "Vendors", "NPC vendors", lambda r: r["NpcId"],
         lambda r: f"{r['Name']} ({r['NpcId']}" + (f", {r['Locations'][0]['Zone']}" if r.get("Locations") else "") + ")",
         first_loc, 0.3, ["Name", "ShopIds"],
         lambda r: [(l["TerritoryId"], l.get("WorldX", 0.0), l.get("WorldZ", 0.0)) for l in r.get("Locations", []) if l.get("TerritoryId")],
         lambda r: ("enpc", r["NpcId"])),
        ("catalog-npc-vendors", "Sales", "shop sales", lambda r: (r["ShopId"], r["ItemId"], r["Quantity"], r["Hq"], r["Kind"]),
         lambda r: f"{item(r['ItemId'])} — shop {r['ShopId']}" + (f" {r['ShopName']}" if r["ShopName"] else ""), None, 0,
         ["Costs", "RequiredRank", "QuestRequired", "AchievementRequired"], None, lambda r: ("item", r["ItemId"])),
        ("catalog-gathering", "Sources", "gathering sources", lambda r: (r["ItemId"], r["PointId"]),
         lambda r: f"{item(r['ItemId'])} — {r['TypeName']} {r['Level']}, point {r['PointId']}" + (f", {r['Zone']}" if r["Zone"] else ""),
         None, 0, ["Level", "TerritoryId", "MapId", "Zone", "Place", "IsHidden", "Schedule"],
         lambda r: [(r["TerritoryId"], None, None)] if r.get("TerritoryId") else [], lambda r: ("item", r["ItemId"])),
        ("catalog-gathering", "Fishing", "fish", lambda r: r["FishId"], lambda r: f"{item(r['ItemId'])} (fish {r['FishId']})", None, 0,
         ["ItemId", "NotebookSpotId", "Hidden", "TimeRestricted", "WeatherRestricted"], None, lambda r: ("item", r["ItemId"])),
        ("catalog-items", "Items", "items", lambda r: r["Id"], lambda r: f"{r['Name']} ({r['Id']})", None, 0,
         ["Name", "ItemLevel", "EquipLevel", "IsMarketable", "UiCategoryId", "SellPrice"], None, lambda r: ("item", r["Id"])),
        ("catalog-recipes", "Rows", "recipes", lambda r: r["RecipeId"],
         lambda r: f"{item(r['ResultItemId'])} — {r['CraftType']} {r['Level']} (recipe {r['RecipeId']})", None, 0,
         ["ResultItemId", "ResultAmount", "Level", "Ingredients"], None, lambda r: ("item", r["ResultItemId"])),
        ("maps", "Maps", "maps", lambda r: r["Id"], lambda r: f"{r['Name']}" + (f" / {r['Sub']}" if r["Sub"] else "") + f" ({r['Id']}, {r['Key']})",
         None, 0, ["Territory", "SizeFactor", "OffsetX", "OffsetY", "Name", "Sub"], lambda r: [(r["Territory"], None, None)], None),
        ("maps", "Markers", "map markers", lambda r: (r["Range"], r["Index"]),
         lambda r: (r["Label"].replace("\r\n", " / ").replace("\n", " / ") or f"icon {r['Icon']}") + f" (range {r['Range']})",
         lambda r: (r["Range"], r["X"], r["Y"]), 4.0, ["Icon", "Label", "DataType", "DataKey"], marker_where, None),
        ("maps", "Aetherytes", "aetherytes", lambda r: r["Id"], lambda r: f"{r['Name'] or r['Aethernet']} ({r['Id']})", None, 0,
         ["Name", "Aethernet", "Territory", "Map", "IsAetheryte"], None, None),
        *[placement_spec(fam, title, thr) for fam, title, thr in PLACEMENTS],
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
    fam, lst, title, key, label, pos, thr, fields = spec[:8]
    where, subject = (spec[8], spec[9]) if len(spec) > 9 else (None, None)
    a, b = keyed(old.get(fam, {}).get(lst, []), key), keyed(new.get(fam, {}).get(lst, []), key)
    res = {"title": title, "family": fam, "list": lst, "before": len(a), "after": len(b), "present": lst in old.get(fam, {}) and lst in new.get(fam, {}),
           "added": [label(b[k]) for k in b if k not in a], "removed": [label(a[k]) for k in a if k not in b], "moved": [], "changed": [],
           "places": [], "new_rows": [b[k] for k in b if k not in a]}

    def note(part, row):
        """X-116: the places a change touches, for « to recheck » (discoveries-merge.py recheck)."""
        if not res["present"]:
            return
        for territory, x, z in (where(row) if where else []):
            res["places"].append({"title": title, "part": part, "territory": territory, "x": x, "z": z,
                                  "subject": list(subject(row)) if subject else None, "label": label(row)})

    for k in b:
        if k not in a:
            note("added", b[k])
    for k in a:
        if k not in b:
            note("removed", a[k])
    for k in a.keys() & b.keys():
        x, y = a[k], b[k]
        if pos:
            p, q = pos(x), pos(y)
            if (p is None) != (q is None) or (p and (p[0] != q[0] or dist(p[1:], q[1:]) > thr)):
                res["moved"].append(f"{label(y)}: {fmt(p)} → {fmt(q)}")
                note("moved", x)
                note("moved", y)
        diff = [f for f in fields if x.get(f) != y.get(f)]
        if diff:
            res["changed"].append(f"{label(y)}: " + ", ".join(f"{f} {short(x.get(f))} → {short(y.get(f))}" for f in diff))
            note("changed", y)
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
           "thresholds": "Move thresholds: map positions 1 unit, vendors 0.3 map coordinate, markers 4 texture pixels; placements of the layout files (X-116): NPCs, objects, aetherytes and coffers 1 yalm, zone exits 2, FATE and CE ranges 5; a change of map or layout counts as a move."},
    "fr": {"title": "Comparaison des données du client", "family": "Famille", "before": "Avant", "after": "Après", "added": "Ajouts",
           "removed": "Retraits", "moved": "Déplacements", "changed": "Changements", "missing": "absent d'un des deux instantanés",
           "more": "… et {n} de plus",
           "intro": "Lu par les chargeurs de catalogues du plugin (tools/ClientSnapshot) ou copié de son cache ; noms dans la langue du client ({lang}).",
           "thresholds": "Seuils de déplacement : positions 1 unité, marchands 0,3 coordonnée de carte, marqueurs 4 pixels de texture ; placements des calques (X-116) : PNJ, objets, éthérites et coffres 1 yalm, sorties de zone 2, zones des ALÉA et des escarmouches 5 ; un changement de carte ou de calque compte comme un déplacement."},
}


def diff(root, v1, v2, lang_ui="en", limit=40):
    (d1, m1), (d2, m2) = resolve(root, v1), resolve(root, v2)
    old, new = load(d1), load(d2)
    results = [compare(old, new, s) for s in specs(context(old, new))]
    for r in results:
        if (r["family"], r["list"]) == ("catalog-items", "Items"):
            r["sourced"] = sources_of(new)
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
    if version not in snaps or "snapshot" not in snaps[version][1].get("sources", []) \
            or "placements" not in snaps[version][1].get("families", []):  # X-116: a snapshot of X-115 has no layout placements
        snapshot(root, sqpack, lang)
    games = [m["game"] for _, m in snapshots(root)]
    older = [g for g in games if g != version and games.index(g) < games.index(version)]
    if not older:
        print(f"only {version} is stored: nothing to compare yet")
        return 0, None, None
    text, results = diff(root, older[-1], version, lang_ui)
    out = root / version / f"diff-from-{older[-1]}.md"
    out.write_text(text, encoding="utf-8")
    metas = {m["game"]: m for _, m in snapshots(root)}
    changes = root / version / f"changes-from-{older[-1]}.json"
    changes.write_text(json.dumps(changes_doc(results, metas[older[-1]], metas[version]), ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"report: {out}")
    print(f"changes: {changes}")
    print(counts_line(results))
    return 0, out, results


# ---------- X-116: facts against the client, places to recheck, content to discover ----------

# X-357 (0.1349): the community base is the v3 set (index.json and kind-NN.jsonl.gz); the v2 discoveries.json is gone.
COMMUNITY = ROOT / "community-discoveries" / "v3"
LOCAL_FACTS = XIVL / "pluginConfigs" / "TenshoAssistant" / "Discoveries" / "Discoveries.json"
# Kinds whose place the client also holds (DiscoveriesModel.cs): NpcPlace (19) → layout NPC of that ENpc; FateSeen (3) → the FATE's range;
# FishCatch, FishBite, FishBiteBait, IntuitionStart (10, 23, 29, 30) → fishing spots of the map; SpearShadow (28) → spearfishing points.
CHECKED = {19: "enpc", 3: "fate", 10: "fishing", 23: "fishing", 29: "fishing", 30: "fishing", 28: "spear"}
# Monsters (MonsterPlace, HuntMarkPlace, MonsterDrop, BlueSpellCaster, MonsterKill): placed by the server, never by the layout files
# (0 BattleNPC entry in 3 733 files, docs/evidence/x116-client-placements.md): never compared.
MONSTER_KINDS = {1, 2, 8, 22, 33}
# Distance in yalms past which a fact is SUSPECT (to the placement for NPCs and FATEs, to the edge of the area for fishing and
# spearfishing). Proven on the community base and the maintainer's own facts of 2026-11-10 (docs/evidence/x116-client-placements.md).
THRESHOLDS = {"enpc": 2.0, "fate": 5.0, "fishing": 50.0, "spear": 20.0}
# Kinds whose Subject (and Detail, second set) is an item: a fact that names an item tells where it comes from (to discover).
ITEM_SUBJECT = {4, 5, 6, 7, 8, 9, 10, 11, 13, 20, 21, 23, 24, 25, 28, 29, 30, 31, 35, 36, 37}
ITEM_DETAIL = {4, 9, 10, 24, 29, 35}
PLACED = {1, 2, 3, 8, 10, 19, 22, 23, 28, 29, 30}  # tools/discoveries-merge.py PLACED (DiscoveryStore.HasPlace)


def merge_tool():
    """tools/discoveries-merge.py as a module (its v3 reader)."""
    import importlib.util
    spec = importlib.util.spec_from_file_location("discoveries_merge", ROOT / "tools" / "discoveries-merge.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def read_facts(paths=None):
    """[(source, fact)] of the community base and this PC's own facts (the defaults), or of the files given. A folder is a v3 set, read by
    tools/discoveries-merge.py (bounded, checked against its index)."""
    out = []
    for path in [Path(x) for x in paths] if paths else [COMMUNITY, LOCAL_FACTS]:
        try:
            data = merge_tool().from_v3_dir(str(path)) if path.is_dir() else json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            continue
        name = "community" if path == COMMUNITY else "local" if path == LOCAL_FACTS else path.name
        out += [(name, f) for f in data.get("Facts", [])]
    return out


def places_index(snap):
    """{family: {key: {territory or map: [(x, z, radius)]}}} of one snapshot: layout NPCs and FATE ranges by base and territory, fishing spots by
    map, spearfishing points by territory."""
    ctx = context(snap)
    idx = {"enpc": {}, "fate": {}, "fishing": {}, "spear": {}}
    pl = snap.get("placements", {})
    for fam in ("enpc", "fate"):
        for r in pl.get(fam, []):
            for terr in ctx["bg_territories"].get(r["Bg"], ()):
                idx[fam].setdefault(r["Base"], {}).setdefault(terr, []).append((r["X"], r["Z"], 0.0))
    spear = {s["BaseId"] for s in snap.get("catalog-gathering", {}).get("Sources", []) if s.get("TypeId") == 5}
    for r in snap.get("catalog-map-positions", {}).get("Positions", []):
        if r["Kind"] == 2 and r["MapId"]:
            idx["fishing"].setdefault(0, {}).setdefault(r["MapId"], []).append((r["X"], r["Z"], r.get("Radius", 0.0)))
        elif r["Kind"] == 1 and r["Id"] in spear and ctx["point_territory"].get(r["Id"]):
            idx["spear"].setdefault(0, {}).setdefault(ctx["point_territory"][r["Id"]], []).append((r["X"], r["Z"], r.get("Radius", 0.0)))
    return idx, bool(pl)


def check_fact(idx, f):
    """(state, distance, where): state ok / far / elsewhere (placed, not in this zone) / unplaced (the client places it nowhere) / none
    (no area on this map); distance to the placement, or to the edge of the area for fishing and spearfishing."""
    fam = CHECKED[f["Kind"]]
    if fam in ("enpc", "fate"):
        zones = idx[fam].get(f["Subject"])
        if not zones:
            return "unplaced", None, None
        here = zones.get(f["Territory"])
        if not here:
            return "elsewhere", None, sorted(zones)[:5]
    else:
        here = idx[fam].get(0, {}).get(f["Map"] if fam == "fishing" else f["Territory"])
        if not here:
            return "none", None, None
    d, best = min((max(0.0, math.hypot(x - f["X"], z - f["Z"]) - rad), (x, z)) for x, z, rad in here)
    return ("far" if d > THRESHOLDS[fam] else "ok"), d, best


def quantiles(values):
    if not values:
        return "—"
    v = sorted(values)
    q = lambda p: v[min(len(v) - 1, int(p * (len(v) - 1) + 0.5))]
    return f"n {len(v)}, median {q(0.5):.1f}, p90 {q(0.9):.1f}, p99 {q(0.99):.1f}, max {v[-1]:.1f}"


def check_facts(snap, facts, limit=40):
    """Report (markdown lines) and counts {kind: Counter(state)} of the facts the client can place. Nothing is written."""
    idx, has_placements = places_index(snap)
    by_kind, dists, suspects, others = {}, {}, [], []
    for source, f in facts:
        k = f.get("Kind")
        if k not in CHECKED or (CHECKED[k] in ("enpc", "fate") and not has_placements):
            continue
        state, d, where = check_fact(idx, f)
        by_kind.setdefault(k, {}).setdefault(state, 0)
        by_kind[k][state] += 1
        if d is not None:
            dists.setdefault(k, []).append(d)
        line = f"kind {k} subject {f['Subject']} territory {f['Territory']} map {f['Map']} at {f['X']:.1f},{f['Z']:.1f} ({source})"
        if state == "far":
            suspects.append(f"SUSPECT {line}: {d:.1f} yalms from the nearest {CHECKED[k]} placement {where[0]:.1f},{where[1]:.1f} "
                            f"(threshold {THRESHOLDS[CHECKED[k]]:.0f})")
        elif state == "elsewhere":
            others.append(f"NOTE {line}: the client places it only in territories {where}")
    out = ["| Kind | Facts | ok | SUSPECT | elsewhere | unplaced | no area | Distances (yalms) |", "|---|---|---|---|---|---|---|---|"]
    for k in sorted(by_kind):
        c = by_kind[k]
        out.append(f"| {k} ({CHECKED[k]}) | {sum(c.values())} | {c.get('ok', 0)} | {c.get('far', 0)} | {c.get('elsewhere', 0)} | "
                   f"{c.get('unplaced', 0)} | {c.get('none', 0)} | {quantiles(dists.get(k, []))} |")
    if not has_placements:
        out.append("")
        out.append("NPCs and FATEs not compared: this snapshot has no layout placements (taken before X-116).")
    out += [""] + suspects[:limit] + (["… and %d more" % (len(suspects) - limit)] if len(suspects) > limit else [])
    out += others[:limit] + (["… and %d more" % (len(others) - limit)] if len(others) > limit else [])
    return out, by_kind


def patch_date(version):
    """The day of a game version string (2026.11.03.0000.0000 → 2026-11-03)."""
    m = re.match(r"(\d{4})\.(\d{2})\.(\d{2})", version)
    return f"{m.group(1)}-{m.group(2)}-{m.group(3)}" if m else ""


def changes_doc(results, m1, m2):
    """The machine form of a diff, read by discoveries-merge.py recheck: every changed place with its territory and world X/Z."""
    return {"Schema": 1, "Old": m1["game"], "New": m2["game"], "PatchDate": patch_date(m2["game"]),
            "Missing": [r["title"] for r in results if not r["present"]],
            "Places": [{k: v for k, v in p.items() if k != "label"} | {"label": p["label"][:120]} for r in results for p in r["places"]]}


def discover(results, facts, limit=60):
    """Content the patch added with no place in the client and no fact known: [(what, French line)]. Items: no gathering source, no shop sale,
    no recipe, no fact naming them; vendors: no position in the client and no NpcPlace fact; fish: no fishing spot in the log."""
    by = {(r["family"], r["list"]): r for r in results}
    present = lambda fam, lst: by.get((fam, lst), {}).get("present")
    new_rows = lambda fam, lst: by.get((fam, lst), {}).get("new_rows", [])
    named = set()
    for _, f in facts:
        if f.get("Kind") in ITEM_SUBJECT:
            named.add(f["Subject"] % 1_000_000 if f["Subject"] < 2_000_000 else f["Subject"])
        if f.get("Kind") in ITEM_DETAIL:
            named.add(f["Detail"])
    npc_seen = {f["Subject"] for _, f in facts if f.get("Kind") == 19}
    out = []
    if present("catalog-items", "Items"):
        new_ids = {r["Id"] for r in new_rows("catalog-items", "Items")}
        # the sources the new snapshot holds for them (every row, not only the new ones: an old shop may sell a new item)
        sourced = by.get(("catalog-items", "Items"), {}).get("sourced", set())
        for r in sorted(new_rows("catalog-items", "Items"), key=lambda r: r["Id"]):
            if r["Id"] not in sourced and r["Id"] not in named:
                out.append(("item", f"Objet {r['Name']} ({r['Id']}) : aucune source dans le client (récolte, boutique, recette) ni fait connu : où l'obtient-on ?"))
    if present("catalog-npc-vendors", "Vendors"):
        for r in sorted(new_rows("catalog-npc-vendors", "Vendors"), key=lambda r: r["NpcId"]):
            if not r.get("Locations") and r["NpcId"] not in npc_seen:
                out.append(("vendor", f"Marchand {r['Name']} ({r['NpcId']}) : aucune position dans le client ni fait connu : où est-il ? (ouvrir sa boutique le note)"))
    if present("catalog-gathering", "Fishing"):
        for r in sorted(new_rows("catalog-gathering", "Fishing"), key=lambda r: r["FishId"]):
            if not r.get("NotebookSpotId") and r["ItemId"] not in named:
                out.append(("fish", f"Poisson {r.get('Name') or r['ItemId']} (objet {r['ItemId']}) : aucun lieu de pêche dans le carnet ni fait connu."))
    return out[:limit], len(out)


def sources_of(snap):
    """Every item the snapshot gives a source: gathered, fished, sold, crafted."""
    g = snap.get("catalog-gathering", {})
    out = {s["ItemId"] for s in g.get("Sources", [])} | {f["ItemId"] for f in g.get("Fishing", [])}
    out |= {s["ItemId"] for s in snap.get("catalog-npc-vendors", {}).get("Sales", [])}
    out |= {r["ResultItemId"] for r in snap.get("catalog-recipes", {}).get("Rows", [])}
    return out


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

        # X-116: layout placements (index and default values left out, as ClientSnapshot writes them), moves per family, changed places,
        # facts against the client, content to discover.
        def placements(rows):
            return {"Schema": 1, "Bgs": ["ffxiv/a/fld/a1f1", "ffxiv/b/fld/b1f1"],
                    "Territories": [{"Id": 10, "Bg": "ffxiv/a/fld/a1f1", "Name": "Alpha", "Map": 2}, {"Id": 11, "Bg": "ffxiv/b/fld/b1f1", "Name": "Beta", "Map": 3}],
                    "Names": {"enpc": {"7": "Marchand"}, "fate": {"4": "ALÉA"}}, "Placements": rows}
        old_rows = [{"Family": "enpc", "Instance": 1, "Base": 7, "X": 100.0, "Z": 100.0}, {"Family": "fate", "Bg": 1, "Instance": 2, "Base": 4, "X": 0.0, "Z": 0.0},
                    {"Family": "eobj", "Instance": 3, "Base": 9, "X": 5.0, "Z": 5.0}]
        new_rows = [{"Family": "enpc", "Instance": 1, "Base": 7, "X": 100.5, "Z": 100.0}, {"Family": "fate", "Bg": 1, "Instance": 2, "Base": 4, "X": 30.0, "Z": 40.0},
                    {"Family": "eobj", "Instance": 3, "Base": 9, "X": 5.0, "Z": 5.0, "Festival": 3}, {"Family": "eobj", "Instance": 5, "Base": 8, "X": -60.0, "Z": 10.0}]
        dirs = [d for d, _ in snapshots(root)]
        for d, rows in zip(dirs, (old_rows, new_rows)):
            (d / "client-placements.json").write_text(json.dumps(placements(rows)), encoding="utf-8")
        text, results = diff(root, "previous", "latest")
        res = {r["title"]: r for r in results}
        check(res["NPC placements"]["moved"] == [] and len(res["FATE ranges"]["moved"]) == 1, "moves under and over the family threshold")
        check(res["FATE ranges"]["moved"][0].startswith("ALÉA (4) — Beta"), "placement label: name, base, zone")
        check(len(res["object placements"]["added"]) == 1 and len(res["object placements"]["changed"]) == 1, "object added, festival changed")
        places = [(p["title"], p["part"], p["territory"], p["x"]) for p in changes_doc(results, {"game": "2026.01.01.0000.0000"}, {"game": "2026.02.01.0000.0000"})["Places"]]
        check(("FATE ranges", "moved", 11, 0.0) in places and ("FATE ranges", "moved", 11, 30.0) in places and ("object placements", "added", 10, -60.0) in places
              and ("map positions", "moved", 0, 0.0) not in places, "changed places: old and new spot of a move, territory from the layout")
        check(patch_date("2026.11.03.0000.0000") == "2026-11-03", "patch day")
        snap = load(dirs[1])
        snap["catalog-map-positions"] = {"Positions": [{"Kind": 2, "Id": 1, "MapId": 2, "X": 0.0, "Z": 0.0, "Radius": 10.0}]}
        fact = lambda kind, subject, terr, x, z, m=2: {"Kind": kind, "Subject": subject, "Detail": 0, "Territory": terr, "Map": m, "X": x, "Z": z}
        facts = [("t", fact(19, 7, 10, 100.5, 100.0)), ("t", fact(19, 7, 10, 110.0, 100.0)), ("t", fact(19, 7, 11, 0.0, 0.0)), ("t", fact(19, 6, 10, 0.0, 0.0)),
                 ("t", fact(3, 4, 11, 30.0, 40.0)), ("t", fact(10, 99, 10, 25.0, 0.0)), ("t", fact(10, 99, 10, 100.0, 0.0)), ("t", fact(10, 99, 10, 0.0, 0.0, 5)),
                 ("t", fact(1, 5, 10, 0.0, 0.0))]
        _, counts = check_facts(snap, facts)
        check(counts.get(19) == {"ok": 1, "far": 1, "elsewhere": 1, "unplaced": 1}, f"NPC states {counts.get(19)}")
        check(counts.get(3) == {"ok": 1} and counts.get(10) == {"ok": 1, "far": 1, "none": 1} and 1 not in counts, f"FATE, fishing, monsters {counts}")
        found, n = discover(results, [("t", {"Kind": 4, "Subject": 3, "Detail": 0})])
        check(n == 0, "a new item named by a fact is not to discover")
        # X-357 (0.1349): the community base read from the v3 set (the v2 discoveries.json is gone).
        if (COMMUNITY / "index.json").is_file():
            base = [s for s, _ in read_facts()]
            check(COMMUNITY.is_dir() and base.count("community") > 0 and read_facts([str(COMMUNITY)])[0][0] == "community",
                  f"community base read from the v3 set ({base.count('community')} facts)")
        found, n = discover(results, [])
        check(n == 1 and "Nouveau (3)" in found[0][1], "a new item with no source and no fact is to discover")
    print(f"client-snapshot selftest: {ok}/{total} ok")
    return 0 if ok == total else 1


def positional(argv):
    """The arguments that are neither an option nor an option's value."""
    out, skip = [], False
    for a in argv:
        if skip:
            skip = False
        elif a.startswith("--"):
            skip = a in ("--root", "--sqpack", "--lang", "--label", "--out", "--limit", "--snapshot")
        else:
            out.append(a)
    return out


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
    if cmd == "check":
        files = positional(argv[1:])
        d, meta = resolve(root, opt(argv, "--snapshot") or "latest")
        lines, _ = check_facts(load(d), read_facts(files or None), int(opt(argv, "--limit", "40")))
        print(f"# Facts against the client {tag(meta)}\n")
        print("\n".join(lines))
        return 0
    if cmd == "discover" and len(argv) > 2:
        _, results = diff(root, argv[1], argv[2])
        rows, total = discover(results, read_facts())
        print("\n".join(line for _, line in rows) or "rien à découvrir")
        return 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
