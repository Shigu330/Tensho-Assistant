// 0.601 (C36): a panel whose reading fails says so.
internal static class ReadFailed601Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Failed reading: each panel says it, beside the game's window, and Diagnostics names it",() =>
        {
            Check(Source("MainWindow.CraftLog.cs").Contains("if(!windowDisposed && configuration.CraftLogHelperEnabled && craftLog.Error is not null)DrawReadFailedPanel(\"TenshoCraftLogFailed\",\"RecipeNote\",\"CraftLog\");"),"crafting log");
            Check(Source("MainWindow.CraftProjection.cs").Contains("DrawReadFailedPanel(\"TenshoCraftLiveFailed\",\"Synthesis\",\"CraftLive\")"),"synthesis");
            Check(Source("MainWindow.GatheringPanel.cs").Contains("DrawReadFailedPanel(\"TenshoGatheringFailed\",\"Gathering\",\"Gathering\")"),"gathering");
            Check(Source("MainWindow.Diagnostics.cs").Contains("FailedReadings()"),"Diagnostics");
        }),
        ("Failed reading: the cause stays in the log, the words in both languages",() =>
        {
            Check(!Source("MainWindow.ReadFailed.cs").Contains("error.Message"),"no raw message");
            foreach(var language in new[]{"en","fr"})
            {
                using var document=System.Text.Json.JsonDocument.Parse(Source($"Localization/Languages/{language}.json"));
                var section=document.RootElement.GetProperty("ReadFailed");
                foreach(var key in new[]{"Panel","CraftLog","CraftLive","Gathering","Diagnostics"})Check(section.TryGetProperty(key,out _),$"{language} {key}");
            }
        }),
    ];
}
