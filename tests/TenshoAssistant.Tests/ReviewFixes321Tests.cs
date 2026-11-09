using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

// 0.321 (queue step d, review 0.316): reset lists, saved crafting limits, a control's tooltip placement, and the network guards.
internal static class ReviewFixes321Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void Until(Func<bool> done,string what) { var deadline = DateTime.UtcNow.AddSeconds(10); while (!done()) { if (DateTime.UtcNow > deadline) throw new Exception("timed out: " + what); Thread.Sleep(10); } }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Reset all: every public setting of Configuration is either kept or reset, never both, and folded groups are kept",()=>
        {
            var properties = typeof(TenshoAssistant.Configuration).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0).Select(property => property.Name).ToArray();
            var kept = TenshoAssistant.ConfigurationReset.Kept; var reset = TenshoAssistant.ConfigurationReset.Reset;
            var missing = properties.Where(name => !kept.Contains(name) && !reset.Contains(name)).ToArray();
            Check(missing.Length == 0,"properties in no list (decide whether « reset all » keeps them): " + string.Join(", ",missing));
            Check(!kept.Overlaps(reset),"a property in both lists: " + string.Join(", ",kept.Intersect(reset)));
            var unknown = kept.Concat(reset).Except(properties).ToArray();
            Check(unknown.Length == 0,"names that are no property: " + string.Join(", ",unknown));
            Check(kept.Contains("NavigationCollapsed") && kept.Contains("Alarms") && reset.Contains("MapView") && reset.Contains("CraftSearch"),"folded groups and saved data kept, settings reset");
        }),
        ("Crafting search limits and export options are saved with their old defaults and bounded when read",()=>
        {
            var fresh = new TenshoAssistant.Configuration().CraftSearch;
            Check(fresh.Steps == 40 && fresh.Width == 128 && fresh.Budget == 100000 && fresh.ExtraWait == 0 && fresh.Echo,"defaults unchanged since the window fields");
            var edited = new TenshoAssistant.CraftSearchSettings { Steps = 0, Width = 9999, Budget = -5, ExtraWait = 7 };
            Check(edited.BoundedSteps == 1 && edited.BoundedWidth == 512 && edited.BoundedBudget == 1 && edited.BoundedExtraWait == 3,"bounds 1–100, 1–512, 1–500 000, 0–3");
        }),
        ("Universalis data centre table: after a failure it is asked again at most once a minute, not on every frame",()=>
        {
            var calls = 0;
            using var service = new TenshoAssistant.Market.MarketBoardService(new HttpClient(new MarketTestHandler((_,_) => { Interlocked.Increment(ref calls); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)); })));
            Check(service.RegionOf("Light") is null,"unknown at first");
            Until(() => Volatile.Read(ref calls) == 1,"first request");
            Thread.Sleep(100);
            for (var frame = 0; frame < 50; frame++) Check(service.RegionOf("Light") is null,"still unknown");
            Thread.Sleep(100);
            Check(Volatile.Read(ref calls) == 1,"no new request within the minute: " + calls);
            Check(TenshoAssistant.Market.MarketBoardService.RegionRetry == TimeSpan.FromMinutes(1),"retry pause");
        }),
        ("Teamcraft bite times: a reply larger than 1 MiB is refused while it is read, never loaded whole",()=>
        {
            var served = 0L; HttpMethod? method = null; var agent = "";
            using var service = new TenshoAssistant.Community.FishBiteTimeService(() => TimeSpan.FromHours(1),null,new HttpClient(new MarketTestHandler((request,_) =>
            {
                method = request.Method; agent = request.Headers.UserAgent.ToString();
                var content = new CountingContent(4 * 1024 * 1024,read => Interlocked.Add(ref served,read));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
            })));
            Check(service.Get(35).Loading,"queued");
            Until(() => !service.Get(35).Loading,"answered");
            Check(method == HttpMethod.Post && agent.Contains("TenshoAssistant",StringComparison.Ordinal),"GraphQL POST with the User-Agent: " + method + " " + agent);
            Check(service.Get(35).Error == "Community response too large.","refused as too large: " + service.Get(35).Error);
            Check(Interlocked.Read(ref served) > TenshoAssistant.Community.FishBiteTimeService.MaximumBytes && Interlocked.Read(ref served) < 2 * 1024 * 1024,
                "stopped soon after the limit, while reading: " + served + " bytes read");
        }),
        ("Fashion Report source turned off: no download, no loading state",()=>
        {
            var calls = 0;
            using var service = new TenshoAssistant.Fashion.FashionDataService(new HttpClient(new MarketTestHandler((_,_) => { Interlocked.Increment(ref calls); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); }))) { Enabled = () => false };
            service.RequestAsync().GetAwaiter().GetResult();
            Check(calls == 0 && service.Disabled && !service.State.Loading && service.State.Error is null,"nothing downloaded while off");
        }),
    ];

    // A reply without a declared length that produces bytes as they are read, counting how many were asked for.
    private sealed class CountingContent(long length,Action<int> counted) : HttpContent
    {
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new Producer(length,counted));
        protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context) => new Producer(length,counted).CopyToAsync(stream);
        protected override bool TryComputeLength(out long size) { size = 0; return false; }
        private sealed class Producer(long length,Action<int> counted) : Stream
        {
            private long position;
            public override int Read(byte[] buffer,int offset,int count)
            {
                var read = (int)Math.Min(count,length - position); if (read <= 0) return 0;
                Array.Fill(buffer,(byte)'a',offset,read); position += read; counted(read); return read;
            }
            public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException(); public override long Position { get => position; set => throw new NotSupportedException(); }
            public override void Flush() { } public override long Seek(long offset,SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException(); public override void Write(byte[] buffer,int offset,int count) => throw new NotSupportedException();
        }
    }
}
