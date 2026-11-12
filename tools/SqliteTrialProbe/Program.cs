using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// X-172 part 1 (docs/evidence/x172-sqlite-trial.md): should the local storage move to SQLite? The discoveries base is measured both ways with the
// plugin's own code for the present files (DiscoveryFiles.WriteSplit / ReadBase / ReadShared, DiscoveryStore.Load) and one SQLite file (WAL,
// synchronous=NORMAL) for the other; then a writer killed in the middle of a save, a reader in another process while the game writes (the desktop
// application's case), and the download cache (DataCache's one JSON file per entry) against BLOB rows. Each measure of a storage runs in its own
// process, so that its memory is its own. Offline; writes only to a temporary folder, removed at the end. Not run by Verify (minutes, gigabytes).
// Usage: dotnet run -c Release -- [--sizes 100000,1000000] [--runs 3] [--kills 20] [--seconds 10] [--trial 100000] [--json out.json]
CultureInfo.DefaultThreadCurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
if(args.Length>0 && args[0].StartsWith("--child-",StringComparison.Ordinal))return Child.Run(args);
return Parent.Run(args);

/// <summary>Invented facts, shaped like real ones: DiscoveryLoadProbe's table and drawing (tools/DiscoveryLoadProbe/Program.cs), copied; half yours, half shared.</summary>
static class Invented
{
    public static readonly DateTimeOffset Now=new(2026,11,12,0,0,0,TimeSpan.Zero);
    // Same kind mix and subject bounds as DiscoveryLoadProbe (the shared ticket #43 and a real local base, then 5 for each kind neither holds).
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

    /// <summary><paramref name="size"/> distinct facts, every second one yours (Seen &gt; 0) and the others shared (Seen 0, as the common file holds them).</summary>
    public static List<DiscoveryFact> Facts(int size,int seed)
    {
        var random=new Random(seed);var total=Kinds.Sum(kind=>kind.Weight);
        var keys=new HashSet<string>(size,StringComparer.Ordinal);var facts=new List<DiscoveryFact>(size);
        var fullKinds=new HashSet<DiscoveryKind>();
        while(facts.Count<size)
        {
            var pick=random.Next(total);var spec=Kinds[0];
            foreach(var kind in Kinds){if(pick<kind.Weight){spec=kind;break;}pick-=kind.Weight;}
            if(fullKinds.Contains(spec.Kind))continue;
            DiscoveryFact? made=null;
            for(var attempt=0;attempt<20 && made is null;attempt++)
            {
                var fact=One(spec,random,facts.Count%2==0);
                if(keys.Add(fact.Key))made=fact;
            }
            if(made is null){fullKinds.Add(spec.Kind);if(fullKinds.Count==Kinds.Length)throw new InvalidOperationException("Not enough distinct facts.");continue;}
            facts.Add(made);
        }
        return facts;
    }

    private static DiscoveryFact One((DiscoveryKind Kind,int Weight,int Subjects,int Details,int Lowest,int Highest) spec,Random random,bool own)
    {
        var draw=random.NextDouble();var subject=(uint)(draw*draw*spec.Subjects)+1;
        var detail=spec.Kind==DiscoveryKind.MonsterPlace ? subject*3%30_000+1+(uint)random.Next(spec.Details):(uint)random.Next(spec.Details)+(spec.Details==1 ? 0u:1u);
        uint territory=0,map=0;float x=0,z=0;
        if(DiscoveryStore.HasPlace(spec.Kind))
        {
            map=(uint)((subject*7919+(uint)random.Next(3))%Maps)+1;territory=map+100;
            var centreX=(float)((subject*131+map*17)%1600)-800;var centreZ=(float)((subject*197+map*29)%1600)-800;
            var spread=spec.Kind is DiscoveryKind.MonsterPlace or DiscoveryKind.HuntMarkPlace or DiscoveryKind.FateSeen ? 160f:40f;
            x=Math.Clamp(centreX+Gauss(random)*spread,-1000f,1000f);z=Math.Clamp(centreZ+Gauss(random)*spread,-1000f,1000f);
        }
        else if(spec.Kind is DiscoveryKind.FishCondition or DiscoveryKind.FishTransition or DiscoveryKind.FashionEvaluation or DiscoveryKind.SpectralBefore or DiscoveryKind.FishPoints)
            territory=(uint)((subject*7919+(uint)random.Next(2))%Maps)+101;
        var (cellX,cellZ)=DiscoveryStore.Cell(x,z);
        var roll=random.NextDouble();var seen=roll<0.64 ? 1:roll<0.82 ? 2:roll<0.90 ? 3:4+random.Next(37);
        var last=Now.AddMinutes(-random.Next(24*60));var first=last.AddMinutes(-random.Next(20*24*60));
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
        if(own)
        {
            fact.Seen=seen;fact.Values=values;fact.Hq=hq;
            // One in twenty of yours was also imported (X-167: your part kept apart in Own), so that every column is filled somewhere.
            if(random.NextDouble()<0.05)
            {
                fact.Imported=1+random.Next(3);fact.ImportedValues=values is null ? null:new(values);
                fact.Own=new OwnPart{First=first,Last=last,LevelMin=lowest,LevelMax=highest,X=x,Z=z};
            }
        }
        else
        {
            var players=random.NextDouble() is var share && share<0.7 ? 1:share<0.9 ? 2+random.Next(2):4+random.Next(17);
            fact.Seen=0;fact.Imported=players;fact.ImportedValues=values;fact.ImportedHq=hq;
        }
        return fact;
    }
    private static float Gauss(Random random)=>(float)(Math.Sqrt(-2*Math.Log(1-random.NextDouble()))*Math.Cos(2*Math.PI*random.NextDouble()));

    /// <summary>
    /// <paramref name="count"/> facts seen once more (one minute of play would be far fewer): Seen + 1, Last, one value; a shared fact seen by you becomes
    /// yours (Seen 1, your part in Own), as DiscoveryStore.SeeSource does. <paramref name="ownOnly"/>: only facts already yours (the file sizes stay put).
    /// </summary>
    public static List<DiscoveryFact> Change(List<DiscoveryFact> facts,int count,Random random,bool ownOnly)
    {
        var picked=new HashSet<int>();var changed=new List<DiscoveryFact>(count);var at=Now.AddDays(1);
        while(changed.Count<count)
        {
            var index=random.Next(facts.Count);var fact=facts[index];
            if(ownOnly && fact.Seen==0 || !picked.Add(index))continue;
            if(fact.Seen==0){fact.Seen=1;fact.Own=new OwnPart{First=at,Last=at,LevelMin=fact.LevelMax,LevelMax=fact.LevelMax,X=fact.X,Z=fact.Z};}
            else fact.Seen++;
            fact.Last=at;
            if(fact.LevelMax>0 || fact.Values is not null){var values=fact.Values;ValueCounts.Add(ref values,fact.LevelMax);fact.Values=values;}
            changed.Add(fact);
        }
        return changed;
    }
}

