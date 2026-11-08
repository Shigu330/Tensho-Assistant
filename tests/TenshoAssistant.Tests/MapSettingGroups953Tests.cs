// X-76 step 3 (0.953): settings groups, batch B (Settings › Map). Each part is drawn after its parent's BeginDisabled and before the
// matching EndDisabled (greyed while the parent is off); the tips name that parent and keep their « Default » line.
internal static class MapSettingGroups953Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

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
        var at=text.IndexOf(guard,StringComparison.Ordinal);
        if(at<0)return -1;
        var end=text.IndexOf(row,at,StringComparison.Ordinal);
        if(end<0)return -1;
        var between=text[at..end];
        return Count(between,"ImGui.BeginDisabled(")-Count(between,"ImGui.EndDisabled();");
    }

    private static void Greyed(string text,string guard,string row,string message,bool indented=true)
    {
        Check(Count(text,row)==1,message+": drawn once");
        Check(Count(text,guard)==1,message+": one guard");
        Check(Depth(text,guard,row)>=1,message+": greyed under its parent");
        if(!indented)return;
        var at=text.IndexOf(guard,StringComparison.Ordinal);
        Check(text.LastIndexOf("ImGui.Indent();",at,StringComparison.Ordinal) is var indent && indent>=0 && at-indent<160,message+": indented under its parent");
    }

    private static void Tip(string section,string key,string english,string french,string message)
    {
        var en=Text("en",section,key);var fr=Text("fr",section,key);
        Check(en.Contains(english,StringComparison.Ordinal),message+": English tip names "+english);
        Check(fr.Contains(french,StringComparison.Ordinal),message+": French tip names "+french);
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
            Tip("MapView","ShowIconNamesHelp","« Game map icons »","« Icônes de la carte du jeu »","icon names");
            Tip("SettingTips","PlaceNameScale","icon names","noms des icônes","name size");
            Tip("MapView","NameZoomFollowHelp","Greyed while neither","Grisé tant que ni","name zoom follow");
            Tip("MapView","NameMinZoomHelp","Greyed while neither","Grisé tant que ni","name minimum zoom");
            Check(Text("en","SettingTips","PlaceNameScale").EndsWith("\nDefault: 100%.",StringComparison.Ordinal),"name size default kept");
        }),
        ("Map setting groups 953: quest tooltips under quests, hide collected under vistas and currents (15, 16)",() =>
        {
            var map=MapSettings();
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowQuests);\n                try{changed|="+Box("QuestTooltips"),Box("QuestTooltips"),"quest tooltips");
            Check(map.IndexOf(Box("ShowQuests"),StringComparison.Ordinal)<map.IndexOf(Box("QuestTooltips"),StringComparison.Ordinal),"right under quests");
            Check(Depth(map,"ImGui.BeginDisabled(!settings.ShowQuests);",Box("ShowFates"))==0,"FATEs not greyed");
            Greyed(map,"ImGui.BeginDisabled(!settings.ShowSightseeing && !settings.ShowAetherCurrents);",Box("HideCollected"),"hide collected");
            Tip("MapView","QuestTooltipsHelp","« Quests and objectives »","« Quêtes et objectifs »","quest tooltips");
            Tip("MapView","HideCollectedHelp","« Aether currents »","« Courants éthérés »","hide collected");
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
            Tip("MapView","GatheringLabelsHelp","« Gathering points »","« Points de récolte »","gathering labels");
            Tip("MapView","GatheringLabelMinZoomHelp","« Level next to gathering points »","« Niveau à côté des points de récolte »","label zoom");
            Tip("MapView","GatheringLabelScaleHelp","« Level next to gathering points »","« Niveau à côté des points de récolte »","label size");
        }),
        ("Map setting groups 953: each edge marker greyed while its map layer is off (18)",() =>
        {
            var map=MapSettings();
            const string edge="ImGui.BeginDisabled(!settings.ShowEdgeMarkers);";
            var layers=new (string Row,string Layer,string English,string French)[]
            {
                ("EdgePlayerOn","ShowPlayer","« My position »","« Ma position »"),
                ("EdgeFlagOn","ShowFlag","« Flag »","« Drapeau »"),
                ("EdgeQuestsOn","ShowQuests","« Quests and objectives »","« Quêtes et objectifs »"),
                ("EdgeLevesOn","ShowQuests","« Quests and objectives »","« Quêtes et objectifs »"),
                ("EdgeEventsOn","ShowQuests","« Quests and objectives »","« Quêtes et objectifs »"),
                ("EdgeFatesOn","ShowFates","« Running FATEs (current zone) »","« ALEA en cours (zone actuelle) »"),
                ("EdgeGatheringOn","ShowGathering","« Gathering points »","« Points de récolte »"),
                ("EdgePartyOn","ShowParty","« Party members »","« Membres du groupe »"),
            };
            var edges=map[map.IndexOf(edge,StringComparison.Ordinal)..];
            foreach(var (row,layer,english,french) in layers)
            {
                Check(Depth(map,edge,Box(row))==2,row+": greyed under the edge markers and its layer");
                Check(Depth(edges,"ImGui.BeginDisabled(!settings."+layer+");",Box(row))==1,row+": greyed while "+layer+" is off");
                Tip("MapView",row+"Help",english,french,row);
            }
            Check(Depth(map,edge,Box("EdgeFlagCoordinatesOn"))==3 && Depth(edges,"ImGui.BeginDisabled(!settings.EdgeFlag);",Box("EdgeFlagCoordinatesOn"))==1,"flag coordinates under the flag's edge marker");
            Greyed(edges,"ImGui.BeginDisabled(!settings.EdgeFlag);",Box("EdgeFlagCoordinatesOn"),"flag coordinates");
            Tip("MapView","EdgeFlagCoordinatesOnHelp","« Flag »","« Drapeau »","flag coordinates");
            Check(Depth(map,edge,Box("EdgeTargetOn"))==1,"journal target only under the edge markers");
        }),
        ("Map setting groups 953: overhead icon rows greyed while their monsters are not noted (19)",() =>
        {
            var map=MapSettings();
            var above=map[map.IndexOf("if(settings.MonsterIconsAbove)",StringComparison.Ordinal)..];
            var rows=new (string Row,string Guard,string English,string French)[]
            {
                ("AboveBlueSpell","!settings.ShowBlueMageMonsters","« Monsters for Blue Mage »","« Monstres pour le mage bleu »"),
                ("AboveList","!settings.ShowListMonsters && !configuration.ListPanelsNearbyMonsters","« Nearby monsters that drop it »","« Monstres proches qui le lâchent »"),
                ("AboveHuntLog","!settings.ShowHuntLogMonsters","« Monsters for your hunting log »","« Monstres de votre bestiaire »"),
                ("AboveFate","!settings.ShowFateMonsters","« Monsters of FATEs »","« Monstres des ALÉA »"),
                ("AboveCollection","!settings.ShowCollectionMonsters","« Monsters with a missing collectible »","« Monstres avec une pièce de collection manquante »"),
                ("AboveBeast","!settings.ShowBeastMonsters","« Monsters for the Beastmaster »","« Monstres pour le dresseur »"),
            };
            foreach(var (row,guard,english,french) in rows)
            {
                Greyed(above,"ImGui.BeginDisabled("+guard+");\n                    try{changed|="+Box(row),Box(row),row,indented:false);
                Check(Depth(above,"ImGui.BeginDisabled("+guard+");\n                    try{changed|="+Box(row),Box("MonsterIconsHideInCombat"))==0,row+": block closed");
                Tip("MapView",row+"Help",english,french,row);
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
            Tip("MapView","BlueMageMonstersAlwaysHelp","« Monsters for Blue Mage »","« Monstres pour le mage bleu »","blue mage always");
            Tip("MapView","BeastMonstersAlwaysHelp","« Monsters for the Beastmaster »","« Monstres pour le dresseur »","beast always");
            var beast=Read("MainWindow.Beastmaster.cs");
            Check(Depth(beast,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.AboveBeast\"")==1,"beast page: overhead row greyed");
            Check(Depth(beast,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.BeastMonstersAlways\"")==0,"beast page: always not greyed by it");
            var spells=Read("MainWindow.BlueSpellSources.cs");
            Check(Depth(spells,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.AboveBlueSpell\"")==1,"blue page: overhead row greyed");
            Check(Depth(spells,"ImGui.BeginDisabled(!configuration.MapView.MonsterIconsAbove);","BoolSetting(\"MapView.BlueMageMonstersAlways\"")==0,"blue page: always not greyed by it");
            Tip("BeastUi","AboveTip","« Icons above nearby monsters »","« Icônes au-dessus des monstres proches »","beast page tip");
            Tip("BlueSpellSources","AboveTip","« Icons above nearby monsters »","« Icônes au-dessus des monstres proches »","blue page tip");
        }),
    ];
}
