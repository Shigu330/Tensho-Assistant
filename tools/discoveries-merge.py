"""X-77 (0.882), 0.886: builds community-discoveries/discoveries.json from what players shared (maintainer tool, run by hand).

Usage:
  python tools/discoveries-merge.py sync [--dry-run]
      Reads every open « Discoveries » issue (label discoveries) through the GitHub CLI, fetches the gist it points to, keeps the player's valid facts
      in community-discoveries/players/<issue>.json (replacing that player's previous file: an updated gist is never counted twice), then rebuilds
      discoveries.json from all player files. --dry-run: report only, nothing written.
  python tools/discoveries-merge.py rebuild
      Rebuilds discoveries.json from community-discoveries/players/*.json.
  python tools/discoveries-merge.py check <file.json|file.zip|gist:ID>
      Validates one file and prints its report, without writing.
  python tools/discoveries-merge.py recheck [--changes FILE] [--local] [--limit N]
      X-116: the facts « to recheck » after a game patch: seen last before the patch day, in a zone or a 50-yalm cell (or next to one) that the
      client comparison says changed (tools/client-snapshot.py patch writes changes-from-<old>.json; the latest one by default), or about an NPC
      or FATE the patch moved or removed. Read-only; --local adds this PC's own Discoveries.json. `sync --dry-run` ends with it.
  python tools/discoveries-merge.py notice <issue> merged <version> <facts> [--dry-run]
  python tools/discoveries-merge.py notice <issue> flag <refused> <total> [--dry-run]
      Comments the ticket in English then French (tools/bilingual.py): facts merged into a version, or a share not merged because over 5 % of
      its facts were refused. --dry-run: prints the comment only.

Rules mirror DiscoveryStore.Valid (src/TenshoAssistant/Discoveries/DiscoveriesModel.cs): a fact that does not hold is refused; any property the plugin does
not write is refused (the computed « Key » is accepted and dropped). In the common file each fact has Seen 0 and Imported = the number of players who have it;
levels and dates are the widest seen. Nothing personal can be in a fact by construction; the report flags a player whose refusals pass 5 %.
0.1012: dates are checked as sent, then kept to the day (First and Last at 00:00 UTC) in every file written here; a gist with times is accepted and
brought to the day. Shared discoveries are published under CC0-1.0 (community-discoveries/README.md).
X-76 E1 (0.916, docs/evidence/0.916-discoveries-format.md): files of version 1 and 2 are read, the common file is written as version 2. Version 2 adds two
optional fields: Hq (how many sightings were HQ) and Values (value seen -> count, at most 32 values, ValueCounts in the plugin); the common file holds their
sums over the players (a new value past 32 is counted under the nearest value kept, the lower one on a tie). ImportedHq and ImportedValues (what the player
imported, never shared by the plugin) are accepted and dropped.
X-113 (0.1029): a ticket whose gist brings nothing new (same facts as its player file) is reported « nothing new »: its file is not written again,
and it gets no « Merged into » comment; discoveries.json is written only when its facts change (not for a new date alone)."""
import base64,collections,gzip,importlib.util,json,math,os,re,subprocess,sys,urllib.request,zipfile
from datetime import datetime,timezone,timedelta
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COMMON=os.path.join(ROOT,"community-discoveries","discoveries.json");PLAYERS=os.path.join(ROOT,"community-discoveries","players")
QUEST_ASKED_FIXED=datetime(2026,11,8,15,42,29,tzinfo=timezone.utc)
QUEST_ASKED_FIXED_DAY=datetime(2026,11,9,tzinfo=timezone.utc)  # 0.1012: a date kept to the day only passes the fix if it is on a later day
KINDS=set(range(1,38))-{26,27};PLACED={1,2,3,8,10,19,22,23,28,29,30}  # 26, 27: 0.890's and 0.891's wrong spearfishing shadows
CELL=50;MAX_FACTS=200_000;VERSIONS={1,2};VERSION=2
FIELDS={"Kind","Subject","Detail","Territory","Map","CellX","CellZ","X","Z","LevelMin","LevelMax","Seen","Imported","First","Last","Hq","Values"}
MAX_VALUES=32;MAX_VALUE=100_000;MAX_COUNT=100_000_000

