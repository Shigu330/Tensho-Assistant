using System.Globalization;
using System.Text;

namespace TenshoAssistant.Lexicon;

public enum LexiconKind { Item,Action,Trait,Status,Place,Monster,Npc,Quest,Duty,Mount,Minion,Fate,Job,Achievement,Emote,Interface }

/// <summary>W-16 (0.719): one word of the game in French and in English, whatever the client's language. Key: both, folded, for the search. docs/evidence/0.719-lexicon.md.</summary>
public sealed record LexiconEntry(LexiconKind Kind,string French,string English,string Key);

public static class LexiconBoard
{
    public const int MinimumQuery=2,Most=500;

    /// <summary>The sheets' soft hyphens and no-break spaces, which would stop a search and a paste.</summary>
    public static string Clean(string text)=>text.Replace("\u00AD","",StringComparison.Ordinal).Replace('\u00A0',' ').Replace('\u202F',' ').Trim();

    /// <summary>No case and no accent: « eureka » finds « Eurêka ».</summary>
    public static string Fold(string text)
    {
        var builder=new StringBuilder(text.Length);
        foreach(var character in text.Normalize(NormalizationForm.FormD))
            if(CharUnicodeInfo.GetUnicodeCategory(character)!=UnicodeCategory.NonSpacingMark)builder.Append(char.ToLowerInvariant(character));
        return builder.ToString();
    }

    /// <summary>Null when one side has no word: there is nothing to compare.</summary>
    public static LexiconEntry? Entry(LexiconKind kind,string french,string english)
    {
        var left=Clean(french);var right=Clean(english);
        return left.Length==0 || right.Length==0 ? null:new(kind,left,right,Fold(left)+"\n"+Fold(right));
    }

    /// <summary>Best first: the whole word, then its start, then anywhere; short words first. Total: every match, of which Most at most are returned.</summary>
    public static (IReadOnlyList<LexiconEntry> Rows,int Total) Search(IEnumerable<LexiconEntry> entries,string query,LexiconKind? kind)
    {
        var wanted=Fold(query.Trim());
        if(wanted.Length<MinimumQuery)return ([],0);
        var found=entries.Where(entry=>(kind is null || entry.Kind==kind) && entry.Key.Contains(wanted,StringComparison.Ordinal)).ToArray();
        return (found.OrderBy(entry=>Rank(entry,wanted)).ThenBy(entry=>entry.French.Length).ThenBy(entry=>entry.Kind).ThenBy(entry=>entry.French,StringComparer.Ordinal).Take(Most).ToArray(),found.Length);
    }

    private static int Rank(LexiconEntry entry,string wanted)
    {
        var cut=entry.Key.IndexOf('\n');
        var left=entry.Key.AsSpan(0,cut);var right=entry.Key.AsSpan(cut+1);
        if(left.Equals(wanted,StringComparison.Ordinal) || right.Equals(wanted,StringComparison.Ordinal))return 0;
        return left.StartsWith(wanted,StringComparison.Ordinal) || right.StartsWith(wanted,StringComparison.Ordinal) ? 1:2;
    }
}
