using TenshoAssistant.Carnivale;
using TenshoAssistant.Deliveries;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.GrandCompanies;
using TenshoAssistant.Relics;
using TenshoAssistant.Societies;

namespace TenshoAssistant;

// 0.722: the pages coded without the game read their sheets and the game's data here too, once per load, in a self-check (developer mode, or on
// request): the same reads as opening the page, off the frame or on the framework thread as there. Only the numbers are kept: docs/evidence/0.722-selfcheck-pages.md.
internal sealed partial class MainWindow
{
    private IReadOnlyList<(string Domain,Task<SelfCheckLine> Read)>? selfCheckPages;
    // X-433 (0.1451): what each page's read allocated on its thread, said once they are all read (developer mode's cost, not a player's).
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string,long> selfCheckAllocated=new(StringComparer.Ordinal);
    /// <summary>X-92 (0.972): the game data check of this load (null result: failed or cancelled).</summary>
    internal Func<Task<GameDataReport?>>? GameDataCheck { get; set; }

    private void StartSelfCheckPages()
    {
        if(selfCheckPages is not null)return;
        (string,Task<SelfCheckLine>) Page(string domain,Func<Task<SelfCheckLine>> page)
        {
            // X-433 (0.1451): measured on the thread that reads, until the read's first wait.
            Task<SelfCheckLine> read()
            {
                var before=GC.GetAllocatedBytesForCurrentThread();
                try{return page();}
                finally{selfCheckAllocated[domain]=GC.GetAllocatedBytesForCurrentThread()-before;}
            }
            return (domain,Task.Run(read));
        }
        selfCheckPages=
        [
            Page("Eureka and Bozja",()=>Task.FromResult(FieldCatalogLoad?.Invoke() is { } field && field.Zones.Sum(zone=>zone.Events.Count)>0
                ? Diagnostics.SelfCheck.Ok("Eureka and Bozja",$"{string.Join("+",field.Zones.Select(zone=>zone.Events.Count))} places, {field.Records.Count} records, {field.Actions.Count} actions, {field.OccultJobs.Count} phantom jobs")
                :Diagnostics.SelfCheck.Failed("Eureka and Bozja","nothing read"))),
            // X-205 (0.1241): the community table of Eureka's notorious monsters against the game's files (R06): each FATE among its zone's
            // FATEs, each weather among its zone's weathers.
            Page("Eureka notorious monsters",()=>Task.FromResult(FieldCatalogLoad?.Invoke() is not { } field || weather.State.Catalog is not { } weathers
                ? Diagnostics.SelfCheck.NotRead("Eureka notorious monsters","game files or weather catalogue not read yet")
                :Fields.EurekaNotoriousBoard.Mismatches((territory,fate)=>field.Zones.Any(zone=>zone.Territory==territory && zone.Events.Any(row=>row.Kind==Fields.FieldEventKind.Fate && row.Id==fate)),
                    (territory,id)=>weathers.Zone(territory)?.Shares.Any(share=>share.WeatherId==id)==true) is { Count:>0 } wrong
                    ? Diagnostics.SelfCheck.Suspect("Eureka notorious monsters",string.Join("; ",wrong))
                    :EurekaWindowsCheck(weathers))),
            // X-322 (0.1320): the game's own map list against the unlock bits of the fixed icons (MapLiveReader.CheckMarkerUnlocks).
            Page("map icon unlocks",()=>Task.FromResult(mapLive?.MarkerUnlockCheck is not { } unlockCheck
                ? Diagnostics.SelfCheck.NotRead("map icon unlocks","no game map with unlock icons built yet")
                :unlockCheck.Agree ? Diagnostics.SelfCheck.Ok("map icon unlocks",unlockCheck.Text):Diagnostics.SelfCheck.Suspect("map icon unlocks",unlockCheck.Text))),
            Page("FATEs",()=>Task.FromResult(FatesLoad?.Invoke() is { Count:>0 } zones
                ? Diagnostics.SelfCheck.Ok("FATEs",$"{zones.Count} zones, {zones.Sum(zone=>zone.Events.Count)} FATEs")
                :Diagnostics.SelfCheck.Failed("FATEs","nothing read"))),
            // X-213 (0.1781): the shared FATE zones and their gemstone traders' items by rank, from the sheets.
            Page("shared FATEs",()=>Task.FromResult(SharedFatesLoad?.Invoke() is { Count:>0 } sharedZones
                ? Diagnostics.SelfCheck.Ok("shared FATEs",$"{sharedZones.Count} zones, {sharedZones.Count(zone=>zone.TraderKnown)} with a trader, {sharedZones.Sum(zone=>zone.Rewards.Count)} items, highest ranks {string.Join("/",sharedZones.Select(zone=>zone.MaxRank).Distinct())}")
                :Diagnostics.SelfCheck.Failed("shared FATEs","nothing read"))),
            Page("deep dungeons",()=>Task.FromResult(DeepDungeonLoad?.Invoke() is { Count:>0 } dungeons
                ? (dungeons.SelectMany(dungeon=>dungeon.Pomanders.Concat(dungeon.Others)).Any(thing=>thing.Tooltip.Contains('<'))
                    ? Diagnostics.SelfCheck.Suspect("deep dungeons","a text kept a macro")
                    :Diagnostics.SelfCheck.Ok("deep dungeons",string.Join(", ",dungeons.Select(dungeon=>$"{dungeon.Pomanders.Count}+{dungeon.Others.Count}"))+" items"))
                :Diagnostics.SelfCheck.Failed("deep dungeons","nothing read"))),
            Page("game data",async ()=>
            {
                if(GameDataCheck is null || await GameDataCheck() is not { } report)return Diagnostics.SelfCheck.Failed("game data","check not run");
                var (needsLook,detail)=GameDataChanges.Summary(report);
                return needsLook ? Diagnostics.SelfCheck.Suspect("game data",detail):Diagnostics.SelfCheck.Ok("game data",detail);
            }),
            // X-606 (0.1710): the quest list's givers, places, end NPCs and jobs from the sheets (the probe of 0.1708 found 5265 givers named of 5374).
            Page("quest givers",()=>Task.FromResult(quests.State.Entries is not { Count:>0 } read
                ? Diagnostics.SelfCheck.NotRead("quest givers","quest list not read yet")
                :read.Count(entry=>entry.IssuerName.Length>0) is var named && named*2<read.Count
                    ? Diagnostics.SelfCheck.Suspect("quest givers",$"{named} givers named of {read.Count} quests")
                    :Diagnostics.SelfCheck.Ok("quest givers",$"{read.Count} quests: {named} givers named, {read.Count(TenshoAssistant.Quests.QuestPlaces.HasGiverPlace)} placed, {read.Count(entry=>entry.EndName.Length>0)} end NPCs, {read.Count(entry=>entry.Jobs.Length>0)} with jobs, {read.Count(entry=>entry.Society.Length>0)} allied society"))),
            // X-746 (0.1766) and X-745 (0.1767): the Beastmaster's data as read in the game (worked out off the frame at load: waited for, ten
            // seconds at most): one action Borrow gives per family of the beasts, the beasts caught in a zone or a duty.
            Page("beastmaster borrow and items",async ()=>
            {
                for(var wait=0;wait<100 && Beasts?.State is { Catalog:null,Error:null };wait++)await Task.Delay(100);
                if(Beasts?.State is not { Catalog:{ } beasts })return Diagnostics.SelfCheck.NotRead("beastmaster borrow and items",Beasts?.State.Error ?? "Beastmaster data not read yet");
                var families=beasts.Beasts.Select(beast=>beast.FamilyId).Where(family=>family>0).Distinct().Count();
                var text=$"{beasts.Beasts.Count} beasts, {beasts.Boards.Count} boards, {beasts.Borrows.Count} Borrow actions for {families} families, "
                    +$"{beasts.Beasts.Count(beast=>beast.CatchKind==TenshoAssistant.Beastmaster.BeastCapture.InZone)} caught in a zone, {beasts.Beasts.Count(beast=>beast.CatchKind==TenshoAssistant.Beastmaster.BeastCapture.InDuty)} in a duty";
                // X-744 (0.1768): the ephemeral items, the feeds whose families were read, the guide's rules.
                var feeds=beasts.Items.Where(item=>item.Kind==TenshoAssistant.Beastmaster.BeastItems.Feed).ToArray();
                var unread=feeds.Count(feed=>feed.Families.Count==0);
                text+=$"; {beasts.Items.Count} ephemeral items ({string.Join("+",beasts.ItemKinds.OrderBy(kind=>kind.Key).Select(kind=>beasts.Items.Count(item=>item.Kind==kind.Key)))}), {feeds.Length-unread} of {feeds.Length} feeds with their families, {beasts.Rules.Count} guide rules";
                // X-744 (0.1770): the boards' spaces that are not fights, the campsites' places, the random spaces' options, the satiety.
                var spaces=beasts.Spaces.Values.SelectMany(list=>list).ToArray();
                int Count(int kind)=>spaces.Count(space=>space.Kind==kind);
                var camps=spaces.Where(space=>space.Kind==TenshoAssistant.Beastmaster.BeastSpaces.Campsite).ToArray();
                text+=$"; {beasts.Spaces.Count} boards with {spaces.Length} spaces ({Count(TenshoAssistant.Beastmaster.BeastSpaces.Shop)} shops, {camps.Length} campsites with {camps.Count(camp=>camp.Places>0)} places read, "
                    +$"{Count(TenshoAssistant.Beastmaster.BeastSpaces.Treasure)} treasures, {Count(TenshoAssistant.Beastmaster.BeastSpaces.Random)} random with {spaces.Count(space=>space.Options.Count>0)} read), {beasts.Beasts.Count(beast=>beast.Satiety>0)} beasts with a satiety";
                var spacesBad=beasts.Spaces.Count==0 || camps.Any(camp=>camp.Places==0) || spaces.Any(space=>space.Kind==TenshoAssistant.Beastmaster.BeastSpaces.Random && space.Options.Count==0) || beasts.Beasts.Any(beast=>beast.Satiety==0);
                return beasts.Borrows.Count!=families || unread>0 || beasts.Items.Count==0 || spacesBad ? Diagnostics.SelfCheck.Suspect("beastmaster borrow and items",text):Diagnostics.SelfCheck.Ok("beastmaster borrow and items",text);
            }),
            Page("lexicon",()=>Task.FromResult(LexiconLoad?.Invoke() is { Count:>0 } words
                ? Diagnostics.SelfCheck.Ok("lexicon",$"{words.Count} words, {words.Select(word=>word.Kind).Distinct().Count()} kinds")
                :Diagnostics.SelfCheck.Failed("lexicon","nothing read"))),
            Page("relics",async ()=>
            {
                if(RelicCatalogLoad?.Invoke() is not { } catalog)return Diagnostics.SelfCheck.Failed("relics","sheets unreadable");
                var armour=catalog.Armours?.Values.Sum(sets=>sets.Count) ?? 0;
                var whole=catalog.Armours?.Values.SelectMany(sets=>sets).SelectMany(set=>set.Stages).Where(stage=>stage.Count>0).All(stage=>stage.Count==5)==true;
                var note=ReadRelics is null ? null:await ReadRelics();
                var text=$"{catalog.Lines.Count} zodiac lines, {catalog.Families?.Count ?? 0} families, {armour} armour sets, {note?.Status}";
                return armour>0 && !whole ? Diagnostics.SelfCheck.Suspect("relics",text+", a stage without five pieces"):Diagnostics.SelfCheck.Ok("relics",text);
            }),
            Page("allied societies",async ()=>
            {
                if(AlliedCatalogLoad?.Invoke() is not { } catalog)return Diagnostics.SelfCheck.Failed("allied societies","sheets unreadable");
                var read=ReadAllied is null ? null:await ReadAllied(catalog);
                return read?.Status==AlliedReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("allied societies",$"{catalog.Societies.Count} societies, {read.Standings.Count} read"):Diagnostics.SelfCheck.NotRead("allied societies",read?.Status.ToString() ?? "no reader");
            }),
            Page("grand companies",async ()=>
            {
                if(GcCatalogLoad?.Invoke(false) is not { } catalog)return Diagnostics.SelfCheck.Failed("grand companies","sheets unreadable");
                var read=ReadGc is null ? null:await ReadGc(catalog.Ranks.Count+1);
                return read?.Status==GcReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("grand companies",$"{catalog.Ranks.Count} ranks, {read.Seals.Count} companies"):Diagnostics.SelfCheck.NotRead("grand companies",read?.Status.ToString() ?? "no reader");
            }),
            Page("custom deliveries",async ()=>
            {
                if(DeliveryClientsLoad?.Invoke() is not { Count:>0 } clients)return Diagnostics.SelfCheck.Failed("custom deliveries","sheets unreadable");
                var read=ReadDeliveries is null ? null:await ReadDeliveries(clients);
                return read?.Status==DeliveryReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("custom deliveries",$"{clients.Count} clients, {read.Remaining} left, {clients.Sum(client=>client.Rewards?.Count ?? 0)} reward lines, {clients.Sum(client=>client.Gifts?.Count ?? 0)} gifts"):Diagnostics.SelfCheck.NotRead("custom deliveries",read?.Status.ToString() ?? "no reader");
            }),
            Page("masked carnivale",async ()=>
            {
                if(CarnivaleStagesLoad?.Invoke() is not { Count:>0 } stages)return Diagnostics.SelfCheck.Failed("masked carnivale","sheets unreadable");
                var read=ReadCarnivale is null ? null:await ReadCarnivale(stages);
                return read?.Status==CarnivaleReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("masked carnivale",$"{stages.Count} stages, {read.Completed.Count(done=>done)} done"):Diagnostics.SelfCheck.NotRead("masked carnivale",read?.Status.ToString() ?? "no reader");
            }),
        ];
        _=Task.WhenAll(selfCheckPages.Select(page=>page.Read)).ContinueWith(_=>log.Information(Diagnostics.SelfCheck.PagesAllocated(selfCheckAllocated)),TaskScheduler.Default);
    }

