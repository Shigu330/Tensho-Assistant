using System.Globalization;
using System.Text;

namespace TenshoAssistant.Help;

public enum QuickSearchKind { Page, Item, Quest, Place }

/// <summary>One answer of the main window's search: what it is, the id or page name it opens, and the text shown.</summary>
public sealed record QuickSearchHit(QuickSearchKind Kind,uint Id,string Key,string Text,string Detail="");

/// <summary>
/// X-220 (0.1230, user's request of 12/11, K-36): the search at the top of the main window's navigation joins the indexes Tensho already has
/// (pages and their help, items, quests, maps) and opens the page or the sheet on a click. No teleport by typing a name (P24) and no global
/// keyboard shortcut (P21): it only opens Tensho's own pages. Every word typed must be in the text, accents and case ignored; names that start
/// with the first word come first, then the order of the source.
/// </summary>
public static class QuickSearch
{
    public const int MinLength=2,PerKind=5,MaxLength=64;

    /// <summary>Upper case, accents removed, every blank a single space.</summary>
    public static string Normalize(string value)
    {
        var text=new StringBuilder(value.Length);
        foreach(var character in value.Normalize(NormalizationForm.FormD))
        {
            if(CharUnicodeInfo.GetUnicodeCategory(character)==UnicodeCategory.NonSpacingMark)continue;
            text.Append(char.IsWhiteSpace(character) ? ' ':char.ToUpperInvariant(character));
        }
        return text.ToString();
    }

    /// <summary>The words of a query (normalized); empty when it is shorter than <see cref="MinLength"/> or longer than <see cref="MaxLength"/>.</summary>
    public static string[] Tokens(string query)
    {
        var trimmed=(query ?? "").Trim();
        if(trimmed.Length is < MinLength or > MaxLength)return [];
        return Normalize(trimmed).Split(' ',StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool Matches(string normalized,IReadOnlyList<string> tokens)
    {
        if(tokens.Count==0)return false;
        foreach(var token in tokens)if(!normalized.Contains(token,StringComparison.Ordinal))return false;
        return true;
    }

    /// <summary>The first <paramref name="limit"/> entries whose text holds every word; those starting with the first word first, then source order.</summary>
    public static IReadOnlyList<T> Top<T>(IEnumerable<T> source,Func<T,string> text,IReadOnlyList<string> tokens,int limit=PerKind)
    {
        if(tokens.Count==0 || limit<=0)return [];
        var first=new List<T>(limit);var rest=new List<T>(limit);
        foreach(var entry in source)
        {
            var name=Normalize(text(entry));
            if(!Matches(name,tokens))continue;
            if(name.StartsWith(tokens[0],StringComparison.Ordinal)){first.Add(entry);if(first.Count==limit)break;}
            else if(rest.Count<limit)rest.Add(entry);
        }
        return first.Concat(rest).Take(limit).ToArray();
    }
}
