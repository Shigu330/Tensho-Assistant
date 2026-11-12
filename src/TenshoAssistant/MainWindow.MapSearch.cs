using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// P10 (0.597): Maps page › search: places, aetherytes, vendors, Triple Triad NPCs, quest givers and gathering resources of every map. The list
// is built once off the frame, again when a catalogue arrives or changes; a click opens the Tensho map centred on the result, without a flag.
// Quest givers are those of the character's journal list; the others come from the game's data.
internal sealed partial class MainWindow
{
    private string mapFindQuery="";
    private string mapFindShownQuery="";
    private object?[]? mapFindSources;
    private Task<MapSearchHit[]>? mapFindTask;
    private MapSearchHit[]? mapFindHits;
    private (MapSearchHit[] Shown,int Total) mapFindResult=([],0);
    private CancellationTokenSource? mapFindCancel;

    private object?[] MapFindSources()=>[maps.State.Catalog,mapLocal?.State.Index,npcVendors?.State.Catalog,triadNpcs?.State.Catalog,quests.State.Entries,gatheringCatalog.State.Catalog,itemCatalog.State.Catalog,mapMarkers];

    private void StartMapFind(object?[] sources)
    {
        mapFindSources=sources;
        mapFindCancel?.Cancel();mapFindCancel?.Dispose();
        var cancel=mapFindCancel=new CancellationTokenSource();var token=cancel.Token;
        var catalog=(MapCatalog?)sources[0];var index=mapLocal?.State.Index;var vendors=npcVendors?.State.Catalog;var triad=triadNpcs?.State.Catalog;
        var entries=quests.State.Entries;var gathering=gatheringCatalog.State.Catalog;var items=itemCatalog.State.Catalog;var markers=mapMarkers;
        mapFindTask=Task.Run(()=>
        {
            try
            {
                var hits=new List<MapSearchHit>();
                if(catalog is null)return [];
                // A range is shared by a map and its copies: the first one shown in the list.
                foreach(var group in catalog.Entries.Where(map=>map.MarkerRange!=0).GroupBy(map=>map.MarkerRange))
                {
                    token.ThrowIfCancellationRequested();
                    var map=group.First();
                    foreach(var marker in markers?.Read(group.Key) ?? [])
                        hits.Add(new(marker.AetheryteId!=0 ? MapSearchKind.Aetheryte:MapSearchKind.Place,marker.Shown,map.Zone,map.MapId,new(marker.X,marker.Y)));
                }
                foreach(var vendor in vendors?.Vendors ?? [])
                foreach(var spot in vendor.Locations)
                    if(catalog.Find(spot.MapId) is { } map)hits.Add(new(MapSearchKind.Vendor,vendor.Name,map.Zone,map.MapId,MapProjection.PixelFromWorld(new Vector3(spot.WorldX,0,spot.WorldZ),map)));
                foreach(var npc in triad?.Npcs ?? [])
                    if(catalog.Find(npc.MapId) is { } map)hits.Add(new(MapSearchKind.Triad,npc.Name,map.Zone,map.MapId,MapProjection.PixelFromCoordinate(new Vector2(npc.X,npc.Y),map)));
                if(index is not null)
                {
                    foreach(var quest in entries)
                        if(index.Quest(quest.Id) is { } giver && catalog.Find(giver.MapId) is { } map)
                            hits.Add(new(MapSearchKind.Quest,quest.Name,map.Zone,map.MapId,MapProjection.PixelFromWorld(new Vector3(giver.X,0,giver.Z),map)));
                    foreach(var source in gathering?.Sources ?? [])
                    {
                        token.ThrowIfCancellationRequested();
                        if(catalog.Find(source.MapId) is not { } map || items?.Find(source.ItemId)?.Name is not { } name)continue;
                        // Same positions as the gathering page's « Go to » (MainWindow.GatheringGoTo.cs).
                        Vector2? pixel=source.IsFishing ? index.Fishing(source.PointId) is { } spot ? new Vector2(spot.X,spot.Z):null
                            :index.Gathering(source.BaseId) is { } point ? MapProjection.PixelFromWorld(new Vector3(point.X,0,point.Z),map):null;
                        if(pixel is { } at)hits.Add(new(MapSearchKind.Resource,name,map.Zone,map.MapId,at));
                    }
                }
                return MapSearch.Distinct(hits);
            }
            catch(OperationCanceledException){return [];}
            catch(Exception error){log.Warning(error,"The map search could not be prepared.");return [];}
        },token);
    }

    private void DrawMapFind(MapCatalog catalog)
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("###MapFind",localization.Get("MapsUi.FindHint"),ref mapFindQuery,80);
        Tip("MapsUi.FindTip");
        var words=mapFindQuery.Trim();
        if(words.Length<MapSearch.MinimumLetters)return;
        var sources=MapFindSources();
        if(mapFindSources is null || !mapFindSources.Zip(sources).All(pair=>ReferenceEquals(pair.First,pair.Second)))StartMapFind(sources);
        if(mapFindTask is {IsCompleted:true} task && !ReferenceEquals(mapFindHits,task.Result))
        {mapFindHits=task.Result;mapFindShownQuery="\0";}
        if(mapFindHits is null){ImGui.TextDisabled(localization.Get("MapsUi.FindLoading"));return;}
        if(mapFindShownQuery!=mapFindQuery){mapFindResult=MapSearch.Find(mapFindHits,mapFindQuery);mapFindShownQuery=mapFindQuery;}
        var (shown,total)=mapFindResult;
        ImGui.TextDisabled(total>shown.Length ? localization.Format("MapsUi.FindMore",shown.Length,total):localization.Format("MapsUi.FindCount",total));
        if(shown.Length==0)return;
        DrawChainList("MapFindList",shown.Length,index=>
        {
            var hit=shown[index];
            ImGui.PushID(index);
            try
            {
                if(ImGui.Selectable(hit.Name+"###Hit"))
                {
                    OpenMapView(hit.MapId);
                    mapFitRequested=false;mapZoom=MapProjection.ClampZoom(Math.Max(mapZoom,MapFocusZoom));mapCenterRequest=hit.Pixel;
                }
                if(maps.State.Catalog?.Find(hit.MapId) is { } hitMap)PlaceMenu("HitPlace",hitMap,MapProjection.CoordinateFromPixel(hit.Pixel,hitMap),hit.Name);
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Get("MapsUi.Find"+hit.Kind)+" · "+hit.Zone);
            }
            finally{ImGui.PopID();}
        });
    }
}