def key(f):return f"{f['Kind']}:{f['Subject']}:{f['Detail']}:{f['Territory']}:{f['Map']}:{f['CellX']}:{f['CellZ']}"
def whole(n):return isinstance(n,int) and not isinstance(n,bool)
def histogram_valid(values):
    """ValueCounts.Valid: at most 32 values from 0 to 100 000 (JSON keys, written as numbers), each counted 1 to 100 000 000 times."""
    return isinstance(values,dict) and len(values)<=MAX_VALUES and all(isinstance(k,str) and re.fullmatch(r"0|[1-9][0-9]{0,5}",k) and int(k)<=MAX_VALUE
        and whole(c) and 1<=c<=MAX_COUNT for k,c in values.items())
def histogram_add(values,value,count):
    """ValueCounts.Add: counted under the value, or under the nearest value kept (the lower one on a tie) when 32 are kept."""
    k=value if value in values or len(values)<MAX_VALUES else min(values,key=lambda v:(abs(v-value),v))
    values[k]=min(values.get(k,0)+count,MAX_COUNT)
def histogram_sum(first,second):
    """ValueCounts.Sum over JSON histograms: the second's values taken in increasing order; keys written in increasing order."""
    values={int(k):c for k,c in (first or {}).items()}
    for k,c in sorted((int(k),c) for k,c in (second or {}).items()):histogram_add(values,k,c)
    return {str(k):values[k] for k in sorted(values)}
def when(text):return datetime.fromisoformat(text.replace("Z","+00:00"))
def day(text):
    """0.1012: the day of a date, as the plugin shares it (CommunityDiscoveries.Day): what the repository publishes never holds the time of a sighting."""
    return when(text).astimezone(timezone.utc).strftime("%Y-%m-%dT00:00:00+00:00")
def daily(f):
    """The fact with First and Last kept to the day."""
    f["First"]=day(f["First"]);f["Last"]=day(f["Last"]);return f
def valid(f):
    try:
        f.pop("Key",None)  # computed by the plugin (DiscoveryFact.Key): written in its files, recomputed here
        f.pop("ImportedHq",None);f.pop("ImportedValues",None)  # 0.916: what the player imported, not theirs (an export holds it; the plugin never shares it)
        if set(f)-FIELDS or f["Kind"] not in KINDS or f["Subject"]<=0:return False
        if f["Kind"] in PLACED and f["Map"]==0:return False
        if f["Kind"]==3 and f["X"]==0 and f["Z"]==0:return False  # 0.911: a FATE at exactly (0, 0) is the game's zero position for about a second
        if f["Kind"]==25 and when(f["Last"])<(QUEST_ASKED_FIXED_DAY if day(f["Last"])==f["Last"] else QUEST_ASKED_FIXED):return False  # 0.1005: QuestItemAsks.Fixed, a quest item noted before 0.911's fix
        if not all(math.isfinite(f[n]) and abs(f[n])<=10000 for n in ("X","Z")):return False
        if (math.floor(f["X"]/CELL),math.floor(f["Z"]/CELL))!=(f["CellX"],f["CellZ"]):return False
        if not (0<=f["LevelMin"]<=100000 and 0<=f["LevelMax"]<=100000):return False
        # 0.916 (version 2): the histogram in its bounds, never more HQ sightings than sightings counted in it.
        if "Values" in f and not histogram_valid(f["Values"]):return False
        if "Hq" in f and not (whole(f["Hq"]) and 0<=f["Hq"]<=sum(f.get("Values",{}).values())):return False
        return when(f["First"])<=when(f["Last"])<=datetime.now(timezone.utc)+timedelta(days=1)
    except Exception:return False

def from_gist(reference):
    """A gist id or URL; the file is base64 of gzip of JSON (GitHubExchange.GistFile)."""
    gist=reference.removeprefix("gist:").rstrip("/").split("/")[-1]
    raw=subprocess.run(["gh","api",f"gists/{gist}","--jq",'.files["tensho-discoveries.b64"].raw_url'],capture_output=True,text=True,check=True).stdout.strip()
    if not raw.startswith("https://gist.githubusercontent.com/"):raise ValueError("unexpected gist file address")
    data=gzip.decompress(base64.b64decode(urllib.request.urlopen(raw).read().decode().strip()))
    if len(data)>64*1024*1024:raise ValueError("too large")
    return json.loads(data.decode("utf-8"))

