using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;
using TenshoAssistant.Npcs;

namespace TenshoAssistant;

// R-06 step 1 (0.263): one right-click menu per kind of thing — quest, NPC, monster, place — so a quest, an NPC… offers the same
// entries wherever it is listed: its name, the tool that shows it, then where it is (flag, Tensho map, zone weather).
// Each method attaches to the last drawn item (BeginPopupContextItem) and draws nothing when the menu is closed.
internal sealed partial class MainWindow
{
    private void QuestMenu(string id,uint questId,string name)
    {
        if(!ImGui.BeginPopupContextItem(id))return;
        try
        {
            MenuHeader(name);
            if(ImGui.Selectable(localization.Get("EntityMenu.OpenQuest"))){SelectQuest(questId);questShowDetails=true;page=Page.Quests;IsOpen=true;}
            // Where the quest starts (cached map position index).
            if(mapLocal?.State.Index?.Quest(questId) is { } start && maps.State.Catalog?.Find(start.MapId) is { } map)
                PlaceEntries(map,MapProjection.CoordinateFromPixel(MapProjection.PixelFromWorld(new Vector3(start.X,0,start.Z),map),map),"EntityMenu.QuestStart");
        }
        finally{ImGui.EndPopup();}
    }

    private void NpcMenu(string id,string name,uint vendorNpcId,IReadOnlyList<NpcLocation> locations,(string Key,Action Open)? tool=null)
    {
        if(!ImGui.BeginPopupContextItem(id))return;
        try
        {
            MenuHeader(name);
            if(tool is { } extra && ImGui.Selectable(localization.Get(extra.Key))){extra.Open();IsOpen=true;}
            if(vendorNpcId!=0 && ImGui.Selectable(localization.Get("EntityMenu.OpenVendor"))){npcVendorFocus=vendorNpcId;page=Page.NpcVendors;IsOpen=true;}
            if(locations.Count==0){ImGui.TextDisabled(localization.Get("NpcUi.NoLocation"));return;}
            for(var index=0;index<locations.Count;index++)
            {
                var location=locations[index];
                if(maps.State.Catalog?.Find(location.MapId) is not { } map)continue;
                ImGui.PushID(index);
                try{PlaceEntries(map,new Vector2(location.X,location.Y),locations.Count>1 ? null:"EntityMenu.Here",NpcPlace(location));}
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndPopup();}
    }

    private void MonsterMenu(string id,ulong monsterId,string name)
    {
        if(!ImGui.BeginPopupContextItem(id))return;
        try
        {
            MenuHeader(name);
            if(ImGui.Selectable(localization.Get("EntityMenu.OpenMonster"))){monsterLoot.SelectMonster(monsterId);selectedMonsterMap=0;page=Page.MonsterLoot;IsOpen=true;}
        }
        finally{ImGui.EndPopup();}
    }

    private void PlaceMenu(string id,MapDefinition map,Vector2 coordinate,string name)
    {
        if(!ImGui.BeginPopupContextItem(id))return;
        try{MenuHeader(name);PlaceEntries(map,coordinate,"EntityMenu.Here");}
        finally{ImGui.EndPopup();}
    }

    // 0.706 (user report): the Island Sanctuary has no aetheryte, nor have other maps: no entry that can only fail. Markers not read yet: the question says so itself.
    private bool MapHasAetheryte(MapDefinition map)=>map.TerritoryId!=0 && map.TerritoryId!=IslandResources.Territory
        && (mapMarkers is null || mapMarkers.For(map.MarkerRange).Any(marker=>marker.AetheryteId!=0));

    private void MenuHeader(string name)
    {
        ImGui.TextColored(Accent,name);
        ImGui.Separator();
    }

    // Flag there, show the map in Tensho, zone weather; with several places each group is titled by the place.
    private void PlaceEntries(MapDefinition map,Vector2 coordinate,string? titleKey,string? place=null)
    {
        // A place text already carries its coordinates (NpcPlace): they are not repeated.
        ImGui.TextDisabled(place ?? $"{(titleKey is null ? map.Zone:localization.Get(titleKey))}  (X {coordinate.X:0.0}  Y {coordinate.Y:0.0})");
        ImGui.BeginDisabled(map.TerritoryId==0);
        try{if(ImGui.Selectable(localization.Get("MapView.Flag")+"##EntityFlag"))PlaceFlagFromMapView(map,coordinate.X,coordinate.Y);}
        finally{ImGui.EndDisabled();}
        // 0.704 (user request): wherever coordinates are shown, the nearest aetheryte: the flag, the Tensho map on the point, the usual teleport question.
        if(MapHasAetheryte(map) && ImGui.Selectable(localization.Get("MapView.NearestAetheryte")+"##EntityAetheryte"))
            AskNearestAetheryteAt(map,MapProjection.PixelFromCoordinate(coordinate,map),place ?? map.Zone,flag:true,direct:configuration.GoThereTeleport);
        // 0.299: remove the flag from here too (when one is placed).
        if(mapLive?.State?.Flag is not null && ImGui.Selectable(localization.Get("MapView.RemoveFlag")+"##EntityRemoveFlag"))mapLive.RemoveFlag();
        if(ImGui.Selectable(localization.Get("EntityMenu.ShowMap")+"##EntityMap"))
        {ShowMapOnTenshoMap(map.MapId);mapCenterRequest=MapProjection.PixelFromCoordinate(coordinate,map);mapViewFollow=false;}
        if(map.TerritoryId!=0 && ImGui.Selectable(localization.Get("GatheringDetails.ZoneWeather")+"##EntityWeather")){OpenWeather(map.TerritoryId,map.Zone);IsOpen=true;}
    }
}
