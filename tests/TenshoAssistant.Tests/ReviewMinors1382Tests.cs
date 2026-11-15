using TenshoAssistant.Caching;

// X-359 (0.1382, review of 0.1330 to 0.1345, minors): M1 the patch reports read again and written under the profile's lock, another
// client's report kept; M4 the Quests tab's keys made only when their sources change; M5 the downloaded zip removed; M6 no French words
// of the user quoted in the code's comments of the map's lists.
internal static class ReviewMinors1382Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
    private static PatchCatalogReport Report(string family, string scope, DateTimeOffset at) => new(family, scope, null, null, at, null, []);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1382 M1: the saved reports keep another client's, the new one replaces its own family", () =>
        {
            var at = new DateTimeOffset(2026, 11, 15, 5, 0, 0, TimeSpan.Zero);
            var saved = new[] { Report("Items", "fr", at), Report("Maps", "fr", at) };
            var joined = DataCache.WithReports(saved, [Report("Items", "fr", at.AddHours(1)), Report("Quests", "fr", at.AddHours(1))]);
            Check(joined.Length == 3 && joined.Single(report => report.Family == "Items").At == at.AddHours(1)
                && joined.Any(report => report.Family == "Maps") && joined.Any(report => report.Family == "Quests"), "Items replaced, Maps kept, Quests added");
            var code = Read("src/TenshoAssistant/Caching/DataCache.cs");
            var held = code.IndexOf("using(HoldProfile(lifetime.Token))\n            {", StringComparison.Ordinal) is var lf and >= 0 ? lf
                : code.IndexOf("using(HoldProfile(lifetime.Token))\r\n            {", StringComparison.Ordinal);
            Check(held > 0 && code.IndexOf("reports=WithReports(ReadReports(),made);", held, StringComparison.Ordinal) is var read and > 0
                && code.IndexOf("AtomicFile.Write(PatchReportsPath,", read, StringComparison.Ordinal) > read, "read and written under the lock");
        }),
        ("Review minors 1382 M4, M5, M6: wiring", () =>
        {
            var lists = Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs");
            Check(lists.Contains("if(!ReferenceEquals(keys.Events,events) || !ReferenceEquals(keys.Festivals,festivals?.State)", StringComparison.Ordinal), "M4: keys by reference");
            var script = Read("scripts/Get-Dalamud.ps1");
            Check(script.Contains("$downloaded = $true", StringComparison.Ordinal) && script.Contains("if ($downloaded -and (Test-Path -LiteralPath $zip)) { Remove-Item", StringComparison.Ordinal), "M5");
            var index = Read("src/TenshoAssistant/Maps/MapLocalIndex.cs");
            foreach (var words in new[] { "tu as aussi", "la carte a raison", "Toutes les cartes" })
                Check(!index.Contains(words, StringComparison.Ordinal), "M6: " + words);
        }),
    ];
}