def read(path):
    if path.startswith("gist:") or "gist.github.com" in path:return from_gist(path)
    if path.endswith(".zip"):
        with zipfile.ZipFile(path) as z:
            name=next(n for n in z.namelist() if n.endswith(".json"))
            if z.getinfo(name).file_size>64*1024*1024:raise ValueError("too large")
            return json.loads(z.read(name).decode("utf-8"))
    return json.load(open(path,encoding="utf-8"))

def clean(data):
    """The valid facts of a shared file (Seen 0, Imported 1) and a report."""
    if data.get("Version") not in VERSIONS:return [],{"facts":0,"refused":0,"kinds":{},"error":"version"}
    facts={};refused=0
    for f in data.get("Facts",[])[:MAX_FACTS]:
        # 0.1012: checked on the time as sent, then kept to the day (an older gist with times is accepted and brought to the day, not refused).
        if not valid(f) or not valid(daily(dict(f))):refused+=1;continue
        g=daily(dict(f));g["Seen"]=0;g["Imported"]=1;facts[key(g)]=g
    kinds=collections.Counter(f["Kind"] for f in facts.values())
    return list(facts.values()),{"facts":len(facts),"refused":refused,"kinds":dict(sorted(kinds.items()))}

def rebuild():
    merged={}
    for name in sorted(os.listdir(PLAYERS)) if os.path.isdir(PLAYERS) else []:
        if not name.endswith(".json"):continue
        path=os.path.join(PLAYERS,name);player=json.load(open(path,encoding="utf-8"))
        # 0.1005: a player file kept before a new rule follows it too, written again without the facts it now refuses.
        kept=[f for f in player["Facts"] if valid(dict(f))]
        # 0.1012: a player file kept with times is written again with the day only.
        timed=sum(1 for f in kept if day(f["First"])!=f["First"] or day(f["Last"])!=f["Last"])
        kept=[daily(f) for f in kept]
        if timed:print(f"{name}: {timed} facts brought to the day")
        if len(kept)<len(player["Facts"]) or timed:
            if len(kept)<len(player["Facts"]):print(f"{name}: {len(player['Facts'])-len(kept)} facts refused by the current rules")
            player["Facts"]=kept;json.dump(player,open(path,"w",encoding="utf-8",newline="\n"),ensure_ascii=False,separators=(",",":"))
        for f in kept:
            known=merged.get(key(f))
            if known is None:merged[key(f)]=dict(f);continue
            known["Imported"]+=1
            if f["LevelMin"]>0:known["LevelMin"]=f["LevelMin"] if known["LevelMin"]==0 else min(known["LevelMin"],f["LevelMin"])
            known["LevelMax"]=max(known["LevelMax"],f["LevelMax"])
            if when(f["First"])<when(known["First"]):known["First"]=f["First"]
            if when(f["Last"])>when(known["Last"]):known["Last"]=f["Last"]
            # 0.916: HQ sightings and values added over the players (each player's file counts once).
            if "Values" in f:known["Values"]=histogram_sum(known.get("Values"),f["Values"])
            if f.get("Hq",0)>0:known["Hq"]=min(known.get("Hq",0)+f["Hq"],sum(known["Values"].values()))
    common={"Version":VERSION,"Producer":"Tensho Assistant community","Written":datetime.now(timezone.utc).isoformat(),
            "Facts":sorted(merged.values(),key=lambda f:(f["Kind"],f["Subject"],f["Detail"],f["Territory"],f["Map"],f["CellX"],f["CellZ"]))}
    players=len([n for n in os.listdir(PLAYERS) if n.endswith(".json")]) if os.path.isdir(PLAYERS) else 0
    try:before=json.load(open(COMMON,encoding="utf-8"))
    except (OSError,ValueError):before={}
    # X-113: the same facts are not written again (the date alone changed in 0.1027).
    if before.get("Version")==VERSION and before.get("Facts")==common["Facts"]:
        print(f"discoveries.json: unchanged, {len(common['Facts'])} facts from {players} players");return False
    json.dump(common,open(COMMON,"w",encoding="utf-8",newline="\n"),ensure_ascii=False,separators=(",",":"))
    print(f"discoveries.json: {len(common['Facts'])} facts from {players} players");return True

