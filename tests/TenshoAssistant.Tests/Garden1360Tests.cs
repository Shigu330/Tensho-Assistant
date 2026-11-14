// X-382 and X-383 (0.1360, user reports of 14/11, Shirogane 2-17, patch 1): a harvest read from the bed's menu just before its gain lines
// (dalamud.log of 14/11 23:48: « Garden menu read … bed 6 », then « Vous obtenez 9 carottes de Krakka » 1.5 s later, the target no longer
// the bed), credited to the garden; and a bed's crossbreed fixed at its planting (docs/evidence/0.456-garden-grid.md), whatever its
// neighbours do afterwards.
using TenshoAssistant.Discoveries;
using TenshoAssistant.Reminders;

internal static class Garden1360Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private const string Here="Shirogane 2-17";
    private static string Place(int bed)=>$"{Here} · potager 1, emplacement {bed}";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden 1360: the bed's menu read, then its gain lines without the bed targeted: one harvest of that bed, credited to the garden",() =>
        {
            var t=new DateTimeOffset(2026,11,14,22,48,25,TimeSpan.Zero);
            var menu=new GardenMenuMemory();
            var beds=new List<GardenBed>();
            foreach(var number in new[]{2,4,6,8})beds.Add(GardenJournal.Plant(Place(number),"Graines de carotte de Krakka",72,0,t.AddDays(-4),8182));
            var harvested=new List<(int Patch,int Bed)?>();
            var last=DateTimeOffset.MinValue;(int,int)? lastSlot=null;
            var gains=new PendingGains<string>();
            // What the watcher does with each chat line of type 62 without a sender, in housing (GardenChatWatcher.OnMessage).
            void Line(DateTimeOffset at,uint target)
            {
                if(!GardenChat.IsHarvest(62,"",target,menu.Peek(Here,at,GardenMenuMemory.HarvestWindow) is not null))return;
                var slot=menu.Take(Here,at);
                if(!GardenHarvests.IsNew(at,last,slot,lastSlot))return;
                last=at;lastSlot=slot;harvested.Add(slot);
                var bed=GardenJournal.CareTargets(beds,Here,slot).Where(row=>GardenJournal.RipeAt(row)<=at).Single();
                beds.Remove(bed);
                gains.Source(GainSource.Garden,(uint)(slot?.Bed ?? 0),at.ToUnixTimeMilliseconds());
            }
            // Bed 6: menu, then the crop and the seeds 1.5 s later (target 0: no longer the bed).
            menu.Seen(1,6,Here,t);
            Line(t.AddMilliseconds(1_554),0);Line(t.AddMilliseconds(1_572),0);
            gains.Add("8165x9, 7728x1",t.AddMilliseconds(1_600).ToUnixTimeMilliseconds());
            Check(harvested.SequenceEqual([(1,6)]),"one harvest of bed 6 for its two gain lines");
            Check(beds.Count==3 && beds.All(bed=>!bed.Place.EndsWith(" 6",StringComparison.Ordinal)),"bed 6 left the journal");
            var settled=gains.Settle(t.AddSeconds(3).ToUnixTimeMilliseconds());
            Check(settled.Explained.Single() is ("8165x9, 7728x1",GainSource.Garden,6u) && settled.Alone.Count==0,"its items credited to the garden, not « not attributed »");
            // Bed 8, 9 s later: a new harvest, then bed 2 with the bed still targeted (the rule of 0.978 stays).
            menu.Seen(1,8,Here,t.AddSeconds(9));Line(t.AddSeconds(10.3),0);
            menu.Seen(1,2,Here,t.AddSeconds(16));Line(t.AddSeconds(16.7),GardenChat.BedObject);
            Check(harvested.SequenceEqual([(1,6),(1,8),(1,2)]) && beds.Count==1,"beds 8 and 2 harvested in turn");
            // No menu (or too old, or another place) and no bed targeted: an ordinary gain, no harvest.
            menu.Seen(1,4,Here,t.AddSeconds(30));
            Line(t.AddSeconds(41),0);
            Check(harvested.Count==3 && beds.Count==1,"a gain 11 s after the menu is no harvest");
            menu.Seen(1,4,"Brumée 3-4",t.AddSeconds(50));
            Line(t.AddSeconds(51),0);
            Check(harvested.Count==3,"a menu read at another place is no harvest here");
            Check(!GardenChat.IsHarvest(62,"Someone",0,true) && !GardenChat.IsHarvest(10,"",0,true),"a player's line, or another chat type, never");
        }),
        ("Garden 1360: a bed's crossbreed is the one of its planting; removing or replanting a neighbour later changes nothing",() =>
        {
            var data=new GardenCrossData([new(100,10,20)],[]);
            var pairs=GardenForecasts.Index(data);
            bool Known(uint seed)=>data.Seeds.Contains(seed);
            var t=new DateTimeOffset(2026,11,11,19,29,38,TimeSpan.Zero);
            // The plan of the wiki: apples (10) and carrots (20) alternate around the ring, bed 1 first.
            List<GardenBed> beds=[];
            for(var number=1;number<=8;number++)
                beds=GardenJournal.Detected(beds,Place(number),number%2==1 ? "Apple":"Carrot",number%2==1 ? 120:72,0,t.AddSeconds(number*10),number%2==1 ? 10u:20u)!;
            IReadOnlyList<GardenForecast> Forecasts()=>GardenForecasts.For(beds.Select(bed=>new GardenForecastBed(GardenJournal.Slot(bed)!.Bed,bed.SeedId,bed.PlantedAt,
                bed.NeighboursAtPlanting?.Select(neighbour=>(neighbour.Bed,(uint?)neighbour.SeedId,neighbour.Seed)).ToArray())).ToArray(),pairs,Known);
            var before=Forecasts();
            Check(before.Single(forecast=>forecast.Bed==1).Kind==GardenForecastKind.NoPartner,"bed 1, planted first in an empty patch, never crosses (the wiki's rule)");
            Check(before.Single(forecast=>forecast.Bed==3) is { Kind:GardenForecastKind.Crosses,Partner:2,PartnerSeed:20 },"bed 3 crosses with bed 2, there before it");
            Check(beds.Single(bed=>bed.Place==Place(3)).NeighboursAtPlanting is [{ Bed:2,SeedId:20 }],"bed 3 kept bed 2 only (bed 4 was planted after it)");
            // The carrots harvested (or removed by hand), then planted again three days later.
            beds.RemoveAll(bed=>bed.SeedId==20);
            Check(Forecasts().Where(forecast=>forecast.Bed%2==1).Select(forecast=>forecast.Kind).SequenceEqual([GardenForecastKind.NoPartner,GardenForecastKind.Crosses,GardenForecastKind.Crosses,GardenForecastKind.Crosses]),"the apples keep their crossbreed with the carrots gone");
            foreach(var number in new[]{8,6,4,2})beds=GardenJournal.Detected(beds,Place(number),"Carrot",72,0,t.AddDays(3).AddSeconds(number),20)!;
            var after=Forecasts();
            foreach(var number in new[]{3,5,7})
                Check(after.Single(forecast=>forecast.Bed==number) is { Kind:GardenForecastKind.Crosses,PartnerSeed:20,PartnerName:"Carrot" },$"bed {number} still crosses with the carrot of its planting");
            Check(after.Single(forecast=>forecast.Bed==1).Kind==GardenForecastKind.NoPartner,"bed 1 still has none");
            Check(after.Single(forecast=>forecast.Bed==2) is { Kind:GardenForecastKind.Crosses,PartnerSeed:10 },"the new carrot of bed 2 crosses with an apple already there");
        }),
        ("Garden 1360: a bed noted before its neighbours were kept: « not known » when a neighbour may have changed, never « no crossbreed »",() =>
        {
            var data=new GardenCrossData([new(100,10,20)],[]);
            var pairs=GardenForecasts.Index(data);
            var t=new DateTimeOffset(2026,11,11,19,0,0,TimeSpan.Zero);
            // The user's patch of 14/11: apples of 11/11 in 1, 3, 5, 7 (nothing kept), carrots replanted in 2, 4, 6, 8 three days later.
            var forecasts=GardenForecasts.For([new GardenForecastBed(1,10,t,null),new GardenForecastBed(3,10,t.AddSeconds(30),null),new GardenForecastBed(2,20,t.AddDays(3),[]),new GardenForecastBed(4,20,t.AddDays(3),[(3,10,"Apple"),(5,10,"Apple")])],pairs,seed=>seed is 10 or 20);
            Check(forecasts.Single(forecast=>forecast.Bed==1).Kind==GardenForecastKind.NotKept && forecasts.Single(forecast=>forecast.Bed==3).Kind==GardenForecastKind.NotKept,"the old beds: not known");
            Check(forecasts.Single(forecast=>forecast.Bed==2).Kind==GardenForecastKind.NoPartner,"a bed planted with no neighbour kept: none");
            Check(forecasts.Single(forecast=>forecast.Bed==4) is { Kind:GardenForecastKind.Crosses,Partner:5,PartnerName:"Apple" },"bed 4 (down then up): bed 5 first");
            var legacy=GardenForecasts.For([(1,10u,t),(2,20u,t.AddMinutes(1))],pairs,seed=>seed is 10 or 20);
            Check(legacy[0].Kind==GardenForecastKind.NoPartner && legacy[1].Kind==GardenForecastKind.Crosses,"the reading of the journal alone is unchanged");
            var en=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));var fr=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(en.Contains("\"ForecastNotKept\"") && fr.Contains("\"ForecastNotKept\""),"the sentence in both languages");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GardenView.cs"));var crosses=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GardenCrosses.cs"));
            Check(view.Contains("case GardenForecastKind.NotKept:") && crosses.Contains("case GardenForecastKind.NotKept:") && crosses.Contains("entry.Bed.NeighboursAtPlanting?.Select("),"drawn in both views, from the neighbours kept");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("itemWatcher?.SourceSeen(Discoveries.GainSource.Garden,"),"the harvest handed to the item sources");
        }),
    ];
}
