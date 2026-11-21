using TenshoAssistant.Items;
using TenshoAssistant.Market;

// 0.1667 (X-695, the user's request of 20/11 around 23:00): a retainer's 20 most profitable items to sell: market price × quantity minus the
// 5 % tax (the game's help), beside the NPC's price; items the market does not take left out; a price not known is unknown, never 0.
internal static class RetainerSaleList1667Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset At=new(2026,11,21,1,0,0,TimeSpan.Zero);
    private static ItemDefinition? Find(uint item)=>item switch
    {
        1=>new ItemDefinition(1,"Ore",true,IsMarketable:true,SellPrice:10),
        2=>new ItemDefinition(2,"Potion",true,IsMarketable:true,SellPrice:500),
        3=>new ItemDefinition(3,"Quest key",false,IsMarketable:false,SellPrice:0),
        4=>new ItemDefinition(4,"Unseen",false,IsMarketable:true,SellPrice:3),
        _=>null,
    };

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Retainer sale list 1667: total after the 5 % tax, stacks added, sorted by total; the NPC's price beside; untradable and unknown items",()=>
        {
            SalePrice? Price(uint item,bool hq)=>(item,hq) switch{(1,false)=>new(100,SaleSource.Board,At),(1,true)=>new(300,SaleSource.Universalis,At),(2,false)=>new(520,SaleSource.Board,At),(3,false)=>new(9999,SaleSource.Board,At),_=>null};
            var list=RetainerSaleList.Build([(1,false,40),(1,false,59),(1,true,5),(2,false,1),(3,false,1),(4,false,7),(0,false,3)],Find,Price);
            Check(list.Rows.Count==3 && list.Rows[0] is { Item:1,Hq:false,Quantity:99,Total:9405,Npc:990 },"ore: 100 × 99 = 9 900 minus 5 % = 9 405; NPC 990");
            Check(list.Rows[1] is { Item:1,Hq:true,Total:1425,Npc:null } && list.Rows[2] is { Item:2,Total:494,Npc:500 },"HQ ore: no NPC price; potion: 494 after tax");
            Check(list.Rows[2].NpcBetter && !list.Rows[0].NpcBetter,"the NPC pays more for the potion");
            Check(list.Rows.All(row=>row.Item!=3),"an item the market does not take: left out, whatever its price");
            Check(list.Unknown==1 && list.ToAsk.SequenceEqual(new uint[]{1,2,4}),"one marketable item without a price (unknown, never 0); the items to ask Universalis");
        }),
        ("Retainer sale list 1667: 20 at most, the most profitable; the newer price; the retainer's pages only",()=>
        {
            var many=Enumerable.Range(1,30).Select(i=>((uint)i,false,(long)i)).ToList();
            var list=RetainerSaleList.Build(many,item=>new ItemDefinition(item,"x",false,IsMarketable:true),(item,hq)=>new SalePrice(10,SaleSource.Board,At));
            Check(list.Rows.Count==RetainerSaleList.Shown && list.Rows[0].Item==30 && list.Rows[^1].Item==11,"top 20 by total");
            var older=new SalePrice(50,SaleSource.Board,At.AddHours(-2));var newer=new SalePrice(70,SaleSource.Universalis,At);
            Check(RetainerSaleList.Newest(older,newer)==newer && RetainerSaleList.Newest(new SalePrice(40,SaleSource.Board,At),newer)!.Source==SaleSource.Board && RetainerSaleList.Newest(null,null) is null,"the newer price; the same age: the board");
            Check(RetainerSaleList.IsRetainerPage(10000) && RetainerSaleList.IsRetainerPage(10006) && !RetainerSaleList.IsRetainerPage(12000) && !RetainerSaleList.IsRetainerPage(12001),"pages only, not the money nor the crystals");
            Check(RetainerSaleList.Net(1,1)==0 && RetainerSaleList.Net(100,1)==95,"rounded down");
        }),
        ("Retainer sale list 1667: a button of the retainer sale panel; Universalis on the click only; nothing put up for sale",()=>
        {
            var panel=Read("src/TenshoAssistant/MainWindow.RetainerSales.cs");
            var window=Read("src/TenshoAssistant/MainWindow.RetainerSaleList.cs");
            Check(panel.Contains("DrawRetainerSaleListButton(state);") && window.Split("RetainerCompare?.Request(").Length==2
                  && window.Contains("if (ImGui.SmallButton(localization.Get(\"RetainerSales.SaleListPrices\")))\n                RetainerCompare?.Request(state.HomeWorld, list.ToAsk, DateTimeOffset.UtcNow, RetainerSaleList.Asked);"),"button; Universalis asked on its click only");
            Check(window.Contains("SourceOn(SourceUse.UniversalisRetainerCompare)") && !window.Contains("SetRetainerMarketPrice"),"source setting; no game call");
            Check(Read("src/TenshoAssistant/Market/RetainerSales.cs").Contains("Math.Clamp(maximum, 1, RetainerSaleList.Asked)"),"100 at most");
        }),
    ];
}
