using TenshoAssistant.Beastmaster;

// X-109 (0.980, DEC-046): the 10 beasts of a board and the 3 of each fight, by plain rules, prepared out of combat.
internal static class BeastTeams980Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static Beast B(uint id,int str,int intel,uint auto)=>new(id,"B"+id,1,"F","","","",[new BeastStats(str,intel,50,50,100)],"",auto);
    private static BoardEnemy E(uint weakness,int pres=2,int mres=2)=>new("E",1,"w",[],new BeastStats(2,1,pres,mres,3),weakness);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
