namespace TenshoAssistant.Gathering;

public enum GpEffect { Success, Yield, BoonChance1, BoonChance2, BoonYield, Bountiful1, Bountiful2 }
public sealed record GpActionSpec(uint Id,uint JobId,int Level,int Cost,GpEffect Effect,int Magnitude);
public sealed record GpAction(GpActionSpec Spec,string Name,uint Icon,string Description);

public sealed class GpActionCatalog
{
    private static readonly IReadOnlyList<GpActionSpec> specs = Array.AsReadOnly(new GpActionSpec[]
    {
        new(235,16,4,50,GpEffect.Success,5),new(237,16,5,100,GpEffect.Success,15),new(295,16,10,250,GpEffect.Success,50),
        new(239,16,30,400,GpEffect.Yield,1),new(241,16,40,500,GpEffect.Yield,2),
        new(21177,16,15,50,GpEffect.BoonChance1,10),new(25589,16,50,100,GpEffect.BoonChance2,30),new(21203,16,81,200,GpEffect.BoonYield,1),
        new(4073,16,24,100,GpEffect.Bountiful1,1),new(272,16,68,100,GpEffect.Bountiful2,0),
        new(218,17,4,50,GpEffect.Success,5),new(220,17,5,100,GpEffect.Success,15),new(294,17,10,250,GpEffect.Success,50),
        new(222,17,30,400,GpEffect.Yield,1),new(224,17,40,500,GpEffect.Yield,2),
        new(21178,17,15,50,GpEffect.BoonChance1,10),new(25590,17,50,100,GpEffect.BoonChance2,30),new(21204,17,81,200,GpEffect.BoonYield,1),
        new(4087,17,24,100,GpEffect.Bountiful1,1),new(273,17,68,100,GpEffect.Bountiful2,0)
    });
    public static IReadOnlyList<GpActionSpec> Specs => specs;
    public IReadOnlyList<GpAction> Actions { get; }
    public string DataVersion { get; }
    public GpActionCatalog(IEnumerable<GpAction> source,string dataVersion)
    {
        var copied=source.ToArray();
        if(copied.Length!=specs.Count || copied.Select(action=>action.Spec.Id).Distinct().Count()!=specs.Count
            || copied.Any(action=>!specs.Contains(action.Spec) || string.IsNullOrWhiteSpace(action.Name) || string.IsNullOrWhiteSpace(action.Description)))
            throw new ArgumentException("Incomplete or incompatible gathering action catalog.",nameof(source));
        Actions=Array.AsReadOnly(copied); DataVersion=dataVersion;
    }
}
public sealed record GpCatalogState(GpActionCatalog? Catalog,string? Error);
public sealed class GpCatalogService
{
    private GpCatalogState state=new(null,null);
    public GpCatalogState State=>Volatile.Read(ref state);
    internal void Publish(GpActionCatalog catalog)=>Interlocked.Exchange(ref state,new(catalog,null));
    internal void Fail(Exception error)=>Interlocked.Exchange(ref state,new(null,error.Message));
    internal void Clear()=>Interlocked.Exchange(ref state,new(null,null));
}
