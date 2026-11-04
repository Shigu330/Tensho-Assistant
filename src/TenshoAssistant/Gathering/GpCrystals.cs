namespace TenshoAssistant.Gathering;

public sealed record GpCrystalAction(uint Id,uint JobId,int Level,int Cost);

/// <summary>
/// 0.519 (user report): shards, crystals and clusters have their own actions, and what they give is random: the game gives no figure, so no amount is computed here. Rows and texts: docs/evidence/0.519-crystal-actions.md.
/// 0.521 (user report in game): both work on all three, although the game's text for The Twelve's Bounty only names shards. Neither has an unlock link: they come with the level.
/// </summary>
public static class GpCrystals
{
    public const uint UiCategory=59;
    private static readonly GpCrystalAction[] Specs=
    [
        new(4589,16,74,200),new(280,16,20,150),
        new(4590,17,74,200),new(282,17,20,150),
    ];

    public static IReadOnlyList<GpCrystalAction> For(uint job,int level,int gp)
    {
        var chosen=new List<GpCrystalAction>();var left=gp;
        foreach(var spec in Specs)
        {
            if(spec.JobId!=job || spec.Level>level || spec.Cost>left)continue;
            chosen.Add(spec);left-=spec.Cost;
        }
        return chosen;
    }
}
