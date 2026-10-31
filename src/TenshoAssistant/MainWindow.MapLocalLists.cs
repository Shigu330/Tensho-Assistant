using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using TenshoAssistant.Maps;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// Right-click menu of the Tensho map: lists of what is on the shown map, in tabs (quests by category, FATEs, gathering by
// type, aetherytes). A click places the game flag there (aetherytes: teleport). Positions come from the cached index
// (MapLocalIndexLoader, DataCache « catalog-map-positions »); quests and gathering reuse their already cached catalogues.
internal sealed partial class MainWindow
{
    private bool mapLocalShowComplete;private bool mapTeleportRequested;
    private string mapLocalQuestCategory="",mapLocalGatheringType="";

    private void DrawMapLocalLists(MapDefinition map,MapLiveState? live)
    {
        var open=ImGui.BeginChild("MapLocalLists",new Vector2(640,360),true);
        try
        {
            if(!open || !ImGui.BeginTabBar("MapLocalTabs"))return;
            try
            {
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Quests"))){try{DrawLocalQuests(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Fates"))){try{DrawLocalFates(map,live);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Gathering"))){try{DrawLocalGathering(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Aetherytes"))){try{DrawLocalAetherytes(map);}finally{ImGui.EndTabItem();}}
            }
            finally{ImGui.EndTabBar();}
        }
        finally{ImGui.EndChild();}
    }

    // Loading or failure of the shared position index, said rather than shown as an empty list.
    private MapLocalIndex? LocalIndexOrStatus()
    {
        var state=mapLocal?.State;
        if(state?.Index is { } index)return index;
        if(state?.Error is { } error)ImGui.TextColored(UI.FfxivTheme.Error,localization.Format("MapLocal.IndexError",error));
        else ImGui.TextDisabled(localization.Format("MapLocal.IndexLoading",state?.Copied ?? 0));
        return null;
    }

    private void FlagAt(MapDefinition map,Vector3 world)
    {
        var coordinate=MapProjection.CoordinateFromPixel(MapProjection.PixelFromWorld(world,map),map);
        PlaceFlagFromMapView(map,coordinate.X,coordinate.Y);
        ImGui.CloseCurrentPopup();
    }
    private string CoordinateText(MapDefinition map,Vector3 world)
    {
        var coordinate=MapProjection.CoordinateFromPixel(MapProjection.PixelFromWorld(world,map),map);
        return $"X {coordinate.X:0.0}  Y {coordinate.Y:0.0}";
    }

    private void DrawLocalQuests(MapDefinition map)
    {
        if(LocalIndexOrStatus() is not { } index)return;
        var state=quests.State;
        if(state.Entries.Count==0){ImGui.TextDisabled(localization.Get(state.Loading ? "MapLocal.QuestsLoading":"MapLocal.QuestsNone"));return;}
        ImGui.Checkbox(localization.Get("MapLocal.ShowComplete"),ref mapLocalShowComplete);
        var groups=MapLocalLists.Quests(state.Entries,index,map.MapId,mapLocalShowComplete);
        if(groups.Count==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        DrawSplitView("Quest",groups,ref mapLocalQuestCategory,item=>
        {
            var world=new Vector3(item.Position.X,0,item.Position.Z);
            var status=item.Quest.Progress switch{QuestProgress.Active=>localization.Get("MapLocal.Active"),QuestProgress.Complete=>localization.Get("MapLocal.Complete"),_=>""};
            if(ImGui.Selectable($"{localization.Format("MapLocal.Level",item.Quest.Level)}  {item.Quest.Name}  {status}##Quest{item.Quest.Id}"))FlagAt(map,world);
            if(ImGui.IsItemHovered())ImGui.SetTooltip(CoordinateText(map,world)+"\n"+localization.Get("MapLocal.ClickFlag"));
        });
    }

    private void DrawLocalFates(MapDefinition map,MapLiveState? live)
    {
        // The game only lists the FATEs of the current zone; no static FATE position exists in the game data.
        if(live is null || live.MapId!=map.MapId){ImGui.TextDisabled(localization.Get("MapLocal.FatesOtherZone"));return;}
        if(live.Fates is not { Count:>0 } fates){ImGui.TextDisabled(localization.Get("MapLocal.FatesNone"));return;}
        foreach(var fate in fates.OrderBy(fate=>fate.Level).ThenBy(fate=>fate.Name,StringComparer.CurrentCultureIgnoreCase))
        {
            DrawSmallGameIcon(fate.Icon);
            if(ImGui.Selectable($"{FateLabel(fate)}##Fate{fate.Id}"))FlagAt(map,fate.Position);
            if(ImGui.IsItemHovered())ImGui.SetTooltip(CoordinateText(map,fate.Position)+"\n"+localization.Get("MapLocal.ClickFlag"));
        }
    }
    private string FateLabel(MapFateMarker fate)
    {
        var left=fate.SecondsLeft>0 ? localization.Format("MapLocal.FateTime",fate.SecondsLeft/60,(fate.SecondsLeft%60).ToString("00")):"";
        return localization.Format("MapLocal.FateLine",fate.Level,fate.Name,fate.Progress)+left;
    }

    private void DrawLocalGathering(MapDefinition map)
    {
        if(LocalIndexOrStatus() is not { } index)return;
        if(gatheringCatalog.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("MapLocal.GatheringLoading"));return;}
        var groups=MapLocalLists.Gathering(catalog.Sources,index,map.MapId);
        if(groups.Count==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        var items=itemCatalog.State.Catalog;
        DrawSplitView("Gathering",groups,ref mapLocalGatheringType,point=>
        {
            var names=string.Join(", ",point.Items.Select(id=>items?.Find(id)?.Name ?? $"#{id}"));
            var label=$"{localization.Format("MapLocal.Level",point.Level)}  {point.Place}{(point.Hidden ? " "+localization.Get("MapLocal.Hidden"):"")}";
            if(point.Position is { } position)
            {
                var world=new Vector3(position.X,0,position.Z);
                if(ImGui.Selectable($"{label}##Gathering{point.BaseId}"))FlagAt(map,world);
                if(ImGui.IsItemHovered())ImGui.SetTooltip(names+"\n"+CoordinateText(map,world)+"\n"+localization.Get("MapLocal.ClickFlag"));
            }
            else
            {
                ImGui.TextDisabled(label);
                if(ImGui.IsItemHovered())ImGui.SetTooltip(names+"\n"+localization.Get("MapLocal.NoPosition"));
            }
        });
    }

    // Double view (0.117, user request): categories on the left with their counts, the chosen category's rows on the right.
    // The choice is kept by name; a category missing from this map falls back to the first one.
    private static void DrawSplitView<T>(string id,IReadOnlyList<(string Key,IReadOnlyList<T> Rows)> groups,ref string selected,Action<T> drawRow)
    {
        var chosen=selected;
        var current=groups.FirstOrDefault(group=>group.Key==chosen);
        if(current.Rows is null)current=groups[0];
        if(ImGui.BeginChild(id+"Keys",new Vector2(190,0),true))
        {
            try{foreach(var (key,rows) in groups)if(ImGui.Selectable($"{key} ({rows.Count})##{id}{key}",key==current.Key))current=(key,rows);}
            finally{ImGui.EndChild();}
        }
        else ImGui.EndChild();
        selected=current.Key;
        ImGui.SameLine();
        if(ImGui.BeginChild(id+"Rows",Vector2.Zero,true))
        {
            try{foreach(var row in current.Rows)drawRow(row);}
            finally{ImGui.EndChild();}
        }
        else ImGui.EndChild();
    }

    private void DrawLocalAetherytes(MapDefinition map)
    {
        var aetherytes=(mapMarkers?.For(map.MarkerRange) ?? []).Where(marker=>marker.AetheryteId!=0).ToArray();
        if(aetherytes.Length==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        foreach(var marker in aetherytes)
        {
            DrawSmallGameIcon(marker.Icon);
            if(ImGui.Selectable($"{marker.Label}##Aetheryte{marker.AetheryteId}"))
            {mapTeleportTarget=(marker.AetheryteId,marker.Label);mapLive?.RefreshTeleportCosts();mapTeleportRequested=true;ImGui.CloseCurrentPopup();}
            if(ImGui.IsItemHovered())ImGui.SetTooltip(localization.Get("MapView.TeleportHint"));
        }
    }

    private void DrawSmallGameIcon(uint icon)
    {
        var size=new Vector2(ImGui.GetTextLineHeight());
        if(icon!=0 && textures.TryGetFromGameIcon(new GameIconLookup(icon),out var shared) && shared.TryGetWrap(out var image,out _) && image is not null)ImGui.Image(image.Handle,size);
        else ImGui.Dummy(size);
        ImGui.SameLine();
    }
}
