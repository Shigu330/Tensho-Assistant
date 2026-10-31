using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Caching;
using TenshoAssistant.Collections;
using TenshoAssistant.Community;
using TenshoAssistant.Craft;
using TenshoAssistant.Duties;
using TenshoAssistant.Fashion;
using TenshoAssistant.Items;
using TenshoAssistant.Market;
using TenshoAssistant.MiniGames;
using TenshoAssistant.Quests;

internal static class CacheTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static T Wait<T>(Task<T> task)=>task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
    private static void Wait(Task task)=>task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
    private sealed class Folder:IDisposable
    {
        public string Root {get;}=Path.Combine(Path.GetTempPath(),"Tensho-CacheTests-"+Guid.NewGuid().ToString("N"));
        public DataCache Cache {get;}
        public Folder()=>Cache=new(Root);
        public void Dispose()
        {
            Wait(Cache.FlushAsync());Cache.Dispose();
            if(!Directory.Exists(Root))return;
            // This exclusively-owned flat directory contains no subdirectories; no recursive deletion.
            foreach(var path in Directory.EnumerateFiles(Root))File.Delete(path);
            Directory.Delete(Root);
        }
    }
    private sealed class Handler(Func<HttpRequestMessage,string> reply):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Interlocked.Increment(ref Calls);return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(reply(request))});}
    }
    private static MarketQuote Quote(DateTimeOffset date)=>new(71,42,date,date,new(10,null,null),new(null,null,null));
    private static string PriceJson=>"""{"results":[{"itemId":42,"nq":{"minListing":{"world":{"price":10}}}}]}""";
    private static CraftActionCatalog Actions()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind+" "+job,1,"Native"))),"fixture");
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Cache policy defaults persistence independent choices and malformed configuration fallback",()=>
        {
            var config=JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
            Check(config.Cache.Duration(CacheArea.Prices)==TimeSpan.FromDays(1) && config.Cache.Duration(CacheArea.Quests)==TimeSpan.FromMinutes(5),"requested price and quest defaults");
            var previous=config.Cache;config.Cache=previous.With(CacheArea.Prices,CacheRetention.SevenDays);
            Check(previous.Prices==CacheRetention.OneDay && config.Cache.Quests==CacheRetention.FiveMinutes,"edit does not mutate previous policy or another tool");
            var restored=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
            Check(restored.Cache.Prices==CacheRetention.SevenDays && !restored.MarketPricesOnHover,"policy persists without enabling HTTP hover");
            restored.Cache.Sources=(CacheRetention)999;Check(restored.Cache.Duration(CacheArea.Sources)==TimeSpan.FromDays(7),"corrupt selection falls back to area default");
        }),
        ("Cache age rejects future or missing dates and expires exactly at the configured boundary",()=>
        {
            var now=DateTimeOffset.UtcNow;var day=TimeSpan.FromDays(1);
            Check(CacheAge.Fresh(now-day+TimeSpan.FromTicks(1),now,day),"inside boundary valid");
            Check(!CacheAge.Fresh(now-day,now,day) && !CacheAge.Fresh(now.AddTicks(1),now,day) && !CacheAge.Fresh(null,now,day),"boundary future and absence never fresh");
            Check(CacheAge.Fresh(now-TimeSpan.FromDays(2),now,TimeSpan.FromDays(7)),"longer configured lifetime applies to existing observation date");
        }),
        ("Disk cache survives a new instance without resetting timestamps or crossing scope version and family",()=>
        {
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddHours(-4);
            Check(Wait(folder.Cache.WriteAsync("prices","71:42","v1",Quote(date),date)),"write successful");
            using var reloaded=new DataCache(folder.Root);
            var restored=Wait(reloaded.ReadAsync<MarketQuote>("prices","71:42","v1"));
            Check(restored is not null && restored.RecordedAt==date && restored.Value.FetchedAt==date && restored.Value.Nq.Minimum==10,"original timestamp and values retained");
            Check(Wait(reloaded.ReadAsync<MarketQuote>("prices","72:42","v1")) is null
                && Wait(reloaded.ReadAsync<MarketQuote>("prices","71:42","v2")) is null
                && Wait(reloaded.ReadAsync<MarketQuote>("other","71:42","v1")) is null,"identity namespaces isolated");
            var invalidated=reloaded.InvalidateAsync("prices","71:42","v1");
            Check(Wait(reloaded.ReadAsync<MarketQuote>("prices","71:42","v1")) is null && Wait(invalidated),"read waits for earlier forced invalidation rather than reviving the old snapshot");
        }),
        ("Disk cache keeps only the newest version of a family and scope, other scopes untouched",()=>
        {
            // 0.145: one quest file per plugin version had filled the 256 MiB cache and evicted the catalogues.
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddHours(-1);
            Check(Wait(folder.Cache.WriteAsync("quests","1","0.120",1,date)) && Wait(folder.Cache.WriteAsync("quests","2","0.120",2,date)),"two characters");
            Check(Wait(folder.Cache.WriteAsync("quests","1","0.144",3,date.AddMinutes(1))),"new version for character 1");
            Check(Directory.GetFiles(folder.Root,"*.json").Length==2,"old version of character 1 removed, character 2 kept");
            Check(Wait(folder.Cache.ReadAsync<int>("quests","1","0.120")) is null && Wait(folder.Cache.ReadAsync<int>("quests","1","0.144"))?.Value==3
                && Wait(folder.Cache.ReadAsync<int>("quests","2","0.120"))?.Value==2,"newest read back");
        }),
        ("Disk cache ordered writes atomic files corruption cancellation and disposal are recoverable",()=>
        {
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddHours(-1);
            var pending=Enumerable.Range(0,10).Select(index=>folder.Cache.WriteAsync("test","same","v1",index,date.AddSeconds(index))).ToArray();
            Wait(Task.WhenAll(pending));Check(Wait(folder.Cache.ReadAsync<int>("test","same","v1"))?.Value==9,"later scheduled snapshot wins even with concurrent workers");
            var path=Directory.GetFiles(folder.Root,"*.json").Single();File.WriteAllText(path,"{broken");
            Check(Wait(folder.Cache.ReadAsync<int>("test","same","v1")) is null && folder.Cache.Error is not null,"broken file is a miss, not a plugin failure");
            using(var stream=new FileStream(path,FileMode.Create))stream.SetLength(DataCache.MaximumEntryBytes+1L);
            Check(Wait(folder.Cache.ReadAsync<int>("test","same","v1")) is null,"oversized file refused before loading");
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();Check(Wait(folder.Cache.ReadAsync<int>("test","same","v1",cancelled.Token)) is null,"cancelled read inert");
            folder.Cache.Dispose();Check(!Wait(folder.Cache.WriteAsync("test","same","v1",5,date)) && Wait(folder.Cache.ReadAsync<int>("test","same","v1")) is null,"disposed store accepts no new operations");
        }),
        ("Quest cache roundtrip preserves reward variants unknowns and observation provenance",()=>
        {
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddHours(-2);
            var reward=new QuestRewardSnapshot(1,5,[new(42,null,QuestRewardRule.Choice,"optional",0)],[new("currency",0,0,"unknown",50)],true,"game-v");
            var entry=new QuestEntry(65537,"Quête","Catégorie",1,QuestProgress.Active,3){Rewards=reward};
            Check(Wait(folder.Cache.WriteAsync("quests","1","FR-game-v",new QuestCacheData(1,new[]{entry}),date)),"snapshot serialized");
            var restored=Wait(folder.Cache.ReadAsync<QuestCacheData>("quests","1","FR-game-v"))!;
            Check(CacheSnapshots.Valid(restored.Value,1) && !CacheSnapshots.Valid(restored.Value,2),"profile guard before publication");
            Check(restored.Value.Entries[0].Rewards is {HasAdditionalRewards:true,DataVersion:"game-v"} copy && copy.Items[0].Quantity is null && copy.Unresolved[0].Quantity==50,"no invented quantity or reward choice during reload");
            using var service=new QuestService();service.Publish(1,restored.Value.Entries,restored.RecordedAt,true);Wait(service.IndexWork);
            Check(service.State.ObservedAt==date && service.State.FromCache && service.State.RewardIndex?.ForItem(42).Count==1,"derived index rebuilt without renewing observation");
            service.Invalidate();Check(service.State.CharacterId==0 && service.State.Entries[0].Progress==QuestProgress.Unconfirmed,"invalidation removes cached progression proof");
        }),
        ("Collection cache retains card metadata loaded-list evidence and isolates owners",()=>
        {
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddDays(-1);
            CollectionEntry[] entries=[new(CollectionKind.TripleTriad,2,"Carte",1,true,Card:new TriadCardDefinition(2,2,1,1,2,3,4,0,"","Description")),
                new(CollectionKind.Title,3,"Titre",1,false,Title:new("Titre","Titre",false,1),NegativeConfirmed:true)];
            Wait(folder.Cache.WriteAsync("collections","1","FR-game-v",new CollectionCacheData(1,entries,[]),date));
            var restored=Wait(folder.Cache.ReadAsync<CollectionCacheData>("collections","1","FR-game-v"))!;
            Check(CacheSnapshots.Valid(restored.Value,1) && !CacheSnapshots.Valid(restored.Value,2) && restored.Value.Entries[0].Card?.Top==1,"card source and owner survive reload");
            var service=new CollectionService();service.Publish(1,restored.Value.Entries,[],date,true);
            Check(service.State.FromCache && service.State.ObservedAt==date && service.State.Entries.Any(row=>row.NegativeConfirmed),"historic loaded proof keeps original date");
            service.Invalidate();Check(service.State.Entries.All(row=>!row.ConfirmedUnlocked && !row.NegativeConfirmed),"foreign or offline context removes both acquisition proofs");
            Check(!CacheSnapshots.Valid(new CollectionCacheData(1,[entries[0],entries[0]],[]),1),"duplicate cached identities rejected");
        }),
        ("Market reload uses persisted prices until policy expiry and manual force bypasses freshness",()=>
        {
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddHours(-2);Wait(folder.Cache.WriteAsync("market","71:42","universalis-aggregated-v2",Quote(date),date));
            using var handler=new Handler(_=>PriceJson);using var client=new HttpClient(handler);using var market=new MarketService(client);
            var ttl=TimeSpan.FromDays(1);market.ConfigureCache(()=>ttl,folder.Cache);market.SetWorld(1,71,"World");Wait(market.RequestAsync(42));
            Check(handler.Calls==0 && market.State.Quote?.FetchedAt==date,"fresh persisted quote reuses original date with no HTTP");
            ttl=TimeSpan.FromMinutes(5);Wait(market.RequestAsync(42));Check(handler.Calls==1 && market.State.Quote?.FetchedAt>date,"shortening policy expires an already loaded quote");
            using var forceHandler=new Handler(_=>PriceJson);using var forceClient=new HttpClient(forceHandler);using var forced=new MarketService(forceClient);
            forced.ConfigureCache(()=>TimeSpan.FromDays(7),folder.Cache);forced.SetWorld(1,71,"World");Wait(forced.RequestAsync(42,true));
            Check(forceHandler.Calls==1,"explicit refresh bypasses disk even when long lifetime would accept it");
            using var otherHandler=new Handler(_=>PriceJson);using var otherClient=new HttpClient(otherHandler);using var other=new MarketService(otherClient);
            other.ConfigureCache(()=>TimeSpan.FromDays(7),folder.Cache);other.SetWorld(1,72,"Other");Wait(other.RequestAsync(42));
            Check(otherHandler.Calls==1 && other.State.Quote?.WorldId==72,"prices are never restored for another world");
        }),
        ("Shared source cache keeps retrieval dates and force does not spend cadence on cache hits",()=>
        {
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddDays(-2);var json="""{"instance":{"id":4,"rewards":[],"fights":[],"coffers":[]}}""";
            Wait(folder.Cache.WriteAsync("sources","Instance:4","garland-routes-fr3-en2-v1",json,date));
            using var handler=new Handler(_=>json);using var client=new HttpClient(handler);using var transport=new GarlandClient(client);
            transport.ConfigureCache(()=>TimeSpan.FromDays(7),folder.Cache);using var service=new DutyLootService(transport);
            service.Select(new(4,4,1036,"Sastasha","Donjon",15));Wait(service.RequestAsync());
            Check(handler.Calls==0 && service.State.Document?.FetchedAt==date,"parsed cached source keeps original fetch date");
            Wait(service.RequestAsync(true));Check(handler.Calls==1 && service.State.Document?.FetchedAt>date,"force can fetch immediately after cache-only consultation");
        }),
        ("Fashion persistent cache avoids both HTTP reads and preserves provider metadata",()=>
        {
            using var folder=new Folder();var date=DateTimeOffset.UtcNow.AddDays(-2);var json="""{"Categories":{"20":{"3533":4}}}""";
            Wait(folder.Cache.WriteAsync("fashion","associations","gacha-v1",new FashionCacheData(json,null),date));
            using var handler=new Handler(request=>request.RequestUri!.AbsolutePath.EndsWith("LastUpdate.json",StringComparison.Ordinal) ? "null":json);
            using var client=new HttpClient(handler);using var service=new FashionDataService(client);service.ConfigureCache(()=>TimeSpan.FromDays(7),folder.Cache);
            Wait(service.RequestAsync());Check(handler.Calls==0 && service.State.Dataset?.FetchedAt==date,"cache does not invent a new observation date");
            Wait(service.RequestAsync(true));Check(handler.Calls==2 && service.State.Dataset?.FetchedAt>date,"force fetches dataset and provider metadata without artificial cache-hit throttle");
        }),
        ("Craft calculations reuse disk across reopen and reject altered macro text before clipboard use",()=>
        {
            using var folder=new Folder();var recipe=new RecipeDefinition(1,2,1,"Test",100,[],0,new(80,100,1000,100,100,100,100,0,0,0));
            var input=new CraftLogInput(1,recipe,new(100,1000,1000,500),Actions(),[],new(1000,4,32,5000),0,true);var calls=0;
            using(var first=new CraftLogSession((value,token)=>{calls++;return CraftLogSession.Build(value,token);}))
            {first.ConfigureCache(()=>TimeSpan.FromDays(1),folder.Cache);Wait(first.StartAsync(input));Check(first.State.Result?.Suggested is not null,"initial computation succeeds");}
            Wait(folder.Cache.FlushAsync());
            using var second=new CraftLogSession((value,token)=>{calls++;return CraftLogSession.Build(value,token);});second.ConfigureCache(()=>TimeSpan.FromDays(1),folder.Cache);
            Wait(second.StartAsync(input with {ObservationRevision=99}));Check(calls==1 && second.State.FromCache,"ephemeral observation revision does not defeat identical-input cache");
            Wait(second.StartAsync(input,true));Wait(folder.Cache.FlushAsync());Check(calls==2 && !second.State.FromCache,"recalculate bypasses cache");
            var path=Directory.GetFiles(folder.Root,"*.json").Single();var document=JsonNode.Parse(File.ReadAllText(path))!;
            document["Value"]!["Suggested"]!["Export"]!["Pages"]![0]!["Lines"]![0]="/echo altered";File.WriteAllText(path,document.ToJsonString());
            Wait(second.StartAsync(input));Check(calls==3 && second.State.Result!.Suggested!.Export.Pages[0].Lines[0]!="/echo altered","cached macro is replayed and export verified before reuse");
        }),
        ("Preparation estimates use configured cache age while retaining independent provider age warning",()=>
        {
            var now=DateTimeOffset.UtcNow;var quote=Quote(now.AddHours(-2));PreparationCostDemand[] demand=[new(42,1,1,false,false)];
            PreparationCostEstimate Cost(TimeSpan duration,MarketQuote value)=>PreparationCost.BuildDemands(demand,71,PreparationCostBasis.AllRequired,PreparationPriceQuality.Nq,
                id=>new ItemDefinition(id,"Objet",true,IsMarketable:true),_=>value,now,cacheLifetime:duration);
            Check(!Cost(TimeSpan.FromDays(1),quote).Lines[0].StalePrice && Cost(TimeSpan.FromMinutes(5),quote).Lines[0].StalePrice,"same quote follows changed price policy");
            Check(Cost(TimeSpan.FromDays(7),quote with {UploadedAt=now.AddDays(-5)}).Lines[0].StalePrice,"retaining cache never pretends the underlying source was recently observed");
        }),
    };
}
