using TenshoAssistant.Market;

// 0.346 (g, I-06 and I-07): NPC sale price in the item information; lowest offer minus 1 for the clipboard, own retainers left out.
internal static class Market346Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Undercut: lowest other offer minus 1, own retainers left out, at least 1 gil, nothing when only own offers",() =>
        {
            var own = new HashSet<ulong> { 7 };
            Check(Undercut.Price([new(500,false,1),new(450,true,2),new(400,false,7)],own) == (450u,449u,true),"own 400 ignored, 450 HQ is the lowest");
            Check(Undercut.Price([new(1,false,3)],own) == (1u,1u,false),"never under 1 gil");
            Check(Undercut.Price([new(400,false,7)],own) is null && Undercut.Price([],own) is null,"only own offers, or none: nothing copied");
            var watcher = Read("Market/UndercutWatcher.Dalamud.cs");
            Check(watcher.Contains("board.OfferingsReceived+=OnOfferings;",StringComparison.Ordinal) && watcher.Contains("board.OfferingsReceived-=OnOfferings;",StringComparison.Ordinal),"subscribed and unsubscribed");
            Check(watcher.Contains("!configuration.UndercutClipboard",StringComparison.Ordinal) && !new TenshoAssistant.Configuration().UndercutClipboard,"off by default, honoured");
            Check(Read("Plugin.cs").Contains("undercut?.FlushClipboard()",StringComparison.Ordinal),"clipboard written from the draw (ImGui)");
        }),
        ("NPC sale: unit price, and the hovered stack's, only for items sold to NPCs",() =>
        {
            var hover = Read("MainWindow.MarketHover.cs");
            Check(hover.Contains("if(item is { SellPrice:>0 } sold)",StringComparison.Ordinal) && hover.Contains("HoverUi.NpcSaleStack",StringComparison.Ordinal),"price 0 = not sold, stack line");
        }),
    ];
}
