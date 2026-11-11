using System.Numerics;
using TenshoAssistant.Discoveries;

// X-43 (0.807, user request): the local base of discoveries: places of monsters, provenance, import that joins, export.
internal static class DiscoveriesTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,7,20,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries: a monster's place once per 10-yalm cell; a second sighting counts after ten minutes; levels widen",() =>
        {
            var store=new DiscoveryStore();
            Check(store.SeeMonster(10,20,130,11,new Vector3(101,0,-5),12,At),"new");
            Check(!store.SeeMonster(10,20,130,11,new Vector3(108,0,-1),14,At.AddMinutes(1)),"same cell");
            store.SeeMonster(10,20,130,11,new Vector3(104,0,-3),11,At.AddMinutes(12));
            var fact=store.Find(DiscoveryKind.MonsterPlace,_=>true,10).Single();
            Check(fact.Seen==2 && fact.LevelMin==11 && fact.LevelMax==14 && fact.X==101 && fact.CellX==2 && fact.CellZ==-1,"counted, widened, first place kept (0.855: 50-yalm cells)");
            Check(store.SeeMonster(10,20,130,11,new Vector3(151,0,-5),12,At),"next cell: new");
            Check(!store.SeeMonster(0,20,130,11,Vector3.Zero,1,At) && !store.SeeMonster(10,20,130,0,Vector3.Zero,1,At),"no name or map: nothing");
        }),
        ("Discoveries: an import joins, never removes; new facts come in as imported only; wrong facts are refused",() =>
        {
            var mine=new DiscoveryStore();mine.SeeMonster(10,20,130,11,new Vector3(101,0,-5),12,At);
            var friend=new DiscoveryStore();friend.SeeMonster(10,20,130,11,new Vector3(102,0,-4),15,At.AddDays(-1));friend.SeeMonster(30,40,140,12,new Vector3(0,0,0),50,At);
            var file=friend.Snapshot(At);
            var broken=file.Facts[0].Copy();broken.CellX+=3;
            var result=mine.Merge(file.Facts.Append(broken).Append(null!));
            Check(result==(1,2,1),"one new, two joined (0.855: a wrong cell is moved to its own), one refused");
            var joined=mine.Find(DiscoveryKind.MonsterPlace,id=>id==10,5).Single();
            Check(joined.Seen==1 && joined.Imported==2 && joined.LevelMax==15 && joined.First==At.AddDays(-1),"ours kept, widened");
            var added=mine.Find(DiscoveryKind.MonsterPlace,id=>id==30,5).Single();
            Check(added.Seen==0 && added.Imported==1,"imported only");
            Check(mine.Summary().Single()==(DiscoveryKind.MonsterPlace,2,1,1),"summary");
        }),
        ("Discoveries: files round-trip; an import reads each file once and leaves an unreadable one",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-discoveries-"+Guid.NewGuid().ToString("N"));
            try
            {
                var store=new DiscoveryStore();store.SeeMonster(10,20,130,11,new Vector3(101,0,-5),12,At);
                DiscoveryFiles.Write(Path.Combine(folder,"Import","a.json"),store.Snapshot(At,false));
                File.WriteAllText(Path.Combine(folder,"Import","b.json"),"{not json");
                Check(store.Dirty,"an export does not mark the base saved");
                var other=new DiscoveryStore();
                var first=DiscoveryFiles.Import(Path.Combine(folder,"Import"),other);
                Check(first==(1,1,0,0,1) && File.Exists(Path.Combine(folder,"Import","a.json.imported")),"read once, renamed");
                Check(DiscoveryFiles.Import(Path.Combine(folder,"Import"),other).Files==0,"not twice");
                Check(DiscoveryFiles.Read(Path.Combine(folder,"Import","a.json.imported"))?.Facts.Count==1,"round trip");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
        ("Discoveries: on by default, monsters only, debug log off; nothing about players; saved only once read",() =>
        {
            Check(new DiscoverySettings() is { Enabled:true, MonsterPlaces:true, DebugLog:false },"defaults");
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            var page=Read("MainWindow.Discoveries.cs");
            Check(page.Contains("|| !DiscoveriesLoaded)return;") && page.Contains("store.SeeMonster(monster.NameId,monster.BaseId,live.TerritoryId,live.MapId,monster.Position,monster.Level,at)"),"after the read, from the monsters");
            Check(Read("Plugin.cs").Contains("DiscoveryFiles.ReadBase(saved,"),"a broken file is set aside (0.1039: under a dated name, DiscoveriesBase1039Tests)");
            // 0.1019 (test audit #2): by reflection, no saved field of a fact (nor of any type of the Discoveries namespace) can carry a player.
            var playerish=new System.Text.RegularExpressions.Regex("Player|Character|ContentId|Account|Retainer|Owner",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var factFields=typeof(DiscoveryFact).GetProperties().Select(property=>property.Name).ToList();
            Check(factFields.Count>=15 && factFields.Contains("Subject") && !factFields.Any(name=>playerish.IsMatch(name) || name.Contains("Name",StringComparison.OrdinalIgnoreCase)),"no player field: "+string.Join(",",factFields));
            var leaks=typeof(DiscoveryFact).Assembly.GetTypes().Where(type=>type.Namespace=="TenshoAssistant.Discoveries")
                .SelectMany(type=>type.GetProperties().Concat<System.Reflection.MemberInfo>(type.GetFields()).Select(member=>type.Name+"."+member.Name))
                .Where(name=>playerish.IsMatch(name)).ToList();
            Check(leaks.Count==0,"no player member in Discoveries: "+string.Join(",",leaks));
        }),
    ];
}