    // X-323 (0.1318): the windows the page shows, computed from the catalogue read in the game (each night monster has one within 70 minutes).
    private static SelfCheckLine EurekaWindowsCheck(Gathering.WeatherCatalog weathers)
    {
        var (withWindow,wrong)=Fields.EurekaNotoriousBoard.ForecastCheck(weathers,DateTimeOffset.UtcNow);
        var count=Fields.EurekaNotoriousBoard.All.Count;
        return wrong.Count>0 ? Diagnostics.SelfCheck.Suspect("Eureka notorious monsters",string.Join("; ",wrong))
            :Diagnostics.SelfCheck.Ok("Eureka notorious monsters",$"{count} monsters, their FATEs and weathers found in the game's files; {withWindow} of {count} with a window in the next 28 hours");
    }

    private IEnumerable<(string Domain,Func<SelfCheckLine> Probe)> SelfCheckPageProbes()=>(selfCheckPages ?? []).Select(page=>(page.Domain,(Func<SelfCheckLine>)(()=>
        !page.Read.IsCompleted ? Diagnostics.SelfCheck.NotRead(page.Domain,"still reading")
        :page.Read.IsCompletedSuccessfully ? page.Read.Result
        :Diagnostics.SelfCheck.Failed(page.Domain,page.Read.Exception?.GetBaseException().Message))));
}
