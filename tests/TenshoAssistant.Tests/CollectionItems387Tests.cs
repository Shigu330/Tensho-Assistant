// 0.387 (user report: the 0.365 collection tabs had no « how to obtain » tooltip): the teaching item from the game data (hairstyle,
// fashion accessory 20086, barding 1013, master book), its item tooltip and sources menu; otherwise the collection tooltip says it is
// unknown; outfits and their pieces show the item tooltip.
internal static class CollectionItems387Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collections: teaching items of the 0.365 tabs, item tooltip and sources, unknown said, outfits hovered",() =>
        {
            var reader = Read("Collections/ExtraCollectionReader.Dalamud.cs");
            Check(reader.Contains("use.Action.RowId is 20086 or 1013",StringComparison.Ordinal) && reader.Contains("ItemId:TeachingItem(20086,row.RowId)",StringComparison.Ordinal)
                && reader.Contains("ItemId:TeachingItem(1013,row.RowId)",StringComparison.Ordinal) && reader.Contains("ItemId:group.Key",StringComparison.Ordinal)
                && reader.Contains("ItemId:row.Item.RowId",StringComparison.Ordinal),"teaching items");
            var page = Read("MainWindow.Collections.cs");
            Check(page.Contains("else if(entry.ItemId!=0)DrawItemInfoTooltip(entry.ItemId);",StringComparison.Ordinal)
                && page.Contains("else if(ExtraKinds.Contains(entry.Kind))DrawCollectionTooltip(entry);",StringComparison.Ordinal),"tooltips");
            Check(Read("MainWindow.SourceLinks.cs").Contains("_ when entry.ItemId!=0",StringComparison.Ordinal),"sources menu");
            Check(Read("MainWindow.MarketHover.cs").Contains("HoverUi.CollectionHowUnknown",StringComparison.Ordinal),"unknown said");
            var outfits = Read("MainWindow.Outfits.cs");
            Check(outfits.Contains("DrawItemInfoTooltip(row.Outfit.SetItemId)",StringComparison.Ordinal) && outfits.Contains("DrawItemInfoTooltip(piece)",StringComparison.Ordinal),"outfits");
        }),
    ];
}
