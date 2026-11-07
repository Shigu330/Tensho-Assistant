using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// g, M-03 (0.373): map links seen in the chat (ChatMapLinkWatcher, this session only), on the Maps page: filter by channel, a click
// places the flag (as the page's own « open » does), another opens the Tensho map on the point and asks for the nearest aetheryte's
// teleport (the usual question with its cost: never automatic).
internal sealed partial class MainWindow
{
    internal ChatMapLinkHistory? ChatLinkHistory { get; set; }
    // 0.384 (g, M-14): the deciphered treasure map held (TreasureMapReader), for the Tensho map and the Reminders page.
    internal Func<TreasureMapSpot?>? TreasureMap { get; set; }
    // Channels kept by the filter (null = every channel); session only.
    private HashSet<ChatLinkChannel>? chatLinkChannels;
    // Nearest aetheryte asked from a link: resolved by the Tensho map once it shows that map and the teleport list is read (3 s at most).
    // Direct (0.399, user request): teleport at once instead of asking, when the aetheryte's cost is known (« Go there » setting).
    private (uint MapId,Vector2 Pixel,string Place,DateTimeOffset Until,bool Direct)? mapNearestRequest;

    private void DrawChatMapLinks(MapCatalog catalog)
    {
        if(ChatLinkHistory is not { } history)return;
        var all=history.Links;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("ChatLinks.Title",all.Count)+"###ChatMapLinks"))return;
        if(!configuration.MapView.ChatLinks){ImGui.TextDisabled(localization.Get("ChatLinks.Off"));return;}
        ImGui.TextDisabled(localization.Get("ChatLinks.Help"));
        ImGui.SetNextItemWidth(200);
        var preview=chatLinkChannels is null ? localization.Get("ChatLinks.AllChannels")
            :string.Join(", ",chatLinkChannels.Order().Select(ChannelName));
        if(ImGui.BeginCombo("##ChatLinkChannels",preview.Length==0 ? localization.Get("ChatLinks.NoChannel"):preview))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("ChatLinks.AllChannels"),chatLinkChannels is null))chatLinkChannels=null;
                foreach(var channel in Enum.GetValues<ChatLinkChannel>())
                {
                    var kept=chatLinkChannels?.Contains(channel) ?? true;
                    if(!ImGui.Selectable(ChannelName(channel)+"##Channel"+channel,kept,ImGuiSelectableFlags.DontClosePopups))continue;
                    var set=chatLinkChannels ?? [..Enum.GetValues<ChatLinkChannel>()];
                    if(!set.Remove(channel))set.Add(channel);
                    chatLinkChannels=set.Count==Enum.GetValues<ChatLinkChannel>().Length ? null:set;
                }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("ChatLinks.ChannelTip");
        ImGui.SameLine();
        ImGui.BeginDisabled(all.Count==0);
        try{if(ImGui.SmallButton(localization.Get("ChatLinks.Clear")))history.Clear();}
        finally{ImGui.EndDisabled();}
        Tip("ChatLinks.ClearTip");
        var links=ChatMapLinkHistory.Filter(all,chatLinkChannels);
        if(links.Count==0){ImGui.TextDisabled(localization.Get("ChatLinks.Empty"));return;}
        var visible=ImGui.BeginChild("ChatMapLinkList",new Vector2(0,Math.Min(180,links.Count*ImGui.GetFrameHeightWithSpacing()+8)),true);
        try
        {
            if(!visible)return;
            var clipper=ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(links.Count);
                while(clipper.Step())
                for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                {
                    var link=links[index];
                    var map=catalog.Find(link.MapId);
                    ImGui.PushID(index);
                    try
                    {
                        ImGui.BeginDisabled(map is null || !MapCatalog.CoordinatesValid(link.X,link.Y));
                        try
                        {
                            if(ImGui.SmallButton(localization.Get("ChatLinks.Flag")) && map is not null)OpenMapPointFromPage(map,link.X,link.Y);
                            Tip("ChatLinks.FlagTip");
                            ImGui.SameLine();
                            if(ImGui.SmallButton(localization.Get("ChatLinks.Aetheryte")) && map is not null)AskNearestAetheryte(map,link);
                            Tip("ChatLinks.AetheryteTip");
                        }
                        finally{ImGui.EndDisabled();}
                        ImGui.SameLine();
                        var place=map is null ? link.Place:string.IsNullOrWhiteSpace(map.Subzone) ? map.Zone:$"{map.Zone} · {map.Subzone}";
                        ImGui.TextUnformatted($"{link.At.ToLocalTime():t} · {place} ({link.X:0.0}, {link.Y:0.0})");
                        if(map is not null && MapCatalog.CoordinatesValid(link.X,link.Y))PlaceMenu($"ChatLinkPlace{link.At.Ticks}",map,new(link.X,link.Y),place);
                        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("ChatLinks.Line",ChannelName(link.Channel),
                            link.Sender.Length>0 ? link.Sender:localization.Get("ChatLinks.NoSender")));
                    }
                    finally{ImGui.PopID();}
                }
            }
            finally{clipper.End();clipper.Destroy();}
        }
        finally{ImGui.EndChild();}
    }

    private string ChannelName(ChatLinkChannel channel)=>localization.Get("ChatLinks.Channel"+channel);

    private void AskNearestAetheryte(MapDefinition map,ChatMapLink link)=>AskNearestAetheryteAt(map,MapProjection.PixelFromCoordinate(new Vector2(link.X,link.Y),map),link.Place);

    // 0.397 (g, R-03): the same for any point of a map (texture pixel): the Tensho map opens on it and the nearest aetheryte's teleport
    // question follows; with flag, the game flag is placed on the point first; with direct (0.399), the teleport starts without the question.
    private void AskNearestAetheryteAt(MapDefinition map,Vector2 pixel,string place,bool flag=false,bool direct=false)
    {
        if(flag){var coordinate=MapProjection.CoordinateFromPixel(pixel,map);FlagFromMap(map,coordinate.X,coordinate.Y);}
        OpenMapView(map.MapId);mapFitRequested=false;mapZoom=MapProjection.ClampZoom(Math.Max(mapZoom,MapFocusZoom));mapCenterRequest=pixel;
        mapLive?.RefreshTeleportCosts();
        mapNearestRequest=(map.MapId,pixel,place,DateTimeOffset.UtcNow.AddSeconds(3),direct);
    }

    // Called by the Tensho map canvas: once the asked map is shown and the teleport list read (or 3 s passed), opens the teleport question.
    /// <summary>0.833 (X-63): the neighbouring maps from the target map's link markers, each with its ways back into the target and its aetherytes.</summary>
    private ZoneEntry? ZoneEntryFor(MapDefinition map,Vector2 point,IReadOnlyDictionary<uint,uint>? costs)
    {
        if(mapMarkers is not { } source || maps.State.Catalog is not { } catalog)return null;
        var links=new List<ZoneEntryLink>();
        foreach(var neighbourId in source.For(map.MarkerRange).Where(marker=>marker.LinkMapId!=0 && marker.LinkMapId!=map.MapId).Select(marker=>marker.LinkMapId).Distinct())
        {
            if(catalog.Find(neighbourId) is not { TerritoryId:not 0 } neighbour)continue;
            var on=source.For(neighbour.MarkerRange);
            links.Add(new(neighbour,on.Where(marker=>marker.LinkMapId==map.MapId).Select(marker=>new Vector2(marker.X,marker.Y)).ToArray(),
                on.Where(marker=>marker.AetheryteId!=0).Select(marker=>(marker.AetheryteId,new Vector2(marker.X,marker.Y))).ToArray()));
        }
        var entry=ZoneEntries.Plan(map,point,source.Aetherytes,links,costs,MapProjection.PixelFromWorld);
        if(developerMode)log.Information("Zone entry for map {0}: {1} neighbours, {2}.",map.MapId,links.Count,entry is null ? "none":$"aetheryte {entry.Aetheryte} shard {entry.Shard} via map {entry.ViaMapId}{(entry.NoAethernet ? " no aethernet":"")}");
        return entry;
    }

    private void ResolveNearestAetheryte(MapDefinition map)
    {
        if(mapNearestRequest is not { } request || request.MapId!=map.MapId)return;
        var now=DateTimeOffset.UtcNow;var failed=mapLive?.TeleportCostsFailed==true;var costs=mapLive?.TeleportCosts;
        if(costs is null && !failed && now<request.Until)return;
        mapNearestRequest=null;
        var markers=(mapMarkers?.For(map.MarkerRange) ?? []).Where(marker=>marker.AetheryteId!=0).ToList();
        // 0.833 (X-63): a zone with no aetheryte: the aetheryte that leads there, and the aethernet step, said before the teleport.
        if(markers.Count==0 && ZoneEntryFor(map,request.Pixel,failed ? null:costs) is { } entry)
        {
            var rows=mapMarkers?.Aetherytes ?? [];
            string Name(uint id)=>rows.FirstOrDefault(row=>row.Id==id)?.Name is { Length:>0 } name ? name:id.ToString();
            var via=maps.State.Catalog?.Find(entry.ViaMapId)?.Zone ?? "";
            var note=entry.NoAethernet ? localization.Format("MapView.TeleportNoAethernet",Name(entry.Aetheryte),map.Zone)
                :entry.Shard is { } shard ? localization.Format("MapView.TeleportThenAethernet",Name(shard)):localization.Format("MapView.TeleportWalk",map.Zone);
            mapTeleportNote=(entry.Aetheryte,localization.Format("MapView.TeleportVia",map.Zone,via.Length>0 ? via:Name(entry.Aetheryte))+" "+note,entry.NoAethernet);
            mapTeleportTarget=(entry.Aetheryte,Name(entry.Aetheryte));mapTeleportRequested=true;
            return;
        }
        if(ChatMapLinks.NearestAetheryte(markers.Select(marker=>(marker.AetheryteId,marker.X,marker.Y)),request.Pixel,failed ? null:costs) is not { } found)
        {
            mapOpenError=localization.Get(markers.Count==0 ? "ChatLinks.NoAetheryte":"ChatLinks.NoAttuned");mapErrorUntil=now.AddSeconds(5);
            return;
        }
        var label=markers.First(marker=>marker.AetheryteId==found).Label;
        // 0.399 (user request): « Go there » with « teleport at once » — straight to the aetheryte when the game's teleport list has its
        // cost (attuned); otherwise the usual question below.
        if(request.Direct && !failed && costs?.ContainsKey(found)==true){mapLive?.Teleport(found);return;}
        mapTeleportTarget=(found,label.Length>0 ? label:request.Place);mapTeleportRequested=true;
    }
}
