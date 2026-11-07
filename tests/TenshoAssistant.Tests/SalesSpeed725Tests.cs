using System.Net;
using System.Net.Http;
using TenshoAssistant.Craft;
using TenshoAssistant.Market;

// 0.725 (user report and request): sales a day, and the market sort that counts them.
internal static class SalesSpeed725Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private sealed class Answers(Func<string,HttpResponseMessage> reply):HttpMessageHandler
    {
        public List<string> Asked=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            lock(Asked)Asked.Add(address);
            return Task.FromResult(reply(address));
        }
    }
    private static HttpResponseMessage Text(string body,HttpStatusCode code=HttpStatusCode.OK)=>new(code){Content=new StringContent(body)};
    private const string Prices="""{"results":[{"itemId":42,"nq":{"recentPurchase":{"world":{"price":10,"timestamp":1793547821000}}},"hq":{}}]}""";
    private static CraftFromStockRow Row(long count,int amount=1)=>new(new RecipeDefinition(1,2,amount,"x",1,[]),count,false);
    private static MarketQuote Quote(MarketQualityPrice nq,MarketQualityPrice hq)=>new(40,2753,DateTimeOffset.UtcNow,null,nq,hq);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sales speed: read from the item's answer, one item or several, and zero is a value; no answer leaves it unknown",() =>
        {
            var quote=Quote(new(null,19900m,null,null,null),new(null,83000m,null,null,null));
            // Universalis, Moogle, item 2753, as read on 2026-11-07.
            var many=quote.WithSpeed("""{"items":{"2753":{"nqSaleVelocity":0.14285715,"hqSaleVelocity":0},"5":{"nqSaleVelocity":1766,"hqSaleVelocity":0}}}""");
            Check(many.Nq.Velocity==0.14285715m && many.Hq.Velocity==0m,"several");
            var one=quote.WithSpeed("""{"nqSaleVelocity":50,"hqSaleVelocity":43.857143}""");
            Check(one.Nq.Velocity==50m && one.Hq.Velocity==43.857143m,"one");
            Check(quote.WithSpeed(null)==quote && quote.WithSpeed("not json")==quote && quote.WithSpeed("""{"items":{"5":{"nqSaleVelocity":1}}}""")==quote,"unknown stays unknown");
            var kept=new MarketQuote(40,2753,DateTimeOffset.UtcNow,null,new(null,1m,null,null,9m),new(null,null,null));
            Check(kept.WithSpeed("{}").Nq.Velocity==9m,"what the other answer gave stays");
            Check(MarketQuote.SpeedUrl(40,[1u,2u]).Contains("/v2/40/1,2?listings=0&entries=200&fields=items.nqSaleVelocity,items.hqSaleVelocity") && MarketQuote.SpeedUrl(40,[1u]).Contains("fields=nqSaleVelocity,hqSaleVelocity"),"addresses");
        }),
        ("Sales speed: a price request asks both answers; a refusal keeps the price and leaves the speed unknown",() =>
        {
            MarketQuote? Ask(Func<string,HttpResponseMessage> speed,out List<string> asked)
            {
                var answers=new Answers(address=>address.Contains("aggregated",StringComparison.Ordinal) ? Text(Prices):speed(address));
                using var client=new HttpClient(answers);
                using var market=new MarketService(client);
                market.SetWorld(1,71,"World");
                market.RequestAsync(42).GetAwaiter().GetResult();
                asked=answers.Asked;
                return market.State.Quote;
            }
            var read=Ask(_=>Text("""{"nqSaleVelocity":0.5,"hqSaleVelocity":0}"""),out var both);
            Check(both.Count==2 && both[0].Contains("/aggregated/71/42") && both[1].Contains("/v2/71/42?listings=0"),"both, prices first");
            Check(read is { Nq.Velocity:0.5m,Hq.Velocity:0m,Nq.RecentSale:10m },"read");
            var refused=Ask(_=>Text("",HttpStatusCode.InternalServerError),out _);
            Check(refused is { Nq.Velocity:null,Hq.Velocity:null,Nq.RecentSale:10m },"refused");
            var failed=Ask(_=>throw new HttpRequestException("down"),out _);
            Check(failed is { Nq.Velocity:null,Nq.RecentSale:10m },"failed");
        }),
        ("Sales speed: an old HQ sale does not price an item whose HQ no longer sells while its NQ does",() =>
        {
            var nq=new MarketQualityPrice(null,19900m,null,19825m,0.14m);
            Check(Quote(nq,new(null,83000m,null,83000m,0m)).Sold(true) is { Price:19825m,PerDay:0.14m },"NQ then");
            Check(Quote(nq,new(null,83000m,null,83000m,1m)).Sold(true) is { Price:83000m,PerDay:1m },"HQ that sells");
            Check(Quote(nq,new(null,83000m,null,83000m,null)).Sold(true) is { Price:83000m,PerDay:null },"unknown is not zero");
            Check(Quote(nq with {Velocity=0m},new(null,83000m,null,83000m,0m)).Sold(true) is { Price:83000m,PerDay:0m },"neither sells: as before");
        }),
        ("Sellable: limited by the stock and by the sales, one sale in seven days is one piece, unknown is not zero",() =>
        {
            Check(CraftFromStock.Sellable(Row(31),0.14285715m,7)==1,"one a week");
            Check(CraftFromStock.Sellable(Row(31),0.3m,7)==2,"whole pieces");
            Check(CraftFromStock.Sellable(Row(31),10m,7)==31,"the stock");
            Check(CraftFromStock.Sellable(Row(3,4),10m,1)==10 && CraftFromStock.Sellable(Row(3,4),100m,1)==12,"pieces per craft");
            Check(CraftFromStock.Sellable(Row(31),0m,7)==0,"no sale");
            Check(CraftFromStock.Sellable(Row(31),null,7) is null,"unknown");
        }),
        ("Market sort: the reported case falls behind what sells, unknown comes last",() =>
        {
            var old=Row(31);var sells=new CraftFromStockRow(old.Recipe with {RecipeId=2},5,false);var unknown=new CraftFromStockRow(old.Recipe with {RecipeId=3},5,false);
            var ranked=CraftFromStock.Ranked([old,unknown,sells],row=>row==old ? 0m:row==sells ? 5m*9000m:null);
            Check(ranked[0]==sells && ranked[1]==old && ranked[2]==unknown,"order");
            var page=Read("MainWindow.CraftFromStock.cs");
            Check(page.Contains("CraftFromStock.Ranked(rows,row=>FromStockSaleValue(row,now))") && !page.Contains("Worth(row,unit)"),"the sort counts what can sell");
            Check(page.Contains("fromStockView=null") && page.Contains("Math.Clamp(saleDays,1,30)"),"the days apply at once");
            var service=Read("Market/MarketService.cs");
            Check(service.Contains("MarketQuote.Parse(json,world,id,at).WithSpeed(speed)") && service.Contains(".WithSpeed(await SpeedAsync(world,[itemId],token)"),"both requests");
            Check(service.Contains("if(ServerPause((int)response.StatusCode,response.Headers.RetryAfter?.Delta) is { } room)") && !service.Contains("universalis-aggregated-v3"),"pause kept, older copies refetched");
        }),
    ];
}
