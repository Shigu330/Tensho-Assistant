"""X-77 (0.882): merges shared discovery files into community-discoveries/discoveries.json (maintainer tool, run by hand, offline).
Usage: python tools/discoveries-merge.py community-discoveries/discoveries.json <file.zip|file.json|gist:ID|https://gist.github.com/...> ...
Rules mirror DiscoveryStore.Valid and Merge (src/TenshoAssistant/Discoveries/DiscoveriesModel.cs): a fact that does not hold is refused; each shared file
counts as one import of a fact (Seen is kept at 0 in the common file); levels and dates are widened. Nothing personal can be in a fact by construction;
the tool still refuses any property it does not know."""
import json,math,sys,zipfile
from datetime import datetime,timezone,timedelta
KINDS=set(range(1,23)); PLACED={1,2,3,8,10,19,22}; CELL=50
FIELDS={"Kind","Subject","Detail","Territory","Map","CellX","CellZ","X","Z","LevelMin","LevelMax","Seen","Imported","First","Last"}
def key(f):return f"{f['Kind']}:{f['Subject']}:{f['Detail']}:{f['Territory']}:{f['Map']}:{f['CellX']}:{f['CellZ']}"
def when(text):return datetime.fromisoformat(text.replace("Z","+00:00"))
def valid(f):
    try:
        f.pop("Key",None)  # computed by the plugin (DiscoveryFact.Key): written in its files, recomputed here
        if set(f)-FIELDS or f["Kind"] not in KINDS or f["Subject"]<=0:return False
        if f["Kind"] in PLACED and f["Map"]==0:return False
        if not all(math.isfinite(f[n]) and abs(f[n])<=10000 for n in ("X","Z")):return False
        if (math.floor(f["X"]/CELL),math.floor(f["Z"]/CELL))!=(f["CellX"],f["CellZ"]):return False
        if not (0<=f["LevelMin"]<=100000 and 0<=f["LevelMax"]<=100000):return False
        return when(f["First"])<=when(f["Last"])<=datetime.now(timezone.utc)+timedelta(days=1)
    except Exception:return False
def read(path):
    if path.endswith(".zip"):
        with zipfile.ZipFile(path) as z:
            name=next(n for n in z.namelist() if n.endswith(".json"))
            if z.getinfo(name).file_size>64*1024*1024:raise ValueError("too large")
            return json.loads(z.read(name).decode("utf-8"))
    return json.load(open(path,encoding="utf-8"))
def from_gist(reference):
    """0.883: a gist id or URL (from a « Discoveries: <login> » issue), read through the GitHub CLI; the file is base64 of gzip of JSON."""
    import base64,gzip,subprocess
    gist=reference.removeprefix("gist:").rstrip("/").split("/")[-1]
    raw=subprocess.run(["gh","api",f"gists/{gist}","--jq",'.files["tensho-discoveries.b64"].raw_url'],capture_output=True,text=True,check=True).stdout.strip()
    text=subprocess.run(["gh","api",raw],capture_output=True,text=True,check=True).stdout if raw.startswith("https://api.") else __import__("urllib.request").request.urlopen(raw).read().decode()
    data=gzip.decompress(base64.b64decode(text.strip()))
    if len(data)>64*1024*1024:raise ValueError("too large")
    return json.loads(data.decode("utf-8"))
def main(common_path,inputs):
    common=json.load(open(common_path,encoding="utf-8"));facts={key(f):f for f in common["Facts"]}
    for path in inputs:
        data=from_gist(path) if path.startswith("gist:") or "gist.github.com" in path else read(path);added=joined=refused=0
        if data.get("Version")!=1:print(path,"refused: version");continue
        for f in data.get("Facts",[]):
            if not valid(f):refused+=1;continue
            known=facts.get(key(f))
            if known is None:
                g=dict(f);g["Seen"]=0;g["Imported"]=1;facts[key(f)]=g;added+=1
            else:
                known["Imported"]+=1
                if f["LevelMin"]>0:known["LevelMin"]=f["LevelMin"] if known["LevelMin"]==0 else min(known["LevelMin"],f["LevelMin"])
                known["LevelMax"]=max(known["LevelMax"],f["LevelMax"])
                if when(f["First"])<when(known["First"]):known["First"]=f["First"]
                if when(f["Last"])>when(known["Last"]):known["Last"]=f["Last"]
                joined+=1
        print(path,f"added {added}, joined {joined}, refused {refused}")
    common["Facts"]=sorted(facts.values(),key=lambda f:(f["Kind"],f["Subject"],f["Detail"],f["Territory"],f["Map"],f["CellX"],f["CellZ"]))
    common["Written"]=datetime.now(timezone.utc).isoformat()
    json.dump(common,open(common_path,"w",encoding="utf-8",newline="\n"),ensure_ascii=False,separators=(",",":"))
if __name__=="__main__":
    if len(sys.argv)<3:print(__doc__);sys.exit(2)
    main(sys.argv[1],sys.argv[2:])
