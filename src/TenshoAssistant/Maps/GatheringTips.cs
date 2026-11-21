namespace TenshoAssistant.Maps;

/// <summary>X-684 (0.1694): what a gathering point's tooltip says, worked out once per point: its title (kind of gathering, level, hidden), a
/// detail line (the job, the kind of point when it is not a plain one), its place, the items it gives (each once, in the game's order, with its
/// name). Whether it is open now, for how long, and what it asks of the player (folklore tome, level) are read live by the window.</summary>
public sealed record GatheringTipFacts(string Title,string? Detail,string? Place,IReadOnlyList<(uint Id,string Name)> Items);

public static class GatheringTips
{
    /// <summary>Items shown in a point's own tooltip before « and n more » (16, as before 0.1694, which left the rest out without a word).</summary>
    public const int MaxItems=16;
    /// <summary>Items shown for each point among the icons under the cursor on the Tensho map (several points in one tooltip).</summary>
    public const int MaxListItems=8;

    /// <summary>The facts: title trimmed, the detail parts joined (blanks left out, each once), a blank place left out, each item once.</summary>
    public static GatheringTipFacts Build(string title,IEnumerable<string?> details,string? place,IEnumerable<(uint Id,string Name)> items)
    {
        var parts=new List<string>();
        foreach(var part in details)
            if(part?.Trim() is { Length:>0 } kept && !parts.Contains(kept))parts.Add(kept);
        var seen=new HashSet<uint>();
        return new(title.Trim(),parts.Count>0 ? string.Join(" · ",parts):null,place?.Trim() is { Length:>0 } shown ? shown:null,
            items.Where(item=>seen.Add(item.Id)).ToArray());
    }

    /// <summary>How many items are left out when at most <paramref name="max"/> are shown (« and n more »).</summary>
    public static int More(GatheringTipFacts facts,int max)=>Math.Max(0,facts.Items.Count-max);
}
