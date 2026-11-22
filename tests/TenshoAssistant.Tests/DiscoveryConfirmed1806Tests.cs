using TenshoAssistant.Discoveries;

// 0.1806 (X-468 part 2, the user's choice D-216 a of 22/11; D-172: « a fact verified several times by several people counts more »): the pages
// and the item lines prefer a fact seen by several players (the common set's count, Imported) to one seen many times by one player, and say
// « seen by N players » or « seen by one player only ». Behaviour on a real store.
internal static class DiscoveryConfirmed1806Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,0,0,0,TimeSpan.Zero);

    private static DiscoveryFact Monster(uint subject,float x,int seen,int imported)
    {
        var cell=DiscoveryStore.Cell(x,10);
        return new(){Kind=DiscoveryKind.MonsterPlace,Subject=subject,Detail=0,Territory=135,Map=16,CellX=cell.X,CellZ=cell.Z,X=x,Z=10,
            LevelMin=5,LevelMax=6,Seen=seen,Imported=imported,First=At,Last=At};
    }

    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery confirmed 1806: how many players saw a fact: the common count, at least 1 when you saw it, never more than known",() =>
        {
            Check(DiscoveryFact.WitnessesOf(Monster(1,0,0,3))==3 && DiscoveryFact.WitnessesOf(Monster(1,0,12,0))==1 && DiscoveryFact.WitnessesOf(Monster(1,0,4,2))==2
                && DiscoveryFact.WitnessesOf(Monster(1,0,0,0))==0,"players");
        }),
        ("Discovery confirmed 1806: behaviour: a store's facts come the most confirmed first: three players before one player's twelve sightings",() =>
        {
            var store=new DiscoveryStore();
            store.Load(new DiscoveryFile{Written=At,Facts=[Monster(7,10,12,0),Monster(7,210,0,3),Monster(7,410,0,1),Monster(8,10,0,2)]});
            var places=store.Find(DiscoveryKind.MonsterPlace,7u,5).Select(fact=>fact.X).ToArray();
            Check(places.SequenceEqual(new float[]{210,10,410}),"one subject: "+string.Join(",",places));
            var all=store.Find(DiscoveryKind.MonsterPlace,_=>true,5).Select(fact=>(fact.Subject,fact.X)).ToArray();
            Check(all.SequenceEqual(new (uint,float)[]{(7,210),(8,10),(7,10),(7,410)}),"every subject: "+string.Join(",",all));
            var byMap=SeenPlaces.ByMap(store.Find(DiscoveryKind.MonsterPlace,7u,5),3);
            Check(byMap.Length==1 && byMap[0].X==210,"the tools' place per map: the most confirmed");
        }),
        ("Discovery confirmed 1806: wiring: the table and the item lines say how many players saw a fact, in English and French",() =>
        {
            var window=Read("src/TenshoAssistant/MainWindow.Discoveries.cs");
            Check(window.Contains("ImGui.TableNextColumn();ImGui.TextUnformatted(DiscoveryPlayers(fact));",StringComparison.Ordinal),"the table's column");
            Check(window.Contains("var lines=DiscoveryFact.MostConfirmed(found).Take(4)",StringComparison.Ordinal) && window.Contains(", {DiscoveryPlayers(fact)})\").ToArray();",StringComparison.Ordinal),"the item lines");
            Check(!Read("src/TenshoAssistant/Discoveries/DiscoveriesModel.cs").Contains("OrderByDescending(DiscoveryFact.TimesOf)",StringComparison.Ordinal),"no list ordered by sightings alone");
            foreach(var (language,players,one) in new[]{("en","seen by {0} players","seen by one player only"),("fr","vu par {0} joueurs","vu par un seul joueur")})
            {
                var text=Read("src/TenshoAssistant/Localization/Languages/"+language+".json");
                Check(text.Contains("\"SeenByPlayers\": \""+players+"\"",StringComparison.Ordinal) && text.Contains("\"SeenByOne\": \""+one+"\"",StringComparison.Ordinal),language);
            }
        }),
    ];
}
