using System.Net;
using System.Net.Http;
using System.Text.Json;
using TenshoAssistant.Community;
using TenshoAssistant.Duties;
using TenshoAssistant.Maps;
using TenshoAssistant.Monsters;

internal static class MonsterLootTests
{
    private const ulong Billy = 1930000000273;
    private const ulong Nanny = 1940000000274;
    private static readonly DateTimeOffset Now=new(2026,10,28,12,0,0,TimeSpan.Zero);
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when(error is InvalidDataException or JsonException or InvalidOperationException or ArgumentException) { return; } throw new Exception("Invalid monster input accepted."); }
    private static void Run(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content=new StringContent(json) };
    private static HttpResponseMessage Item(uint id=5296,params ulong[]? mobs) => Json(JsonSerializer.Serialize(new { item=new { id,drops=mobs is { Length:>0 } ? mobs : new[] {Billy,Nanny} } }));
    private static HttpResponseMessage Mob(ulong id=Billy,uint itemId=5296) => Json(JsonSerializer.Serialize(new { mob=new { id,name="Bouquetin",lvl="16 - 18",zoneid=44,drops=new[] {itemId},coords=new[] {17.41,22.37,20.86} } }));
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp()=>ticks;
        public override DateTimeOffset GetUtcNow()=>Now.AddTicks(ticks);
        public void Advance()=>ticks+=TimeSpan.TicksPerSecond*3;
        public void Expire()=>ticks+=TimeSpan.TicksPerHour;
    }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Monster numeric identities beyond floating-point integer precision never collapse",()=>
        {
            var parsed=MonsterDocuments.ParseItem("""{"item":{"id":5296,"drops":[9007199254740992,9007199254740993]}}""",5296,Now);
            Check(parsed.Monsters.Count==2 && parsed.Monsters[0].Id+1==parsed.Monsters[1].Id,"UInt64 parsing preserves neighbouring large IDs without a double conversion");
        }),
        ("Monster item parser retains full composite IDs and only links explicit drops",()=>
        {
            var parsed=MonsterDocuments.ParseItem("""{"item":{"id":5296,"drops":[1930000000273,"1940000000274",1930000000273]},"partials":[{"type":"mob","id":"1930000000273","obj":{"i":1930000000273,"n":"Bouquetin","l":"16 - 18","z":44}},{"type":"mob","id":"999","obj":{"i":999,"n":"Unrelated"}}]}""",5296,Now);
            Check(parsed.HasDrops && parsed.Monsters.Count==2 && parsed.Monsters[0].Id==Billy && parsed.Monsters[1].Id==Nanny,"large IDs and ordered dedup preserved");
            Check(parsed.Monsters[0].Name=="Bouquetin" && parsed.Monsters[0].PlaceNameId==44 && parsed.Monsters[1].Name=="","unresolved reference retained without linking unrelated partial");
            try { ((IList<MonsterReference>)parsed.Monsters).Clear(); throw new Exception("Mutable references"); } catch(NotSupportedException) { }
        }),
        ("Monster missing and empty drop lists remain different observations",()=>
        {
            var missing=MonsterDocuments.ParseItem("""{"item":{"id":5296}}""",5296,Now);
            var empty=MonsterDocuments.ParseItem("""{"item":{"id":5296,"drops":[]}}""",5296,Now);
            Check(!missing.HasDrops && empty.HasDrops && missing.Monsters.Count==0 && empty.Monsters.Count==0,"missing not invented empty coverage");
            var mob=MonsterDocuments.ParseMonster($"{{\"mob\":{{\"id\":{Billy}}}}}",Billy,Now);
            Check(!mob.HasDrops && mob.X is null && mob.Y is null && mob.PlaceNameId==0 && mob.InstanceId==0,"missing details stay unknown");
        }),
        ("Monster parser validates entity partial item location and coordinate identities",()=>
        {
            Reject(()=>MonsterDocuments.ParseItem("""{"item":{"id":5297}}""",5296,Now));
            Reject(()=>MonsterDocuments.ParseItem("""{"item":{"id":5296,"drops":[1930000000273]},"partials":[{"type":"mob","id":"1930000000273","obj":{"i":274}}]}""",5296,Now));
            Reject(()=>MonsterDocuments.ParseItem("""{"item":{"id":5296,"drops":[0]}}""",5296,Now));
            Reject(()=>MonsterDocuments.ParseMonster("""{"mob":{"id":1930000000274}}""",Billy,Now));
            Reject(()=>MonsterDocuments.ParseMonster("""{"mob":{"id":1930000000273,"coords":[1]}}""",Billy,Now));
            Reject(()=>MonsterDocuments.ParseMonster("""{"mob":{"id":1930000000273,"coords":[10001,2]}}""",Billy,Now));
            Reject(()=>MonsterDocuments.ParseMonster("""{"mob":{"id":1930000000273,"drops":[4294967296]}}""",Billy,Now));
        }),
        ("Monster parser bounds bodies collections and cancellation without mutating source data",()=>
        {
            Reject(()=>MonsterDocuments.ParseItem(new string(' ',GarlandClient.MaximumBytes+1),5296,Now));
            Reject(()=>MonsterDocuments.ParseItem(JsonSerializer.Serialize(new {item=new {id=5296,drops=Enumerable.Repeat(Billy,4097)}}),5296,Now));
            try { MonsterDocuments.ParseItem("{}",5296,Now,new CancellationToken(true)); throw new Exception("Cancellation ignored"); } catch(OperationCanceledException) { }
            var parsed=MonsterDocuments.ParseMonster("""{"mob":{"id":1930000000273,"drops":[5296,5296,5436],"coords":[17.41,22.37,20.86],"zoneid":44,"lvl":"16 - 18"}}""",Billy,Now.ToOffset(TimeSpan.FromHours(2)));
            Check(parsed.DropItemIds.SequenceEqual(new uint[]{5296,5436}) && parsed.X==17.41f && parsed.Y==22.37f && parsed.FetchedAt.Offset==TimeSpan.Zero,"copied details preserve values and UTC");
            try { ((IList<uint>)parsed.DropItemIds).Clear(); throw new Exception("Mutable drops"); } catch(NotSupportedException) { }
        }),
        ("Monster place links never confuse PlaceName with map or territory and retain ambiguous maps",()=>
        {
            var catalog=new MapCatalog([new(10,100,"Region","Zone","",0,100,44),new(11,100,"Region","Zone","Cave",1,100,44),new(44,44,"Elsewhere","Wrong","",0,100,999)]);
            var matches=catalog.ForPlace(44);
            Check(matches.Count==2 && matches.All(map=>map.TerritoryId==100) && matches.All(map=>map.MapId!=44),"PlaceName match independent of colliding numeric IDs");
            Check(catalog.ForPlace(0).Count==0 && catalog.ForPlace(100).Count==0,"no fallback to territory or zero");
            try { ((IList<MapDefinition>)matches).Clear(); throw new Exception("Mutable map candidates"); } catch(NotSupportedException) { }
        }),
        ("Monster requests are manual use numeric routes and share Garland cadence with Duty Loot",()=>
        {
            var calls=new List<string>(); var clock=new Clock();
            using var http=new HttpClient(new MarketTestHandler((message,_)=>
            {
                var route=message.RequestUri!.AbsolutePath; calls.Add(route);
                return Task.FromResult(route.Contains("instance/") ? Json("""{"instance":{"id":4}}""") : route.Contains("item/") ? Item() : Mob());
            }));
            using var shared=new GarlandClient(http,clock); using var duty=new DutyLootService(shared,clock); using var service=new MonsterLootService(shared,clock);
            duty.Select(new(4,4,1036,"Sastasha","Dungeon",15)); service.SelectItem(5296);
            Check(calls.Count==0,"neither constructor nor selection fetches"); Run(duty.RequestAsync()); Run(service.RequestItemAsync());
            Check(calls.Count==1 && service.State.Error is not null,"shared cadence applies across domains");
            clock.Advance(); Run(service.RequestItemAsync()); service.SelectMonster(Billy);
            Check(calls.Count==2 && service.State.Monster is null,"monster selection never fetches");
            clock.Advance(); Run(service.RequestMonsterAsync());
            Check(calls.SequenceEqual(new[]{"/db/doc/instance/en/2/4.json","/db/doc/item/fr/3/5296.json","/db/doc/mob/fr/2/1930000000273.json"}),"exact public IDs and French endpoints only");
            Check(service.State.Monster?.Id==Billy && service.State.Item?.ItemId==5296,"scope published");
        }),
        ("Monster caches expire explicitly and item refresh removes stale selected associations",()=>
        {
            var calls=0; var clock=new Clock(); var remove=false;
            using var http=new HttpClient(new MarketTestHandler((message,_)=>
            { calls++; return Task.FromResult(message.RequestUri!.AbsolutePath.Contains("item/") ? (remove ? Item(5296,Nanny) : Item()) : Mob()); }));
            using var shared=new GarlandClient(http,clock); using var service=new MonsterLootService(shared,clock);
            service.SelectItem(5296); Run(service.RequestItemAsync()); clock.Advance(); service.SelectMonster(Billy); Run(service.RequestMonsterAsync());
            Run(service.RequestItemAsync()); Run(service.RequestMonsterAsync()); Check(calls==2,"both fresh caches reused");
            clock.Expire(); Run(service.RequestMonsterAsync()); Check(calls==3,"expired detail fetched on explicit request");
            clock.Advance(); remove=true; Run(service.RequestItemAsync(true));
            Check(service.State.MonsterId==0 && service.State.Monster is null && service.State.Item!.Monsters.Single().Id==Nanny,"refresh prunes removed association");
            Reject(()=>service.SelectMonster(Billy));
        }),
        ("Monster item changes and cancellation reject late responses without polluting caches",()=>
        {
            foreach(var cancelOnly in new[]{false,true})
            {
                var pending=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var http=new HttpClient(new MarketTestHandler((_,_)=>pending.Task)); using var shared=new GarlandClient(http); using var service=new MonsterLootService(shared);
                service.SelectItem(5296); var task=service.RequestItemAsync();
                if(cancelOnly) service.Cancel(); else service.SelectItem(5436);
                pending.SetResult(Item()); Run(task);
                Check(service.State.Item is null && !service.State.Loading,"late item response rejected");
                service.SelectItem(0); service.SelectItem(5296); Check(service.State.Item is null,"discarded item not cached");
            }
        }),
        ("Monster selection generation rejects old detail while preserving the newer detail request",()=>
        {
            var first=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var second=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously); var clock=new Clock();
            using var http=new HttpClient(new MarketTestHandler((message,_)=>message.RequestUri!.AbsolutePath.Contains("item/") ? Task.FromResult(Item()) : message.RequestUri.AbsolutePath.Contains(Billy.ToString()) ? first.Task : second.Task));
            using var shared=new GarlandClient(http,clock); using var service=new MonsterLootService(shared,clock);
            service.SelectItem(5296); Run(service.RequestItemAsync()); clock.Advance(); service.SelectMonster(Billy); var old=service.RequestMonsterAsync();
            service.SelectMonster(Nanny); clock.Advance(); var newer=service.RequestMonsterAsync();
            first.SetResult(Mob()); Run(old); Check(service.State.Loading && service.State.MonsterId==Nanny,"old finally does not complete new work");
            second.SetResult(Mob(Nanny)); Run(newer); Check(service.State.Monster?.Id==Nanny,"new detail published");
            service.SelectMonster(Billy); Check(service.State.Monster is null,"late prior detail not cached");
        }),
        ("Monster malformed or failed refresh retains old dated evidence with an explicit error",()=>
        {
            var clock=new Clock(); var mode=0;
            using var http=new HttpClient(new MarketTestHandler((_,_)=>Task.FromResult(mode==0 ? Item() : mode==1 ? Item(999) : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
            using var shared=new GarlandClient(http,clock); using var service=new MonsterLootService(shared,clock);
            service.SelectItem(5296); Run(service.RequestItemAsync()); var original=service.State.Item;
            foreach(var failure in new[]{1,2})
            { clock.Advance(); mode=failure; Run(service.RequestItemAsync(true)); Check(ReferenceEquals(original,service.State.Item) && service.State.Error is not null && !service.State.Loading,"failed refresh not a new empty success"); }
        }),
        ("Monster item and detail caches are bounded without conflating their numeric keys",()=>
        {
            var calls=0; var clock=new Clock(); var refs=Enumerable.Range(1,MonsterLootService.MonsterCacheCapacity+1).Select(id=>Billy+(ulong)id).ToArray();
            using var http=new HttpClient(new MarketTestHandler((message,_)=>
            { calls++; var id=ulong.Parse(Path.GetFileNameWithoutExtension(message.RequestUri!.AbsolutePath)); return Task.FromResult(message.RequestUri.AbsolutePath.Contains("item/") ? Item((uint)id,refs) : Mob(id)); }));
            using var shared=new GarlandClient(http,clock); using var service=new MonsterLootService(shared,clock);
            for(uint id=1;id<=MonsterLootService.ItemCacheCapacity+1;id++) { service.SelectItem(id); Run(service.RequestItemAsync()); clock.Advance(); }
            service.SelectItem(1); Check(service.State.Item is null,"oldest item evicted"); Run(service.RequestItemAsync()); clock.Advance();
            foreach(var id in refs) { service.SelectMonster(id); Run(service.RequestMonsterAsync()); clock.Advance(); }
            service.SelectMonster(refs[0]); Check(service.State.Monster is null,"oldest monster evicted");
            var before=calls; service.SelectMonster(refs[1]); Run(service.RequestMonsterAsync()); Check(calls==before && service.State.Monster?.Id==refs[1],"retained monster reused without request");
        }),
        ("Monster disposal and external cancellation reject ignored-token responses and preserve shared ownership",()=>
        {
            foreach(var dispose in new[]{false,true})
            {
                var pending=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously); var calls=0; var clock=new Clock();
                using var http=new HttpClient(new MarketTestHandler((_,_)=>++calls==1 ? pending.Task : Task.FromResult(Item())));
                using var shared=new GarlandClient(http,clock); using var service=new MonsterLootService(shared,clock); using var cancellation=new CancellationTokenSource();
                service.SelectItem(5296); var task=service.RequestItemAsync(cancellation:cancellation.Token);
                if(dispose) service.Dispose(); else cancellation.Cancel(); pending.SetResult(Item()); Run(task);
                Check(service.State.Item is null && !service.State.Loading,"no cancelled publication"); clock.Advance();
                Run(shared.ReadAsync(GarlandResource.Item,5296,CancellationToken.None)); Check(calls==2,"service never disposes borrowed shared transport");
            }
        }),
        ("Shared Garland transport cancels lifetime and rejects invalid UTF8 before JSON parsing",()=>
        {
            using(var http=new HttpClient(new MarketTestHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(new byte[]{0xc3,0x28})}))))
            using(var shared=new GarlandClient(http))
            using(var service=new MonsterLootService(shared))
            { service.SelectItem(5296); Run(service.RequestItemAsync()); Check(service.State.Error is not null && service.State.Item is null,"strict UTF8 rejected"); }
            var pending=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var borrowed=new HttpClient(new MarketTestHandler((_,_)=>pending.Task)); var client=new GarlandClient(borrowed);
            var read=client.ReadAsync(GarlandResource.Item,5296,CancellationToken.None); client.Dispose(); pending.SetResult(Item());
            try { Run(read); throw new Exception("Lifetime cancellation ignored"); } catch(OperationCanceledException) { }
        })
    ];
}
