using TenshoAssistant.Monsters;

// 0.804 (X-39, user request): nearby monsters a missing beast is tamed from, by the wiki's bestiary.
internal static class BeastNearby804Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private const string Html="<table><tr><th>No.</th><th>Name</th><th>Tamed from</th><th>Location</th><th>Min. Level</th></tr>"
        +"<tr><td>2</td><td><a>Squirrel</a></td><td><a href='x'>Ground Squirrel</a></td><td>Central Shroud (X:24, Y:16)</td><td>1</td></tr>"
        +"<tr><td>3</td><td>Lamb</td><td>Lost Lamb</td><td>Middle La Noscea (X:22, Y:26)</td><td>3</td></tr>"
        +"<tr><td>x</td><td>Bad</td><td>Row</td><td></td><td></td></tr></table>";
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Beast wiki: the bestiary's table read by its headers; the number is the beast's row",() =>
        {
            var rows=BeastWikiParser.Rows(Html);
            Check(rows.Count==2 && rows[0]==new BeastWikiRow(2,"Squirrel","Ground Squirrel","Central Shroud (X:24, Y:16)") && rows[1].Enemy=="Lost Lamb","rows");
            var names=new Dictionary<string,uint[]>(StringComparer.OrdinalIgnoreCase){["ground squirrel"]=[500,501]};
            var map=BeastWikiParser.Map(rows,name=>names.TryGetValue(name,out var ids) ? ids:[]);
            Check(map[500].SequenceEqual(new uint[]{2}) && map[501].SequenceEqual(new uint[]{2}) && map.Count==2,"enemy rows");
            var failed=false;try{BeastWikiParser.Rows("<p>nothing</p>");}catch(InvalidDataException){failed=true;}
            Check(failed,"no table: an error, not an empty list");
        }),
        ("Beast wiki: asked on a click only; as Beastmaster (job 43) unless always; the job icon above the head",() =>
        {
            Check(Read("MainWindow.Beastmaster.cs").Contains("SourceButton(localization.Get(wiki.Book is null ? \"BeastUi.WikiFetch\" : \"BeastUi.WikiRefresh\"), configuration.CommunityWiki, true)) wiki.Fetch();"),"click");
            Check(Read("MainWindow.NearbyMonsters.cs").Contains("internal const uint BeastmasterJob=43;") && Read("MainWindow.NearbyMonsters.cs").Contains("!tamed.Contains(beast)"),"job, not tamed");
            Check(Read("MainWindow.MonsterIcons.cs").Contains("BeastmasterIcon=62100+BeastmasterJob"),"icon");
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.ShowBeastMonsters && settings.AboveBeast && !settings.BeastMonstersAlways,"defaults");
        }),
    ];
}
