using System.IO.Compression;
using TenshoAssistant;
using TenshoAssistant.Competitive;

// 0.1141 (X-169, GPT audit of 12/11 R05 and R06, accepted by the user). Behaviour tests (R28): two zip copies in the same second (the second wrote
// over the first), copies kept in the real order when names go back (end of summer time); the competitive decision when a read fails in the arena,
// and never asked elsewhere.
internal static class BackupNames1141Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string,string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-backup-1141-"+Guid.NewGuid().ToString("N"));
        var source=Path.Combine(root,"Discoveries");Directory.CreateDirectory(source);
        try{run(source,Path.Combine(root,"Backups"));}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static string Content(string zip)
    {
        using var archive=ZipFile.OpenRead(zip);using var reader=new StreamReader(archive.GetEntry("data.txt")!.Open());return reader.ReadToEnd();
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Backups 1141: two copies in the same second keep both, each with its own content; neither is written over",() => InTemp((source,backups) =>
        {
            var at=new DateTimeOffset(2026,11,12,6,30,0,TimeSpan.Zero);
            File.WriteAllText(Path.Combine(source,"data.txt"),"first");
            var first=BackupZip.Create(source,backups,"discoveries",at)!;
            File.WriteAllText(Path.Combine(source,"data.txt"),"second");
            var second=BackupZip.Create(source,backups,"discoveries",at)!;
            File.WriteAllText(Path.Combine(source,"data.txt"),"third");
            var third=BackupZip.Create(source,backups,"discoveries",at)!;
            Check(new[]{first,second,third}.Distinct(StringComparer.OrdinalIgnoreCase).Count()==3,"three names: "+string.Join(", ",Path.GetFileName(first),Path.GetFileName(second),Path.GetFileName(third)));
            Check(Content(first)=="first" && Content(second)=="second" && Content(third)=="third","each copy holds what was there");
            Check(Path.GetFileName(second).EndsWith("-2-discoveries.zip",StringComparison.Ordinal) && Directory.GetFiles(backups,"*.tmp").Length==0,"« -2 » before the label, no temporary left");
        })),
        ("Backups 1141: the copies kept are the newest written, even when their names go back (end of summer time) or carry « -2 »",() => InTemp((source,backups) =>
        {
            File.WriteAllText(Path.Combine(source,"data.txt"),"x");
            var start=new DateTimeOffset(2026,10,25,3,0,0,TimeSpan.Zero);var written=DateTime.UtcNow.AddHours(-1);
            var made=new List<string>();
            for(var index=0;index<7;index++)
            {
                // The clock goes back: each name is an hour before the one of before; the files are written one after the other.
                var path=BackupZip.Create(source,backups,"cache",start.AddHours(-index))!;
                File.SetLastWriteTimeUtc(path,written.AddSeconds(index));
                made.Add(path);
            }
            var kept=Directory.GetFiles(backups,"backup-*-cache.zip").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
            var newest=made.Skip(2).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
            Check(kept.SequenceEqual(newest),"the five written last: "+string.Join(", ",kept));
        })),
        ("Competitive 1141: in the arena, a read that fails counts as « in the arena » (stopped); elsewhere the arena is never read",() =>
        {
            // X-262 (0.1308): content type 21 (deep dungeons) is no longer cut: 430 is content flagged PvP here.
            var zones=CompetitiveZones.Build([(100,false),(250,true)],[(430,true,21)]);
            var asked=0;
            bool Failing(){asked++;throw new InvalidOperationException("read lost after a game update");}
            Check(CompetitiveZones.IsCut(true,579,false,zones,false,Failing),"arena read failed: stopped (in doubt)");
            Check(asked==1,"asked in the arena");
            Check(!CompetitiveZones.IsCut(true,100,false,zones,true,Failing) && CompetitiveZones.IsCut(true,250,false,zones,false,Failing) && CompetitiveZones.IsCut(true,430,false,zones,false,Failing),"elsewhere: the zones decide");
            Check(!CompetitiveZones.IsCut(false,579,false,zones,true,Failing) && CompetitiveZones.IsCut(true,0,false,zones,true,Failing),"logged out, loading screen: unchanged rules");
            Check(asked==1,"never read outside the arena: "+asked);
            Check(!CompetitiveZones.IsCut(true,579,false,zones,true,()=>false) && CompetitiveZones.IsCut(true,579,false,zones,false,()=>true),"a good read decides");
            var gate=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("CompetitiveGate.Cut,InArenaInstance);",StringComparison.Ordinal) && !gate.Contains("InArenaInstance());",StringComparison.Ordinal),"the watcher passes the read, not its result");
            Check(gate.Contains("var pvpInstance=GameMain.IsInPvPInstance();\n        try",StringComparison.Ordinal) || gate.Contains("var pvpInstance=GameMain.IsInPvPInstance();\r\n        try",StringComparison.Ordinal),"the log readings after the deciding read, in their own try");
        }),
    ];
}
