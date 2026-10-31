using System.Collections.Frozen;

namespace TenshoAssistant.Craft;

public enum CraftActionKind { BasicSynthesis,CarefulSynthesis,Groundwork,BasicTouch,StandardTouch,AdvancedTouch,Byregot,MastersMend,Observe,Veneration,Innovation,GreatStrides,WasteNot,WasteNot2,Manipulation,Reflect,MuscleMemory,
    DelicateSynthesis,PreparatoryTouch,PrudentSynthesis,PrudentTouch,RefinedTouch,TrainedEye,TrainedFinesse,ImmaculateMend,TrainedPerfection }
public sealed record CraftActionSpec(CraftActionKind Kind,uint CanonicalId,int Level,int Cp,int Durability);
public sealed record CraftActionMetadata(CraftActionSpec Spec,uint JobId,uint NativeId,string Name,uint Icon,string Description);
public sealed class CraftActionCatalog
{
    private static readonly IReadOnlyList<CraftActionSpec> specs=Array.AsReadOnly(new CraftActionSpec[]
    {
        new(CraftActionKind.BasicSynthesis,100001,1,0,10),new(CraftActionKind.CarefulSynthesis,100203,62,7,10),new(CraftActionKind.Groundwork,100403,72,18,20),
        new(CraftActionKind.BasicTouch,100002,5,18,10),new(CraftActionKind.StandardTouch,100004,18,32,10),new(CraftActionKind.AdvancedTouch,100411,68,46,10),
        new(CraftActionKind.Byregot,100339,50,24,10),new(CraftActionKind.MastersMend,100003,7,88,0),new(CraftActionKind.Observe,100010,13,7,0),
        new(CraftActionKind.Veneration,19297,15,18,0),new(CraftActionKind.Innovation,19004,26,18,0),new(CraftActionKind.GreatStrides,260,21,32,0),
        new(CraftActionKind.WasteNot,4631,15,56,0),new(CraftActionKind.WasteNot2,4639,47,98,0),new(CraftActionKind.Manipulation,4574,65,96,0),
        new(CraftActionKind.Reflect,100387,69,6,10),new(CraftActionKind.MuscleMemory,100379,54,6,10),
        new(CraftActionKind.DelicateSynthesis,100323,76,32,10),new(CraftActionKind.PreparatoryTouch,100299,71,40,20),
        new(CraftActionKind.PrudentSynthesis,100427,88,18,5),new(CraftActionKind.PrudentTouch,100227,66,25,5),
        new(CraftActionKind.RefinedTouch,100443,92,24,10),new(CraftActionKind.TrainedEye,100283,80,250,0),
        new(CraftActionKind.TrainedFinesse,100435,90,32,0),new(CraftActionKind.ImmaculateMend,100467,98,112,0),new(CraftActionKind.TrainedPerfection,100475,100,0,0)
    });
    public static IReadOnlyList<CraftActionSpec> Specs=>specs;
    private readonly FrozenDictionary<(uint Job,CraftActionKind Kind),CraftActionMetadata> rows;
    private readonly FrozenDictionary<uint,CraftStatusMetadata> statuses;
    public string DataVersion {get;}
    public CraftActionCatalog(IEnumerable<CraftActionMetadata> source,string version,IEnumerable<CraftStatusMetadata>? statusSource=null)
    {
        var copied=source.ToArray();
        if(copied.Length!=specs.Count*8 || copied.Any(row=>row.JobId is <8 or >15 || !specs.Contains(row.Spec) || row.NativeId==0
            || string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.Description))
            || copied.Select(row=>(row.JobId,row.Spec.Kind)).Distinct().Count()!=copied.Length) throw new ArgumentException("Incomplete craft action catalog.");
        var statusRows=statusSource?.ToArray() ?? [];
        if(statusSource is not null && (statusRows.Length!=CraftObservedEffects.Specs.Count || statusRows.Select(row=>row.Id).Distinct().Count()!=statusRows.Length
            || statusRows.Any(row=>!CraftObservedEffects.Specs.Any(spec=>spec.Id==row.Id) || string.IsNullOrWhiteSpace(row.Name) || row.Icon==0)))
            throw new ArgumentException("Incomplete craft status catalog.");
        statuses=statusRows.ToFrozenDictionary(row=>row.Id);
        rows=copied.ToFrozenDictionary(row=>(row.JobId,row.Spec.Kind)); DataVersion=version;
    }
    public CraftActionMetadata? Find(uint jobId,CraftActionKind kind)=>rows.GetValueOrDefault((jobId,kind));
    public CraftStatusMetadata? FindStatus(uint id)=>statuses.GetValueOrDefault(id);
}
public sealed record CraftActionCatalogState(CraftActionCatalog? Catalog,string? Error);
public sealed class CraftActionCatalogService
{
    private CraftActionCatalogState state=new(null,null);
    public CraftActionCatalogState State=>Volatile.Read(ref state);
    internal void Publish(CraftActionCatalog catalog)=>Interlocked.Exchange(ref state,new(catalog,null));
    internal void Fail(Exception error)=>Interlocked.Exchange(ref state,new(null,error.Message));
    internal void Clear()=>Interlocked.Exchange(ref state,new(null,null));
}
