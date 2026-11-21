namespace TenshoAssistant.Maps;

/// <summary>X-684 (0.1673): what a monster's tooltip says: its name, its level (or a hunt mark's rank), its zone when known, and every
/// reason Tensho has to point at it (blue magic, hunting log, a list's item, FATE, collection, beast to tame, relic, wish, leve target).</summary>
public sealed record MonsterTipFacts(string Name,string? Level,string? Zone,IReadOnlyList<string> Notes);

public static class MonsterTips
{
    /// <summary>The facts with the reasons cleaned: blank ones left out, each said once, in the order given.</summary>
    public static MonsterTipFacts Build(string name,string? level,string? zone,IEnumerable<string?> notes)
    {
        var kept=new List<string>();
        foreach(var note in notes)
            if(note is { } text && text.Trim() is { Length:>0 } trimmed && !kept.Contains(trimmed))kept.Add(trimmed);
        return new(name.Trim(),level is { Length:>0 } ? level:null,zone is { Length:>0 } ? zone:null,kept);
    }
}
