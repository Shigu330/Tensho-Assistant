using TenshoAssistant.Quests;

// X-89 (0.970): the origin expansion, exact for quests (game data), for items only when every sure source agrees.
internal static class Expansion970Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Expansion 970: one expansion from agreeing sources, nothing when unknown or mixed",() =>
        {
            Check(ExpansionOrigin.Of(["Heavensward","Heavensward"],[]) is ("Heavensward",1),"zones");
            Check(ExpansionOrigin.Of([],["Stormblood"]) is ("Stormblood",2),"quests");
            Check(ExpansionOrigin.Of(["Endwalker"],["Endwalker"]) is ("Endwalker",3),"both");
            Check(ExpansionOrigin.Of(["Endwalker"],["Dawntrail"]) is null,"mixed");
            Check(ExpansionOrigin.Of(["Endwalker",null],[]) is null,"unknown source");
            Check(ExpansionOrigin.Of([],[]) is null,"no source");
        }),
        ("Expansion 970: quest expansion read from the game, shown in the quest tooltip, details and item tooltips",() =>
        {
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Quests/QuestReader.Dalamud.cs")).Contains("Expansion = row.Expansion.IsValid ? row.Expansion.Value.Name.ExtractText()"),"reader");
            var quests=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Quests.cs"));
            Check(System.Text.RegularExpressions.Regex.Matches(quests,"\"QuestDetails.Expansion\"").Count==2,"quest tooltip and details");
            var hover=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketHover.cs"));
            Check(hover.Contains("DrawItemExpansion(id);") && hover.Contains("ExpansionOrigin.Of(zones,fromQuests)"),"item tooltips");
        }),
    ];
}