def news(issue,facts):
    """X-113: (new or changed, gone) facts of a ticket against its player file: (0, 0) is « nothing new »."""
    try:kept=json.load(open(os.path.join(PLAYERS,f"{issue}.json"),encoding="utf-8"))["Facts"]
    except (OSError,ValueError,KeyError):kept=[]
    old={key(f):json.dumps(f,sort_keys=True) for f in kept};new={key(f):json.dumps(f,sort_keys=True) for f in facts}
    return sum(1 for k,v in new.items() if old.get(k)!=v),sum(1 for k in old if k not in new)

def sync(dry):
    issues=json.loads(subprocess.run(["gh","issue","list","-R","Shigu330/Tensho-Assistant","--label","discoveries","--state","open","--limit","500",
        "--json","number,title,body,author"],capture_output=True,text=True,check=True).stdout)
    os.makedirs(PLAYERS,exist_ok=True)
    for issue in issues:
        found=re.search(r"https://gist\.github\.com/([0-9a-f]{20,40})",issue.get("body") or "")
        if not found:print(f"#{issue['number']} {issue['title']}: no gist link, skipped");continue
        try:facts,report=clean(from_gist(found.group(1)))
        except Exception as error:print(f"#{issue['number']} {issue['title']}: gist unreadable ({type(error).__name__}), skipped");continue
        total=report["facts"]+report["refused"];flag=" FLAG: refusals over 5 %" if total and report["refused"]/total>0.05 else ""
        added,gone=news(issue["number"],facts)
        what=" nothing new (same facts as its player file): no write, no « Merged » comment" if (added,gone)==(0,0) else f" {added} new or changed, {gone} gone"
        print(f"#{issue['number']} {issue['title']} (author {issue['author']['login']}): {report['facts']} facts, {report['refused']} refused, kinds {report['kinds']};{what}{flag}")
        if dry or flag or (added,gone)==(0,0):continue
        json.dump({"Issue":issue["number"],"Facts":facts},open(os.path.join(PLAYERS,f"{issue['number']}.json"),"w",encoding="utf-8",newline="\n"),ensure_ascii=False,separators=(",",":"))
    if not dry:rebuild()
    else:
        print();print(recheck_report())

# ---------- X-116: facts to recheck after a game patch ----------
CELL_KINDS=PLACED  # every fact with a place: a changed layout cell can change what was seen there
ZONE_KINDS={"maps":PLACED,"gathering sources":{10,23,28,29,30}}  # a change with no position: the whole zone, for the kinds it touches
SUBJECT_KINDS={19:"enpc",3:"fate"}  # a fact about an NPC or a FATE the patch moved or removed, wherever it was seen

def snapshots_module():
    spec=importlib.util.spec_from_file_location("client_snapshot",os.path.join(ROOT,"tools","client-snapshot.py"))
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod);return mod

def latest_changes(cs=None):
    """The newest changes-from-*.json of the snapshot store (tools/client-snapshot.py patch), or None."""
    cs=cs or snapshots_module();found=[]
    for d,meta in cs.snapshots(cs.default_root()):
        found+=sorted(d.glob("changes-from-*.json"))
    return found[-1] if found else None

