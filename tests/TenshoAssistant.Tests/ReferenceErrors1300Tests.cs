using TenshoAssistant.Desktop;

// 0.1300 (X-285, the user's look at the gallery on 13/11): a reference picture shows the page, never an error or a load that never ends
// caused by the desktop application; the sheet loaders are given by one method to the game and the application.
internal static class ReferenceErrors1300Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reference errors 1300: error and waiting keys told from the others", () =>
        {
            foreach (var key in new[] { "Allied.SheetsUnavailable", "OceanFishing.Unavailable", "Weather.Error", "Common.StateFailed", "Squadron.ReadFailed" })
                Check(ReferenceErrors.IsError(key), key);
            foreach (var key in new[] { "Weather.ErrorTip", "Allied.Title", "Desktop.ReadInGame", "Settings.UnavailableHint", "Lexicon.Loading" })
                Check(!ReferenceErrors.IsError(key), "not an error: " + key);
            Check(ReferenceErrors.IsWaiting("DeepDungeon.Loading") && ReferenceErrors.IsWaiting("Common.StateWaiting") && !ReferenceErrors.IsWaiting("Lexicon.Title"), "waits");
            Check(ReferenceErrors.Shown(["Lexicon.Loading", "Allied.Title", "Weather.Error", "Weather.Error"]).SequenceEqual(new[] { "Lexicon.Loading", "Weather.Error" }), "sorted, once");
            Check(ReferenceErrors.Shown(["Lexicon.Loading", "Allied.Title"], loadingPicture: true).Length == 0, "a loading picture may wait");
        }),
        ("Reference errors 1300: wiring: the sheet loaders are given by one method, called by the plugin and the desktop application", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("mainWindow.UseSheetLoaders(data,log);"), "the plugin calls it");
            foreach (var name in new[] { "IslandCatalogLoad", "DeliveryClientsLoad", "FatesLoad", "LexiconLoad", "DeepDungeonLoad", "FieldCatalogLoad", "GcCatalogLoad", "AlliedCatalogLoad", "RelicCatalogLoad", "CarnivaleStagesLoad" })
            {
                Check(!plugin.Contains("mainWindow." + name + "="), name + " given only by UseSheetLoaders");
                Check(Read("src/TenshoAssistant/MainWindow.SheetLoaders.cs").Contains("        " + name + "="), name + " in UseSheetLoaders");
            }
            Check(!plugin.Contains("mainWindow.OceanTimetable=") && !plugin.Contains("IKDRouteTable>().OrderBy") && Read("src/TenshoAssistant/MainWindow.SheetLoaders.cs").Contains("OceanTimetable=()=>ocean.Value;"), "the ocean timetable too");
            var host = Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("GetMethod(\"UseSheetLoaders\"") && !host.Contains("\"LexiconLoad\""), "the desktop application calls the same method");
            var references = Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("[\"error_keys\"]=shown") && references.Contains("asked.Clear();") && references.Contains("&& !waiting;"),
                "the pictured frame's keys go to the report; a waiting page is drawn on");
            Check(Read("src/TenshoAssistant/Localization/LocalizationManager.cs").Contains("KeyRead?.Invoke(key);"), "the keys asked are heard");
        }),
        ("Reference errors 1300: the pages read in the game say so in the desktop application, never a load that never ends", () =>
        {
            foreach (var (file, key) in new[] { ("Collections", "CollectionsBrowser.Loading"), ("TripleTriad", "TriadUi.Loading"), ("Quests", "QuestsBrowser.Loading"),
                         ("SpecialDeliveries", "SpecialDeliveries.Loading"), ("Discoveries", "DiscoveriesUi.Loading"), ("Beastmaster", "BeastUi.Loading") })
            {
                var text = Read($"src/TenshoAssistant/MainWindow.{file}.cs");
                Check(text.Contains($"LoadingText(\"{key}\")") && !text.Contains($"localization.Get(\"{key}\")"), file);
            }
            Check(Read("src/TenshoAssistant/MainWindow.SheetLoaders.cs").Contains("localization.Get(inDesktopApp ? \"Desktop.ReadInGame\":key)"), "one helper");
            foreach (var (file, read) in new[] { ("AlliedSocieties", "if(allied is not { } current){ReadInGameNote();return;}"), ("CustomDeliveries", "if(deliverySnapshot is not { } current){ReadInGameNote();return;}"),
                         ("GrandCompanies", "if(gcSnapshot is not { } current){ReadInGameNote();return;}"), ("IslandSanctuary", "if(island is not { } current){ReadInGameNote();return;}"),
                         ("MaskedCarnivale", "if(carnivaleSnapshot is not { } current){ReadInGameNote();return;}"), ("RelicWeapons", "case null:ReadInGameNote();return;") })
                Check(Read($"src/TenshoAssistant/MainWindow.{file}.cs").Contains(read), file + ": what the player has is said to be read in the game");
            Check(Read("src/TenshoAssistant/MainWindow.GrandCompanies.cs").Contains("else if(!gcTried && GcCatalogLoad is not null){gcTried=true;gcCatalog=GcCatalogLoad(false);}"),
                "the Grand Companies' sheets without the character (the desktop application)");
        }),
    ];
}
