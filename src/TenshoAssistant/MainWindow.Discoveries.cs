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
    /// <summary>X-172 (0.1136): the SQLite database of the discoveries, once opened (null: the JSON files of before, when it could not be made).</summary>
    internal DiscoveryDatabase? DiscoveryDatabase { get; set; }
    /// <summary>X-77.1 (0.883): the exchange with the repository through GitHub (connection by code, opt-in automatic exchange).</summary>
    internal GitHubExchange? DiscoveryExchange { get; set; }
    internal Func<uint,string>? MonsterName { get; set; }
    /// <summary>X-72 C (0.856): a FATE's name from the game's sheet.</summary>
    internal Func<uint,string>? FateName { get; set; }
    /// <summary>X-74 (0.860): a duty's name (ContentFinderCondition).</summary>
    internal Func<uint,string>? DutyName { get; set; }
    /// <summary>X-73.8 (0.862): a submersible sector's name.</summary>
    internal Func<uint,string>? SectorName { get; set; }
    internal Func<uint,string>? AirshipSectorName { get; set; }
    /// <summary>X-73.9 (0.863): a blue magic spell's and a beast's names.</summary>
    internal Func<uint,string>? SpellName { get; set; }
    /// <summary>X-73.18 (0.889): a Quest row's name (QuestNameOf, MainWindow.ShopRequirements.cs), or its number.</summary>
    private string DiscoveryQuestName(uint id)=>QuestNameOf?.Invoke(id) is { Length:>0 } name ? name:$"#{id}";
    internal Func<uint,string>? BeastName { get; set; }
    private string DiscoveryNamed(Func<uint,string>? name,uint id)=>name?.Invoke(id) is { Length:>0 } found ? found:$"#{id}";
    private string DiscoverySectorName(uint id)=>SectorName?.Invoke(id) is { Length:>0 } name ? name:$"#{id}";
    internal Market.SeenPriceStore? SeenPrices { get; set; }
    /// <summary>Set once the saved base is read (or found missing): nothing is written before, so that a slow read never loses it.</summary>
    internal volatile bool DiscoveriesLoaded;
    /// <summary>X-93 part 2 (0.1039): the unreadable base set aside at load (its new name), and whether it could not be moved (then nothing is saved).</summary>
    internal volatile string? DiscoveriesSetAside;
    internal volatile bool DiscoveriesWriteBlocked;
    private long discoveriesAt=-100_000,discoveriesSavedAt;private int discoverySaving,discoveryBusy;
    private string discoverySearch="",discoverySearched="\u0001";private int discoveryShownRevision=-1;
    private IReadOnlyList<DiscoveryFact> discoveryRows=[];private string? discoveryMessage;private bool discoveryClearAsked;
    private readonly Dictionary<uint,string> discoveryNames=[];
    private string DiscoveryBase=>Path.Combine(DiscoveryFolder ?? "","Discoveries.json");
    private readonly Dictionary<DiscoveryKind,string> sharedWritten=[];

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
        // X-77.1 (0.883): the automatic exchange, only when the player turned it on, once a day at most.
        if(settings.Enabled && settings.AutoExchange && DiscoveryExchange is { } exchange && exchange.Due(DateTimeOffset.UtcNow))exchange.Exchange(store,exchange.Connected);
    }

    /// <summary>
    /// X-142 (0.1121, audit A04): at unload, a save already running is waited for (5 s at most), then what is still to save is written at
    /// once; a write that fails is logged (the file on disk stays the last good one).
    /// </summary>
    private void FlushDiscoveries()
    {
        var until=Environment.TickCount64+5_000;
        while(Volatile.Read(ref discoverySaving)==1 && Environment.TickCount64<until)Thread.Sleep(20);
        if(DiscoveryStore?.Dirty==true || SeenPrices?.Dirty==true)SaveDiscoveries(true);
        // X-172 (0.1136): the journal folded into the database file at unload.
        try{DiscoveryDatabase?.Checkpoint();}catch(Exception error){log.Warning(error,"The discoveries database journal could not be folded in; SQLite does it at the next start.");}
    }

    /// <summary>The base written off the frame (on unload: at once).</summary>
    private void SaveDiscoveries(bool now)
    {
        if(DiscoveryStore is not { } store || DiscoveryFolder is null || !DiscoveriesLoaded || DiscoveriesWriteBlocked || Interlocked.Exchange(ref discoverySaving,1)==1)return;
        discoveriesSavedAt=Environment.TickCount64;
        var path=DiscoveryBase;
        // X-142 (0.1121, audit A04): each copy is marked written only after its write; a failed write keeps it to save (retried a minute later).
        long pricesStamp=0;
        var seenPrices=SeenPrices;
        var prices=seenPrices is { Dirty:true } seen ? seen.SnapshotToSave(out pricesStamp):null;var pricesPath=Path.Combine(DiscoveryFolder,"MarketSeen.json");
        void Write()
        {
            // X-93 (0.940): yours in Discoveries.json, the shared ones apart, by kind, compressed (DiscoveryFiles.WriteSplit). X-93 part 2 (0.1040): the base is
            // copied here, off the frame and by slices (DiscoveryStore.Snapshot): on the frame it was 21 ms at 100 000 facts, 339 ms at a million.
            // X-172 (0.1136): with the database, only the rows changed since the last save, in one transaction; a failed write keeps them to save.
            try
            {
                if(DiscoveryDatabase is { } database)
                {
                    var changed=store.ChangesToSave(out var stamp);
                    try{database.Write(changed.Write,changed.Remove,changed.All);}
                    catch(Exception){store.SaveFailed(changed);throw;}
                    store.MarkSaved(stamp);
                }
                else{var held=store.SharedKindsHeld();var file=store.SnapshotToSave(DateTimeOffset.UtcNow,out var stamp);lock(sharedWritten)DiscoveryFiles.WriteSplit(path,file,sharedWritten,held);store.MarkSaved(stamp);}
            }
            catch(Exception error){log.Warning(error,"The discoveries could not be saved; kept to save again.");}
            try{if(prices is not null){Market.SeenPriceStore.Write(pricesPath,prices);seenPrices!.MarkSaved(pricesStamp);}}
            catch(Exception error){log.Warning(error,"The seen prices could not be saved; kept to save again.");}
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
    /// <summary>X-74 (0.865): where this item was seen in game coming from (yours or shared; the item-source kinds, most seen first, four at most); worked out again when the base changes.</summary>
    private (uint Item,long Revision,string[] Lines) discoveredSources=(0,-1,[]);
    internal IReadOnlyList<string> DiscoveredSourcesOf(uint item)
    {
        if(item==0 || DiscoveryStore is not { } store || !DiscoveriesLoaded)return [];
        if(discoveredSources.Item==item && discoveredSources.Revision==store.Revision)return discoveredSources.Lines;
        var lines=ItemSourceKinds.Where(kind=>kind is not (DiscoveryKind.FishCondition or DiscoveryKind.FestivalSeen or DiscoveryKind.VistaSeen or DiscoveryKind.VoyageUnlock
                or DiscoveryKind.BlueSpellLearned or DiscoveryKind.BeastTamed or DiscoveryKind.FashionEvaluation or DiscoveryKind.NpcPlace or DiscoveryKind.BlueSpellCaster
                or DiscoveryKind.FishLure or DiscoveryKind.FishTransition or DiscoveryKind.FishBite or DiscoveryKind.QuestItemAsked or DiscoveryKind.SpearShadow))
            .SelectMany(kind=>store.Find(kind,subject=>subject==item,4)).OrderByDescending(DiscoveryFact.TimesOf).Take(4)
            .Select(fact=>$"{DiscoveryFactName(fact)} ({DiscoveryFactLevel(fact)}, ×{DiscoveryFact.TimesOf(fact)})").ToArray();
        discoveredSources=(item,store.Revision,lines);
        return lines;
    }
    private static readonly DiscoveryKind[] ItemSourceKinds=[DiscoveryKind.Reduction,DiscoveryKind.Desynthesis,DiscoveryKind.Venture,DiscoveryKind.DutyChest,DiscoveryKind.FateReward,
        DiscoveryKind.MonsterDrop,DiscoveryKind.ItemFromItem,DiscoveryKind.FishCatch,DiscoveryKind.FishCondition,DiscoveryKind.FashionEvaluation,DiscoveryKind.VoyageDrop,DiscoveryKind.AirshipDrop,DiscoveryKind.VoyageUnlock,DiscoveryKind.BlueSpellLearned,DiscoveryKind.BeastTamed,DiscoveryKind.FestivalSeen,DiscoveryKind.VistaSeen];
    /// <summary>0.911 (X-76): the other kinds the page's search lists too (never shown as an item's source); with the places, FATEs and item sources, every kind.</summary>
    private static readonly DiscoveryKind[] OtherSeenKinds=[DiscoveryKind.NpcPlace,DiscoveryKind.BlueSpellCaster,DiscoveryKind.FishLure,DiscoveryKind.FishTransition,DiscoveryKind.FishBite,
        DiscoveryKind.QuestItemAsked,DiscoveryKind.SpearShadow,DiscoveryKind.FishBiteBait,DiscoveryKind.IntuitionStart,DiscoveryKind.IntuitionCatch,DiscoveryKind.VoyageVisit,DiscoveryKind.MonsterKill,DiscoveryKind.DutyChestOpened,DiscoveryKind.SpectralBefore,DiscoveryKind.FishPoints,DiscoveryKind.AirshipUnlock];
    /// <summary>
    /// X-73.16 (0.872): where you saw a shopkeeper, as a vendor place, for the vendors the game sheets do not place (their own places always come first).
    /// Worked out again only when the base changes.
    /// </summary>
    private (long Revision,Dictionary<uint,Npcs.NpcLocation?> Places) seenNpcPlaces=(-1,[]);
    internal Npcs.NpcLocation? SeenNpcPlace(uint npc)
    {
        if(npc==0 || DiscoveryStore is not { } store || !DiscoveriesLoaded || maps.State.Catalog is not { } catalog)return null;
        if(seenNpcPlaces.Revision!=store.Revision)seenNpcPlaces=(store.Revision,[]);
        if(seenNpcPlaces.Places.TryGetValue(npc,out var known))return known;
        Npcs.NpcLocation? place=null;
        if(store.Find(DiscoveryKind.NpcPlace,npc,1).FirstOrDefault() is { } fact && catalog.Find(fact.Map) is { } map)
        {
            var at=MapProjection.CoordinateFromWorld(new Vector3(fact.X,0,fact.Z),map);
            place=new Npcs.NpcLocation(fact.Territory,fact.Map,map.Zone,at.X,at.Y,fact.X,0,fact.Z);
        }
        if(seenNpcPlaces.Places.Count<4096)seenNpcPlaces.Places[npc]=place;
        return place;
    }
    internal IReadOnlyList<Npcs.NpcLocation> VendorLocations(Npcs.NpcVendor vendor)=>vendor.Locations.Count>0 ? vendor.Locations:SeenNpcPlace(vendor.NpcId) is { } seen ? [seen]:[];
    /// <summary>
    /// X-75.2 (0.875, user request: what was learned in game first wherever community data is read): the discoveries by monster, by item and by source item,
    /// worked out once per change of the base (bounded reads), for the pages that still read LuminaSupplemental, Garland or the wiki.
    /// </summary>
    internal sealed record SeenIndex(IReadOnlyDictionary<uint,uint[]> DropsByMonster,IReadOnlyDictionary<uint,uint[]> MonstersByItem,IReadOnlyDictionary<uint,uint[]> BeastsByMonster,
        IReadOnlyDictionary<uint,uint[]> GivesBySource,IReadOnlyDictionary<uint,uint[]> SourcesByItem,IReadOnlyDictionary<uint,uint[]> DutiesByItem,
        IReadOnlyDictionary<uint,uint[]> ItemsByDuty);
    private (long Revision,SeenIndex? Index) seenIndex=(-1,null);
    internal SeenIndex? Seen()
    {
        if(DiscoveryStore is not { } store || !DiscoveriesLoaded)return null;
        if(seenIndex.Revision==store.Revision)return seenIndex.Index;
        Dictionary<uint,uint[]> By(IEnumerable<DiscoveryFact> facts,Func<DiscoveryFact,uint> key,Func<DiscoveryFact,uint> value)=>
            facts.Where(fact=>key(fact)!=0 && value(fact)!=0).GroupBy(key).ToDictionary(group=>group.Key,group=>group.Select(value).Distinct().Take(32).ToArray());
        var drops=store.Find(DiscoveryKind.MonsterDrop,_=>true,20_000);
        var fromItems=store.Find(DiscoveryKind.Desynthesis,_=>true,20_000).Concat(store.Find(DiscoveryKind.ItemFromItem,_=>true,20_000))
            .Concat(store.Find(DiscoveryKind.Reduction,_=>true,20_000)).ToArray();
        var chests=store.Find(DiscoveryKind.DutyChest,_=>true,20_000);
        var index=new SeenIndex(By(drops,fact=>fact.Detail,fact=>fact.Subject),By(drops,fact=>fact.Subject,fact=>fact.Detail),
            By(store.Find(DiscoveryKind.BeastTamed,_=>true,2_000),fact=>fact.Detail,fact=>fact.Subject),By(fromItems,fact=>fact.Detail,fact=>fact.Subject),
            By(fromItems,fact=>fact.Subject,fact=>fact.Detail),By(chests,fact=>fact.Subject,fact=>fact.Detail),By(chests,fact=>fact.Detail,fact=>fact.Subject));
        seenIndex=(store.Revision,index);
        return index;
    }
    /// <summary>X-75.3 (0.877): what was seen in game for a fish (yours or shared: baits or mooched fish, weathers, weathers before, lures, Eorzea hours), one line; never a requirement.</summary>
    private (long Revision,Dictionary<uint,string?> Lines) seenFishLines=(-1,[]);
    internal string? SeenFishLine(uint fish)
    {
        if(DiscoveryStore is not { } store || !DiscoveriesLoaded)return null;
        if(seenFishLines.Revision!=store.Revision)seenFishLines=(store.Revision,[]);
        if(seenFishLines.Lines.TryGetValue(fish,out var known))return known;
        var catches=store.Find(DiscoveryKind.FishCatch,fish,16);
        string Weathers(DiscoveryKind kind)=>string.Join(", ",store.Find(kind,subject=>subject==fish,8).Select(fact=>weather.State.Catalog?.Weather(fact.Detail)?.Name ?? $"#{fact.Detail}").Distinct());
        var parts=new List<string>();
        if(catches.Where(fact=>fact.Detail!=0).Select(fact=>DiscoveryItemName(fact.Detail)).Distinct().ToArray() is { Length:>0 } baits)parts.Add(localization.Format("FishingPanel.SeenBaits",string.Join(", ",baits)));
        if(Weathers(DiscoveryKind.FishTransition) is { Length:>0 } before)parts.Add(localization.Format("FishingPanel.SeenBefore",before));
        if(Weathers(DiscoveryKind.FishCondition) is { Length:>0 } during)parts.Add(localization.Format("FishingPanel.SeenWeather",during));
        if(store.Find(DiscoveryKind.FishCondition,fish,64) is { Count:>0 } hours)
            parts.Add(EtRange(hours.Min(fact=>fact.LevelMin),hours.Max(fact=>fact.LevelMax)));
        // X-73.18 (0.887): the bites seen in game (yours or shared): strength and time from the cast.
        if(store.Find(DiscoveryKind.FishBite,fish,32) is { Count:>0 } bites)
            parts.Add(string.Join(", ",bites.GroupBy(fact=>fact.Detail).OrderBy(group=>group.Key).Select(group=>
                $"{(group.Key switch{1=>"!",2=>"!!",3=>"!!!",_=>"?"})} {group.Min(fact=>fact.LevelMin)/10.0:0.0}–{group.Max(fact=>fact.LevelMax)/10.0:0.0} s")));
        if(store.Find(DiscoveryKind.FishLure,fish,2) is { Count:>0 } lures)
            parts.Add(string.Join(", ",lures.Select(fact=>localization.Get(fact.Detail==1 ? "FishingPanel.SeenAmbitious":"FishingPanel.SeenModest"))));
        // X-73.18 (0.890, 0.891): the spearfishing shadows it was seen in game with (yours or shared).
        var shadows=store.Find(DiscoveryKind.SpearShadow,fish,8);
        if(shadows.Count>0)parts.Add(localization.Format("Spearfishing.SeenShadow",string.Join(", ",shadows.Select(fact=>SpearShadowText((int)fact.Detail,fact.LevelMin)).Distinct())));
        var line=parts.Count==0 ? null:localization.Format("FishingPanel.SeenInGame",catches.Sum(DiscoveryFact.TimesOf)+shadows.Sum(DiscoveryFact.TimesOf),string.Join(" · ",parts));
        if(seenFishLines.Lines.Count<2048)seenFishLines.Lines[fish]=line;
        return line;
    }
    // X-77.1 (0.883): connection to GitHub by a code, exchange now, automatic exchange (off by default), disconnection.
    private void DrawDiscoveryExchange(DiscoveryStore store)
    {
        if(DiscoveryExchange is not { } exchange)return;
        ImGui.Spacing();
        ImGui.TextColored(Accent,localization.Get("DiscoveriesUi.ExchangeTitle"));
        Tip("DiscoveriesUi.ExchangeHelp");
        var working=exchange.Busy;
        if(!exchange.Connected)
        {
            if(exchange.UserCode is { } code)
            {
                ImGui.TextUnformatted(localization.Format("DiscoveriesUi.ConnectCode",code));
                ImGui.SameLine();
                CopyButton(localization.Get("DiscoveriesUi.ConnectCopy"),()=>code);
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("DiscoveriesUi.ConnectOpen")))Dalamud.Utility.Util.OpenLink(exchange.VerifyAddress ?? "https://github.com/login/device");
            }
            else
            {
                // X-76 step 5 (0.949, user's go-ahead): said before connecting, not only in a tooltip.
                ImGui.PushTextWrapPos();
                try{ImGui.TextColored(Warning,localization.Get("DiscoveriesUi.PublicWarning"));}
                finally{ImGui.PopTextWrapPos();}
                ImGui.BeginDisabled(working);
                try{if(ImGui.SmallButton(localization.Get("DiscoveriesUi.Connect")))exchange.Connect();}
                finally{ImGui.EndDisabled();}
                Tip("DiscoveriesUi.ConnectTip");
            }
        }
        else
        {
            ImGui.TextUnformatted(localization.Format("DiscoveriesUi.ConnectedAs",exchange.Login));
            ImGui.SameLine();
            ImGui.BeginDisabled(working);
            try{if(ImGui.SmallButton(localization.Get("DiscoveriesUi.ExchangeNow")))exchange.Exchange(store,true);}
            finally{ImGui.EndDisabled();}
            Tip("DiscoveriesUi.ExchangeNowTip");
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("DiscoveriesUi.Disconnect")))exchange.Disconnect();
            Tip("DiscoveriesUi.DisconnectTip");
        }
        // X-76 step 3 (0.952): the automatic exchange runs only while discoveries are noted (settings.Enabled), greyed while that is off; its tip says so.
        ImGui.BeginDisabled(!configuration.Discovery.Enabled);
        try{BoolSetting("DiscoveriesUi.AutoExchange","DiscoveriesUi.AutoExchangeTip",()=>configuration.Discovery.AutoExchange,value=>configuration.Discovery.AutoExchange=value,c=>c.Discovery.AutoExchange);}
        finally{ImGui.EndDisabled();}
        if(exchange.Exchanged is { } last)ImGui.TextDisabled(localization.Format("DiscoveriesUi.ExchangedAt",last.ToLocalTime().ToString("g")));
        if(working)ImGui.TextDisabled(localization.Get("DiscoveriesUi.Working"));
        else if(exchange.Message is { } said)
            ImGui.TextWrapped(said.Key=="ExchangeDone"
                ? localization.Format("DiscoveriesUi.ExchangeDone",said.Args[0],said.Args[1],localization.Get("DiscoveriesUi."+said.Args[2]),said.Args[3].Length>0 ? localization.Get("DiscoveriesUi."+said.Args[3]):"")
                :localization.Format("DiscoveriesUi."+said.Key,said.Args.Cast<object>().ToArray()));
    }
    /// <summary>X-76 (0.911): a key item's name (EventItem row, 2 000 000 and up).</summary>
    internal Func<uint,string?>? KeyItemNameOf { get; set; }
    private string DiscoveryItemName(uint id)=>id>=ItemGains.KeyItemStart ? KeyItemNameOf?.Invoke(id) is { Length:>0 } key ? key:$"#{id}"
        :itemCatalog.State.Catalog?.Find(id)?.Name ?? $"#{id}";
    private string DiscoveryDutyName(uint id)=>DutyName?.Invoke(id) is { Length:>0 } name ? name:$"#{id}";
    private static DateTime DiscoveryDay(int day)=>new DateTime(2013,8,27).AddDays(day);
    // « HH:mm–HH:mm ET » of two Eorzea minutes of the day.
    private static string EtRange(int from,int to)=>$"{Gathering.GatheringTime.Clock(from)}–{Gathering.GatheringTime.Clock(to)} ET";
    // X-76 (0.908): « a » or « a–b », a level or quantity range.
    private static string LevelRange(int min,int max)=>min==max ? max.ToString():$"{min}–{max}";
    /// <summary>X-74 (0.860): the level column: a monster's levels, a quantity range, or the Eorzea hours a fish was caught.</summary>
    private static string DiscoveryFactLevel(DiscoveryFact fact)
    {
        if(fact.Kind==DiscoveryKind.FestivalSeen)
            return $"{DiscoveryDay(fact.LevelMin):d} – {DiscoveryDay(fact.LevelMax):d}";
        if(fact.Kind is DiscoveryKind.FishCondition or DiscoveryKind.VistaSeen or DiscoveryKind.FishTransition)return EtRange(fact.LevelMin,fact.LevelMax);
        if(fact.Kind is DiscoveryKind.FishBite or DiscoveryKind.FishBiteBait)return BiteRange(fact.LevelMin,fact.LevelMax)+" s";
        if(fact.Kind==DiscoveryKind.IntuitionStart)return LevelRange(fact.LevelMin,fact.LevelMax)+" s";
        var range=LevelRange(fact.LevelMin,fact.LevelMax);
        return ItemSourceKinds.Contains(fact.Kind) || fact.Kind==DiscoveryKind.QuestItemAsked ? "×"+range:range;
    }
    // 0.911 (X-76): a bite time range from tenths of a second, « 1.2–3.4 ».
    private static string BiteRange(int min,int max)=>min==max ? $"{min/10.0:0.0}":$"{min/10.0:0.0}–{max/10.0:0.0}";
    private static string BiteMark(uint strength)=>strength switch{1=>"!",2=>"!!",3=>"!!!",_=>"?"};
    private string DiscoveryWeatherName(uint id)=>weather.State.Catalog?.Weather(id)?.Name ?? $"#{id}";
    /// <summary>0.856 (X-72 C): the name shown for a fact: the monster, the hunt mark with its rank, or the FATE. 0.860: the item and its source.</summary>
    private string DiscoveryFactName(DiscoveryFact fact)=>fact.Kind switch
    {
        DiscoveryKind.Desynthesis=>localization.Format("DiscoveriesUi.FromDesynthesis",DiscoveryItemName(fact.Subject),DiscoveryItemName(fact.Detail)),
        DiscoveryKind.Venture=>localization.Format("DiscoveriesUi.FromVenture",DiscoveryItemName(fact.Subject),fact.Detail),
        DiscoveryKind.DutyChest=>localization.Format("DiscoveriesUi.FromDutyChest",DiscoveryItemName(fact.Subject),DiscoveryDutyName(fact.Detail)),
        DiscoveryKind.FateReward=>localization.Format("DiscoveriesUi.FromFate",DiscoveryItemName(fact.Subject),DiscoveryFateName(fact.Detail)),
        DiscoveryKind.MonsterDrop=>localization.Format("DiscoveriesUi.FromMonster",DiscoveryItemName(fact.Subject),DiscoveryMonsterName(fact.Detail)),
        DiscoveryKind.QuestItemAsked=>localization.Format("DiscoveriesUi.QuestAsked",DiscoveryItemName(QuestItemAsks.Item(fact.Subject))+(QuestItemAsks.Hq(fact.Subject) ? " HQ":""),DiscoveryQuestName(fact.Detail)),
        DiscoveryKind.SpearShadow=>localization.Format("DiscoveriesUi.SpearShadow",DiscoveryItemName(fact.Subject),SpearShadowText((int)fact.Detail,fact.LevelMin)),
        DiscoveryKind.Reduction=>localization.Format("DiscoveriesUi.FromReduction",DiscoveryItemName(fact.Subject),DiscoveryItemName(fact.Detail)),
        DiscoveryKind.ItemFromItem=>localization.Format("DiscoveriesUi.FromItem",DiscoveryItemName(fact.Subject),DiscoveryItemName(fact.Detail)),
        DiscoveryKind.FishCatch=>localization.Format("DiscoveriesUi.FromBait",DiscoveryItemName(fact.Subject),fact.Detail==0 ? "?":DiscoveryItemName(fact.Detail)),
        DiscoveryKind.BlueSpellCaster=>localization.Format("DiscoveriesUi.SpellCaster",DiscoveryNamed(SpellName,fact.Subject),DiscoveryMonsterName(fact.Detail)),
        DiscoveryKind.BlueSpellLearned=>localization.Format("DiscoveriesUi.SpellFrom",DiscoveryNamed(SpellName,fact.Subject),fact.Detail==0 ? "?":DiscoveryMonsterName(fact.Detail)),
        DiscoveryKind.BeastTamed=>localization.Format("DiscoveriesUi.BeastFrom",DiscoveryNamed(BeastName,fact.Subject),fact.Detail==0 ? "?":DiscoveryMonsterName(fact.Detail)),
        DiscoveryKind.FestivalSeen=>localization.Format("DiscoveriesUi.FestivalSeen",fact.Subject,fact.Detail),
        DiscoveryKind.NpcPlace=>localization.Format("DiscoveriesUi.NpcPlace",SquadronMemberName?.Invoke(fact.Subject) is { Length:>0 } npcName ? npcName:$"#{fact.Subject}"),
        DiscoveryKind.VistaSeen=>localization.Format("DiscoveriesUi.VistaSeen",fact.Subject,weather.State.Catalog?.Weather(fact.Detail)?.Name ?? $"#{fact.Detail}"),
        DiscoveryKind.VoyageDrop=>localization.Format("DiscoveriesUi.FromSector",DiscoveryItemName(fact.Subject),DiscoverySectorName(fact.Detail)),
        DiscoveryKind.AirshipDrop=>localization.Format("DiscoveriesUi.FromAirshipSector",DiscoveryItemName(fact.Subject),AirshipSectorName?.Invoke(fact.Detail) is { Length:>0 } sector ? sector:$"#{fact.Detail}"),
        DiscoveryKind.AirshipUnlock=>localization.Format("DiscoveriesUi.AirshipSectorUnlock",AirshipSectorName?.Invoke(fact.Subject) is { Length:>0 } unlocked ? unlocked:$"#{fact.Subject}"),
        DiscoveryKind.VoyageUnlock=>localization.Format("DiscoveriesUi.SectorUnlock",DiscoverySectorName(fact.Subject),DiscoverySectorName(fact.Detail)),
        DiscoveryKind.FashionEvaluation=>localization.Format("DiscoveriesUi.FashionJudged",DiscoveryItemName(fact.Subject),fashionHints.State.Catalog?.Hints.FirstOrDefault(hint=>hint.Id==fact.Detail)?.Name ?? $"#{fact.Detail}"),
        DiscoveryKind.FishCondition=>localization.Format("DiscoveriesUi.FishWeather",DiscoveryItemName(fact.Subject),weather.State.Catalog?.Weather(fact.Detail)?.Name ?? $"#{fact.Detail}"),
        // 0.911 (X-76): the fishing kinds read as fish, not as monsters.
        DiscoveryKind.FishLure=>localization.Format("DiscoveriesUi.FishLure",DiscoveryItemName(fact.Subject),fact.Detail switch
            {1=>localization.Get("FishingPanel.SeenAmbitious"),2=>localization.Get("FishingPanel.SeenModest"),_=>$"#{fact.Detail}"}),
        DiscoveryKind.FishTransition=>localization.Format("DiscoveriesUi.FishTransition",DiscoveryItemName(fact.Subject),DiscoveryWeatherName(fact.Detail)),
        DiscoveryKind.FishBite=>localization.Format("DiscoveriesUi.FishBite",DiscoveryItemName(fact.Subject),BiteMark(fact.Detail),BiteRange(fact.LevelMin,fact.LevelMax)),
        DiscoveryKind.VoyageVisit=>localization.Format(fact.Detail==1 ? "DiscoveriesUi.VoyageVisitTwice":"DiscoveriesUi.VoyageVisit",DiscoverySectorName(fact.Subject)),
        DiscoveryKind.FishPoints=>localization.Format("DiscoveriesUi.FishPoints",DiscoveryItemName(fact.Subject),LevelRange(fact.LevelMin,fact.LevelMax)),
        DiscoveryKind.SpectralBefore=>localization.Format("DiscoveriesUi.SpectralBefore",DiscoveryItemName(fact.Subject),fact.Detail==0 ? "?":DiscoveryItemName(fact.Detail)),
        DiscoveryKind.MonsterKill=>localization.Format("DiscoveriesUi.MonsterKill",DiscoveryMonsterName(fact.Subject)),
        DiscoveryKind.DutyChestOpened=>localization.Format("DiscoveriesUi.DutyChestOpened",DiscoveryDutyName(fact.Subject)),
        DiscoveryKind.FishBiteBait=>localization.Format("DiscoveriesUi.FishBiteBait",DiscoveryItemName(fact.Subject),DiscoveryItemName(fact.Detail),BiteRange(fact.LevelMin,fact.LevelMax)),
        DiscoveryKind.IntuitionStart=>localization.Format("DiscoveriesUi.IntuitionStart",DiscoveryItemName(fact.Subject),LevelRange(fact.LevelMin,fact.LevelMax)),
        DiscoveryKind.IntuitionCatch=>localization.Format("DiscoveriesUi.IntuitionCatch",DiscoveryItemName(fact.Subject),fact.Detail==0 ? "?":DiscoveryItemName(fact.Detail)),
        DiscoveryKind.HuntMarkPlace=>localization.Format("DiscoveriesUi.HuntMarkName",DiscoveryMonsterName(fact.Subject),fact.LevelMax switch {1=>"B",2=>"A",3=>"S",_=>"?"}),
        DiscoveryKind.FateSeen=>localization.Format("DiscoveriesUi.FateName",DiscoveryFateName(fact.Subject)),
        DiscoveryKind.MonsterPlace=>DiscoveryMonsterName(fact.Subject),
        _=>$"#{fact.Subject}",
    };

    private void DrawDiscoveries()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("DiscoveriesUi.Title"),localization.Get("DiscoveriesUi.Help"));
        if(DiscoveriesSetAside is { } setAside)
        {
            ImGui.PushStyleColor(ImGuiCol.Text,DiscoveriesWriteBlocked ? Error:Warning);
            ImGui.TextWrapped(localization.Format(DiscoveriesWriteBlocked ? "DiscoveriesUi.BaseBlocked":"DiscoveriesUi.BaseSetAside",Path.GetFileName(setAside)));
            ImGui.PopStyleColor();
        }
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
        // X-93 (0.942): the page reads every shared kind still on disk, and says so while it does.
        store.WantAll();
        if(store.SharedKindsWaiting is > 0 and var waiting)ImGui.TextDisabled(localization.Format("DiscoveriesUi.SharedReading",waiting));
        var summary=store.Summary();
        // X-93 (0.939): never silent: shared discoveries left out because the shared part is full, or nearly full.
        var sharedHeld=summary.Count==0 ? 0:summary.Max(row=>row.ImportedOnly);
        if(store.LeftOut>0)ImGui.TextColored(Warning,localization.Format("DiscoveriesUi.SharedFull",store.LeftOut.ToString("N0"),Discoveries.DiscoveryStore.MaxFacts.ToString("N0")));
        else if(sharedHeld>=Discoveries.DiscoveryStore.MaxFacts*9/10)ImGui.TextColored(Warning,localization.Format("DiscoveriesUi.SharedNearlyFull",sharedHeld.ToString("N0"),Discoveries.DiscoveryStore.MaxFacts.ToString("N0")));
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
        DrawDiscoveryExchange(store);
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
            var level=LevelRange(fact.LevelMin,fact.LevelMax);
            ImGui.PushStyleColor(ImGuiCol.Text,Muted);
            try{ImGui.Selectable(localization.Format("DiscoveriesUi.SeenHere",MapName(map),MapProjection.CoordinateNumber(place.X),MapProjection.CoordinateNumber(place.Y),level)+$"##Seen{key}{index}");}
            finally{ImGui.PopStyleColor();}
            Tip("DiscoveriesUi.SeenHereTip");
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
                // 0.911 (X-76): and every other kind (OtherSeenKinds).
                ..ItemSourceKinds.Concat(OtherSeenKinds).SelectMany(kind=>store.Find(kind,_=>true,20_000)).Where(fact=>DiscoveryFactName(fact).Contains(wanted,StringComparison.CurrentCultureIgnoreCase)).Take(200)];
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
                    ImGui.Selectable(localization.Format("DiscoveriesUi.Coordinates",MapProjection.CoordinateNumber(place.X),MapProjection.CoordinateNumber(place.Y))+$"##DiscoveryPlace{index}");
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
                    Tip("DiscoveriesUi.PlaceTip");
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
        if(ImGui.SmallButton(localization.Get("DiscoveriesUi.ClearYes")))
        {
            // X-76 step 3 (0.945, user request): a zip copy of the Discoveries folder first (never the GitHub token nor its refresh token, X-175), then the base emptied.
            try{DiscoveryDatabase?.Checkpoint();BackupZip.Create(folder,Path.Combine(Path.GetDirectoryName(folder) ?? folder,"Backups"),"discoveries",DateTimeOffset.UtcNow,GitHubExchange.SecretFiles);}
            catch(Exception error){log.Warning(error,"The discoveries copy before emptying failed; nothing was removed.");discoveryClearAsked=false;return;}
            store.Clear();SaveDiscoveries(false);discoveryClearAsked=false;
        }
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("DiscoveriesUi.ClearNo")))discoveryClearAsked=false;
    }
}
