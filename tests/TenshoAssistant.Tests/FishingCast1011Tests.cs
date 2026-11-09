using TenshoAssistant.Gathering;

// C10 (0.1011): the fishing stopwatch restarts at each cast, from the line's CastingOut state read on the framework thread; the manual button stays.
internal static class FishingCast1011Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fishing cast 1011: a newer cast restarts the stopwatch, an older one or none keeps it",() =>
        {
            DateTimeOffset? Run(DateTimeOffset? since,DateTimeOffset? cast)=>FishingStopwatch.FromCast(since,cast);
            var start=new DateTimeOffset(2026,11,9,12,0,0,TimeSpan.Zero);
            Check(Run(start,start.AddSeconds(20))==start.AddSeconds(20),"newer cast");
            Check(Run(start,start.AddSeconds(-5))==start,"cast before fishing or before a manual restart");
            Check(Run(start,null)==start,"no cast read");
            Check(Run(null,start)==start,"first cast");
        }),
        ("Fishing cast 1011: the line read every frame, kept on change, wired to both stopwatches",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/FishingBaitReader.Dalamud.cs"));
            Check(reader.Contains("if(disposed)return;\n        ReadLine();") || reader.Contains("if(disposed)return;\r\n        ReadLine();"),"every frame, before the half-second throttle");
            Check(reader.Contains("if(state==lineState)return;") && reader.Contains("if(state==1)Volatile.Write(ref castAt"),"CastingOut only, on change");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.FishingCastAt=()=>baitReader?.CastAt;"),"wired");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs")).Contains("fishingSince=FromCast(fishingSince,FishingCastAt?.Invoke());"),"fishing help");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs")).Contains("oceanFishingSince=FromCast(oceanFishingSince,FishingCastAt?.Invoke());"),"ocean fishing");
        }),
    ];
}