def recheck(changes,facts):
    """[(source, fact, reasons)] of the facts seen last before the patch day in a changed cell or zone, or about a moved or removed subject."""
    day=changes.get("PatchDate","")
    cells,zones,subjects={},{},{}
    for pl in changes.get("Places",[]):
        if pl["x"] is None:zones.setdefault(pl["territory"],[]).append(pl)
        else:cells.setdefault((pl["territory"],math.floor(pl["x"]/CELL),math.floor(pl["z"]/CELL)),[]).append(pl)
        if pl.get("subject") and pl["part"] in ("moved","removed"):subjects.setdefault(tuple(pl["subject"]),[]).append(pl)
    out=[]
    for source,f in facts:
        if f.get("Kind") not in PLACED or not f.get("Map") or not day or f["Last"][:10]>=day:continue
        reasons=[]
        for dx in (-1,0,1):
            for dz in (-1,0,1):
                for pl in cells.get((f["Territory"],f["CellX"]+dx,f["CellZ"]+dz),[]):reasons.append(f"cell: {pl['title']} {pl['part']} — {pl['label']}")
        for pl in zones.get(f["Territory"],[]):
            if f["Kind"] in ZONE_KINDS.get(pl["title"],()):reasons.append(f"zone: {pl['title']} {pl['part']} — {pl['label']}")
        fam=SUBJECT_KINDS.get(f["Kind"])
        for pl in subjects.get((fam,f["Subject"]),[]) if fam else []:reasons.append(f"subject: {pl['title']} {pl['part']} — {pl['label']}")
        if reasons:out.append((source,f,sorted(set(reasons))))
    return out

def facts_of(local=False):
    out=[("community",f) for f in json.load(open(COMMON,encoding="utf-8")).get("Facts",[])] if os.path.exists(COMMON) else []
    if local:
        path=os.path.join(os.environ.get("APPDATA",""),"XIVLauncher","pluginConfigs","TenshoAssistant","Discoveries","Discoveries.json")
        try:out+=[("local",f) for f in json.load(open(path,encoding="utf-8")).get("Facts",[])]
        except (OSError,ValueError):pass
    return out

def recheck_report(changes_path=None,local=False,limit=40):
    """Text report (English, maintainer tool)."""
    path=changes_path or latest_changes()
    if not path:return "To recheck: no client comparison yet (tools/client-snapshot.py patch)."
    changes=json.load(open(path,encoding="utf-8"));rows=recheck(changes,facts_of(local))
    head=f"To recheck after the patch {changes['Old']} → {changes['New']} (day {changes['PatchDate']}, {len(changes.get('Places',[]))} changed places): {len(rows)} facts"
    if changes.get("Missing"):head+=f"; not compared (missing from one snapshot): {', '.join(changes['Missing'])}"
    lines=[head]
    by=collections.Counter(f["Kind"] for _,f,_ in rows)
    if by:lines.append("by kind: "+", ".join(f"{k}: {n}" for k,n in sorted(by.items())))
    for source,f,reasons in rows[:limit]:
        lines.append(f"- kind {f['Kind']} subject {f['Subject']} territory {f['Territory']} cell {f['CellX']},{f['CellZ']} last {f['Last'][:10]} ({source}): {reasons[0]}"+(f" (+{len(reasons)-1})" if len(reasons)>1 else ""))
    if len(rows)>limit:lines.append(f"- … and {len(rows)-limit} more")
    return "\n".join(lines)

