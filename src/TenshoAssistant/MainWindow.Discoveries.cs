using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// X-43 (0.807, user request): what the player discovers in range, kept locally to complete the tools, with an export and an import that joins files to it.
// Data: the game's monsters as Tensho already reads them (Maps/MapLiveReader: name and base rows, place, level); never another player.
internal sealed partial class MainWindow
{
    internal DiscoveryStore? DiscoveryStore { get; set; }
    internal string? DiscoveryFolder { get; set; }
    internal Func<uint,string>? MonsterName { get; set; }
    /// <summary>X-72 C (0.856): a FATE's name from the game's sheet.</summary>
    internal Func<uint,string>? FateName { get; set; }
    /// <summary>X-74 (0.860): a duty's name (ContentFinderCondition).</summary>
    internal Func<uint,string>? DutyName { get; set; }
    internal Market.SeenPriceStore? SeenPrices { get; set; }
    /// <summary>Set once the saved base is read (or found missing): nothing is written before, so that a slow read never loses it.</summary>
    internal volatile bool DiscoveriesLoaded;
    private long discoveriesAt=-100_000,discoveriesSavedAt;private int discoverySaving,discoveryBusy;
    private string discoverySearch="",discoverySearched="\u0001";private int discoveryShownRevision=-1;
    private IReadOnlyList<DiscoveryFact> discoveryRows=[];private string? discoveryMessage;private bool discoveryClearAsked;
    private readonly Dictionary<uint,string> discoveryNames=[];
    private string DiscoveryBase=>Path.Combine(DiscoveryFolder ?? "","Discoveries.json");

    /// <summary>Once a second, every frame's overlay pass: the monsters in range noted; the base written every minute when it changed.</summary>
    internal void CollectDiscoveries()
    {
        var now=Environment.TickCount64;
        if(now-discoveriesAt<1000 || windowDisposed || DiscoveryStore is not { } store || !DiscoveriesLoaded)return;
        discoveriesAt=now;
        var settings=configuration.Discovery;
        if(settings.Enabled && settings.MonsterPlaces && mapLive?.State is { Monsters:{ Count:>0 } monsters, MapId:not 0 } live)
        {
            var at=DateTimeOffset.UtcNow;
            foreach(var monster in monsters)
                if(store.SeeMonster(monster.NameId,monster.BaseId,live.TerritoryId,live.MapId,monster.Position,monster.Level,at) && developerMode && settings.DebugLog)
                    log.Information("Discovery: monster {0} (name {1}, base {2}) on map {3} at {4:F1}, {5:F1}, level {6}.",monster.Name,monster.NameId,monster.BaseId,live.MapId,monster.Position.X,monster.Position.Z,monster.Level);
        }
        // X-72 C (0.856): the hunt marks' spawn places and the FATEs seen.
        if(settings.Enabled && mapLive?.State is { MapId:not 0 } here)
        {
            var at=DateTimeOffset.UtcNow;
            if(settings.HuntMarks)
                foreach(var mark in here.Objects ?? [])
                    if(mark.Kind==MapObjectKind.HuntMark && store.SeeHuntMark(mark.NameId,mark.BaseId,here.TerritoryId,here.MapId,mark.Position,mark.Level,at) && developerMode && settings.DebugLog)
                        log.Information("Discovery: hunt mark {0} (name {1}, rank {2}) on map {3} at {4:F1}, {5:F1}.",mark.Name,mark.NameId,mark.Level,here.MapId,mark.Position.X,mark.Position.Z);
            if(settings.Fates)
                foreach(var fate in here.Fates ?? [])
                    if(store.SeeFate(fate.Id,here.TerritoryId,here.MapId,fate.Position,fate.Level,at) && developerMode && settings.DebugLog)
                        log.Information("Discovery: FATE {0} ({1}) on map {2} at {3:F1}, {4:F1}.",fate.Name,fate.Id,here.MapId,fate.Position.X,fate.Position.Z);
        }
        if((store.Dirty || SeenPrices?.Dirty==true) && now-discoveriesSavedAt>60_000)SaveDiscoveries(false);
    }

    /// <summary>The base written off the frame (on unload: at once).</summary>
    private void SaveDiscoveries(bool now)
    {
        if(DiscoveryStore is not { } store || DiscoveryFolder is null || !DiscoveriesLoaded || Interlocked.Exchange(ref discoverySaving,1)==1)return;
        discoveriesSavedAt=Environment.TickCount64;
        var file=store.Snapshot(DateTimeOffset.UtcNow);var path=DiscoveryBase;
        var prices=SeenPrices is { Dirty:true } seen ? seen.Snapshot():null;var pricesPath=Path.Combine(DiscoveryFolder,"MarketSeen.json");
        void Write()
        {
            try{DiscoveryFiles.Write(path,file);if(prices is not null)Market.SeenPriceStore.Write(pricesPath,prices);}
            catch(Exception error){log.Warning(error,"The discoveries could not be saved.");}
            finally{Volatile.Write(ref discoverySaving,0);}
        }
        if(now)Write();else _=Task.Run(Write);
    }

