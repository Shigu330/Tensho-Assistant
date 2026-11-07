namespace TenshoAssistant.Gear;

/// <summary>A plan kept aside: its pieces and its materia.</summary>
public sealed class GearNamedPlan
{
    public string Name {get;set;}="";
    public Dictionary<GearSlot,uint> Pieces {get;set;}=[];
    public Dictionary<GearSlot,List<uint>> Materia {get;set;}=[];
}

/// <summary>
/// 0.593 (T25): the other plans of a job. The plan in use stays in the configuration's GearPlans and GearMateria, as before, with all the code
/// that reads them: here only its name (Active) and the plans kept aside. A job without a set has one plan, without a name.
/// </summary>
public sealed class GearPlanSet
{
    public string Active {get;set;}="";
    public List<GearNamedPlan> Others {get;set;}=[];
}

public static class GearPlanSets
{
    public const int MaxPlans=20,MaxName=40;

    /// <summary>A usable new name: 1 to 40 characters, none taken in this job's set (capitals ignored). Null when refused.</summary>
    public static string? CleanName(GearPlanSet? set,string? name,string? ignoring=null)
    {
        var clean=(name ?? "").Trim();
        if(clean.Length is 0 or >MaxName || clean.Any(char.IsControl))return null;
        bool Same(string other)=>string.Equals(other,clean,StringComparison.OrdinalIgnoreCase);
        if(ignoring is not null && Same(ignoring))return clean;
        if(set is not null && (Same(set.Active) || set.Others.Any(plan=>Same(plan.Name))))return null;
        return clean;
    }

    private static Dictionary<GearSlot,List<uint>> Copy(Dictionary<GearSlot,List<uint>>? materia)=>materia?.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToList()) ?? [];

    /// <summary>A new plan starting as a copy of the current one, which is kept aside under its name. The current plan's dictionaries stay in use.</summary>
    public static bool New(ref GearPlanSet? set,string defaultName,string? name,Dictionary<GearSlot,uint>? pieces,Dictionary<GearSlot,List<uint>>? materia)
    {
        var working=set ?? new GearPlanSet {Active=defaultName};
        if(working.Others.Count+1>=MaxPlans || CleanName(working,name) is not { } clean)return false;
        working.Others.Add(new(){Name=working.Active,Pieces=new(pieces ?? []),Materia=Copy(materia)});
        working.Active=clean;
        set=working;
        return true;
    }

    /// <summary>Takes another plan: the current one is kept aside under its name. The result is what the configuration now holds for this job.</summary>
    public static GearNamedPlan? Switch(GearPlanSet set,string name,Dictionary<GearSlot,uint>? pieces,Dictionary<GearSlot,List<uint>>? materia)
    {
        var target=set.Others.FirstOrDefault(plan=>plan.Name==name);
        if(target is null)return null;
        set.Others.Remove(target);
        set.Others.Add(new(){Name=set.Active,Pieces=new(pieces ?? []),Materia=Copy(materia)});
        set.Active=target.Name;
        return target;
    }

    public static bool Rename(GearPlanSet set,string? name)
    {
        if(CleanName(set,name,set.Active) is not { } clean)return false;
        set.Active=clean;
        return true;
    }

    /// <summary>Removes the current plan and takes the first one kept aside. The only plan of a job is not removed: « Clear » empties it.</summary>
    public static GearNamedPlan? Delete(GearPlanSet set)
    {
        if(set.Others.Count==0)return null;
        var next=set.Others[0];
        set.Others.RemoveAt(0);
        set.Active=next.Name;
        return next;
    }
}
