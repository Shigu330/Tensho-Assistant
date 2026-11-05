using TenshoAssistant;

// 0.328 (queue step e, the map first: defects of the read-only map audit, confirmed by a skeptic). The game map stays on screen when
// the Tensho map cannot be drawn; bounded and isolated native reads (R-14); zoom around the view centre; edge markers follow the
// layers; the remaining fixes are pinned in the source (UI and native code are outside the managed test project).
internal static class Map328Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map settings: a null MapView becomes the defaults, and « restore defaults » of the icons also clears the right-click mode",() =>
        {
            var configuration = new Configuration { MapView = null! };
            Check(configuration.MapView is not null,"null coerced to the defaults");
            var settings = new MapViewSettings { RightClickIgnore = [60453] };
            settings.Reset(0);
            Check(settings.RightClickIgnore is null && !settings.RightClickIgnored(60453),"right-click mode reset with the global icon defaults");
        }),
        ("Game map kept when the Tensho map cannot be drawn; « open the game map » brings a hosted map back",() =>
        {
            var replacer = Read("Maps/NativeMapReplacer.Dalamud.cs");
            Check(replacer.Contains("keepGameMap=!options.ReplaceGameMap || bypass || !canShow() || !canMove;",StringComparison.Ordinal),"decided with canShow");
            Check(Read("Plugin.cs").Contains("()=>mainWindow.MapViewVisible()",StringComparison.Ordinal),"canShow is the map's own visibility (catalogue, combat, duty)");
        }),
        ("Live map reads: vectors bounded, each secondary read isolated so position and flag survive",() =>
        {
            var reader = Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("var count=agent->EventMarkers.LongCount;",StringComparison.Ordinal) && reader.Contains("if(++visited>1024 || result.Count>=256)break;",StringComparison.Ordinal),"event markers bounded");
            Check(reader.Contains("list->LongCount is <0 or >512",StringComparison.Ordinal),"teleport costs bounded");
            Check(Read("Maps/MapMarkerSource.Dalamud.cs").Contains("public bool Failed(",StringComparison.Ordinal),"a failed marker read is not cached as « no icons »");
        }),
        ("Map canvas: zoom keeps the view centre, edge markers follow hidden layers, errors and missing data are said",() =>
        {
            var view = Read("MainWindow.MapView.cs");
            Check(view.Contains("private void ZoomTo(float zoom)",StringComparison.Ordinal) && view.Split("ZoomTo(").Length - 1 >= 4,"+, − and the slider zoom around the centre");
            Check(view.Contains("settings.SizeOf(FlagIcon",StringComparison.Ordinal) && view.Contains("settings.SizeOf(GlyphParty",StringComparison.Ordinal),"edge markers skip hidden icons");
            Check(view.Contains("\"MapView.PositionUnknown\"",StringComparison.Ordinal) && view.Contains("\"MapView.MarkersUnavailable\"",StringComparison.Ordinal),"unknown position and unreadable icons are said");
            Check(view.Contains("!settings.RightClickIgnored(chosen.Icon) && sightseeingTip is null",StringComparison.Ordinal),"over a vista the general menu (with its alarm) opens");
            Check(!view.Contains("if(mapOpenError is { } error)WrappedTooltip(error);",StringComparison.Ordinal),"a map error no longer follows the mouse everywhere");
        }),
    ];
}
