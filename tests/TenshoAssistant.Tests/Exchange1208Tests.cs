using System.Net;
using System.Numerics;
using System.Text;
using TenshoAssistant.Discoveries;

// 0.1208 (X-184 N15, night review of 12/11): a common file whose body is cut midway (an IOException, not a refused request) ended the whole
// exchange (« ExchangeFailed ») before the player's own facts were sent; the other kinds were not taken either. It now counts as unreadable
// and the exchange goes on. Behaviour test (R28): a fake GitHub that cuts one kind file in the middle of its body, and the v2 file the same.
internal static class Exchange1208Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Vector3 Place = new(120, 0, -40);

    private static DiscoveryFact Fish(uint subject)
    {
        var cell = DiscoveryStore.Cell(Place.X, Place.Z);
        return new() { Kind = DiscoveryKind.FishCatch, Subject = subject, Detail = 1, Territory = 135, Map = 16, CellX = cell.X, CellZ = cell.Z, X = Place.X, Z = Place.Z,
            LevelMin = 1, LevelMax = 1, Imported = 1, First = At.AddDays(-3), Last = At.AddDays(-2) };
    }
    private static DiscoveryFact Venture(uint subject) => new() { Kind = DiscoveryKind.Venture, Subject = subject, Detail = 7, LevelMin = 1, LevelMax = 1, Imported = 1, First = At.AddDays(-3), Last = At.AddDays(-1) };

    /// <summary>A body that gives half its bytes, then breaks as a dropped connection does.</summary>
    private sealed class Cut(byte[] bytes) : Stream
    {
        private int given;
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => bytes.Length; public override long Position { get => given; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (given >= bytes.Length / 2) throw new IOException("connection lost in the middle of the body (test)");
            var take = Math.Min(count, bytes.Length / 2 - given);
            Array.Copy(bytes, given, buffer, offset, take); given += take;
            return take;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>A fake GitHub: the files given, those whose name ends with « cut » broken midway, 404 otherwise.</summary>
    private sealed class Server : HttpMessageHandler
    {
        public readonly Dictionary<string, byte[]> Files = [];
        public readonly HashSet<string> Broken = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var address = request.RequestUri!.ToString();
            if (!Files.TryGetValue(address, out var bytes)) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            HttpContent content = Broken.Contains(address) ? new StreamContent(new Cut(bytes)) : new ByteArrayContent(bytes);
            content.Headers.ContentLength = bytes.Length;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    private static ExchangeMessage Run(GitHubExchange exchange, DiscoveryStore store)
    {
        Check(exchange.Exchange(store, false), "the exchange starts");
        for (var wait = 0; exchange.Busy && wait < 1500; wait++) Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is { Key: "ExchangeDone" }, $"the exchange ends done ({exchange.Message?.Key} {string.Join(", ", exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Exchange 1208: behaviour: a kind file cut in the middle of its body counts as unreadable; the exchange ends done and the other kinds are taken", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "tensho-1208-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                using var server = new Server();
                var set = ExchangeV3.Build(new DiscoveryFile { Producer = "Tensho Assistant community", Written = At, Facts = [Fish(1), Fish(2), Venture(5)] });
                server.Files[CommunityDiscoveries.AddressV3 + ExchangeV3.IndexName] = Encoding.UTF8.GetBytes(set.Index);
                foreach (var (name, bytes) in set.Files) server.Files[CommunityDiscoveries.AddressV3 + name] = bytes;
                server.Broken.Add(CommunityDiscoveries.AddressV3 + ExchangeV3.FileName(DiscoveryKind.FishCatch));
                using var exchange = new GitHubExchange(Path.Combine(root, "a"), server);
                var store = new DiscoveryStore();
                var message = Run(exchange, store);
                Check(message.Args[2] == "CommonTaken" && store.Find(DiscoveryKind.Venture, 5u, 5).Count == 1 && store.Find(DiscoveryKind.FishCatch, 1u, 5).Count == 0,
                    "the venture kind taken, the cut fish kind left for the next time: " + string.Join(", ", message.Args));
                // The v2 file cut the same way (no v3 index): the exchange still ends done.
                using var old = new Server();
                old.Files[CommunityDiscoveries.Address] = Encoding.UTF8.GetBytes(new string('x', 4096));
                old.Broken.Add(CommunityDiscoveries.Address);
                using var v2 = new GitHubExchange(Path.Combine(root, "b"), old);
                Check(Run(v2, new DiscoveryStore()).Args[2] == "CommonUnreadable", "v2 cut: unreadable, done");
            }
            finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }),
        ("Exchange 1208: wiring: one test for what a failed read of the common may throw; the player's facts are sent after it whatever it gave", () =>
        {
            var source = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs")).Replace("\r\n", "\n");
            Check(source.Contains("|| error is TaskCanceledException && !stop.IsCancellationRequested;") && source.Contains("catch(Exception error) when(CommonUnreadable(error)){taken=(\"CommonUnreadable\",0,0);}"), "unreadable, not failed");
            var read = source.IndexOf("catch(Exception error) when(CommonUnreadable(error)){taken=", StringComparison.Ordinal);
            Check(read > 0 && read < source.IndexOf("if(upload && Connected)", read, StringComparison.Ordinal), "the upload comes after, whatever the read gave");
        }),
    ];
}
