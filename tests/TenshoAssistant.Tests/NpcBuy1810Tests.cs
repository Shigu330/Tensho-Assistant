using TenshoAssistant.Inventory;

// X-797 (0.1810, the user's test T-0.1782-2 of 22/11): the market compared with buying from an NPC too, and the line about the NPC's buying
// price says it is the price of selling to an NPC.
internal static class NpcBuy1810Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("NPC buy 1810: the market less the NPC's selling price, a piece; unknown when either is unknown",() =>
        {
            Check(StockValue.MarketOverNpcBuy(120,400)==280,"dark matter grade VI (the user's capture): 280 cheaper a piece at the NPC");
            Check(StockValue.MarketOverNpcBuy(500,400)==-100,"the market cheaper: below 0");
            Check(StockValue.MarketOverNpcBuy(null,400) is null && StockValue.MarketOverNpcBuy(0,400) is null && StockValue.MarketOverNpcBuy(120,null) is null && StockValue.MarketOverNpcBuy(120,0) is null,"no NPC sells it, or no market price: nothing said");
            Check(StockValue.MarketOverNpc(4,400)==100,"the selling-to-NPC ratio unchanged");
        }),
        ("NPC buy 1810: the « sits and sells » lines carry the NPC's selling price",() =>
        {
            static InventoryStack S(uint id,long quantity)=>new(new InventoryItem(new InventoryItemId(id),false),quantity);
            var (lines,_)=StockValue.Sleepers([("Bags",S(1,10)),("Bags",S(2,3))],_=>true,(id,_)=>(id==1 ? 400m:50m,1m),id=>id==1 ? 4u:0u,5,id=>id==1 ? 120u:null);
            Check(lines.Single(line=>line.ItemId==1) is { NpcPrice:4,NpcBuy:120 },"item 1: sold to an NPC 4, bought from an NPC 120");
            Check(lines.Single(line=>line.ItemId==2).NpcBuy is null,"no NPC sells it: null (« — »)");
            var (old,_)=StockValue.Sleepers([("Bags",S(1,10))],_=>true,(_,_)=>(400m,1m),_=>4u,5);
            Check(old[0].NpcBuy is null,"without the vendors: nothing guessed");
        }),
        ("NPC buy 1810: the tooltip and the list say both prices, the selling one named as such",() =>
        {
            var hover=Read("src/TenshoAssistant/MainWindow.MarketHover.cs");
            Check(hover.Contains("npcVendors?.State.Catalog?.GilPrice(id) is { } npcBuy") && hover.Contains("localization.Format(\"HoverUi.NpcBuy\""),"the NPC's selling price under its buying price");
            Check(hover.Contains("Inventory.StockValue.MarketOverNpcBuy(npcVendors?.State.Catalog?.GilPrice(hovered.ItemId),Inventory.StockValue.FromQuote(prices,false)?.Unit)"),"compared with the market's normal quality (what the NPC sells)");
            var page=Read("src/TenshoAssistant/MainWindow.StockValue.cs");
            Check(page.Contains("BeginDataTable(\"StockSleepers\",8)") && page.Contains("localization.Get(\"StockValue.ColumnNpcBuy\")") && page.Contains("id=>npcVendors?.State.Catalog?.GilPrice(id)"),"a column of its own");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(fr.Contains("\"MarketOverNpc\": \"Le marché paie {0} fois le prix de vente au PNJ.\"") && en.Contains("what an NPC gives you for it"),"the ratio says it is the selling price");
            foreach(var (name,text) in new[]{("en",en),("fr",fr)})
                foreach(var key in new[]{"NpcBuy","NpcBuyCheaper","MarketBelowNpcBuy","ColumnNpcBuy"})Check(text.Contains($"\n    \"{key}\": \""),$"{name}: {key}");
        }),
    ];
}
