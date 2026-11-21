using TenshoAssistant;
using TenshoAssistant.Items;

// X-728 (0.1732, the user's report of 21/11): Tensho's menu on the game's gathering log (GatheringNote) and fishing log (FishGuide2): the
// windows are item windows (their menu « name set » is not a character's); the hovered ids of the log of 21/11 replayed through the menu's
// session: 538937 → 38937 (collectable, offset 500 000), 36178 → 36178, 536300 → 36300; an HQ offset (1 000 000) too.
internal static class GatheringLogMenu1732Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static ItemCatalog Catalog() => ItemCatalog.Build([new(38937, "Collectable ore", false), new(36178, "Log", false), new(36300, "Fish", false), new(5111, "HQ-capable", true)], new("fixture", "French", "v", 1));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gathering log menu 1732: the hovered ids of 21/11 become the item, offsets removed", () =>
        {
            using var session = new ItemContextMenuSession(); var catalog = Catalog(); var now = DateTimeOffset.UtcNow;
            foreach (var (raw, id, kind) in new (ulong, uint, ItemHoverKind)[] { (538937, 38937, ItemHoverKind.Collectible), (36178, 36178, ItemHoverKind.Normal), (536300, 36300, ItemHoverKind.Collectible), (1005111, 5111, ItemHoverKind.Hq) })
            {
                var token = session.Begin(true, 7, raw, catalog, now);
                Check(token is not null && token.Item.ItemId == id && token.Item.Kind == kind, raw + " → " + (token?.Item.ItemId.ToString() ?? "nothing"));
            }
            Check(session.Begin(true, 7, 543919, catalog, now) is null, "an id the catalogue does not hold: no menu");
        }),
        ("Gathering log menu 1732: the gathering and fishing logs are item windows; characters still skipped elsewhere", () =>
        {
            var bridge = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemContextMenuBridge.Dalamud.cs"));
            var windows = bridge[bridge.IndexOf("ItemWindows=", StringComparison.Ordinal)..];
            windows = windows[..windows.IndexOf("};", StringComparison.Ordinal)];
            Check(windows.Contains("\"GatheringNote\"", StringComparison.Ordinal) && windows.Contains("\"FishGuide2\"", StringComparison.Ordinal), "the two logs");
            Check(bridge.Contains("var itemWindow=ItemWindows.Contains(args.AddonName ?? \"\");", StringComparison.Ordinal)
                && bridge.Contains("|| !itemWindow && !chatItem && (target.TargetContentId!=0 || target.TargetObject is not null || !string.IsNullOrEmpty(target.TargetName)))return;", StringComparison.Ordinal), "an item window's « name set » is not a character's");
            Check(bridge.Contains("var token=session.Begin(true,owner,raw,catalog.State.Catalog,DateTimeOffset.UtcNow);", StringComparison.Ordinal), "the hovered id goes through the session (Normalize)");
        }),
    ];
}
