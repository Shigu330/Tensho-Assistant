namespace TenshoAssistant.Npcs;

/// <summary>X-684 (0.1689): what an NPC's tooltip says: its game icon when it has one (the shop or exchange icon of the map), its name, where
/// it stands (zone and coordinates, a few, then how many more), its shops (each with how many items it sells; shops no sale was read for
/// counted apart), then what else Tensho knows of it (a Triple Triad match: fee and cards it may give, cards still missing, beaten or not,
/// the quests it asks for).</summary>
public sealed record NpcTipFacts(string Name,uint Icon,IReadOnlyList<string> Places,IReadOnlyList<string> Shops,IReadOnlyList<string> Notes);

public static class NpcTips
{
    /// <summary>Places shown before « and n more ».</summary>
    public const int MaxPlaces=3;
    /// <summary>Shops shown before « and n more » (as the map's labels since 0.137).</summary>
    public const int MaxShops=6;

    /// <summary>The lines of a list: blanks left out, each said once, in the order given, at most <paramref name="max"/>, then « and n more ».
    /// X-707 (0.1693): each said once through a set (List.Contains made it quadratic on a vendor's many places).</summary>
    public static IReadOnlyList<string> Lines(IEnumerable<string?> lines,int max,Func<int,string> more)
    {
        var kept=new List<string>();var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var line in lines)
            if(line is { } text && text.Trim() is { Length:>0 } trimmed && seen.Add(trimmed))kept.Add(trimmed);
        if(kept.Count<=max)return kept;
        return [..kept.Take(max),more(kept.Count-max)];
    }

    /// <summary>A vendor's shop lines: the shops read in the catalogue (at most MaxShops, then « and n more »), the shops with no sale read
    /// counted apart, « no shop read » when neither. An NPC of a service whose shop no sheet gives has no shop id: no line at all.</summary>
    public static IReadOnlyList<string> Shops(int shopIds,IEnumerable<string?> read,int unknown,Func<int,string> more,Func<int,string> unknownText,string none)
    {
        if(shopIds==0)return [];
        var shown=Lines(read,MaxShops,more).ToList();
        if(unknown>0)shown.Add(unknownText(unknown));
        if(shown.Count==0)shown.Add(none);
        return shown;
    }

    /// <summary>The facts with the name trimmed and the notes cleaned (blanks left out, each once).</summary>
    public static NpcTipFacts Build(string name,uint icon,IReadOnlyList<string> places,IReadOnlyList<string> shops,IEnumerable<string?> notes)
        =>new(name.Trim(),icon,places,shops,Lines(notes,int.MaxValue,_=>""));

    /// <summary>The same facts as text, for the Tensho map's labels (written as text, one per icon): the name, the shops and the notes; the
    /// places are left out there, the icon stands on its place.</summary>
    public static string Text(NpcTipFacts facts)
        =>string.Join("\n",new[]{facts.Name}.Concat(facts.Shops).Concat(facts.Notes));
}
