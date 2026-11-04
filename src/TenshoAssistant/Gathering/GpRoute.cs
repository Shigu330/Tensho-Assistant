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

/// <summary>0.479: what a node gives for an item: attempts, quantity per gather, extra chance.</summary>
public sealed record GatherNodeShape(int Attempts,int Quantity,int Boon);

/// <summary>
/// 0.479 (user request, in-game campaign): the normal node of an item is the plainest one seen during this session; a node that beats it by an attempt, a quantity or a large extra chance has a major bonus.
/// The first node seen is normal by definition: a bonus can only be told from what came before.
/// </summary>
public sealed class GatherNodeNorms
{
    public const int BoonMargin=30;
    private readonly Dictionary<uint,GatherNodeShape> normal=[];
    public GatherNodeShape Observe(uint item,GatherNodeShape node)
    {
        var next=normal.TryGetValue(item,out var seen) ? new GatherNodeShape(Math.Min(seen.Attempts,node.Attempts),Math.Min(seen.Quantity,node.Quantity),Math.Min(seen.Boon,node.Boon)):node;
        normal[item]=next;
        return next;
    }
    public static bool IsBonus(GatherNodeShape normal,GatherNodeShape node)
        =>node.Attempts>normal.Attempts || node.Quantity>normal.Quantity || node.Boon>=normal.Boon+BoonMargin;
}

/// <param name="SaveForBonus">0.479: a normal node spends only the GP that would be lost at the maximum; a node with a major bonus spends everything.</param>
/// <param name="CurrentIsBonus">The node in front of the player has a major bonus.</param>
public sealed record GpRouteInput(long Needed,int Gp,int MaxGp,int RegenPerNode,bool SaveForBonus=false,bool CurrentIsBonus=false);
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

    /// <param name="optimization">The node in front of the player.</param>
    /// <param name="normal">0.479: a normal node, for the nodes after this one when this one has a bonus. Null: they are like this one.</param>
    public static GpRoute? Build(GpOptimization optimization,GpRouteInput input,GpOptimization? normal=null)
    {
        ArgumentNullException.ThrowIfNull(optimization);ArgumentNullException.ThrowIfNull(input);
        var plans=optimization.Frontier;var later=(normal ?? optimization).Frontier;
        if(input.Needed<=0 || plans.Count==0 || later.Count==0 || input.Gp<0 || input.MaxGp<0)return null;
        GpRoute? last=null;
        for(var nodes=1;nodes<=MaxNodes;nodes++)
        {
            last=Walk(plans,later,input,nodes);
            if(last.Reached)return last;
        }
        return last;
    }

    private static GpRoute Walk(IReadOnlyList<GpPlan> current,IReadOnlyList<GpPlan> later,GpRouteInput input,int nodes)
    {
        var gp=Math.Min(input.Gp,input.MaxGp);var regen=Math.Max(0,input.RegenPerNode);
        var total=0m;var spent=0;GpPlan? first=null;
        for(var node=0;node<nodes;node++)
        {
            var left=nodes-node;var plans=node==0 ? current:later;
            var share=!input.SaveForBonus ? Math.Min(gp,(gp+regen*(left-1))/left)
                :node==0 && input.CurrentIsBonus ? gp
                :Math.Max(0,gp+regen-input.MaxGp);
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
