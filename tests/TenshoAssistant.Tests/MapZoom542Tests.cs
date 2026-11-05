using TenshoAssistant.Maps;

// 0.542 (P1): the map's zoom at opening.
internal static class MapZoom542Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map (0.586): a zone's flight comes from its own link to a set, so that the old zones have their wing",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("territory.AetherCurrentCompFlgSet.RowId") && reader.Contains("currents.TryAdd(territory.RowId,(linked,shared))"),"from TerritoryType");
        }),
        ("Map (0.569): each part that stops reading is named, and leaves the list when it reads again",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            foreach(var part in new[]{"flights","collected","events","objects","FATEs","party"})
            {
                Check(reader.Contains($"Read(\"{part}\");}}"),"cleared: "+part);
                Check(reader.Contains($"Failed(\"{part}\",error)"),"said: "+part);
                Check(Read("Localization/Languages/fr.json").Contains($"\"Part{part}\":") && Read("Localization/Languages/en.json").Contains($"\"Part{part}\":"),"named: "+part);
            }
            Check(Read("MainWindow.Diagnostics.cs").Contains("MapFailedParts()"),"Diagnostics too");
        }),
        ("Map (0.548): a FATE's bonus is written in its line; its area keeps its colour",() =>
        {
            Check(Read("Maps/MapLiveReader.Dalamud.cs").Contains("fate.State.ToString(),fate.HasBonus));") && Read("MainWindow.MapLocalLists.cs").Contains("MapLocal.FateBonus"),"read and written");
            Check(Read("MainWindow.MapView.cs").Contains("settings.FateArea"),"the area keeps the setting's colour (user request)");
        }),
        ("Map (0.547): a layers button with the same settings, and all at once",() =>
        {
            var view=Read("MainWindow.MapView.cs");
            Check(view.Contains("const int buttons=9;") && view.Contains("room-9*step-label"),"the column counts it");
            Check(view.Contains("ToolButton(FontAwesomeIcon.LayerGroup,\"MapView.Layers\"") && view.Contains("DrawMapLayers();"),"the button");
            foreach(var layer in new[]{"ShowQuests","ShowFates","ShowGathering","ShowVendors","ShowSightseeing","ShowAetherCurrents","ShowParty","ShowBookmarks","ShowHuntMarks","ShowTreasure","ShowFixedIcons","ShowPlaceNames"})
                Check(view[view.IndexOf("private void DrawMapLayers()")..view.IndexOf("// Mappy-like display settings")].Contains($"SettingBox(\"MapView.{layer}\""),layer);
        }),
        ("Map (0.546): the flag on a left click is a setting, off by default, on an empty spot, not after a drag",() =>
        {
            Check(!new TenshoAssistant.MapViewSettings().FlagOnClick,"off by default");
            var view=Read("MainWindow.MapView.cs");
            Check(view.Contains("if(settings.FlagOnClick && hovered && top is null && linkUnderMouse is null && hoveredBookmark is null") && view.Contains("map.TerritoryId!=0 && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)"),"empty spot, zone map, no drag");
        }),
        ("Map (0.545): the wheel keeps its old step until set, can be inverted, and a bad value does no harm",() =>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.WheelFactor(1)==1.15f && settings.WheelFactor(-1)==1/1.15f,"as before");
            settings.WheelInvert=true;
            Check(settings.WheelFactor(1)==1/1.15f && settings.WheelFactor(-1)==1.15f,"inverted");
            settings.WheelInvert=false;settings.WheelStep=99;
            Check(settings.WheelFactor(1)==1.5f,"bounded");
            settings.WheelStep=float.NaN;
            Check(settings.WheelFactor(1)==1.15f,"not finite");
            Check(Read("MainWindow.MapView.cs").Contains("ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows))ZoomTo(mapZoom*configuration.MapView.WheelFactor(io.MouseWheel));"),"over the whole window");
        }),
        ("Map (0.544): nearest aetheryte from the general and the icon menus, through the same confirmed question",() =>
        {
            var view=Read("MainWindow.MapView.cs");
            Check(view.Split("DrawNearestAetheryteEntry(map);").Length==3,"both menus");
            Check(view.Contains("AskNearestAetheryteAt(map,mapContextPixel,map.Zone);"),"the question of 0.373, no flag placed");
        }),
        ("Map labels (0.543): nothing changes until set; names and gathering labels have their own rules",() =>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.NameFactor(0.5f)==settings.ZoomFactor(0.5f) && settings.NamesShown(0.15f),"names as before");
            Check(!settings.GatheringLabelShown(0.59f) && settings.GatheringLabelShown(0.6f) && settings.GatheringLabelSize(10)==10,"gathering labels as before");
            settings.NameZoomFollow=0;settings.NameMinZoom=0.5f;settings.GatheringLabelMinZoom=0.2f;settings.GatheringLabelScale=1.5f;
            Check(settings.NameFactor(0.25f)==1f && !settings.NamesShown(0.4f) && settings.NamesShown(0.5f),"names on their own");
            Check(settings.GatheringLabelShown(0.2f) && settings.GatheringLabelSize(10)==15,"gathering labels on their own");
            settings.GatheringLabelScale=float.NaN;settings.NameMinZoom=float.NaN;
            Check(settings.GatheringLabelSize(10)==10 && settings.NamesShown(0.15f),"a bad saved value does no harm");
        }),
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
