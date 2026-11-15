namespace TenshoAssistant;

/// <summary>
/// X-389 (0.1367): one apostrophe in every search. The game's names (all sheets probed on 15/11, docs/evidence/0.1367-search-apostrophes.md)
/// and Tensho's texts (D-162) use the straight one; a player may type another form (curly, modifier letter, acute, grave, prime,
/// full-width): each one is the straight apostrophe for a search.
/// </summary>
public static class SearchText
{
    private static readonly char[] Others=['\u2019','\u2018','\u201B','\u02BC','\u00B4','`','\u2032','\uFF07'];

    /// <summary>The straight apostrophe for any apostrophe form; any other character unchanged.</summary>
    public static char Apostrophe(char character)=>Array.IndexOf(Others,character)>=0 ? '\'':character;

    /// <summary>The text with every apostrophe form made straight (the same string when there is none).</summary>
    public static string Fold(string text)
    {
        if(text.IndexOfAny(Others)<0)return text;
        return string.Create(text.Length,text,(span,source)=>{for(var index=0;index<source.Length;index++)span[index]=Apostrophe(source[index]);});
    }

    /// <summary>Whether <paramref name="text"/> holds <paramref name="search"/>, apostrophes folded on both sides; null text holds nothing.</summary>
    public static bool Contains(string? text,string search,StringComparison comparison=StringComparison.CurrentCultureIgnoreCase)
        =>text is not null && Fold(text).Contains(Fold(search),comparison);
}
