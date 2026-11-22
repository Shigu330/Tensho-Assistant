"""X-77 (0.882), 0.886: builds the common discoveries (community-discoveries/v3/) from what players shared (maintainer tool, run by hand).

Usage:
  python tools/discoveries-merge.py sync [--dry-run]
      Reads every open « Discoveries » issue through the GitHub CLI, fetches the gist it points to, keeps the player's valid facts
      in community-discoveries/players/<issue>.jsonl.gz (replacing that player's previous file: an updated gist is never counted twice), then rebuilds
      the common v3 set from all player files. --dry-run: report only, nothing written.
      X-168 (0.1146): GitHub drops the « discoveries » label of an issue opened by a player without rights on the repository, so issues are found
      by the label, or by their title « Discoveries: <login> », or by their content (« Shared from Tensho Assistant » with a gist link). Before
      anything is read, each one must hold one gist link only, the CC0-1.0 sharing ticked, a title naming its author (when it names one), and a gist
      owned by the issue's author; otherwise it is left for manual triage (MANUAL, never merged) with the notice command to tell the player.
      X-183 (0.1210): one issue per GitHub account: the newest one is read, an older one of the same author is reported and not read; a player
      file notes its author (« Login »); a player file whose issue is no longer an open share is reported with the withdraw command.
  python tools/discoveries-merge.py rebuild
      Rebuilds the common set community-discoveries/v3/ from the player files (community-discoveries/players/) and the common set as it is:
      since X-772 the places and days of the facts are in v3/ only (and in the gists sync reads). X-795: v3/index.json missing (while player
      files of the new format exist), an index listing fewer kind files than the folder holds, a set that does not read whole, or a fact a
      counted player holds that is in the common set of git HEAD but has no place now: STOP (exit 2), nothing written; restore v3/ from git first.
  python tools/discoveries-merge.py withdraw <issue> [--dry-run]
      X-183: removes players/<issue>.jsonl.gz, or the <issue>.json of before (a player who closed their issue or asked to be taken out), then
      rebuilds.
  python tools/discoveries-merge.py check <file.json|file.zip|v3 folder|gist:ID>
      Validates one file (or a v3 set) and prints its report, without writing.
  python tools/discoveries-merge.py recheck [--changes FILE] [--local] [--limit N]
      X-116: the facts « to recheck » after a game patch: seen last before the patch day, in a zone or a 50-yalm cell (or next to one) that the
      client comparison says changed (tools/client-snapshot.py patch writes changes-from-<old>.json; the latest one by default), or about an NPC
      or FATE the patch moved or removed. Read-only; --local adds this PC's own Discoveries.json. `sync --dry-run` ends with it.
  python tools/discoveries-merge.py notice <issue> merged <version> <facts> [--dry-run]
  python tools/discoveries-merge.py notice <issue> flag <refused> <total> [--dry-run]
  python tools/discoveries-merge.py notice <issue> manual <gist|cc0|title|owner> [--dry-run]
      Comments the ticket in English then French (tools/bilingual.py): facts merged into a version, a share not merged because over 5 % of
      its facts were refused, or (X-168) a share left for manual triage and why. --dry-run: prints the comment only.

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
and it gets no « Merged into » comment; the common set is written only when its facts change (not for a new date alone).
X-174 (0.1137, DEC-052): exchange format v3. A set is index.json and one kind-NN.jsonl.gz per kind (gzip of UTF-8 lines: a header line, then one
fact per line), each listed in the index with its fact count, compressed size and SHA-256; read bounded (compressed and decompressed size, line
length, facts per kind) and checked against the index's hash first; a bad line is counted as refused. A player's gist is read in v3 (index.json and
kind-NN.jsonl.gz.b64, base64 of the gzip bytes) or in v2 (the single tensho-discoveries.b64, still read for a while). The player files stay v2 JSON
(players/<issue>.json); rebuild writes the v3 set community-discoveries/v3/, the same facts giving the same bytes (gzip mtime 0, facts and
fields in a fixed order, dated by their newest day). X-357 (0.1349, DEC-052): the v2 common file discoveries.json (plugins before 0.1137) is no
longer written: the plugin reads v3 only; rebuild compares to the v3 set (a file written only when its bytes change).
X-183 (0.1210, night review of 12/11, N5, N12, N13, N16, N17): each field has the plugin's type and bounds (a gist can no longer get a whole kind refused
by the plugin's reader); a share's facts are its sightings (Seen from 1 to 100 000, at least the values counted) and no date before 2026-11-01; each
player counts for at most 1 000 sightings of a fact in the common histograms; the exact place is the earliest day's; a player alone far outside the
others' levels on over 5 % of the facts they share is left out of rebuild (FLAG); one file per GitHub account; every file written whole or not at all;
issue titles printed without control or format characters.
X-146 (0.1214, audit A10): every remote call (gh, a gist file) has a deadline of 60 s, a slow trickle included; an issue whose gist cannot be read
in time is reported unreadable and skipped, the batch goes on.
X-772 (0.1801, D-217, R12.2): a player file no longer keeps any exact place (X, Z) nor day (First, Last): beside the « Discoveries: <login> »
issue, they said where that GitHub account was on that day (a fish is noted at the character's exact place). It keeps what the merge needs
(PLAYER_ORDER: the fact's identity, its levels, its HQ sightings and values), compressed like the common files: players/<issue>.jsonl.gz, gzip
(mtime 0) of UTF-8 lines, a header {"Format":"tensho-discoveries-player","Version":1,"Issue":n,"Login":…,"Count":n} then one fact per line.
The place and days of a common fact (the earliest day's exact place, the latest day seen) come from the common set as it is (v3/), from a
player file of before (<issue>.json, still read, written again by rebuild in the new format) and from the gists read by sync; on the same
first day, the first met keeps its place. A fact whose place is in none of them waits in its player file (said) until a share brings it.
Withdrawing a player removes their facts that no other player has; a fact others have keeps the place and days of the common set."""
import base64,collections,gzip,hashlib,importlib.util,io,json,math,os,re,struct,subprocess,sys,tempfile,time,unicodedata,urllib.request,zipfile
from datetime import datetime,timezone,timedelta
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PLAYERS=os.path.join(ROOT,"community-discoveries","players")
V3_DIR=os.path.join(ROOT,"community-discoveries","v3")
QUEST_ASKED_FIXED=datetime(2026,11,8,15,42,29,tzinfo=timezone.utc)
QUEST_ASKED_FIXED_DAY=datetime(2026,11,9,tzinfo=timezone.utc)  # 0.1012: a date kept to the day only passes the fix if it is on a later day
KINDS=set(range(1,39))-{26,27};PLACED={1,2,3,8,10,19,22,23,28,29,30}  # 26, 27: 0.890's and 0.891's wrong spearfishing shadows
CELL=50;MAX_FACTS=2_000_000;VERSIONS={1,2,3};VERSION=2  # X-468 (0.1805, D-216 a): 200 000 raised to a high safety bound
FIELDS={"Kind","Subject","Detail","Territory","Map","CellX","CellZ","X","Z","LevelMin","LevelMax","Seen","Imported","First","Last","Hq","Values"}
MAX_VALUES=32;MAX_VALUE=100_000;MAX_COUNT=100_000_000
# X-183 (N12): the plugin's types (DiscoveryFact: uint ids, int cells, levels and counts). (N5): no sighting before the plugin noted any (0.807, 2026-11-07);
# a share's sightings of one fact are at most MAX_SEEN; each player counts for at most PLAYER_SIGHTINGS sightings of a fact in the common histograms
# (the shape of theirs kept); a player alone far outside the others' levels on over 5 % of the facts they share (FAR_FACTS at least) is left out.
UINT=2**32-1;INT=2**31-1;FIRST_DAY=datetime(2026,11,1,tzinfo=timezone.utc)
# X-146 (0.1214, audit A10): each remote call (gh, a gist file) stops past REMOTE_TIMEOUT seconds, a slow trickle included; the issue is then
# reported unreadable and skipped, the batch goes on.
REMOTE_TIMEOUT=60
MAX_SEEN=100_000;PLAYER_SIGHTINGS=1_000;FAR_FACTS=3
# X-174: the v3 format and its bounds (ExchangeV3 in the plugin).
V3_FORMAT="tensho-discoveries";V3_VERSION=3;V3_INDEX="index.json";GIST_V2="tensho-discoveries.b64";GIST_SUFFIX=".b64"
MAX_COMPRESSED=32*1024*1024;MAX_DECOMPRESSED=256*1024*1024;MAX_LINE=64*1024;MAX_INDEX=1024*1024
# X-807 M2 (review of 0.1806): the producer written in every kind file of the common set, also the one fit_bytes measures with (a shorter one
# measured a file that the write then made larger than the bound).
PRODUCER="Tensho Assistant community"
# X-807 M3: the plugins up to 0.1804 refuse the whole index once one entry counts more facts than this (their MaxFacts); said when reached.
OLD_PLUGIN_FACTS=200_000
ORDER=["Kind","Subject","Detail","Territory","Map","CellX","CellZ","X","Z","LevelMin","LevelMax","Seen","Imported","First","Last","Hq","Values"]
# X-772 (0.1801, D-217): a player file keeps only what the merge needs, compressed like the common files: no exact place nor day.
PLAYER_FORMAT="tensho-discoveries-player";PLAYER_VERSION=1;PLAYER_SUFFIX=".jsonl.gz"
PLAYER_ORDER=["Kind","Subject","Detail","Territory","Map","CellX","CellZ","LevelMin","LevelMax","Hq","Values"]
PLACE=("X","Z","First","Last")

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
def number(n):return isinstance(n,(int,float)) and not isinstance(n,bool)
def f32(x):return struct.unpack("<f",struct.pack("<f",x))[0]
def cell_of(x,z):
    """DiscoveryStore.Cell: the 50-yalm cell computed in single precision, as the plugin does (X-183, N12)."""
    return math.floor(f32(f32(x)/CELL)),math.floor(f32(f32(z)/CELL))
def typed(f):
    """X-183 (N12): each field of the plugin's type (DiscoveryFact): a fact accepted here is never one the plugin's reader refuses."""
    return (whole(f["Kind"]) and all(whole(f[n]) and 0<=f[n]<=UINT for n in ("Subject","Detail","Territory","Map"))
        and all(whole(f[n]) and -INT-1<=f[n]<=INT for n in ("CellX","CellZ")) and number(f["X"]) and number(f["Z"])
        and all(whole(f[n]) for n in ("LevelMin","LevelMax")) and all(whole(f.get(n,0)) and 0<=f.get(n,0)<=INT for n in ("Seen","Imported"))
        and isinstance(f["First"],str) and isinstance(f["Last"],str))
