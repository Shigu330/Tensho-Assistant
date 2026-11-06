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
        ("Map place (0.621): where the player stands, as the game's map writes it",() =>
        {
            Check(TenshoAssistant.Maps.MapMenus.PlaceLines(12,12,"L'Octant","").SequenceEqual(["> L'Octant"]),"no sub-place: no line, not even the game's « -- » (user's choice)");
            Check(TenshoAssistant.Maps.MapMenus.PlaceLines(12,12,"L'Octant","Quai").SequenceEqual(["> L'Octant","> Quai"]),"a sub-place");
            Check(TenshoAssistant.Maps.MapMenus.PlaceLines(12,11,"L'Octant","").Count==0,"another map than the player's");
            Check(TenshoAssistant.Maps.MapMenus.PlaceLines(0,0,"L'Octant","").Count==0 && TenshoAssistant.Maps.MapMenus.PlaceLines(12,12,"","").Count==0,"unknown");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("try{ReadPlace();Read(\"place\");}catch(Exception error)"),"guarded on the framework thread");
        }),
        ("Map request (0.627): the same request again is what the game kept: the map opens on the player",() =>
        {
            var kept=new TenshoAssistant.Maps.GameMapRequest(919,919,1278,12,QuestLog:true,Target:new System.Numerics.Vector2(29,13));
            Check(kept.Pointed,"captured in the log: a quest request");
            var plain=kept.Plain();
            Check(!plain.Pointed && plain.Selected==12 && plain.Requested==12 && plain.Target is null,"asks for nothing, on the player's map");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs"));
            Check(view.Contains("if(signature==lastPointed)request=request.Plain();"),"only when unchanged");
        }),
        ("Map request (0.628): a quest target asked on the world map opens its territory's own map",() =>
        {
            var village=new TenshoAssistant.Maps.MapDefinition(1025,1269,"","Phantom Village","",0,400);
            var request=new TenshoAssistant.Maps.GameMapRequest(919,919,1278,12,QuestLog:true,Target:new System.Numerics.Vector2(29,13));
            Check(request.Resolve(new TenshoAssistant.Maps.MapCatalog([village],default,new Dictionary<uint,uint>{[1278]=1025}))==1025,"the map the territory names");
            Check(request.Resolve(new TenshoAssistant.Maps.MapCatalog([village]))==919,"unknown: the map the game asked");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("if(focus.Target is { } target && map.TerritoryId!=0)return"),"never placed on a world or region map");
            Check(view.Contains("focus.TargetIcon!=0 && map.TerritoryId!=0 ?"),"no icon there either");
        }),
        ("Map enlarged: no stepping aside then, the same as before otherwise",() =>
        {
            var tracker=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.TrackerWindow.cs")).Replace("\r\n","\n");
            Check(tracker.Contains("if(UI.MapEnlarged.Is(mapRestore is not null,size.X,size.Y,screen.X,screen.Y))return false;\n        var byWindow=BehindGame(at,size);var byPanel=Panels?.Over(at,size)==true;"),"checked first");
        }),
    ];
}
