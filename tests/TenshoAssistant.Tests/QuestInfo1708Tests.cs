// X-606 (0.1708, the user's request of 18/11: the Quests page gives little information): the probe of what the sheets give for a quest
// (PROBE_QUESTINFO) and its evidence; the probe project builds again (the planner's file it links names the submarine unlock model, 0.1703).
internal static class QuestInfo1708Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath(name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Quest info 1708: the probe reads the giver, its place, the end NPC, the jobs and the quests after; its evidence has both halves", () =>
        {
            var probe = Read("tools/ShopCurrencyProbe/Program.cs");
            Check(probe.Contains("Environment.GetEnvironmentVariable(\"PROBE_QUESTINFO\")", StringComparison.Ordinal), "the block");
            foreach (var read in new[] { "quest.IssuerStart.RowId", "quest.IssuerLocation.Value", "quest.TargetEnd.RowId", "quest.ClassJobCategory0", "quest.BeastTribe.RowId", "quest.GilReward" })
                Check(probe.Contains(read, StringComparison.Ordinal), "reads " + read);
            var project = Read("tools/ShopCurrencyProbe/ShopCurrencyProbe.csproj");
            Check(project.Contains("FreeCompany/VoyagePlanner.cs", StringComparison.Ordinal) && project.Contains("FreeCompany/SubmarineUnlocks.cs", StringComparison.Ordinal), "the probe links what its planner file needs");
            var evidence = Read("docs/evidence/0.1708-quest-info.md");
            Check(evidence.Contains("# What the Quests page can show", StringComparison.Ordinal) && evidence.Contains("# Ce que la page Quêtes peut montrer", StringComparison.Ordinal)
                && evidence.Contains("PROBE_QUESTINFO", StringComparison.Ordinal), "the evidence, two halves");
        }),
    ];
}
