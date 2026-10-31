using System.Net;
using System.Net.Http;
using System.Text.Json;
using TenshoAssistant.Fashion;
using TenshoAssistant.Items;

internal static class FashionTests
{
    private const string Data="""{"Categories":{"20":{"3533":4,"3538":1,"4294967295":2},"99":{}},"WeeklyDyes":{"999999":{"ignored":"weekly dyes are not equipment hints"}}}""";
    private const string Date="\"Wed, 28 Oct 2026 16:21:18 GMT\"";
    private static readonly DateTimeOffset Now=new(2026,10,28,17,0,0,TimeSpan.Zero);
    private static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    private static void Reject(Action action) { try { action(); } catch(Exception error) when(error is InvalidDataException or JsonException or ArgumentException) { return; } throw new Exception("Invalid fashion input accepted."); }
    private static void Run(Task task)=>task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    private static HttpResponseMessage Json(string value)=>new(HttpStatusCode.OK) {Content=new StringContent(value)};
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency=>TimeSpan.TicksPerSecond;
        public override long GetTimestamp()=>ticks;
        public override DateTimeOffset GetUtcNow()=>Now.AddTicks(ticks);
        public void Advance(int seconds=15)=>ticks+=TimeSpan.TicksPerSecond*seconds;
    }
    private static ItemCatalog Items()=>ItemCatalog.Build(new[]
    {
        new ItemDefinition(3533,"Vêtement éprouvé",false,Equipment:EquipmentSlots.Body),
        new ItemDefinition(3538,"Chapeau",true,UiCategoryId:34,Equipment:EquipmentSlots.Head)
    },new("fixture","French","test",1));
    public static IEnumerable<(string Name,Action Run)> Cases=>
    [
        ("Fashion associations preserve native IDs counts unknown references and ignore weekly dye sections",()=>
        {
            var data=FashionDataset.Parse(Data,Date,Now);
            Check(data.HintIds.SequenceEqual(new uint[]{20,99}) && data.ForHint(20).Count==3 && data.ForHint(99).Count==0,"category and item identities preserved");
            Check(data.ForHint(20)[0]==new FashionMatch(3533,4) && data.ForHint(20)[1].ItemId==uint.MaxValue,"counts sorted without dropping unknown item");
            Check(data.ForHint(999999).Count==0 && data.DateStatus==FashionDateStatus.Parsed && data.ProviderUpdatedAt==new DateTimeOffset(2026,10,28,16,21,18,TimeSpan.Zero),"weekly dyes never become gear categories; provider date distinct");
            try { ((IList<FashionMatch>)data.ForHint(20)).Clear(); throw new Exception("Mutable associations"); } catch(NotSupportedException) { }
        }),
        ("Fashion source dates preserve missing invalid future and unqualified metadata without invented freshness",()=>
        {
            Check(FashionDataset.Parse(Data,null,Now).DateStatus==FashionDateStatus.Missing,"missing date stays missing");
            foreach(var invalid in new[]{"{}","null","\"2026-10-28T16:21:18\"","\"garbage\"","\"Fri, 01 Jan 2010 00:00:00 GMT\""})
            { var parsed=FashionDataset.Parse(Data,invalid,Now); Check(parsed.ProviderUpdatedAt is null && parsed.DateStatus==FashionDateStatus.Invalid,"ambiguous metadata not replaced by fetch time"); }
            var future=FashionDataset.Parse(Data,"\"2027-10-28T17:00:00Z\"",Now);
            Check(future.DateStatus==FashionDateStatus.Future && future.ProviderUpdatedAt>future.FetchedAt,"future date retained and flagged");
        }),
        ("Fashion malformed duplicate contradictory IDs counts and bounded inputs fail explicitly",()=>
        {
            foreach(var invalid in new[]{"{}","[]","""{"Categories":{},"Categories":{}}""","""{"Categories":{"0":{}}}""","""{"Categories":{"20":{"3533":0}}}""",
                """{"Categories":{"20":{"3533":1,"3533":2}}}""","""{"Categories":{"20":{},"020":{}}}""","""{"Categories":{"20":{"3533":-1}}}"""})
                Reject(()=>FashionDataset.Parse(invalid,Date,Now));
            Reject(()=>FashionDataset.Parse(new string(' ',FashionDataset.MaximumBytes+1),null,Now));
            try { FashionDataset.Parse(Data,null,Now,new CancellationToken(true)); throw new Exception("Cancellation ignored"); } catch(OperationCanceledException) { }
        }),
        ("Fashion slot filters use positive native equipment bits and preserve blocked slots and both rings correctly",()=>
        {
            var slots=EquipmentSlotMap.FromNative(-1,1,0,-1,0,0,0,0,0,1);
            Check(slots==(EquipmentSlots.Body|EquipmentSlots.Ring),"negative reserved slots are not positive equipment slots");
            var data=FashionDataset.Parse(Data,Date,Now); var items=Items();
            Check(data.Filter(20,EquipmentSlots.Body,items,"").Single().Match.ItemId==3533,"body resolved from equipment mask");
            Check(data.Filter(20,EquipmentSlots.Feet,items,"").Count==0,"UI category 34 cannot masquerade as equipment slot");
            Check(data.Filter(20,EquipmentSlots.None,items,"").Count==3 && data.Filter(20,EquipmentSlots.Head,items,"").Single().Match.ItemId==3538,"unknown item retained globally but not assigned a slot");
            Check(data.Filter(20,EquipmentSlots.None,items,"eprouve").Single().Match.ItemId==3533,"accent insensitive native item search");
        }),
        ("Fashion native hint catalog keeps duplicate localized names distinct by ID and copies producer lists",()=>
        {
            var source=new List<FashionHint>{new(1,"Même indice"),new(2,"Même indice"),new(0,""),new(3,"")};
            var catalog=new FashionHintCatalog(source); source.Clear();
            Check(catalog.Hints.Count==2 && catalog.Hints[0].Id!=catalog.Hints[1].Id,"localized names never identify or merge categories");
            Reject(()=>new FashionHintCatalog([new(1,"One"),new(1,"Other")]));
            var service=new FashionHintService(); service.Publish(catalog); service.Fail(new Exception("test"));
            Check(service.State.Catalog is null && service.State.Error=="test","failed native catalog remains unavailable");
        }),
        ("Fashion dataset fetch is explicit fixed public GET with metadata separate from associations",()=>
        {
            var calls=new List<Uri>();
            using var client=new HttpClient(new MarketTestHandler((request,_)=>
            { Check(request.Method==HttpMethod.Get,"no uploads"); calls.Add(request.RequestUri!); return Task.FromResult(Json(request.RequestUri!.AbsolutePath.EndsWith("LastUpdate.json",StringComparison.Ordinal) ? Date:Data)); }));
            using var service=new FashionDataService(client);
            Check(calls.Count==0 && service.State.Dataset is null,"construction never fetches"); Run(service.RequestAsync());
            Check(calls.Count==2 && calls.All(uri=>uri.Query=="" && uri.AbsoluteUri.StartsWith(FashionDataService.DataBase,StringComparison.Ordinal)),"only two fixed dataset routes without character or hint parameters");
            Check(service.State.Dataset?.ForHint(20).Count==3 && service.State.Error is null && !service.State.Loading,"published dataset independent from any current week");
        }),
        ("Fashion cache expiry and force refresh retain minimum batch cadence",()=>
        {
            var calls=0; var clock=new Clock();
            using var client=new HttpClient(new MarketTestHandler((request,_)=>{calls++; return Task.FromResult(Json(request.RequestUri!.AbsolutePath.EndsWith("LastUpdate.json") ? Date:Data));}));
            using var service=new FashionDataService(client,clock); Run(service.RequestAsync()); Run(service.RequestAsync());
            Check(calls==2,"fresh parsed dataset reused"); Run(service.RequestAsync(true)); Check(calls==2 && service.State.Error is not null,"force does not bypass cadence");
            clock.Advance(); Run(service.RequestAsync(true)); Check(calls==4,"manual refresh after cadence");
            clock.Advance(1801); Run(service.RequestAsync()); Check(calls==6,"stale dataset re-fetched only after explicit request");
        }),
        ("Fashion missing or malformed source timestamp does not discard usable historical associations",()=>
        {
            foreach(var bad in new[]{false,true})
            {
                using var client=new HttpClient(new MarketTestHandler((request,_)=>Task.FromResult(request.RequestUri!.AbsolutePath.EndsWith("LastUpdate.json")
                    ? bad ? Json("{") : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Json(Data))));
                using var service=new FashionDataService(client); Run(service.RequestAsync());
                Check(service.State.Dataset?.ForHint(20).Count==3 && service.State.Dataset.DateStatus!=FashionDateStatus.Parsed,"date provenance remains unknown while association data survives");
            }
        }),
        ("Fashion cancellation rejects late batch replies without publishing or caching foreign completion",()=>
        {
            foreach(var external in new[]{false,true})
            {
                var data=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                var metadata=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var client=new HttpClient(new MarketTestHandler((request,_)=>request.RequestUri!.AbsolutePath.EndsWith("LastUpdate.json") ? metadata.Task:data.Task));
                using var service=new FashionDataService(client); using var cancellation=new CancellationTokenSource();
                var pending=service.RequestAsync(cancellation:cancellation.Token); Run(service.RequestAsync());
                if(external) cancellation.Cancel(); else service.Cancel(); data.SetResult(Json(Data)); metadata.SetResult(Json(Date)); Run(pending);
                Check(service.State.Dataset is null && !service.State.Loading && service.State.Error is not null,"cancelled batch never published");
            }
        }),
        ("Fashion data errors preserve previous dated evidence and never replace it with an empty success",()=>
        {
            var clock=new Clock(); var fail=false;
            using var client=new HttpClient(new MarketTestHandler((request,_)=>Task.FromResult(request.RequestUri!.AbsolutePath.EndsWith("LastUpdate.json") ? Json(Date)
                : fail ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable):Json(Data))));
            using var service=new FashionDataService(client,clock); Run(service.RequestAsync()); var before=service.State.Dataset;
            clock.Advance(); fail=true; Run(service.RequestAsync(true));
            Check(ReferenceEquals(before,service.State.Dataset) && service.State.Error is not null && !service.State.Loading,"old retrieval/date and contents retained with error");
        }),
        ("Fashion failed data batch owns pending metadata until completion and dispose prevents late publication",()=>
        {
            var metadata=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously); var calls=0;
            using var client=new HttpClient(new MarketTestHandler((request,_)=>
            { calls++; return request.RequestUri!.AbsolutePath.EndsWith("LastUpdate.json") ? metadata.Task:Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); }));
            var service=new FashionDataService(client); var pending=service.RequestAsync();
            Check(!pending.IsCompleted && service.State.Loading && calls==2,"failed data does not orphan pending metadata operation");
            service.Dispose(); service.Dispose(); metadata.SetResult(Json(Date)); Run(pending); Run(service.RequestAsync());
            Check(service.State.Dataset is null && !service.State.Loading && calls==2,"disposed batch and future requests inert");
            using var borrowed=client.GetAsync("https://example.invalid/ownership-test").GetAwaiter().GetResult();
            Check(calls==3,"borrowed client remains usable");
        })
    ];
}