/// <summary>The SQLite side: one file, WAL, synchronous=NORMAL, one row per fact keyed by the fact's key columns (kind first: the key is the index on kind).</summary>
static class Db
{
    private static readonly string[] Columns=["kind","subject","detail","territory","map","cellx","cellz","x","z","levelmin","levelmax","seen","imported","first","last","hq","importedhq","vals","importedvals","own"];
    private const string KeyColumns="kind,subject,detail,territory,map,cellx,cellz";
    // WITHOUT ROWID: the table is stored in key order, so the rows of one kind are one range of the file and no second copy of the key is kept.
    public const string Schema="CREATE TABLE IF NOT EXISTS facts(kind INTEGER NOT NULL,subject INTEGER NOT NULL,detail INTEGER NOT NULL,territory INTEGER NOT NULL,"
        +"map INTEGER NOT NULL,cellx INTEGER NOT NULL,cellz INTEGER NOT NULL,x REAL NOT NULL,z REAL NOT NULL,levelmin INTEGER NOT NULL,levelmax INTEGER NOT NULL,"
        +"seen INTEGER NOT NULL,imported INTEGER NOT NULL,first INTEGER NOT NULL,last INTEGER NOT NULL,hq INTEGER NOT NULL,importedhq INTEGER NOT NULL,"
        +"vals TEXT,importedvals TEXT,own TEXT,PRIMARY KEY("+KeyColumns+")) WITHOUT ROWID";

    /// <summary>A connection as the plugin would hold one (pooling off, so that a closed connection lets the file go at once).</summary>
    public static SqliteConnection Open(string path,bool readOnly=false,string? schema=Schema)
    {
        var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=readOnly ? SqliteOpenMode.ReadOnly:SqliteOpenMode.ReadWriteCreate,
            Pooling=false,DefaultTimeout=2}.ToString());
        connection.Open();
        if(!readOnly)Exec(connection,"PRAGMA journal_mode=WAL");
        Exec(connection,"PRAGMA synchronous=NORMAL");
        Exec(connection,"PRAGMA busy_timeout=2000");
        if(!readOnly && schema is not null)Exec(connection,schema);
        return connection;
    }
    public static void Exec(SqliteConnection connection,string sql){using var command=connection.CreateCommand();command.CommandText=sql;command.ExecuteNonQuery();}
    public static object? Scalar(SqliteConnection connection,string sql){using var command=connection.CreateCommand();command.CommandText=sql;return command.ExecuteScalar();}
    public static void Checkpoint(SqliteConnection connection)=>Scalar(connection,"PRAGMA wal_checkpoint(TRUNCATE)");
    public static long Bytes(string path)=>(File.Exists(path) ? new FileInfo(path).Length:0)+(File.Exists(path+"-wal") ? new FileInfo(path+"-wal").Length:0);
    public static long Wal(string path)=>File.Exists(path+"-wal") ? new FileInfo(path+"-wal").Length:0;

    /// <summary>Inserts or updates the facts: in one transaction, or (<paramref name="transaction"/> false) each in its own, as a save at every sighting would.</summary>
    public static void Upsert(SqliteConnection connection,IEnumerable<DiscoveryFact> facts,bool transaction=true)
    {
        using var scope=transaction ? connection.BeginTransaction():null;
        using var command=connection.CreateCommand();command.Transaction=scope;
        command.CommandText=$"INSERT INTO facts({string.Join(',',Columns)}) VALUES({string.Join(',',Columns.Select(name=>"$"+name))}) ON CONFLICT({KeyColumns}) DO UPDATE SET "
            +string.Join(',',Columns.Skip(7).Select(name=>$"{name}=excluded.{name}"));
        var parameters=Columns.Select(name=>command.Parameters.Add("$"+name,name is "x" or "z" ? SqliteType.Real:name is "vals" or "importedvals" or "own" ? SqliteType.Text:SqliteType.Integer)).ToArray();
        command.Prepare();
        foreach(var fact in facts)
        {
            parameters[0].Value=(int)fact.Kind;parameters[1].Value=(long)fact.Subject;parameters[2].Value=(long)fact.Detail;parameters[3].Value=(long)fact.Territory;
            parameters[4].Value=(long)fact.Map;parameters[5].Value=fact.CellX;parameters[6].Value=fact.CellZ;parameters[7].Value=(double)fact.X;parameters[8].Value=(double)fact.Z;
            parameters[9].Value=fact.LevelMin;parameters[10].Value=fact.LevelMax;parameters[11].Value=fact.Seen;parameters[12].Value=fact.Imported;
            parameters[13].Value=fact.First.UtcTicks;parameters[14].Value=fact.Last.UtcTicks;parameters[15].Value=fact.Hq;parameters[16].Value=fact.ImportedHq;
            parameters[17].Value=fact.Values is null ? DBNull.Value:JsonSerializer.Serialize(fact.Values);
            parameters[18].Value=fact.ImportedValues is null ? DBNull.Value:JsonSerializer.Serialize(fact.ImportedValues);
            parameters[19].Value=fact.Own is null ? DBNull.Value:JsonSerializer.Serialize(fact.Own);
            command.ExecuteNonQuery();
        }
        scope?.Commit();
    }

    /// <summary>Every fact, or one kind's (a range of the key).</summary>
    public static List<DiscoveryFact> Read(SqliteConnection connection,DiscoveryKind? kind=null)
    {
        using var command=connection.CreateCommand();
        command.CommandText=$"SELECT {string.Join(',',Columns)} FROM facts"+(kind is null ? "":" WHERE kind=$kind");
        if(kind is { } wanted)command.Parameters.AddWithValue("$kind",(int)wanted);
        var facts=new List<DiscoveryFact>();
        using var reader=command.ExecuteReader();
        while(reader.Read())
            facts.Add(new DiscoveryFact{Kind=(DiscoveryKind)reader.GetInt32(0),Subject=(uint)reader.GetInt64(1),Detail=(uint)reader.GetInt64(2),Territory=(uint)reader.GetInt64(3),
                Map=(uint)reader.GetInt64(4),CellX=reader.GetInt32(5),CellZ=reader.GetInt32(6),X=(float)reader.GetDouble(7),Z=(float)reader.GetDouble(8),
                LevelMin=reader.GetInt32(9),LevelMax=reader.GetInt32(10),Seen=reader.GetInt32(11),Imported=reader.GetInt32(12),
                First=new DateTimeOffset(reader.GetInt64(13),TimeSpan.Zero),Last=new DateTimeOffset(reader.GetInt64(14),TimeSpan.Zero),Hq=reader.GetInt32(15),ImportedHq=reader.GetInt32(16),
                Values=reader.IsDBNull(17) ? null:JsonSerializer.Deserialize<Dictionary<int,int>>(reader.GetString(17)),
                ImportedValues=reader.IsDBNull(18) ? null:JsonSerializer.Deserialize<Dictionary<int,int>>(reader.GetString(18)),
                Own=reader.IsDBNull(19) ? null:JsonSerializer.Deserialize<OwnPart>(reader.GetString(19))});
        return facts;
    }
}

