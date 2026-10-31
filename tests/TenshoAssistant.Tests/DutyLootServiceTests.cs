using System.Net;
using System.Net.Http;
using System.Text;
using TenshoAssistant.Duties;

internal static class DutyLootServiceTests
{
    private static DutyDefinition Duty(uint id = 4, uint? instance = null) => new(id,instance ?? id,1000+id,$"Duty {id}","Dungeon",15);
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static HttpResponseMessage Reply(uint id = 4) => new(HttpStatusCode.OK)
    { Content = new StringContent($"{{\"instance\":{{\"id\":{id},\"rewards\":[2684],\"fights\":[],\"coffers\":[]}}}}") };
    private static void Run(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026,10,28,12,0,0,TimeSpan.Zero).AddTicks(ticks);
        public void Advance(TimeSpan duration) => ticks += duration.Ticks;
    }
    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context) => stream.WriteAsync(bytes).AsTask();
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(bytes));
    }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Duty catalog preserves independent finder instance territory IDs and immutable source copies", () =>
        {
            var input = new List<DutyDefinition> { Duty(100,4),Duty(101,4),Duty(0),Duty(9) with { Name="" } };
            var catalog = new DutyCatalog(input); input.Clear();
            Check(catalog.Entries.Count == 2 && catalog.Find(100)?.InstanceId == 4 && catalog.Find(100)?.TerritoryId == 1100,"independent native keys retained");
            try { _ = new DutyCatalog([Duty(1),Duty(1,9)]); throw new Exception("Duplicate accepted"); } catch (ArgumentException) { }
            try { _ = new DutyCatalog([Duty()],new CancellationToken(true)); throw new Exception("Cancellation ignored"); } catch (OperationCanceledException) { }
            var service = new DutyCatalogService(); service.Publish(catalog); service.Fail(new Exception("test"));
            Check(service.State.Catalog is null && service.State.Error == "test","failed catalog never presents empty success");
        }),
        ("Duty loot selection is local and manual HTTP sends only the instance ID", () =>
        {
            var calls = 0;
            using var client = new HttpClient(new MarketTestHandler((message,_) =>
            {
                calls++; Check(message.Method == HttpMethod.Get && message.RequestUri!.AbsoluteUri == "https://www.garlandtools.org/db/doc/instance/en/2/4.json","route uses instance not finder/territory");
                return Task.FromResult(Reply());
            }));
            using var service = new DutyLootService(client);
            service.Select(Duty(100,4)); Check(calls == 0 && service.State.Document is null,"select never fetches");
            Run(service.RequestAsync());
            Check(calls == 1 && service.State.Duty!.DutyId == 100 && service.State.Document?.InstanceId == 4 && !service.State.Loading,"manual request published with exact scope");
        }),
        ("Duty loot cache expiry force refresh and cadence use a controlled monotonic clock", () =>
        {
            var calls = 0; var clock = new Clock();
            using var client = new HttpClient(new MarketTestHandler((_,_) => { calls++; return Task.FromResult(Reply()); }));
            using var service = new DutyLootService(client,clock); service.Select(Duty()); Run(service.RequestAsync());
            Run(service.RequestAsync()); Check(calls == 1,"fresh cache reused");
            Run(service.RequestAsync(true)); Check(calls == 1 && service.State.Error is not null,"force still throttled");
            clock.Advance(TimeSpan.FromSeconds(3)); Run(service.RequestAsync(true)); Check(calls == 2,"explicit refresh after interval");
            clock.Advance(DutyLootService.FreshFor); Run(service.RequestAsync()); Check(calls == 3,"stale cache re-fetched only on request");
        }),
        ("Duty selection cancellation rejects late replies and prevents foreign cache entries", () =>
        {
            var reply = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var client = new HttpClient(new MarketTestHandler((_,_) => reply.Task));
            using var service = new DutyLootService(client); service.Select(Duty()); var pending = service.RequestAsync();
            service.Select(Duty(5)); reply.SetResult(Reply()); Run(pending);
            Check(service.State.Duty!.DutyId == 5 && service.State.Document is null && !service.State.Loading,"late previous scope rejected");
            service.Select(Duty()); Check(service.State.Document is null,"discarded response never cached");
        }),
        ("Duty request overlap and late finally never alter the newer request", () =>
        {
            var old = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var newer = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0; var clock = new Clock();
            using var client = new HttpClient(new MarketTestHandler((_,_) => ++calls == 1 ? old.Task : newer.Task));
            using var service = new DutyLootService(client,clock); service.Select(Duty()); var first = service.RequestAsync();
            Run(service.RequestAsync()); Check(calls == 1,"same selection overlap ignored");
            service.Select(Duty(5)); clock.Advance(TimeSpan.FromSeconds(3)); var second = service.RequestAsync();
            old.SetResult(Reply()); Run(first); Check(service.State.Loading && service.State.Duty!.DutyId == 5,"old completion does not finish new request");
            newer.SetResult(Reply(5)); Run(second); Check(service.State.Document?.InstanceId == 5 && !service.State.Loading,"new selection published");
        }),
        ("Duty manual and external cancellation do not publish replies from cancellation-ignoring handlers", () =>
        {
            foreach (var external in new[] {false,true})
            {
                var reply = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var client = new HttpClient(new MarketTestHandler((_,_) => reply.Task));
                using var service = new DutyLootService(client); using var cancellation = new CancellationTokenSource();
                service.Select(Duty()); var pending = service.RequestAsync(cancellation:cancellation.Token);
                if (external) cancellation.Cancel(); else service.Cancel();
                reply.SetResult(Reply()); Run(pending);
                Check(service.State.Document is null && !service.State.Loading && service.State.Error is not null,"cancelled response not published");
            }
        }),
        ("Duty errors retain previous dated evidence without inventing a new empty loot table", () =>
        {
            var calls = 0; var clock = new Clock();
            using var client = new HttpClient(new MarketTestHandler((_,_) => Task.FromResult(++calls == 1 ? Reply() : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
            using var service = new DutyLootService(client,clock); service.Select(Duty()); Run(service.RequestAsync());
            var saved = service.State.Document; clock.Advance(TimeSpan.FromSeconds(3)); Run(service.RequestAsync(true));
            Check(ReferenceEquals(saved,service.State.Document) && service.State.Error is not null && !service.State.Loading,"old document retained with new error");
        }),
        ("Duty HTTP identity malformed JSON and declared or streamed oversized bodies fail explicitly", () =>
        {
            foreach (var mode in Enumerable.Range(0,4))
            {
                using var client = new HttpClient(new MarketTestHandler((_,_) =>
                {
                    var response = mode == 0 ? Reply(5) : Reply();
                    if (mode == 1) response.Content = new StringContent("{");
                    if (mode == 2) response.Content.Headers.ContentLength = DutyLootParser.MaximumBytes+1;
                    if (mode == 3) response.Content = new UnknownLengthContent(Encoding.UTF8.GetBytes(new string(' ',DutyLootParser.MaximumBytes+1)));
                    return Task.FromResult(response);
                }));
                using var service = new DutyLootService(client); service.Select(Duty()); Run(service.RequestAsync());
                Check(service.State.Error is not null && service.State.Document is null && !service.State.Loading,$"mode {mode} fails explicitly");
            }
        }),
        ("Duty cache is bounded and reuses data by instance while preserving selected finder identity", () =>
        {
            var clock = new Clock(); var calls = 0;
            using var client = new HttpClient(new MarketTestHandler((message,_) =>
            { calls++; var id = uint.Parse(Path.GetFileNameWithoutExtension(message.RequestUri!.AbsolutePath)); return Task.FromResult(Reply(id)); }));
            using var service = new DutyLootService(client,clock);
            for (uint id = 1; id <= DutyLootService.CacheCapacity+1; id++)
            { service.Select(Duty(id)); Run(service.RequestAsync()); clock.Advance(TimeSpan.FromSeconds(3)); }
            service.Select(Duty(100,2)); Check(service.State.Document?.InstanceId == 2 && service.State.Duty?.DutyId == 100,"same instance separate finder supported");
            Run(service.RequestAsync()); Check(calls == 33,"cached alternate finder needs no network");
            service.Select(Duty(1)); Check(service.State.Document is null,"oldest entry evicted");
            Run(service.RequestAsync()); Check(calls == 34,"evicted entry requires explicit re-fetch");
        }),
        ("Duty disposal rejects pending and future work and leaves borrowed HTTP ownership intact", () =>
        {
            var reply = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            using var client = new HttpClient(new MarketTestHandler((_,_) => ++calls == 1 ? reply.Task : Task.FromResult(Reply())));
            var service = new DutyLootService(client); service.Select(Duty()); var pending = service.RequestAsync();
            service.Dispose(); service.Dispose(); reply.SetResult(Reply()); Run(pending); Run(service.RequestAsync());
            Check(service.State.Duty is null && service.State.Document is null && !service.State.Loading,"disposed publication rejected");
            using var borrowedResponse = client.GetAsync("https://example.invalid/ownership-test").GetAwaiter().GetResult();
            Check(borrowedResponse.IsSuccessStatusCode && calls == 2,"borrowed client remains usable after service disposal");
        })
    ];
}
