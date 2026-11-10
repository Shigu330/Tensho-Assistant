"""X-93 part 1 (0.1036, docs/evidence/x93-load-test.md): load test of the maintainer's merge tool, tools/discoveries-merge.py, called as it is.

Invented but realistic shares (the kind mix of the shared ticket #43 and a real local base, game-bounded subjects, 50-yalm cells, value histograms,
dates kept to the day) are written as gist contents in a temporary folder, one per player; then, as `sync` does without the network: clean() of
each share and the player files written, rebuild() of discoveries.json, and rebuild() again with nothing new. Times, file sizes and the peak memory
of the process (each run in its own process, so that peaks do not add up) are printed, median over the runs. Writes only to a temporary folder,
removed at the end; the repository's community-discoveries/ is never touched. Not run by Verify (minutes at a million facts).

Usage:
  python -I -X utf8 tools/discoveries-load.py [--sizes 100000,1000000] [--players 100] [--runs 3] [--json out.json]
  python -I -X utf8 tools/discoveries-load.py --selftest     a 2 000-fact run, checks the merge kept every distinct fact (a few seconds)
"""
import contextlib,importlib.util,io,json,math,os,random,shutil,statistics,subprocess,sys,tempfile,time
from datetime import datetime,timedelta,timezone

HERE=os.path.dirname(os.path.abspath(__file__))
# Same table as tools/DiscoveryLoadProbe/Program.cs: (kind, weight, subjects, details, lowest value, highest value); weights from ticket #43 and a
# real local base added together (2026-11-10), 5 for each kind neither holds.
KINDS=[(1,2278,12000,2,1,100),(3,212,2600,1,1,100),(28,94,300,3,1,300),(23,105,1600,4,10,900),(10,102,1600,120,1,1),(11,92,1600,150,0,1439),
       (21,84,1600,150,0,1439),(4,66,9000,40000,1,5),(12,62,10000,12,0,255),(29,33,1600,120,10,900),(20,31,1600,2,1,1),(19,29,3000,1,0,0),
       (17,12,150,4,1,5000),(13,10,3000,150,1,10),(5,8,2000,700,1,500),(2,7,400,1,1,3),
       (6,5,8000,900,1,3),(7,5,2000,2600,1,5),(8,5,8000,12000,1,5),(9,5,3000,3000,1,5),(14,5,150,150,1,1),(15,5,124,12000,0,0),(16,5,200,12000,0,0),
       (18,5,300,150,0,1439),(22,5,124,12000,1,100),(24,5,300,300,1,10),(25,5,10000,5000,1,10),(30,5,1600,1,1,600),(31,5,1600,1600,0,0),
       (32,5,150,2,1,200),(33,5,12000,1,1,100),(34,5,900,1,0,0),(35,5,300,300,0,0),(36,5,300,1,1,2000),(37,5,600,40,1,10)]
PLACED={1,2,3,8,10,19,22,23,28,29,30};ZONED={11,21,12,35,36};MAPS=450

def load_merge():
    spec=importlib.util.spec_from_file_location("discoveries_merge",os.path.join(HERE,"discoveries-merge.py"))
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def peak_mb():
    """Peak working set of this process (Windows), or peak resident size elsewhere."""
    if os.name=="nt":
        import ctypes
        from ctypes import wintypes
        class Counters(ctypes.Structure):
            _fields_=[("cb",wintypes.DWORD),("PageFaultCount",wintypes.DWORD)]+[(n,ctypes.c_size_t) for n in ("PeakWorkingSetSize","WorkingSetSize",
                "QuotaPeakPagedPoolUsage","QuotaPagedPoolUsage","QuotaPeakNonPagedPoolUsage","QuotaNonPagedPoolUsage","PagefileUsage","PeakPagefileUsage")]
        counters=Counters();counters.cb=ctypes.sizeof(counters)
        process=ctypes.windll.kernel32.GetCurrentProcess
        process.restype=wintypes.HANDLE
        query=ctypes.windll.psapi.GetProcessMemoryInfo
        query.argtypes=[wintypes.HANDLE,ctypes.POINTER(Counters),wintypes.DWORD];query.restype=wintypes.BOOL
        query(process(),ctypes.byref(counters),counters.cb)
        return counters.PeakWorkingSetSize/1048576
    import resource
    return resource.getrusage(resource.RUSAGE_SELF).ru_maxrss/1024