/// <summary>Time of an action and the highest managed heap and working set seen while it ran (sampled every 2 ms).</summary>
static class Sampler
{
    public const double MB=1_048_576.0;
    public static void Settle(){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
    public static (double Ms,long PeakManaged,long PeakWorkingSet) Run(Action action)
    {
        long peakManaged=GC.GetTotalMemory(false),peakSet=Environment.WorkingSet;var done=0;
        var thread=new Thread(()=>{while(Volatile.Read(ref done)==0){peakManaged=Math.Max(peakManaged,GC.GetTotalMemory(false));peakSet=Math.Max(peakSet,Environment.WorkingSet);Thread.Sleep(2);}}){IsBackground=true};
        thread.Start();
        var watch=Stopwatch.StartNew();action();var ms=watch.Elapsed.TotalMilliseconds;
        Volatile.Write(ref done,1);thread.Join();
        return (ms,Math.Max(peakManaged,GC.GetTotalMemory(false)),Math.Max(peakSet,Environment.WorkingSet));
    }
    public static double Time(Action action){var watch=Stopwatch.StartNew();action();return watch.Elapsed.TotalMilliseconds;}
}

/// <summary>The measures run in a process of their own (the parent starts them), each printing « M name value » lines.</summary>
static class Child
{
    private static void Out(string name,double value)=>Console.WriteLine($"M\t{name}\t{value.ToString("R",CultureInfo.InvariantCulture)}");
    private static string MainPath(string folder)=>Path.Combine(folder,"Discoveries.json");
    private static long JsonBytes(string folder)=>Directory.Exists(folder) ? Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(path=>!path.EndsWith(".tmp",StringComparison.Ordinal)).Sum(path=>new FileInfo(path).Length):0;

    public static int Run(string[] args)
    {
        switch(args[0])
        {
            case "--child-json-write":JsonWrite(args[1],int.Parse(args[2]),int.Parse(args[3]));return 0;
            case "--child-json-read":JsonRead(args[1]);return 0;
            case "--child-sqlite-write":SqliteWrite(args[1],int.Parse(args[2]),int.Parse(args[3]));return 0;
            case "--child-sqlite-read":SqliteRead(args[1]);return 0;
            case "--child-kill-json":KillJson(args[1],int.Parse(args[2]),int.Parse(args[3]));return 0;
            case "--child-kill-sqlite":KillSqlite(args[1],int.Parse(args[2]),int.Parse(args[3]));return 0;
            case "--child-save-json":SaveJson(args[1],int.Parse(args[2]),int.Parse(args[3]),double.Parse(args[4],CultureInfo.InvariantCulture));return 0;
            case "--child-upsert-sqlite":UpsertSqlite(args[1],int.Parse(args[2]),int.Parse(args[3]),double.Parse(args[4],CultureInfo.InvariantCulture));return 0;
            default:Console.Error.WriteLine("Unknown child: "+args[0]);return 2;
        }
    }

    /// <summary>1. The present save: DiscoveryFiles.WriteSplit of the whole base (yours in Discoveries.json, the shared ones in Shared/kind-NN.json.gz).</summary>
    private static void JsonWrite(string folder,int size,int seed)
    {
        var facts=Invented.Facts(size,seed);var path=MainPath(folder);
        Out("facts",facts.Count);Out("facts yours (Seen > 0)",facts.Count(fact=>fact.Seen>0));
        Sampler.Settle();var managed=GC.GetTotalMemory(true);var set=Environment.WorkingSet;
        var all=new DiscoveryFile{Written=Invented.Now,Facts=facts};var written=new Dictionary<DiscoveryKind,string>();
        var (ms,peakManaged,peakSet)=Sampler.Run(()=>DiscoveryFiles.WriteSplit(path,all,written));
        Out("1 json: full save, WriteSplit (ms)",ms);
        Out("1 json: managed memory added during the save, peak (MB)",(peakManaged-managed)/Sampler.MB);
        Out("1 json: working set added during the save, peak (MB)",(peakSet-set)/Sampler.MB);
        var main=new FileInfo(path).Length;
        Out("1 json: Discoveries.json, yours (MB)",main/Sampler.MB);
        Out("1 json: Discoveries.json over DiscoveryFiles.MaxBytes, 64 MB (1 yes)",main>DiscoveryFiles.MaxBytes ? 1:0);
        Out("1 json: Shared/kind-NN.json.gz, all (MB)",(JsonBytes(folder)-main)/Sampler.MB);
        Out("1 json: size on disk, all files (MB)",JsonBytes(folder)/Sampler.MB);
        // The save pattern: 1 000 facts seen again, then the whole save again (the plugin writes every minute when the base changed).
        var stamps=Directory.GetFiles(folder,"*",SearchOption.AllDirectories).ToDictionary(file=>file,File.GetLastWriteTimeUtc);
        Invented.Change(facts,1_000,new Random(seed+1),false);
        Out("1 json: save after 1 000 changed facts, WriteSplit (ms)",Sampler.Time(()=>DiscoveryFiles.WriteSplit(path,all,written)));
        var rewritten=Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(file=>!stamps.TryGetValue(file,out var at) || File.GetLastWriteTimeUtc(file)!=at).Sum(file=>new FileInfo(file).Length);
        Out("1 json: bytes rewritten by that save (MB)",rewritten/Sampler.MB);
        Out("1 json: peak working set of the writing process (MB)",Process.GetCurrentProcess().PeakWorkingSet64/Sampler.MB);
    }

