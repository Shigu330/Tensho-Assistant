using TenshoAssistant;

// X-790 (0.1812, idea I-054): one CSV field escape for the four exports, no private copy left; the outputs stay the same (their own tests).
internal static class CsvText1812Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("CSV text 1812: quoted, quotes doubled, a leading formula sign prefixed",() =>
        {
            Check(CsvText.Field("Dark Matter")=="\"Dark Matter\"","quoted");
            Check(CsvText.Field("a \"b\" c")=="\"a \"\"b\"\" c\"","inner quotes doubled");
            foreach(var sign in new[]{"=","+","-","@"})Check(CsvText.Field(sign+"SUM(A1)")=="\"'"+sign+"SUM(A1)\"","a leading "+sign+" never a formula");
            Check(CsvText.Field("a=b")=="\"a=b\"" && CsvText.Field("")=="\"\"","only the first letter; empty stays empty");
            Check(CsvText.Field("x,y\nz")=="\"x,y\nz\"","commas and line breaks stay inside the quotes");
        }),
        ("CSV text 1812: the four exports go through it, no private copy left",() =>
        {
            foreach(var file in new[]{"FreeCompany/VoyageHistory.cs","Gathering/Notebooks.cs","Inventory/BankList.cs","Market/MarketBoard.cs"})
            {
                var code=Read("src/TenshoAssistant/"+file);
                Check(code.Contains("CsvText.Field(",StringComparison.Ordinal),file+": uses the shared escape");
                Check(!code.Contains("\"=+-@\"",StringComparison.Ordinal) && !code.Contains("private static string Quote(",StringComparison.Ordinal) && !code.Contains("private static string CsvQuote(",StringComparison.Ordinal),file+": no copy left");
            }
        }),
    ];
}
