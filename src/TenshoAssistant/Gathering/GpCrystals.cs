namespace TenshoAssistant.Gathering;

public sealed record GpCrystalAction(uint Id,uint JobId,int Level,int Cost);

/// <summary>
/// 0.519 (user report): shards, crystals and clusters have their own actions, and what they give is random: the game gives no figure, so no amount is computed here. Rows and texts: docs/evidence/0.519-crystal-actions.md.
/// 0.521 (user report in game): both work on all three, although the game's text for The Twelve's Bounty only names shards. Neither has an unlock link: they come with the level.
/// </summary>
public static class GpCrystals
{
    public const uint UiCategory=59;
    /// <summary>0.522 (user report): Nature's Bounty has a recast of 180 s; the other has none (docs/evidence/0.519-crystal-actions.md).</summary>
    public static IEnumerable<uint> WithRecast(uint job)=>job==16 ? [4589u]:job==17 ? [4590u]:[];
    private static readonly GpCrystalAction[] Specs=
    [
        new(4589,16,74,200),new(280,16,20,150),
        new(4590,17,74,200),new(282,17,20,150),
    ];

    /// <param name="cooling">Actions still recasting: they take no GP from the others.</param>
    public static IReadOnlyList<GpCrystalAction> For(uint job,int level,int gp,IReadOnlyCollection<uint>? cooling=null)
    {
        var chosen=new List<GpCrystalAction>();var left=gp;
        foreach(var spec in Specs)
        {
            if(spec.JobId!=job || spec.Level>level || spec.Cost>left || cooling?.Contains(spec.Id)==true)continue;
            chosen.Add(spec);left-=spec.Cost;
        }
        return chosen;
    }
}
