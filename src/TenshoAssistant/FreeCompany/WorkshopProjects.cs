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
public static class WorkshopStock
{
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
