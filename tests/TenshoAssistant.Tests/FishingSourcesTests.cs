using System.Net;
using TenshoAssistant.Community;
using TenshoAssistant.Monsters;
using TenshoAssistant.Gathering;

internal static class FishingSourcesTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Now=DateTimeOffset.UtcNow;
    private const string Valid="""{"item":{"id":42,"drops":[777],"fish":{"spots":[{"spot":11,"baits":[[2585,4869,4869],[2587]]},{"spot":"11","baits":[]},{"node":99}]}}}""";
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Community bait routes preserve alternative chains step order repeats and source variants",()=>
        {
            var item=MonsterDocuments.ParseItem(Valid,42,Now);var fish=item.Fishing!;
            Check(item.ItemId==42 && item.FetchedAt==Now && item.Monsters.Single().Id==777,"shared identity provenance and monster links preserved");
            Check(fish.SpotsKnown && fish.Spots.Count==3 && fish.Spots[0].SpotId==11 && fish.Spots[1].SpotId==11,"same-place variants not merged");
            Check(fish.Spots[0].BaitChains![0].SequenceEqual(new uint[]{2585,4869,4869}) && fish.Spots[0].BaitChains![1].Single()==2587,"ordered routes never flattened sorted or deduplicated");
            Check(fish.Spots[1].BaitChains?.Count==0 && fish.Spots[2].BaitChains is null && fish.Spots[2].SpotId is null,"empty and missing distinct, spearfishing node not reinterpreted as FishingSpot");
        }),
        ("Absent fishing fields remain unknown while explicit empty spots are only a listed empty result",()=>
        {
            foreach(var json in new[]{"""{"item":{"id":42}}""","""{"item":{"id":42,"fish":null}}"""})
                Check(MonsterDocuments.ParseItem(json,42,Now).Fishing is null,"no implicit absence of fishing source");
            var missing=MonsterDocuments.ParseItem("""{"item":{"id":42,"fish":{}}}""",42,Now).Fishing!;
            var empty=MonsterDocuments.ParseItem("""{"item":{"id":42,"fish":{"spots":[]}}}""",42,Now).Fishing!;
            Check(!missing.SpotsKnown && empty.SpotsKnown && empty.Spots.Count==0,"known list versus absent list");
        }),
        ("Malformed optional fishing data cannot erase unrelated item source information",()=>
        {
            foreach(var fish in new[]{"[]","{\"spots\":{}}","{\"spots\":[{\"spot\":0}]}","{\"spots\":[{\"spot\":11,\"baits\":[[]]}]}","{\"spots\":[{\"baits\":[[4294967296]]}]}","{\"spots\":[{\"baits\":[[\"-1\"]]}]}"})
            {
                var json="{\"item\":{\"id\":42,\"drops\":[777],\"fish\":"+fish+"}}";
                var item=MonsterDocuments.ParseItem(json,42,Now);
                Check(item.Fishing is null && item.FishingError is not null && item.HasDrops && item.Monsters.Single().Id==777,"optional domain parse isolated from canonical item identity and monster links");
            }
            var refused=false;try{MonsterDocuments.ParseItem(Valid,43,Now);}catch(InvalidDataException){refused=true;}
            Check(refused,"global item identity mismatch still rejects response");
        }),
        ("Fishing routes enforce array limits cancellation and independent managed copies",()=>
        {
            var tooMany="{\"item\":{\"id\":42,\"fish\":{\"spots\":["+string.Join(',',Enumerable.Repeat("{}",257))+ "]}}}";
            Check(MonsterDocuments.ParseItem(tooMany,42,Now).FishingError is not null,"source count bounded");
            var longChain="{\"item\":{\"id\":42,\"fish\":{\"spots\":[{\"baits\":[["+string.Join(',',Enumerable.Repeat("2585",9))+ "]]}]}}}";
            Check(MonsterDocuments.ParseItem(longChain,42,Now).FishingError is not null,"chain length bounded");
            using var cancellation=new CancellationTokenSource();cancellation.Cancel();var stopped=false;
            try{MonsterDocuments.ParseItem(Valid,42,Now,cancellation.Token);}catch(OperationCanceledException){stopped=true;}
            Check(stopped,"cancellation never converted into optional parse error");
            var copied=MonsterDocuments.ParseItem(Valid,42,Now).Fishing!;
            Check(copied.Spots[0].BaitChains![0][1]==4869,"usable after JsonDocument disposal");
        }),
        ("Fishing and monster views reuse one manual item cache without implicit network or state mutation",()=>
        {
            var calls=0;using var http=new HttpClient(new MarketTestHandler((_,_)=>{calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Valid)});}));
            using var transport=new GarlandClient(http);using var service=new MonsterLootService(transport);
            service.SelectItem(42);Check(calls==0 && service.State.Item is null,"selection remains local");
            service.RequestItemAsync().GetAwaiter().GetResult();var item=service.State.Item!;
            Check(item.Fishing?.Spots.Count==3 && item.Monsters.Count==1 && calls==1,"single transport response serves both projections");
            service.RequestItemAsync().GetAwaiter().GetResult();Check(ReferenceEquals(item,service.State.Item) && calls==1,"same timestamp and document reused from cache");
        }),
        ("A cancelled or foreign shared item response cannot publish bait routes into another selection",()=>
        {
            var pending=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var http=new HttpClient(new MarketTestHandler((_,_)=>pending.Task));using var transport=new GarlandClient(http);using var service=new MonsterLootService(transport);
            service.SelectItem(42);var work=service.RequestItemAsync();service.SelectItem(43);
            pending.SetResult(new(HttpStatusCode.OK){Content=new StringContent(Valid)});work.GetAwaiter().GetResult();
            Check(service.State.ItemId==43 && service.State.Item is null && !service.State.Loading,"old source routes rejected after selection changes");
        }),
        ("Fishing place lookup never confuses a same-number mining point or unknown spot",()=>
        {
            var rows=new List<GatheringSource>{new(42,11,11,100,"Pêche",0,1,2,"Zone pêche","Côte",IsFishing:true),new(43,11,21,1,"Mine",0,1,3,"Zone mine","Roche")};
            var catalog=new GatheringCatalog(rows,"fixture");rows.Clear();
            Check(catalog.ForFishingSpot(11) is {IsFishing:true,TerritoryId:2,Place:"Côte"} && catalog.ForFishingSpot(21) is null,"typed native FishingSpot index, not BaseId or mineral point");
            Check(catalog.ForFishingSpot(99) is null && catalog.ForItem(43).Single().Zone=="Zone mine","unknown lookup and original item index preserved");
        }),
    };
}
