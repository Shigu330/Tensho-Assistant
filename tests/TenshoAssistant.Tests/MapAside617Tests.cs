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
            Check(view.Contains("if(repeated && !clicked)request=request.Plain();"),"only when unchanged and not clicked");
        }),
        ("Map request (0.634): a dungeon quest shows the entrance, from outside the dungeon",() =>
        {
            var inside=new TenshoAssistant.Maps.MapDefinition(752,1050,"","Alzadaal","",1,200,MarkerRange:900);
            var outside=new TenshoAssistant.Maps.MapDefinition(695,958,"","Thavnair","",0,100,MarkerRange:478);
            var entrances=TenshoAssistant.Maps.MapEntrances.Build([(1050u,4154u,"Legs"),(1037u,48u,"Mines"),(1u,5u,"Absent")],[(478u,4154u,"Legs",754f,1634f),(478u,77u,"MINES",10f,20f),(999u,5u,"Absent",1f,1f)],[inside,outside]);
            Check(entrances[1050]==new TenshoAssistant.Maps.MapEntrance(695,754,1634),"by its place's row");
            Check(entrances[1037].X==10f,"or by the same name");
            Check(!entrances.ContainsKey(1),"a marker on no known map is left out");
            var catalog=new TenshoAssistant.Maps.MapCatalog([inside,outside],default,null,entrances);
            var quest=new TenshoAssistant.Maps.GameMapRequest(752,752,1050,12,QuestLog:true,Target:new System.Numerics.Vector2(5,5));
            Check(quest.Resolve(catalog)==695,"the entrance's map");
            Check((quest with {Current=752}).Resolve(catalog)==752 || (quest with {Current=752}).Resolve(catalog)==0,"inside: the dungeon's map");
            Check(quest.EntranceTarget(catalog)!.X==754,"focused there");
            Check((quest with {RouteMap=695}).EntranceTarget(catalog) is null,"the game's own way first");
            Check((quest with {QuestLog=false,Target=null}).EntranceTarget(catalog) is null,"not for a plain opening");
        }),
        ("Map request (0.638): a dungeon asked from the world map opens the way in, not its own map",() =>
        {
            var dungeon=new TenshoAssistant.Maps.MapDefinition(946,1248,"","Dungeon","",0,200);
            var zone=new TenshoAssistant.Maps.MapDefinition(859,1189,"","Yak T'el","",0,100);
            var catalog=new TenshoAssistant.Maps.MapCatalog([dungeon,zone]);
            var request=new TenshoAssistant.Maps.GameMapRequest(919,919,1248,4,QuestLog:true,Target:new System.Numerics.Vector2(150,800),RouteMap:859,Route:new System.Numerics.Vector2(-528,671));
            Check(request.Resolve(catalog)==859,"the game's way in, captured in the log");
            Check(request.UsesRoute(zone),"centred on it");
            Check((request with {Current=946}).Resolve(catalog)==946,"inside the dungeon: its own map");
        }),
        ("Map request (0.637): a quest always adjusts the map and keeps the zoom",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("mapFocusRequest=null;mapZoom=MapProjection.ClampZoom(mapZoom);mapCenterRequest=pixel;"),"centred on it, same zoom");
            Check(view.Contains("mapFocusRequest=null;mapCenterRequest=new Vector2(MapProjection.TextureSize/2f);mapFitRequested=false;"),"not found: the map's centre, same zoom");
        }),
        ("Map request (0.633): away from a quest's zone, the map of the NPC that leads there",() =>
        {
            var village=new TenshoAssistant.Maps.MapDefinition(1025,1269,"","Phantom Village","",0,400);
            var city=new TenshoAssistant.Maps.MapDefinition(855,1185,"","Tuliyollal","",0,180);
            var catalog=new TenshoAssistant.Maps.MapCatalog([village,city],default,new Dictionary<uint,uint>{[1278]=1025});
            var npc=new System.Numerics.Vector2(206,63);
            var away=new TenshoAssistant.Maps.GameMapRequest(919,919,1278,4,QuestLog:true,Target:new System.Numerics.Vector2(29,13),RouteMap:855,Route:npc);
            Check(away.Resolve(catalog)==855,"the way there");
            Check(away.UsesRoute(city) && !away.UsesRoute(village),"focused on the NPC only on that map");
            Check((away with {Current=1025}).Resolve(catalog)==1025,"already in the zone: its own map");
            Check(!away.Plain().Pointed && away.Plain().RouteMap==0,"the map key forgets it");
            Check((away with {RouteIcon=70975}).Plain().RouteIcon==0,"and its icon");
            var ordinary=new TenshoAssistant.Maps.GameMapRequest(855,855,1185,4,QuestLog:true,Target:npc,RouteMap:855,Route:npc);
            Check(!ordinary.UsesRoute(city),"a quest in that zone keeps its own target");
        }),
        ("Map request (0.628): a quest target asked on the world map opens its territory's own map",() =>
        {
            var village=new TenshoAssistant.Maps.MapDefinition(1025,1269,"","Phantom Village","",0,400);
            var request=new TenshoAssistant.Maps.GameMapRequest(919,919,1278,12,QuestLog:true,Target:new System.Numerics.Vector2(29,13));
            Check(request.Resolve(new TenshoAssistant.Maps.MapCatalog([village],default,new Dictionary<uint,uint>{[1278]=1025}))==1025,"the map the territory names");
            Check(request.Resolve(new TenshoAssistant.Maps.MapCatalog([village]))==919,"unknown: the map the game asked");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("if(focus.Target is { } target && map.TerritoryId!=0)return"),"never placed on a world or region map");
            Check(view.Contains("focus.TargetIcon!=0 && map.TerritoryId!=0 && !focus.UsesRoute(map) &&"),"no icon there either");
        }),
        ("Map request (0.629): a click just before asks for the quest again, once; the map key does not",() =>
        {
            var clicks=new TenshoAssistant.Maps.MapClickMemory();
            var now=new DateTimeOffset(2026,11,6,1,0,0,TimeSpan.Zero);
            Check(!clicks.Take(now),"no click");
            clicks.Clicked(now);
            Check(!clicks.Take(now+TimeSpan.FromSeconds(1)),"too old: the map key");
            clicks.Clicked(now);
            Check(clicks.Take(now+TimeSpan.FromMilliseconds(100)),"just clicked");
            Check(!clicks.Take(now+TimeSpan.FromMilliseconds(110)),"only once");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs"));
            Check(view.Contains("!Occupied() && lastPointed.Length>0 && lastPointed==lastRequest && clicks.Take(now)"),"the map stays open on that click, never during an event");
            Check(view.Contains("open(clickedAgain);") && !view.Contains("shown->Show();"),"on the quest clicked, which the game forgot, without showing its map again (0.636)");
            Check(view.Contains("if(detached && !clicked && tenshoOpen())"),"the map key still closes it");
        }),
        ("Map enlarged: no stepping aside then, the same as before otherwise",() =>
        {
            var tracker=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.TrackerWindow.cs")).Replace("\r\n","\n");
            Check(tracker.Contains("if(UI.MapEnlarged.Is(mapRestore is not null,size.X,size.Y,screen.X,screen.Y))return false;\n        var byWindow=BehindGame(at,size);var byPanel=Panels?.Over(at,size)==true;"),"checked first");
        }),
    ];
}
