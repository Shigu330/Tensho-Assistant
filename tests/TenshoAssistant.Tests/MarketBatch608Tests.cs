using TenshoAssistant.Market;

// 0.608 (I4): prices read by batch (docs/evidence/0.608-universalis-batch.md).
internal static class MarketBatch608Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Market batch: chunks of at most 100, without repeats or empty ids, only when two or more are missing",() =>
        {
            Check(MarketService.BatchIds([1,2,2,0,3],_=>false) is [1,2,3],"distinct, no zero");
            Check(MarketService.BatchIds([1,2,3],id=>id!=3).Length==0,"one missing: the usual request does it");
            Check(MarketService.BatchIds(Enumerable.Range(1,150).Select(id=>(uint)id).ToArray(),_=>false).Length==MarketService.BatchMaximum,"never past what the server answers");
            Check(MarketService.BatchMaximum==100,"the proven limit");
        }),
        ("Market batch: a result per id, an item without a price is left to the usual request",() =>
        {
            var json="""{"results":[{"itemId":5,"nq":{},"hq":{}}],"failedItems":[9]}""";
            var quote=MarketQuote.Parse(json,56,5,DateTimeOffset.UtcNow);
            Check(quote.ItemId==5,"read from the same response");
            try{MarketQuote.Parse(json,56,9,DateTimeOffset.UtcNow);throw new Exception("not refused");}
            catch(InvalidDataException){}
        }),
        ("Market batch: fills the cache first, then the list goes on as before",() =>
        {
            var session=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/PreparationPriceSession.cs"));
            Check(session.Contains("if(await market.FillCacheAsync(copied,context.CharacterId,context.WorldId,token).ConfigureAwait(false) is { } failed)throw"),"before the loop; a server failure ends the list");
            var service=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/MarketService.cs"));
            Check(service.Contains("api/v2/aggregated/{world}/{string.Join(',',missing)}"),"one request");
            Check(service.Contains("Enabled?.Invoke()==false"),"respects the source's switch");
        }),
    ];
}