    /// <summary>1. The present read at start, all of it: ReadBase, then every shared kind file (ReadShared), loaded into a DiscoveryStore.</summary>
    private static void JsonRead(string folder)
    {
        var path=MainPath(folder);
        Sampler.Settle();var managed=GC.GetTotalMemory(true);var set=Environment.WorkingSet;
        DiscoveryStore? store=null;double readBase=0,loadBase=0,shared=0;
        var (ms,peakManaged,peakSet)=Sampler.Run(()=>
        {
            // The measure reads past the 64 MB bound (the plugin would set such a file aside and start empty): its time is what is wanted here.
            (DiscoveryFile? File,string? SetAside,bool Unsafe) read=default;
            readBase=Sampler.Time(()=>read=DiscoveryFiles.ReadBase(path,DateTimeOffset.UtcNow,(_,error)=>throw error,long.MaxValue));
            store=new DiscoveryStore();
            loadBase=Sampler.Time(()=>store.Load(read.File!));
            read=default;
            shared=Sampler.Time(()=>{foreach(var file in DiscoveryFiles.ReadShared(path,(_,error)=>throw error))store.Load(file);});
        });
        Out("1 json: full read, ReadBase + ReadShared + Load (ms)",ms);
        Out("1 json: read part, ReadBase of Discoveries.json (ms)",readBase);
        Out("1 json: read part, Load of your facts (ms)",loadBase);
        Out("1 json: read part, ReadShared + Load of every kind file (ms)",shared);
        Out("1 json: managed memory added during the read, peak (MB)",(peakManaged-managed)/Sampler.MB);
        Out("1 json: working set added during the read, peak (MB)",(peakSet-set)/Sampler.MB);
        Sampler.Settle();
        Out("1 json: managed memory held after the read, store loaded (MB)",(GC.GetTotalMemory(true)-managed)/Sampler.MB);
        Out("1 json: working set after the read (MB)",Environment.WorkingSet/Sampler.MB);
        Out("1 json: peak working set of the reading process (MB)",Process.GetCurrentProcess().PeakWorkingSet64/Sampler.MB);
        Out("1 json: facts held by the store",store!.Summary().Sum(row=>row.Total));
        Out("1 json: shared facts left out by the 200 000-per-kind bound",store.LeftOut);
        // One kind: only the shared facts have a file per kind; yours are all in Discoveries.json (read whole above).
        DiscoveryFile? kind=null;
        Out("1 json: one kind, ReadSharedKind kind 1 MonsterPlace, shared facts only (ms)",Sampler.Time(()=>kind=DiscoveryFiles.ReadSharedKind(path,DiscoveryKind.MonsterPlace,(_,error)=>throw error)));
        Out("1 json: one kind, rows read, kind 1",kind?.Facts.Count ?? 0);
        Out("1 json: one kind, ReadSharedKind kind 10 FishCatch, shared facts only (ms)",Sampler.Time(()=>kind=DiscoveryFiles.ReadSharedKind(path,DiscoveryKind.FishCatch,(_,error)=>throw error)));
        Out("1 json: one kind, rows read, kind 10",kind?.Facts.Count ?? 0);
    }

    /// <summary>2. SQLite: the whole base in one transaction, then the real save pattern: only the 1 000 changed facts.</summary>
    private static void SqliteWrite(string db,int size,int seed)
    {
        var facts=Invented.Facts(size,seed);
        Out("facts",facts.Count);
        Sampler.Settle();var managed=GC.GetTotalMemory(true);var set=Environment.WorkingSet;
        using var connection=Db.Open(db);
        var (ms,peakManaged,peakSet)=Sampler.Run(()=>Db.Upsert(connection,facts));
        Out("2 sqlite: full first write, one transaction (ms)",ms);
        Out("2 sqlite: managed memory added during the write, peak (MB)",(peakManaged-managed)/Sampler.MB);
        Out("2 sqlite: working set added during the write, peak (MB)",(peakSet-set)/Sampler.MB);
        Out("2 sqlite: WAL file after that commit (MB)",Db.Wal(db)/Sampler.MB);
        Out("2 sqlite: checkpoint of that WAL into the database (ms)",Sampler.Time(()=>Db.Checkpoint(connection)));
        Out("2 sqlite: size on disk, database (MB)",Db.Bytes(db)/Sampler.MB);
        var random=new Random(seed+1);
        var changed=Invented.Change(facts,1_000,random,false);
        Out("2 sqlite: save after 1 000 changed facts, upsert in one transaction (ms)",Sampler.Time(()=>Db.Upsert(connection,changed)));
        Out("2 sqlite: WAL file after that save (MB)",Db.Wal(db)/Sampler.MB);
        Db.Checkpoint(connection);
        changed=Invented.Change(facts,1_000,random,false);
        Out("2 sqlite: 1 000 changed facts, each its own transaction (ms, all)",Sampler.Time(()=>{foreach(var fact in changed)Db.Upsert(connection,[fact],false);}));
        Db.Checkpoint(connection);
        Out("2 sqlite: size on disk after the changes and a checkpoint (MB)",Db.Bytes(db)/Sampler.MB);
        Out("2 sqlite: peak working set of the writing process (MB)",Process.GetCurrentProcess().PeakWorkingSet64/Sampler.MB);
    }

    /// <summary>2. SQLite: every row into a DiscoveryStore (as the plugin's start would), then one kind only, then the counts by kind without loading anything.</summary>
    private static void SqliteRead(string db)
    {
        Sampler.Settle();var managed=GC.GetTotalMemory(true);var set=Environment.WorkingSet;
        DiscoveryStore? store=null;double rows=0,load=0;
        var (ms,peakManaged,peakSet)=Sampler.Run(()=>
        {
            using var connection=Db.Open(db,true);
            List<DiscoveryFact>? facts=null;
            rows=Sampler.Time(()=>facts=Db.Read(connection));
            store=new DiscoveryStore();
            load=Sampler.Time(()=>store.Load(new DiscoveryFile{Facts=facts!}));
        });
        Out("2 sqlite: full read, every row + Load (ms)",ms);
        Out("2 sqlite: read part, every row read (ms)",rows);
        Out("2 sqlite: read part, Load into the store (ms)",load);
        Out("2 sqlite: managed memory added during the read, peak (MB)",(peakManaged-managed)/Sampler.MB);
        Out("2 sqlite: working set added during the read, peak (MB)",(peakSet-set)/Sampler.MB);
        Sampler.Settle();
        Out("2 sqlite: managed memory held after the read, store loaded (MB)",(GC.GetTotalMemory(true)-managed)/Sampler.MB);
        Out("2 sqlite: working set after the read (MB)",Environment.WorkingSet/Sampler.MB);
        Out("2 sqlite: peak working set of the reading process (MB)",Process.GetCurrentProcess().PeakWorkingSet64/Sampler.MB);
        Out("2 sqlite: facts held by the store",store!.Summary().Sum(row=>row.Total));
        Out("2 sqlite: shared facts left out by the 200 000-per-kind bound",store.LeftOut);
        using var reader=Db.Open(db,true);
        List<DiscoveryFact>? kind=null;
        Out("2 sqlite: one kind, kind 1 MonsterPlace, yours and shared (ms)",Sampler.Time(()=>kind=Db.Read(reader,DiscoveryKind.MonsterPlace)));
        Out("2 sqlite: one kind, rows read, kind 1",kind!.Count);
        Out("2 sqlite: one kind, kind 10 FishCatch, yours and shared (ms)",Sampler.Time(()=>kind=Db.Read(reader,DiscoveryKind.FishCatch)));
        Out("2 sqlite: one kind, rows read, kind 10",kind!.Count);
        Out("2 sqlite: counts by kind, SELECT kind, count(*) GROUP BY kind (ms)",Sampler.Time(()=>
        {
            using var command=reader.CreateCommand();command.CommandText="SELECT kind,count(*),sum(seen>0) FROM facts GROUP BY kind";
            using var rowsRead=command.ExecuteReader();while(rowsRead.Read()){}
        }));
    }

