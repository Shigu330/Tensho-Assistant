using System.Net;
using TenshoAssistant.Community;
using TenshoAssistant.Duties;

// 0.1556 (X-589 second part, external audit of 18/11, family « network sources »): behaviour tests with a simulated network (R28.14):
// A10 an item asked as the wiki's queue goes idle, A11 a change of world while loading and after a failure, A02 the competitive cut
// between the read and the chat line, A07 a source turned off during a batch, A24 what PaissaDB is sent, observed on the transport.
internal static class NetworkEnds1556Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Until(Func<bool> done) { for (var wait = 0; !done() && wait < 500; wait++) Thread.Sleep(10); }

    /// <summary>A simulated network: each request recorded, then answered by <c>answer</c>.</summary>
    private sealed class Network(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        public readonly List<(string Method, string Address, bool Body, string[] Headers)> Sent = [];
        public int Calls { get { lock (Sent) return Sent.Count; } }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            lock (Sent) Sent.Add((request.Method.Method, request.RequestUri!.ToString(), request.Content is not null, request.Headers.Select(header => header.Key).ToArray()));
            return answer(request);
        }
        public static Task<HttpResponseMessage> Text(string text, HttpStatusCode code = HttpStatusCode.OK) => Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(text) });
    }

    private const string World = """{"id":40,"name":"Moogle","districts":[{"id":339,"name":"Mist","open_plots":[{"district_id":339,"ward_number":4,"plot_number":11,"size":1,"price":3000000,"purchase_system":5}]}]}""";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Network ends 1556: A10, an item asked just as the wiki's queue goes idle is read, never left « loading »", () =>
        {
            var network = new Network(_ => Network.Text("{}"));
            using var service = new WikiItemAcquisitionService(id => "Item " + id, () => TimeSpan.FromDays(1), null, network) { Gap = TimeSpan.Zero };
            var asked = 0;
            // The drainer found the queue empty and is about to end: item 2 asked right then (the window of the audit).
            service.Idle = () => { if (Interlocked.Increment(ref asked) == 1) service.Get(2); };
            service.Get(1);
            Until(() => service.Peek(2) is { Loading: false });
            Check(service.Peek(1) is { Loading: false } && service.Peek(2) is { Loading: false } && network.Calls == 2, "both items ended, each asked once: " + service.Peek(2));
        }),
        ("Network ends 1556: A11, PaissaDB: world A's plots never shown under world B, loading or failed; A24: only the world's id sent, once", () =>
        {
            var gate = new TaskCompletionSource<HttpResponseMessage>();
            var network = new Network(request => request.RequestUri!.AbsolutePath.EndsWith("/40", StringComparison.Ordinal) ? Network.Text(World) : gate.Task);
            using var service = new PaissaDbService(new HttpClient(network));
            service.Request(40);
            Until(() => !service.State.Loading);
            Check(service.State is { WorldId: 40, Plots.Count: 1, Error: null }, "world 40 read");
            service.Request(41);
            Check(service.State is { WorldId: 41, Loading: true, Plots: null, ReadAt: null }, "loading world 41: no plots of world 40 under it");
            service.Request(41);
            gate.SetResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            Until(() => !service.State.Loading);
            Check(service.State is { WorldId: 41, Loading: false, Plots: null, ReadAt: null, Error.Length: > 0 }, "world 41 refused: an error, no plots of world 40");
            // A24: the transport saw exactly one GET per read, the world's id in the path, no body, no header but Tensho's User-Agent.
            Check(network.Sent.Count == 2 && network.Sent[0] is ("GET", "https://paissadb.zhu.codes/worlds/40", false, _) && network.Sent[1] is ("GET", "https://paissadb.zhu.codes/worlds/41", false, _),
                "one GET per world, nothing else: " + string.Join(" ", network.Sent.Select(sent => sent.Method + " " + sent.Address)));
            Check(network.Sent.All(sent => sent.Headers.SequenceEqual(["User-Agent"])), "no header but the User-Agent");
            // Back to world 40 after the failure: its own last list is not kept (it is read again), never world 41's error under it.
            service.Request(40);
            Until(() => !service.State.Loading);
            Check(service.State is { WorldId: 40, Plots.Count: 1, Error: null } && network.Calls == 3, "world 40 read again");
        }),
        ("Network ends 1556: A02, a plot alert scheduled before the competitive cut is not printed once Tensho is cut", () =>
        {
            var cut = false;
            var queued = new Queue<Action>();
            var printed = new List<string>();
            var alert = PaissaDb.Alert(() => cut, queued.Enqueue, printed.Add);
            alert("plot 1");
            cut = true;                       // ranked content entered before the game's thread runs the line
            alert("plot 2");
            while (queued.TryDequeue(out var run)) run();
            Check(printed.Count == 0, "nothing printed while cut");
            cut = false;
            alert("plot 3");
            while (queued.TryDequeue(out var run)) run();
            Check(printed.SequenceEqual(["plot 3"]), "printed once the cut is over");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.PlotAlert=Community.PaissaDb.Alert(()=>Competitive.CompetitiveGate.Cut,", StringComparison.Ordinal), "the plugin's alert reads the cut when printing");
        }),
        ("Network ends 1556: A07, LuminaSupplemental: turned off during its batch, no further file asked; asked again once back on", () =>
        {
            var enabled = true;
            var network = new Network(_ => { enabled = false; return Network.Text("Id\n"); });
            using var service = new LuminaSupplementalService(() => TimeSpan.FromDays(1), null, network) { Enabled = () => enabled };
            service.Request();
            Until(() => network.Calls > 0);
            Thread.Sleep(300);
            Check(network.Calls == 1, "one file asked, none after the switch: " + network.Calls);
            enabled = true;
            Check(!service.State.Loading, "not left loading");
            service.Request();
            Until(() => network.Calls > 1);
            Check(network.Calls == 2, "asked again once the source is back on, without the 5 minutes' pause");
        }),
        ("Network ends 1556: A07, a wiki guide's second try is not sent once the wiki is turned off", () =>
        {
            var enabled = true;
            var network = new Network(_ => { enabled = false; return Network.Text("{\"error\":{\"code\":\"missingtitle\"}}"); });
            using var guides = new WikiDutyGuides(_ => "The Palace of the Dead", () => TimeSpan.FromDays(1), null, new HttpClient(network)) { Enabled = () => enabled };
            guides.Select(1);
            guides.RequestAsync().Wait();
            Check(network.Calls == 1 && guides.State is { Loading: false, Error: { } error } && error.EndsWith(SourceDisabledException.Suffix, StringComparison.Ordinal),
                "one request, then « turned off »: " + network.Calls + " " + guides.State.Error);
        }),
    ];
}
