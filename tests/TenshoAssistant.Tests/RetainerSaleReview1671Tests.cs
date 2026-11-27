using TenshoAssistant.Items;
using TenshoAssistant.Market;

// 0.1671 (X-698, review of 0.1665 to 0.1670): the retainer sale list. M1: its own Universalis service (the panel's answer no longer wipes
// the list's prices, nor the reverse); M2: its answers add up and the items to ask come in order (not answered, no price, most valuable);
// M3: the answer's size limit follows the items asked; M4: the city's market tax when the game sent it, the help's 5 % otherwise.
internal static class RetainerSaleReview1671Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset At=new(2026,11,21,2,30,0,TimeSpan.Zero);
    private static RetainerItemOffers Offers(long price)=>new([new MarketOffer(price,1,false,"World","Seller",0,At)],At);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Retainer sale review 1671: the panel's answer replaces its own only; the list's answers add up on the same world, an error keeps them",()=>
        {
            var before=new RetainerCompareState(true,new Dictionary<uint,RetainerItemOffers>{[1]=Offers(10)},null,At.AddMinutes(-5),"World");
            var panel=RetainerCompareService.Merged(before,"World",[2],new Dictionary<uint,RetainerItemOffers>{[2]=Offers(20)},null,At,false);
            Check(panel.Items!.Count==1 && panel.Items.ContainsKey(2) && panel.FetchedAt==At && !panel.Loading,"the panel: the answer alone, as before");
            var list=RetainerCompareService.Merged(before,"World",[2,3],new Dictionary<uint,RetainerItemOffers>{[2]=Offers(20)},null,At,true);
            Check(list.Items!.Count==3 && list.Items[1].Offers[0].UnitPrice==10 && list.Items[2].Offers[0].UnitPrice==20,"the list: the first answer kept, the new one added");
            Check(list.Items[3] is { DataAt:null } missing && missing.Offers.Count==0,"an item asked and not answered: « no data », never asked first again");
            var failed=RetainerCompareService.Merged(list,"World",[4],null,"Network",null,true);
            Check(failed.Items!.Count==3 && failed.Error=="Network" && failed.FetchedAt==At && !failed.Loading,"an error keeps what the list knew");
            var panelFailed=RetainerCompareService.Merged(before,"World",[4],null,"Data",null,false);
            Check(panelFailed.Items is null && panelFailed.Error=="Data","the panel's error: as before");
            var plugin=Read("src/TenshoAssistant/Plugin.cs")+Read("src/TenshoAssistant/Plugin.Market.cs");
            Check(plugin.Contains("mainWindow.RetainerSaleListCompare=retainerSaleListCompare=new Market.RetainerCompareService{Accumulate=true,")
                  && plugin.Contains("mainWindow.RetainerCompare=retainerCompare=new Market.RetainerCompareService{Enabled="),"two services: the panel's and the list's");
            var service=Read("src/TenshoAssistant/Market/RetainerSales.cs");
            Check(service.Contains("Items = Accumulate && state.World != world ? null : state.Items"),"another world: the list starts again");
        }),
        ("Retainer sale review 1671: the items to ask: not answered first, then without a price, then the most valuable; 100 at most",()=>
        {
            var stacks=Enumerable.Range(1,150).Select(i=>((uint)i,false,(long)i)).ToList();
            ItemDefinition Find(uint item)=>new(item,"x",false,IsMarketable:true);
            SalePrice? Price(uint item,bool hq)=>item<=20 ? new SalePrice(10,SaleSource.Board,At):null;
            var first=RetainerSaleList.Build(stacks,Find,Price);
            Check(first.ToAsk.Count==RetainerSaleList.Asked && first.ToAsk[0]==150 && first.ToAsk.All(item=>item>20),"without a price first, the largest quantity first");
            var asked=first.ToAsk.ToHashSet();
            var second=RetainerSaleList.Build(stacks,Find,Price,answered:item=>asked.Contains(item));
            Check(second.ToAsk.Take(50).All(item=>!asked.Contains(item)) && second.ToAsk.Take(30).All(item=>item>20) && second.ToAsk.Skip(30).Take(20).SequenceEqual(Enumerable.Range(1,20).Reverse().Select(i=>(uint)i)),
                "the next click: the 50 not answered yet come first, the 30 without a price then the 20 priced ones, the best total first");
        }),
        ("Retainer sale review 1671: the answer's size limit grows with the items asked (64 KB an item, 4 MB at least)",()=>
        {
            Check(RetainerCompareService.MaximumBytes(20)==4*1024*1024 && RetainerCompareService.MaximumBytes(100)==100*64*1024,"4 MB for the panel's 20, 6.4 MB for 100");
            Check(Read("src/TenshoAssistant/Market/RetainerSales.cs").Contains("MaximumBytes(asked.Count), cancellation.Token"),"the limit used is the one for the items asked");
        }),
        ("Retainer sale review 1671: the city's tax when the game sent it and it holds; the help's 5 % otherwise",()=>
        {
            var rates=MarketTax.From([5,5,5,5,5,3,5,0],At.AddDays(2))!;
            Check(MarketTax.RateFor(rates,10,At)==3 && MarketTax.RateFor(rates,14,At)==0 && MarketTax.RateFor(rates,1,At)==5,"Crystarium 3 %, Tuliyollal 0 %, Limsa Lominsa 5 %");
            Check(MarketTax.RateFor(rates,10,At.AddDays(3)) is null && MarketTax.RateFor(rates,0,At) is null && MarketTax.RateFor(null,10,At) is null,"expired, unknown town or no rates: null");
            Check(MarketTax.From([5,5,5,5,5,101,5,5],At) is null && MarketTax.From([5,5],At) is null,"out of shape: not kept");
            Check(RetainerSaleList.Net(100,1,3)==97 && RetainerSaleList.Net(100,1,0)==100 && RetainerSaleList.Net(100,1)==95,"the total after the rate");
            var list=RetainerSaleList.Build([(1,false,10)],item=>new ItemDefinition(item,"x",false,IsMarketable:true,SellPrice:95),(item,hq)=>new SalePrice(10,SaleSource.Board,At),taxPercent:0);
            Check(list.Rows[0] is { Total:100,Npc:950 } && list.Rows[0].NpcBetter,"the rate given is the one used (the NPC's 950 against the market's 100)");
            var window=Read("src/TenshoAssistant/MainWindow.RetainerSaleList.cs");
            // X-700 M1 (0.1686): any retainer whose town was read takes its city's rate (Review1676Fixes1686Tests), no longer the open one alone.
            Check(window.Contains("MarketTax.ForRetainer(MarketTaxRatesSource?.Invoke(), state.Towns, shown.Key.StorageId, DateTimeOffset.UtcNow)"),"the retainer's city; the help's rate otherwise, with the reason");
        }),
    ];
}