def valid(f):
    try:
        f.pop("Key",None)  # computed by the plugin (DiscoveryFact.Key): written in its files, recomputed here
        f.pop("ImportedHq",None);f.pop("ImportedValues",None)  # 0.916: what the player imported, not theirs (an export holds it; the plugin never shares it)
        if set(f)-FIELDS or not typed(f) or f["Kind"] not in KINDS or f["Subject"]<=0:return False
        if f["Kind"] in PLACED and f["Map"]==0:return False
        if f["Kind"]==3 and f["X"]==0 and f["Z"]==0:return False  # 0.911: a FATE at exactly (0, 0) is the game's zero position for about a second
        if f["Kind"]==25 and when(f["Last"])<(QUEST_ASKED_FIXED_DAY if day(f["Last"])==f["Last"] else QUEST_ASKED_FIXED):return False  # 0.1005: QuestItemAsks.Fixed, a quest item noted before 0.911's fix
        if not all(math.isfinite(f[n]) and abs(f[n])<=10000 for n in ("X","Z")):return False
        if cell_of(f["X"],f["Z"])!=(f["CellX"],f["CellZ"]):return False
        if not (0<=f["LevelMin"]<=100000 and 0<=f["LevelMax"]<=100000):return False
        if f["LevelMax"]!=0 and f["LevelMin"]>f["LevelMax"]:return False  # X-185 (N25): DiscoveryStore.Valid, a highest level of 0 being « unknown »
        # 0.916 (version 2): the histogram in its bounds, never more HQ sightings than sightings counted in it.
        if "Values" in f and not histogram_valid(f["Values"]):return False
        if "Hq" in f and not (whole(f["Hq"]) and 0<=f["Hq"]<=sum(f.get("Values",{}).values())):return False
        return FIRST_DAY<=when(f["First"])<=when(f["Last"])<=datetime.now(timezone.utc)+timedelta(days=1)
    except Exception:return False

def sighted(f):
    """X-183 (N5): a fact of a player's share is a sighting of theirs: Seen a whole count from 1 to MAX_SEEN, at least the values counted in it."""
    return whole(f.get("Seen")) and 1<=f["Seen"]<=MAX_SEEN and sum((f.get("Values") or {}).values())<=f["Seen"]

