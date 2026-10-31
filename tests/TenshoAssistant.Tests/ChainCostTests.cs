using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;
using TenshoAssistant.Market;

internal static class ChainCostTests
{
    private static void Check(bool value,string message) {if(!value)throw new Exception(message);}
    private static ItemHoldingsSummary Stock(long amount=0,bool observed=true,bool unknown=false,bool cached=false)=>new([],amount,0,observed,unknown,cached,!unknown&&!cached);
    private static readonly DateTimeOffset Now=new(2026,10,28,12,0,0,TimeSpan.Zero);
    private static PreparationCostEstimate Price(CraftChainResult chain,Func<uint,MarketQuote?>? quote=null)=>PreparationCost.FromChain(chain,71,PreparationPriceQuality.Nq,
        id=>new ItemDefinition(id,$"Item {id}",true,IsMarketable:true),quote??(id=>new(71,id,Now,Now,new(10,null,null),new(8,null,null))),Now);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chain cost reuses terminal shortages without deducting intermediate or raw stock twice",()=>
        {
            var root=new RecipeDefinition(1,500,1,"Job",1,[new(100,2),new(101,1)]);
            var sub=new RecipeDefinition(2,100,3,"Job",1,[new(42,4),new(43,1)]);
            var recipes=RecipeCatalog.Build([root,sub],"test");
            var chain=CraftChain.Build(root,2,recipes,id=>Stock(id==100?1:id==42?3:0));
            var cost=Price(chain);
            Check(chain.Materials.Single(row=>row.ItemId==42).Amount==1 && chain.Observations[42].Nq==3,"chain already consumed three raw items");
            Check(cost.Lines.Single(row=>row.ItemId==42).Quantity==1 && cost.KnownSubtotal==40 && cost.AllCosted,"one raw42, one raw43 and two raw101 at ten");
            Check(cost.Basis==PreparationCostBasis.TerminalChain && !cost.StockUncertain,"explicit terminal scope");
        }),
        ("Chain cost keeps every unresolved branch outside priced totals and requests",()=>
        {
            foreach(var reason in new[] {CraftChainReason.RecipeChoice,CraftChainReason.Cycle,CraftChainReason.DepthLimit,CraftChainReason.MissingIngredients})
            {
                var requests=new List<uint>();
                var chain=new CraftChainResult(1,1,[],[new(42,2,CraftChainReason.RawMaterial),new(100,3,reason)],
                    new Dictionary<uint,ItemHoldingsSummary>{{42,Stock()},{100,Stock()}});
                var result=Price(chain,id=>{requests.Add(id);return new(71,id,Now,Now,new(10,null,null),new(8,null,null));});
                Check(result.KnownSubtotal==20 && !result.AllCosted && result.Lines.Single(row=>row.ItemId==100).Status==PreparationCostStatus.UnresolvedChain,"raw subtotal only, unresolved never silently treated as purchase");
                Check(requests.SequenceEqual([42u]),"no price lookup for unresolved branch");
            }
        }),
        ("Chain cost propagates upstream missing uncertain and cached observations",()=>
        {
            var observations=new Dictionary<uint,ItemHoldingsSummary>{{42,Stock()},{100,Stock(observed:false)}};
            var chain=new CraftChainResult(1,1,[],[new(42,2,CraftChainReason.RawMaterial)],observations);
            var missing=Price(chain);
            Check(missing.StockUncertain && missing.Lines[0].Quantity is null && missing.Lines[0].Cost is null,"upstream unobserved stock leaves terminal quantity unknown");
            observations[100]=Stock(unknown:true,cached:true);
            var tentative=Price(chain);
            Check(tentative.StockUncertain && tentative.CachedStock && tentative.Lines[0].Quantity==2 && tentative.KnownSubtotal==20,"observed ledger gap can be priced only as tentative");
            observations.Remove(42);
            Check(Price(chain).Lines[0].Quantity is null,"missing ledger evidence is unknown rather than implicit zero");
        }),
        ("Empty chain cost preserves stock provenance while recording zero calculated purchases",()=>
        {
            var chain=new CraftChainResult(1,1,[],[],new Dictionary<uint,ItemHoldingsSummary>{{42,Stock(10,unknown:true,cached:true)}});
            var cost=Price(chain);
            Check(cost.AllCosted && cost.KnownSubtotal==0 && cost.Lines.Count==0 && cost.StockUncertain && cost.CachedStock,"no terminal need does not erase uncertain imported provenance");
            try{Price(chain with {Observations=new Dictionary<uint,ItemHoldingsSummary>()});throw new Exception("unknown zero accepted");}catch(ArgumentException){}
        }),
        ("Chain cost aggregates same item reasons without hiding unresolved evidence or overflow",()=>
        {
            var ledger=new Dictionary<uint,ItemHoldingsSummary>{{42,Stock()}};
            var mixed=new CraftChainResult(1,1,[],[new(42,2,CraftChainReason.RawMaterial),new(42,3,CraftChainReason.Cycle)],ledger);
            var result=Price(mixed);
            Check(result.Lines.Count==1 && result.Lines[0].Required==5 && result.Lines[0].Cost is null && !result.AllCosted,"one mixed item remains unresolved");
            var overflow=mixed with {Materials=[new(42,long.MaxValue,CraftChainReason.RawMaterial),new(42,1,CraftChainReason.RawMaterial)]};
            try{Price(overflow);throw new Exception("overflow accepted");}catch(OverflowException){}
        }),
        ("Chain price list ignores direct quantity mode but invalidates changed recipe choices",()=>
        {
            var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var response=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var client=new HttpClient(new MarketTestHandler((_,_)=>{entered.TrySetResult(true);return response.Task;}));
            using var market=new MarketService(client);market.SetWorld(1,71,"World");using var session=new PreparationPriceSession();
            var pending=session.StartAsync(market,new(1,71,500,10,1,0,0,true,4),[42]);
            Check(entered.Task.Wait(TimeSpan.FromSeconds(5)),"chain request started");
            session.ValidateContext(1,71,500,1,1,0,4);
            Check(session.State.Loading,"direct all-required mode does not alter terminal quantities");
            session.ValidateContext(1,71,500,1,1,0,5);
            response.SetResult(new(System.Net.HttpStatusCode.OK){Content=new StringContent("{\"results\":[{\"itemId\":42}]}")});
            pending.GetAwaiter().GetResult();
            Check(session.State.Context is null && market.GetCached(71,42) is null,"choice change cancels and rejects late quote");
        }),
    ];
}
