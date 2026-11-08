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

    private void DrawDiscoveries()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("DiscoveriesUi.Title"),localization.Get("DiscoveriesUi.Help"));
        if(DiscoveryStore is not { } store || DiscoveryFolder is not { } folder || !DiscoveriesLoaded){ImGui.TextDisabled(localization.Get("DiscoveriesUi.Loading"));return;}
        BoolSetting("DiscoveriesUi.Enabled","DiscoveriesUi.EnabledTip",()=>configuration.Discovery.Enabled,value=>configuration.Discovery.Enabled=value,c=>c.Discovery.Enabled);
        if(configuration.Discovery.Enabled)
        {
            ImGui.Indent();
            BoolSetting("DiscoveriesUi.MonsterPlaces","DiscoveriesUi.MonsterPlacesTip",()=>configuration.Discovery.MonsterPlaces,value=>configuration.Discovery.MonsterPlaces=value,c=>c.Discovery.MonsterPlaces);
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
            discoveryRows=store.Find(DiscoveryKind.MonsterPlace,id=>DiscoveryMonsterName(id).Contains(wanted,StringComparison.CurrentCultureIgnoreCase),200);
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
                ImGui.TableNextColumn();ImGui.TextUnformatted(DiscoveryMonsterName(fact.Subject));
                ImGui.TableNextColumn();ImGui.TextUnformatted(map is null ? $"#{fact.Map}":MapName(map));
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
                ImGui.TableNextColumn();ImGui.TextUnformatted(fact.LevelMin==fact.LevelMax ? fact.LevelMax.ToString():$"{fact.LevelMin}–{fact.LevelMax}");
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
