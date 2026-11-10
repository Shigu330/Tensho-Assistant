using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// X-93 (0.938, redone in 0.1036 for X-93 part 1, docs/evidence/x93-load-test.md): how the discoveries base behaves with many facts. Invented but
// realistic facts (kind mix of the shared ticket #43 and a real local base, game-bounded subjects, 50-yalm cells, value histograms) go through the
// plugin's own code, called as it is (DiscoveryStore, DiscoveryFiles, CommunityDiscoveries, SeenPlaces): load, memory, save, merge, and the reads the
// pages and tooltips make. Offline; writes only to a temporary folder, removed at the end. Not run by Verify (it takes minutes at a million facts).
// Usage: dotnet run -c Release -- [--sizes 100000,1000000] [--runs 5] [--json out.json]
var sizes=new[]{100_000,1_000_000};var runs=5;string? jsonOut=null;
for(var index=0;index<args.Length;index++)
    switch(args[index])
    {
        case "--sizes":sizes=args[++index].Split(',').Select(int.Parse).ToArray();break;
        case "--runs":runs=int.Parse(args[++index]);break;
        case "--json":jsonOut=args[++index];break;
        default:sizes=args.Where(arg=>int.TryParse(arg,out _)).Select(int.Parse).ToArray();index=args.Length;break;
    }
