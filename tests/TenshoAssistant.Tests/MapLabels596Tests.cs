using System.Numerics;
using TenshoAssistant.Maps;

// 0.596 (P12): the names the game writes beside its icons.
internal static class MapLabels596Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map labels: left, right, below, above the icon, clear of it",() =>
        {
            var text=new Vector2(60,10);const float icon=20;
            Check(MapLabels.Offset(1,icon,text)==new Vector2(-71,-5),"left: ends before the icon");
            Check(MapLabels.Offset(2,icon,text)==new Vector2(11,-5),"right: starts after it");
            Check(MapLabels.Offset(3,icon,text)==new Vector2(-30,11),"below, centred");
            Check(MapLabels.Offset(4,icon,text)==new Vector2(-30,-21),"above, centred");
        }),
        ("Map labels: a value not established goes to the right",() =>
        {
            foreach(var value in new byte[]{0,5,8,11})Check(MapLabels.Offset(value,20,new(60,10))==MapLabels.Offset(2,20,new(60,10)),$"value {value}");
        }),
        ("Map labels (0.623): zoomed out, only the map links; none on top of another, links first",() =>
        {
            Check(MapLabels.Shown(true,0.3f) && !MapLabels.Shown(false,0.3f) && MapLabels.Shown(false,MapLabels.DetailZoom),"by zoom");
            (Vector2,Vector2,bool)[] names=[(new(0,0),new(50,10),false),(new(10,0),new(50,10),true),(new(100,0),new(50,10),false),(new(0,10),new(50,10),false)];
            Check(MapLabels.Fitting(names).Order().SequenceEqual([1,2,3]),"the link wins over the name it touches; touching edges do not count");
            Check(MapLabels.Fitting([]).Length==0,"none");
        }),
        ("Map labels (0.626): the name of a map link opens that map, through the same click as its icon",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("if(overLink)linkUnderMouse=label.Link;"),"over the name");
            Check(view.IndexOf("if(overLink)linkUnderMouse=label.Link;")<view.IndexOf("if(linkUnderMouse is { } link && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)NavigateMap(link);"),"before the click is read");
        }),
        ("Map labels (0.631): a link's whole-zone picture is not a small icon",() =>
        {
            Check(TenshoAssistant.Maps.MapLabels.IsZoneImage(855,1024),"Tuliyollal on Yok Tural");
            Check(!TenshoAssistant.Maps.MapLabels.IsZoneImage(855,64),"a small link icon stays one");
            Check(!TenshoAssistant.Maps.MapLabels.IsZoneImage(0,1024),"not a link");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.IndexOf("0.631: the zones of a region map")<view.IndexOf("var icons=new List<MapIconDraw>();"),"under every icon");
        }),
        ("Map labels (0.632): zones and crystals on a region map, the world's own choice",() =>
        {
            // Wachunpelo on Urqopacha: pixel 1356, 607, link at 479, 799; the game's crystal is at about 618, 624 of Yok Tural.
            var crystal=TenshoAssistant.Maps.MapLabels.ZonePixel(new System.Numerics.Vector2(479,799),new System.Numerics.Vector2(1356,607));
            Check(Math.Abs(crystal.X-618)<4 && Math.Abs(crystal.Y-624)<4,"measured on the capture");
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs"));
            Check(source.Contains("if(worldMap==0 || row.PlaceName.RowId==20)worldMap=row.RowId;"),"the latest Hydaelyn");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("HeaderEntry($\"ZoneWorld{choice.MapId}\"") && view.Contains("##ZoneWorld{choice.MapId}"),"in both menus");
        }),
        ("Map labels (0.639): a dungeon's icon and a door like Ville fantôme open the map they name",() =>
        {
            Check(TenshoAssistant.Maps.MapLabels.OpensNamedMap(60414,0),"dungeon entrance");
            Check(TenshoAssistant.Maps.MapLabels.OpensNamedMap(60441,0),"Ville fantôme");
            Check(TenshoAssistant.Maps.MapLabels.OpensNamedMap(60467,0) && TenshoAssistant.Maps.MapLabels.OpensNamedMap(60442,0),"Locaux de la Société de l'Arcadion, Le Paddock (0.640)");
            Check(!TenshoAssistant.Maps.MapLabels.OpensNamedMap(0,0),"a plain place name");
            Check(!TenshoAssistant.Maps.MapLabels.OpensNamedMap(60414,3),"another kind of marker");
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs"));
            Check(source.Contains("row.DataType==1 ? row.DataKey.RowId:NamedMap("),"as a link, for the icon and its name");
            Check(source.Contains("if(label.Length==0 || !MapLabels.OpensNamedMap(icon,dataType))return 0;"),"services have no name: they stay as they are");
            Check(source.Contains("&& found.Range!=range ? found.Map:0;"),"not a marker named like its own map");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("if(hit.Fixed is { LinkMapId:not 0 } door)linkUnderMouse=door.LinkMapId;"),"under a quest icon too");
        }),
        ("Map labels (0.642): place names follow the map when zoomed out; the target stays when going up",() =>
        {
            Check(TenshoAssistant.Maps.MapLabels.PlaceNameSize(40,0.39f,1)<18.5f,"as on the game's map");
            Check(TenshoAssistant.Maps.MapLabels.PlaceNameSize(20,0.17f,1)<8,"zoomed out");
            Check(TenshoAssistant.Maps.MapLabels.PlaceNameSize(20,2f,1)==20,"zoomed in: the setting");
            Check(TenshoAssistant.Maps.MapLabels.PlaceNameSize(20,0.01f,1)==6,"still drawable");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("GameTargetAbove(map,markers,upperTarget.MapId,upperTarget.World)"),"on the region and the world");
            Check(view.Contains("new MapIconMarker(upper.Pixel.X,upper.Pixel.Y,upperTarget.Icon,\"\",upper.Link)"),"and a click goes down one step (0.643)");
        }),
        ("Map labels (0.645): an aetheryte is said not attuned only when the game's list is known",() =>
        {
            var costs=new Dictionary<uint,uint>{[8]=100};
            Check(TenshoAssistant.Maps.MapLabels.NotAttuned(9,costs),"missing from the list");
            Check(!TenshoAssistant.Maps.MapLabels.NotAttuned(8,costs),"in the list");
            Check(!TenshoAssistant.Maps.MapLabels.NotAttuned(0,costs),"not an aetheryte");
            Check(!TenshoAssistant.Maps.MapLabels.NotAttuned(9,null) && !TenshoAssistant.Maps.MapLabels.NotAttuned(9,new Dictionary<uint,uint>()),"unknown says nothing");
        }),
        ("Leve areas (0.654): remembered while the leve is accepted, replaced by a new read, gone with the leve",() =>
        {
            var memory=new TenshoAssistant.Maps.LeveAreaMemory<string>();
            var accepted=new HashSet<string>{"a"};
            Check(memory.Update([(697,"a","circle 1"),(697,"a","circle 2")],accepted).Count==0,"given by the read: nothing to add");
            Check(string.Join(",",memory.Update([],accepted))=="circle 1,circle 2","missing from the next read: remembered");
            Check(memory.Update([(697,"a","circle 2")],accepted).Count==0,"read again: replaced");
            Check(string.Join(",",memory.Update([],accepted))=="circle 2","only the new one stays");
            Check(memory.Update([(697,"b","other")],accepted).Count==1,"a leve not accepted is not remembered");
            Check(memory.Update([],new HashSet<string>()).Count==0,"abandoned or finished: forgotten");
            Check(memory.Update([],accepted).Count==0,"and it does not come back");
        }),
        ("Event markers (0.653): kept while a journal request empties them, on the same map only",() =>
        {
            Check(TenshoAssistant.Maps.MapEventHold.Holds(0,true,697,697),"the leve's click: 0 markers, type QuestLog, same map");
            Check(!TenshoAssistant.Maps.MapEventHold.Holds(12,true,697,697),"markers read: they win");
            Check(!TenshoAssistant.Maps.MapEventHold.Holds(0,false,697,697),"no request: really none");
            Check(!TenshoAssistant.Maps.MapEventHold.Holds(0,true,698,697),"another map: nothing kept");
            Check(!TenshoAssistant.Maps.MapEventHold.Holds(0,true,0,0),"no map yet");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("if(!MapEventHold.Holds(result.Count,journalRequest,agent->CurrentMapId,lastEventsMap)){lastEvents=result.AsReadOnly();"),"wired in the reader");
        }),
        ("Leve markers (P17): the icon read in game, or a tooltip naming a leve, never a quest",() =>
        {
            var names=new HashSet<string>{TenshoAssistant.Maps.MapLeves.Key("Grosse récolte : pierre septentrionale d'ornement")};
            Check(TenshoAssistant.Maps.MapLeves.Is(60492,0,"",null),"the icon alone");
            Check(TenshoAssistant.Maps.MapLeves.Is(60000,0,"Grosse récolte: pierre septentrionale d'ornement",names),"the tooltip writes it without the space");
            Check(TenshoAssistant.Maps.MapLeves.Is(60000,0,"\uE0BB Grosse récolte\u00A0: pierre septentrionale d'ornement",names),"a glyph and another space change nothing");
            Check(!TenshoAssistant.Maps.MapLeves.Is(60987,0,"Échange d'articles locaux",names),"another event");
            Check(!TenshoAssistant.Maps.MapLeves.Is(60492,3,"Grosse récolte: pierre septentrionale d'ornement",names),"a quest stays a quest");
            Check(!TenshoAssistant.Maps.MapLeves.Is(60000,0,"",names),"no text");
        }),
        ("Map labels: from the sheet, only for a drawn icon, with their setting",() =>
        {
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs"));
            Check(source.Contains("row.SubtextOrientation"),"the game's value");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Replace("\r\n","\n");
            Check(view.Contains("settings.ShowIconNames && settings.NamesShown(mapZoom) && item.Fixed is {Label.Length:>0} named && MapLabels.Shown(named.LinkMapId!=0,mapZoom)"),"on its setting and the zoom");
            Check(new TenshoAssistant.MapViewSettings().ShowIconNames,"on by default");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.596-map-label-orientation.md")),"the evidence");
        }),
    ];
}
