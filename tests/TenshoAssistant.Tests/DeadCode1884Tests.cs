// X-569 (0.1884, audit of 17/11, part C): the public members the plugin never used, kept only for their tests, are gone with those tests'
// checks; the tests that read them now read what the plugin uses (ProblemReport.Prepare, TimeLeft.Text, the duty loot client's
// CacheLifetime, ChatHistory.Trim). IDE0051 cannot see public members: this list keeps them from coming back unseen.
internal static class DeadCode1884Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dead code 1884: the public members served only to the tests are gone", () =>
        {
            foreach (var (file, text) in new[]
            {
                ("Gathering/Spearfishing.cs", "SpearfishCandidates"),
                ("Gathering/Spearfishing.cs", "Candidates("),
                ("Community/GatherBuddyFish.cs", "IEnumerable<GatherBuddyFish> Spearfish"),
                ("Help/ProblemReport.cs", "ProblemLink"),
                ("Monsters/WikiMonsterSources.cs", "MonsterDropCandidates"),
                ("Character/StatFormulas.cs", "Useful("),
                ("Market/CurrencyValue.cs", "> Rank("),
                ("Localization/TenshoCulture.cs", "Hours("),
                ("Chat/ChatHistory.cs", "void Rewrite("),
                ("Duties/DutyLootDocument.cs", "IsExhaustive"),
                ("Duties/DutyLootService.cs", "FreshFor"),
                ("Gathering/OceanFishing.cs", "SlotOf("),
                ("Quests/QuestService.cs", "JoinLabel("),
                ("Reminders/GardenView.cs", "LeftText("),
                ("Npcs/NpcVendorCatalog.cs", "SalesOfShop("),
            })
                Check(!Read("src/TenshoAssistant/" + file).Contains(text, StringComparison.Ordinal), $"{Path.GetFileName(file)}: {text}");
        }),
        ("Dead code 1884: a duty loot table is never said to be whole (the property that always said no is gone, nothing claims it)", () =>
        {
            foreach (var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant/Duties"), "*.cs"))
                Check(!File.ReadAllText(file).Contains("Exhaustive", StringComparison.Ordinal), Path.GetFileName(file));
        }),
    ];
}