    /// <summary>
    /// 3. Saves without end (each a full WriteSplit with every kind file rewritten), until the parent kills the process. One whole save first, so that the files
    /// on disk hold this process's base before the parent starts its clock; then shared facts may become yours (X-166: one way only), as in play.
    /// </summary>
    private static void KillJson(string folder,int size,int seed)
    {
        var facts=Invented.Facts(size,seed);var all=new DiscoveryFile{Written=Invented.Now,Facts=facts};var random=new Random(seed+2);
        DiscoveryFiles.WriteSplit(MainPath(folder),all,new Dictionary<DiscoveryKind,string>());
        Console.WriteLine("READY");
        while(true)
        {
            Invented.Change(facts,1_000,random,false);
            Console.WriteLine("B");
            DiscoveryFiles.WriteSplit(MainPath(folder),all,new Dictionary<DiscoveryKind,string>());
            Console.WriteLine("C");
        }
    }

    /// <summary>3. Transactions without end (every row upserted each time, so that a transaction lasts long enough to be cut), until the parent kills the process.</summary>
    private static void KillSqlite(string db,int size,int seed)
    {
        var facts=Invented.Facts(size,seed);var random=new Random(seed+2);
        using var connection=Db.Open(db);
        Console.WriteLine("READY");
        while(true)
        {
            Invented.Change(facts,1_000,random,true);
            Console.WriteLine("B");
            Db.Upsert(connection,facts);
            Console.WriteLine("C");
        }
    }

    /// <summary>4. The game saving in a loop while another process reads: WriteSplit after 1 000 changes of yours, errors counted, for <paramref name="seconds"/>.</summary>
    private static void SaveJson(string folder,int size,int seed,double seconds)
    {
        var facts=Invented.Facts(size,seed);var all=new DiscoveryFile{Written=Invented.Now,Facts=facts};var random=new Random(seed+3);
        var written=new Dictionary<DiscoveryKind,string>();DiscoveryFiles.WriteSplit(MainPath(folder),all,written);
        var errors=new SortedDictionary<string,int>(StringComparer.Ordinal);int saves=0;double longest=0;
        Console.WriteLine("READY");
        var watch=Stopwatch.StartNew();
        while(watch.Elapsed.TotalSeconds<seconds)
        {
            Invented.Change(facts,1_000,random,true);
            var one=Stopwatch.StartNew();
            try{DiscoveryFiles.WriteSplit(MainPath(folder),all,written);saves++;}
            catch(Exception error){errors[error.GetType().Name]=errors.GetValueOrDefault(error.GetType().Name)+1;}
            longest=Math.Max(longest,one.Elapsed.TotalMilliseconds);
        }
        Out("writer saves done",saves);Out("writer longest save (ms)",longest);
        Out("writer errors",errors.Values.Sum());
        foreach(var (name,count) in errors)Out("writer error "+name,count);
    }

    /// <summary>4. The game saving in a loop while another process reads: 1 000 changed facts upserted in one transaction, errors counted, for <paramref name="seconds"/>.</summary>
    private static void UpsertSqlite(string db,int size,int seed,double seconds)
    {
        var facts=Invented.Facts(size,seed);var random=new Random(seed+3);
        using var connection=Db.Open(db);
        var errors=new SortedDictionary<string,int>(StringComparer.Ordinal);int saves=0;double longest=0;
        Console.WriteLine("READY");
        var watch=Stopwatch.StartNew();
        while(watch.Elapsed.TotalSeconds<seconds)
        {
            var changed=Invented.Change(facts,1_000,random,true);
            var one=Stopwatch.StartNew();
            try{Db.Upsert(connection,changed);saves++;}
            catch(SqliteException error){var name="SqliteException "+error.SqliteErrorCode;errors[name]=errors.GetValueOrDefault(name)+1;}
            catch(Exception error){errors[error.GetType().Name]=errors.GetValueOrDefault(error.GetType().Name)+1;}
            longest=Math.Max(longest,one.Elapsed.TotalMilliseconds);
        }
        Out("writer saves done",saves);Out("writer longest save (ms)",longest);
        Out("writer errors",errors.Values.Sum());
        foreach(var (name,count) in errors)Out("writer error "+name,count);
    }
}

static class Parent
{
    private static readonly Dictionary<string,object> Report=[];

