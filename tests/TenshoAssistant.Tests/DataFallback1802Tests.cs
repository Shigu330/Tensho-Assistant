using Microsoft.Data.Sqlite;
using TenshoAssistant;

// 0.1802 (X-789, idea I-058): when the player's data file (TenshoData.db) is absent and cannot be created, the gil journal, the voyages, the
// achievements and the bank are written to the JSON files of before, and what is written then is not taken into the data file once it can be
// created (X-574). The main window says so on every page while it lasts. The condition (TenshoDataFile.JsonFallback) is read without opening
// the file (it is read in Draw); behaviour tests on real files.
internal static class DataFallback1802Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-1802-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Data fallback 1802: behaviour: a data file that cannot be created is the « json » mode, known once a family opened it, never before",() => InTemp(root =>
        {
            var notFolder = Path.Combine(root,"a file, not a folder");
            File.WriteAllText(notFolder,"x");
            var warned = new List<string>();
            var data = new TenshoDataFile(Path.Combine(notFolder,TenshoDataDatabase.FileName),(_,message) => warned.Add(message));
            Check(!data.JsonFallback,"not known before a family opens it (the banner never opens the file)");
            Check(warned.Count == 0,"nothing tried yet");
            Check(data.Database() is null && data.JsonFallback && data.Mode == "json","could not be created: the JSON files stay in use, the banner shows");
            Check(warned.Count == 1 && warned[0].Contains("could not be created",StringComparison.Ordinal),"said once in the log: " + string.Join(" | ",warned));
        })),
        ("Data fallback 1802: behaviour: a data file created, or there but unusable, is not the « json » mode (no banner)",() => InTemp(root =>
        {
            var data = new TenshoDataFile(Path.Combine(root,TenshoDataDatabase.FileName),(_,_) => { });
            Check(data.Database() is not null && !data.JsonFallback && data.Mode == "sqlite","created: no banner");
            var damaged = Path.Combine(root,"damaged");
            Directory.CreateDirectory(damaged);
            File.WriteAllText(Path.Combine(damaged,TenshoDataDatabase.FileName),"this is not a database, it only has its name");
            var blocked = new TenshoDataFile(Path.Combine(damaged,TenshoDataDatabase.FileName),(_,_) => { });
            var threw = false;
            try { blocked.Database(); } catch (IOException) { threw = true; }
            Check(threw && !blocked.JsonFallback && blocked.Mode == "blocked","there but unusable: the « blocked » mode, not this one");
        })),
        ("Data fallback 1802: wiring: the banner on every page, under the page's header, from the plugin's data file, in English and French",() =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("DrawPageHeader();\n                        DrawDataFallbackBanner();\n                        DrawHelpBanner();",StringComparison.Ordinal),"drawn before each page");
            var help = Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(help.Contains("if (TenshoData?.JsonFallback != true) return;",StringComparison.Ordinal)
                && help.Contains("localization.Format(\"Help.DataFallback\", TenshoDataDatabase.FileName)",StringComparison.Ordinal),"shown only in that mode");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("mainWindow.TenshoData=tenshoData;",StringComparison.Ordinal),"the plugin's own data file");
            foreach (var language in new[] { "en","fr" })
            {
                var text = Read("src/TenshoAssistant/Localization/Languages/" + language + ".json");
                var line = text.Split('\n').Single(l => l.StartsWith("    \"DataFallback\": ",StringComparison.Ordinal));
                Check(line.Contains("{0}",StringComparison.Ordinal),"the file named in " + language);
            }
        }),
    ];
}