def selftest():
    """recheck and news on made-up data (no network, no game)."""
    ok=total=0
    def check(cond,what):
        nonlocal ok,total;total+=1;ok+=bool(cond)
        if not cond:print("FAIL",what)
    f=lambda kind,subject,t,x,z,last:{"Kind":kind,"Subject":subject,"Detail":0,"Territory":t,"Map":1,"CellX":math.floor(x/CELL),"CellZ":math.floor(z/CELL),"X":x,"Z":z,
        "LevelMin":1,"LevelMax":1,"Seen":0,"Imported":1,"First":last,"Last":last}
    changes={"Old":"a","New":"b","PatchDate":"2026-11-03","Places":[
        {"title":"NPC placements","part":"moved","territory":128,"x":10.0,"z":120.0,"subject":["enpc",7],"label":"npc"},
        {"title":"maps","part":"changed","territory":129,"x":None,"z":None,"subject":None,"label":"map"},
        {"title":"gathering sources","part":"added","territory":130,"x":None,"z":None,"subject":["item",5],"label":"src"}]}
    facts=[("c",f(1,1,128,60.0,140.0,"2026-11-01T00:00:00+00:00")),  # next cell, before: recheck
           ("c",f(1,1,128,60.0,140.0,"2026-11-03T00:00:00+00:00")),  # seen on the patch day: no
           ("c",f(1,1,128,300.0,300.0,"2026-11-01T00:00:00+00:00")),  # far cell: no
           ("c",f(19,7,999,0.0,0.0,"2026-11-01T00:00:00+00:00")),  # the NPC moved, seen elsewhere: subject
           ("c",f(3,2,129,0.0,0.0,"2026-11-01T00:00:00+00:00")),  # map changed: zone
           ("c",f(1,1,130,0.0,0.0,"2026-11-01T00:00:00+00:00")),  # gathering zone change does not touch monsters
           ("c",f(10,9,130,0.0,0.0,"2026-11-01T00:00:00+00:00")),  # but touches fishing
           ("c",{**f(4,1,128,10.0,120.0,"2026-11-01T00:00:00+00:00"),"Map":0})]  # no place: never
    got=[(r[1]["Kind"],r[1]["Territory"],r[1]["Last"][:10]) for r in recheck(changes,facts)]
    check(got==[(1,128,"2026-11-01"),(19,999,"2026-11-01"),(3,129,"2026-11-01"),(10,130,"2026-11-01")],f"recheck rows {got}")
    check(recheck({**changes,"PatchDate":""},facts)==[],"no patch day: nothing")
    global PLAYERS
    import tempfile
    saved=PLAYERS
    with tempfile.TemporaryDirectory() as tmp:
        PLAYERS=tmp
        a=f(1,1,128,60.0,140.0,"2026-11-01T00:00:00+00:00");b=f(1,2,128,60.0,140.0,"2026-11-01T00:00:00+00:00")
        check(news(5,[a])==(1,0),"no player file: all new")
        json.dump({"Issue":5,"Facts":[a,b]},open(os.path.join(tmp,"5.json"),"w",encoding="utf-8"))
        check(news(5,[b,a])==(0,0),"same facts in another order: nothing new")
        check(news(5,[a,{**b,"LevelMax":3}])==(1,0) and news(5,[a])==(0,1),"changed and gone")
    PLAYERS=saved
    print(f"discoveries-merge selftest: {ok}/{total} ok");return 0 if ok==total else 1

def notice(argv):
    sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
    from bilingual import compose
    dry="--dry-run" in argv;argv=[a for a in argv if a!="--dry-run"]
    if len(argv)!=4 or argv[1] not in ("merged","flag") or not argv[0].isdigit():print(__doc__);return 2
    issue,what,a,b=argv
    if what=="merged":body=compose(f"Merged into {a}: {b} facts. Thank you! They reach every player with the next release.",
                                   f"Fusionné dans la {a} : {b} faits. Merci ! Ils arrivent chez tous les joueurs avec la prochaine release.")
    else:body=compose(f"Not merged: {a} of {b} facts were refused (over 5 %). Please update Tensho Assistant, export again and update the gist.",
                      f"Pas fusionné : {a} faits refusés sur {b} (plus de 5 %). Merci de mettre Tensho Assistant à jour, d'exporter à nouveau et de mettre le gist à jour.")
    if dry:print(body);return 0
    out=subprocess.run(["gh","issue","comment",issue,"-R","Shigu330/Tensho-Assistant","--body",body],capture_output=True,text=True)
    print(f"#{issue}: {'commented' if out.returncode==0 else 'comment failed'}");return out.returncode

if __name__=="__main__":
    if len(sys.argv)<2:print(__doc__);sys.exit(2)
    command=sys.argv[1]
    if command=="sync":sync("--dry-run" in sys.argv)
    elif command=="rebuild":rebuild()
    elif command=="check" and len(sys.argv)==3:print(clean(read(sys.argv[2]))[1])
    elif command=="notice":sys.exit(notice(sys.argv[2:]))
    elif command=="recheck":
        argv=sys.argv[2:];changes=argv[argv.index("--changes")+1] if "--changes" in argv else None
        print(recheck_report(changes,"--local" in argv,int(argv[argv.index("--limit")+1]) if "--limit" in argv else 40))
    elif command=="--selftest":sys.exit(selftest())
    else:print(__doc__);sys.exit(2)
