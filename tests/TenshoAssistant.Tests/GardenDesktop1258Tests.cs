// 0.1258 (X-178, version 4 of the specification, done before the crossbreed icons so they can be seen in the application's captures):
// the garden's seed list and crossbreeds are built by the window itself from the game's sheets, so the desktop application has them as the
// plugin does (R33). Before, Plugin.cs alone set them: the application greyed the seed list and said the crossbreeds could not be read.
internal static class GardenDesktop1258Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden desktop 1258: one way to give the garden its catalogues, for the plugin and the application",() =>
        {
            var data=Read("src/TenshoAssistant/MainWindow.GardenData.cs");
            Check(data.Contains("internal void ConfigureGarden(Dalamud.Plugin.Services.IDataManager data)"),"the window builds them from the sheets");
            Check(data.Contains("GardenSeedCatalog=()=>gardenSeeds??=ReadGardenSeeds(data);") && data.Contains("GardenCrossCatalog=()=>gardenCrosses??=Task.Run(()=>ReadGardenCrosses(data));"),"read once, the crossbreeds off the frame");
            Check(data.Contains("catch(Exception error){log.Warning(error,\"Garden seeds unavailable.\");return [];}") && data.Contains("catch(Exception error){log.Warning(error,\"Garden crossbreeds unavailable.\");throw;}"),"a failure is logged: an empty list, or a faulted task the page says");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("mainWindow.ConfigureGarden(data);") && !plugin.Contains("mainWindow.GardenSeedCatalog=") && !plugin.Contains("mainWindow.GardenCrossCatalog="),"the plugin calls it and no longer builds them itself");
            var host=Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("mainType.GetMethod(\"ConfigureGarden\",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(Window,[DataStub]);"),"the application calls it with its own game data");
            Check(Read("src/TenshoAssistant.Desktop/DesktopContract.cs").Contains("new(Main,Kind.Method,\"ConfigureGarden\",false,[\"Dalamud.Plugin.Services.IDataManager\"],"),"declared in the contract (a plugin without it is said at start)");
        }),
    ];
}
