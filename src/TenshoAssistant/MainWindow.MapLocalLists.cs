using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using TenshoAssistant.Maps;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// Right-click menu of the Tensho map: lists of what is on the shown map, in tabs (quests by category, FATEs of the current zone,
// gathering by type, aetherytes, NPC vendors, Triple Triad NPCs, sightseeing vistas, aether currents). A click places the game
// flag there (aetherytes: teleport); a right click opens the row's own menu (matching Tensho tool). Positions come from the cached
// index (MapLocalIndexLoader, DataCache « catalog-map-positions »); quests, gathering, vendors and Triad NPCs reuse their already
// cached catalogues.
internal sealed partial class MainWindow
{
    private bool mapLocalShowComplete;private bool mapTeleportRequested;private bool mapLocalCloseMenu;
    private string mapLocalQuestCategory="",mapLocalGatheringType="";
    // 0.328: the lists of the shown tab, rebuilt only when what they are made of changes (the menu stays open for seconds; quests and
    // gathering sources are walked in full, MapCatalog.Equivalent walks every map).
    private (object? Entries,object? Index,uint MapId,bool Complete,bool Locked,string Season,string Offered,IReadOnlyList<(string Category,IReadOnlyList<MapLocalQuest> Quests)> Groups) mapLocalQuestCache=(null,null,0,false,true,"","",[]);
    private (uint MapId,object? Catalog,object? Index,IReadOnlyList<(string Type,IReadOnlyList<MapLocalGatheringPoint> Points)> Groups) mapLocalGatheringCache=(0,null,null,[]);
    private (object? Catalog,uint MapId,IReadOnlySet<uint> Ids) mapLocalEquivalentCache=(null,0,new HashSet<uint>());
    private (object? Maps,object? Index,MapLocalKind Kind,MapLocalPosition[] Rows) mapLocalCollectibleCache=(null,null,default,[]);

