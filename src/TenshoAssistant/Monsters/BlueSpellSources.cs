using System.Globalization;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Monsters;

/// <summary>Where a wiki row says a monster is: the English place name and, when given, its map coordinates.</summary>
public sealed record WikiPlace(string Place,float? X,float? Y);

/// <summary>
/// g, C-13 (0.367): blue magic sources from the wiki's spell page (the same « Enemy / Level / Location » table as item drops,
/// WikiMonsterParser), read on click only, facts only. A location « East Shroud (23.1,20.4) » gives a place and map coordinates;
/// a third number (height) is ignored.
/// </summary>
public static class BlueSpellSources
{
    private static readonly Regex WithCoordinates=new(@"^(?<place>.*?)\s*\(\s*(?<x>\d{1,2}(?:\.\d+)?)\s*,\s*(?<y>\d{1,2}(?:\.\d+)?)\s*(?:,\s*-?\d+(?:\.\d+)?\s*)?\)\s*$",
        RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));

    private static readonly Regex FirstNumber=new(@"\d{1,3}",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));

    /// <summary>0.390 (user request): a wiki level for sorting — « 43 » or « 43-48 » gives 43; none (empty, « ? ») gives null.</summary>
    public static int? Level(string level)=>FirstNumber.Match(level ?? "") is { Success:true } match ? int.Parse(match.Value,CultureInfo.InvariantCulture):null;

    /// <summary>0.390: rows by monster level (0 = wiki order, 1 = lowest first, 2 = highest first); rows without a level stay last, in wiki order.</summary>
    public static IReadOnlyList<WikiMonsterRow> SortByLevel(IReadOnlyList<WikiMonsterRow> rows,int order)
    {
        if(order is not (1 or 2))return rows;
        var indexed=rows.Select((row,index)=>(Row:row,Index:index,Level:Level(row.Level))).ToArray();
        var known=order==1 ? indexed.Where(entry=>entry.Level is not null).OrderBy(entry=>entry.Level).ThenBy(entry=>entry.Index)
            :indexed.Where(entry=>entry.Level is not null).OrderByDescending(entry=>entry.Level).ThenBy(entry=>entry.Index);
        return known.Concat(indexed.Where(entry=>entry.Level is null)).Select(entry=>entry.Row).ToArray();
    }

    /// <summary>
    /// 0.395 (user request): spells by that level (1 = lowest first, 2 = highest first); spells with no known level (not read yet, learnt
    /// otherwise) stay last in their first order. Order 0 keeps the list as given.
    /// </summary>
    public static T[] SortByMonsterLevel<T>(IReadOnlyList<T> spells,Func<T,int?> level,int order)
    {
        if(order is not (1 or 2))return spells.ToArray();
        var indexed=spells.Select((spell,index)=>(Spell:spell,Index:index,Level:level(spell))).ToArray();
        var known=indexed.Where(entry=>entry.Level is not null);
        known=order==1 ? known.OrderBy(entry=>entry.Level).ThenBy(entry=>entry.Index):known.OrderByDescending(entry=>entry.Level).ThenBy(entry=>entry.Index);
        return known.Concat(indexed.Where(entry=>entry.Level is null)).Select(entry=>entry.Spell).ToArray();
    }

    public static WikiPlace Parse(string location)
    {
        var text=(location ?? "").Trim();
        var match=WithCoordinates.Match(text);
        if(!match.Success)return new(text,null,null);
        return new(match.Groups["place"].Value.Trim(),float.Parse(match.Groups["x"].Value,CultureInfo.InvariantCulture),
            float.Parse(match.Groups["y"].Value,CultureInfo.InvariantCulture));
    }
}
