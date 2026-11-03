// A-12 (0.427): the recipe's needs against the character, and the job's gear set on click.
internal static class CraftReadiness427Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft readiness: level from the job levels, attributes only with the job worn, unknown said; equip on click",() =>
        {
            var ui=Read("MainWindow.CraftReadiness.cs");
            Check(ui.Contains("gearsets?.JobLevels.TryGetValue(job,out var known)==true ? known:null") && ui.Contains("level?.ToString() ?? \"?\""),"level known or « ? »");
            Check(ui.Contains("var worn=stats.JobId==job") && ui.Contains("worn ? stats.Values[\"Control\"].ToString():\"?\""),"attributes of the worn job only");
            Check(ui.Contains("+\"###CraftEquip\"))GearsetSource!.Equip(set.Number);") && ui.Contains("GameGearsets.ForJob(sets,job)"),"equip on click, lowest set");
            Check(Read("MainWindow.CraftSimulation.cs").Contains("DrawCraftReadiness(recipe);"),"shown on the Crafting page");
        }),
    ];
}