    private void DrawMapLocalLists(MapDefinition map,MapLiveState? live)
    {
        // 0.400 (user report « framing »): wide enough for every tab name (8 since 0.401), at most 90 % of the screen; when the screen is
        // narrower the tabs scroll with arrows instead of being cut.
        var style=ImGui.GetStyle();
        var tabs=new[]{"MapLocal.Favorites","MapLocal.Quests","MapLocal.Fates","MapLocal.Gathering","MapLocal.Aetherytes","MapLocal.Vendors","MapLocal.TripleTriad","MapLocal.Currents","VistaLog.Tab"};
        var needed=tabs.Sum(key=>ImGui.CalcTextSize(localization.Get(key)).X+style.FramePadding.X*2+style.ItemInnerSpacing.X)+style.WindowPadding.X*2+8;
        var width=Math.Clamp(needed,640,Math.Max(640,ImGui.GetMainViewport().WorkSize.X*0.9f));
        var open=ImGui.BeginChild("MapLocalLists",new Vector2(width,360),true);
        try
        {
            if(!open || !ImGui.BeginTabBar("MapLocalTabs",ImGuiTabBarFlags.FittingPolicyScroll))return;
            try
            {
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Favorites"))){try{DrawLocalFavorites(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Quests"))){try{DrawLocalQuests(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Fates"))){try{DrawLocalFates(map,live);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Gathering"))){try{DrawLocalGathering(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Aetherytes"))){try{DrawLocalAetherytes(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Vendors"))){try{DrawLocalVendors(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.TripleTriad"))){try{DrawLocalTriad(map);}finally{ImGui.EndTabItem();}}
                if(ImGui.BeginTabItem(localization.Get("MapLocal.Currents"))){try{DrawLocalCollectibles(map,live,MapLocalKind.AetherCurrent);}finally{ImGui.EndTabItem();}}
                // 0.361 (g, C-11): the whole sightseeing log, every zone. 0.401 (user report): the zone-only « Sightseeing » tab is gone, this
                // log covers it.
                if(ImGui.BeginTabItem(localization.Get("VistaLog.Tab"))){try{DrawVistaLog(live);}finally{ImGui.EndTabItem();}}
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
        if(state?.Error is { } error)StateLine(UI.UiState.Failed,localization.Format("MapLocal.IndexError",error));
        else StateLine(UI.UiState.Waiting,localization.Format("MapLocal.IndexLoading",state?.Copied ?? 0));
        return null;
    }

    private void FlagAt(MapDefinition map,Vector3 world)
    {
        var coordinate=MapProjection.CoordinateFromWorld(world,map);
        PlaceFlagFromMapView(map,coordinate.X,coordinate.Y);
        ImGui.CloseCurrentPopup();
    }
    private static Vector2 CoordinateOf(MapDefinition map,Vector3 world)=>MapProjection.CoordinateFromWorld(world,map);
    private static string CoordinateText(MapDefinition map,Vector3 world)=>MapProjection.CoordinateText(CoordinateOf(map,world));

    private void DrawLocalQuests(MapDefinition map)
    {
        if(LocalIndexOrStatus() is not { } index)return;
        var state=quests.State;
        if(state.Entries.Count==0){ImGui.TextDisabled(localization.Get(state.Loading ? "MapLocal.QuestsLoading":"MapLocal.QuestsNone"));return;}
        ImGui.Checkbox(localization.Get("MapLocal.ShowComplete"),ref mapLocalShowComplete);Tip("MapLocal.ShowCompleteTip");
        // X-128 (0.1107, user capture): the box of the quests not available yet lives in this tab; it hides them from this list and from the map.
        var settings=configuration.MapView;
        ImGui.SameLine();
        if(SettingBox("MapView.ShowLockedQuests",settings.ShowLockedQuests,value=>settings.ShowLockedQuests=value))SaveMapSettings();
        var locked=settings.ShowLockedQuests;
        // X-355 (0.1344): the seasonal events running now (unknown: « ? », nothing taken for out of season).
        var festivals=locked ? null:Festivals?.Invoke();festivals?.Want();
        var running=festivals?.State?.Active.Select(festival=>festival.Id).Order().ToArray();
        var season=festivals?.Quests is null || running is null ? "?":string.Join(',',running);
        // X-355 (0.1345): the quests the game offers on this map (its own list, as the map draws it), when it covers the map.
        var offered=locked ? null:MapLocalLists.Offered(mapLive?.State?.Events?.Select(marker=>(marker.MapId,marker.ObjectiveId,marker.Offered)),map.MapId);
        var offeredKey=offered is null ? "?":string.Join(',',offered.Order());
        var cache=mapLocalQuestCache;
        if(!ReferenceEquals(cache.Entries,state.Entries) || !ReferenceEquals(cache.Index,index) || cache.MapId!=map.MapId || cache.Complete!=mapLocalShowComplete || cache.Locked!=locked || cache.Season!=season || cache.Offered!=offeredKey)
            mapLocalQuestCache=(state.Entries,index,map.MapId,mapLocalShowComplete,locked,season,offeredKey,
                MapLocalLists.Quests(state.Entries,index,map.MapId,mapLocalShowComplete,locked,MapLocalLists.OutOfSeason(festivals?.Quests,running),offered));
        var groups=mapLocalQuestCache.Groups;
        if(groups.Count==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        DrawSplitView("Quest",groups,ref mapLocalQuestCategory,item=>
        {
            var world=new Vector3(item.Position.X,0,item.Position.Z);
            var status=item.Quest.Progress switch{QuestProgress.Active=>localization.Get("MapLocal.Active"),QuestProgress.Complete=>localization.Get("MapLocal.Complete"),_=>""};
            DrawSmallGameIcon(item.Position.Data);
            if(ImGui.Selectable($"{localization.Format("MapLocal.Level",item.Quest.Level)}  {item.Quest.Name}  {status}##Quest{item.Quest.Id}"))FlagAt(map,world);
            QuestMenu($"QuestTool{item.Quest.Id}",item.Quest.Id,item.Quest.Name);
            if(ImGui.IsItemHovered())WrappedTooltip(CoordinateText(map,world)+"\n"+localization.Get("MapLocal.ClickFlag"));
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
            PlaceMenu($"FatePlace{fate.Id}",map,CoordinateOf(map,fate.Position),FateLabel(fate));
            if(ImGui.IsItemHovered())WrappedTooltip(CoordinateText(map,fate.Position)+"\n"+localization.Get("MapLocal.ClickFlag"));
        }
    }
    private string FateLabel(MapFateMarker fate)
    {
        var left=fate.SecondsLeft>0 ? localization.Format("MapLocal.FateTime",fate.SecondsLeft/60,(fate.SecondsLeft%60).ToString("00")):"";
        return localization.Format("MapLocal.FateLine",fate.Level,fate.Name,fate.Progress)+left+(fate.HasBonus ? localization.Get("MapLocal.FateBonus"):"");
    }

    private void DrawLocalGathering(MapDefinition map)
    {
        if(LocalIndexOrStatus() is not { } index)return;
        if(gatheringCatalog.State.Catalog is not { } catalog){StateLine(UI.UiState.Waiting,localization.Get("MapLocal.GatheringLoading"));return;}
        if(mapLocalGatheringCache.MapId!=map.MapId || !ReferenceEquals(mapLocalGatheringCache.Catalog,catalog) || !ReferenceEquals(mapLocalGatheringCache.Index,index))
            mapLocalGatheringCache=(map.MapId,catalog,index,MapLocalLists.Gathering(catalog.Sources,index,map.MapId));
        var groups=mapLocalGatheringCache.Groups;
        if(groups.Count==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        var items=itemCatalog.State.Catalog;
        DrawSplitView("Gathering",groups,ref mapLocalGatheringType,point=>
        {
            var label=$"{localization.Format("MapLocal.Level",point.Level)}  {point.Place}{(point.Hidden ? " "+localization.Get("MapLocal.Hidden"):"")}";
            DrawSmallGameIcon(point.Icon);
            if(point.Position is { } position)
            {
                var world=new Vector3(position.X,0,position.Z);
                if(ImGui.Selectable($"{label}##Gathering{point.BaseId}"))FlagAt(map,world);
                GatheringToolMenu($"GatheringTool{point.BaseId}",point.Items,(map,CoordinateOf(map,world)));
                if(ImGui.IsItemHovered())DrawGatheringTooltip(point);
            }
            else
            {
                ImGui.TextDisabled(label);
                GatheringToolMenu($"GatheringTool{point.BaseId}",point.Items);
                if(ImGui.IsItemHovered())WrappedTooltip(string.Join(", ",point.Items.Select(id=>items?.Find(id)?.Name ?? $"#{id}"))+"\n"+localization.Get("MapLocal.NoPosition"));
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

    // Sightseeing vistas and aether currents of the map with their state (read in game), number and coordinates, to compare
    // with the game's logs; a click places the flag.
    private void DrawLocalCollectibles(MapDefinition map,MapLiveState? live,MapLocalKind kind)
    {
        if(LocalIndexOrStatus() is not { } index)return;
        var mapIds=EquivalentMaps(map);
        if(!ReferenceEquals(mapLocalCollectibleCache.Maps,mapIds) || !ReferenceEquals(mapLocalCollectibleCache.Index,index) || mapLocalCollectibleCache.Kind!=kind)
            mapLocalCollectibleCache=(mapIds,index,kind,mapIds.SelectMany(id=>index.OnMap(kind,id)).OrderBy(row=>row.Id).ToArray());
        var rows=mapLocalCollectibleCache.Rows;
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        var collected=kind==MapLocalKind.Sightseeing ? live?.Sightseen:live?.UnlockedCurrents;
        var done=collected is null ? 0:rows.Count(row=>collected.Contains(row.Id));
        ImGui.TextDisabled(collected is null ? localization.Get("MapView.CollectedUnknown"):localization.Format("MapLocal.CollectedCount",done,rows.Length));
        // The log numbers beyond A Realm Reborn come from the wiki (loaded once, as on hovering a vista; off with the wiki switch).
        if(kind==MapLocalKind.Sightseeing)wikiVistas?.Request();
        foreach(var row in rows)
        {
            var world=new Vector3(row.X,0,row.Z);
            bool? state=collected is null ? null:collected.Contains(row.Id);
            var mark=state switch{true=>"[x]",false=>"[ ]",_=>"[?]"};
            var name=kind==MapLocalKind.Sightseeing
                ? (VistaNumber(row) is { Length:>0 } number ? number+"  "+row.Name:row.Name)
                : row.Data!=0 ? localization.Get("MapView.CurrentQuest")+(QuestOf(row.Data)?.Name is { Length:>0 } quest ? " · "+quest:""):localization.Get("MapView.CurrentField");
            ImGui.PushStyleColor(ImGuiCol.Text,state==true ? UI.FfxivTheme.TextMuted:UI.FfxivTheme.Text);
            try{if(ImGui.Selectable($"{mark}  {name}  ({CoordinateText(map,world)})##Collectible{kind}{row.Id}"))FlagAt(map,world);}
            finally{ImGui.PopStyleColor();}
            if(kind!=MapLocalKind.Sightseeing)PlaceMenu($"CollectiblePlace{kind}{row.Id}",map,CoordinateOf(map,world),name);
            // 0.328: the vista tooltip says « right click: create an alarm »; the row has that menu, as the vista on the Tensho map.
            if(kind==MapLocalKind.Sightseeing && map.TerritoryId!=0 && ImGui.BeginPopupContextItem($"VistaAlarm{row.Id}"))
            {
                try{DrawVistaAlarmEntry(row,map.TerritoryId);ImGui.Separator();PlaceEntries(map,CoordinateOf(map,world),"EntityMenu.Here");}
                finally{ImGui.EndPopup();}
            }
            if(ImGui.IsItemHovered())
            {
                if(kind==MapLocalKind.Sightseeing)DrawSightseeingTooltip(row,state);
                else WrappedTooltip(localization.Get("MapLocal.ClickFlag"));
            }
        }
    }
    // 0.328: the sightseeing log number, as in the vista tooltip: the wiki's when its row is matched (numbering restarts in each
    // expansion), else the sheet row order, which is the log number only for the 80 A Realm Reborn vistas (no number beyond).
    // X-76 P5 (0.929): the game's own numbering first (AdventureExPhase, Maps/VistaLogNumbers.cs), the wiki's only when the game data is not there.
    private string VistaNumber(MapLocalPosition vista)
    {
        if(VistaPhases?.Invoke() is { } phases && Maps.VistaLogNumbers.Of(vista.Id,phases) is { } game)
            return localization.Format(game.Expansion==0 ? "MapView.VistaNumberArr":"MapView.VistaNumberEx",game.Number,ExpansionName(game.Expansion));
        if(WikiVistaFor(vista) is { } wiki)return localization.Format(wiki.Section==0 ? "MapView.VistaNumberArr":"MapView.VistaNumberEx",wiki.Number,ExpansionName(wiki.Section));
        return "";
    }
    internal Func<IReadOnlyList<(uint Begin,uint End,int Expansion)>?>? VistaPhases { get; set; }
    // Maps showing the same ground as the shown map (MapCatalog.Equivalent walks every map), kept per catalogue and map.
    private IReadOnlySet<uint> EquivalentMaps(MapDefinition map)
    {
        var catalog=maps.State.Catalog;
        if(mapLocalEquivalentCache.Ids.Count==0 || mapLocalEquivalentCache.MapId!=map.MapId || !ReferenceEquals(mapLocalEquivalentCache.Catalog,catalog))
            mapLocalEquivalentCache=(catalog,map.MapId,(catalog?.Equivalent(map) ?? [map.MapId]).ToHashSet());
        return mapLocalEquivalentCache.Ids;
    }

    // NPC vendors of the map from the cached vendor catalogue, by name; click = flag, hover = shops.
    private void DrawLocalVendors(MapDefinition map)
    {
        if(npcVendors?.State.Catalog is null){StateLine(UI.UiState.Waiting,localization.Get("MapLocal.VendorsLoading"));return;}
        var rows=MapVendors(map);
        var vendorMarkers=VendorCandidates(VisibleFixed(map.MapId,mapMarkers?.For(map.MarkerRange) ?? []));
        if(rows.Count==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        ImGui.TextDisabled(localization.Format("MapLocal.VendorCount",rows.Count));
        foreach(var (vendor,spot) in rows.OrderBy(row=>row.Vendor.Name,StringComparer.CurrentCultureIgnoreCase))
        {
            var name=vendor.Name.Length>0 ? vendor.Name:localization.Get("MapLocal.UnnamedVendor");
            // Same icon as on the map: the game's icon the vendor stands by (MAP-017), else the icon of its kind.
            DrawSmallGameIcon(VendorMarker(vendorMarkers,MapProjection.PixelFromWorld(new Vector3(spot.WorldX,0,spot.WorldZ),map),VendorIconOf(vendor))?.Icon ?? VendorIconOf(vendor));
            if(ImGui.Selectable($"{name}  ({MapProjection.CoordinateText(new(spot.X,spot.Y))})##Vendor{vendor.NpcId}/{spot.X}/{spot.Y}")){PlaceFlagFromMapView(map,spot.X,spot.Y);ImGui.CloseCurrentPopup();}
            NpcMenu($"VendorTool{vendor.NpcId}/{spot.X}/{spot.Y}",name,vendor.NpcId,vendor.Locations);
            if(ImGui.IsItemHovered())WrappedTooltip(VendorLabel(vendor)+"\n"+localization.Get("MapLocal.ClickFlag")+"\n"+localization.Get("MapLocal.RightClickTool"));
        }
    }

    // Triple Triad NPCs of the map (0.244, R-03), from the cached Triad NPC catalogue: beaten state, missing reward cards;
    // click = flag, right click = Triple Triad tool. Not beaten first, as in the Triad tool.
    private void DrawLocalTriad(MapDefinition map)
    {
        var state=triadNpcs?.State;
        if(state?.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get(state?.Error is null ? "TriadNpcs.Loading":"TriadNpcs.Error"));return;}
        var mapIds=EquivalentMaps(map);
        var entries=TriadEntriesById();
        bool? Beaten(uint match)=>state.Beaten is { } beaten ? beaten.Contains(match):null;
        var questsDone=state.QuestsDone;
        var rows=catalog.ToFace(Beaten,id=>entries.TryGetValue(id,out var entry) && entry.ConfirmedUnlocked,false,npc=>npc.Available(questsDone)).Where(row=>mapIds.Contains(row.Npc.MapId)).ToArray();
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        if(state.Beaten is null)ImGui.TextDisabled(localization.Get(state.BeatenUnavailable ? "TriadNpcs.BeatenUnavailable":"TriadNpcs.BeatenUnknown"));
        foreach(var (npc,beaten,missing) in rows)
        {
            var mark=beaten switch{true=>"[x]",false=>"[ ]",_=>"[?]"};
            var locked=npc.Available(questsDone)==false;
            var lacking=(missing.Count>0 ? "  "+localization.Format("MapLocal.TriadMissing",missing.Count):"")+(locked ? "  "+localization.Get("TriadNpcs.QuestNeeded"):"");
            ImGui.PushStyleColor(ImGuiCol.Text,locked || (beaten==true && missing.Count==0) ? UI.FfxivTheme.TextMuted:UI.FfxivTheme.Text);
            try{if(ImGui.Selectable($"{mark}  {npc.Name}  ({MapProjection.CoordinateText(new(npc.X,npc.Y))}){lacking}##LocalTriad{npc.MatchId}")){PlaceFlagFromMapView(map,npc.X,npc.Y);ImGui.CloseCurrentPopup();}}
            finally{ImGui.PopStyleColor();}
            NpcMenu($"TriadTool{npc.MatchId}",npc.Name,0,[new(map.TerritoryId,map.MapId,map.Zone,npc.X,npc.Y,0,0,0)],("MapLocal.OpenTriadTool",()=>OpenTriadTab("Npcs")));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("TriadNpcs.Tooltip",npc.Fee,npc.RewardCards.Count==0 ? "—"
                :string.Join(", ",npc.RewardCards.Select(id=>entries.TryGetValue(id,out var entry) ? entry.Name:id.ToString())))
                +"\n"+localization.Get("TriadNpcs.Beaten")+" : "+localization.Get(beaten==true ? "TriadNpcs.Yes":beaten==false ? "TriadNpcs.No":"TriadNpcs.Unknown")
                +TriadQuestLines(npc,questsDone)+"\n"+localization.Get("MapLocal.ClickFlag")+"\n"+localization.Get("MapLocal.RightClickTool"));
        }
    }

    // 0.490 (user request, in-game campaign): favorites could only be removed from the Maps page. Those of this map first, then the others; a click goes there, « Remove » takes one away.
    private void DrawLocalFavorites(MapDefinition map)
    {
        var points=configuration.MapBookmarks ?? [];
        if(points.Count==0){ImGui.TextDisabled(localization.Get("MapLocal.FavoritesEmpty"));return;}
        var catalog=maps.State.Catalog;
        var remove=-1;
        void Rows(bool here)
        {
            for(var index=0;index<points.Count;index++)
            {
                var point=points[index];
                if(point is null || point.MapId==map.MapId!=here)continue;
                MapDefinition? target=null;
                var known=catalog is not null && MapBookmarks.TryResolve(point,catalog,out target);
                ImGui.PushID(index);
                try
                {
                    if(DeleteButton("MapsUi.Remove"))remove=index;
                    Tip("MapLocal.FavoriteRemoveTip");
                    ImGui.SameLine();
                    var name=(point.Group is { } group ? $"[{group}] ":"")+point.Name.Replace("##","# #");
                    var place=here || !known ? $"{MapProjection.CoordinateNumber(point.X)}, {MapProjection.CoordinateNumber(point.Y)}":$"{target!.Zone} · {MapProjection.CoordinateNumber(point.X)}, {MapProjection.CoordinateNumber(point.Y)}";
                    if(!known)ImGui.TextDisabled(localization.Format("MapLocal.FavoriteUnknown",name));
                    else if(ImGui.Selectable($"{name} ({place})###Favorite"))
                    {
                        if(here)FlagFromMap(map,point.X,point.Y);
                        else OpenMapPointFromPage(target!,point.X,point.Y);
                        ImGui.CloseCurrentPopup();
                    }
                    if(known)PlaceMenu("FavoritePlace",here ? map:target!,new(point.X,point.Y),name);
                    if(point.Note is { } note && ImGui.IsItemHovered())WrappedTooltip(note);
                }
                finally{ImGui.PopID();}
            }
        }
        if(points.Any(point=>point is not null && point.MapId==map.MapId))Rows(true);
        else ImGui.TextDisabled(localization.Get("MapLocal.FavoritesNoneHere"));
        if(points.Any(point=>point is not null && point.MapId!=map.MapId))
        {
            ImGui.Spacing();
            ImGui.TextColored(Accent,localization.Get("MapLocal.FavoritesElsewhere"));
            Rows(false);
        }
        if(remove>=0 && SaveBookmarks(items=>MapBookmarks.RemoveAt(items,remove)))selectedBookmark=Guid.Empty;
        if(bookmarkError is { } problem)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get(problem));
    }

    private void DrawLocalAetherytes(MapDefinition map)
    {
        var aetherytes=(mapMarkers?.For(map.MarkerRange) ?? []).Where(marker=>marker.AetheryteId!=0).ToArray();
        if(aetherytes.Length==0){ImGui.TextDisabled(localization.Get("MapLocal.Empty"));return;}
        foreach(var marker in aetherytes)
        {
            DrawSmallGameIcon(marker.Icon);
            if(ImGui.Selectable($"{marker.Shown}##Aetheryte{marker.AetheryteId}"))
            {mapTeleportNote=null;mapTeleportTarget=(marker.AetheryteId,marker.Shown);mapLive?.RefreshTeleportCosts();mapTeleportRequested=true;ImGui.CloseCurrentPopup();}
            Tip("MapView.TeleportHint");
        }
    }

    // Right click on a row (0.118, user request): open the matching tool on that entry instead of placing the flag.
    // Right click on a gathering point (0.218): every resource with its icon; the chosen one opens in the gathering tool.
    private void GatheringToolMenu(string id,IReadOnlyList<uint> resources,(MapDefinition Map,Vector2 At)? place=null)
    {
        if(!ImGui.BeginPopupContextItem(id))return;
        try
        {
            ImGui.TextDisabled(localization.Get("MapLocal.OpenGatheringTool"));
            foreach(var resource in resources)
            {
                var item=itemCatalog.State.Catalog?.Find(resource);
                DrawSmallGameIcon(item?.IconId ?? 0);
                if(ImGui.Selectable($"{item?.Name ?? $"#{resource}"}##{id}/{resource}")){itemId=(int)resource;page=Page.Gathering;IsOpen=true;mapLocalCloseMenu=true;}
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(resource);
            }
            if(place is { } spot){ImGui.Separator();PlaceEntries(spot.Map,spot.At,"EntityMenu.Here");}
        }
        finally{ImGui.EndPopup();}
    }
    private void DrawSmallGameIcon(uint icon)
    {
        var size=new Vector2(ImGui.GetTextLineHeight());
        DrawGameIcon(icon,size);
        ImGui.SameLine();
    }
}
