// 0.840 (X-65): the list panel's lines written to the log in developer mode, on change.
internal static class ListPanelLog840Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List panel log: developer mode, on change only",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("if(listPanelLogged.GetValueOrDefault(key)!=line){listPanelLogged[key]=line;log.Information(\"List panel {0}: {1}.\",key,line);}"),"on change");
        }),
    ];
}
