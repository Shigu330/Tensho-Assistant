// Test audit of 2026-11-09, batch 2 (0.1021): BeastBoard981Tests, BeastMap985Tests, BeastNearby804Tests, BeastPlaces688Tests, BeastPlaces984Tests, BeastTeams980Tests merged into BeastmasterTests; every case is kept.
using TenshoAssistant.Beastmaster;
using TenshoAssistant.Monsters;
internal static class BeastmasterTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private const string Html="<table><tr><th>No.</th><th>Name</th><th>Tamed from</th><th>Location</th><th>Min. Level</th></tr>"
        +"<tr><td>2</td><td><a>Squirrel</a></td><td><a href='x'>Ground Squirrel</a></td><td>Central Shroud (X:24, Y:16)</td><td>1</td></tr>"
        +"<tr><td>3</td><td>Lamb</td><td>Lost Lamb</td><td>Middle La Noscea (X:22, Y:26)</td><td>3</td></tr>"
        +"<tr><td>x</td><td>Bad</td><td>Row</td><td></td><td></td></tr></table>";
    private static Beast B(uint id,int str,int intel,uint auto)=>new(id,"B"+id,1,"F","","","",[new BeastStats(str,intel,50,50,100)],"",auto);
    private static BoardEnemy E(uint weakness,int pres=2,int mres=2)=>new("E",1,"w",[],new BeastStats(2,1,pres,mres,3),weakness);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From BeastBoard981Tests: X-109 (0.981, DEC-047): the board's team panel; in the labyrinth only while its board window is open and out of combat.
        ("Beast board 981: ranks read from the team window's texts, name then rank",() =>
        {
            var beasts=new[]{new Beast(1,"Golem",1,"F","","","",[new(1,1,1,1,1),new(1,1,1,1,1),new(1,1,1,1,1),new(1,1,1,1,1),new(1,1,1,1,1)]),new Beast(2,"Cu sith",1,"F","","","",[new(1,1,1,1,1),new(1,1,1,1,1),new(1,1,1,1,1),new(1,1,1,1,1)])};
            var ranks=BeastTeams.ParseRanks(["81","RÉS MAG","Exp","667/667","PV","Golem","5","111","Cu sith","4","Cu sith","9"],beasts);
            Check(ranks.Count==2 && ranks[1]==5 && ranks[2]==4,"ranks");
        }),
        ("Beast board 981: the gate's board state needs the board window and no combat; the reader goes through AddBoard",() =>
        {
            var gate=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("board=!window.IsNull && window.IsVisible && !condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];"),"board and no combat");
            Check(gate.Contains("void Guarded(IFramework running){if(!cut || labyrinthBoard)Timed(handler,running);}"),"AddBoard");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs")).Contains("CompetitiveGate.AddBoard(framework, OnUpdate);"),"reader");
            Check(new TenshoAssistant.Configuration().ShowBeastBoardPanel,"on by default");
        }),
        // From BeastMap985Tests: X-110 (0.985): the beasts not tamed yet, at the places seen in game, on the Tensho map, with the beast layer.
        ("Beast map 985: untamed only, this map only, beast layer and Beastmaster (or always), click = flag",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("if(tamedAlready.Contains(wild.Id))continue;") && map.Contains("if(place.MapId!=map.MapId)continue;"),"filters");
            Check(map.Contains("settings.ShowBeastMonsters && (settings.BeastMonstersAlways || characterStats.State.JobId==BeastmasterJob)"),"layer");
            Check(map.Contains("FlagFromMap(map,place.X,place.Y);"),"flag");
        }),
        // From BeastNearby804Tests: 0.804 (X-39, user request): nearby monsters a missing beast is tamed from, by the wiki's bestiary.
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
        // From BeastPlaces688Tests: 0.688 (user report): the Beastmaster page says where a beast is caught.
        ("Beast places: columns 6 and 7 are a zone or a duty, read from their own sheets",() =>
        {
            var loader=Read("Beastmaster/BeastCatalogLoader.Dalamud.cs");
            Check(loader.Contains("1 => data.GetExcelSheet<PlaceName>().GetRowOrDefault(id)"),"zone");
            Check(loader.Contains("2 => data.GetExcelSheet<ContentFinderCondition>().GetRowOrDefault(id)"),"duty");
            Check(Read("Beastmaster/BeastCatalog.cs").Contains("catchPlace?.Invoke(row.Int(6), row.UInt(7))"),"columns");
        }),
        ("Beast places: the column is last, so the sort columns do not move",() =>
        {
            var page=Read("MainWindow.Beastmaster.cs");
            Check(page.Contains("BeginTable(\"BeastTable\", 9,") && page.Contains("localization.Get(\"BeastUi.CatchPlace\"), ImGuiTableColumnFlags.NoSort"),"column");
        }),
        // From BeastPlaces984Tests: X-110 (0.984): where to tame a beast: its tames with a place, then its monster's places seen (the wiki's enemy too), with the place menu.
        ("Beast places 984: tames first, then the monster's places, the menu on them, the tame kind still optional on place",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("store.Find(Discoveries.DiscoveryKind.BeastTamed, beast, 8)") && page.Contains("store.Find(Discoveries.DiscoveryKind.MonsterPlace, monster, 3)"),"sources");
            Check(page.Contains("NpcMenu($\"BeastPlaces{beast.Id}\", beast.Name, 0, places);"),"menu");
            Check(!TenshoAssistant.Discoveries.DiscoveryStore.HasPlace(TenshoAssistant.Discoveries.DiscoveryKind.BeastTamed),"tames without a monster stay valid");
        }),
        // From BeastTeams980Tests: X-109 (0.980, DEC-046): the 10 beasts of a board and the 3 of each fight, by plain rules, prepared out of combat.
        ("Beast teams 980: the weakness first, then attack; three at most; every enemy's weakness covered when possible",() =>
        {
            var beasts=new[]{B(1,200,10,8),B(2,60,10,7),B(3,150,10,9),B(4,140,10,9),B(5,50,10,8)};
            var (picks,hit)=BeastTeams.Best(beasts.Select(beast=>(beast.Ranks[0],beast.AutoRow)).ToArray(),[E(7),E(8)]);
            Check(picks.Count==3 && hit==2,"both weaknesses covered");
            Check(picks.Contains(1) && picks.Contains(0),"the blunt one and the best piercing one");
        }),
        ("Beast teams 980: the 10 from every fight, only beasts held, ranks synchronised to the board's limit and said assumed",() =>
        {
            var beasts=Enumerable.Range(1,14).Select(id=>B((uint)id,40+id*5,30,(uint)(7+id%3))).ToList();
            var board=new Board(1,"Board",30,25,10,5,[new BoardBattle(false,[E(7)]),new BoardBattle(false,[E(8),E(9)]),new BoardBattle(true,[E(9),E(7),E(8)])],[]);
            var held=new HashSet<uint>(Enumerable.Range(1,12).Select(id=>(uint)id));
            var plan=BeastTeams.Plan(board,beasts,held,null);
            Check(plan.Team.Count==10 && plan.Team.All(pick=>held.Contains(pick.Beast)) && plan.RanksAssumed,"ten held");
            Check(plan.Battles.Count==3 && plan.Battles.All(battle=>battle.Team.Count==3 && battle.Team.All(pick=>plan.Team.Any(ten=>ten.Beast==pick.Beast))),"3 of the 10 each");
            var back=BeastTeams.Read(BeastTeams.Write(new(new(){[1]=plan},new(){[1]="k"})));
            Check(back?.Plans[1].Team.Count==10,"round trip");
        }),
        ("Beast teams 980: enemy stars read from XBMBattleDetail 1 to 5, the team tab and the plans file",() =>
        {
            var catalog=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalog.cs"));
            Check(catalog.Contains("new BeastStats(row.Int(1), row.Int(2), row.Int(3), row.Int(4), row.Int(5)), row.UInt(8))"),"stars");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("\"BeastTeams.json\""),"disk");
        }),
    ];
}
