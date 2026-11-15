using System.Numerics;
using TenshoAssistant.Maps;

// X-419 (0.1393, review of 0.1383 to 0.1389, minors): M2 the server bar entries find their place by the label still on screen while the game
// has not drawn the new one; M3 the chat link's box is symmetric around the point where the mouse entered: one link width beside the link is
// inside (the in-game test now checks two widths); M4 the merge measure reports the largest kind file against the exchange's bounds.
internal static class ReviewMinors1393Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly HoveredMapLink Link = new(132, 2, 11.2f, 13.5f, "New Gridania");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1393: M2: each server bar entry falls back on the label still on screen", () =>
        {
            var info = Read("src/TenshoAssistant/Alarms/InfoDtrEntries.Dalamud.cs");
            var alarm = Read("src/TenshoAssistant/Alarms/AlarmDtrEntry.Dalamud.cs");
            var infoFind = info.IndexOf("var bounds=AlarmDtrEntry.FindTextNode(gui,shownLine.Label);", StringComparison.Ordinal);
            var infoBack = info.IndexOf("if(bounds is null && lines[index] is { } before && before.Label!=shownLine.Label)bounds=AlarmDtrEntry.FindTextNode(gui,before.Label);", StringComparison.Ordinal);
            var infoKept = info.IndexOf("lines[index]=new(shownLine.Label,shownLine.Tip,bounds);", StringComparison.Ordinal);
            Check(infoFind > 0 && infoBack > infoFind && infoKept > infoBack, "optional entries: the label before is read before it is replaced");
            var alarmBefore = alarm.IndexOf("var before=shownText;", StringComparison.Ordinal);
            var alarmBack = alarm.IndexOf("if(bounds is null && before.Length>0 && before!=shownText)bounds=FindTextNode(gui,before);", StringComparison.Ordinal);
            Check(alarmBefore > 0 && alarmBack > alarmBefore, "alarm entry: the same fallback");
        }),
        ("Review minors 1393: M3: the chat link's box: one link width beside it is inside, two widths are not", () =>
        {
            var size = ChatLinkHover.Extent(0, 0, 240, 22, 1f);
            var entered = new Vector2(500, 300);
            var hover = new ChatLinkHover();
            hover.Over(Link, 10_000, entered, size);
            Check(hover.RightClick(10_100, entered + new Vector2(120, 0), false, true) == Link, "on the link (entered by its left end)");
            Check(hover.RightClick(10_100, entered - new Vector2(240, 0), false, true) == Link, "one link width to the left: inside the symmetric box (width + 8)");
            Check(hover.RightClick(10_100, entered - new Vector2(480, 0), false, true) is null, "two link widths to the left: outside (the in-game test)");
        }),
        ("Review minors 1393: M4: the merge measure reports the largest kind file against the exchange's bounds", () =>
        {
            var load = Read("tools/discoveries-load.py");
            Check(load.Contains("\"largest_kind_facts\"") && load.Contains("\"largest_kind_mb\"") && load.Contains("\"largest_kind_read_mb\"")
                && load.Contains("merge.MAX_COMPRESSED") && load.Contains("merge.MAX_DECOMPRESSED"), "facts, bytes and bytes once read of the largest kind, with the bounds");
        }),
    ];
}