def day(at):return at.strftime("%Y-%m-%dT00:00:00+00:00")

def one_fact(spec,rng,today):
    kind,_,subjects,details,lowest,highest=spec
    draw=rng.random();subject=int(draw*draw*subjects)+1
    detail=subject*3%30000+1+rng.randrange(details) if kind==1 else rng.randrange(details)+(0 if details==1 else 1)
    territory=mapid=0;x=z=0.0
    if kind in PLACED:
        mapid=(subject*7919+rng.randrange(3))%MAPS+1;territory=mapid+100
        cx=(subject*131+mapid*17)%1600-800;cz=(subject*197+mapid*29)%1600-800;spread=160 if kind in (1,2,3) else 40
        x=round(max(-1000,min(1000,cx+rng.gauss(0,spread))),3);z=round(max(-1000,min(1000,cz+rng.gauss(0,spread))),3)
    elif kind in ZONED:territory=(subject*7919+rng.randrange(2))%MAPS+101
    return {"Kind":kind,"Subject":subject,"Detail":detail,"Territory":territory,"Map":mapid,"CellX":math.floor(x/50),"CellZ":math.floor(z/50),"X":x,"Z":z}

def sighting(fact,spec,rng,today):
    """One player's copy of a fact, as their gist holds it (Seen >= 1, dates to the day, values counted)."""
    _,_,_,_,lowest,highest=spec
    roll=rng.random();seen=1 if roll<0.64 else 2 if roll<0.82 else 3 if roll<0.90 else 4+rng.randrange(37)
    last=today-timedelta(days=rng.randrange(2));first=last-timedelta(days=rng.randrange(21))
    f=dict(fact,Seen=seen,Imported=0,First=day(first),Last=day(last),LevelMin=0,LevelMax=0)
    if highest>0:
        centre=lowest+rng.randrange(highest-lowest+1);width=(highest-lowest)//20+1;values={}
        for _ in range(seen):
            v=max(lowest,min(highest,centre+(rng.randrange(-width,width+1) if highest>lowest else 0)))
            k=v if v in values or len(values)<32 else min(values,key=lambda w:(abs(w-v),w));values[k]=values.get(k,0)+1
        f["Values"]={str(k):values[k] for k in sorted(values)};f["LevelMin"]=min(values);f["LevelMax"]=max(values)
        if kind_hq(fact["Kind"]) and rng.random()<0.1:f["Hq"]=rng.randint(1,sum(values.values()))
    return f
def kind_hq(kind):return kind in (10,8,6,4)

def generate(folder,size,players,seed):
    """size distinct facts spread over players gist files (70 % of facts held by one player, 20 % by 2-3, 10 % by 4-20)."""
    rng=random.Random(seed);today=datetime.now(timezone.utc).replace(hour=0,minute=0,second=0,microsecond=0)
    total=sum(k[1] for k in KINDS);keys=set();shares=[[] for _ in range(players)];full=set();made=0
    while made<size:
        pick=rng.randrange(total)
        for spec in KINDS:
            if pick<spec[1]:break
            pick-=spec[1]
        if spec[0] in full:continue
        for _ in range(20):
            fact=one_fact(spec,rng,today);key=f"{fact['Kind']}:{fact['Subject']}:{fact['Detail']}:{fact['Territory']}:{fact['Map']}:{fact['CellX']}:{fact['CellZ']}"
            if key not in keys:keys.add(key);break
        else:
            full.add(spec[0]);continue
        share=rng.random();holders=1 if share<0.7 else 2+rng.randrange(2) if share<0.9 else 4+rng.randrange(17)
        for player in rng.sample(range(players),min(holders,players)):shares[player].append(sighting(fact,spec,rng,today))
        made+=1
    raw=os.path.join(folder,"raw");os.makedirs(raw)
    written=datetime.now(timezone.utc).isoformat()
    for number,facts in enumerate(shares,1):
        with open(os.path.join(raw,f"{number}.json"),"w",encoding="utf-8",newline="\n") as out:
            json.dump({"Version":2,"Producer":"Tensho Assistant","Written":written,"Facts":facts},out,separators=(",",":"))
    return sum(len(s) for s in shares),max(len(s) for s in shares)

