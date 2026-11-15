using TenshoAssistant.Link;

// 0.1445 (X-433): LocalLinkFeed's first pass made 17 JSON getters in a frame of the game (0.1440's names). The feed serializes one full
// snapshot off the game's thread when it is made, while the desktop link is on. The prepared line is a valid snapshot (every part read back).
internal static class LinkPrepare1445Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Link prepare 1445: the prepared snapshot has every part and reads back",() =>
        {
            var line = LocalLinkProtocol.Prepare();
            Check(line is { Length:>0 },"a line");
            Check(LocalLinkProtocol.TryParse(line,out var snapshot,out var error),"read back: " + error);
            Check(snapshot!.Live is not null && snapshot.Changes is { Count:1 } && snapshot.Notices is { Count:1 } && snapshot.Held is { Count:1 } && snapshot.State?.Measures is not null,"every part filled");
            foreach (var part in new[]{"\"Live\"","\"Changes\"","\"Notices\"","\"Held\"","\"State\"","\"Measures\""})Check(line!.Contains(part,StringComparison.Ordinal),part);
        }),
        ("Link prepare 1445: the feed prepares it off the game's thread, only while the link is on",() =>
        {
            var feed = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs")).Replace("\r\n","\n");
            Check(feed.Contains("if(configuration.DesktopLink)_=Task.Run(LocalLinkProtocol.Prepare);",StringComparison.Ordinal),"in the constructor, on a background thread");
        }),
    ];
}
