using System.Net;
using TenshoAssistant.Community;

// X-313 (0.1317): user's remark of 14/11 (test T-0.1227-1): « Added in patch 7.x (Garland Tools) » was in an item sheet's header but not in its
// tooltip. The common item tooltip shows it from what Garland already gave (Peek: never a request on hover); Garland off: no line.
internal static class ItemTooltipPatch1317Tests
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

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-313: a patch is written as the game writes it", () =>
        {
            Check(GarlandItemSourceParser.PatchLabel(7.4) == "7.4", "7.4");
            Check(GarlandItemSourceParser.PatchLabel(6.55) == "6.55", "6.55");
            Check(GarlandItemSourceParser.PatchLabel(1.0) == "1.0", "1.0 keeps its decimal");
        }),
        ("X-313: the patch is known from what Garland already gave, hovering asks nothing, and Garland off gives no line", () =>
        {
            var stub = new StubGarland("{\"item\":{\"id\":46246,\"name\":\"x\",\"patch\":7.4},\"partials\":[]}");
            var enabled = true;
            using var client = new GarlandClient(new HttpClient(stub)) { Enabled = () => enabled };
            using var service = new GarlandItemSourceService(client) { Enabled = () => enabled };
            Check(service.Peek(46246) is null && stub.Calls == 0, "nothing known yet, and Peek (the hover) asks nothing");
            service.Get(46246);
            var known = WaitKnown(service, 46246);
            Check(known?.List?.Patch == 7.4, $"the sources' read gives the patch ({known?.Error ?? "no error"})");
            Check(stub.Calls == 1, "one request, from the sources' read only");
            for (var hover = 0; hover < 50; hover++) Check(service.Peek(46246)?.List?.Patch == 7.4, "every hover reads the known patch");
            Check(service.Peek(5057) is null && stub.Calls == 1, "another item hovered: nothing known, no request");
            enabled = false;
            Check(service.Peek(46246) is null, "Garland off: no patch line");
            enabled = true;
            Check(service.Peek(46246)?.List?.Patch == 7.4 && stub.Calls == 1, "back on: known again, still no request");
        }),
        ("X-313: the common item tooltip and the item sheet write the patch the one way", () =>
        {
            var hover = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketHover.cs"));
            var tooltip = hover[hover.IndexOf("private void DrawItemInfoTooltip(", StringComparison.Ordinal)..];
            tooltip = tooltip[..tooltip.IndexOf("DrawItemInfoBody(item,id);", StringComparison.Ordinal)];
            // X-363 (0.1350): the tooltip asks once the mouse rested on the item (ItemPatchAsk1350Tests).
            Check(tooltip.Contains("if(ItemPatchText(id,ask:patchDwell.Settled(id,Environment.TickCount64)) is { } patchText)ImGui.TextDisabled(patchText);", StringComparison.Ordinal), "the tooltip shows it");
            var sheet = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ItemSheet.cs"));
            Check(sheet.Contains("if(ItemPatchText(id,ask:true) is { } patchText)facts.Add(patchText);", StringComparison.Ordinal), "the sheet uses the same helper");
            Check(sheet.Split("\"ItemSheet.Patch\"").Length == 2 && !hover.Contains("\"ItemSheet.Patch\"", StringComparison.Ordinal), "the text is written in one place only");
            // X-363 (0.1350): Peek only hid the line for every item with a source known from the game data; Get when asked.
            Check(sheet.Contains("(ask ? itemSources?.Get(id):itemSources?.Peek(id))?.List?.Patch is { } patch ? localization.Format(\"ItemSheet.Patch\",Community.GarlandItemSourceParser.PatchLabel(patch)):null", StringComparison.Ordinal), "Get when asked, Peek otherwise");
        }),
    ];
}
