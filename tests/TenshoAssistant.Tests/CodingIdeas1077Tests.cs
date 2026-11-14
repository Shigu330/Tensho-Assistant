// 0.1077 (audit A055, rest): the « ideas spotted while coding » of G-PROPOSALS gave their real state (five done, one not), and the
// code still holds what the done ones claim. 0.1348 (X-357): G-PROPOSALS is removed (git history keeps it); the code no longer points there.
internal static class CodingIdeas1077Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("G-PROPOSALS: removed, the code no longer names it, the done ideas still in the code",() =>
        {
            Check(!File.Exists(Path.GetFullPath("docs/project/G-PROPOSALS.md")),"G-PROPOSALS removed");
            foreach (var path in new[] { "src/TenshoAssistant/MainWindow.CraftMacroImport.cs","src/TenshoAssistant/MainWindow.RetainerSales.cs" })
                Check(!File.ReadAllText(Path.GetFullPath(path)).Contains("G-PROPOSALS"),path + " no longer names G-PROPOSALS");
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            Check(en.Contains("\"RetainerGil\": \"(+{0} with retainers)\"") && en.Contains("\"OtherCharacters\": \"On your other characters:\"") && en.Contains("\"ListTitle\": \"Annotated items ({0})\""),"the texts the done ideas cite still exist");
        }),
    ];
}
