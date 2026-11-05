using TenshoAssistant.UI;

// 0.618 (user report): the enlarged map stays; at its usual size it steps aside.
internal static class MapAside617Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map enlarged: by the button, or by its size after a reload; the usual size is not",() =>
        {
            Check(MapEnlarged.Is(true,400,300,1920,1080),"the button");
            Check(MapEnlarged.Is(false,1728,972,1920,1080),"90 % of the screen, button state lost by a reload (the size in the user's file)");
            Check(!MapEnlarged.Is(false,720,760,1920,1080),"the usual size");
            Check(!MapEnlarged.Is(false,1800,500,1920,1080),"wide only");
        }),
        ("Map floors (0.619): a name, a number, or the zone's name rather than « 0 »",() =>
        {
            TenshoAssistant.Maps.MapDefinition Map(string subzone,int index)=>new(1,1,"Noscea","Limsa Lominsa",subzone,index,100,1,"",0,0,0,1,1,1);
            Check(TenshoAssistant.Maps.MapMenus.FloorLabel(Map("Airship Landing",0))=="Airship Landing","a name");
            Check(TenshoAssistant.Maps.MapMenus.FloorLabel(Map("",3))=="3","a dungeon's floor");
            Check(TenshoAssistant.Maps.MapMenus.FloorLabel(Map("",0))=="Limsa Lominsa","no « 0 »");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(!view.Contains("floor.MapIndex.ToString()") && !view.Contains("$\"{map.MapIndex}\""),"both menus, the one above the map included (user report)");
        }),
        ("Map enlarged: no stepping aside then, the same as before otherwise",() =>
        {
            var tracker=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.TrackerWindow.cs")).Replace("\r\n","\n");
            Check(tracker.Contains("if(UI.MapEnlarged.Is(mapRestore is not null,size.X,size.Y,screen.X,screen.Y))return false;\n        return BehindGame(at,size) || Panels?.Over(at,size)==true;"),"checked first");
        }),
    ];
}
