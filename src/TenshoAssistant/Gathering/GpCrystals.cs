namespace TenshoAssistant.Gathering;

/// <param name="ShardsOnly">The game's text speaks of shards only.</param>
public sealed record GpCrystalAction(uint Id,uint JobId,int Level,int Cost,bool ShardsOnly);

/// <summary>
/// 0.519 (user report): shards, crystals and clusters have their own actions, and what they give is random: the game gives no figure, so no amount is computed here. Rows and texts: docs/evidence/0.519-crystal-actions.md.
/// The general action first, as the budget may only allow one. Neither has an unlock link: they come with the level.
/// </summary>
public static class GpCrystals
{
    public const uint UiCategory=59;
    private static readonly GpCrystalAction[] Specs=
    [
        new(4589,16,74,200,false),new(280,16,20,150,true),
        new(4590,17,74,200,false),new(282,17,20,150,true),
    ];
    public static bool IsShard(uint item)=>item is >=2 and <=7;

    public static IReadOnlyList<GpCrystalAction> For(uint job,int level,int gp,uint item)
    {
        var chosen=new List<GpCrystalAction>();var left=gp;
        foreach(var spec in Specs)
        {
            if(spec.JobId!=job || spec.Level>level || spec.ShardsOnly && !IsShard(item) || spec.Cost>left)continue;
            chosen.Add(spec);left-=spec.Cost;
        }
        return chosen;
    }
}
