// Test audit of 2026-11-09, batch 2 (0.1021): FishBite887Tests, FishCollectable982Tests, FishRestrictions859Tests, Fishing877Tests, FishingBait398Tests, FishingCast1011Tests, FishingFacts933Tests, FishingHelp604Tests, FishingHoleKind960Tests merged into FishingWiringTests; every case is kept.
using TenshoAssistant.Discoveries;
using TenshoAssistant.Gathering;
internal static class FishingWiringTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool HasCode(string text,string code)=>System.Text.RegularExpressions.Regex.Replace(text,@"\s+","").Contains(System.Text.RegularExpressions.Regex.Replace(code,@"\s+",""),StringComparison.Ordinal);
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From FishBite887Tests: 0.887 (X-73.18): bite strength and bite time from the cast, proved by the in-game capture (docs/evidence/0.887-fishing-bite.md).
        ("Fish bite: 292/293/294 are ! / !! / !!!, measured from CastingOut, Chum casts left out, kept with the place",() =>
        {
            Check(ItemGains.BiteStrength(292)==1 && ItemGains.BiteStrength(293)==2 && ItemGains.BiteStrength(294)==3 && ItemGains.BiteStrength(278)==0,"strengths");
            Check(DiscoveryStore.HasPlace(DiscoveryKind.FishBite),"place");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(state==1){castStarted=now;bitAt=-100_000;castChum=me.StatusList.Any(status=>status.StatusId==763);}"),"cast start, chum");
            Check(watcher.Contains("if(bitAt>castStarted && castStarted>0 && !castChum"),"measured only after a cast, without chum");
        }),
        // From FishCollectable982Tests: X-97 (0.982): the fishing helper shows a collectable fish's tiers and scrips, from the same rules as the gathering panel.
        ("Fish collectable 982: tiers and scrips under the fish, scrip exchanges only",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs"));
            Check(panel.Contains("Collectables?.Catalog?.For(row.FishId) is { Count:>0 } collectable && collectable.FirstOrDefault(rule=>rule.Scrips) is { } scrip"),"rules");
            Check(panel.Contains("\"FishingPanel.CollectableTip\""),"scrips in the tooltip");
        }),
        // From FishRestrictions859Tests: 0.859 (X-73.3): the game's fishing log flags first; a community window or weather is left out when the game says there is none.
        ("Fish restrictions: the game's « none » hides the community window and weather; unknown keeps them",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs"));
            Check(panel.Contains("if(timeLimited!=false && known.StartMinute is { } start && known.EndMinute is { } stop)"),"window hidden only on « none »");
            Check(panel.Contains("if(weatherLimited!=false && known.Weathers.Count>0)") && panel.Contains("if(weatherLimited!=false && found.Weather is { Count:>0 } names)"),"weather, both sources");
            Check(panel.Contains("return list.All(value=>value==false) ? false:list.All(value=>value==true) ? true:null;"),"several rows must agree");
        }),
        // From Fishing877Tests: 0.877 (X-75.3): mooch put down to the fish caught before, lures and the previous weather kept, seen lines in the fishing panel.
        ("Fishing: mooch flag 16 uses the last fish, lures 1 and 2 kept, previous weather from the forecast; the panel shows what was seen",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("var bait=(flags & 16)!=0 && lastFish!=0 ? lastFish:") && watcher.Contains("lastFish=item;"),"mooch");
            Check(watcher.Contains("if((flags & 3) is 1 or 2)store.SeeSource(DiscoveryKind.FishLure,"),"lure");
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.FishTransition,item,before,EorzeaMinute(at),at,territory);"),"previous weather");
            Check(!DiscoveryStore.HasPlace(DiscoveryKind.FishLure) && !DiscoveryStore.HasPlace(DiscoveryKind.FishTransition),"no place");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs")).Contains("if(SeenFishLine(fishId) is { } seenLine)"),"panel");
        }),
        // From FishingBait398Tests: 0.398 (g, R-04): bait in hand — read on the framework thread for fishers only, shown with its stock, matching fish framed.
        ("Fishing bait: framework thread, fishers only, shown with stock and matching fish",() =>
        {
            var reader = Read("Gathering/FishingBaitReader.Dalamud.cs");
            Check(reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);",StringComparison.Ordinal) && reader.Contains("ClassJob.RowId==18 && state!=null && state->IsLoaded ? state->FishingBait:-1",StringComparison.Ordinal),"read");
            var panel = Read("MainWindow.FishingPanel.cs");
            Check(panel.Contains("rows.Count(row=>row.Route.Count>0 && row.Route[0]==held)",StringComparison.Ordinal) && panel.Contains("inHand==row.Route[0]",StringComparison.Ordinal),"shown");
        }),
        // From FishingCast1011Tests: C10 (0.1011): the fishing stopwatch restarts at each cast, from the line's CastingOut state read on the framework thread; the manual button stays.
        ("Fishing cast 1011: a newer cast restarts the stopwatch, an older one or none keeps it",() =>
        {
            DateTimeOffset? Run(DateTimeOffset? since,DateTimeOffset? cast)=>FishingStopwatch.FromCast(since,cast);
            var start=new DateTimeOffset(2026,11,9,12,0,0,TimeSpan.Zero);
            Check(Run(start,start.AddSeconds(20))==start.AddSeconds(20),"newer cast");
            Check(Run(start,start.AddSeconds(-5))==start,"cast before fishing or before a manual restart");
            Check(Run(start,null)==start,"no cast read");
            Check(Run(null,start)==start,"first cast");
        }),
        ("Fishing cast 1011: the line read every frame, kept on change, wired to both stopwatches",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/FishingBaitReader.Dalamud.cs"));
            Check(HasCode(reader,"if(disposed)return; ReadLine();"),"every frame, before the half-second throttle");
            Check(reader.Contains("if(state==lineState)return;") && reader.Contains("if(state==1)Volatile.Write(ref castAt"),"CastingOut only, on change");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.FishingCastAt=()=>baitReader?.CastAt;"),"wired");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs")).Contains("fishingSince=FromCast(fishingSince,FishingCastAt?.Invoke());"),"fishing help");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs")).Contains("oceanFishingSince=FromCast(oceanFishingSince,FishingCastAt?.Invoke());"),"ocean fishing");
        }),
        // From FishingFacts933Tests: X-76 P6 (0.933): fishing facts: bite time by bait, Fisher's Intuition start and the catches under it; new kinds known to the merge tool.
        ("Fishing facts 933: new kinds, placed where they should, written by the watcher, merge tool aware",() =>
        {
            Check((int)DiscoveryKind.FishBiteBait==29 && (int)DiscoveryKind.IntuitionStart==30 && (int)DiscoveryKind.IntuitionCatch==31,"numbers");
            Check(DiscoveryStore.HasPlace(DiscoveryKind.FishBiteBait) && DiscoveryStore.HasPlace(DiscoveryKind.IntuitionStart) && !DiscoveryStore.HasPlace(DiscoveryKind.IntuitionCatch),"places");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.FishBiteBait,item,bait,") && watcher.Contains("store.SeeSource(DiscoveryKind.IntuitionStart,trigger,0,intuition.Seconds,")
                && watcher.Contains("store.SeeSource(DiscoveryKind.IntuitionCatch,item,running.Trigger,1,at,territory);"),"written");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("PLACED={1,2,3,8,10,19,22,23,28,29,30}"),"merge tool");
        }),
        // From FishingHelp604Tests: 0.604 (C35): the fishing help reads nothing in the frame.
        ("Fishing help: the player copied on the framework thread, the tables off the frame",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,copyFishingNow)") && plugin.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,copyFishingNow)"),"copied, with its clean-up");
            Check(plugin.Contains("mainWindow.ConfigureFishingHelp(()=>Volatile.Read(ref fishingNowCopy).Value,"),"the frame reads the copy");
            Check(plugin.Contains("fishingSpotsTask??=Task.Run(") && plugin.Contains("bountifulBases??=Task.Run("),"both tables on a worker");
            Check(plugin.Contains("if(!fishingSpotsTask.IsCompletedSuccessfully)return null;") && plugin.Contains("bountifulBases.IsCompletedSuccessfully &&"),"nothing until they are ready");
        }),
        // From FishingHoleKind960Tests: X-94 (0.960): a fishing spot is never looked up as a gathering point (kind, tome, level).
        ("Fishing hole 960: kind and tome read for gathering points only",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("private static bool IsGatheringPointBase(MapLocalGatheringPoint point)=>point.TypeId!=100;"),"type 100 is a fishing spot");
            Check(map.Contains("var access=PointAccess(point);") && map.Contains("if(IsGatheringPointBase(point) && GatherKind?.Invoke(point.BaseId)"),"kind and access guarded");
            // 0.1019 (test audit #0): every GatherAccess call outside PointAccess is counted; since 0.1041 (X-112) none is allowed.
            Check(map.Contains("private TenshoAssistant.Gathering.GatherPointAccess? PointAccess(MapLocalGatheringPoint point)=>IsGatheringPointBase(point) ? GatherAccess?.Invoke(point.BaseId):null;"),"PointAccess guards the read");
            // X-112 (0.1041): the map-edge arrows of timed points go through PointAccess too (no named exception left).
            Check(map.Contains("point.ActiveAt(minutes) && PointAccess(point)?.TomeMissing is null"),"edge arrows read access through PointAccess");
            var unguarded=System.Text.RegularExpressions.Regex.Matches(map,@"GatherAccess\?\.Invoke\(").Count
                -System.Text.RegularExpressions.Regex.Matches(map,@"IsGatheringPointBase\(point\) \? GatherAccess\?\.Invoke\(point\.BaseId\):null").Count;
            Check(unguarded==0,$"one guarded access: {unguarded} unguarded GatherAccess?.Invoke(...) outside PointAccess");
        }),
    ];
}
