using System.Net;
using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;
using TenshoAssistant.Market;

internal static class PreparationCostTests
{
    private static void Check(bool value,string message) {if(!value)throw new Exception(message);}
    private static ItemDefinition Item(uint id)=>new(id,$"Item {id}",true,IsMarketable:true);
    private static ItemHoldingsSummary Stock(long nq,long hq=0,bool observed=true,bool unknown=false,bool cached=false)=>new([],nq,hq,observed,unknown,cached,!unknown&&!cached);
    private static readonly DateTimeOffset Now=new(2026,10,28,12,0,0,TimeSpan.Zero);
    private static MarketQuote Quote(uint id,decimal? nq=10,decimal? hq=8)=>new(71,id,Now,Now,new(nq,null,null),new(hq,null,null));
    private static CraftPlan Plan(params CraftPlanMaterial[] materials)=>new(1,1,1,materials);
    private static PreparationCostEstimate Cost(CraftPlan plan,Func<uint,MarketQuote?>? quotes=null,PreparationCostBasis basis=PreparationCostBasis.Missing,
        PreparationPriceQuality quality=PreparationPriceQuality.Nq,Func<uint,ItemDefinition?>? items=null,uint world=71)
        =>PreparationCost.Build(plan,world,basis,quality,items??Item,quotes??(id=>Quote(id)),Now);
    private static HttpResponseMessage Reply(uint id)=>new(HttpStatusCode.OK) {Content=new StringContent("{\"results\":[{\"itemId\":"+id+",\"nq\":{\"minListing\":{\"world\":{\"price\":10}}}}]}")};
    private static PreparationPriceContext Context=>new(1,71,500,10,1,0,0);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Preparation cost combines counted NQ HQ stock and selected purchase quality without changing requirements",()=>
        {
            var plan=Plan(new(1,12,Stock(3,2)),new(2,4,Stock(0)));
            var result=Cost(plan,id=>id==1?Quote(id):Quote(id,2,3),quality:PreparationPriceQuality.Cheapest);
            Check(result.Lines[0].Quantity==7 && result.Lines[0].HighQuality==true && result.Lines[0].Cost==56 && result.KnownSubtotal==64 && result.AllCosted,"7 HQ at 8 plus 4 NQ at 2");
            var all=Cost(plan,id=>id==1?Quote(id):Quote(id,2,3),PreparationCostBasis.AllRequired,PreparationPriceQuality.Cheapest);
            Check(all.KnownSubtotal==104 && plan.Materials[0].Required==12,"buy-all mode explicitly ignores owned stock");
            try{((IList<PreparationCostLine>)result.Lines).Clear();throw new Exception("mutable result");}catch(NotSupportedException){}
        }),
        ("Preparation cost preserves unknown stock and partial or cached coverage rather than assuming zero",()=>
        {
            var plan=Plan(new(1,12,Stock(99,observed:false)),new(2,4,Stock(0)));
            var missing=Cost(plan);
            Check(missing.Lines[0].Quantity is null && missing.Lines[0].Cost is null && missing.Lines[0].Status==PreparationCostStatus.UnknownStock
                && missing.KnownSubtotal==40 && !missing.AllCosted,"unobserved 99 is not usable stock or assumed zero");
            var partial=Cost(Plan(new CraftPlanMaterial(1,12,Stock(3,2,unknown:true,cached:true))));
            Check(partial.Lines[0].Quantity==7 && partial.StockUncertain && partial.CachedStock && partial.KnownSubtotal==70,"observed gap remains tentative");
            var all=Cost(plan,basis:PreparationCostBasis.AllRequired);
            Check(all.AllCosted && !all.StockUncertain && all.KnownSubtotal==160,"buy-all ignores irrelevant unknown holdings");
        }),
        ("Preparation cost rejects foreign scope unavailable market qualities and absent positive prices",()=>
        {
            var plan=Plan(new CraftPlanMaterial(1,2,Stock(0)));
            Check(Cost(plan,id=>Quote(id) with {WorldId=72}).Lines[0].Status==PreparationCostStatus.WrongScope,"no other world substituted");
            Check(Cost(plan,_=>Quote(99)).Lines[0].Cost is null,"no other item substituted");
            Check(Cost(plan,items:id=>Item(id) with {IsMarketable=false}).Lines[0].Status==PreparationCostStatus.NotMarketable,"bound item not priced as zero");
            Check(Cost(plan,quality:PreparationPriceQuality.Hq,items:id=>Item(id) with {CanBeHq=false}).Lines[0].Status==PreparationCostStatus.UnsupportedQuality,"no silent NQ fallback");
            Check(Cost(plan,id=>Quote(id,null,5)).Lines[0].Status==PreparationCostStatus.MissingPrice,"missing NQ minimum not inferred from sale or HQ");
            Check(Cost(plan,id=>Quote(id,null,5),quality:PreparationPriceQuality.Cheapest).Lines[0].Cost==10,"explicit cheapest mode can choose HQ");
            Check(Cost(plan,id=>Quote(id,-1,0)).Lines[0].Cost is null,"nonpositive prices unknown");
            Check(Cost(plan,world:0).Lines[0].Status==PreparationCostStatus.UnknownWorld,"offline world remains unknown");
            var covered=Cost(Plan(new CraftPlanMaterial(1,2,Stock(2))),world:0);
            Check(covered.AllCosted && covered.KnownSubtotal==0 && covered.Lines[0].Status==PreparationCostStatus.Covered,"observed fully covered quantity needs no market quote");
        }),
        ("Preparation cost flags source age future dates and checked decimal overflow without complete totals",()=>
        {
            var plan=Plan(new CraftPlanMaterial(1,2,Stock(0)));
            foreach(var quote in new[] {Quote(1) with {UploadedAt=null},Quote(1) with {UploadedAt=Now.AddDays(-2)},Quote(1) with {FetchedAt=Now.AddHours(1)},Quote(1) with {FetchedAt=Now.AddMinutes(-5)}})
                Check(Cost(plan,_=>quote).StalePrices,"unverified age not fresh");
            var line=Cost(plan,id=>Quote(id,decimal.MaxValue));
            Check(line.Lines[0].Status==PreparationCostStatus.Overflow && line.Lines[0].Cost is null && !line.AllCosted,"line overflow not zero cost");
            var sum=Cost(Plan(new(1,1,Stock(0)),new(2,1,Stock(0))),id=>Quote(id,decimal.MaxValue));
            Check(sum.KnownSubtotal is null && !sum.AllCosted,"sum overflow does not keep an incomplete subtotal as total");
        }),
        ("Preparation requests share cache cadence and preserve manual state",()=>
        {
            var calls=0;
            using var client=new HttpClient(new MarketTestHandler((request,_)=>{calls++;return Task.FromResult(Reply(uint.Parse(request.RequestUri!.Segments.Last())));}));
            using var market=new MarketService(client);market.SetWorld(1,71,"World");var before=market.State;
            var first=market.RequestForPreparationAsync(42,1,71).GetAwaiter().GetResult();
            Check(first.Status==MarketFetchStatus.Success && calls==1 && ReferenceEquals(before,market.State),"preparation does not replace selected price");
            Check(market.RequestForPreparationAsync(42,1,71).GetAwaiter().GetResult().Status==MarketFetchStatus.Success && calls==1,"same shared cached quote");
            var throttle=market.RequestForPreparationAsync(43,1,71).GetAwaiter().GetResult();
            Check(throttle.Status==MarketFetchStatus.Cooldown && throttle.RetryAfter>TimeSpan.Zero && calls==1,"same three second network cadence");
            Check(market.RequestForPreparationAsync(43,2,71).GetAwaiter().GetResult().Status==MarketFetchStatus.ScopeChanged,"wrong owner blocked before request");
        }),
        ("Manual market priority and preparation cancellation reject late responses",()=>
        {
            var response=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var client=new HttpClient(new MarketTestHandler((_,_)=>response.Task));using var market=new MarketService(client);market.SetWorld(1,71,"World");
            var pending=market.RequestForPreparationAsync(42,1,71); market.CancelBackground();
            market.RequestAsync(43).GetAwaiter().GetResult();
            response.SetResult(Reply(42));var result=pending.GetAwaiter().GetResult();
            Check(result.Status==MarketFetchStatus.Preempted && market.GetCached(71,42) is null && market.State.ItemId==43,"manual consultation wins over preparation");
            var late=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var secondClient=new HttpClient(new MarketTestHandler((_,_)=>late.Task));using var second=new MarketService(secondClient);second.SetWorld(1,71,"World");
            using var cancel=new CancellationTokenSource();var request=second.RequestForPreparationAsync(42,1,71,cancel.Token);second.CancelBackground();cancel.Cancel();late.SetResult(Reply(42));
            Check(request.GetAwaiter().GetResult().Status==MarketFetchStatus.Cancelled && second.GetCached(71,42) is null,"external cancellation cannot publish even if handler ignores token");
        }),
        ("Shared market priorities preserve preparation after hover ends and keep manual work protected",()=>
        {
            var response=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var client=new HttpClient(new MarketTestHandler((_,_)=>response.Task));using var market=new MarketService(client);market.SetWorld(1,71,"World");
            var pending=market.RequestForPreparationAsync(42,1,71);market.CancelBackground();response.SetResult(Reply(42));
            Check(pending.GetAwaiter().GetResult().Status==MarketFetchStatus.Success && market.GetCached(71,42) is not null,"hover change cannot cancel preparation");
            var manualResponse=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var manualClient=new HttpClient(new MarketTestHandler((_,_)=>manualResponse.Task));using var manual=new MarketService(manualClient);manual.SetWorld(1,71,"World");
            var manualTask=manual.RequestAsync(42);var snapshot=manual.State;
            Check(manual.RequestForPreparationAsync(43,1,71).GetAwaiter().GetResult().Status==MarketFetchStatus.Busy && ReferenceEquals(snapshot,manual.State),"preparation cannot preempt manual request");
            manualResponse.SetResult(Reply(42));manualTask.GetAwaiter().GetResult();
        }),
        ("Preparation batch deduplicates prices and respects shared cadence across two requested items",()=>
        {
            var times=new List<DateTimeOffset>();
            using var client=new HttpClient(new MarketTestHandler((request,_)=>{times.Add(DateTimeOffset.UtcNow);return Task.FromResult(Reply(uint.Parse(request.RequestUri!.Segments.Last())));}));
            using var market=new MarketService(client);market.SetWorld(1,71,"World");using var session=new PreparationPriceSession();
            session.StartAsync(market,Context,[42,42,43]).GetAwaiter().GetResult();
            Check(session.State.Completed==2 && !session.State.Loading && session.State.Error is null && times.Count==2,"each unique ingredient fetched once");
            Check(times[1]-times[0]>=TimeSpan.FromSeconds(2.8) && market.State.ItemId==0,"queue shares network spacing without changing manual item");
        }),
        ("Preparation batch clears context cancels in flight work and is inert after disposal",()=>
        {
            var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var response=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var client=new HttpClient(new MarketTestHandler((_,_)=>{entered.TrySetResult(true);return response.Task;}));
            using var market=new MarketService(client);market.SetWorld(1,71,"World");using var session=new PreparationPriceSession();
            var pending=session.StartAsync(market,Context,[42]);
            Check(entered.Task.Wait(TimeSpan.FromSeconds(5)),"request entered handler");
            session.ValidateContext(1,71,501,1,0,0);response.SetResult(Reply(42));pending.GetAwaiter().GetResult();
            Check(session.State.Context is null && !session.State.Loading && market.GetCached(71,42) is null,"context change rejects stale publications");
            session.Dispose();session.StartAsync(market,Context,[43]).GetAwaiter().GetResult();
            Check(session.State.Context is null,"disposed list cannot restart");
        }),
        ("Preparation batch stops after provider failure instead of issuing the rest of the list",()=>
        {
            var calls=0;using var client=new HttpClient(new MarketTestHandler((_,_)=>{calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));}));
            using var market=new MarketService(client);market.SetWorld(1,71,"World");using var session=new PreparationPriceSession();
            session.StartAsync(market,Context,[42,43]).GetAwaiter().GetResult();
            Check(calls==1 && session.State.Error is not null && !session.State.Loading && session.State.Completed==0,"failure ends user batch without retry storm");
        }),
    ];
}
