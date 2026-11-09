// X-76 step 3 (0.953): settings groups, batch B (Settings › Map). Each part is drawn after its parent's BeginDisabled and before the
// matching EndDisabled (greyed while the parent is off); the tips name that parent and keep their « Default » line.
internal static class MapSettingGroups953Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Squash(string text)=>System.Text.RegularExpressions.Regex.Replace(text,@"\s+","");

    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    // The body of Settings › Map only (the map's quick layer menu draws some of the same boxes, without groups).
    private static string MapSettings()
    {
        var text=Read("MainWindow.MapView.cs");
        var start=text.IndexOf("private void DrawMapSettingsBody(",StringComparison.Ordinal);
        Check(start>=0,"settings body found");
        var end=text.IndexOf("\n    private ",start+10,StringComparison.Ordinal);
        return text[start..(end<0 ? text.Length:end)];
    }

    private static string Text(string language,string section,string key)
    {
        using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/"+language+".json")));
        return document.RootElement.GetProperty(section).GetProperty(key).GetString() ?? "";
    }

    private static int Count(string text,string part)=>text.Split(part).Length-1;

    // Open greyed blocks between the guard and the row: 1 or more = greyed by the guard; 0 = the block closed before the row; -1 = missing.
    private static int Depth(string text,string guard,string row)
    {
        text=Squash(text);guard=Squash(guard);row=Squash(row);
        var at=text.IndexOf(guard,StringComparison.Ordinal);
        if(at<0)return -1;
        var end=text.IndexOf(row,at,StringComparison.Ordinal);
        if(end<0)return -1;
        var between=text[at..end];
        return Count(between,"ImGui.BeginDisabled(")-Count(between,"ImGui.EndDisabled();");
    }

    private static void Greyed(string text,string guard,string row,string message,bool indented=true)
    {
        text=Squash(text);guard=Squash(guard);row=Squash(row);
        Check(Count(text,row)==1,message+": drawn once");
        Check(Count(text,guard)==1,message+": one guard");
        Check(Depth(text,guard,row)>=1,message+": greyed under its parent");
        if(!indented)return;
        var at=text.IndexOf(guard,StringComparison.Ordinal);
        Check(text.LastIndexOf("ImGui.Indent();",at,StringComparison.Ordinal) is var indent && indent>=0 && at-indent<160,message+": indented under its parent");
    }

    // label: "Section.Key" of the parent setting, whose current text the tip must name (null: the tip only has to exist).
    private static void Tip(string section,string key,string? label,string message)
    {
        var en=Text("en",section,key);var fr=Text("fr",section,key);
        Check(en.Length>0 && fr.Length>0,message+": tip exists");
        if(label is { } parent)
        {
            var (s,k)=(parent[..parent.IndexOf('.')],parent[(parent.IndexOf('.')+1)..]);
            Check(en.Contains(Text("en",s,k),StringComparison.Ordinal),message+": English tip names its parent "+parent);
            Check(fr.Contains(Text("fr",s,k),StringComparison.Ordinal),message+": French tip names its parent "+parent);
        }
        Check(en.Contains("\nDefault: ",StringComparison.Ordinal) && en.LastIndexOf('\n')==en.IndexOf("\nDefault: ",StringComparison.Ordinal),message+": English default kept last");
        Check(fr.Contains("\nPar défaut : ",StringComparison.Ordinal) && fr.LastIndexOf('\n')==fr.IndexOf("\nPar défaut : ",StringComparison.Ordinal),message+": French default kept last");
    }

    private static string Box(string key)=>"SettingBox(\"MapView."+key+"\"";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map setting groups 953: icon names under the game map icons, name sliders under the names (14)",() =>
        {
            var map=MapSettings();
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowFixedIcons);",Box("ShowIconNames"),"icon names",indented:false);
            const string names="ImGui.BeginDisabled(!settings.ShowPlaceNames && !(settings.ShowIconNames && settings.ShowFixedIcons));";
            foreach(var row in new[]{"localization.Get(\"MapView.PlaceNameScale\")","localization.Get(\"MapView.NameZoomFollow\")","localization.Get(\"MapView.NameMinZoom\")",
                "DefaultButton(\"PlaceNameScale\"","DefaultButton(\"NameZoomFollow\"","DefaultButton(\"NameMinZoom\""})
                Greyed(map,names,row,row);
            Check(map.IndexOf(Box("ShowPlaceNames"),StringComparison.Ordinal)<map.IndexOf(names,StringComparison.Ordinal)
                && map.IndexOf(Box("ShowIconNames"),StringComparison.Ordinal)<map.IndexOf(names,StringComparison.Ordinal),"sliders after both name boxes");
            Check(Depth(map,names,Box("FlagOnClick"))==0,"the flag rows stay outside");
            Tip("MapView","ShowIconNamesHelp","MapView.ShowFixedIcons","icon names");
            Tip("SettingTips","PlaceNameScale",null,"name size");
            Tip("MapView","NameZoomFollowHelp",null,"name zoom follow");
            Tip("MapView","NameMinZoomHelp",null,"name minimum zoom");
            Check(Text("en","SettingTips","PlaceNameScale").EndsWith("\nDefault: 100%.",StringComparison.Ordinal),"name size default kept");
        }),
        ("Map setting groups 953: quest tooltips under quests, hide collected under vistas and currents (15, 16)",() =>
        {
            var map=MapSettings();
            // 0.966: the block also holds « quests not available yet ».
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowQuests);\n                try\n                {\n                    changed|="+Box("QuestTooltips"),Box("QuestTooltips"),"quest tooltips");
            Check(Depth(map,"ImGui.BeginDisabled(!settings.ShowQuests);",Box("ShowLockedQuests"))>=1,"locked quests greyed under quests");
            Check(map.IndexOf(Box("ShowQuests"),StringComparison.Ordinal)<map.IndexOf(Box("QuestTooltips"),StringComparison.Ordinal),"right under quests");
            Check(Depth(map,"ImGui.BeginDisabled(!settings.ShowQuests);",Box("ShowFates"))==0,"FATEs not greyed");
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowSightseeing && !settings.ShowAetherCurrents);",Box("HideCollected"),"hide collected");
            Tip("MapView","QuestTooltipsHelp","MapView.ShowQuests","quest tooltips");
            Tip("MapView","HideCollectedHelp","MapView.ShowAetherCurrents","hide collected");
        }),
        ("Map setting groups 953: gathering labels next to the gathering points, their zoom and size under them (17)",() =>
        {
            var map=MapSettings();
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowGathering);\n                try\n                {\n                    changed|="+Box("GatheringLabels"),Box("GatheringLabels"),"gathering labels");
            Check(map.IndexOf("GathererBoxes(settings,true);",StringComparison.Ordinal) is var gatherers && gatherers>0
                && gatherers<map.IndexOf(Box("GatheringLabels"),StringComparison.Ordinal)
                && map.IndexOf(Box("GatheringLabels"),StringComparison.Ordinal)<map.IndexOf(Box("ShowHuntMarks"),StringComparison.Ordinal),"right after the gatherer boxes");
            foreach(var row in new[]{"localization.Get(\"MapView.GatheringLabelMinZoom\")","localization.Get(\"MapView.GatheringLabelScale\")","DefaultButton(\"GatheringLabelMinZoom\"","DefaultButton(\"GatheringLabelScale\""})
            {
                Greyed(map,"ImGui.BeginDisabled(!settings.GatheringLabels);",row,row);
                Check(Depth(map,"ImGui.BeginDisabled(!settings.ShowGathering);\n                try\n                {",row)==2,row+": also greyed while gathering points are off");
            }
            Check(Depth(map,"ImGui.BeginDisabled(!settings.ShowGathering);\n                try\n                {",Box("ShowHuntMarks"))==0,"hunt marks not greyed");
            Tip("MapView","GatheringLabelsHelp","MapView.ShowGathering","gathering labels");
            Tip("MapView","GatheringLabelMinZoomHelp","MapView.GatheringLabels","label zoom");
            Tip("MapView","GatheringLabelScaleHelp","MapView.GatheringLabels","label size");
        }),
        ("Map setting groups 953: each edge marker greyed while its map layer is off (18)",() =>
        {
            var map=MapSettings();
            const string edge="ImGui.BeginDisabled(!settings.ShowEdgeMarkers);";
            var layers=new (string Row,string Layer,string Label)[]
            {
                ("EdgePlayerOn","ShowPlayer","MapView.ShowPlayer"),
                ("EdgeFlagOn","ShowFlag","MapView.ShowFlag"),
                ("EdgeQuestsOn","ShowQuests","MapView.ShowQuests"),
                ("EdgeLevesOn","ShowQuests","MapView.ShowQuests"),
                ("EdgeEventsOn","ShowQuests","MapView.ShowQuests"),
                ("EdgeFatesOn","ShowFates","MapView.ShowFates"),
                ("EdgeGatheringOn","ShowGathering","MapView.ShowGathering"),
                ("EdgePartyOn","ShowParty","MapView.ShowParty"),
            };
            var edges=map[map.IndexOf(edge,StringComparison.Ordinal)..];
            foreach(var (row,layer,label) in layers)
            {
                Check(Depth(map,edge,Box(row))==2,row+": greyed under the edge markers and its layer");
                Check(Depth(edges,"ImGui.BeginDisabled(!settings."+layer+");",Box(row))==1,row+": greyed while "+layer+" is off");
                Tip("MapView",row+"Help",label,row);
            }
            Check(Depth(map,edge,Box("EdgeFlagCoordinatesOn"))==3 && Depth(edges,"ImGui.BeginDisabled(!settings.EdgeFlag);",Box("EdgeFlagCoordinatesOn"))==1,"flag coordinates under the flag's edge marker");
            Greyed(edges,"ImGui.BeginDisabled(!settings.EdgeFlag);",Box("EdgeFlagCoordinatesOn"),"flag coordinates");
            Tip("MapView","EdgeFlagCoordinatesOnHelp","MapView.EdgeFlagOn","flag coordinates");
            Check(Depth(map,edge,Box("EdgeTargetOn"))==1,"journal target only under the edge markers");
        }),
        ("Map setting groups 953: overhead icon rows greyed while their monsters are not noted (19)",() =>
        {
            var map=MapSettings();
            var above=map[map.IndexOf("if(settings.MonsterIconsAbove)",StringComparison.Ordinal)..];
            var rows=new (string Row,string Guard,string Label)[]
            {
                ("AboveBlueSpell","!settings.ShowBlueMageMonsters","MapView.ShowBlueMageMonsters"),
                ("AboveList","!settings.ShowListMonsters && !configuration.ListPanelsNearbyMonsters","ListPanels.NearbyMonsters"),
                ("AboveHuntLog","!settings.ShowHuntLogMonsters","MapView.ShowHuntLogMonsters"),
                ("AboveFate","!settings.ShowFateMonsters","MapView.ShowFateMonsters"),
                ("AboveCollection","!settings.ShowCollectionMonsters","MapView.ShowCollectionMonsters"),
                ("AboveBeast","!settings.ShowBeastMonsters","MapView.ShowBeastMonsters"),
            };
            foreach(var (row,guard,label) in rows)
            {
                Greyed(above,"ImGui.BeginDisabled("+guard+");\n                    try{changed|="+Box(row),Box(row),row,indented:false);
                Check(Depth(above,"ImGui.BeginDisabled("+guard+");\n                    try{changed|="+Box(row),Box("MonsterIconsHideInCombat"))==0,row+": block closed");
                Tip("MapView",row+"Help",label,row);
            }
            Check(Text("en","MapView","AboveListHelp").Contains("« Monsters for your lists »",StringComparison.Ordinal)
                && Text("fr","MapView","AboveListHelp").Contains("« Monstres de vos listes »",StringComparison.Ordinal),"list tip names both switches");
        }),
        ("Map setting groups 953: « even when not … » under its switch, overhead rows of the job pages under « Icons above » (20)",() =>
        {
            var map=MapSettings();
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowBlueMageMonsters);\n                try{changed|="+Box("BlueMageMonstersAlways"),Box("BlueMageMonstersAlways"),"blue mage always");
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowBeastMonsters);\n                try{changed|="+Box("BeastMonstersAlways"),Box("BeastMonstersAlways"),"beast always");
            Check(map.IndexOf(Box("ShowBlueMageMonsters"),StringComparison.Ordinal) is var blue && blue<map.IndexOf(Box("BlueMageMonstersAlways"),StringComparison.Ordinal)
                && map.IndexOf(Box("BlueMageMonstersAlways"),StringComparison.Ordinal)<map.IndexOf(Box("ShowCollectionMonsters"),StringComparison.Ordinal),"blue mage always right under its switch");
            Check(map.IndexOf(Box("ShowBeastMonsters"),StringComparison.Ordinal)<map.IndexOf(Box("BeastMonstersAlways"),StringComparison.Ordinal)
                && map.IndexOf(Box("BeastMonstersAlways"),StringComparison.Ordinal)<map.IndexOf(Box("MonsterIconsAbove"),StringComparison.Ordinal),"beast always right under its switch");
            Tip("MapView","BlueMageMonstersAlwaysHelp","MapView.ShowBlueMageMonsters","blue mage always");
            Tip("MapView","BeastMonstersAlwaysHelp","MapView.ShowBeastMonsters","beast always");
            var beast=Read("MainWindow.Beastmaster.cs");
            Check(Depth(beast,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.AboveBeast\"")==1,"beast page: overhead row greyed");
            Check(Depth(beast,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.BeastMonstersAlways\"")==0,"beast page: always not greyed by it");
            var spells=Read("MainWindow.BlueSpellSources.cs");
            Check(Depth(spells,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.AboveBlueSpell\"")==1,"blue page: overhead row greyed");
            Check(Depth(spells,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.BlueMageMonstersAlways\"")==0,"blue page: always not greyed by it");
            Tip("BeastUi","AboveTip","MapView.MonsterIconsAbove","beast page tip");
            Tip("BlueSpellSources","AboveTip","MapView.MonsterIconsAbove","blue page tip");
        }),
    ];
}
