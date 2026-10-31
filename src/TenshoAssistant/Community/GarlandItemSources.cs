using System.Text.Json;

namespace TenshoAssistant.Community;

/// <summary>
/// Where an item comes from according to Garland Tools (route item/fr/3): the « partials » of the item document name the
/// duties, monsters, FATEs, quests, achievements, leves and NPCs linked to it (checked 2026-10-31: item 16574 « Alexander
/// articulé » → instances 30042/30046 « Alexander - L'Âme du Créateur »). Community data: absent means unknown, not none.
/// </summary>
public sealed record GarlandItemSource(string Type,ulong Id,string Name);
// Trades: exchanges listed in « tradeShops » for this item (currency item id and amount), even without a known NPC
// (e.g. 40382 Cor buffle insulaire ← 50 000 × 37549 Assignat insulaire azur, island sanctuary shop).
public sealed record GarlandTrade(uint CurrencyId,uint Amount);
public sealed record GarlandItemSourceList(uint ItemId,IReadOnlyList<GarlandItemSource> Sources,DateTimeOffset FetchedAt,IReadOnlyList<GarlandTrade>? Trades=null);

public static class GarlandItemSourceParser
{
    // Partial types that are sources; ingredients and other items are not.
    public static readonly IReadOnlySet<string> SourceTypes=new HashSet<string>(StringComparer.Ordinal){"instance","mob","fate","quest","achievement","leve","npc"};
    public static GarlandItemSourceList Parse(string json,uint expectedId,DateTimeOffset fetchedAt)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});
        if(!document.RootElement.TryGetProperty("item",out var item) || item.ValueKind!=JsonValueKind.Object
           || !item.TryGetProperty("id",out var id) || id.ValueKind!=JsonValueKind.Number || id.GetUInt32()!=expectedId)
            throw new InvalidDataException($"Garland item document does not match item {expectedId}.");
        var sources=new List<GarlandItemSource>();
        if(document.RootElement.TryGetProperty("partials",out var partials) && partials.ValueKind==JsonValueKind.Array)
            foreach(var partial in partials.EnumerateArray())
            {
                if(sources.Count>=64)break;
                var type=partial.TryGetProperty("type",out var typeValue) && typeValue.ValueKind==JsonValueKind.String ? typeValue.GetString() ?? "":"";
                if(!SourceTypes.Contains(type))continue;
                var key=partial.TryGetProperty("id",out var idValue) ? idValue.ValueKind switch
                {
                    JsonValueKind.String when ulong.TryParse(idValue.GetString(),out var parsed)=>parsed,
                    JsonValueKind.Number when idValue.TryGetUInt64(out var number)=>number,
                    _=>0ul,
                }:0ul;
                var name=partial.TryGetProperty("obj",out var obj) && obj.ValueKind==JsonValueKind.Object && obj.TryGetProperty("n",out var nameValue) && nameValue.ValueKind==JsonValueKind.String
                    ? nameValue.GetString() ?? "":"";
                if(key==0 || name.Length==0 || sources.Any(source=>source.Type==type && source.Id==key))continue;
                sources.Add(new(type,key,name));
            }
        var trades=new List<GarlandTrade>();
        if(item.TryGetProperty("tradeShops",out var shops) && shops.ValueKind==JsonValueKind.Array)
            foreach(var shop in shops.EnumerateArray())
            {
                if(!shop.TryGetProperty("listings",out var listings) || listings.ValueKind!=JsonValueKind.Array)continue;
                foreach(var listing in listings.EnumerateArray())
                {
                    if(trades.Count>=16)break;
                    if(!Contains(listing,"item",expectedId) || !listing.TryGetProperty("currency",out var currencies) || currencies.ValueKind!=JsonValueKind.Array)continue;
                    foreach(var currency in currencies.EnumerateArray())
                        if(Number(currency,"id") is { } currencyId && currencyId!=0 && Number(currency,"amount") is { } amount && amount!=0 && !trades.Contains(new(currencyId,amount)))
                            trades.Add(new(currencyId,amount));
                }
            }
        return new(expectedId,sources.AsReadOnly(),fetchedAt,trades.AsReadOnly());
    }
    private static bool Contains(JsonElement listing,string name,uint id)=>listing.TryGetProperty(name,out var list) && list.ValueKind==JsonValueKind.Array
        && list.EnumerateArray().Any(entry=>Number(entry,"id")==id);
    // Garland writes ids as text or numbers.
    private static uint? Number(JsonElement owner,string name)=>owner.TryGetProperty(name,out var value) ? value.ValueKind switch
    {
        JsonValueKind.String when uint.TryParse(value.GetString(),out var parsed)=>parsed,
        JsonValueKind.Number when value.TryGetUInt32(out var number)=>number,
        _=>null,
    }:null;
}

public sealed record GarlandItemSourceState(GarlandItemSourceList? List,bool Loading,string? Error);

/// <summary>On-demand item sources, one Garland request at a time (its cadence), through the shared « sources » cache.</summary>
public sealed class GarlandItemSourceService:IDisposable
{
    private readonly GarlandClient garland;
    private readonly object gate=new();
    private readonly Dictionary<uint,GarlandItemSourceState> states=[];
    private readonly Queue<uint> pending=new();
    private readonly CancellationTokenSource lifetime=new();
    private Task? worker;private bool disposed;
    public GarlandItemSourceService(GarlandClient garland){this.garland=garland;}
    public GarlandItemSourceState Get(uint itemId)
    {
        if(itemId==0)return new(null,false,null);
        lock(gate)
        {
            if(states.TryGetValue(itemId,out var known))return known;
            if(states.Count>=512)states.Remove(states.Keys.First());
            var loading=new GarlandItemSourceState(null,true,null);states[itemId]=loading;
            if(!disposed){pending.Enqueue(itemId);if(worker is null || worker.IsCompleted)worker=Task.Run(DrainAsync);}
            return loading;
        }
    }
    private async Task DrainAsync()
    {
        while(true)
        {
            uint id;
            lock(gate){if(disposed || pending.Count==0)return;id=pending.Dequeue();}
            GarlandItemSourceState result;
            try
            {
                var response=await ReadAsync(id).ConfigureAwait(false);
                var list=GarlandItemSourceParser.Parse(response.Json,id,response.FetchedAt);
                garland.Remember(GarlandResource.Item,id,response);
                result=new(list,false,null);
            }
            catch(OperationCanceledException) when(lifetime.IsCancellationRequested){return;}
            catch(Exception error){result=new(null,false,error.Message);}
            lock(gate){if(!disposed)states[id]=result;}
        }
    }
    // Garland refuses a second request within three seconds: wait and retry rather than failing.
    private async Task<GarlandResponse> ReadAsync(uint id)
    {
        for(var attempt=0;;attempt++)
        {
            try{return await garland.ReadDocumentAsync(GarlandResource.Item,id,lifetime.Token).ConfigureAwait(false);}
            catch(InvalidOperationException) when(attempt<5){await Task.Delay(GarlandClient.RequestInterval,lifetime.Token).ConfigureAwait(false);}
        }
    }
    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;pending.Clear();}
        lifetime.Cancel();lifetime.Dispose();
    }
}
