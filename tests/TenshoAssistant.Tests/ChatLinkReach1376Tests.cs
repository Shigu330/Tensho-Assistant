using System.Numerics;
using TenshoAssistant.Maps;

// X-409 (0.1376, review of 0.1365 to 0.1374, I2): a right click anywhere on a wide chat map link opens the menu, wherever the mouse
// entered the link; a right click beyond the link's size is still refused; without readable bounds the 48 px reach of 0.1366 stays.
internal static class ChatLinkReach1376Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly HoveredMapLink Link = new(140, 20, 22.4f, 21.7f, "Thanalan occidental");
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat link reach 1376: the link's size from its bounds and the panel's scale", () =>
        {
            Check(ChatLinkHover.Extent(100, 200, 340, 222, 1.5f) == new Vector2(360, 33), "scaled size");
            Check(ChatLinkHover.Extent(100, 200, 340, 222, float.NaN) == new Vector2(240, 22), "a scale that is not a number is 1");
            Check(ChatLinkHover.Extent(0, 0, 0, 0, 1f) == Vector2.Zero, "empty bounds");
            Check(ChatLinkHover.Extent(10, 0, 5, 20, 1f) == Vector2.Zero, "reversed bounds");
            Check(ChatLinkHover.Extent(0, 0, 100_000, 10, 1f) == Vector2.Zero, "bounds beyond any chat line");
            Check(ChatLinkHover.Extent(int.MinValue, 0, int.MaxValue, 10, 1f) == Vector2.Zero, "no overflow");
        }),
        ("Chat link reach 1376: a right click far along a wide link, entered at one end, opens the menu", () =>
        {
            var hover = new ChatLinkHover();
            var entered = new Vector2(300, 900);
            var size = ChatLinkHover.Extent(0, 0, 240, 22, 1.5f);
            hover.Over(Link, 10_000, entered, size);
            Check(hover.RightClick(10_500, entered + new Vector2(340, 20), cut: false, enabled: true) == Link, "far end of the link, entered at the left end");
            Check(hover.RightClick(10_500, entered - new Vector2(340, 20), cut: false, enabled: true) == Link, "far end of the link, entered at the right end");
            Check(hover.RightClick(10_500, entered + new Vector2(0, 90), cut: false, enabled: true) is null, "three lines below: no menu");
            Check(hover.RightClick(10_500, entered + new Vector2(400, 0), cut: false, enabled: true) is null, "beyond the link's width: no menu");
            Check(hover.Describe(10_500, entered + new Vector2(400, 0))!.StartsWith("map 20 not taken: cursor 400 px away, beyond the link's 368 x 48 px", StringComparison.Ordinal), "traced with the reach");
        }),
        ("Chat link reach 1376: without readable bounds, the 48 px reach of 0.1366 stays", () =>
        {
            var hover = new ChatLinkHover();
            var entered = new Vector2(300, 900);
            hover.Over(Link, 10_000, entered);
            Check(hover.RightClick(10_500, entered + new Vector2(5, 3), cut: false, enabled: true) == Link, "near");
            Check(hover.RightClick(10_500, entered + new Vector2(40, 40), cut: false, enabled: true) is null, "57 px away");
            hover.Over(Link, 20_000, entered, Vector2.Zero);
            Check(hover.RightClick(20_500, entered + new Vector2(100, 0), cut: false, enabled: true) is null, "zero size: plain reach");
        }),
        ("Chat link reach 1376: the watcher gives the link's bounds and the panel's scale", () =>
        {
            var watcher = Read("src/TenshoAssistant/Maps/ChatLinkHoverWatcher.Dalamud.cs");
            Check(watcher.Contains("return ChatLinkHover.Extent(data->MinX,data->MinY,data->MaxX,data->MaxY,panel.Address==0 ? 1f:panel.Scale);"), "bounds and scale");
            Check(watcher.Contains("NativeMouse.Cursor(),link is null ? default:Extent(received.AtkEventData,args.Addon));"), "given with the hover");
        }),
    ];
}
