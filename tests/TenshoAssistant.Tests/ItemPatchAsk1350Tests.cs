using System.Net;
using TenshoAssistant.Community;

// X-363 (0.1350): test T-0.1317-1 of 14/11: « Added in patch 7.x (Garland Tools) » no longer showed for a recent vendor item (49731) in its
// tooltip nor in its sheet header: the line only read what Garland had given for the « How to get it » fallback, never asked for an item
// whose sources the game data knows. The sheet asks Garland for its item; the tooltip asks once the mouse rested on the item.
internal static class ItemPatchAsk1350Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class StubGarland(string json) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }

    private static GarlandItemSourceState? WaitKnown(GarlandItemSourceService service, uint id)
    {
        var deadline = Environment.TickCount64 + 30_000;
        while (Environment.TickCount64 < deadline)
        {
            if (service.Peek(id) is { Loading: false } state) return state;
            Thread.Sleep(20);
        }
        return service.Peek(id);
    }

    // The patch line as the window computes it (MainWindow.ItemPatchText): Get when asked, Peek otherwise.
    private static double? Line(GarlandItemSourceService service, uint id, bool ask) => (ask ? service.Get(id) : service.Peek(id))?.List?.Patch;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-363: a vendor item (sources known from the game, Garland never asked for them) gets its patch line when its sheet asks", () =>
        {
            // Shape of Garland's document for 49731 read on 14/11: patch 7.4, an NPC among the partials (a vendor item).
            var stub = new StubGarland("{\"item\":{\"id\":49731,\"name\":\"Bague d'attaquant de grand champion\",\"patch\":7.4},\"partials\":[{\"type\":\"npc\",\"id\":\"1049081\",\"obj\":{\"n\":\"Hhihwi\"}}]}");
            var enabled = true;
            using var client = new GarlandClient(new HttpClient(stub)) { Enabled = () => enabled };
            using var service = new GarlandItemSourceService(client) { Enabled = () => enabled };
            Check(Line(service, 49731, ask: false) is null && stub.Calls == 0, "the 0.1317 way (Peek only): no line and no request, the bug");
            Line(service, 49731, ask: true);
            var known = WaitKnown(service, 49731);
            Check(known?.List?.Patch == 7.4, $"the sheet's ask gives 7.4 ({known?.Error ?? "no error"})");
            for (var frame = 0; frame < 50; frame++) Check(Line(service, 49731, ask: true) == 7.4, "every frame reads the known patch");
            Check(stub.Calls == 1, "one request for the item, not one per frame");
            enabled = false;
            Check(Line(service, 49731, ask: true) is null && Line(service, 49731, ask: false) is null, "Garland off: no line");
            Check(Line(service, 5057, ask: true) is null && stub.Calls == 1, "Garland off: another item asks nothing");
            enabled = true;
            Check(Line(service, 49731, ask: false) == 7.4 && stub.Calls == 1, "back on: known again without a new request");
        }),
        ("X-363: the tooltip asks only once the mouse rested on the same item", () =>
        {
            var dwell = new HoverDwell(600);
            // The tooltip is drawn every frame (16 ms) while the mouse stays on the item; the answer of the last frame.
            static bool Hold(HoverDwell dwell, uint id, long from, long to) { var settled = false; for (var now = from; now <= to; now += 16) settled = dwell.Settled(id, now); return settled; }
            Check(!dwell.Settled(49731, 1_000), "first frame: not yet");
            Check(!Hold(dwell, 49731, 1_016, 1_500), "0.5 s: not yet");
            Check(Hold(dwell, 49731, 1_516, 1_612) && dwell.Settled(49731, 1_628), "0.6 s on the same item: asked, and it stays so");
            // A sweep over a list: a new item every frame, none rested on.
            var asked = 0;
            for (uint row = 1; row <= 40; row++) if (dwell.Settled(row, 1_640 + row * 16)) asked++;
            Check(asked == 0, "rows swept by the mouse are not asked");
            Check(!dwell.Settled(49731, 2_312), "back on the item: the wait starts again");
            Check(Hold(dwell, 49731, 2_328, 2_920), "rested again: asked");
            Check(!dwell.Settled(49731, 3_920), "tooltip gone for a second (no frame drawn): the wait starts again");
            Check(!dwell.Settled(0, 4_000) && !dwell.Settled(49731, 4_016), "no item: nothing, and the wait restarts");
        }),
        ("X-363: the item sheet asks, the tooltip asks after the rest, the text written once", () =>
        {
            var sheet = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ItemSheet.cs"));
            var hover = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketHover.cs"));
            Check(sheet.Contains("if(ItemPatchText(id,ask:true) is { } patchText)facts.Add(patchText);", StringComparison.Ordinal), "the sheet header asks");
            Check(hover.Contains("ItemPatchText(id,ask:patchDwell.Settled(id,Environment.TickCount64))", StringComparison.Ordinal), "the tooltip asks after the rest");
            Check(sheet.Contains("private readonly Community.HoverDwell patchDwell=new(600);", StringComparison.Ordinal), "0.6 s");
            Check(!sheet.Contains("ItemPatchText(id)", StringComparison.Ordinal) && !hover.Contains("ItemPatchText(id)", StringComparison.Ordinal), "no caller left on Peek alone");
        }),
    ];
}
