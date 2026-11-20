// 0.1590 (X-646, review of 0.1588, M2, R07): the island's sheets are read by a task, like FatesLoad, DeepDungeonLoad, AlliedCatalogLoad
// and ZoneNamesLoad, never in the first drawing of the workshop.
internal static class IslandSheets1590Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Root()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);
        while(dir!=null && !Directory.Exists(Path.Combine(dir.FullName,"src","TenshoAssistant")))dir=dir.Parent;
        return dir!.FullName;
    }

    public static IEnumerable<(string Name,Action Test)> Cases =>
    [
        ("Island sheets 1590: read by a task, a waiting line meanwhile",() =>
        {
            var ui=File.ReadAllText(Path.Combine(Root(),"src","TenshoAssistant","MainWindow.IslandSanctuary.cs")).Replace("\r\n","\n");
            Check(!ui.Contains("IslandCatalogLoad?.Invoke()") && !ui.Contains("IslandCatalogLoad()"),"never read in the drawing");
            Check(ui.Contains("islandCatalogTask=Task.Run(load);"),"read by a task");
            Check(ui.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"Island.Loading\"))"),"a waiting line while reading");
            Check(ui.Contains("islandCatalogFailed=islandCatalog is not { Valid:true };"),"a failed reading is not tried again at each frame");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.Combine(Root(),"src","TenshoAssistant","Localization","Languages",language+".json"));
                var island=text.IndexOf("\n  \"Island\": {",StringComparison.Ordinal);
                Check(island>0 && text.IndexOf("\"Loading\"",island,StringComparison.Ordinal) is var at && at>0 && at<text.IndexOf("\n  }",island,StringComparison.Ordinal),language+": Island.Loading");
            }
        }),
    ];
}