def capped(f):
    """X-183 (N5): one player's histogram of a fact brought to at most PLAYER_SIGHTINGS sightings, its shape kept (each value kept at 1 at least) and
    its HQ sightings in the same proportion; a histogram already within the bound is left as it is (the same file gives the same facts)."""
    values=f.get("Values") or {};total=sum(values.values())
    if total<=PLAYER_SIGHTINGS:return f
    target=PLAYER_SIGHTINGS-MAX_VALUES  # at most MAX_VALUES values raised to 1: the result stays within the bound
    f["Values"]={k:max(1,c*target//total) for k,c in values.items()}
    if "Hq" in f:f["Hq"]=min(f["Hq"]*target//total,sum(f["Values"].values()))
    return f

# ---------- X-174: exchange format v3 ----------
def sort_key(f):return (f["Kind"],f["Subject"],f["Detail"],f["Territory"],f["Map"],f["CellX"],f["CellZ"])
def v3_name(kind):return f"kind-{kind:02d}.jsonl.gz"
def newest(facts):return max((f["Last"] for f in facts),key=when,default="1970-01-01T00:00:00+00:00")
def v3_line(obj):return json.dumps(obj,ensure_ascii=False,separators=(",",":"))

def v3_kind_bytes(kind,facts,producer):
    """One kind file: the header line, then each fact (fields in ORDER) on its line, gzip with mtime 0: the same facts give the same bytes."""
    facts=sorted(facts,key=sort_key)
    header={"Format":V3_FORMAT,"Version":V3_VERSION,"Kind":kind,"Count":len(facts),"Producer":producer,"Written":newest(facts)}
    text="".join(v3_line(o)+"\n" for o in [header]+[{k:f[k] for k in ORDER if k in f} for f in facts])
    return gzip.compress(text.encode("utf-8"),compresslevel=9,mtime=0)

def weight(f):return (-f["Imported"],-when(f["Last"]).timestamp(),sort_key(f))

def bounded(facts,limit=None,say=print,max_compressed=None,max_decompressed=None):
    """X-406: each kind kept to `limit` facts (MAX_FACTS: the plugin's DiscoveryStore.MaxFacts, the bound of an index entry). Over it, the
    facts the most players saw are kept (Imported), then the most recent (Last), then the fixed order; the others are said, never written.
    X-468 (0.1805, D-216 a): MAX_FACTS raised to 2 000 000; a kind's file is also kept within the format's byte bounds (MAX_COMPRESSED,
    MAX_DECOMPRESSED: its index entry, or the plugin's reader, would refuse it), by the same order."""
    limit=MAX_FACTS if limit is None else limit
    max_compressed=MAX_COMPRESSED if max_compressed is None else max_compressed
    max_decompressed=MAX_DECOMPRESSED if max_decompressed is None else max_decompressed
    by=collections.defaultdict(list)
    for f in facts:by[f["Kind"]].append(f)
    kept=[];cut=False
    for kind in sorted(by):
        mine=by[kind]
        if len(mine)>limit:
            mine=sorted(mine,key=lambda f:(-f["Imported"],-when(f["Last"]).timestamp(),sort_key(f)))[:limit];cut=True
            say(f"kind {kind}: {len(by[kind])} facts over the bound of {limit}: {len(by[kind])-limit} left out (the least seen, then the oldest)")
        mine=fit_bytes(kind,mine,max_compressed,max_decompressed,say)
        if len(mine)>OLD_PLUGIN_FACTS:
            say(f"WARN kind {kind}: {len(mine)} facts, over {OLD_PLUGIN_FACTS}: Tensho 0.1804 and older refuse the whole common set (every kind), "
                "not only this one (X-807 M3)")
        cut=cut or len(mine)<len(by[kind])
        kept.extend(mine)
    return sorted(kept,key=sort_key) if cut else facts

def fit_bytes(kind,facts,max_compressed,max_decompressed,say=print):
    """X-468: the facts of one kind whose file stays within the byte bounds; over them, the least seen then the oldest left out (said)."""
    header=1024   # the header line, with room
    sizes=[len(v3_line({k:f[k] for k in ORDER if k in f}).encode("utf-8"))+1 for f in facts]
    if sum(sizes)+header<=min(max_decompressed,max_compressed-header):return facts   # compressed never larger than this: nothing to measure
    ordered=sorted(range(len(facts)),key=lambda n:weight(facts[n]))
    total=header;count=0
    for n in ordered:
        if total+sizes[n]>max_decompressed:break
        total+=sizes[n];count+=1
    keep=[facts[n] for n in ordered[:count]]
    while keep and len(v3_kind_bytes(kind,keep,PRODUCER))>max_compressed:
        keep=keep[:max(0,min(len(keep)-1,int(len(keep)*max_compressed/len(v3_kind_bytes(kind,keep,PRODUCER))*0.98)))]
    if len(keep)<len(facts):
        say(f"kind {kind}: {len(facts)} facts over the format's byte bounds: {len(facts)-len(keep)} left out (the least seen, then the oldest)")
        return keep
    return facts

def v3_build(facts,producer):
    """(index.json text, {name: gzip bytes}) of a v3 set. X-406: a kind over MAX_FACTS is refused (its index entry would refuse the whole set).
    X-807 M2: so is a kind file over MAX_COMPRESSED (v3_index and the plugin's ParseIndex refuse the whole index for one such entry)."""
    by=collections.defaultdict(list)
    for f in facts:by[f["Kind"]].append(f)
    over={k:len(v) for k,v in by.items() if len(v)>MAX_FACTS}
    if over:raise ValueError(f"kinds over the bound of {MAX_FACTS} facts: {over} (bounded() first)")
    files={};entries=[]
    for kind in sorted(by):
        data=v3_kind_bytes(kind,by[kind],producer);files[v3_name(kind)]=data
        if len(data)>MAX_COMPRESSED:raise ValueError(f"kind {kind}: {len(data)} compressed bytes, over the bound of {MAX_COMPRESSED} (bounded() first)")
        entries.append({"Name":v3_name(kind),"Kind":kind,"Count":len(by[kind]),"Bytes":len(data),"Sha256":hashlib.sha256(data).hexdigest().upper()})
    index={"Format":V3_FORMAT,"Version":V3_VERSION,"Written":newest(facts),"Files":entries}
    return json.dumps(index,ensure_ascii=False,indent=1)+"\n",files

def v3_index(text):
    """The entries of an index (ExchangeV3.ParseIndex): another format or version, or a malformed entry, refuses the set."""
    if len(text.encode("utf-8"))>MAX_INDEX:raise ValueError("index too large")
    index=json.loads(text)
    if not isinstance(index,dict) or index.get("Format")!=V3_FORMAT or index.get("Version")!=V3_VERSION:raise ValueError("unknown index format or version")
    seen=set();out=[]
    for e in index.get("Files") or []:
        if not (isinstance(e,dict) and whole(e.get("Kind")) and e["Kind"] not in seen and e.get("Name")==v3_name(e["Kind"])
                and whole(e.get("Count")) and 0<=e["Count"]<=MAX_FACTS and whole(e.get("Bytes")) and 1<=e["Bytes"]<=MAX_COMPRESSED
                and isinstance(e.get("Sha256"),str) and re.fullmatch(r"[0-9A-Fa-f]{64}",e["Sha256"])):raise ValueError("malformed index entry")
        seen.add(e["Kind"]);out.append(e)
    return out

def v3_kind(data,entry,max_decompressed=MAX_DECOMPRESSED,max_line=MAX_LINE):
    """(facts as read, unreadable lines) of one kind file: size and SHA-256 checked against the index first, then decompressed line by line, bounded.
    A line that is not a JSON object of the file's kind is counted unreadable (its fields are checked later by clean); over 5 % refuses the file."""
    if len(data)!=entry["Bytes"] or len(data)>MAX_COMPRESSED:raise ValueError("size differs from the index")
    if hashlib.sha256(data).hexdigest().upper()!=entry["Sha256"].upper():raise ValueError("hash differs from the index")
    facts=[];bad=0;header=None;lines=0;total=0
    with gzip.GzipFile(fileobj=io.BytesIO(data)) as z:
        while True:
            raw=z.readline(max_line+1)
            if not raw:break
            total+=len(raw)
            too_long=len(raw)>max_line and not raw.endswith(b"\n")  # a line longer than max_line: skipped to its end, counted
            while too_long and not raw.endswith(b"\n"):
                raw=z.readline(max_line+1)
                if not raw:break
                total+=len(raw)
                if total>max_decompressed:raise ValueError("too large once decompressed")
            if total>max_decompressed:raise ValueError("too large once decompressed")
            line=None if too_long else raw.rstrip(b"\n")
            try:obj=json.loads(line.decode("utf-8")) if line is not None else None
            except ValueError:obj=None
            if header is None:
                if not isinstance(obj,dict) or obj.get("Format")!=V3_FORMAT:raise ValueError("no header")
                if obj.get("Version")!=V3_VERSION:raise ValueError("unknown version")
                if obj.get("Kind")!=entry["Kind"] or obj.get("Count")!=entry["Count"]:raise ValueError("header of another kind or count than the index")
                header=obj;continue
            lines+=1
            if lines>header["Count"]:raise ValueError("more facts than the header says")
            if not isinstance(obj,dict) or obj.get("Kind")!=entry["Kind"]:bad+=1;continue
            facts.append(obj)
    if header is None:raise ValueError("no header")
    if lines!=header["Count"]:raise ValueError("fewer facts than the header says")
    if bad*20>lines:raise ValueError("over 5 % of the lines unreadable")
    return facts,bad

def v3_read(fetch_index,fetch_kind):
    """A v3 set as a file to clean: {"Version":3,"Facts":[…],"Unreadable":n}."""
    facts=[];bad=0
    for e in v3_index(fetch_index()):
        got,unreadable=v3_kind(fetch_kind(e),e);facts+=got;bad+=unreadable
    return {"Version":3,"Facts":facts,"Unreadable":bad}

def from_v3_dir(path):
    """A v3 set in a folder (community-discoveries/v3, or a gist's files saved): kind-NN.jsonl.gz, or kind-NN.jsonl.gz.b64."""
    def kind_file(e):
        p=os.path.join(path,e["Name"])
        if os.path.exists(p):
            if os.path.getsize(p)>MAX_COMPRESSED:raise ValueError("too large")
            return open(p,"rb").read()
        return base64.b64decode(open(p+GIST_SUFFIX,"rb").read(MAX_COMPRESSED*2).strip(),validate=True)
    return v3_read(lambda:open(os.path.join(path,V3_INDEX),"rb").read(MAX_INDEX+1).decode("utf-8"),kind_file)

def gunzip_bounded(data,limit):
    with gzip.GzipFile(fileobj=io.BytesIO(data)) as z:out=z.read(limit+1)
    if len(out)>limit:raise ValueError("too large once decompressed")
    return out

def from_gist_files(files,fetch):
    """A gist's files ({name: {"raw_url": …}}), fetched by fetch(url, limit) -> bytes: v3 (index.json and the kind files in base64) or v2 (one file)."""
    if V3_INDEX in files:
        def kind_file(e):
            name=e["Name"]+GIST_SUFFIX
            if name not in files:raise ValueError(f"{name} missing from the gist")
            return base64.b64decode(fetch(files[name]["raw_url"],(MAX_COMPRESSED+2)//3*4+64).strip(),validate=True)
        return v3_read(lambda:fetch(files[V3_INDEX]["raw_url"],MAX_INDEX).decode("utf-8"),kind_file)
    if GIST_V2 in files:  # v2, still read for a while: base64 of gzip of JSON
        return json.loads(gunzip_bounded(base64.b64decode(fetch(files[GIST_V2]["raw_url"],64*1024*1024).strip()),64*1024*1024).decode("utf-8"))
    raise ValueError("no discoveries file in the gist")

def fetch_gist_raw(url,limit,deadline=None,opener=None):
    """A gist file's bytes, at most limit (over it: refused), within deadline seconds in all (X-146: a server that sends a byte at a time is
    stopped too), read by chunks of 1 MB."""
    if not url.startswith("https://gist.githubusercontent.com/"):raise ValueError("unexpected gist file address")
    deadline=REMOTE_TIMEOUT if deadline is None else deadline;end=time.monotonic()+deadline;data=bytearray()
    with (opener or urllib.request.urlopen)(url,timeout=deadline) as answer:
        while len(data)<=limit:
            chunk=answer.read(min(1<<20,limit+1-len(data)))
            if not chunk:break
            data+=chunk
            if time.monotonic()>end:raise TimeoutError("the gist file came too slowly")
    if len(data)>limit:raise ValueError("too large")
    return bytes(data)

def gist_listing(gist):
    """X-168: a gist's owner login and files ({name: {"raw_url": …}}), through the GitHub CLI; nothing of its content is read here."""
    listed=json.loads(subprocess.run(["gh","api",f"gists/{gist}","--jq","{owner:(.owner.login // \"\"),files:(.files|map_values({raw_url:.raw_url}))}"],
        capture_output=True,text=True,check=True,timeout=REMOTE_TIMEOUT).stdout)
    return listed.get("owner") or "",listed.get("files") or {}

def from_gist(reference):
    """A gist id or URL: its files listed through the GitHub CLI, then read in v3 or v2 (from_gist_files)."""
    gist=reference.removeprefix("gist:").rstrip("/").split("/")[-1]
    return from_gist_files(gist_listing(gist)[1],fetch_gist_raw)

def read(path):
    if path.startswith("gist:") or "gist.github.com" in path:return from_gist(path)
    if os.path.isdir(path):return from_v3_dir(path)
    if path.endswith(".zip"):
        with zipfile.ZipFile(path) as z:
            name=next(n for n in z.namelist() if n.endswith(".json"))
            if z.getinfo(name).file_size>64*1024*1024:raise ValueError("too large")
            return json.loads(z.read(name).decode("utf-8"))
    return json.load(open(path,encoding="utf-8"))

def clean(data,player=False):
    """The valid facts of a shared file (Seen 0, Imported 1) and a report. X-183 (N5): player=True for a player's share (sync): each fact must be a
    sighting of theirs (sighted) before Seen is set to 0, and its histogram is capped (capped)."""
    if data.get("Version") not in VERSIONS:return [],{"facts":0,"refused":0,"kinds":{},"error":"version"}
    facts={};refused=data.get("Unreadable",0) if data.get("Version")==3 else 0  # X-174: v3 lines that were not a fact of their file's kind
    for f in data.get("Facts",[])[:MAX_FACTS*len(KINDS) if data.get("Version")==3 else MAX_FACTS]:
        # 0.1012: checked on the time as sent, then kept to the day (an older gist with times is accepted and brought to the day, not refused).
        if not isinstance(f,dict) or not valid(f) or not valid(daily(dict(f))) or player and not sighted(f):refused+=1;continue
        g=capped(daily(dict(f)));g["Seen"]=0;g["Imported"]=1;facts[key(g)]=g
    kinds=collections.Counter(f["Kind"] for f in facts.values())
    return list(facts.values()),{"facts":len(facts),"refused":refused,"kinds":dict(sorted(kinds.items()))}

def write_atomic(path,data):
    """X-183 (N16): a file replaced whole or not at all: written to a temporary file beside it, flushed to the disk, then renamed over it."""
    folder=os.path.dirname(path) or ".";handle,temporary=tempfile.mkstemp(prefix="."+os.path.basename(path)+".",suffix=".tmp",dir=folder)
    try:
        with os.fdopen(handle,"wb") as out:
            out.write(data.encode("utf-8") if isinstance(data,str) else data);out.flush();os.fsync(out.fileno())
        os.replace(temporary,path)
    except BaseException:
        try:os.remove(temporary)
        except OSError:pass
        raise
def write_json(path,obj):write_atomic(path,json.dumps(obj,ensure_ascii=False,separators=(",",":")))

# ---------- X-772 (0.1801, D-217): the player files without places nor days ----------
def stem(name):
    """The issue part of a player file's name (« 43 » of 43.jsonl.gz or 43.json)."""
    return name[:-len(PLAYER_SUFFIX)] if name.endswith(PLAYER_SUFFIX) else name[:-5] if name.endswith(".json") else name

def without_place(f):
    """A fact as a player file keeps it: PLAYER_ORDER only (no X, Z, First, Last; Seen 0 and Imported 1 are the same for every share)."""
    return {k:f[k] for k in PLAYER_ORDER if k in f}

def player_bytes(issue,author,facts):
    """A player file: gzip (mtime 0) of UTF-8 lines, a header then one fact per line (without_place, sorted): the same facts give the same bytes."""
    facts=sorted((without_place(f) for f in facts),key=sort_key)
    header={"Format":PLAYER_FORMAT,"Version":PLAYER_VERSION,"Issue":issue,**({"Login":author} if author else {}),"Count":len(facts)}
    return gzip.compress("".join(v3_line(o)+"\n" for o in [header]+facts).encode("utf-8"),compresslevel=9,mtime=0)

def read_player(path):
    """A player file: {"Issue","Login","Facts","Old"}; Old: an <issue>.json of before 0.1801 (v2 JSON, its facts with place and days). A file that
    does not read whole raises (a rebuild then stops before writing anything)."""
    if not path.endswith(PLAYER_SUFFIX):
        player=json.load(open(path,encoding="utf-8"))
        return {"Issue":player.get("Issue"),"Login":player.get("Login"),"Facts":player.get("Facts") or [],"Old":True}
    if os.path.getsize(path)>MAX_COMPRESSED:raise ValueError(f"{os.path.basename(path)}: too large")
    lines=gunzip_bounded(open(path,"rb").read(),MAX_DECOMPRESSED).decode("utf-8").splitlines()
    header=json.loads(lines[0]) if lines else None
    if not isinstance(header,dict) or header.get("Format")!=PLAYER_FORMAT or header.get("Version")!=PLAYER_VERSION:
        raise ValueError(f"{os.path.basename(path)}: not a player file of version {PLAYER_VERSION}")
    facts=[json.loads(line) for line in lines[1:] if line]
    if header.get("Count")!=len(facts) or not all(isinstance(f,dict) and not set(f)-set(PLAYER_ORDER) for f in facts):
        raise ValueError(f"{os.path.basename(path)}: its facts differ from its header or hold a field it never keeps")
    return {"Issue":header.get("Issue"),"Login":header.get("Login"),"Facts":facts,"Old":False}

def write_player(issue,author,facts):
    """The player's file written whole (<issue>.jsonl.gz), then the <issue>.json of before removed."""
    write_atomic(os.path.join(PLAYERS,f"{issue}{PLAYER_SUFFIX}"),player_bytes(int(issue) if str(issue).isdigit() else issue,author,facts))
    old=os.path.join(PLAYERS,f"{issue}.json")
    if os.path.exists(old):os.remove(old)

def player_of(issue):
    """The player file of an issue (the new one, else the one of before), read; None without one."""
    for name in (f"{issue}{PLAYER_SUFFIX}",f"{issue}.json"):
        path=os.path.join(PLAYERS,name)
        if os.path.isfile(path):return read_player(path)
    return None

def name_order(n):return (0,int(stem(n)),n) if stem(n).isdigit() else (1,0,n)

def player_names():
    """The player files, in issue order (X-183: « 43 » before « 100 »); X-772: <issue>.jsonl.gz, and the <issue>.json of before."""
    names=[n for n in os.listdir(PLAYERS) if (n.endswith(".json") or n.endswith(PLAYER_SUFFIX)) and not n.startswith(".")] if os.path.isdir(PLAYERS) else []
    return sorted(names,key=name_order)

def far(values,mine,lowest):
    """X-183 (N5): one player's level far outside what all the others saw: more than twice their widest plus 10."""
    return mine<min(values) and min(values)>2*mine+10 if lowest else mine>2*max(values)+10

def outliers(held):
    """X-183 (N5): {player file: (facts where it alone is far outside the others' levels, facts it shares with others)}."""
    alone=collections.Counter();shared=collections.Counter()
    for entries in held.values():
        if len(entries)<2:continue
        for name,f in entries:
            others=[g for n,g in entries if n!=name]
            if not others:continue
            shared[name]+=1
            low=[g["LevelMin"] for g in others if g["LevelMin"]>0]
            if f["LevelMax"]>0 and far([g["LevelMax"] for g in others],f["LevelMax"],False) or f["LevelMin"]>0 and low and far(low,f["LevelMin"],True):alone[name]+=1
    return {name:(alone[name],shared[name]) for name in shared}

class Lossy(ValueError):
    """X-795: a rebuild that would lose places or days (the common set it reads is missing or incomplete): stopped before anything is written."""

def v3_guard(folder=None):
    """X-795 (review of 0.1802, I1): since X-772 the place and days of a fact live in the common set only (v3/), beside the gists read by sync and
    the player files of before. A rebuild that reads no v3/index.json, or an index listing fewer files than the folder holds, would write a common
    set without those places and remove the kind files: refused here, before anything is written. → the v3 set as read (None without an index,
    when no player file of the new format exists: a first build, from the player files of before)."""
    folder=folder or V3_DIR
    index=os.path.join(folder,V3_INDEX)
    renewed=[n for n in player_names() if n.endswith(PLAYER_SUFFIX)]
    if not os.path.isfile(index):
        if renewed:raise Lossy(f"{os.path.relpath(index,ROOT)} is missing while {len(renewed)} player file(s) of the new format exist "
                               f"({', '.join(renewed[:3])}): their facts' places and days are only in the common set; restore community-discoveries/v3 "
                               "from git history first")
        return None
    try:entries=v3_index(open(index,"rb").read(MAX_INDEX+1).decode("utf-8"))
    except (OSError,ValueError,UnicodeDecodeError) as error:raise Lossy(f"{os.path.relpath(index,ROOT)} unreadable ({error}): restore community-discoveries/v3 from git history first")
    listed={e["Name"] for e in entries}
    unlisted=sorted(n for n in os.listdir(folder) if re.fullmatch(r"kind-\d+\.jsonl\.gz(\.b64)?",n) and n.split(".b64")[0] not in listed)
    if unlisted:raise Lossy(f"the index lists {len(listed)} kind file(s) and the folder holds {', '.join(unlisted[:5])} besides: an index cut short; "
                            "restore community-discoveries/v3 from git history first")
    try:return from_v3_dir(folder)
    except (OSError,ValueError) as error:raise Lossy(f"the common set does not read whole ({type(error).__name__}: {error}): restore community-discoveries/v3 from git history first")

GIT_TIMEOUT=60   # a local git call (the remote calls' deadline is REMOTE_TIMEOUT)
def git_bytes(rel):
    """A file of git HEAD (bytes), or None (no git, no such file)."""
    try:out=subprocess.run(["git","-C",ROOT,"show",f"HEAD:{rel}"],capture_output=True,timeout=GIT_TIMEOUT)
    except (OSError,subprocess.SubprocessError):return None
    return out.stdout if out.returncode==0 else None

def head_v3_keys():
    """X-795: the keys of the common set of git HEAD (the set last committed), or None when it cannot be read (another folder than the
    repository's, no git, no set committed yet)."""
    if os.path.normcase(os.path.abspath(V3_DIR))!=os.path.normcase(os.path.join(ROOT,"community-discoveries","v3")):return None
    rel="community-discoveries/v3/"
    index=git_bytes(rel+V3_INDEX)
    if index is None:return None
    try:
        got=v3_read(lambda:index.decode("utf-8"),lambda e:git_bytes(rel+e["Name"]) or b"")
        return {key(f) for f in got["Facts"] if isinstance(f,dict) and all(n in f for n in ("Kind","Subject","Detail","Territory","Map","CellX","CellZ"))}
    except (ValueError,UnicodeDecodeError):return None

def rebuild(say=print,fresh=(),skip=(),history=None,pending=None):
    # X-772 (0.1801, D-217): the place (X, Z) and days (First, Last) of a fact are no longer in the player files: per fact, the earliest day's
    # exact place and the latest day seen, from the common set as it is (v3/), from the player files of before (each written again here without
    # them) and from the gists just read (fresh, sync); on the same first day, the first met keeps its place.
    # X-795: the common set read whole first (v3_guard), and nothing written when a fact a counted player holds is in the common set of git HEAD
    # but has no place now (Lossy). X-796 M1: the places and days of a player left out (FLAG, or an older issue of the same account) never count.
    # fresh: facts, or (player file name, fact) pairs (sync); skip: player file names read as if removed (withdraw removes them after the write).
    # X-807 M1 (review of 0.1806): pending: {player file name: (issue, login, facts)}, the player files sync has read from the gists: taken here
    # instead of the disk, and written in `later` like every other player file, once the common set is written. Before, sync wrote them before
    # rebuild, whose stop then left them rewritten beside a common set that was not.
    pending=dict(pending or {})
    current=v3_guard()
    sources=[(None,f) for f in clean(current)[0]] if current is not None else []
    names=[n for n in player_names() if n not in set(skip)]
    names=sorted(names+[n for n in pending if n not in names],key=name_order);renewed={stem(n) for n in names if n.endswith(PLAYER_SUFFIX)}
    files=[];later=[]   # later: the player files' writes, done once the common set is written (a stop before it leaves every file as it was)
    for name in names:
        path=os.path.join(PLAYERS,name);issue=stem(name)
        if name in pending:
            _,author,given=pending[name]
            player={"Issue":issue,"Login":author,"Facts":[without_place(f) for f in given],"Old":False}
            later.append(lambda issue=issue,author=author,given=given:write_player(issue,author,given))
        else:player=read_player(path)
        if player["Old"]:
            # 0.1005, 0.1012: a file of before follows the current rules and keeps the day only; X-772: its places and days kept for the
            # common set, then the file written again without them (or, beside its new file after a conversion cut short, removed).
            kept=[f for f in player["Facts"] if valid(dict(f))]
            kept=[daily(f) for f in kept]
            sources+=[(issue+PLAYER_SUFFIX,f) for f in kept]
            if issue in renewed:
                later.append(lambda path=path,name=name,issue=issue:(os.path.exists(path) and os.remove(path),say(f"{name}: removed (its {issue}{PLAYER_SUFFIX} is there: a conversion cut short)")))
                continue
            if len(kept)<len(player["Facts"]):say(f"{name}: {len(player['Facts'])-len(kept)} facts refused by the current rules")
            later.append(lambda issue=issue,player=player,kept=kept,name=name:(write_player(issue,player.get("Login"),kept),
                         say(f"{name}: written again as {issue}{PLAYER_SUFFIX}, without exact places nor days (X-772)")))
        files.append((issue+PLAYER_SUFFIX,issue,(player.get("Login") or "").lower(),player.get("Login"),
                      [without_place(f) for f in kept] if player["Old"] else player["Facts"]))
    sources+=[pair if isinstance(pair,tuple) else (None,pair) for pair in fresh]
    def places_of(excluded):
        places={}
        for owner,f in sources:
            if owner in excluded:continue
            known=places.get(key(f))
            if known is None:places[key(f)]={n:f[n] for n in PLACE};continue
            if when(f["First"])<when(known["First"]):known["First"]=f["First"];known["X"]=f["X"];known["Z"]=f["Z"]
            if when(f["Last"])>when(known["Last"]):known["Last"]=f["Last"]
        return places
    # Each player's facts with their fact's place and days; refused by the current rules: the file written again without them; with no
    # place known anywhere: kept in the file, left out of the common set until a share brings it.
    def placed_with(places):
        placed=[]
        for name,issue,author,shown,facts in files:
            kept=[];held=[];waiting=set()
            for f in facts:
                try:spot=places.get(key(f))
                except (KeyError,TypeError):continue
                if spot is None:waiting.add(key(f));held.append(f);continue
                full={**f,**spot,"Seen":0,"Imported":1}
                if valid(dict(full)):kept.append(capped(full));held.append(f)
            placed.append((name,issue,author,shown,facts,kept,held,waiting))
        return placed
    placed=placed_with(places_of(set()))
    # X-183 (N13): one file per GitHub account: the newest issue of a login counts, an older one is reported and left out (a file without its login,
    # written before 0.1210, counts as it is until sync notes it).
    newest={p[2]:p[0] for p in placed if p[2]}
    counted=[p[0] for p in placed if not (p[2] and newest[p[2]]!=p[0])]
    # X-183 (N5): a player alone far outside the others' levels on over 5 % of the facts they share is left out, never allowed to widen everyone's facts.
    held=collections.defaultdict(list)
    for p in placed:
        if p[0] in counted:
            for f in p[5]:held[key(f)].append((p[0],f))
    left=set()
    for name,(alone,shared) in sorted(outliers(held).items()):
        if alone>=FAR_FACTS and alone*20>shared:left.add(name)
    # X-796 M1: the places and days a left-out player brought (a file of before, a gist read now) are taken again without them.
    excluded=left|{p[0] for p in placed if p[0] not in counted}
    if any(owner in excluded for owner,_ in sources):placed=placed_with(places_of(excluded))
    for name,issue,login,shown,facts,kept,kept_held,waiting in placed:
        if len(kept_held)<len(facts):
            say(f"{name}: {len(facts)-len(kept_held)} facts refused by the current rules")
            later.append(lambda issue=issue,shown=shown,kept_held=kept_held:write_player(issue,shown,kept_held))
        if waiting:say(f"{name}: {len(waiting)} facts whose place is in no common file nor gist read now: kept in the player file, left out of the common set")
        if login and newest[login]!=name:say(f"{name}: same author as {newest[login]} (a newer issue): not counted")
    for name,(alone,shared) in sorted(outliers(held).items()):
        if name in left:say(f"{name}: FLAG: alone far outside the other players' levels on {alone} of the {shared} facts it shares: not counted, check it by hand")
    # X-795: a fact a counted player holds, in the common set of git HEAD, with no place now: the common set read was incomplete; nothing written.
    past=head_v3_keys() if history is None else history()
    if past:
        lost=sorted({k for p in placed if p[0] in counted and p[0] not in left for k in p[7] if k in past})
        if lost:raise Lossy(f"{len(lost)} fact(s) held by players and in the common set of git HEAD have no place in the common set read now "
                            f"(first: {lost[0]}): it is incomplete; restore community-discoveries/v3 from git history first")
    merged={}
    for name,issue,author,shown,facts,kept,kept_held,waiting in placed:
        if name not in counted or name in left:continue
        for f in kept:
            known=merged.get(key(f))
            if known is None:merged[key(f)]=dict(f);continue
            known["Imported"]+=1
            if f["LevelMin"]>0:known["LevelMin"]=f["LevelMin"] if known["LevelMin"]==0 else min(known["LevelMin"],f["LevelMin"])
            known["LevelMax"]=max(known["LevelMax"],f["LevelMax"])
            # X-183 (N5), X-772: the place and days are the fact's own (places above), the same in every player's copy.
            # 0.916: HQ sightings and values added over the players (each player's file counts once).
            if "Values" in f:known["Values"]=histogram_sum(known.get("Values"),f["Values"])
            if f.get("Hq",0)>0:known["Hq"]=min(known.get("Hq",0)+f["Hq"],sum(known["Values"].values()))
    common={"Version":VERSION,"Producer":PRODUCER,"Written":datetime.now(timezone.utc).isoformat(),
            "Facts":sorted(merged.values(),key=sort_key)}
    players=len(counted)-len(left)
    # X-174, X-357 (0.1349): the v3 set only, compared byte for byte (X-113: the same facts are not written again, a date alone changes nothing).
    say(f"common: {len(common['Facts'])} facts from {players} players")
    changed=write_v3(bounded(common["Facts"],say=say),V3_DIR,say)
    for write in later:write()
    return changed

def write_v3(facts,folder,say=print):
    """X-174: the v3 set of these facts in folder: a file written only when its bytes change, a kind file no longer in the set removed. True if changed."""
    index,files=v3_build(facts,PRODUCER);files={**files,V3_INDEX:index.encode("utf-8")}
    os.makedirs(folder,exist_ok=True);changed=0
    for name,data in files.items():
        path=os.path.join(folder,name)
        try:same=open(path,"rb").read()==data
        except OSError:same=False
        if not same:write_atomic(path,data);changed+=1
    for name in os.listdir(folder):
        if re.fullmatch(r"kind-\d+\.jsonl\.gz",name) and name not in files:os.remove(os.path.join(folder,name));changed+=1
    say(f"v3: {len(files)-1} kind files, {len(facts)} facts, "+(f"{changed} files written" if changed else "unchanged"))
    return changed>0

def news(issue,facts):
    """X-113: (new or changed, gone) facts of a ticket against its player file: (0, 0) is « nothing new ». X-772: compared on what a player file
    keeps (without_place); a new place or day of a fact goes to the common set through rebuild (fresh), never to the player file."""
    try:kept=(player_of(issue) or {}).get("Facts") or []
    except (OSError,ValueError,KeyError):kept=[]
    old={key(f):json.dumps(without_place(f),sort_keys=True) for f in kept};new={key(f):json.dumps(without_place(f),sort_keys=True) for f in facts}
    return sum(1 for k,v in new.items() if old.get(k)!=v),sum(1 for k in old if k not in new)

# ---------- X-168 (0.1146): which issues are shares, and which can be merged without a look by hand ----------
DISCOVERIES_TITLE=re.compile(r"^\s*Discoveries:\s*(\S+)\s*$",re.I)
GIST_LINK=re.compile(r"https://gist\.github\.com/(?:[A-Za-z0-9-]{1,39}/)?([0-9a-f]{20,40})\b")
CC0_TICKED=re.compile(r"^\s*[-*]\s*\[[xX]\]\s*I share these discoveries under CC0-1\.0\b",re.M)
SHARED_FROM="Shared from Tensho Assistant"
DOUBTS={"gist":"no gist link, or more than one","cc0":"the CC0-1.0 sharing is not ticked","title":"the title names another account than the issue's author",
        "owner":"the gist is not owned by the issue's author"}

def login(issue):return ((issue.get("author") or {}).get("login") or "").strip()

def is_share(issue):
    """X-168: a share by its label, its title « Discoveries: <login> », or its content (GitHub drops the label of a player without rights)."""
    labels={(l.get("name") or "").lower() for l in issue.get("labels") or []}
    body=issue.get("body") or ""
    return "discoveries" in labels or bool(DISCOVERIES_TITLE.match(issue.get("title") or "")) or (SHARED_FROM in body and bool(GIST_LINK.search(body)))

def triage(issue):
    """X-168: (gist id, None) when the issue can be read, else (None, doubt) for manual triage. The gist's owner is checked by owner_doubt."""
    body=issue.get("body") or ""
    gists={m.group(1) for m in GIST_LINK.finditer(body)}
    if len(gists)!=1:return None,"gist"
    if not CC0_TICKED.search(body):return None,"cc0"
    named=DISCOVERIES_TITLE.match(issue.get("title") or "")
    if named and named.group(1).lower()!=login(issue).lower():return None,"title"
    return gists.pop(),None

def owner_doubt(owner,issue):
    """X-168: the gist must belong to the account that opened the issue (GitHub logins ignore case)."""
    return None if owner and login(issue) and owner.lower()==login(issue).lower() else "owner"

def short(text,limit=60):
    """A title on one line, cut to limit; X-183 (N17): control and format characters (terminal escapes, bidi marks) shown as « ? »."""
    text="".join("?" if unicodedata.category(c) in ("Cc","Cf") else c for c in " ".join((text or "").split()))
    return text if len(text)<=limit else text[:limit-1]+"…"

ISSUE_LIMIT=500
def sync(dry):
    # X-168: every open issue is listed (no label filter), then is_share picks the shares.
    issues=json.loads(subprocess.run(["gh","issue","list","-R","Shigu330/Tensho-Assistant","--state","open","--limit",str(ISSUE_LIMIT),
        "--json","number,title,body,author,labels"],capture_output=True,text=True,check=True,timeout=REMOTE_TIMEOUT).stdout)
    os.makedirs(PLAYERS,exist_ok=True)
    # X-807 M1: the player files to write (issue file name: (issue, login, facts)), given to rebuild, which writes them after the common set.
    pending={}
    # X-795: the common set read whole before any player file is written (a stop here leaves everything as it was).
    try:v3_guard()
    except Lossy as error:
        if not dry:raise
        print(f"STOP at the write: {error}")
    taken={};shares=set();fresh=[]
    # X-183 (N13): the newest issue of an account first; an older one of the same account (a gist made again, a second issue) is not read.
    for issue in sorted(issues,key=lambda i:-i["number"]):
        if not is_share(issue):continue
        shares.add(issue["number"]);author=login(issue).lower()
        issue["title"]=short(issue.get("title"))
        if author in taken:print(f"#{issue['number']} {issue['title']}: same author as #{taken[author]} (a newer issue): not read, not counted");continue
        def manual(doubt):
            print(f"#{issue['number']} {issue['title']}: MANUAL ({DOUBTS[doubt]}): not merged; tell the player: "
                  f"python -I -X utf8 tools/discoveries-merge.py notice {issue['number']} manual {doubt}")
        gist,doubt=triage(issue)
        if doubt:manual(doubt);continue
        try:owner,files=gist_listing(gist)
        except Exception as error:print(f"#{issue['number']} {issue['title']}: gist unreadable ({type(error).__name__}), skipped");continue
        doubt=owner_doubt(owner,issue)
        if doubt:manual(doubt);continue
        try:facts,report=clean(from_gist_files(files,fetch_gist_raw),player=True)
        except Exception as error:print(f"#{issue['number']} {issue['title']}: gist unreadable ({type(error).__name__}), skipped");continue
        taken[author]=issue["number"]
        total=report["facts"]+report["refused"];flag=" FLAG: refusals over 5 %" if total and report["refused"]/total>0.05 else ""
        added,gone=news(issue["number"],facts)
        what=" nothing new (same facts as its player file): no write, no « Merged » comment" if (added,gone)==(0,0) else f" {added} new or changed, {gone} gone"
        print(f"#{issue['number']} {issue['title']} (author {short(login(issue))}): {report['facts']} facts, {report['refused']} refused, kinds {report['kinds']};{what}{flag}")
        if dry or flag:continue
        fresh.extend((f"{issue['number']}{PLAYER_SUFFIX}",f) for f in facts)   # X-772: their places and days go to the common set (rebuild), never to the player file; X-796 M1: with their player
        if (added,gone)==(0,0):
            # X-183 (N13): a file written before 0.1210 gets its author noted (the facts unchanged), so rebuild counts one file per account.
            try:held=player_of(issue["number"])
            except (OSError,ValueError):continue
            if held is not None and held.get("Login")!=login(issue):pending[f"{issue['number']}{PLAYER_SUFFIX}"]=(issue["number"],login(issue),facts);print(f"#{issue['number']}: author noted in its player file")
            continue
        pending[f"{issue['number']}{PLAYER_SUFFIX}"]=(issue["number"],login(issue),facts)
    # X-183 (N13): a player file whose issue is no longer an open share (closed by the player, or deleted) still counts until it is withdrawn.
    if len(issues)<ISSUE_LIMIT:
        for name in player_names():
            if stem(name).isdigit() and int(stem(name)) not in shares:
                print(f"players/{name}: issue #{stem(name)} is no longer an open share (closed or deleted): its facts still count; "
                      f"python -I -X utf8 tools/discoveries-merge.py withdraw {stem(name)} removes them")
    if not dry:rebuild(fresh=fresh,pending=pending)
    else:
        print();print(recheck_report())

def withdraw(issue,dry):
    """X-183 (N13): a player's facts taken out of the common files (their issue closed, or asked): players/<issue>.jsonl.gz (X-772; or the
    <issue>.json of before) removed, then rebuild: their facts no other player has leave the common set."""
    paths=[os.path.join(PLAYERS,f"{issue}{suffix}") for suffix in (PLAYER_SUFFIX,".json")] if issue.isdigit() else []
    present=[path for path in paths if os.path.isfile(path)]
    if not present:print(f"players/{issue}: no such player file");return 2
    shown=", ".join("players/"+os.path.basename(path) for path in present)
    if dry:print(f"{shown} would be removed, then the common files rebuilt");return 0
    # X-795: rebuilt without them first; the files removed once the common set is written (a stop leaves them, and the set, as they were)
    rebuild(skip=[os.path.basename(path) for path in present])
    for path in present:os.remove(path)
    print(f"{shown} removed");return 0

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
    # X-357 (0.1349): the common facts from the v3 set (the v2 discoveries.json is gone).
    out=[("community",f) for f in from_v3_dir(V3_DIR).get("Facts",[])] if os.path.isfile(os.path.join(V3_DIR,V3_INDEX)) else []
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
    global MAX_COMPRESSED,OLD_PLUGIN_FACTS   # X-807: lowered for a case, then given back
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
    # X-168: shares found without their label, gist owner and CC0 checked, doubt left to manual triage.
    gid="0123456789abcdef0123";other="fedcba9876543210fedc"
    body=(f"Shared from Tensho Assistant 0.1146.0 (World › Discoveries › automatic exchange).\n\nGist: https://gist.github.com/{gid}\n\n"
          "- [x] I share these discoveries under CC0-1.0 (public domain, https://creativecommons.org/publicdomain/zero/1.0/).\n\nThe gist is updated at each exchange; this issue stays the same.")
    share=lambda **k:{"number":7,"title":"Discoveries: Ann","body":body,"author":{"login":"Ann"},"labels":[],**k}
    check(is_share(share()) and is_share(share(labels=[{"name":"discoveries"}])),"a share without its label is found by its title")
    check(is_share(share(title="my discoveries")),"by its content when the title was changed")
    check(not is_share({"number":8,"title":"Map is wrong","body":f"see https://gist.github.com/{gid}","author":{"login":"Bo"},"labels":[{"name":"bug"}]}),"a bug report with a gist is not a share")
    check(triage(share())==(gid,None) and triage(share(author={"login":"ann"}))==(gid,None),"the plugin's issue passes (login case ignored)")
    check(triage(share(body=body.replace(f"gist.github.com/{gid}",f"gist.github.com/Ann/{gid}")))==(gid,None),"a gist link with its owner's name")
    check(triage(share(body=body.replace("- [x] I share","- [ ] I share")))==(None,"cc0"),"CC0 not ticked: manual")
    check(triage(share(body=body.replace("- [x] I share these discoveries under CC0-1.0","I share")))==(None,"cc0"),"CC0 sentence gone: manual")
    check(triage(share(title="Discoveries: Bob"))==(None,"title"),"title naming another account: manual")
    check(triage(share(body=body+f"\nalso https://gist.github.com/{other}"))==(None,"gist") and triage(share(body="hello"))==(None,"gist"),"two gists, or none: manual")
    check(owner_doubt("Ann",share()) is None and owner_doubt("ANN",share()) is None,"gist of the author")
    check(owner_doubt("Bob",share())=="owner" and owner_doubt("",share())=="owner" and owner_doubt("Ann",share(author={}))=="owner","someone else's gist, or unknown: manual")
    check(short("x"*80).endswith("…") and len(short("x"*80))==60 and short("a\nb")=="a b","titles cut to one line")
    # X-174: the v3 format.
    def refused(run):
        try:run();return False
        except ValueError:return True
    a=f(1,1,128,60.0,140.0,"2026-11-01T00:00:00+00:00");b={**f(10,9,130,0.0,0.0,"2026-11-02T00:00:00+00:00"),"Values":{"3":2},"Hq":1}
    c=f(1,2,128,60.0,140.0,"2026-11-01T00:00:00+00:00")
    index,files=v3_build([b,c,a],"test")
    check((index,files)==v3_build([a,b,c],"test"),"v3: the same facts give the same bytes, whatever their order")
    entries=v3_index(index);by={e["Name"]:e for e in entries}
    got=v3_read(lambda:index,lambda e:files[e["Name"]])
    check(got["Unreadable"]==0 and sorted(got["Facts"],key=sort_key)==sorted([a,b,c],key=sort_key),"v3: round trip")
    check(clean(got)[1]["facts"]==3 and clean(got)[1]["refused"]==0,"v3: the facts read are clean")
    e1=by[v3_name(1)]
    check(refused(lambda:v3_kind(files[v3_name(1)],{**e1,"Sha256":"0"*64})),"v3: a hash that differs from the index is refused")
    check(refused(lambda:v3_kind(files[v3_name(1)][:-1],e1)),"v3: a size that differs from the index is refused")
    check(refused(lambda:v3_index(index.replace(f'"Bytes": {e1["Bytes"]}',f'"Bytes": {MAX_COMPRESSED+1}'))),"v3: an index entry over the bound is refused")
    check(refused(lambda:v3_kind(files[v3_name(1)],e1,max_decompressed=100)),"v3: a file over the decompressed bound is refused")
    check(refused(lambda:v3_index(index.replace('"Version": 3','"Version": 4'))),"v3: an unknown index version is refused")
    def kind_file(lines):
        data=gzip.compress("".join(l+"\n" for l in lines).encode("utf-8"),mtime=0)
        return data,{"Name":v3_name(1),"Kind":1,"Count":len(lines)-1,"Bytes":len(data),"Sha256":hashlib.sha256(data).hexdigest().upper()}
    head=lambda n,kind=1,version=3:v3_line({"Format":V3_FORMAT,"Version":version,"Kind":kind,"Count":n,"Producer":"t","Written":a["Last"]})
    many=[v3_line({**a,"Subject":s}) for s in range(1,40)]
    data,entry=kind_file([head(40)]+many+["x"*400])
    facts,bad=v3_kind(data,entry,max_line=300)
    check(len(facts)==39 and bad==1,f"v3: a line too long is counted, not the whole file ({len(facts)}, {bad})")
    data,entry=kind_file([head(40)]+many+["{not json"])
    check(v3_kind(data,entry)[1]==1,"v3: a bad line is counted")
    data,entry=kind_file([head(3)]+many[:1]+["{bad","{bad"])
    check(refused(lambda:v3_kind(data,entry)),"v3: over 5 % of bad lines refuses the file")
    data,entry=kind_file([head(1,kind=10)]+many[:1])
    check(refused(lambda:v3_kind(data,entry)),"v3: a header of another kind is refused")
    data,entry=kind_file([head(1,version=4)]+many[:1])
    check(refused(lambda:v3_kind(data,entry)),"v3: an unknown header version is refused")
    data,entry=kind_file([head(1)]+[v3_line({**a,"Own":{"First":a["First"]}})])
    check(clean(v3_read(lambda:json.dumps({"Format":V3_FORMAT,"Version":3,"Files":[entry]}),lambda e:data))[1]["refused"]==1,"v3: a fact holding Own is refused")
    # Gists: v3 (index.json and the kind files in base64) and v2 (one file), read the same.
    store={"u/i":index.encode("utf-8"),**{"u/"+n:base64.b64encode(d) for n,d in files.items()},
           "u/v2":base64.b64encode(gzip.compress(json.dumps({"Version":2,"Facts":[a,b,c]}).encode("utf-8")))}
    fetch=lambda url,limit:store[url]
    v3gist=from_gist_files({V3_INDEX:{"raw_url":"u/i"},**{n+GIST_SUFFIX:{"raw_url":"u/"+n} for n in files}},fetch)
    v2gist=from_gist_files({GIST_V2:{"raw_url":"u/v2"}},fetch)
    check(sorted(clean(v3gist)[0],key=sort_key)==sorted(clean(v2gist)[0],key=sort_key) and len(clean(v2gist)[0])==3,"v3 gist and v2 gist read the same facts")
    check(refused(lambda:from_gist_files({V3_INDEX:{"raw_url":"u/i"}},fetch)),"v3 gist: a kind file missing is refused")
    with tempfile.TemporaryDirectory() as tmp:
        quiet=lambda *_:None
        check(write_v3([a,b,c],tmp,quiet) and not write_v3([c,b,a],tmp,quiet),"v3: written once, then unchanged")
        check(write_v3([a],tmp,quiet) and sorted(os.listdir(tmp))==[V3_INDEX,v3_name(1)],"v3: a kind gone is removed")
        check(sorted(from_v3_dir(tmp)["Facts"],key=sort_key)==[a],"v3: a folder reads back")
    # X-183 (N12): a field of another type than the plugin's is refused (the plugin's reader would refuse the line); the plugin's bounds are accepted.
    base=f(1,1,128,60.0,140.0,"2026-11-02T00:00:00+00:00")
    wrong=[{"Detail":-1},{"Territory":5_000_000_000},{"Subject":1.5},{"Subject":True},{"Kind":True},{"LevelMin":2.5},{"Detail":"x"},{"Map":-1},
           {"CellX":1.0},{"X":True},{"Seen":-1},{"Imported":1.5},{"First":20261102},{"CellZ":2**31}]
    check(not [w for w in wrong if valid({**base,**w})],f"N12: wrong types accepted: {[w for w in wrong if valid({**base,**w})]}")
    check(clean({"Version":2,"Facts":[{**base,**w} for w in wrong]})[1]["refused"]==len(wrong),"N12: clean refuses them")
    check(not valid({**base,"LevelMin":90,"LevelMax":10}) and valid({**base,"LevelMin":90,"LevelMax":0}),"N25: the lowest level never above the highest (0 is unknown)")
    check(valid({**base,"Subject":UINT,"Territory":UINT,"Detail":UINT,"Map":UINT}) and valid({**base,"X":60,"Z":140}),"N12: the plugin's bounds and whole places accepted")
    edge={**f(1,1,128,0.0,0.0,"2026-11-02T00:00:00+00:00"),"X":-50.0000001}
    check(valid({**edge,"CellX":-1}) and not valid({**edge,"CellX":-2}),"N12: the cell computed in single precision, as the plugin does")
    # X-183 (N5): a player's share holds their sightings; no date before the plugin noted any; a histogram capped.
    check(not valid({**base,"First":"1970-01-01T00:00:00+00:00"}),"N5: no date before the plugin noted any")
    mine=lambda **k:{**base,"Seen":3,"Values":{"3":2},**k}
    share=lambda *facts:clean({"Version":2,"Facts":[dict(x) for x in facts]},player=True)[1]
    check(share(mine())["facts"]==1 and clean({"Version":2,"Facts":[mine(Seen=0)]})[1]["facts"]==1,"N5: a sighting of the player's own; a file that is not a share keeps Seen 0")
    check(all(share(mine(Seen=s))["refused"]==1 for s in (0,1,MAX_SEEN+1,True)),"N5: Seen 0, below the values counted, over the bound or not whole: refused")
    big=capped({**base,"Values":{"3":100_000_000,"5":1},"Hq":50_000_000})
    check(sum(big["Values"].values())<=PLAYER_SIGHTINGS and big["Values"]["5"]==1 and big["Hq"]<=sum(big["Values"].values()) and capped(dict(big))==big,
          f"N5: a histogram capped, its shape and HQ share kept, the same when capped twice ({big})")
    check(not any(unicodedata.category(c) in ("Cc","Cf") for c in short("a\x1b[2Kb‮c\r\nd")) and short("a\x1b[2Kb")=="a?[2Kb","N17: control and format characters shown as ?")
    # X-183 (N5, N13, N16): rebuild on made-up player files.
    global V3_DIR
    saved=(PLAYERS,V3_DIR)
    with tempfile.TemporaryDirectory() as tmp:
        PLAYERS=os.path.join(tmp,"players");os.makedirs(PLAYERS);V3_DIR=os.path.join(tmp,"v3")
        said=[];say=said.append
        def put(n,facts,author=None):write_json(os.path.join(PLAYERS,f"{n}.json"),{"Issue":n,**({"Login":author} if author else {}),"Facts":facts})
        def common():return sorted(from_v3_dir(V3_DIR)["Facts"],key=sort_key)
        first=[{**f(1,s,128,60.0,140.0,"2026-11-03T00:00:00+00:00"),"LevelMin":10,"LevelMax":12,"Values":{"10":2,"12":1}} for s in range(1,5)]
        put(43,first,"Ann");put(100,[{**g,"LevelMax":100000,"Values":{"999":100_000_000}} for g in first],"Bob")
        rebuild(say);held={key(g):g for g in common()}
        check(all(held[key(g)]["LevelMax"]==12 and held[key(g)]["Imported"]==1 and held[key(g)]["Values"]=={"10":2,"12":1} for g in first)
              and any(l.startswith("100.jsonl.gz: FLAG") for l in said),f"N5: a player far outside the others' levels is left out, their facts unchanged ({said})")
        check(not os.path.exists(os.path.join(tmp,"discoveries.json")) and sorted(os.listdir(tmp))==["players","v3"],"X-357: rebuild writes the v3 set only, no v2 discoveries.json")
        said.clear();check(rebuild(say) is False and "v3: 1 kind files, 4 facts, unchanged" in said,f"X-357: the same facts again: the v3 set unchanged, nothing written ({said})")
        for n in os.listdir(PLAYERS):os.remove(os.path.join(PLAYERS,n))
        put(43,[{**first[0],"X":61.0}],"Ann");put(100,[{**first[0],"X":62.0,"First":"2026-11-02T00:00:00+00:00"}],"Bob")
        rebuild(say);got=common()[0]
        check(got["X"]==62.0 and got["First"].startswith("2026-11-02") and got["Imported"]==2,f"N5: the exact place of the earliest day (« 100.json » read after « 43.json ») {got}")
        said.clear();put(101,[dict(first[1])],"ann");put(7,[dict(first[2])])
        rebuild(say);got={g["Subject"]:(g["Imported"],g["X"]) for g in common()}
        check(got=={1:(1,62.0),2:(1,60.0),3:(1,60.0)} and "43.jsonl.gz: same author as 101.jsonl.gz (a newer issue): not counted" in said,f"N13: one file per account, the newest issue's ({got}, {said})")
        target=os.path.join(tmp,"held.json");write_atomic(target,"old");real=os.replace
        def broken(*_):raise OSError("disk full")
        os.replace=broken
        try:write_atomic(target,"new");raised=False
        except OSError:raised=True
        finally:os.replace=real
        check(raised and open(target,encoding="utf-8").read()=="old" and not [n for n in os.listdir(tmp) if n.endswith(".tmp")],"N16: a failed write leaves the old file whole and no temporary")
        # X-772 (0.1801, D-217): a player file holds no exact place nor day; a file of before is converted by rebuild, the common set unchanged.
        for folder in (PLAYERS,V3_DIR):
            for n in os.listdir(folder):os.remove(os.path.join(folder,n))
        spots=[{**f(1,s,128,60.0+s,140.0-s,"2026-11-03T00:00:00+00:00"),"Last":f"2026-11-0{4+s}T00:00:00+00:00","LevelMin":10,"LevelMax":12,
                "Values":{"10":2,"12":1}} for s in range(1,4)]
        def lines(name):
            with gzip.open(os.path.join(PLAYERS,name),"rt",encoding="utf-8") as z:return [json.loads(l) for l in z.read().splitlines()]
        put(43,[dict(s) for s in spots],"Ann")
        said.clear();rebuild(say)
        before=open(os.path.join(V3_DIR,v3_name(1)),"rb").read()
        got={g["Subject"]:(g["X"],g["Z"],g["First"][:10],g["Last"][:10]) for g in common()}
        check(got=={s["Subject"]:(s["X"],s["Z"],"2026-11-03",s["Last"][:10]) for s in spots},f"X-772: the common set keeps each fact's exact place and days ({got})")
        check(sorted(os.listdir(PLAYERS))==["43.jsonl.gz"] and "43.json: written again as 43.jsonl.gz, without exact places nor days (X-772)" in said,
              f"X-772: the file of before written again in the new format, the old one removed ({os.listdir(PLAYERS)}, {said})")
        head,*rows=lines("43.jsonl.gz")
        check(head=={"Format":PLAYER_FORMAT,"Version":PLAYER_VERSION,"Issue":43,"Login":"Ann","Count":3} and len(rows)==3
              and all(set(r)<=set(PLAYER_ORDER) and not set(r)&{"X","Z","First","Last","Seen","Imported"} for r in rows),
              f"X-772: behaviour: the player file produced holds neither place nor day ({head}, {rows[:1]})")
        said.clear();check(rebuild(say) is False and open(os.path.join(V3_DIR,v3_name(1)),"rb").read()==before and not [l for l in said if "written again" in l],
              f"X-772: rebuilt from the new file: the common set the same bytes, nothing converted again ({said})")
        check(player_bytes(43,"Ann",list(reversed(spots)))==open(os.path.join(PLAYERS,"43.jsonl.gz"),"rb").read(),"X-772: the same facts give the same bytes")
        # sync's gists bring places and days (fresh): an earlier day takes the place, a later one the last day; the player file stays without them
        rebuild(say,fresh=[{**spots[0],"X":70.0,"Z":130.0,"First":"2026-11-02T00:00:00+00:00","Last":"2026-11-02T00:00:00+00:00"},
                           {**spots[1],"X":99.0,"First":"2026-11-04T00:00:00+00:00","Last":"2026-11-09T00:00:00+00:00"}])
        got={g["Subject"]:(g["X"],g["Z"],g["First"][:10],g["Last"][:10]) for g in common()}
        check(got[1]==(70.0,130.0,"2026-11-02","2026-11-05") and got[2]==(62.0,138.0,"2026-11-03","2026-11-09"),f"X-772: places and days from the gists read ({got})")
        check(all(not set(r)&set(PLACE) for r in lines("43.jsonl.gz")[1:]),"X-772: the player file still without them")
        # a fact whose place is known nowhere waits in its player file; a new share brings it
        write_player(100,"Bob",[*[without_place(s) for s in spots[:1]],without_place(f(1,9,128,10.0,10.0,"2026-11-03T00:00:00+00:00"))])
        said.clear();rebuild(say);got={g["Subject"]:g["Imported"] for g in common()}
        check(got=={1:2,2:1,3:1} and "100.jsonl.gz: 1 facts whose place is in no common file nor gist read now: kept in the player file, left out of the common set" in said
              and len(lines("100.jsonl.gz"))==3,f"X-772: a fact without a place known waits ({got}, {said})")
        rebuild(say,fresh=[f(1,9,128,10.0,10.0,"2026-11-03T00:00:00+00:00")])
        check(9 in {g["Subject"] for g in common()},"X-772: brought by a share, it enters the common set")
        # a file read back whole only; the news of a ticket compared without places nor days
        check(read_player(os.path.join(PLAYERS,"100.jsonl.gz"))["Facts"]==sorted([without_place(spots[0]),without_place(f(1,9,128,10.0,10.0,"2026-11-03T00:00:00+00:00"))],key=sort_key),
              "X-772: read back")
        bad=os.path.join(PLAYERS,"7.jsonl.gz")
        for broken in ([{"Format":"x","Version":1,"Count":0}],[{"Format":PLAYER_FORMAT,"Version":1,"Count":2},without_place(spots[0])],
                       [{"Format":PLAYER_FORMAT,"Version":1,"Count":1},spots[0]]):
            write_atomic(bad,gzip.compress("".join(v3_line(o)+"\n" for o in broken).encode("utf-8"),mtime=0))
            check(refused(lambda:read_player(bad)),f"X-772: refused: {broken[0]}, {len(broken)-1} line(s)")
        os.remove(bad)
        check(news(43,[{**s,"X":1.0,"First":"2026-11-01T00:00:00+00:00"} for s in spots])==(0,0),"X-772: another place or day alone is nothing new for the player file")
        import contextlib
        with contextlib.redirect_stdout(io.StringIO()):gone=withdraw("100",False)   # its lines are the tool's report, not the selftest's
        check(gone==0 and sorted(os.listdir(PLAYERS))==["43.jsonl.gz"] and {g["Subject"] for g in common()}=={1,2,3},
              "X-772: withdrawn: the file removed, the facts no other player has gone")
        # X-795 (review of 0.1802, I1): a rebuild that reads the common set missing or cut short stops before writing anything (behaviour).
        for folder in (PLAYERS,V3_DIR):
            for n in os.listdir(folder):os.remove(os.path.join(folder,n))
        put(43,[dict(s) for s in spots],"Ann");put(44,[dict(s) for s in spots[:2]],"Cy");rebuild(say)
        def snapshot():return {(d,n):open(os.path.join(d,n),"rb").read() for d in (PLAYERS,V3_DIR) for n in sorted(os.listdir(d))}
        def stops(run):
            try:run();return False
            except Lossy:return True
        whole_set=snapshot();keys={key(g) for g in common()}
        index_text=open(os.path.join(V3_DIR,V3_INDEX),encoding="utf-8").read()
        os.remove(os.path.join(V3_DIR,V3_INDEX))
        check(stops(lambda:rebuild(say)) and snapshot()=={k:v for k,v in whole_set.items() if k[1]!=V3_INDEX},
              "X-795: v3/index.json missing: rebuild stops, no kind file removed, no player file written")
        check(stops(lambda:withdraw("44",False)) and os.path.isfile(os.path.join(PLAYERS,"44.jsonl.gz")),"X-795: withdraw stops too, the player file kept")
        write_atomic(os.path.join(V3_DIR,V3_INDEX),index_text);check(snapshot()==whole_set,"X-795: restored, the same set")
        entry=json.loads(index_text);entry["Files"]=[];write_atomic(os.path.join(V3_DIR,V3_INDEX),json.dumps(entry))
        check(stops(lambda:rebuild(say)) and open(os.path.join(V3_DIR,v3_name(1)),"rb").read()==whole_set[(V3_DIR,v3_name(1))],
              "X-795: an index listing fewer kind files than the folder holds: rebuild stops, the kind file kept")
        os.remove(os.path.join(V3_DIR,v3_name(1)))
        check(stops(lambda:rebuild(say,history=lambda:keys)) and not os.path.exists(os.path.join(V3_DIR,v3_name(1)))
              and snapshot()=={(d,n):v for (d,n),v in whole_set.items() if d==PLAYERS} | {(V3_DIR,V3_INDEX):json.dumps(entry).encode("utf-8")},
              "X-795: an index cut short with its file gone: the facts of git HEAD without a place stop the rebuild, nothing written")
        write_atomic(os.path.join(V3_DIR,V3_INDEX),index_text)
        check(stops(lambda:rebuild(say)),"X-795: a kind file listed in the index but missing: rebuild stops")
        write_atomic(os.path.join(V3_DIR,v3_name(1)),whole_set[(V3_DIR,v3_name(1))])
        said.clear();check(not rebuild(say,history=lambda:keys) and snapshot()==whole_set,f"X-795: the set whole again: rebuilt, nothing changed ({said})")
        write_player(45,"Di",[without_place(f(1,9,128,10.0,10.0,"2026-11-03T00:00:00+00:00"))])
        said.clear();rebuild(say,history=lambda:keys)
        check("45.jsonl.gz: 1 facts whose place is in no common file nor gist read now: kept in the player file, left out of the common set" in said,
              f"X-795: a fact never in the common set still waits, without a stop ({said})")
        os.remove(os.path.join(PLAYERS,"45.jsonl.gz"))
        # X-796 M1: a player left out (FLAG) brings no place nor day, even through a gist read now (sync) or a file of before.
        for folder in (PLAYERS,V3_DIR):
            for n in os.listdir(folder):os.remove(os.path.join(folder,n))
        put(43,[dict(g) for g in first],"Ann");rebuild(say)
        write_player(100,"Bob",[{**without_place(g),"LevelMax":100000} for g in first])
        early=[{**g,"X":70.0,"Z":130.0,"First":"2026-11-01T00:00:00+00:00","Last":"2026-11-20T00:00:00+00:00"} for g in first]
        said.clear();rebuild(say,fresh=[("100.jsonl.gz",g) for g in early])
        got={(g["X"],g["First"][:10],g["Last"][:10]) for g in common()}
        check(got=={(60.0,"2026-11-03","2026-11-03")} and any(l.startswith("100.jsonl.gz: FLAG") for l in said),
              f"X-796 M1: the gist of a player left out gives neither place nor day ({got}, {said})")
        os.remove(os.path.join(PLAYERS,"100.jsonl.gz"));put(100,[{**g,"LevelMax":100000} for g in early],"Bob")
        said.clear();rebuild(say);got={(g["X"],g["First"][:10],g["Last"][:10]) for g in common()}
        check(got=={(60.0,"2026-11-03","2026-11-03")} and sorted(os.listdir(PLAYERS))==["100.jsonl.gz","43.jsonl.gz"],
              f"X-796 M1: nor their file of before (converted all the same) ({got}, {said})")
        rebuild(say,fresh=early);got={(g["X"],g["First"][:10]) for g in common()}
        check(got=={(70.0,"2026-11-01")},f"X-796 M1: a place from a counted source still takes the earliest day ({got})")
        # X-807 M1 (review of 0.1806): the player files sync read are written by rebuild after the common set; a stop leaves them as they were.
        for folder in (PLAYERS,V3_DIR):
            for n in os.listdir(folder):os.remove(os.path.join(folder,n))
        solo={**spots[0],"Subject":8}   # held by Ann alone: no gist brings its place
        put(43,[*[dict(s) for s in spots],dict(solo)],"Ann");put(44,[dict(s) for s in spots[:2]],"Cy");rebuild(say)
        whole_set=snapshot();keys={key(g) for g in common()}
        index_text=open(os.path.join(V3_DIR,V3_INDEX),encoding="utf-8").read()
        cut=json.loads(index_text);cut["Files"]=[];write_atomic(os.path.join(V3_DIR,V3_INDEX),json.dumps(cut));os.remove(os.path.join(V3_DIR,v3_name(1)))
        newer=[*[dict(s) for s in spots],{**spots[0],"Subject":7}]
        pending={"44.jsonl.gz":(44,"Cy",newer),"45.jsonl.gz":(45,"Di",[dict(spots[2])])}
        gists=[(n,g) for n,(_,_,given) in pending.items() for g in given]
        before=snapshot()
        check(stops(lambda:rebuild(say,fresh=gists,history=lambda:keys,pending=pending)) and snapshot()==before,
              "X-807 M1: a stop in the rebuild of sync leaves every player file as it was (none rewritten, none created)")
        write_atomic(os.path.join(V3_DIR,V3_INDEX),index_text);write_atomic(os.path.join(V3_DIR,v3_name(1)),whole_set[(V3_DIR,v3_name(1))])
        said.clear();rebuild(say,fresh=gists,history=lambda:keys,pending=pending)
        check(read_player(os.path.join(PLAYERS,"44.jsonl.gz"))["Facts"]==sorted([without_place(g) for g in newer],key=sort_key)
              and read_player(os.path.join(PLAYERS,"45.jsonl.gz"))["Login"]=="Di" and 7 in {g["Subject"] for g in common()},
              f"X-807 M1: the set whole: the player files of sync written after it, their facts in the common set ({said})")
    PLAYERS,V3_DIR=saved
    # X-146: a gist file read within its bound and its time, a slow trickle stopped.
    class Answer:
        def __init__(self,data,pause=0.0):self.data=data;self.pause=pause
        def __enter__(self):return self
        def __exit__(self,*_):return False
        def read(self,n):
            time.sleep(self.pause);n=min(n,1 if self.pause else n);out,self.data=self.data[:n],self.data[n:];return out
    url="https://gist.githubusercontent.com/a/b/raw/c"
    check(fetch_gist_raw(url,10,opener=lambda u,timeout:Answer(b"abc"))==b"abc","X-146: a gist file read")
    check(refused(lambda:fetch_gist_raw(url,2,opener=lambda u,timeout:Answer(b"abc"))),"X-146: over its bound: refused")
    try:fetch_gist_raw(url,100,deadline=0.2,opener=lambda u,timeout:Answer(b"x"*50,0.02));slow=False
    except TimeoutError:slow=True
    check(slow,"X-146: a byte at a time past the deadline: stopped")
    check(refused(lambda:fetch_gist_raw("https://example.com/x",10,opener=lambda u,timeout:Answer(b"abc"))),"X-146: another address: refused")
    # X-406: a kind over the bound kept to it (most seen, then most recent), the index read back whole; unbounded, refused before writing.
    global MAX_FACTS
    bound=MAX_FACTS
    try:
        MAX_FACTS=3
        g=lambda subject,imported,last:{**f(1,subject,128,60.0,140.0,last),"Imported":imported}
        many=[g(1,1,"2026-11-01T00:00:00+00:00"),g(2,5,"2026-11-01T00:00:00+00:00"),g(3,1,"2026-11-05T00:00:00+00:00"),
              g(4,2,"2026-11-02T00:00:00+00:00"),g(5,1,"2026-11-04T00:00:00+00:00"),f(3,9,128,0.0,0.0,"2026-11-01T00:00:00+00:00")]
        said=[];kept=bounded(many,say=said.append)
        check(sorted(k["Subject"] for k in kept if k["Kind"]==1)==[2,3,4] and any(k["Kind"]==3 for k in kept) and len(kept)==4
              and said==["kind 1: 5 facts over the bound of 3: 2 left out (the least seen, then the oldest)"],f"X-406: bounded keeps {[(k['Kind'],k['Subject']) for k in kept]}, {said}")
        check(bounded(many[:3],say=said.append) is not None and len(said)==1,"X-406: a kind under the bound: unchanged, nothing said")
        index,files=v3_build(kept,"t")
        check([e["Count"] for e in v3_index(index)]==[3,1],"X-406: the bounded set's index reads whole")
        check(refused(lambda:v3_build(many,"t")),"X-406: a kind over the bound: v3_build refuses to write")
        with tempfile.TemporaryDirectory(prefix="tensho-x406-") as tmp:
            check(write_v3(bounded(many,say=lambda s:None),tmp,lambda s:None) and len(v3_index(open(os.path.join(tmp,V3_INDEX),encoding="utf-8").read()))==2,
                  "X-406: the set written in a folder reads back")
        # X-468 (0.1805, D-216 a): a kind's file kept within the format's byte bounds, the most seen then the most recent kept (behaviour).
        MAX_FACTS=bound
        wide=[g(s,1+(s%7==0)+(s%11==0),f"2026-11-{1+s%20:02d}T00:00:00+00:00") for s in range(1,401)]
        line=len(v3_line({k:wide[0][k] for k in ORDER if k in wide[0]}).encode("utf-8"))+1
        said=[];kept=bounded(wide,say=said.append,max_decompressed=1024+100*line)
        heavy=sorted(wide,key=lambda x:(-x["Imported"],-when(x["Last"]).timestamp(),sort_key(x)))
        check(90<=len(kept)<=100 and {key(x) for x in kept}=={key(x) for x in heavy[:len(kept)]} and {key(x) for x in wide if x["Imported"]>=2}<={key(x) for x in kept}
              and len(gzip.decompress(v3_kind_bytes(1,kept,PRODUCER)))<=1024+100*line and said and "over the format's byte bounds" in said[0],
              f"X-468: over the decompressed bound, the most seen then the most recent kept ({len(kept)}, {said})")
        squeezed=bounded(wide,say=said.append,max_compressed=1200)
        check(0<len(squeezed)<len(wide) and len(v3_kind_bytes(1,squeezed,PRODUCER))<=1200 and {key(x) for x in squeezed}=={key(x) for x in heavy[:len(squeezed)]},
              f"X-468: over the compressed bound, the file written fits it ({len(squeezed)}, {len(v3_kind_bytes(1,squeezed,PRODUCER))} bytes)")
        # X-807 M2: measured with the producer the write uses: the file as write_v3 writes it fits; v3_build refuses a kind file over the bound.
        compressed,old=MAX_COMPRESSED,OLD_PLUGIN_FACTS
        try:
            MAX_COMPRESSED=1200
            check(len(v3_build(squeezed,PRODUCER)[1][v3_name(1)])<=1200,"X-807 M2: the file bounded() keeps is the size v3_build writes, within the bound")
            check(refused(lambda:v3_build(wide,PRODUCER)),"X-807 M2: v3_build refuses a kind file over the compressed bound (its entry would refuse the index)")
            OLD_PLUGIN_FACTS=3;said.clear();bounded(wide[:5],say=said.append)
            check(any(s.startswith("WARN kind 1: 5 facts, over 3: Tensho 0.1804 and older refuse the whole common set") for s in said),
                  f"X-807 M3: a kind past the bound of the plugins up to 0.1804 is said ({said})")
        finally:MAX_COMPRESSED,OLD_PLUGIN_FACTS=compressed,old
        said.clear();check(bounded(wide,say=said.append) is wide and not said,"X-468: within every bound: the same list, nothing said")
    finally:MAX_FACTS=bound
    plugin=open(os.path.join(ROOT,"src","TenshoAssistant","Discoveries","DiscoveriesModel.cs"),encoding="utf-8").read()
    found=re.search(r"public const int MaxFacts=([0-9_]+);",plugin)
    check(found and MAX_FACTS==int(found.group(1).replace("_","")) and MAX_FACTS==2_000_000,"X-406, X-468: the bound is the plugin's DiscoveryStore.MaxFacts (2 000 000)")
    if os.path.isfile(os.path.join(V3_DIR,V3_INDEX)):
        # X-357 (0.1349): was « v3 holds the facts of discoveries.json »; the v2 file is gone: the repository's set reads whole and clean.
        repo=from_v3_dir(V3_DIR);facts,report=clean(repo)
        check(repo["Unreadable"]==0 and len(repo["Facts"])>0 and report["refused"]==0 and report["facts"]==len(repo["Facts"]),
              f"v3: the repository's set reads whole, every fact valid ({report['facts']} facts, {report['refused']} refused)")
    print(f"discoveries-merge selftest: {ok}/{total} ok");return 0 if ok==total else 1

# X-168: the player is told why a share was not taken (English then French).
MANUAL_NOTICES={
    "gist":("Not merged: this issue should hold exactly one link to your discoveries gist. Please share again from Tensho Assistant (World › Discoveries › Exchange now).",
            "Pas fusionné : ce ticket doit contenir un seul lien vers votre gist de découvertes. Merci de partager à nouveau depuis Tensho Assistant (Monde › Découvertes › Échanger maintenant)."),
    "cc0":("Not merged: the CC0-1.0 sharing is not ticked in this issue, and shared discoveries are published under CC0-1.0 only. Tick it (or share again from Tensho Assistant) if you agree.",
           "Pas fusionné : le partage sous CC0-1.0 n'est pas coché dans ce ticket, et les découvertes partagées ne sont publiées que sous CC0-1.0. Cochez-le (ou partagez à nouveau depuis Tensho Assistant) si vous êtes d'accord."),
    "title":("Not merged yet: the title of this issue names another account than the one that opened it. The maintainer will look at it by hand.",
             "Pas encore fusionné : le titre de ce ticket nomme un autre compte que celui qui l'a ouvert. Le mainteneur va le regarder à la main."),
    "owner":("Not merged yet: the linked gist does not belong to the account that opened this issue. The maintainer will look at it by hand; sharing from Tensho Assistant with your own account avoids this.",
             "Pas encore fusionné : le gist lié n'appartient pas au compte qui a ouvert ce ticket. Le mainteneur va le regarder à la main ; partager depuis Tensho Assistant avec votre propre compte l'évite."),
}

def notice_manual(issue,doubt,dry):
    sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
    from bilingual import compose
    if doubt not in MANUAL_NOTICES:print(__doc__);return 2
    body=compose(*MANUAL_NOTICES[doubt])
    if dry:print(body);return 0
    out=subprocess.run(["gh","issue","comment",issue,"-R","Shigu330/Tensho-Assistant","--body",body],capture_output=True,text=True,timeout=REMOTE_TIMEOUT)
    print(f"#{issue}: {'commented' if out.returncode==0 else 'comment failed'}");return out.returncode

def notice(argv):
    sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
    from bilingual import compose
    dry="--dry-run" in argv;argv=[a for a in argv if a!="--dry-run"]
    if len(argv)==3 and argv[1]=="manual" and argv[2] in MANUAL_NOTICES:argv=argv+[""]
    if len(argv)!=4 or argv[1] not in ("merged","flag","manual") or not argv[0].isdigit():print(__doc__);return 2
    issue,what,a,b=argv
    if what=="manual":return notice_manual(issue,a,dry)
    if what=="merged":body=compose(f"Merged into {a}: {b} facts. Thank you! They reach every player with the next release.",
                                   f"Fusionné dans la {a} : {b} faits. Merci ! Ils arrivent chez tous les joueurs avec la prochaine release.")
    else:body=compose(f"Not merged: {a} of {b} facts were refused (over 5 %). Please update Tensho Assistant, export again and update the gist.",
                      f"Pas fusionné : {a} faits refusés sur {b} (plus de 5 %). Merci de mettre Tensho Assistant à jour, d'exporter à nouveau et de mettre le gist à jour.")
    if dry:print(body);return 0
    out=subprocess.run(["gh","issue","comment",issue,"-R","Shigu330/Tensho-Assistant","--body",body],capture_output=True,text=True,timeout=REMOTE_TIMEOUT)
    print(f"#{issue}: {'commented' if out.returncode==0 else 'comment failed'}");return out.returncode

def utf8_output(stream):
    """X-625 (0.1569): the reports hold « → » and accented names; a Windows console (cp1252) could not write them and `sync --dry-run`
    stopped on a UnicodeEncodeError when run with plain `python -I`. The tool's own output is UTF-8 whatever the console."""
    if hasattr(stream,"reconfigure") and (stream.encoding or "").lower().replace("-","")!="utf8":stream.reconfigure(encoding="utf-8",errors="replace")

if __name__=="__main__":
    utf8_output(sys.stdout);utf8_output(sys.stderr)
    if len(sys.argv)<2:print(__doc__);sys.exit(2)
    command=sys.argv[1]
    try:
        if command=="sync":sync("--dry-run" in sys.argv);sys.exit(0)
        if command=="rebuild":rebuild();sys.exit(0)
        if command=="withdraw" and len([a for a in sys.argv[2:] if a!="--dry-run"])==1:
            sys.exit(withdraw([a for a in sys.argv[2:] if a!="--dry-run"][0],"--dry-run" in sys.argv))
    except Lossy as error:   # X-795
        print(f"STOP: {error}; nothing written");sys.exit(2)
    if command=="check" and len(sys.argv)==3:print(clean(read(sys.argv[2]))[1])
    elif command=="notice":sys.exit(notice(sys.argv[2:]))
    elif command=="recheck":
        argv=sys.argv[2:];changes=argv[argv.index("--changes")+1] if "--changes" in argv else None
        print(recheck_report(changes,"--local" in argv,int(argv[argv.index("--limit")+1]) if "--limit" in argv else 40))
    elif command=="--selftest":sys.exit(selftest())
    else:print(__doc__);sys.exit(2)
