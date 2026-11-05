// 0.604 (C35): the fishing help reads nothing in the frame.
internal static class FishingHelp604Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fishing help: the player copied on the framework thread, the tables off the frame",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("framework.Update+=copyFishingNow") && plugin.Contains("framework.Update-=copyFishingNow"),"copied, with its clean-up");
            Check(plugin.Contains("mainWindow.ConfigureFishingHelp(()=>Volatile.Read(ref fishingNowCopy).Value,"),"the frame reads the copy");
            Check(plugin.Contains("fishingSpotsTask??=Task.Run(") && plugin.Contains("bountifulBases??=Task.Run("),"both tables on a worker");
            Check(plugin.Contains("if(!fishingSpotsTask.IsCompletedSuccessfully)return null;") && plugin.Contains("bountifulBases.IsCompletedSuccessfully &&"),"nothing until they are ready");
        }),
    ];
}
