// Test audit of 2026-11-09, batch 2 (0.1021): Hover327Tests, HoverPrices343Tests merged into ItemHoverTests; every case is kept.
using System.Net;
using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Items;
using TenshoAssistant.Market;
using TenshoAssistant.UI;
internal static class ItemHoverTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool HasCode(string text,string code)=>System.Text.RegularExpressions.Regex.Replace(text,@"\s+","").Contains(System.Text.RegularExpressions.Regex.Replace(code,@"\s+",""),StringComparison.Ordinal);
    private static ItemCatalog Catalog()=>ItemCatalog.Build([new(42,"Marketable",true,IsMarketable:true),new(43,"Bound",true),new(44,"NQ only",false,IsMarketable:true)],new("fixture","French","v",1));
    private static HttpResponseMessage Response()=>new(HttpStatusCode.OK){Content=new StringContent("{\"results\":[{\"itemId\":42}]}")};
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));
    // A language line, trimmed and without its final comma (escapes kept: a new line reads « \n »).
    private static string Text(string code,string key) =>
        File.ReadLines(Path.Combine(Source,"Localization","Languages",code + ".json")).Select(line => line.Trim().TrimEnd(','))
            .FirstOrDefault(line => line.StartsWith("\"" + key + "\": ",StringComparison.Ordinal)) ?? "";
    private static string ReadFile(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    // Shape of a real Universalis aggregated answer (item 5057, world 97, read 2026-11-03), values shortened.
    private const string Answer = """
        {"results":[{"itemId":5057,
          "nq":{"minListing":{"world":{"price":89},"dc":{"price":45,"worldId":83},"region":{"price":40,"worldId":33}},
                "recentPurchase":{"world":{"price":71,"timestamp":1793612456000}},
                "averageSalePrice":{"world":{"price":104.09}},
                "dailySaleVelocity":{"world":{"quantity":93.3}}},
          "hq":{},
          "worldUploadTimes":[{"worldId":97,"timestamp":1793612456000}]}]}
        """;
    private static string ReadLf(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Common hover resolves NQ HQ collectible and bound items without market assumptions",()=>
        {
            var catalog=Catalog();
            Check(ItemHoverService.Normalize(42,catalog)==new ItemHoverItem(42,ItemHoverKind.Normal),"normal");
            Check(ItemHoverService.Normalize(1000042,catalog)==new ItemHoverItem(42,ItemHoverKind.Hq),"HQ");
            Check(ItemHoverService.Normalize(500044,catalog)==new ItemHoverItem(44,ItemHoverKind.Collectible),"collectible distinct from HQ");
            Check(ItemHoverService.Normalize(43,catalog)?.ItemId==43 && MarketHoverTracker.Normalize(43,catalog) is null,"bound item useful without price eligibility");
            foreach(var raw in new ulong[]{0,500000,1000000,2000042,ulong.MaxValue,99,1000044})Check(ItemHoverService.Normalize(raw,catalog) is null,"invalid unknown special or impossible HQ");
            Check(MarketHoverTracker.Normalize(500042,catalog) is null && ItemHoverService.Normalize(42,null) is null,"collectible never priced and missing catalogue unknown");
        }),
        ("Common hover owner freshness and stability are independent of world",()=>
        {
            using var context=new ItemHoverService();var catalog=Catalog();var now=DateTimeOffset.UtcNow;
            context.Observe(true,7,43,catalog,now);Check(!context.State.Stable(now.AddMilliseconds(599)) && context.State.Stable(now.AddMilliseconds(600)),"600ms stable context");
            context.Observe(true,7,43,catalog,now.AddSeconds(1));Check(context.State.StableSince==now,"same candidate preserves stability");
            Check(context.State.Fresh(now.AddSeconds(3)) && !context.State.Fresh(now.AddSeconds(3).AddTicks(1)) && !context.State.Fresh(now),"two-second age and future boundary");
            context.Observe(true,8,43,catalog,now.AddSeconds(3));Check(!context.State.Stable(now.AddSeconds(3)) && context.State.Owner==8,"new owner resets");
            context.Observe(true,0,43,catalog,now.AddSeconds(4));Check(context.State.Item is null && context.State.ObservedAt is null,"logout");
            context.Observe(false,8,43,catalog,now.AddSeconds(5));Check(!context.State.Enabled && context.State.Owner==0,"disabled");
        }),
        ("Common hover clock rollback clear and disposal cannot retain stale facts",()=>
        {
            var context=new ItemHoverService();var now=DateTimeOffset.UtcNow;
            context.Observe(true,1,42,Catalog(),now);context.Observe(true,1,42,Catalog(),now.AddSeconds(-1));
            Check(context.State.StableSince==now.AddSeconds(-1) && !context.State.Stable(now.AddSeconds(-1)),"clock reset restarts stability");
            context.Clear();Check(context.State.Item is null,"clear");context.Dispose();context.Observe(true,1,42,Catalog(),now);context.Dispose();
            Check(!context.State.Enabled && context.State.Item is null,"dispose terminal");
        }),
        ("Usage-only hover never calls the market even with eligible item and valid market context",()=>
        {
            var calls=0;using var http=new HttpClient(new MarketTestHandler((_,_)=>{calls++;return Task.FromResult(Response());}));
            using var market=new MarketService(http);market.SetWorld(7,71,"World");using var context=new ItemHoverService();using var coordinator=new ItemHoverCoordinator(context,market);
            var now=DateTimeOffset.UtcNow;
            foreach(var seconds in new[]{0,1,10,60})Check(coordinator.Observe(false,true,7,71,42,Catalog(),now.AddSeconds(seconds)) is null,"no price dispatch");
            Check(calls==0 && context.State.Stable(now.AddSeconds(60)),"local usages ready without HTTP");
            coordinator.Observe(false,true,7,0,43,Catalog(),now.AddSeconds(61));Check(context.State.Item?.ItemId==43 && calls==0,"no world and nonmarketable supported");
            coordinator.Observe(false,false,7,71,42,Catalog(),now.AddSeconds(62));Check(!context.State.Enabled && calls==0,"both off");
        }),
        ("Enabling price hover after stable usage restarts price delay and uses shared cache",()=>
        {
            var calls=0;using var http=new HttpClient(new MarketTestHandler((_,_)=>{calls++;return Task.FromResult(Response());}));
            using var market=new MarketService(http);market.SetWorld(7,71,"World");using var context=new ItemHoverService();using var coordinator=new ItemHoverCoordinator(context,market);
            var now=DateTimeOffset.UtcNow;coordinator.Observe(false,true,7,71,42,Catalog(),now);
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddSeconds(2)) is null,"new price opt-in starts delay");
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(2599)) is null && calls==0,"not before600");
            var task=coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(2600));Check(task is not null,"price dispatched at600");task!.GetAwaiter().GetResult();
            Check(calls==1 && market.GetCached(71,42) is not null,"shared market cache");
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddSeconds(8)) is null && calls==1,"fresh cache prevents second HTTP call");
        }),
        ("Refresh key (0.524): a press of one tick is answered, within the 5 s bound, and forgotten on another item",()=>
        {
            var calls=0;using var http=new HttpClient(new MarketTestHandler((_,_)=>{calls++;return Task.FromResult(Response());}));
            // 0.1202: the market's pause at 1.5 s instead of 3 (the same waits, half the time; the 3 s is checked by VerifySpeed1202Tests).
            using var market=new MarketService(http){RequestPause=TimeSpan.FromSeconds(1.5)};market.SetWorld(7,71,"World");using var context=new ItemHoverService();using var coordinator=new ItemHoverCoordinator(context,market);
            var now=DateTimeOffset.UtcNow;
            coordinator.Observe(true,true,7,71,42,Catalog(),now);
            coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(600))!.GetAwaiter().GetResult();
            Check(calls==1,"first price");
            // Ticks with a fresh price take nothing from the bound: the press after them gets through, as it almost never did before.
            for(var tick=0;tick<100;tick++)Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(5700+tick*16)) is null,"fresh: nothing asked");
            var pressed=coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(7400),true);
            Check(pressed is not null,"the press of one tick is answered");
            // The market's own pause (real time) is not the end of the press: it waits for the next slot, with no new press.
            Check(pressed!.GetAwaiter().GetResult().Status==MarketFetchStatus.Cooldown && calls==1,"inside the market's pause");
            Thread.Sleep(1600);
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(9000)) is null && calls==1,"inside the 5 s bound");
            var waited=coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(12400));
            Check(waited is not null,"then answered");waited!.GetAwaiter().GetResult();
            Check(calls==2,"asked again although fresh");
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(12500)) is null && calls==2,"one request for that press");
            // Another item forgets a press still waiting.
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(12600),true) is null,"waiting");
            coordinator.Observe(true,true,7,71,0,Catalog(),now.AddMilliseconds(12700));
            coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(12800));
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(20000)) is null && calls==2,"the press is gone, the price still fresh");
        }),
        ("Stopping only price hover rejects a late reply while preserving usage context",()=>
        {
            var response=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var http=new HttpClient(new MarketTestHandler((_,_)=>response.Task));using var market=new MarketService(http);market.SetWorld(7,71,"World");
            using var context=new ItemHoverService();using var coordinator=new ItemHoverCoordinator(context,market);var now=DateTimeOffset.UtcNow;
            coordinator.Observe(true,true,7,71,42,Catalog(),now);var task=coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(600))!;
            Check(market.BackgroundState?.Loading==true,"pending hover");coordinator.Observe(false,true,7,71,42,Catalog(),now.AddMilliseconds(700));
            response.SetResult(Response());task.GetAwaiter().GetResult();
            Check(market.GetCached(71,42) is null && market.BackgroundState is null && context.State.Stable(now.AddMilliseconds(700)),"late price rejected, usages stay ready");
        }),
        ("Hover coordinator scope checks prevent a request for mismatched owner or world",()=>
        {
            foreach(var scope in new[]{(Owner:8ul,World:71u),(Owner:7ul,World:72u)})
            {
                var calls=0;using var http=new HttpClient(new MarketTestHandler((_,_)=>{calls++;return Task.FromResult(Response());}));
                using var market=new MarketService(http);market.SetWorld(7,71,"World");using var context=new ItemHoverService();using var coordinator=new ItemHoverCoordinator(context,market);
                var now=DateTimeOffset.UtcNow;coordinator.Observe(true,true,scope.Owner,scope.World,42,Catalog(),now);
                var result=coordinator.Observe(true,true,scope.Owner,scope.World,42,Catalog(),now.AddMilliseconds(600))!.GetAwaiter().GetResult();
                Check(result.Status==MarketFetchStatus.ScopeChanged && calls==0,"scope checked before HTTP");
            }
        }),
        ("Hover coordinator preserves failure backoff and manual request ownership",()=>
        {
            var calls=0;using var failedHttp=new HttpClient(new MarketTestHandler((_,_)=>{calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));}));
            using var failed=new MarketService(failedHttp);failed.SetWorld(7,71,"World");using var context=new ItemHoverService();using var coordinator=new ItemHoverCoordinator(context,failed);
            var now=DateTimeOffset.UtcNow;coordinator.Observe(true,true,7,71,42,Catalog(),now);coordinator.Observe(true,true,7,71,42,Catalog(),now.AddMilliseconds(600))!.GetAwaiter().GetResult();
            Check(failed.BackgroundState?.RetryAt>=now.AddSeconds(30) && coordinator.Observe(true,true,7,71,42,Catalog(),now.AddSeconds(6)) is null && calls==1,"30s failure backoff kept before taking next request");
            var response=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var http=new HttpClient(new MarketTestHandler((_,_)=>response.Task));using var market=new MarketService(http);market.SetWorld(7,71,"World");
            using var manualContext=new ItemHoverService();using var manualCoordinator=new ItemHoverCoordinator(manualContext,market);
            var task=market.RequestAsync(42);manualCoordinator.Observe(false,true,7,71,43,Catalog(),now);manualCoordinator.Clear();
            Check(market.State.Loading,"usage and hover clear do not cancel manual work");response.SetResult(Response());task.GetAwaiter().GetResult();Check(market.State.Quote?.ItemId==42,"manual result preserved");
        }),
        ("Hover options remain independently opt-in across configuration v1 and service disposal",()=>
        {
            var config=JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
            Check(!config.ItemUsagesOnHover && !config.MarketPricesOnHover,"legacy defaults");config.ItemUsagesOnHover=true;
            var restored=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;Check(restored.ItemUsagesOnHover && !restored.MarketPricesOnHover,"usage flag never enables network flag");
            using var http=new HttpClient(new MarketTestHandler((_,_)=>throw new Exception("unexpected HTTP")));
            using var market=new MarketService(http);using var context=new ItemHoverService();
            var coordinator=new ItemHoverCoordinator(context,market);coordinator.Observe(false,true,7,0,43,Catalog(),DateTimeOffset.UtcNow);coordinator.Dispose();
            Check(coordinator.Observe(true,true,7,71,42,Catalog(),DateTimeOffset.UtcNow) is null && context.State.Item is null,"coordinator disposed terminal");
        }),
        // From Hover327Tests: 0.327 (queue step e, settings that need no decision, audit 2026-11-02): Market page scope saved, item hover panel hidden in duties,
        // the four parts of that panel. Every default keeps the 0.326 behaviour.
        ("Market page scope: data centre by default and for older files, saved, reset; the page and Settings share it",() =>
        {
            var fresh = new Configuration();
            Check(fresh.MarketBoardScope == MarketScope.DataCenter && ConfigurationReset.Reset.Contains("MarketBoardScope"),"default data centre, reset with the settings");
            Check(JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!.MarketBoardScope == MarketScope.DataCenter,"older file: data centre");
            fresh.MarketBoardScope = MarketScope.World;
            Check(JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(fresh))!.MarketBoardScope == MarketScope.World,"choice kept");
            var page = Read("MainWindow.MarketBoard.cs");
            Check(!page.Contains("marketScope",StringComparison.Ordinal),"no window-only scope left");
            Check(page.Contains("Enum.IsDefined(configuration.MarketBoardScope) ? configuration.MarketBoardScope:MarketScope.DataCenter",StringComparison.Ordinal),"unknown saved value: data centre");
            Check(page.Contains("MarketScopeShown==scope))ChooseMarketScope(scope);",StringComparison.Ordinal),"a click on the page saves it");
            var settings = Read("MainWindow.Settings.cs");
            // X-76 H5 (0.915): the shared combo row brings the tooltip and the « ↺ ».
            Check(settings.Contains("ComboSetting(\"Settings.MarketScope\",\"Settings.MarketScopeTip\",()=>MarketScopeShown,",StringComparison.Ordinal),"Settings row with tooltip and ↺");
            Check(Text("en","MarketScopeTip").Split("\\n")[^1].StartsWith("Default: ",StringComparison.Ordinal) && Text("fr","MarketScopeTip").Split("\\n")[^1].StartsWith("Par défaut : ",StringComparison.Ordinal),"tips end with the default");
        }),
        ("Hover hidden in duties: off by default, reset; in a duty nothing is observed, so no panel and no price request",() =>
        {
            var fresh = new Configuration();
            Check(!fresh.HoverHideInDuty && ConfigurationReset.Reset.Contains("HoverHideInDuty"),"off by default, reset with the settings");
            var reader = Read("Items/ItemHoverReader.Dalamud.cs");
            Check(reader.Contains("var hidden=configuration.HoverHideInDuty && InDuty();",StringComparison.Ordinal)
                && reader.Contains("owner!=0 && !hidden && !gui.GameUiHidden ? gui.HoveredItem:0;",StringComparison.Ordinal),"the reader stops in a duty");
            Check(Read("Plugin.cs").Contains("{ InDuty = () => mapLive?.State is { InDuty: true },",StringComparison.Ordinal),"same detection as the map's « Hide in duties »");
            Check(Read("MainWindow.Settings.cs").Contains("BoolSetting(\"Settings.HoverHideInDuty\",\"Settings.HoverHideInDutyTip\",",StringComparison.Ordinal),"shared row (tooltip and ↺)");
            // Managed half of the reader in a duty: « nothing hovered » clears the item, so the panel is not drawn and nothing is asked.
            using var http = new HttpClient(new MarketTestHandler((_,_) => throw new Exception("unexpected HTTP")));
            using var market = new MarketService(http);
            using var context = new ItemHoverService(); using var coordinator = new ItemHoverCoordinator(context,market);
            var catalog = ItemCatalog.Build([new(42,"Marketable",true,IsMarketable:true)],new("fixture","French","v",1));
            var now = DateTimeOffset.UtcNow;
            coordinator.Observe(false,true,7,71,42,catalog,now);
            Check(context.State.Fresh(now),"hovered outside a duty");
            Check(coordinator.Observe(true,true,7,71,0,catalog,now) is null && context.State.Item is null && !context.State.Fresh(now),"in a duty: no item, no panel, no request");
        }),
        ("Hover panel parts: all four on by default and for older files, reset; only the panel under the game's tooltip follows them",() =>
        {
            var fresh = new Configuration(); var legacy = JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
            foreach (var name in new[] { "HoverShowCollection","HoverShowStock","HoverShowUsages","HoverShowSources" })
            {
                var property = typeof(Configuration).GetProperty(name)!;
                Check((bool)property.GetValue(fresh)! && (bool)property.GetValue(legacy)!,"on by default: " + name);
                Check(ConfigurationReset.Reset.Contains(name),"reset with the settings: " + name);
            }
            var hover = Read("MainWindow.MarketHover.cs");
            Check(hover.Contains("DrawItemInfoBody(item,hovered.ItemId,hoverPanel:true);",StringComparison.Ordinal),"the hover panel follows the parts");
            Check(hover.Contains("            DrawItemInfoBody(item,id);",StringComparison.Ordinal) && Read("MainWindow.ItemSheet.cs").Contains("try{DrawItemInfoBody(item,id);}",StringComparison.Ordinal),"Tensho's item tooltip and the item sheet keep every part");
            Check(hover.Contains("if(hoverPanel && !configuration.HoverShowSources)return;",StringComparison.Ordinal),"no « How to get it » (nor its Garland / wiki requests) when hidden");
            Check(hover.Contains("observed.Stable(now) && HoverInfoShown(item,hovered.ItemId);",StringComparison.Ordinal),"no empty panel when nothing is left to draw");
            var settings = Read("MainWindow.Settings.cs");
            foreach (var key in new[] { "HoverCollection","HoverStock","HoverUsages","HoverSources" })
            {
                Check(settings.Contains("BoolSetting(\"Settings." + key + "\",\"Settings." + key + "Tip\",",StringComparison.Ordinal),"shared row (tooltip and ↺): " + key);
            }
        }),
        // From HoverPrices343Tests: 0.343 (g, I-01): complete hover price — last sale, average, sales per day, region cheapest, stack value, Ctrl+Alt refresh.
        ("Hover prices: Universalis velocity, average and region cheapest parsed; missing values stay unknown",() =>
        {
            var quote = MarketQuote.Parse(Answer,97,5057,DateTimeOffset.UnixEpoch);
            Check(quote.Nq.Minimum == 89 && quote.Nq.RecentSale == 71 && quote.Nq.Average == 104.09m && quote.Nq.Velocity == 93.3m,"world values");
            Check(quote.NqRegion is { Price: 40, WorldId: 33 } && quote.NqCheapest is { Price: 45, WorldId: 83 },"region and data center cheapest");
            Check(quote.Hq.Average is null && quote.Hq.Velocity is null && quote.HqRegion is null,"HQ absent: unknown, never zero");
            Check(ReadFile("Market/MarketService.cs").Contains("\"universalis-aggregated-v4\"",StringComparison.Ordinal),"disk copies without the new fields are fetched again");
        }),
        ("Hover prices: stack of the hovered slot kept only for that item; refresh asked once per Ctrl+Alt press, by the player",() =>
        {
            using var service = new ItemHoverService();
            var catalog = ItemCatalog.Build([new(5057,"Item",true,IsMarketable: true)],new("test","French","test",1));
            service.Observe(true,1,5057,catalog,DateTimeOffset.UnixEpoch);
            service.SetStack(42);
            Check(service.State.Stack == 42,"stack recorded");
            service.Observe(true,1,5057,catalog,DateTimeOffset.UnixEpoch.AddSeconds(1));
            Check(service.State.Stack == 42,"same item: kept");
            service.SetStack(0);
            Check(service.State.Stack is null,"zero is no stack");
            var reader = ReadFile("Items/ItemHoverReader.Dalamud.cs");
            Check(reader.Contains("held && !refreshHeld",StringComparison.Ordinal) && reader.Contains("configuration.HoverPriceRefresh",StringComparison.Ordinal),"one request per press, setting honoured");
            Check(reader.Contains("slot->ItemId==id",StringComparison.Ordinal) && reader.Contains("DetailKind.InventoryItem",StringComparison.Ordinal),"stack only from the player's own slot holding that item");
            var coordinator=ReadFile("Market/ItemHoverCoordinator.cs").Replace("\r\n","\n");
            Check(coordinator.Contains("refreshWanted|=refresh;") && HasCode(coordinator,"if(!refreshWanted && cached is not null && market.IsFresh(cached,now))return null;\n            if(!tracker.TakeRequest(now))return null;"),"0.524: the press waits for the 5 s bound, and only a real request takes it");
            Check(coordinator.Contains("market.CancelBackground();refreshWanted=false;"),"another item forgets the press");
            Check(ReadFile("MainWindow.MarketHover.cs").Contains("prices.FetchedAt.ToLocalTime().ToString(\"G\")"),"the seconds show that it changed");
        }),
        ("0.559: a material's tooltip says which storage holds it, from the rows already computed",() =>
        {
            Check(ReadLf("MainWindow.CraftPlan.cs").Contains("DrawStorageDetail(material.Holdings);") && ReadLf("MainWindow.CraftChain.cs").Contains("DrawStorageDetail(stored);"),"both tables");
            Check(ReadLf("MainWindow.CraftPlan.cs").Contains("row.IsCached && row.ObservedAt is { } seen"),"a saved read shows its date");
        }),
    ];
}
