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

Rules mirror DiscoveryStore.Valid (src/TenshoAssistant/Discoveries/DiscoveriesModel.cs): a fact that does not hold is refused; any property the plugin does
not write is refused (the computed « Key » is accepted and dropped). In the common file each fact has Seen 0 and Imported = the number of players who have it;
levels and dates are the widest seen. Nothing personal can be in a fact by construction; the report flags a player whose refusals pass 5 %.
X-76 E1 (0.916, docs/evidence/0.916-discoveries-format.md): files of version 1 and 2 are read, the common file is written as version 2. Version 2 adds two
optional fields: Hq (how many sightings were HQ) and Values (value seen -> count, at most 32 values, ValueCounts in the plugin); the common file holds their
sums over the players (a new value past 32 is counted under the nearest value kept, the lower one on a tie). ImportedHq and ImportedValues (what the player
imported, never shared by the plugin) are accepted and dropped."""
import base64,collections,gzip,json,math,os,re,subprocess,sys,urllib.request,zipfile
from datetime import datetime,timezone,timedelta
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COMMON=os.path.join(ROOT,"community-discoveries","discoveries.json");PLAYERS=os.path.join(ROOT,"community-discoveries","players")
KINDS=set(range(1,32))-{26,27};PLACED={1,2,3,8,10,19,22,23,28,29,30}  # 26, 27: 0.890's and 0.891's wrong spearfishing shadows
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
def valid(f):
    try:
        f.pop("Key",None)  # computed by the plugin (DiscoveryFact.Key): written in its files, recomputed here
        f.pop("ImportedHq",None);f.pop("ImportedValues",None)  # 0.916: what the player imported, not theirs (an export holds it; the plugin never shares it)
        if set(f)-FIELDS or f["Kind"] not in KINDS or f["Subject"]<=0:return False
        if f["Kind"] in PLACED and f["Map"]==0:return False
        if f["Kind"]==3 and f["X"]==0 and f["Z"]==0:return False  # 0.911: a FATE at exactly (0, 0) is the game's zero position for about a second
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
        if not valid(f):refused+=1;continue
        g=dict(f);g["Seen"]=0;g["Imported"]=1;facts[key(g)]=g
    kinds=collections.Counter(f["Kind"] for f in facts.values())
    return list(facts.values()),{"facts":len(facts),"refused":refused,"kinds":dict(sorted(kinds.items()))}

def rebuild():
    merged={}
    for name in sorted(os.listdir(PLAYERS)) if os.path.isdir(PLAYERS) else []:
        if not name.endswith(".json"):continue
        for f in json.load(open(os.path.join(PLAYERS,name),encoding="utf-8"))["Facts"]:
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
    json.dump(common,open(COMMON,"w",encoding="utf-8",newline="\n"),ensure_ascii=False,separators=(",",":"))
    print(f"discoveries.json: {len(common['Facts'])} facts from {len(os.listdir(PLAYERS)) if os.path.isdir(PLAYERS) else 0} players")

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
        print(f"#{issue['number']} {issue['title']} (author {issue['author']['login']}): {report['facts']} facts, {report['refused']} refused, kinds {report['kinds']}{flag}")
        if dry or flag:continue
        json.dump({"Issue":issue["number"],"Facts":facts},open(os.path.join(PLAYERS,f"{issue['number']}.json"),"w",encoding="utf-8",newline="\n"),ensure_ascii=False,separators=(",",":"))
    if not dry:rebuild()

if __name__=="__main__":
    if len(sys.argv)<2:print(__doc__);sys.exit(2)
    command=sys.argv[1]
    if command=="sync":sync("--dry-run" in sys.argv)
    elif command=="rebuild":rebuild()
    elif command=="check" and len(sys.argv)==3:print(clean(read(sys.argv[2]))[1])
    else:print(__doc__);sys.exit(2)
