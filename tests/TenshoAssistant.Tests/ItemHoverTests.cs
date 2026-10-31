using System.Net;
using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Items;
using TenshoAssistant.Market;

internal static class ItemHoverTests
{
    private static ItemCatalog Catalog()=>ItemCatalog.Build([new(42,"Marketable",true,IsMarketable:true),new(43,"Bound",true),new(44,"NQ only",false,IsMarketable:true)],new("fixture","French","v",1));
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static HttpResponseMessage Response()=>new(HttpStatusCode.OK){Content=new StringContent("{\"results\":[{\"itemId\":42}]}")};
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
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
        })
    };
}