def measure(size,players,seed):
    """One run, in this process: generate, then sync's clean + player files, rebuild, rebuild again. Prints one JSON line."""
    folder=tempfile.mkdtemp(prefix="tensho-merge-load-")
    try:
        sightings,largest=generate(folder,size,players,seed)
        merge=load_merge();merge.PLAYERS=os.path.join(folder,"players");merge.COMMON=os.path.join(folder,"discoveries.json");os.makedirs(merge.PLAYERS)
        raw=os.path.join(folder,"raw");refused=0;kept=0;quiet=io.StringIO()
        start=time.perf_counter()
        for name in sorted(os.listdir(raw),key=lambda n:int(n.split(".")[0])):
            # sync(): clean() of the gist's content, then the player file written (news() and the network left out).
            data=merge.read(os.path.join(raw,name));facts,report=merge.clean(data);refused+=report["refused"];kept+=len(facts)
            with open(os.path.join(merge.PLAYERS,name),"w",encoding="utf-8",newline="\n") as out:
                json.dump({"Issue":int(name.split(".")[0]),"Facts":facts},out,ensure_ascii=False,separators=(",",":"))
        clean_s=time.perf_counter()-start
        start=time.perf_counter()
        with contextlib.redirect_stdout(quiet):changed=merge.rebuild()
        rebuild_s=time.perf_counter()-start
        start=time.perf_counter()
        with contextlib.redirect_stdout(quiet):again=merge.rebuild()
        again_s=time.perf_counter()-start
        common=json.load(open(merge.COMMON,encoding="utf-8"))["Facts"]
        return {"sightings":sightings,"largest_share":largest,"refused":refused,"kept":kept,"common_facts":len(common),"changed":changed,"unchanged_second":not again,
                "clean_s":clean_s,"rebuild_s":rebuild_s,"rebuild_unchanged_s":again_s,
                "common_mb":os.path.getsize(merge.COMMON)/1048576,"players_mb":sum(os.path.getsize(os.path.join(merge.PLAYERS,n)) for n in os.listdir(merge.PLAYERS))/1048576,
                "bytes_a_fact":os.path.getsize(merge.COMMON)/max(1,len(common)),"peak_mb":peak_mb()}
    finally:shutil.rmtree(folder,ignore_errors=True)

def main(argv):
    if "--one" in argv:
        at=argv.index("--one");print(json.dumps(measure(int(argv[at+1]),int(argv[at+2]),int(argv[at+3]))));return 0
    if "--selftest" in argv:
        result=measure(2000,10,1)
        ok=result["common_facts"]==2000 and result["refused"]==0 and result["changed"] and result["unchanged_second"]
        print(("OK" if ok else "FAIL")+f" discoveries-load selftest: {result['common_facts']} facts merged from {result['sightings']} sightings, {result['refused']} refused");return 0 if ok else 1
    option=lambda name,default:argv[argv.index(name)+1] if name in argv else default
    sizes=[int(s) for s in option("--sizes","100000,1000000").split(",")];players=int(option("--players","100"));runs=int(option("--runs","3"))
    report={}
    print(f"Python {sys.version.split()[0]}, {players} players, {runs} runs a size (median [min-max]), each run in its own process")
    for size in sizes:
        results=[]
        for run in range(runs):
            out=subprocess.run([sys.executable,"-I","-X","utf8",os.path.abspath(__file__),"--one",str(size),str(players),"93"],capture_output=True,text=True,check=True)
            results.append(json.loads(out.stdout.strip().splitlines()[-1]));print(f"  size {size:,}: run {run+1}/{runs} done",file=sys.stderr)
        print(f"\nSIZE {size:,} distinct facts: {results[0]['sightings']:,} sightings in {players} shares (largest {results[0]['largest_share']:,}), "
              f"{results[0]['common_facts']:,} facts in discoveries.json, {results[0]['refused']} refused")
        rows={}
        for name in ("clean_s","rebuild_s","rebuild_unchanged_s","common_mb","players_mb","bytes_a_fact","peak_mb"):
            values=sorted(r[name] for r in results);rows[name]={"median":statistics.median(values),"min":values[0],"max":values[-1]}
            print(f"  {name:<22} {statistics.median(values):>12.2f}   [{values[0]:.2f} - {values[-1]:.2f}]")
        report[str(size)]={"facts":results[0],"metrics":rows}
    if "--json" in argv:
        with open(option("--json",""),"w",encoding="utf-8") as out:json.dump(report,out,indent=2)
    return 0

if __name__=="__main__":sys.exit(main(sys.argv[1:]))
