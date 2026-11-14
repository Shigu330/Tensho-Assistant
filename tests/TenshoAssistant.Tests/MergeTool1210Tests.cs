using System.IO.Compression;
using System.Numerics;
using System.Text;
using TenshoAssistant.Discoveries;

// 0.1210 (X-183, night review of 12/11, N5, N12, N13, N14, N16, N17). The merge tool (tools/discoveries-merge.py): fields of the plugin's types and
// bounds, a share's facts are its sightings, histograms capped per player, the exact place of the earliest day, a player far outside the others'
// levels left out, one file per GitHub account, atomic writes, clean titles; its behaviour is checked by its selftest (made-up shares and player
// files, run by check.sh). Here: the wiring, the plugin side of N14 (the common file read again at each version no longer adds one import), and
// that the extreme values the tool accepts are read by the plugin's v3 reader without a refusal (N12).
internal static class MergeTool1210Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset At=new(2026,11,10,0,0,0,TimeSpan.Zero);

    private static DiscoveryFact Fish(uint subject,int imported)
    {
        var place=new Vector3(120,0,-40);var cell=DiscoveryStore.Cell(place.X,place.Z);
        return new(){Kind=DiscoveryKind.FishCatch,Subject=subject,Detail=1,Territory=135,Map=16,CellX=cell.X,CellZ=cell.Z,X=place.X,Z=place.Z,
            LevelMin=1,LevelMax=1,Imported=imported,First=At.AddDays(-3),Last=At.AddDays(-2)};
    }
    private static void Saved(DiscoveryStore store){store.SnapshotToSave(At,out var stamp);store.MarkSaved(stamp);}
    private static int Imported(DiscoveryStore store,uint subject)=>store.Find(DiscoveryKind.FishCatch,subject,5).Single().Imported;

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Merge tool 1210: the common file read again at each of its versions keeps its import counts (N14)",() =>
        {
            var store=new DiscoveryStore();
            store.Merge([Fish(9,1)]);  // a fact from a file the player imported: one import
            store.Merge([Fish(1,3),Fish(9,4)],common:true);
            Check(Imported(store,1)==3 && Imported(store,9)==4,"a new fact takes the file's count, one already held the larger of the two");
            Saved(store);
            // The next version of the common file: one fact added, the others the same.
            for(var version=0;version<10;version++){store.Merge([Fish(1,3),Fish(9,4),Fish(2,1)],common:true);Saved(store);}
            Check(Imported(store,1)==3 && Imported(store,9)==4 && Imported(store,2)==1,$"ten versions read: still the file's counts ({Imported(store,1)}, {Imported(store,9)}, {Imported(store,2)})");
            store.Merge([Fish(1,5)],common:true);
            Check(Imported(store,1)==5,"a later version with more players raises it");
            // A file you import yourself still counts one more import each.
            var files=new DiscoveryStore();
            files.Merge([Fish(1,1)]);files.Merge([Fish(1,1)]);
            Check(Imported(files,1)==2,"two imported files: two imports");
        }),
        ("Merge tool 1210: the exchange passes common:true for the common file only",() =>
        {
            var exchange=Read("src/TenshoAssistant/Discoveries/GitHubExchange.cs");
            var model=Read("src/TenshoAssistant/Discoveries/DiscoveriesModel.cs");
            // X-357 (0.1349): the v2 common file is no longer read.
            Check(exchange.Contains("var result=store.Merge(read.Facts,common:true);") && !exchange.Contains("store.Merge(file.Facts,common:true)"),"the v3 common files (the v2 one gone)");
            Check(model.Contains("public (int Added,int Joined,int Refused) Merge(IEnumerable<DiscoveryFact> imported,bool common=false)")
                && model.Contains("if(common)known.Imported=Math.Max(known.Imported,source.Imported);else known.Imported++;")
                && model.Contains("copy.Imported=common ? Math.Max(1,source.Imported):1;")
                && model.Contains("var result=store.Merge(file.Facts);\n"),"the store; a file the player imports keeps one import each");
        }),
        ("Merge tool 1210: the tool's checks and selftest cases are in place (N5, N12, N13, N16, N17)",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("if set(f)-FIELDS or not typed(f) or f[\"Kind\"] not in KINDS") && tool.Contains("if cell_of(f[\"X\"],f[\"Z\"])!=(f[\"CellX\"],f[\"CellZ\"]):return False"),"N12: types, cell in single precision");
            Check(tool.Contains("return FIRST_DAY<=when(f[\"First\"])") && tool.Contains("clean(from_gist_files(files,fetch_gist_raw),player=True)") && tool.Contains("g=capped(daily(dict(f)))"),"N5: dates, sightings, cap");
            Check(tool.Contains("known[\"First\"]=f[\"First\"];known[\"X\"]=f[\"X\"];known[\"Z\"]=f[\"Z\"]") && tool.Contains("FLAG: alone far outside the other players' levels"),"N5: earliest place, far player left out");
            Check(tool.Contains("same author as {newest[login]} (a newer issue): not counted") && tool.Contains("write_json(path,{\"Issue\":issue[\"number\"],\"Login\":login(issue),\"Facts\":facts})")
                && tool.Contains("def withdraw(issue,dry):"),"N13: one file per account, withdraw");
            Check(!tool.Contains("open(path,\"w\"") && !tool.Contains("open(COMMON,\"w\"") && !tool.Contains(".write(data);changed+=1") && tool.Contains("os.replace(temporary,path)"),"N16: every write atomic");
            Check(tool.Contains("\"?\" if unicodedata.category(c) in (\"Cc\",\"Cf\") else c"),"N17: clean titles");
            foreach(var name in new[]{"N12: wrong types accepted","N5: a player far outside","N13: one file per account","N16: a failed write","N17: control and format"})
                Check(tool.Contains(name),"selftest case "+name);
        }),
        ("Merge tool 1210: the extreme values the tool accepts are read by the plugin's v3 reader without a refusal (N12)",() =>
        {
            // Whole places, the largest ids, a negative cell in single precision, dates kept to the day, Seen and Imported at their bounds.
            string[] facts=
            [
                "{\"Kind\":10,\"Subject\":4294967295,\"Detail\":4294967295,\"Territory\":4294967295,\"Map\":4294967295,\"CellX\":1,\"CellZ\":2,\"X\":60,\"Z\":140,\"LevelMin\":0,\"LevelMax\":100000,\"Seen\":0,\"Imported\":2147483647,\"First\":\"2026-11-01T00:00:00+00:00\",\"Last\":\"2026-11-02T00:00:00+00:00\"}",
                "{\"Kind\":10,\"Subject\":1,\"Detail\":0,\"Territory\":1,\"Map\":1,\"CellX\":-1,\"CellZ\":0,\"X\":-50.0000001,\"Z\":0.0,\"LevelMin\":1,\"LevelMax\":1,\"Seen\":0,\"Imported\":1,\"First\":\"2026-11-01T00:00:00+00:00\",\"Last\":\"2026-11-01T00:00:00+00:00\",\"Hq\":1,\"Values\":{\"0\":1,\"100000\":100000000}}",
            ];
            var header="{\"Format\":\"tensho-discoveries\",\"Version\":3,\"Kind\":10,\"Count\":2,\"Producer\":\"t\",\"Written\":\"2026-11-02T00:00:00+00:00\"}";
            using var memory=new MemoryStream();
            using(var gzip=new GZipStream(memory,CompressionLevel.Optimal,true))gzip.Write(Encoding.UTF8.GetBytes(string.Concat(new[]{header}.Concat(facts).Select(line=>line+"\n"))));
            var bytes=memory.ToArray();
            var entry=new ExchangeV3Entry{Name=ExchangeV3.FileName(DiscoveryKind.FishCatch),Kind=DiscoveryKind.FishCatch,Count=2,Bytes=bytes.Length,Sha256=ExchangeV3.Sha256(bytes)};
            var read=ExchangeV3.ReadKind(new MemoryStream(bytes),entry);
            Check(read.Refused==0 && read.Facts.Count==2,$"read whole ({read.Facts.Count} facts, {read.Refused} refused)");
            Check(read.Facts.All(DiscoveryStore.Valid),"and each one holds for the store");
        }),
    ];
}