CultureInfo.DefaultThreadCurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
var folder=Path.Combine(Path.GetTempPath(),"tensho-load-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
var report=new Dictionary<string,object>();
try
{
    Console.WriteLine($"Machine: {Environment.ProcessorCount} logical CPUs, .NET {Environment.Version}, {(Debugger.IsAttached ? "debugger":"no debugger")}, {(IsRelease() ? "Release":"Debug")}");
    foreach(var size in sizes)
    {
        var samples=new SortedDictionary<string,List<double>>(StringComparer.Ordinal);
        void Add(string name,double value){if(!samples.TryGetValue(name,out var list))samples[name]=list=[];list.Add(value);}
        Dictionary<int,int>? kindCounts=null;
        for(var run=0;run<runs;run++)
        {
            var own=Invented.Facts(size,true,93);
            kindCounts??=own.GroupBy(fact=>(int)fact.Kind).OrderBy(group=>group.Key).ToDictionary(group=>group.Key,group=>group.Count());
            Measure.Own(own,folder,Add);
            own=null;Measure.Settle();
            var shared=Invented.Facts(size,false,93);
            Measure.Shared(shared,folder,Add);
            shared=null;Measure.Settle();
            Console.Error.WriteLine($"  size {size:N0}: run {run+1}/{runs} done");
        }
        Console.WriteLine();
        Console.WriteLine($"SIZE {size:N0} facts, {runs} runs (median [min–max]); kinds: {string.Join(" ",kindCounts!.OrderByDescending(pair=>pair.Value).Select(pair=>$"{pair.Key}:{pair.Value}"))}");
        var rows=new Dictionary<string,object>();
        foreach(var (name,values) in samples)
        {
            values.Sort();var median=values.Count%2==1 ? values[values.Count/2]:(values[values.Count/2-1]+values[values.Count/2])/2;
            Console.WriteLine($"  {name,-58} {Format(median),12}   [{Format(values[0])} – {Format(values[^1])}]");
            rows[name]=new{median,min=values[0],max=values[^1]};
        }
        report[size.ToString(CultureInfo.InvariantCulture)]=new{kinds=kindCounts,metrics=rows};
    }
    Console.WriteLine($"Peak working set of the whole run: {Process.GetCurrentProcess().PeakWorkingSet64/1_048_576.0:0} MB");
    if(jsonOut is not null)File.WriteAllText(jsonOut,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
}
finally{try{Directory.Delete(folder,true);}catch(IOException){}}

static string Format(double value)=>Math.Abs(value)>=100 ? value.ToString("N0",CultureInfo.InvariantCulture):value.ToString("0.000",CultureInfo.InvariantCulture);
static bool IsRelease()
{
#if DEBUG
    return false;
#else
    return true;
#endif
}

/// <summary>Invented facts, shaped like real ones.</summary>
static class Invented
{
    // Kind mix: the shared ticket #43 (1 453 facts) and a real local base (1 772 facts) added together (2026-11-10), then 5 for each kind neither holds,
    // so that every kind's code path runs. Subjects: about how many the game has for that kind (monsters, FATEs, fish, items, NPCs…).
    private static readonly (DiscoveryKind Kind,int Weight,int Subjects,int Details,int Lowest,int Highest)[] Kinds=
    [
        (DiscoveryKind.MonsterPlace,2278,12_000,2,1,100),(DiscoveryKind.FateSeen,212,2_600,1,1,100),(DiscoveryKind.SpearShadow,94,300,3,1,300),
        (DiscoveryKind.FishBite,105,1_600,4,10,900),(DiscoveryKind.FishCatch,102,1_600,120,1,1),(DiscoveryKind.FishCondition,92,1_600,150,0,1439),
        (DiscoveryKind.FishTransition,84,1_600,150,0,1439),(DiscoveryKind.Desynthesis,66,9_000,40_000,1,5),(DiscoveryKind.FashionEvaluation,62,10_000,12,0,255),
        (DiscoveryKind.FishBiteBait,33,1_600,120,10,900),(DiscoveryKind.FishLure,31,1_600,2,1,1),(DiscoveryKind.NpcPlace,29,3_000,1,0,0),
        (DiscoveryKind.FestivalSeen,12,150,4,1,5000),(DiscoveryKind.VoyageDrop,10,3_000,150,1,10),(DiscoveryKind.Venture,8,2_000,700,1,500),
        (DiscoveryKind.HuntMarkPlace,7,400,1,1,3),
        (DiscoveryKind.DutyChest,5,8_000,900,1,3),(DiscoveryKind.FateReward,5,2_000,2_600,1,5),(DiscoveryKind.MonsterDrop,5,8_000,12_000,1,5),
        (DiscoveryKind.ItemFromItem,5,3_000,3_000,1,5),(DiscoveryKind.VoyageUnlock,5,150,150,1,1),(DiscoveryKind.BlueSpellLearned,5,124,12_000,0,0),
        (DiscoveryKind.BeastTamed,5,200,12_000,0,0),(DiscoveryKind.VistaSeen,5,300,150,0,1439),(DiscoveryKind.BlueSpellCaster,5,124,12_000,1,100),
        (DiscoveryKind.Reduction,5,300,300,1,10),(DiscoveryKind.QuestItemAsked,5,10_000,5_000,1,10),(DiscoveryKind.IntuitionStart,5,1_600,1,1,600),
        (DiscoveryKind.IntuitionCatch,5,1_600,1_600,0,0),(DiscoveryKind.VoyageVisit,5,150,2,1,200),(DiscoveryKind.MonsterKill,5,12_000,1,1,100),
        (DiscoveryKind.DutyChestOpened,5,900,1,0,0),(DiscoveryKind.SpectralBefore,5,300,300,0,0),(DiscoveryKind.FishPoints,5,300,1,1,2000),
        (DiscoveryKind.AirshipDrop,5,600,40,1,10)
    ];
    private const int Maps=450;

    /// <summary><paramref name="size"/> distinct facts (distinct keys); yours (Seen &gt; 0) or shared (Seen 0, as an imported common file holds them).</summary>
    public static List<DiscoveryFact> Facts(int size,bool own,int seed)
    {
        var random=new Random(seed);var total=Kinds.Sum(kind=>kind.Weight);
        var keys=new HashSet<string>(size,StringComparer.Ordinal);var facts=new List<DiscoveryFact>(size);
        var now=DateTimeOffset.UtcNow;var fullKinds=new HashSet<DiscoveryKind>();
        while(facts.Count<size)
        {
            var pick=random.Next(total);var spec=Kinds[0];
            foreach(var kind in Kinds){if(pick<kind.Weight){spec=kind;break;}pick-=kind.Weight;}
            if(fullKinds.Contains(spec.Kind))continue;
            DiscoveryFact? made=null;
            for(var attempt=0;attempt<20 && made is null;attempt++)
            {
                var fact=One(spec,random,now,own);
                if(keys.Add(fact.Key))made=fact;
            }
            if(made is null){fullKinds.Add(spec.Kind);if(fullKinds.Count==Kinds.Length)throw new InvalidOperationException("Not enough distinct facts.");continue;}
            facts.Add(made);
        }
        return facts;
    }

    private static DiscoveryFact One((DiscoveryKind Kind,int Weight,int Subjects,int Details,int Lowest,int Highest) spec,Random random,DateTimeOffset now,bool own)
    {
        // Popular subjects come up more often (squared draw), as in a real base.
        var draw=random.NextDouble();var subject=(uint)(draw*draw*spec.Subjects)+1;
        var detail=spec.Kind==DiscoveryKind.MonsterPlace ? subject*3%30_000+1+(uint)random.Next(spec.Details):(uint)random.Next(spec.Details)+(spec.Details==1 ? 0u:1u);
        uint territory=0,map=0;float x=0,z=0;
        if(DiscoveryStore.HasPlace(spec.Kind))
        {
            // A subject lives on one to three maps, around one centre per map (monsters roam, fish holes and NPCs stay put).
            map=(uint)((subject*7919+(uint)random.Next(3))%Maps)+1;territory=map+100;
            var centreX=(float)((subject*131+map*17)%1600)-800;var centreZ=(float)((subject*197+map*29)%1600)-800;
            var spread=spec.Kind is DiscoveryKind.MonsterPlace or DiscoveryKind.HuntMarkPlace or DiscoveryKind.FateSeen ? 160f:40f;
            x=Math.Clamp(centreX+Gauss(random)*spread,-1000f,1000f);z=Math.Clamp(centreZ+Gauss(random)*spread,-1000f,1000f);
        }
        else if(spec.Kind is DiscoveryKind.FishCondition or DiscoveryKind.FishTransition or DiscoveryKind.FashionEvaluation or DiscoveryKind.SpectralBefore or DiscoveryKind.FishPoints)
            territory=(uint)((subject*7919+(uint)random.Next(2))%Maps)+101;
        var (cellX,cellZ)=DiscoveryStore.Cell(x,z);
        // Sightings: 64 % once, 18 % twice, 8 % three times, the rest up to 40 (the local base's shape).
        var roll=random.NextDouble();var seen=roll<0.64 ? 1:roll<0.82 ? 2:roll<0.90 ? 3:4+random.Next(37);
        var last=now.AddMinutes(-random.Next(24*60));var first=last.AddMinutes(-random.Next(20*24*60));
        Dictionary<int,int>? values=null;var lowest=int.MaxValue;var highest=0;
        if(spec.Highest>0)
        {
            var centre=spec.Lowest+random.Next(spec.Highest-spec.Lowest+1);
            for(var sighting=0;sighting<seen;sighting++)
            {
                var value=Math.Clamp(centre+(spec.Highest>spec.Lowest ? random.Next(-(spec.Highest-spec.Lowest)/20-1,(spec.Highest-spec.Lowest)/20+2):0),spec.Lowest,spec.Highest);
                ValueCounts.Add(ref values,value);lowest=Math.Min(lowest,value);highest=Math.Max(highest,value);
            }
        }
        else lowest=0;
        var hq=values is not null && spec.Kind is DiscoveryKind.FishCatch or DiscoveryKind.MonsterDrop or DiscoveryKind.DutyChest or DiscoveryKind.Desynthesis && random.NextDouble()<0.1
            ? random.Next(1,(int)ValueCounts.Total(values)+1):0;
        var fact=new DiscoveryFact{Kind=spec.Kind,Subject=subject,Detail=detail,Territory=territory,Map=map,CellX=cellX,CellZ=cellZ,X=x,Z=z,LevelMin=lowest,LevelMax=highest,First=first,Last=last};
        if(own){fact.Seen=seen;fact.Values=values;fact.Hq=hq;}
        else
        {
            // As the common file and an import hold them: Seen 0, Imported = players who have it (most facts: one player).
            var players=random.NextDouble() is var share && share<0.7 ? 1:share<0.9 ? 2+random.Next(2):4+random.Next(17);
            fact.Seen=0;fact.Imported=players;fact.ImportedValues=values;fact.ImportedHq=hq;
        }
        return fact;
    }
    private static float Gauss(Random random)=>(float)(Math.Sqrt(-2*Math.Log(1-random.NextDouble()))*Math.Cos(2*Math.PI*random.NextDouble()));
}

/// <summary>The measures; each one calls the plugin's code as the plugin calls it (MainWindow.Discoveries.cs, Plugin.cs, GitHubExchange.cs).</summary>
static class Measure
{
    public static void Settle(){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}

    private static double Time(Action action){var watch=Stopwatch.StartNew();action();return watch.Elapsed.TotalMilliseconds;}
    /// <summary>The median time of one call, over <paramref name="count"/> calls of <paramref name="action"/>(i).</summary>
    private static double PerCall(int count,Action<int> action)
    {
        var times=new double[count];
        for(var index=0;index<count;index++){var watch=Stopwatch.StartNew();action(index);times[index]=watch.Elapsed.TotalMilliseconds;}
        Array.Sort(times);return times[count/2];
    }

    /// <summary>Your own base: Discoveries.json read at start (Plugin.cs, off the frame), the snapshot taken on the frame every minute, the split write off it.</summary>
    public static void Own(List<DiscoveryFact> facts,string folder,Action<string,double> add)
    {
        var path=Path.Combine(folder,"own","Discoveries.json");Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var at=DateTimeOffset.UtcNow;
        add("own: 1 file write, DiscoveryFiles.Write (ms)",Time(()=>DiscoveryFiles.Write(path,new DiscoveryFile{Written=at,Facts=facts})));
        var bytes=new FileInfo(path).Length;
        add("own: 2 Discoveries.json size (MB)",bytes/1_048_576.0);
        add("own: 2 Discoveries.json bytes a fact",bytes/(double)facts.Count);
        add("own: 2 Discoveries.json over DiscoveryFiles.MaxBytes (1 yes)",bytes>DiscoveryFiles.MaxBytes ? 1:0);
        facts.Clear();Settle();
        // DiscoveryFiles.Read returns null past its 64 MB bound (the plugin then starts empty); the JSON is read past the bound to time it all the same.
        DiscoveryFile? ReadOwn()=>DiscoveryFiles.Read(path) ?? JsonSerializer.Deserialize<DiscoveryFile>(File.ReadAllText(path));
        // Memory: the JSON reader keeps its large buffers pooled after a first read (ArrayPool), for the life of the process: counted apart, then the store
        // measured from a baseline taken after that first read, so that only its own copies count.
        var cold=GC.GetTotalMemory(true);
        var warm=ReadOwn();warm=null;Settle();var before=GC.GetTotalMemory(true);
        add("own: 3 buffers the JSON reader keeps after a read (MB)",(before-cold)/1_048_576.0);
        DiscoveryFile? file=null;
        add("own: 3 file read, DiscoveryFiles.Read (ms)",Time(()=>file=DiscoveryFiles.Read(path)));
        if(file is null)add("own: 3 file read past the bound, same deserializer (ms)",Time(()=>file=JsonSerializer.Deserialize<DiscoveryFile>(File.ReadAllText(path))));
        var store=new DiscoveryStore();
        add("own: 4 DiscoveryStore.Load (ms, holds the lock)",Time(()=>store.Load(file!)));
        file=null;Settle();var after=GC.GetTotalMemory(true);
        add("own: 5 memory held by the store (MB)",(after-before)/1_048_576.0);
        add("own: 5 memory a fact (bytes)",(after-before)/(double)Total(store));
        DiscoveryFile? snapshot=null;
        add("own: 6 Snapshot (ms, ON THE FRAME every minute when dirty)",Time(()=>snapshot=store.Snapshot(DateTimeOffset.UtcNow)));
        var written=new Dictionary<DiscoveryKind,string>();
        add("own: 7 WriteSplit (ms, off the frame)",Time(()=>DiscoveryFiles.WriteSplit(path,snapshot!,written,store.SharedKindsHeld())));
        // The share sent to your gist (GitHubExchange: Snapshot, Mine, Encode), off the frame.
        string gist="";
        add("own: 8 gist encode, Mine+Encode (ms, off the frame)",Time(()=>gist=CommunityDiscoveries.Encode(CommunityDiscoveries.Mine(snapshot!))));
        add("own: 8 gist text size (MB)",gist.Length/1_048_576.0);
        snapshot=null;gist="";Settle();
        // One more sighting on a full base (the parsers call this on the frame or their own thread).
        var random=new Random(7);
        add("own: 9 SeeSource, new fact (ms a call)",PerCall(2_000,index=>store.SeeSource(DiscoveryKind.FishCatch,(uint)(900_000+index),1,1,DateTimeOffset.UtcNow,500,400)));
        Reads("own",store,add);
    }

    /// <summary>The shared base: the common file joined (GitHubExchange: download, Parse, Merge, off the frame but under the store's lock), then written apart and read again by kind.</summary>
    public static void Shared(List<DiscoveryFact> facts,string folder,Action<string,double> add)
    {
        var common=JsonSerializer.Serialize(new DiscoveryFile{Written=DateTimeOffset.UtcNow,Producer="Tensho Assistant community",Facts=facts});
        add("shared: 1 common file size (MB)",common.Length/1_048_576.0);
        add("shared: 1 common file over CommunityDiscoveries.MaxBytes (1 yes)",common.Length>CommunityDiscoveries.MaxBytes ? 1:0);
        facts.Clear();Settle();
        DiscoveryFile? file=null;
        add("shared: 2 CommunityDiscoveries.Parse (ms)",Time(()=>file=CommunityDiscoveries.Parse(common)));
        common="";Settle();
        var store=new DiscoveryStore();
        var first=(Added:0,Joined:0,Refused:0);var second=first;
        add("shared: 3 Merge into an empty base (ms, holds the lock)",Time(()=>first=store.Merge(file!.Facts)));
        add("shared: 3 facts held after the merge",Total(store));
        add("shared: 3 facts left out by the 200 000-per-kind bound",store.LeftOut);
        add("shared: 4 Merge again, all joined (ms, holds the lock)",Time(()=>second=store.Merge(file!.Facts)));
        file=null;Settle();
        var path=Path.Combine(folder,"shared","Discoveries.json");Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var snapshot=store.Snapshot(DateTimeOffset.UtcNow);var written=new Dictionary<DiscoveryKind,string>();
        add("shared: 5 WriteSplit, kind files gzip (ms, off the frame)",Time(()=>DiscoveryFiles.WriteSplit(path,snapshot,written,store.SharedKindsHeld())));
        add("shared: 5 WriteSplit again, nothing changed (ms)",Time(()=>DiscoveryFiles.WriteSplit(path,snapshot,written,store.SharedKindsHeld())));
        var kindFiles=Directory.GetFiles(DiscoveryFiles.SharedFolder(path),"kind-*.json.gz").Select(name=>new FileInfo(name).Length).ToArray();
        add("shared: 5 kind files, total gzip (MB)",kindFiles.Sum()/1_048_576.0);
        add("shared: 5 largest kind file, gzip (MB)",kindFiles.Max()/1_048_576.0);
        snapshot=null;
        var held=Total(store);store=null;Settle();
        // At start, a kind's file is read when a page first asks for it (UseSharedSource): the largest one, MonsterPlace, read and loaded.
        var before=GC.GetTotalMemory(true);
        var lazy=new DiscoveryStore();DiscoveryFile? kind=null;
        add("shared: 6 kind 1 file read, ReadSharedKind (ms, off the frame)",Time(()=>kind=DiscoveryFiles.ReadSharedKind(path,DiscoveryKind.MonsterPlace,(_,error)=>throw error)));
        add("shared: 6 kind 1 file loaded (ms, holds the lock)",Time(()=>lazy.Load(kind!)));
        kind=null;
        foreach(var other in Enum.GetValues<DiscoveryKind>().Where(other=>other!=DiscoveryKind.MonsterPlace))
            if(DiscoveryFiles.ReadSharedKind(path,other,(_,error)=>throw error) is { } more)lazy.Load(more);
        Settle();var after=GC.GetTotalMemory(true);
        add("shared: 7 memory, every kind held (MB)",(after-before)/1_048_576.0);
        add("shared: 7 memory a fact (bytes)",(after-before)/(double)Math.Max(1,held));
        Reads("shared",lazy,add);
    }

    private static int Total(DiscoveryStore store)=>store.Summary().Sum(row=>row.Total);

    // MainWindow.Discoveries.cs's lists, copied: ItemSourceKinds without the kinds DiscoveredSourcesOf leaves out, and OtherSeenKinds.
    private static readonly DiscoveryKind[] TooltipKinds=[DiscoveryKind.Reduction,DiscoveryKind.Desynthesis,DiscoveryKind.Venture,DiscoveryKind.DutyChest,DiscoveryKind.FateReward,
        DiscoveryKind.MonsterDrop,DiscoveryKind.ItemFromItem,DiscoveryKind.FishCatch,DiscoveryKind.VoyageDrop,DiscoveryKind.AirshipDrop];
    private static readonly DiscoveryKind[] SearchKinds=[DiscoveryKind.Reduction,DiscoveryKind.Desynthesis,DiscoveryKind.Venture,DiscoveryKind.DutyChest,DiscoveryKind.FateReward,
        DiscoveryKind.MonsterDrop,DiscoveryKind.ItemFromItem,DiscoveryKind.FishCatch,DiscoveryKind.FishCondition,DiscoveryKind.FashionEvaluation,DiscoveryKind.VoyageDrop,DiscoveryKind.AirshipDrop,
        DiscoveryKind.VoyageUnlock,DiscoveryKind.BlueSpellLearned,DiscoveryKind.BeastTamed,DiscoveryKind.FestivalSeen,DiscoveryKind.VistaSeen,
        DiscoveryKind.NpcPlace,DiscoveryKind.BlueSpellCaster,DiscoveryKind.FishLure,DiscoveryKind.FishTransition,DiscoveryKind.FishBite,
        DiscoveryKind.QuestItemAsked,DiscoveryKind.SpearShadow,DiscoveryKind.FishBiteBait,DiscoveryKind.IntuitionStart,DiscoveryKind.IntuitionCatch,DiscoveryKind.VoyageVisit,DiscoveryKind.MonsterKill,
        DiscoveryKind.DutyChestOpened,DiscoveryKind.SpectralBefore,DiscoveryKind.FishPoints];

    /// <summary>The reads of the pages and tooltips, with the same store calls and arguments as MainWindow.Discoveries.cs (names: a cached lookup, as there).</summary>
    private static void Reads(string label,DiscoveryStore store,Action<string,double> add)
    {
        var names=new Dictionary<uint,string>();
        string Name(uint id){if(!names.TryGetValue(id,out var name))names[id]=name="Name "+id.ToString(CultureInfo.InvariantCulture);return name;}
        // Warm-up: one of each, so that the first call's JIT is not counted.
        store.Summary();store.Find(DiscoveryKind.FishCatch,1u,16);store.Find(DiscoveryKind.MonsterDrop,_=>true,20_000);
        // DrawDiscoveries: Summary() on every frame while the Discoveries page is open.
        add($"{label}: R1 page, Summary() (ms a frame, page open)",PerCall(15,_=>store.Summary()));
        // SeenFishLine (fishing help, one row per fish, cached per fish until the base changes): its seven searches.
        add($"{label}: R2 tooltip, SeenFishLine searches (ms a fish)",PerCall(200,index=>
        {
            var fish=(uint)(index*7%1_600+1);
            store.Find(DiscoveryKind.FishCatch,fish,16);store.Find(DiscoveryKind.FishTransition,subject=>subject==fish,8);store.Find(DiscoveryKind.FishCondition,subject=>subject==fish,8);
            store.Find(DiscoveryKind.FishCondition,fish,64);store.Find(DiscoveryKind.FishBite,fish,32);store.Find(DiscoveryKind.FishLure,fish,2);store.Find(DiscoveryKind.SpearShadow,fish,8);
        }));
        // DiscoveredSourcesOf (item tooltips and item pages, one item cached until the base changes or another item is shown).
        add($"{label}: R3 tooltip, DiscoveredSourcesOf searches (ms an item)",PerCall(200,index=>
        {
            var item=(uint)(index*13%8_000+1);
            _=TooltipKinds.SelectMany(kind=>store.Find(kind,subject=>subject==item,4)).OrderByDescending(DiscoveryFact.TimesOf).Take(4).ToArray();
        }));
        // SeenNpcPlace (vendor places), SeenPlacesOf (monster places on the hunt, map and item pages).
        add($"{label}: R4 tooltip, SeenNpcPlace (ms an NPC)",PerCall(500,index=>store.Find(DiscoveryKind.NpcPlace,(uint)(index%3_000+1),1)));
        add($"{label}: R5 page, SeenPlacesOf one monster (ms)",PerCall(200,index=>{var monster=(uint)(index*11%12_000+1);SeenPlaces.ByMap(store.Find(DiscoveryKind.MonsterPlace,subject=>subject==monster,500),4);}));
        // Seen(): the drop, desynthesis and chest indexes, worked out again on the frame after every change of the base (revision).
        add($"{label}: R6 Seen() index rebuilt (ms after each change)",PerCall(5,_=>
        {
            static Dictionary<uint,uint[]> By(IEnumerable<DiscoveryFact> facts,Func<DiscoveryFact,uint> key,Func<DiscoveryFact,uint> value)=>
                facts.Where(fact=>key(fact)!=0 && value(fact)!=0).GroupBy(key).ToDictionary(group=>group.Key,group=>group.Select(value).Distinct().Take(32).ToArray());
            var drops=store.Find(DiscoveryKind.MonsterDrop,_=>true,20_000);
            var fromItems=store.Find(DiscoveryKind.Desynthesis,_=>true,20_000).Concat(store.Find(DiscoveryKind.ItemFromItem,_=>true,20_000)).Concat(store.Find(DiscoveryKind.Reduction,_=>true,20_000)).ToArray();
            var chests=store.Find(DiscoveryKind.DutyChest,_=>true,20_000);
            GC.KeepAlive((By(drops,fact=>fact.Detail,fact=>fact.Subject),By(drops,fact=>fact.Subject,fact=>fact.Detail),By(store.Find(DiscoveryKind.BeastTamed,_=>true,2_000),fact=>fact.Detail,fact=>fact.Subject),
                By(fromItems,fact=>fact.Detail,fact=>fact.Subject),By(fromItems,fact=>fact.Subject,fact=>fact.Detail),By(chests,fact=>fact.Subject,fact=>fact.Detail),By(chests,fact=>fact.Detail,fact=>fact.Subject)));
        }));
        // DrawDiscoveryPlaces: the page's search, worked out again on each new text or change of the base.
        add($"{label}: R7 page search, DrawDiscoveryPlaces rows (ms a search)",PerCall(5,index=>
        {
            var wanted="Name 1"+index.ToString(CultureInfo.InvariantCulture);
            _=store.Find(DiscoveryKind.MonsterPlace,id=>Name(id).Contains(wanted,StringComparison.CurrentCultureIgnoreCase),200)
                .Concat(store.Find(DiscoveryKind.HuntMarkPlace,id=>Name(id).Contains(wanted,StringComparison.CurrentCultureIgnoreCase),100))
                .Concat(store.Find(DiscoveryKind.FateSeen,id=>Name(id).Contains(wanted,StringComparison.CurrentCultureIgnoreCase),100))
                .Concat(SearchKinds.SelectMany(kind=>store.Find(kind,_=>true,20_000)).Where(fact=>Name(fact.Subject).Contains(wanted,StringComparison.CurrentCultureIgnoreCase)).Take(200)).ToList();
        }));
        // No page reads the facts by zone today; for reference, what one would cost with the store's present API (every kind read, then the zone kept).
        add($"{label}: R8 reference, one zone through Find (ms, no such reader yet)",PerCall(3,index=>
        {
            var territory=(uint)(index*37%450+101);
            _=Enum.GetValues<DiscoveryKind>().SelectMany(kind=>store.Find(kind,_=>true,int.MaxValue)).Where(fact=>fact.Territory==territory).ToList();
        }));
    }
}