    private string DiscoveryMonsterName(uint id)
    {
        if(!discoveryNames.TryGetValue(id,out var name))
        {
            name=MonsterName?.Invoke(id) ?? "";
            discoveryNames[id]=name=name.Length>0 ? char.ToUpper(name[0],System.Globalization.CultureInfo.CurrentCulture)+name[1..]:$"#{id}";
        }
        return name;
    }

    private readonly Dictionary<uint,string> discoveryFateNames=[];
    private string DiscoveryFateName(uint id)
    {
        if(!discoveryFateNames.TryGetValue(id,out var name))discoveryFateNames[id]=name=FateName?.Invoke(id) is { Length:>0 } found ? found:$"#{id}";
        return name;
    }
    private static readonly DiscoveryKind[] ItemSourceKinds=[DiscoveryKind.Desynthesis,DiscoveryKind.Venture,DiscoveryKind.DutyChest,DiscoveryKind.FateReward,
        DiscoveryKind.MonsterDrop,DiscoveryKind.ItemFromItem,DiscoveryKind.FishCatch,DiscoveryKind.FishCondition,DiscoveryKind.FashionEvaluation];
    private string DiscoveryItemName(uint id)=>itemCatalog.State.Catalog?.Find(id)?.Name ?? $"#{id}";
    private string DiscoveryDutyName(uint id)=>DutyName?.Invoke(id) is { Length:>0 } name ? name:$"#{id}";
    /// <summary>X-74 (0.860): the level column: a monster's levels, a quantity range, or the Eorzea hours a fish was caught.</summary>
    private static string DiscoveryFactLevel(DiscoveryFact fact)
    {
        if(fact.Kind==DiscoveryKind.FishCondition)return $"{fact.LevelMin/60:00}:{fact.LevelMin%60:00}–{fact.LevelMax/60:00}:{fact.LevelMax%60:00} ET";
        var range=fact.LevelMin==fact.LevelMax ? fact.LevelMax.ToString():$"{fact.LevelMin}–{fact.LevelMax}";
        return ItemSourceKinds.Contains(fact.Kind) ? "×"+range:range;
    }
    /// <summary>0.856 (X-72 C): the name shown for a fact: the monster, the hunt mark with its rank, or the FATE. 0.860: the item and its source.</summary>
    private string DiscoveryFactName(DiscoveryFact fact)=>fact.Kind switch
    {
        DiscoveryKind.Desynthesis=>localization.Format("DiscoveriesUi.FromDesynthesis",DiscoveryItemName(fact.Subject),DiscoveryItemName(fact.Detail)),
        DiscoveryKind.Venture=>localization.Format("DiscoveriesUi.FromVenture",DiscoveryItemName(fact.Subject),fact.Detail),
        DiscoveryKind.DutyChest=>localization.Format("DiscoveriesUi.FromDutyChest",DiscoveryItemName(fact.Subject),DiscoveryDutyName(fact.Detail)),
        DiscoveryKind.FateReward=>localization.Format("DiscoveriesUi.FromFate",DiscoveryItemName(fact.Subject),DiscoveryFateName(fact.Detail)),
        DiscoveryKind.MonsterDrop=>localization.Format("DiscoveriesUi.FromMonster",DiscoveryItemName(fact.Subject),DiscoveryMonsterName(fact.Detail)),
        DiscoveryKind.ItemFromItem=>localization.Format("DiscoveriesUi.FromItem",DiscoveryItemName(fact.Subject),DiscoveryItemName(fact.Detail)),
        DiscoveryKind.FishCatch=>localization.Format("DiscoveriesUi.FromBait",DiscoveryItemName(fact.Subject),fact.Detail==0 ? "?":DiscoveryItemName(fact.Detail)),
        DiscoveryKind.FashionEvaluation=>localization.Format("DiscoveriesUi.FashionJudged",DiscoveryItemName(fact.Subject),fashionHints.State.Catalog?.Hints.FirstOrDefault(hint=>hint.Id==fact.Detail)?.Name ?? $"#{fact.Detail}"),
        DiscoveryKind.FishCondition=>localization.Format("DiscoveriesUi.FishWeather",DiscoveryItemName(fact.Subject),weather.State.Catalog?.Weather(fact.Detail)?.Name ?? $"#{fact.Detail}"),
        DiscoveryKind.HuntMarkPlace=>localization.Format("DiscoveriesUi.HuntMarkName",DiscoveryMonsterName(fact.Subject),fact.LevelMax switch {1=>"B",2=>"A",3=>"S",_=>"?"}),
        DiscoveryKind.FateSeen=>localization.Format("DiscoveriesUi.FateName",DiscoveryFateName(fact.Subject)),
        _=>DiscoveryMonsterName(fact.Subject),
    };

