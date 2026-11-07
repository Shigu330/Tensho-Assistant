using TenshoAssistant.Craft;

// 0.739 (user request): one line per item in « What can I craft? », one icon per job.
internal static class CraftLines739Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static CraftFromStockRow Row(uint recipe,uint item,long count,uint job)=>new(new RecipeDefinition(recipe,item,1,"j"+job,44,[],job),count,true,true);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft lines: the recipes of one item share a line, in the list's order, the best first",() =>
        {
            // The report: cobalt rivets by armourer (92) and blacksmith (80), each counting the same 52 sales a day.
            var lines=CraftFromStock.Lines([Row(1,5,92,2),Row(2,7,30,7),Row(3,5,80,1)]);
            Check(lines.Length==2 && lines[0].Lead.Recipe.RecipeId==1 && lines[0].All.Select(row=>row.Recipe.RecipeId).SequenceEqual([1u,3u]),"merged, lead first");
            Check(lines[1].All.Count==1 && lines[1].Lead.Recipe.ResultItemId==7,"alone");
            Check(CraftFromStock.Lines([]).Length==0,"empty");
        }),
        ("Craft lines: job icons with their name on hover, a click simulates; pages count lines",() =>
        {
            var page=Read("MainWindow.CraftFromStock.cs");
            Check(page.Contains("CraftFromStock.Paged(CraftFromStock.Lines(ranked)") && page.Contains("foreach(var recipe in line.All){ImGui.SameLine();DrawFromStockJob(recipe);}"),"lines and icons");
            Check(page.Contains("62108+recipe.CraftTypeId") && page.Contains("\"CraftStock.JobIconTip\"") && page.Contains("if(ImGui.IsItemClicked())OpenCraftSimulation(recipe);"),"icon, name, click");
            Check(Read("Localization/Languages/fr.json").Contains("\"Row\": \"{0} fabrication(s) · niv. {1}\""),"the job name left the line");
        }),
    ];
}
