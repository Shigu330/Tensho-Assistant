// 0.1669 (X-684, first part: items; the user's request of 20/11 around 21:52: « every tooltip gives everything Tensho knows »): the item
// tooltips written by hand go through the common item tooltip (DrawItemInfoTooltip), with what the caller knows besides in its footer; the
// item's name is no longer written again under it; the quality is given where it is known.
internal static class ItemTips1669Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item tips 1669: the sort preview, the events' rewards, the baits and the retainer lists use the common tooltip",()=>
        {
            Check(Read("MainWindow.SortPreview.cs").Contains("DrawItemInfoTooltip(stack.Item.Id.Value,hq:stack.Item.IsHighQuality);\n                                        WrappedTooltip("),"sort preview: common, then the slot");
            Check(!Read("MainWindow.Events.cs").Contains("WrappedTooltip(definition?.Name"),"events: the name not written again");
            Check(!Read("MainWindow.FishingPanel.cs").Contains("WrappedTooltip((bait?.Name") && !Read("MainWindow.OceanVoyagePanel.cs").Contains("WrappedTooltip((bait?.Name"),"baits: the name not written again");
            Check(Read("MainWindow.RetainerSales.cs").Contains("ItemTip(listing.ItemId, hq: listing.Hq);") && Read("MainWindow.RetainerSaleList.cs").Contains("ItemTip(row.Item, hq: row.Hq);"),"retainer lists: the quality");
        }),
    ];
}
