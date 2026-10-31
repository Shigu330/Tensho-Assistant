using System.Collections.Frozen;

namespace TenshoAssistant.Items;

public sealed record ItemUseRequest(uint ItemId,int? Quantity);
public sealed record ItemLeveUse(uint RowId,uint LeveId,string Name,int Level,string Job,string Place,byte Repeats,IReadOnlyList<ItemUseRequest> Requests,uint IconId=0);

/// <summary>Static catalogue evidence only: no current daily demand, accepted leve or unlock inferred.</summary>
public sealed class ItemUseCatalog
{
    private readonly FrozenDictionary<uint,IReadOnlyList<ItemLeveUse>> byItem;
    private readonly FrozenSet<uint> supplies,aquarium;
    public string Language {get;}public string DataVersion {get;}
    public string Source=>"Lumina.Excel/CraftLeve+Leve+GCSupplyDuty+AquariumFish";
    public int LeveCount {get;}public int SupplyItemCount=>supplies.Count;public int AquariumItemCount=>aquarium.Count;
    public int UnresolvedLeveRows {get;}
    public ItemUseCatalog(IEnumerable<ItemLeveUse> leves,IEnumerable<uint> supplies,IEnumerable<uint> aquarium,string language,string version,
        int unresolvedLeveRows=0,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(leves);ArgumentNullException.ThrowIfNull(supplies);ArgumentNullException.ThrowIfNull(aquarium);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);ArgumentException.ThrowIfNullOrWhiteSpace(version);
        if(unresolvedLeveRows<0)throw new ArgumentOutOfRangeException(nameof(unresolvedLeveRows));
        cancellation.ThrowIfCancellationRequested();var copied=new List<ItemLeveUse>();var seen=new HashSet<uint>();
        foreach(var leve in leves)
        {
            cancellation.ThrowIfCancellationRequested();
            if(leve is null || leve.RowId==0 || leve.LeveId==0 || string.IsNullOrWhiteSpace(leve.Name) || leve.Level<0 || leve.Requests is not {Count:>0 and <=16}
                || leve.Requests.Any(request=>request is null || request.ItemId==0 || request.Quantity is <=0) || !seen.Add(leve.RowId))
                throw new ArgumentException("Ligne de mandat invalide ou identité dupliquée.",nameof(leves));
            copied.Add(leve with {Requests=Array.AsReadOnly(leve.Requests.ToArray())});
        }
        byItem=copied.SelectMany(leve=>leve.Requests.Select(request=>request.ItemId).Distinct().Select(id=>(Id:id,Leve:leve))).GroupBy(row=>row.Id)
            .ToFrozenDictionary(group=>group.Key,group=>(IReadOnlyList<ItemLeveUse>)Array.AsReadOnly(group.Select(row=>row.Leve)
                .OrderBy(leve=>leve.Level).ThenBy(leve=>leve.Name,StringComparer.OrdinalIgnoreCase).ThenBy(leve=>leve.RowId).ToArray()));
        this.supplies=CopySet(supplies,cancellation);this.aquarium=CopySet(aquarium,cancellation);
        Language=language;DataVersion=version;UnresolvedLeveRows=unresolvedLeveRows;LeveCount=copied.Count;
    }
    private static FrozenSet<uint> CopySet(IEnumerable<uint> values,CancellationToken token)
    {
        var copied=new HashSet<uint>();foreach(var value in values){token.ThrowIfCancellationRequested();if(value!=0)copied.Add(value);}return copied.ToFrozenSet();
    }
    public IReadOnlyList<ItemLeveUse> LevesFor(uint item)=>byItem.GetValueOrDefault(item) ?? [];
    public bool IsSupplyCandidate(uint item)=>supplies.Contains(item);
    public bool IsAquariumFish(uint item)=>aquarium.Contains(item);
}
public sealed record ItemUseCatalogState(ItemUseCatalog? Catalog,int Copied,int Total,string? Error);
public sealed class ItemUseCatalogService
{
    private ItemUseCatalogState state=new(null,0,0,null);
    public ItemUseCatalogState State=>Volatile.Read(ref state);
    internal void Progress(int copied,int total)=>Interlocked.Exchange(ref state,new(null,copied,total,null));
    internal void Publish(ItemUseCatalog catalog)=>Interlocked.Exchange(ref state,new(catalog,0,0,null));
    internal void Fail(Exception error)=>Interlocked.Exchange(ref state,new(null,0,0,error.Message));
    internal void Clear()=>Interlocked.Exchange(ref state,new(null,0,0,null));
}
