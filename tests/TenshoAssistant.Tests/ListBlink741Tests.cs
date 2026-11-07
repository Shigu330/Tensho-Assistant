// 0.741 (user report): counts of the list panels fell to 0 for 200 ms at each item gathered.
internal static class ListBlink741Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List blink: stock counts read the last reading; the bag sort still reads only a sure one",() =>
        {
            var items=Read("MainWindow.Items.cs");
            var refresh=items[items.IndexOf("private void RefreshRecipeHoldingsCache()")..];
            refresh=refresh[..refresh.IndexOf("recipeHoldingsLive = live;")];
            // 0.744: the first fix landed on another function that began with the same line; this one feeds the lists and panels.
            Check(refresh.Contains("var live = inventory.Shown;") && !refresh.Contains("inventory.Current"),"lists and panels");
            foreach(var file in new[]{"MainWindow.SortPreview.cs","MainWindow.InventorySortButton.cs"})
                Check(!Read(file).Contains("inventory.Shown"),file+": what acts never reads the display copy");
        }),
    ];
}
