namespace TenshoAssistant.Gathering;

/// <summary>The gathering actions the player does not want planned, as for crafting macros. One key covers the miner's action and the botanist's twin.</summary>
public static class GpActionRules
{
    public static string Key(GpActionSpec spec)=>$"{spec.Effect}:{spec.Magnitude}";
    public static string Joined(IEnumerable<string>? keys)=>string.Join('|',(keys ?? []).Where(key=>!string.IsNullOrWhiteSpace(key)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    public static bool Forbidden(string? joined,GpActionSpec spec)=>!string.IsNullOrEmpty(joined) && joined.Split('|').Contains(Key(spec),StringComparer.Ordinal);
    /// <summary>Each key once, in the catalogue's order, with the miner's and the botanist's action.</summary>
    public static IReadOnlyList<(string Key,GpAction? Miner,GpAction? Botanist)> Groups(GpActionCatalog catalog)
        =>catalog.Actions.GroupBy(action=>Key(action.Spec)).Select(group=>(group.Key,group.FirstOrDefault(action=>action.Spec.JobId==16),group.FirstOrDefault(action=>action.Spec.JobId==17))).ToArray();
}

public sealed record GpRouteInput(long Needed,int Gp,int MaxGp,int RegenPerNode);
/// <param name="Nodes">The nodes needed, on average.</param>
/// <param name="First">What to do on this node.</param>
/// <param name="Reached">False: <see cref="GpRoutes.MaxNodes"/> nodes do not reach the amount.</param>
public sealed record GpRoute(int Nodes,decimal Expected,GpPlan First,bool Reached,int GpSpent);

/// <summary>
/// 0.478 (user request, in-game campaign): GP over the nodes a crafting list still needs, not for one node alone. Every node is taken as the one in front of the player (same attempts and rates),
/// GP comes back between nodes. The fewest nodes first: their GP is shared out evenly, and the last node spends only what it needs. An average, not a promise.
/// </summary>
public static class GpRoutes
{
    public const int MaxNodes=300;

    public static GpRoute? Build(GpOptimization optimization,GpRouteInput input)
    {
        ArgumentNullException.ThrowIfNull(optimization);ArgumentNullException.ThrowIfNull(input);
        var plans=optimization.Frontier;
        if(input.Needed<=0 || plans.Count==0 || input.Gp<0 || input.MaxGp<0)return null;
        GpRoute? last=null;
        for(var nodes=1;nodes<=MaxNodes;nodes++)
        {
            last=Walk(plans,input,nodes);
            if(last.Reached)return last;
        }
        return last;
    }

    private static GpRoute Walk(IReadOnlyList<GpPlan> plans,GpRouteInput input,int nodes)
    {
        var gp=Math.Min(input.Gp,input.MaxGp);var regen=Math.Max(0,input.RegenPerNode);
        var total=0m;var spent=0;GpPlan? first=null;
        for(var node=0;node<nodes;node++)
        {
            var left=nodes-node;
            var share=Math.Min(gp,(gp+regen*(left-1))/left);
            var plan=Affordable(plans,share);
            // This node can finish the list: the cheapest plan that does, as far as GP allows.
            if(total+Affordable(plans,gp).ExpectedYield>=input.Needed)
                plan=plans.First(row=>row.Cost<=gp && total+row.ExpectedYield>=input.Needed);
            first??=plan;
            total+=plan.ExpectedYield;spent+=plan.Cost;
            gp=Math.Min(input.MaxGp,gp-plan.Cost+regen);
        }
        return new(nodes,total,first!,total>=input.Needed,spent);
    }
    private static GpPlan Affordable(IReadOnlyList<GpPlan> plans,int gp)=>plans.LastOrDefault(plan=>plan.Cost<=gp) ?? plans[0];
}
