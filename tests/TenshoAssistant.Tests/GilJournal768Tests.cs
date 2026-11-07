// 0.768 (X-18, user request): the gil journal with icons, and what « Other » was.
internal static class GilJournal768Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil journal: icons, colours, and the windows of an « Other » change in developer mode",() =>
        {
            var page=Read("MainWindow.GilJournal.cs");
            Check(page.Contains("GilCategoryIcon(category).ToIconString()") && page.Contains("Find(1)"),"icons");
            foreach(var category in Enum.GetValues<TenshoAssistant.Inventory.GilCategory>())Check(page.Contains("GilCategory."+category) || category==TenshoAssistant.Inventory.GilCategory.Other,category.ToString());
            var watcher=Read("Inventory/GilWatcher.Dalamud.cs");
            Check(watcher.Contains("if(category==GilCategory.Other && developerMode())") && watcher.Contains("\"Gil other: "),"what Other was");
        }),
    ];
}
