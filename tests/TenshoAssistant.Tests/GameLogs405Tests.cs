// 0.405 (user request): the game's crafting and gathering logs opened on a recipe or an item — functions checked before the call,
// entries in the item menu, the Gathering page and the Crafting page.
internal static class GameLogs405Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game logs: functions checked, opened from the item menu, Gathering and Crafting pages",() =>
        {
            var opener = Read("Items/GameLogOpener.Dalamud.cs");
            Check(opener.Contains("(nint)AgentRecipeNote.MemberFunctionPointers.OpenRecipeByRecipeIdInternal==0",StringComparison.Ordinal)
                && opener.Contains("(nint)AgentGatheringNote.MemberFunctionPointers.OpenGatherableByItemId==0",StringComparison.Ordinal),"checked");
            Check(Read("MainWindow.DutyFinder.cs").Contains("DrawGameLogEntries(item);",StringComparison.Ordinal)
                && Read("MainWindow.Gathering.cs").Contains("OpenGameGatherable(source.ItemId)",StringComparison.Ordinal)
                && Read("MainWindow.CraftSimulation.cs").Contains("OpenGameRecipe(recipe)",StringComparison.Ordinal),"entries");
        }),
    ];
}