    private void DrawDiscoveries()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("DiscoveriesUi.Title"),localization.Get("DiscoveriesUi.Help"));
        if(DiscoveryStore is not { } store || DiscoveryFolder is not { } folder || !DiscoveriesLoaded){ImGui.TextDisabled(localization.Get("DiscoveriesUi.Loading"));return;}
        BoolSetting("DiscoveriesUi.Enabled","DiscoveriesUi.EnabledTip",()=>configuration.Discovery.Enabled,value=>configuration.Discovery.Enabled=value,c=>c.Discovery.Enabled);
        if(configuration.Discovery.Enabled)
        {
            ImGui.Indent();
            BoolSetting("DiscoveriesUi.MonsterPlaces","DiscoveriesUi.MonsterPlacesTip",()=>configuration.Discovery.MonsterPlaces,value=>configuration.Discovery.MonsterPlaces=value,c=>c.Discovery.MonsterPlaces);
            BoolSetting("DiscoveriesUi.HuntMarks","DiscoveriesUi.HuntMarksTip",()=>configuration.Discovery.HuntMarks,value=>configuration.Discovery.HuntMarks=value,c=>c.Discovery.HuntMarks);
            BoolSetting("DiscoveriesUi.Fates","DiscoveriesUi.FatesTip",()=>configuration.Discovery.Fates,value=>configuration.Discovery.Fates=value,c=>c.Discovery.Fates);
            BoolSetting("DiscoveriesUi.ItemSources","DiscoveriesUi.ItemSourcesTip",()=>configuration.Discovery.ItemSources,value=>configuration.Discovery.ItemSources=value,c=>c.Discovery.ItemSources);
            BoolSetting("DiscoveriesUi.Fishing","DiscoveriesUi.FishingTip",()=>configuration.Discovery.Fishing,value=>configuration.Discovery.Fishing=value,c=>c.Discovery.Fishing);
            BoolSetting("DiscoveriesUi.FashionReport","DiscoveriesUi.FashionReportTip",()=>configuration.Discovery.FashionReport,value=>configuration.Discovery.FashionReport=value,c=>c.Discovery.FashionReport);
            BoolSetting("DiscoveriesUi.MarketPrices","DiscoveriesUi.MarketPricesTip",()=>configuration.Discovery.MarketPrices,value=>configuration.Discovery.MarketPrices=value,c=>c.Discovery.MarketPrices);
            ImGui.Unindent();
        }
        var summary=store.Summary();
        if(summary.Count==0)ImGui.TextDisabled(localization.Get("DiscoveriesUi.Empty"));
        if(SeenPrices is { Count:>0 } prices)ImGui.TextUnformatted(localization.Format("DiscoveriesUi.SummaryPrices",prices.Count.ToString("N0")));
        foreach(var (kind,total,seen,importedOnly) in summary)ImGui.TextUnformatted(localization.Format("DiscoveriesUi.Summary"+kind,total.ToString("N0"),seen.ToString("N0"),importedOnly.ToString("N0")));
        DrawDiscoveryFiles(store,folder);
        DrawDiscoveryPlaces(store);
        if(developerMode)DrawDiscoveryDebug(store,folder);
    }

    // Export: a dated copy in « Export »; import: every .json put in « Import », joined to the base, each file once.
    private void DrawDiscoveryFiles(DiscoveryStore store,string folder)
    {
        var busy=Volatile.Read(ref discoveryBusy)==1;
        ImGui.BeginDisabled(busy);
        try
        {
            if(ImGui.SmallButton(localization.Get("DiscoveriesUi.Export")))
                Run(()=>
                {
                    var path=Path.Combine(folder,"Export",DiscoveryFiles.ExportName(DateTimeOffset.UtcNow));
                    DiscoveryFiles.Write(path,store.Snapshot(DateTimeOffset.UtcNow,false));
                    return localization.Format("DiscoveriesUi.ExportDone",path);
                });
            Tip("DiscoveriesUi.ExportTip");
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("DiscoveriesUi.Import")))
                Run(()=>
                {
                    var result=DiscoveryFiles.Import(Path.Combine(folder,"Import"),store);
                    return localization.Format("DiscoveriesUi.ImportDone",result.Files,result.Added.ToString("N0"),result.Joined.ToString("N0"),result.Refused.ToString("N0"),result.Unreadable);
                });
            Tip("DiscoveriesUi.ImportTip");
        }
        finally{ImGui.EndDisabled();}
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("DiscoveriesUi.OpenFolder")))
        {
            try{Directory.CreateDirectory(Path.Combine(folder,"Import"));Directory.CreateDirectory(Path.Combine(folder,"Export"));Dalamud.Utility.Util.OpenLink(folder);}
            catch(Exception error){log.Warning(error,"The discoveries folder could not be opened.");}
        }
        Tip("DiscoveriesUi.OpenFolderTip");
        if(busy)ImGui.TextDisabled(localization.Get("DiscoveriesUi.Working"));
        else if(discoveryMessage is { } message)ImGui.TextWrapped(message);
        void Run(Func<string> work)
        {
            if(Interlocked.Exchange(ref discoveryBusy,1)==1)return;
            discoveryMessage=null;
            _=Task.Run(()=>
            {
                try{discoveryMessage=work();}
                catch(Exception error){log.Warning(error,"A discoveries file could not be handled.");discoveryMessage=localization.Get("DiscoveriesUi.FileFailed");}
                finally{Volatile.Write(ref discoveryBusy,0);}
            });
        }
    }

    // X-72 A (0.854, user choice « ABC » after the parser review: no tool used the places seen): where you saw a monster, for the tools: one line per map, its
    // most seen place, four maps at most; right click: the nearest aetheryte with the flag, or the flag alone. Worked out again when the base changes.
    private readonly Dictionary<string,(long Revision,DiscoveryFact[] Rows)> seenPlaces=[];
    private DiscoveryFact[] SeenPlacesOf(string key,Func<uint,bool> subject)
    {
        if(DiscoveryStore is not { } store || !DiscoveriesLoaded)return [];
        if(seenPlaces.TryGetValue(key,out var known) && known.Revision==store.Revision)return known.Rows;
        var rows=SeenPlaces.ByMap(store.Find(DiscoveryKind.MonsterPlace,subject,500),4);
        if(seenPlaces.Count>=256)seenPlaces.Clear();
        seenPlaces[key]=(store.Revision,rows);
        return rows;
    }
    internal void DrawSeenPlaces(string key,Func<uint,bool> subject,string name)
    {
        var rows=SeenPlacesOf(key,subject);
        if(rows.Length==0 || maps.State.Catalog is not { } catalog)return;
        for(var index=0;index<rows.Length;index++)
        {
            var fact=rows[index];
            if(catalog.Find(fact.Map) is not { } map)continue;
            var pixel=MapProjection.PixelFromWorld(new Vector3(fact.X,0,fact.Z),map);var place=MapProjection.CoordinateFromPixel(pixel,map);
            var level=fact.LevelMin==fact.LevelMax ? fact.LevelMax.ToString():$"{fact.LevelMin}–{fact.LevelMax}";
            ImGui.PushStyleColor(ImGuiCol.Text,Muted);
            try{ImGui.Selectable(localization.Format("DiscoveriesUi.SeenHere",MapName(map),place.X.ToString("0.0"),place.Y.ToString("0.0"),level)+$"##Seen{key}{index}");}
            finally{ImGui.PopStyleColor();}
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("DiscoveriesUi.SeenHereTip"));
            if(!ImGui.BeginPopupContextItem($"SeenMenu{key}{index}"))continue;
            try
            {
                ImGui.BeginDisabled(!MapHasAetheryte(map));
                try{if(ImGui.Selectable(localization.Get("MapView.NearestAetheryte")))AskNearestAetheryteAt(map,pixel,name,flag:true);}
                finally{ImGui.EndDisabled();}
                if(ImGui.Selectable(localization.Get("ItemMenu.PlaceFlag")))FlagFromMap(map,place.X,place.Y);
            }
            finally{ImGui.EndPopup();}
        }
    }

    // The places of the monsters whose name holds the search, most seen first, 200 at most; right click on the place: the nearest aetheryte.
    private void DrawDiscoveryPlaces(DiscoveryStore store)
    {
        ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("##DiscoverySearch",localization.Get("DiscoveriesUi.Search"),ref discoverySearch,64);
        Tip("DiscoveriesUi.SearchTip");
        if(discoverySearch.Trim().Length<2){ImGui.TextDisabled(localization.Get("DiscoveriesUi.SearchFirst"));return;}
        if(discoverySearched!=discoverySearch || discoveryShownRevision!=store.Revision)
        {
            discoverySearched=discoverySearch;discoveryShownRevision=store.Revision;
            var wanted=discoverySearch.Trim();
            // 0.856 (X-72 C): the hunt marks and the FATEs seen too, by their names.
            discoveryRows=[..store.Find(DiscoveryKind.MonsterPlace,id=>DiscoveryMonsterName(id).Contains(wanted,StringComparison.CurrentCultureIgnoreCase),200),
                ..store.Find(DiscoveryKind.HuntMarkPlace,id=>DiscoveryMonsterName(id).Contains(wanted,StringComparison.CurrentCultureIgnoreCase),100),
                ..store.Find(DiscoveryKind.FateSeen,id=>DiscoveryFateName(id).Contains(wanted,StringComparison.CurrentCultureIgnoreCase),100),
                // X-74 (0.860): the items got and their sources, by the item's or the source's name.
                ..ItemSourceKinds.SelectMany(kind=>store.Find(kind,_=>true,20_000)).Where(fact=>DiscoveryFactName(fact).Contains(wanted,StringComparison.CurrentCultureIgnoreCase)).Take(200)];
        }
        if(discoveryRows.Count==0){ImGui.TextDisabled(localization.Get("DiscoveriesUi.NoneFound"));return;}
        if(!ImGui.BeginTable("Discoveries",6,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"Monster","Zone","Place","Level","Seen","Imported"})ImGui.TableSetupColumn(localization.Get("DiscoveriesUi.Column"+column));
            ImGui.TableHeadersRow();
            var catalog=maps.State.Catalog;
            for(var index=0;index<discoveryRows.Count;index++)
            {
                var fact=discoveryRows[index];var map=catalog?.Find(fact.Map);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(DiscoveryFactName(fact));
                ImGui.TableNextColumn();ImGui.TextUnformatted(fact.Map==0 ? (fact.Kind==DiscoveryKind.DutyChest ? DiscoveryDutyName(fact.Detail):"—"):map is null ? $"#{fact.Map}":MapName(map));
                ImGui.TableNextColumn();
                if(map is null)ImGui.TextDisabled("—");
                else
                {
                    var pixel=MapProjection.PixelFromWorld(new Vector3(fact.X,0,fact.Z),map);var place=MapProjection.CoordinateFromPixel(pixel,map);
                    ImGui.Selectable(localization.Format("DiscoveriesUi.Coordinates",place.X.ToString("0.0"),place.Y.ToString("0.0"))+$"##DiscoveryPlace{index}");
                    if(ImGui.BeginPopupContextItem($"DiscoveryMenu{index}"))
                    {
                        try
                        {
                            ImGui.BeginDisabled(!MapHasAetheryte(map));
                            try{if(ImGui.Selectable(localization.Get("MapView.NearestAetheryte")))AskNearestAetheryteAt(map,pixel,DiscoveryMonsterName(fact.Subject),flag:true);}
                            finally{ImGui.EndDisabled();}
                        }
                        finally{ImGui.EndPopup();}
                    }
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("DiscoveriesUi.PlaceTip"));
                }
                ImGui.TableNextColumn();ImGui.TextUnformatted(DiscoveryFactLevel(fact));
                ImGui.TableNextColumn();ImGui.TextUnformatted(fact.Seen.ToString("N0"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(fact.Imported.ToString("N0"));
            }
        }
        finally{ImGui.EndTable();}
    }

    // Developer mode: a line in the log for each new fact, where the files are, and emptying the base (asked twice).
    private void DrawDiscoveryDebug(DiscoveryStore store,string folder)
    {
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("DiscoveriesUi.Debug"));
        BoolSetting("DiscoveriesUi.DebugLog","DiscoveriesUi.DebugLogTip",()=>configuration.Discovery.DebugLog,value=>configuration.Discovery.DebugLog=value,c=>c.Discovery.DebugLog);
        ImGui.TextDisabled(folder);
        if(!discoveryClearAsked){if(ImGui.SmallButton(localization.Get("DiscoveriesUi.Clear")))discoveryClearAsked=true;Tip("DiscoveriesUi.ClearTip");return;}
        ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("DiscoveriesUi.ClearSure"));
        if(ImGui.SmallButton(localization.Get("DiscoveriesUi.ClearYes"))){store.Clear();SaveDiscoveries(false);discoveryClearAsked=false;}
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("DiscoveriesUi.ClearNo")))discoveryClearAsked=false;
    }
}
