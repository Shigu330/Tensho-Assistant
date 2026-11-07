// 0.741 (user report): counts of the list panels fell to 0 for 200 ms at each item gathered.
internal static class ListBlink741Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List blink: stock counts read the last reading; the bag sort still reads only a sure one",() =>
        {
            Check(Read("MainWindow.Items.cs").Contains("var live = inventory.Shown;"),"counts");
            foreach(var file in new[]{"MainWindow.SortPreview.cs","MainWindow.InventorySortButton.cs"})
                Check(!Read(file).Contains("inventory.Shown"),file+": what acts never reads the display copy");
        }),
    ];
}