    public static int Run(string[] args)
    {
        var sizes=new[]{100_000,1_000_000};var runs=3;var kills=20;var seconds=10.0;var trial=100_000;string? jsonOut=null;
        for(var index=0;index<args.Length;index++)
            switch(args[index])
            {
                case "--sizes":sizes=args[++index].Split(',').Select(int.Parse).ToArray();break;
                case "--runs":runs=int.Parse(args[++index]);break;
                case "--kills":kills=int.Parse(args[++index]);break;
                case "--seconds":seconds=double.Parse(args[++index],CultureInfo.InvariantCulture);break;
                case "--trial":trial=int.Parse(args[++index]);break;
                case "--json":jsonOut=args[++index];break;
                default:Console.Error.WriteLine("Unknown argument: "+args[index]);return 2;
            }
        var root=Path.Combine(Path.GetTempPath(),"tensho-sqlite-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using(var probe=new SqliteConnection("Data Source=:memory:")){probe.Open();Console.WriteLine($"SQLite {Db.Scalar(probe,"SELECT sqlite_version()")}, "
                +$"Microsoft.Data.Sqlite {typeof(SqliteConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}, "
                +$"SQLitePCLRaw {typeof(SQLitePCL.raw).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");}
            Console.WriteLine($"Machine: {Environment.ProcessorCount} logical CPUs, {RuntimeInformation.OSDescription}, .NET {Environment.Version}, {(IsRelease() ? "Release":"Debug")}");
            foreach(var size in sizes)Storage(root,size,runs);
            Kill(root,trial,kills);
            Concurrent(root,trial,seconds);
            Cache(root);
            if(jsonOut is not null)File.WriteAllText(jsonOut,JsonSerializer.Serialize(Report,new JsonSerializerOptions{WriteIndented=true}));
        }
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        return 0;
    }

    private static string Format(double value)=>Math.Abs(value)>=100 ? value.ToString("N0",CultureInfo.InvariantCulture):value.ToString("0.0##",CultureInfo.InvariantCulture);
    private static bool IsRelease()
    {
#if DEBUG
        return false;
#else
        return true;
#endif
    }

    private static ProcessStartInfo Start(IEnumerable<string> childArgs,bool redirect=true)
    {
        var path=Environment.ProcessPath!;var info=new ProcessStartInfo(path){UseShellExecute=false,RedirectStandardOutput=redirect};
        if(Path.GetFileNameWithoutExtension(path).Equals("dotnet",StringComparison.OrdinalIgnoreCase))info.ArgumentList.Add(typeof(Parent).Assembly.Location);
        foreach(var arg in childArgs)info.ArgumentList.Add(arg);
        return info;
    }
    private static Dictionary<string,double> Parse(string output)
    {
        var values=new Dictionary<string,double>(StringComparer.Ordinal);
        foreach(var line in output.Split('\n'))
            if(line.Split('\t') is ["M",var name,var value])values[name]=double.Parse(value.Trim(),CultureInfo.InvariantCulture);
        return values;
    }
    private static Dictionary<string,double> Spawn(params string[] childArgs)
    {
        using var process=Process.Start(Start(childArgs))!;
        var output=process.StandardOutput.ReadToEnd();process.WaitForExit();
        if(process.ExitCode!=0)throw new InvalidOperationException($"Child {childArgs[0]} ended with {process.ExitCode}.");
        return Parse(output);
    }
    private static void Delete(string path)
    {
        SqliteConnection.ClearAllPools();
        if(Directory.Exists(path))Directory.Delete(path,true);
        foreach(var file in new[]{path,path+"-wal",path+"-shm"})if(File.Exists(file))File.Delete(file);
    }

    /// <summary>1 and 2: each storage written then read, each step in its own process; median of the runs.</summary>
    private static void Storage(string root,int size,int runs)
    {
        var samples=new SortedDictionary<string,List<double>>(StringComparer.Ordinal);
        void Add(Dictionary<string,double> values){foreach(var (name,value) in values){if(!samples.TryGetValue(name,out var list))samples[name]=list=[];list.Add(value);}}
        for(var run=0;run<runs;run++)
        {
            var seed=172+run;var folder=Path.Combine(root,$"json-{size}-{run}");var db=Path.Combine(root,$"facts-{size}-{run}.db");
            Add(Spawn("--child-json-write",folder,size.ToString(CultureInfo.InvariantCulture),seed.ToString(CultureInfo.InvariantCulture)));
            Add(Spawn("--child-json-read",folder));
            Delete(folder);
            Add(Spawn("--child-sqlite-write",db,size.ToString(CultureInfo.InvariantCulture),seed.ToString(CultureInfo.InvariantCulture)));
            Add(Spawn("--child-sqlite-read",db));
            Delete(db);
            Console.Error.WriteLine($"  size {size:N0}: run {run+1}/{runs} done");
        }
        Console.WriteLine();
        Console.WriteLine($"SIZE {size:N0} facts, {runs} runs (median [min – max])");
        var rows=new Dictionary<string,object>();
        foreach(var (name,values) in samples)
        {
            values.Sort();var median=values.Count%2==1 ? values[values.Count/2]:(values[values.Count/2-1]+values[values.Count/2])/2;
            Console.WriteLine($"  {name,-84} {Format(median),10}   [{Format(values[0])} – {Format(values[^1])}]");
            rows[name]=new{median,min=values[0],max=values[^1]};
        }
        Report["size "+size.ToString(CultureInfo.InvariantCulture)]=rows;
    }

    /// <summary>One child that writes without end, killed after a random 50 to 500 ms: whether it was inside a write when killed, and how many writes it finished.</summary>
    private static (bool InsideWrite,int Finished) KillOne(string[] childArgs,Random random)
    {
        using var process=Process.Start(Start(childArgs))!;
        string? line;
        while((line=process.StandardOutput.ReadLine()) is not null && line!="READY"){}
        var last="READY";var finished=0;
        var reader=new Thread(()=>{string? read;while((read=process.StandardOutput.ReadLine()) is not null){Volatile.Write(ref last,read);if(read=="C")Interlocked.Increment(ref finished);}}){IsBackground=true};
        reader.Start();
        Thread.Sleep(random.Next(50,501));
        var state=Volatile.Read(ref last);
        process.Kill(true);process.WaitForExit();reader.Join();
        return (state=="B",finished);
    }

    /// <summary>3. A writer killed in the middle of a save, <paramref name="kills"/> times for each storage; then what a restart would read.</summary>
    private static void Kill(string root,int size,int kills)
    {
        var random=new Random(1720);var seed="172";var sizeText=size.ToString(CultureInfo.InvariantCulture);
        var folder=Path.Combine(root,"kill-json");var db=Path.Combine(root,"kill.db");
        Spawn("--child-sqlite-write",db,sizeText,seed);
        var results=new Dictionary<string,object>();
        foreach(var json in new[]{true,false})
        {
            int failures=0,inside=0,finished=0,twice=0;var notes=new List<string>();
            for(var kill=0;kill<kills;kill++)
            {
                var (insideWrite,done)=KillOne(json ? ["--child-kill-json",folder,sizeText,seed]:["--child-kill-sqlite",db,sizeText,seed],random);
                if(insideWrite)inside++;
                finished+=done;
                string? failure=null;
                try
                {
                    if(json)
                    {
                        Exception? broken=null;var sharedBroken=0;
                        var read=DiscoveryFiles.ReadBase(Path.Combine(folder,"Discoveries.json"),DateTimeOffset.UtcNow,(_,error)=>broken=error,long.MaxValue);
                        if(read.File is null)failure="Discoveries.json unreadable: "+(broken?.Message ?? "missing");
                        else
                        {
                            // Every fact in at least one file (a fact in both is the X-166 order's known case, left out when read); none lost.
                            var keys=read.File.Facts.Select(fact=>fact.Key).ToList();
                            foreach(var file in DiscoveryFiles.ReadShared(Path.Combine(folder,"Discoveries.json"),(_,_)=>sharedBroken++))keys.AddRange(file.Facts.Select(fact=>fact.Key));
                            var distinct=keys.Distinct(StringComparer.Ordinal).Count();twice+=keys.Count-distinct;
                            if(sharedBroken>0)failure=$"{sharedBroken} kind file(s) unreadable";
                            else if(distinct!=size)failure=$"{distinct} distinct facts instead of {size}";
                        }
                    }
                    else
                    {
                        using var connection=Db.Open(db);
                        var integrity=Db.Scalar(connection,"PRAGMA integrity_check") as string;
                        var count=Convert.ToInt64(Db.Scalar(connection,"SELECT count(*) FROM facts"),CultureInfo.InvariantCulture);
                        var rows=Db.Read(connection).Count;
                        if(integrity!="ok")failure="integrity_check: "+integrity;
                        else if(count!=size || rows!=size)failure=$"{count} rows counted, {rows} read, instead of {size}";
                    }
                }
                catch(Exception error){failure=error.GetType().Name+": "+error.Message;}
                if(failure is not null){failures++;notes.Add($"kill {kill+1}: {failure}");}
            }
            var label=json ? "json":"sqlite";
            var leftovers=json ? Directory.GetFiles(folder,"*.tmp",SearchOption.AllDirectories).Length:0;
            Console.WriteLine();
            Console.WriteLine($"KILL {label}: {kills} kills after 50–500 ms, {inside} seen inside a write, {finished} writes finished before the kills; failures {failures}"
                +(json ? $"; temporary files left behind {leftovers}; facts found in both files after a kill {twice}":$"; WAL left {Db.Wal(db)/Sampler.MB:0.0} MB"));
            foreach(var note in notes)Console.WriteLine("  "+note);
            results[label]=new{kills,inside,finished,failures,leftovers,twice,notes};
        }
        Report["kill"]=results;
        Delete(folder);Delete(db);
    }

    /// <summary>4. A reader in another process every 50 ms while the game saves in a loop: SQLite (read-only, then read-write with busy_timeout) and the JSON file.</summary>
    private static void Concurrent(string root,int size,double seconds)
    {
        var sizeText=size.ToString(CultureInfo.InvariantCulture);var secondsText=seconds.ToString(CultureInfo.InvariantCulture);
        var folder=Path.Combine(root,"concurrent-json");var db=Path.Combine(root,"concurrent.db");
        Spawn("--child-json-write",folder,sizeText,"173");Spawn("--child-sqlite-write",db,sizeText,"173");
        var results=new Dictionary<string,object>();
        foreach(var mode in new[]{"sqlite read-only","sqlite read-write, busy_timeout 2000","json"})
        {
            var json=mode=="json";
            using var process=Process.Start(Start(json ? ["--child-save-json",folder,sizeText,"173",secondsText]:["--child-upsert-sqlite",db,sizeText,"173",secondsText]))!;
            string? line;
            while((line=process.StandardOutput.ReadLine()) is not null && line!="READY"){}
            var errors=new SortedDictionary<string,int>(StringComparer.Ordinal);int reads=0;double longest=0;long rows=0;
            void Error(string name)=>errors[name]=errors.GetValueOrDefault(name)+1;
            SqliteConnection? connection=null;
            if(!json)connection=Db.Open(db,mode.Contains("read-only",StringComparison.Ordinal),null);
            var watch=Stopwatch.StartNew();
            while(watch.Elapsed.TotalSeconds<seconds)
            {
                var one=Stopwatch.StartNew();
                try
                {
                    if(json)
                    {
                        var file=JsonSerializer.Deserialize<DiscoveryFile>(File.ReadAllText(Path.Combine(folder,"Discoveries.json")));
                        if(!DiscoveryFile.Readable(file))throw new InvalidDataException("unreadable");
                        rows+=file!.Facts.Count;
                    }
                    else
                    {
                        rows+=Convert.ToInt64(Db.Scalar(connection!,"SELECT count(*) FROM facts"),CultureInfo.InvariantCulture);
                        rows+=Db.Read(connection!,DiscoveryKind.FishBite).Count;
                    }
                    reads++;
                }
                catch(SqliteException error){Error("SqliteException "+error.SqliteErrorCode);}
                catch(Exception error){Error(error.GetType().Name);}
                longest=Math.Max(longest,one.Elapsed.TotalMilliseconds);
                Thread.Sleep(50);
            }
            connection?.Dispose();
            var writer=Parse(process.StandardOutput.ReadToEnd());process.WaitForExit();
            Console.WriteLine();
            Console.WriteLine($"CONCURRENT {mode}: {seconds:0} s, reads done {reads}, read errors {errors.Values.Sum()}, longest read {longest:0.0} ms; "
                +$"writer saves {writer.GetValueOrDefault("writer saves done"):0}, writer errors {writer.GetValueOrDefault("writer errors"):0}, longest save {writer.GetValueOrDefault("writer longest save (ms)"):0.0} ms");
            foreach(var (name,count) in errors)Console.WriteLine($"  read error {name}: {count}");
            foreach(var (name,count) in writer.Where(pair=>pair.Key.StartsWith("writer error ",StringComparison.Ordinal)))Console.WriteLine($"  {name}: {count:0}");
            results[mode]=new{reads,errors,longest,writer};
        }
        Report["concurrent"]=results;
        Delete(folder);Delete(db);
    }

    private sealed record Envelope(int Schema,string Family,string Scope,string Version,DateTimeOffset RecordedAt,JsonElement Value);

    /// <summary>A JSON value of about <paramref name="bytes"/> bytes, shaped like a catalogue copy (rows of ids, names and numbers).</summary>
    private static byte[] Entry(string scope,int bytes,Random random)
    {
        var text=new StringBuilder(bytes+256).Append('[');
        for(var row=0;text.Length<bytes;row++)
            text.Append(row==0 ? "":",").Append($"{{\"Id\":{random.Next(1,60_000)},\"Name\":\"Row {row} of {scope}\",\"Territory\":{random.Next(100,1300)},\"X\":{random.NextDouble()*42:0.00},\"Y\":{random.NextDouble()*42:0.00},\"Level\":{random.Next(1,101)}}}");
        text.Append(']');
        using var value=JsonDocument.Parse(text.ToString());
        return JsonSerializer.SerializeToUtf8Bytes(new Envelope(1,"catalog-trial",scope,"2026.11.12",Invented.Now,value.RootElement.Clone()));
    }
    private static long OnDisk(long bytes)=>(bytes+4095)/4096*4096;

    /// <summary>5. The download cache: 200 entries of 50 KB to 2 MB as one file each (DataCache: SHA-256 name, temporary file then move), or as BLOB rows.</summary>
    private static void Cache(string root)
    {
        const int Count=200;var random=new Random(1725);
        var names=Enumerable.Range(0,Count).Select(index=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("trial "+index)))).ToArray();
        var entries=names.Select((name,index)=>Entry("scope "+index,random.Next(50_000,2_000_001),random)).ToArray();
        var folder=Path.Combine(root,"cache");Directory.CreateDirectory(folder);var db=Path.Combine(root,"cache.db");
        var results=new Dictionary<string,double>(StringComparer.Ordinal);
        results["entries"]=Count;results["entries, total bytes (MB)"]=entries.Sum(entry=>(long)entry.Length)/Sampler.MB;
        // Files.
        results["files: write all, one AtomicFile.Write each (ms)"]=Sampler.Time(()=>{for(var index=0;index<Count;index++)AtomicFile.Write(Path.Combine(folder,names[index]+".json"),entries[index]);});
        long FilesBytes()=>Directory.GetFiles(folder).Sum(path=>new FileInfo(path).Length);
        long FilesOnDisk()=>Directory.GetFiles(folder).Sum(path=>OnDisk(new FileInfo(path).Length));
        results["files: size, bytes (MB)"]=FilesBytes()/Sampler.MB;results["files: size on disk, 4 KB clusters (MB)"]=FilesOnDisk()/Sampler.MB;
        results["files: read all, ReadAllBytes + envelope parsed (ms)"]=Sampler.Time(()=>{foreach(var name in names)GC.KeepAlive(JsonSerializer.Deserialize<Envelope>(File.ReadAllBytes(Path.Combine(folder,name+".json"))));});
        // BLOB rows, one autocommit transaction per entry (the cache writes entries one at a time).
        using(var connection=Db.Open(db,false,"CREATE TABLE IF NOT EXISTS entries(name TEXT PRIMARY KEY,recorded INTEGER NOT NULL,value BLOB NOT NULL)"))
        {
            void Put(int index,byte[] bytes)
            {
                using var command=connection.CreateCommand();
                command.CommandText="INSERT INTO entries(name,recorded,value) VALUES($name,$recorded,$value) ON CONFLICT(name) DO UPDATE SET recorded=excluded.recorded,value=excluded.value";
                command.Parameters.AddWithValue("$name",names[index]);command.Parameters.AddWithValue("$recorded",Invented.Now.UtcTicks);command.Parameters.Add("$value",SqliteType.Blob).Value=bytes;
                command.ExecuteNonQuery();
            }
            results["sqlite: write all, one transaction each (ms)"]=Sampler.Time(()=>{for(var index=0;index<Count;index++)Put(index,entries[index]);});
            results["sqlite: WAL after the writes (MB)"]=Db.Wal(db)/Sampler.MB;
            results["sqlite: checkpoint (ms)"]=Sampler.Time(()=>Db.Checkpoint(connection));
            results["sqlite: size on disk (MB)"]=Db.Bytes(db)/Sampler.MB;
            results["sqlite: read all, one SELECT each + envelope parsed (ms)"]=Sampler.Time(()=>
            {
                foreach(var name in names)
                {
                    using var command=connection.CreateCommand();command.CommandText="SELECT value FROM entries WHERE name=$name";command.Parameters.AddWithValue("$name",name);
                    GC.KeepAlive(JsonSerializer.Deserialize<Envelope>((byte[])command.ExecuteScalar()!));
                }
            });
            // Every entry replaced by a new one of another size (a new game version), then half of them removed (the cache's bound).
            var replaced=names.Select((name,index)=>Entry("scope "+index+" new",random.Next(50_000,2_000_001),random)).ToArray();
            results["entries replaced, total bytes (MB)"]=replaced.Sum(entry=>(long)entry.Length)/Sampler.MB;
            results["files: replace all (ms)"]=Sampler.Time(()=>{for(var index=0;index<Count;index++)AtomicFile.Write(Path.Combine(folder,names[index]+".json"),replaced[index]);});
            results["sqlite: replace all, one transaction each (ms)"]=Sampler.Time(()=>{for(var index=0;index<Count;index++)Put(index,replaced[index]);});
            Db.Checkpoint(connection);
            results["files: size on disk after the replacement (MB)"]=FilesOnDisk()/Sampler.MB;
            results["sqlite: size on disk after the replacement and a checkpoint (MB)"]=Db.Bytes(db)/Sampler.MB;
            results["files: remove half (ms)"]=Sampler.Time(()=>{for(var index=0;index<Count;index+=2)File.Delete(Path.Combine(folder,names[index]+".json"));});
            results["sqlite: remove half, one transaction (ms)"]=Sampler.Time(()=>
            {
                using var transaction=connection.BeginTransaction();
                for(var index=0;index<Count;index+=2)
                {
                    using var command=connection.CreateCommand();command.Transaction=transaction;command.CommandText="DELETE FROM entries WHERE name=$name";command.Parameters.AddWithValue("$name",names[index]);command.ExecuteNonQuery();
                }
                transaction.Commit();
            });
            Db.Checkpoint(connection);
            results["files: size on disk after removing half (MB)"]=FilesOnDisk()/Sampler.MB;
            results["sqlite: size on disk after removing half, no VACUUM (MB)"]=Db.Bytes(db)/Sampler.MB;
            results["sqlite: VACUUM (ms)"]=Sampler.Time(()=>Db.Exec(connection,"VACUUM"));
            Db.Checkpoint(connection);
            results["sqlite: size on disk after VACUUM (MB)"]=Db.Bytes(db)/Sampler.MB;
        }
        Console.WriteLine();
        Console.WriteLine($"CACHE {Count} entries of 50 KB to 2 MB");
        foreach(var (name,value) in results)Console.WriteLine($"  {name,-72} {Format(value),10}");
        Report["cache"]=results;
        Delete(folder);Delete(db);
    }
}
