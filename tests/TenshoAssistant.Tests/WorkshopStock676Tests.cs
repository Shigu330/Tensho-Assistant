// 0.676 (user report): the workshop's stock column stayed on a dash while the item's tooltip gave the right count.
internal static class WorkshopStock676Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Stocks: every page that reads the holdings cache also refreshes it",() =>
        {
            var folder=Path.GetFullPath("src/TenshoAssistant");
            var checkedPages=0;
            foreach(var file in Directory.GetFiles(folder,"MainWindow*.cs"))
            {
                var text=File.ReadAllText(file);
                if(!text.Contains("GetRecipeHoldings"))continue;
                checkedPages++;
                Check(text.Contains("RefreshRecipeHoldingsCache();") || text.Contains("void RefreshRecipeHoldingsCache"),Path.GetFileName(file)+" reads stocks that nothing refreshes");
            }
            Check(checkedPages>5,"the pages were found");
        }),
    ];
}
