using TenshoAssistant.Beastmaster;

// X-109 (0.981, DEC-047): the board's team panel; in the labyrinth only while its board window is open and out of combat.
internal static class BeastBoard981Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
