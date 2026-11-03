// 0.428 (user report « cannot display a recipe… », for some recipes only): the game does not open a recipe above the job's level.
internal static class GameLogLevel428Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game log: no level check, open log closed first, the right recipe of several",() =>
        {
            var logs=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GameLogs.cs"));
            // 0.430 (user remark): the game's log shows recipes above the job's level too: no level check before opening.
            Check(!logs.Contains("level<recipe.Level") && !logs.Contains("GameLogs.LevelTooLow"),"no level check; the game decides");
            // 0.429: with the log already open the game refuses some recipes: an open log is closed first, then opened on the entry.
            var opener=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/GameLogOpener.Dalamud.cs"));
            Check(opener.Split("if(agent->AgentInterface.IsAgentActive()){agent->AgentInterface.Hide();return GameLogOpen.Closing;}").Length==3,"both logs closed first");
            Check(logs.Contains("if(result==GameLogOpen.Closing)return;") && logs.Contains("DateTimeOffset.UtcNow.AddSeconds(2)")
                && File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("mainWindow.ProcessGameLogRequest();"),"retried each frame, 2 s at most");
            // 0.429 (user remark): several recipes: the worn job first, then a job at the recipe's level.
            Check(logs.Contains("private RecipeDefinition PreferredRecipe(") && logs.Contains("recipe.CraftTypeId+8==worn ? 0:1"),"recipe choice");
        }),
    ];
}
