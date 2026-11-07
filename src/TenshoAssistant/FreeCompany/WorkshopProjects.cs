namespace TenshoAssistant.FreeCompany;

/// <summary>C22 (0.664): a material of a phase: so many items per delivery, so many deliveries (docs/evidence/0.664-workshop-projects.md).</summary>
public sealed record WorkshopMaterial(uint Item,int PerSet,int Sets)
{
    public int Total=>PerSet*Sets;
}
public sealed record WorkshopPhase(int Number,IReadOnlyList<WorkshopMaterial> Materials);
public sealed record WorkshopPart(string Name,IReadOnlyList<WorkshopPhase> Phases);
public sealed record WorkshopProject(uint Id,uint ResultItem,string Type,IReadOnlyList<WorkshopPart> Parts,uint Draft=0,uint Order=0,int Category=0)
{
    /// <summary>Each item once, in the order it first comes, with its whole quantity.</summary>
    public IReadOnlyList<(uint Item,int Total)> Totals()
        =>Parts.SelectMany(part=>part.Phases).SelectMany(phase=>phase.Materials).GroupBy(material=>material.Item).Select(group=>(group.Key,group.Sum(material=>material.Total))).ToArray();
}
/// <summary>
/// 0.787 (X-28, user request): what the workshop already received, as its delivery window (SubmarinePartsMenu) shows it; the game keeps it nowhere else
/// Tensho knows. Phase: the one in progress, from 0; the phases before it are done. Items: that phase's sets delivered (docs/evidence/0.787-workshop-delivered.md).
/// </summary>
public sealed class WorkshopDelivery
{
    public int Phase { get; set; }
    public int Phases { get; set; }
    public List<WorkshopDeliveredItem> Items { get; set; } = [];
    public DateTimeOffset SeenAt { get; set; }
    public bool SameAs(WorkshopDelivery other)=>Phase==other.Phase && Phases==other.Phases && Items.Count==other.Items.Count
        && Items.Zip(other.Items).All(pair=>pair.First.Item==pair.Second.Item && pair.First.Sets==pair.Second.Sets && pair.First.PerSet==pair.Second.PerSet && pair.First.Required==pair.Second.Required);

    private const int Slots=12;
    /// <summary>
    /// The window's numbers, by index (-1: not a number), as captured on 2026-11-07: 0 the project's item, 6 the phase in progress from 0, 7 the phases,
    /// 11 the items of the phase, then twelve slots each: 12 item, 60 per set, 108 sets delivered, 120 sets required. Anything out of shape: nothing.
    /// </summary>
    public static (uint Result,WorkshopDelivery Delivery)? FromWindow(IReadOnlyList<int> numbers,bool loaded,DateTimeOffset at)
    {
        if(!loaded || numbers.Count<120+Slots)return null;
        int result=numbers[0],phase=numbers[6],phases=numbers[7],count=numbers[11];
        if(result<=0 || phases<=0 || phase<0 || phase>=phases || count<=0 || count>Slots)return null;
        var items=new List<WorkshopDeliveredItem>();
        for(var index=0;index<count;index++)
        {
            int item=numbers[12+index],perSet=numbers[60+index],sets=numbers[108+index],required=numbers[120+index];
            if(item<=0 || perSet<=0 || sets<0 || required<=0 || sets>required)return null;
            items.Add(new(){Item=(uint)item,PerSet=perSet,Sets=sets,Required=required});
        }
        return ((uint)result,new(){Phase=phase,Phases=phases,Items=items,SeenAt=at});
    }
}
public sealed class WorkshopDeliveredItem
{
    public uint Item { get; set; }
    public int PerSet { get; set; }
    public int Sets { get; set; }
    public int Required { get; set; }
}

public static class WorkshopStock
{
    /// <summary>
    /// 0.787 (X-28): used only when it fits the project as the game's sheets give it: one part, the same number of phases, and the phase in progress with the
    /// same items, per set and sets. Otherwise the whole need stays (unknown is not zero).
    /// </summary>
    public static bool Applies(WorkshopProject project,WorkshopDelivery? delivery)=>delivery is not null && project.Parts.Count==1 && project.Parts[0].Phases.Count==delivery.Phases
        && delivery.Phase>=0 && delivery.Phase<delivery.Phases && project.Parts[0].Phases[delivery.Phase].Materials is { } materials && materials.Count==delivery.Items.Count
        && materials.All(material=>delivery.Items.Any(item=>item.Item==material.Item && item.PerSet==material.PerSet && item.Required==material.Sets));

    /// <summary>What is still to deliver: the phases done left out, the sets delivered of the phase in progress taken off.</summary>
    public static IReadOnlyList<(uint Item,int Total)> Remaining(WorkshopProject project,WorkshopDelivery? delivery)
    {
        if(!Applies(project,delivery))return project.Totals();
        var phases=project.Parts[0].Phases;
        var rows=new List<(uint Item,int Total)>();
        for(var index=delivery!.Phase;index<phases.Count;index++)
            foreach(var material in phases[index].Materials)
            {
                var left=material.Total;
                if(index==delivery.Phase && delivery.Items.FirstOrDefault(item=>item.Item==material.Item) is { } got)left=Math.Max(0,material.Total-got.Sets*got.PerSet);
                if(left>0)rows.Add((material.Item,left));
            }
        return rows.GroupBy(row=>row.Item).Select(group=>(group.Key,group.Sum(row=>row.Total))).ToArray();
    }

    public static IReadOnlyList<(uint Item,int Total)> Totals(IEnumerable<WorkshopProject> projects,Func<uint,WorkshopDelivery?> delivered)=>
        projects.SelectMany(project=>Remaining(project,delivered(project.ResultItem))).GroupBy(row=>row.Item).Select(group=>(group.Key,group.Sum(row=>row.Total))).ToArray();

    /// <summary>
    /// 0.694 (user request): the parts that go with a project: the game gives them one prototype and one order (Coque, Poupe, Proue, Pont de classe Unkiu: 25, 2; docs/evidence/0.694-workshop-sets.md).
    /// 0.727 (user report): and one category. Everything with no prototype and order 1 shared a pair, so the first airship also took a tower base and a housing wall. Row 0 of the prototypes is a real one.
    /// </summary>
    public static IReadOnlyList<WorkshopProject> SameSet(IEnumerable<WorkshopProject> projects,WorkshopProject project)=>
        project.Category==0 ? [project]:projects.Where(other=>other.Id==project.Id || other.Category==project.Category && other.Draft==project.Draft && other.Order==project.Order).ToArray();

    /// <summary>Several projects as one list: each item once, in the order it first comes, with its whole quantity.</summary>
    public static IReadOnlyList<(uint Item,int Total)> Totals(IEnumerable<WorkshopProject> projects)=>
        projects.SelectMany(project=>project.Totals()).GroupBy(row=>row.Item).Select(group=>(group.Key,group.Sum(row=>row.Total))).ToArray();

    public static long Missing(long need,long held)=>Math.Max(0,need-Math.Max(0,held));
}
