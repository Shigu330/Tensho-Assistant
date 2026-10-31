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
    public static CraftLogContext Resolve(CraftLogObservation observation,CharacterStatsState stats,RecipeCatalog? catalog,DateTimeOffset now)
    {
        if(!observation.Fresh(now))return new(null,null,"Sélection du carnet trop ancienne ou invalide.");
        var recipe=catalog?.ForResult(observation.ItemId).FirstOrDefault(row=>row.RecipeId==observation.RecipeId && row.CraftTypeId==observation.CraftTypeId);
        if(recipe is null)return new(null,null,"Recette non reliée au catalogue courant ; aucun rapprochement par nom.");
        if(CraftSimulation.Unsupported(recipe) is { } unsupported)return new(recipe,null,unsupported);
        var p=recipe.Simulation!;
        if(p.Quality>0 && (observation.StartingQuality is null || observation.MaximumQuality!=p.Quality))
            return new(recipe,null,"Qualité initiale du carnet absente ou qualité maximale incompatible ; calcul suspendu.");
        var starting=observation.StartingQuality ?? 0;
        if(starting<0 || starting>p.MaxStartingQuality)return new(recipe,null,"Qualité initiale du carnet hors des limites de la recette.");
        if(observation.CurrentJobId!=recipe.CraftTypeId+8)return new(recipe,null,$"Équipez le métier {recipe.CraftType} pour calculer avec ses attributs.");
        if(stats.CharacterId!=observation.Owner || stats.ObservedAt is not { } date || now<date || now-date>TimeSpan.FromSeconds(3))
            return new(recipe,null,"En attente des attributs récents du personnage.");
        if(stats.JobId!=recipe.CraftTypeId+8)return new(recipe,null,$"Équipez le métier {recipe.CraftType} pour calculer avec ses attributs.");
        if(!stats.Values.TryGetValue("Craftsmanship",out var cs) || !stats.Values.TryGetValue("Control",out var control)
            || !stats.Values.TryGetValue("CraftingPoints",out var cp) || stats.Level is <1 or >100 || cs is <0 or >100000 || control is <0 or >100000 || cp is <0 or >10000)
            return new(recipe,null,"Attributs d’artisanat incomplets ou incompatibles.");
        if(cs<recipe.Simulation!.RequiredCraftsmanship || control<recipe.Simulation.RequiredControl)
            return new(recipe,null,"Habileté ou contrôle sous le minimum de la recette.");
        return new(recipe,new(stats.Level,cs,control,cp,false),null,starting);
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
