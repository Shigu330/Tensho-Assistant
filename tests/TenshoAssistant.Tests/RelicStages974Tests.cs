using TenshoAssistant.Relics;

// X-90 (0.974): relic weapons and armour by version (one stage, every job), beside the per-job view.
internal static class RelicStages974Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Relic stages 974: grouped by item level, lowest first, unknown level left out",() =>
        {
            var levels=new Dictionary<uint,int>{[1]=135,[2]=150,[3]=135,[4]=150,[5]=0};
            var stages=RelicBoard.ByLevel([[1,2],[3,4,5]],id=>levels.GetValueOrDefault(id));
            Check(stages.Count==2 && stages[0].Level==135 && stages[0].Items.SequenceEqual([1u,3u]) && stages[1].Items.SequenceEqual([2u,4u]),"stages");
        }),
        ("Relic stages 974: the page offers the stage view for weapons and armour",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.RelicWeapons.cs"));
            Check(page.Contains("if(relicByStage){DrawRelicStageWeapons(family,lines);return;}") && page.Contains("if(relicByStage){DrawRelicStageArmour(family,sets);return;}"),"views");
        }),
    ];
}
