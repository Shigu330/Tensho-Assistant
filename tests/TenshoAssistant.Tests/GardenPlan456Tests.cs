using TenshoAssistant.Reminders;

// 0.456 (in-game campaign): the planting plan to get as many of a seed as possible (rules: docs/evidence/0.456-garden-grid.md).
internal static class GardenPlan456Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden plan: the pairs that give the fewest other seeds come first",() =>
        {
            var data=new GardenCrossData([new(100,1,2),new(200,1,2),new(100,3,4),new(100,5,6),new(300,5,6),new(400,5,6)],[]);
            var best=data.BestPairsFor(100);
            Check(best.Count==3 && best[0] is { First:3, Second:4 } && best[0].Outcomes.Count==1,"only this seed first");
            Check(best[1] is { First:1, Second:2 } && best[1].Outcomes.Count==2 && best[2].Outcomes.Count==3,"then by number of outcomes");
            Check(data.BestPairsFor(999).Count==0,"no pair");
        }),
        ("Garden plan: 8 beds in a ring around an empty centre, seeds alternating, 4 of each, slower seed first, bed 1 without a cross",() =>
        {
            var pair=new GardenCrossPair(1,2,[100]);
            var plan=GardenCrosses.Plan(pair,seed=>seed==2 ? 120:24);
            Check(plan.Count==8 && plan.Select(bed=>(bed.Row,bed.Column)).Distinct().Count()==8 && !plan.Any(bed=>bed is { Row:1, Column:1 }),"ring");
            Check(plan[0] is { Order:1, Row:0, Column:0, Seed:2, Crosses:false } && plan.Skip(1).All(bed=>bed.Crosses),"bed 1 at the top left, slower seed, no cross");
            Check(plan.Count(bed=>bed.Seed==1)==4 && plan.Count(bed=>bed.Seed==2)==4,"4 of each");
            for(var index=0;index<8;index++)
            {
                var next=plan[(index+1)%8];
                Check(next.Seed!=plan[index].Seed && Math.Abs(next.Row-plan[index].Row)+Math.Abs(next.Column-plan[index].Column)==1,"neighbours differ: "+index);
            }
            Check(GardenCrosses.Plan(pair,_=>null)[0].Seed==1,"unknown durations: the pair's order");
        }),
        ("Garden plan: chosen by click, nothing read from the game, credited with its evidence",() =>
        {
            var ui=Read("MainWindow.GardenCrosses.cs");
            Check(ui.Contains("if(ImGui.Selectable(label+") && ui.Contains("if(gardenPair is { } chosen)DrawGardenPlan(chosen,name);") && !ui.Contains("unsafe"),"click");
            Check(ui.Contains("best.Seed!=gardenCrossSeed"),"pairs kept while the seed stays");
            Check(Read("Plugin.cs").Contains("crosses.Seeds.Concat([MainWindow.GardenTopsoil3,MainWindow.GardenTopsoil2])"),"soil names in the game's language");
            Check(Read("Credits/CreditCatalog.cs").Contains("new(\"GardeningGuide\", CreditKind.Rule") && File.ReadAllText(Path.GetFullPath("docs/evidence/0.456-garden-grid.md")).Contains("7766"),"credit and evidence");
        }),
    ];
}
