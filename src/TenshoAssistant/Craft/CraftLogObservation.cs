using System.Numerics;
using TenshoAssistant.Character;

namespace TenshoAssistant.Craft;

public sealed record CraftLogObservation(ulong Owner,uint RecipeId,uint ItemId,uint CraftTypeId,Vector2 Position,Vector2 Size,DateTimeOffset ObservedAt,uint CurrentJobId=0,long Revision=0,int? StartingQuality=null,int? MaximumQuality=null)
{
    public bool Fresh(DateTimeOffset now)=>Owner!=0 && RecipeId!=0 && ItemId!=0 && CraftTypeId<=7
        && now>=ObservedAt && now-ObservedAt<=TimeSpan.FromSeconds(1);
}
public sealed record CraftLogOpenRequest(ulong Owner,uint RecipeId,uint ItemId);

public sealed record CraftLogContext(RecipeDefinition? Recipe,CraftStats? Stats,string? Issue,int StartingQuality=0)
{
    // X-11 (0.419): manipulation = the job has learned Manipulation (IUnlockState, read by the caller); it used to be always off.
    public static CraftLogContext Resolve(CraftLogObservation observation,CharacterStatsState stats,RecipeCatalog? catalog,DateTimeOffset now,bool manipulation=false)
    {
        if(!observation.Fresh(now))return new(null,null,"Crafting log selection too old or invalid.");
        var recipe=catalog?.ForResult(observation.ItemId).FirstOrDefault(row=>row.RecipeId==observation.RecipeId && row.CraftTypeId==observation.CraftTypeId);
        if(recipe is null)return new(null,null,"Recipe not linked to the current catalogue; no matching by name.");
        if(CraftSimulation.Unsupported(recipe) is { } unsupported)return new(recipe,null,unsupported);
        var p=recipe.Simulation!;
        if(p.Quality>0 && (observation.StartingQuality is null || observation.MaximumQuality!=p.Quality))
            return new(recipe,null,"Crafting log starting quality missing or maximum quality incompatible; calculation suspended.");
        var starting=observation.StartingQuality ?? 0;
        if(starting<0 || starting>p.MaxStartingQuality)return new(recipe,null,"Crafting log starting quality outside the recipe's limits.");
        if(observation.CurrentJobId!=recipe.CraftTypeId+8)return new(recipe,null,$"Equip the {recipe.CraftType} job to calculate with its attributes.");
        if(stats.CharacterId!=observation.Owner || stats.ObservedAt is not { } date || now<date || now-date>TimeSpan.FromSeconds(3))
            return new(recipe,null,"Waiting for the character's recent attributes.");
        if(stats.JobId!=recipe.CraftTypeId+8)return new(recipe,null,$"Equip the {recipe.CraftType} job to calculate with its attributes.");
        if(!stats.Values.TryGetValue("Craftsmanship",out var cs) || !stats.Values.TryGetValue("Control",out var control)
            || !stats.Values.TryGetValue("CraftingPoints",out var cp) || stats.Level is <1 or >100 || cs is <0 or >100000 || control is <0 or >100000 || cp is <0 or >10000)
            return new(recipe,null,"Crafting attributes incomplete or incompatible.");
        if(cs<recipe.Simulation!.RequiredCraftsmanship || control<recipe.Simulation.RequiredControl)
            return new(recipe,null,"Craftsmanship or control below the recipe's minimum.");
        return new(recipe,new(stats.Level,cs,control,cp,manipulation),null,starting);
    }
}

public sealed class CraftLogObservationService
{
    private CraftLogObservation? observation;
    private CraftLogOpenRequest? requested;
    private string? error;
    private long revision;
    public long Revision=>Volatile.Read(ref revision);
    public CraftLogObservation? Observation=>Volatile.Read(ref observation);
    public string? Error=>Volatile.Read(ref error);
    public void Publish(CraftLogObservation value)
    {
        if(!value.Fresh(value.ObservedAt) || !float.IsFinite(value.Position.X) || !float.IsFinite(value.Position.Y)
            || !float.IsFinite(value.Size.X) || !float.IsFinite(value.Size.Y) || value.Size.X<=0 || value.Size.Y<=0)
            throw new ArgumentException("Invalid crafting log observation.");
        Volatile.Write(ref error,null);Volatile.Write(ref observation,value with {Revision=Revision});
    }
    public void RequestOpen(CraftLogOpenRequest request)
    {
        if(request.Owner==0 || request.RecipeId is 0 or >ushort.MaxValue || request.ItemId==0)throw new ArgumentException("Invalid crafting log request.");
        Volatile.Write(ref error,null);Interlocked.Exchange(ref requested,request);
    }
    public CraftLogOpenRequest? TakeOpen()=>Interlocked.Exchange(ref requested,null);
    public void Clear(string? issue=null)
    {
        if(Interlocked.Exchange(ref observation,null) is not null)Interlocked.Increment(ref revision);
        Volatile.Write(ref error,issue);
    }
    public void Reset(){Interlocked.Exchange(ref requested,null);Clear();}
}
