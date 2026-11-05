using TenshoAssistant.UI;

// 0.617 (user report): a window already open must not hide the map asked for.
internal static class MapAside617Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static GameWindow Window(string name)=>new(name,100,100,300,300,1);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map aside: what was open first does not count, what comes later does",() =>
        {
            var with=new OpenedWith();
            with.Opened([Window("ContentsFinder")]);
            Check(with.Later([Window("ContentsFinder")]).Count==0,"already there");
            Check(with.Later([Window("ContentsFinder"),Window("Inventory")]) is [{Name:"Inventory"}],"a new one counts");
        }),
        ("Map aside: closed and opened again, it counts; a new opening starts over",() =>
        {
            var with=new OpenedWith();
            with.Opened([Window("ContentsFinder")]);
            Check(with.Later([]).Count==0 && with.Present.Count==0,"closed: forgotten");
            Check(with.Later([Window("ContentsFinder")]).Count==1,"opened again after the map");
            with.Opened([Window("ContentsFinder")]);
            Check(with.Later([Window("ContentsFinder")]).Count==0,"the map asked again");
        }),
        ("Map aside: wired on the map window and its panels",() =>
        {
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MapViewWindow.cs"));
            Check(window.Contains("if(frame!=lastConditionFrame+1){owner.MapOpened();"),"on opening");
            Check(window.Contains("if (lastPosition is { } before && (before != position || lastSize != size)) owner.MapOpened();"),"on « Enlarge », on moving and resizing: it must not vanish");
            var tracker=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.TrackerWindow.cs"));
            Check(tracker.Contains("Panels?.Over(at,size,mapOpenedWith.Present)"),"their panels too");
            Check(tracker.Contains("Map {0}; windows coming after it"),"the reason in the log");
        }),
    ];
}
