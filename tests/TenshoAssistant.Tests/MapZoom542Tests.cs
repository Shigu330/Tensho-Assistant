using TenshoAssistant.Maps;

// 0.542 (P1): the map's zoom at opening.
internal static class MapZoom542Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map zoom at opening: last one by default, 50 % the first time; saved once at rest",() =>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.OpenZoom==MapOpenZoom.Last && settings.LastZoom==0.5f && settings.OpenZoomValue==0.5f && settings.FocusZoom==1f,"nothing changes until set");
            var memory=new MapZoomMemory(TimeSpan.FromSeconds(2));
            Check(memory.Tick(0.5f,0.5f,TimeSpan.Zero) is null && memory.Tick(0.5f,0.5f,TimeSpan.FromSeconds(9)) is null,"same as saved: nothing");
            Check(memory.Tick(1f,0.5f,TimeSpan.FromSeconds(10)) is null && memory.Tick(1f,0.5f,TimeSpan.FromSeconds(11)) is null,"not while moving");
            Check(memory.Tick(1.2f,0.5f,TimeSpan.FromSeconds(11.5)) is null && memory.Tick(1.2f,0.5f,TimeSpan.FromSeconds(13)) is null,"a new zoom starts the rest again");
            Check(memory.Tick(1.2f,0.5f,TimeSpan.FromSeconds(13.5))==1.2f,"saved once at rest");
            Check(memory.Tick(1.2f,1.2f,TimeSpan.FromSeconds(20)) is null,"once");
            var view=Read("MainWindow.MapView.cs");
            Check(view.Contains("if(mapId==0)ApplyOpenZoom();") && view.Contains("mapFitRequested=mapId!=0;"),"a chosen map still opens whole");
            Check(!view.Contains("Math.Max(mapZoom,1f)") && !Read("MainWindow.ChatMapLinks.cs").Contains("Math.Max(mapZoom,1f)"),"the zoom on a point is the setting");
            Check(Read("Plugin.cs").Contains("mainWindow.KeepMapZoom();"),"kept also when the map has just closed");
            foreach(var key in new[]{"OpenZoom","OpenZoomLast","OpenZoomFixed","OpenZoomWhole","OpenZoomHelp","OpenZoomValue","FocusZoom","FocusZoomHelp"})
                Check(Read("Localization/Languages/fr.json").Contains($"\"{key}\":") && Read("Localization/Languages/en.json").Contains($"\"{key}\":"),key);
        }),
    ];
}
