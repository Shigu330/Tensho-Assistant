using System.Text.Json;
using TenshoAssistant.Caching;
using TenshoAssistant.Gathering;

internal static class GatheringCatalogCacheTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static T Wait<T>(Task<T> task)=>task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
    private const string Game="ffxiv=2026.10.01;ex1=2026.10.01";
    private static readonly GatheringSchedule Rare=GatheringSchedule.FromRare([(200,200),(1400,200)]);
    private static GatheringSource[] Sources()=>
    [
        new(5111,30,40,2,"Minage",60438,50,139,"Noscea","Mine",15,false,false,Rare),
        new(5112,31,41,3,"Coupe",60433,10,140,"Forêt","Bois",16,false,true,GatheringSchedule.FromEphemeral(ushort.MaxValue,ushort.MaxValue)),
        new(4869,250,250,100,"Pêcheur",62118,5,134,"Limsa","Quai",11,true),
    ];
    private static FishingNotebookEntry[] Fishing()=>[new(1,4869,250,"Un poisson","Légende",true,false,null,true,true)];
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Gathering schedules persist exactly and malformed stored windows are refused",()=>
        {
            foreach(var schedule in new[]{Rare,GatheringSchedule.Unknown,GatheringSchedule.FromEphemeral(ushort.MaxValue,ushort.MaxValue),GatheringSchedule.FromEphemeral(2400,2400),
                GatheringSchedule.FromEphemeral(9999,1),GatheringSchedule.FromRare([]),GatheringSchedule.FromEphemeral(2200,200)})
            {
                var copy=JsonSerializer.Deserialize<GatheringSchedule>(JsonSerializer.Serialize(schedule))!;
                Check(copy.Kind==schedule.Kind && copy.Key==schedule.Key && copy.Label==schedule.Label && copy.Windows.SequenceEqual(schedule.Windows),"schedule roundtrip "+schedule.Key);
            }
            Check(ReferenceEquals(JsonSerializer.Deserialize<GatheringSchedule>(JsonSerializer.Serialize(GatheringSchedule.Unknown)),GatheringSchedule.Unknown),"unknown stays the shared instance");
            Check(GatheringSchedule.FromStored(GatheringScheduleKind.Limited,[new(0,1440)]) is null && GatheringSchedule.FromStored(GatheringScheduleKind.Limited,[new(100,50)]) is null
                && GatheringSchedule.FromStored(GatheringScheduleKind.Limited,[new(200,400),new(100,300)]) is null && GatheringSchedule.FromStored(GatheringScheduleKind.Limited,[]) is null
                && GatheringSchedule.FromStored(GatheringScheduleKind.Unknown,[new(0,60)]) is null && GatheringSchedule.FromStored((GatheringScheduleKind)42,[]) is null,"non-normalized or inconsistent windows refused");
            var refused=false;
            try{JsonSerializer.Deserialize<GatheringSchedule>("{\"Kind\":2,\"Windows\":[{\"StartMinute\":-5,\"EndMinute\":10}]}");}catch(JsonException){refused=true;}
            Check(refused,"bad JSON schedule throws instead of inventing a window");
            Check(!JsonSerializer.Serialize(Sources()[0]).Contains("Timing",StringComparison.Ordinal),"computed fallback not persisted");
        }),
        ("Gathering cache roundtrips sources schedules and fishing notes and rebuilds the catalogue",()=>
        {
            var root=Path.Combine(Path.GetTempPath(),"Tensho-GatheringCacheTests-"+Guid.NewGuid().ToString("N"));
            try
            {
                using(var cache=new DataCache(root))
                    Check(Wait(cache.WriteAsync(GatheringCatalogCache.Family,"French",GatheringCatalogCache.Version(Game),GatheringCatalogCache.Snapshot(Game,Sources(),Fishing()),DateTimeOffset.UtcNow)),"written");
                using var reloaded=new DataCache(root);
                var value=Wait(reloaded.ReadAsync<GatheringCatalogCacheData>(GatheringCatalogCache.Family,"French",GatheringCatalogCache.Version(Game)))?.Value;
                Check(GatheringCatalogCache.Valid(value,Game),"restored snapshot valid");
                var catalog=new GatheringCatalog(value!.Sources,Game,default,value.Fishing);
                var mining=catalog.ForItem(5111).Single();
                Check(mining.Timing.Key==Rare.Key && mining.Schedule!.Windows.SequenceEqual(Rare.Windows) && catalog.ForItem(5112).Single().IsHidden,"schedule and flags kept");
                Check(catalog.ForFishingSpot(250)?.ItemId==4869 && catalog.ForItem(4869).Single().Schedule is null && catalog.Fishing.ForItem(4869).Single()==Fishing()[0],"fishing rows and notebook kept");
                Check(catalog.Count==3 && catalog.Source==Game,"same count and source as a game build");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }),
        ("Gathering cache key follows client version and shape and null rows are refused",()=>
        {
            Check(GatheringCatalogCache.Version(Game)!=GatheringCatalogCache.Version("ffxiv=2026.11.15") && GatheringCatalogCache.Shape.Contains("GatheringSchedule(",StringComparison.Ordinal)
                && GatheringCatalogCache.Shape.Contains("FishingNotebookEntry(",StringComparison.Ordinal),"key carries game version and shapes");
            var good=GatheringCatalogCache.Snapshot(Game,Sources(),Fishing());
            Check(GatheringCatalogCache.Valid(good,Game) && !GatheringCatalogCache.Valid(good,"other") && !GatheringCatalogCache.Valid(null,Game),"identity rechecked");
            foreach(var bad in new[]{good with{Sources=[]},good with{Sources=null!},good with{Fishing=null!},good with{Sources=[null!]},good with{Sources=[Sources()[0] with{Zone=null!}]},
                good with{Fishing=[null!]},good with{Fishing=[Fishing()[0] with{Folklore=null!}]}})
                Check(!GatheringCatalogCache.Valid(bad,Game),"invalid gathering snapshot refused");
        }),
    };
}
