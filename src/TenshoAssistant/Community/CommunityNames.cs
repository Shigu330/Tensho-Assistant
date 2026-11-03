using System.Text.RegularExpressions;

namespace TenshoAssistant.Community;

/// <summary>
/// 0.407 (user rule, 2026-11-03): game data is shown in the game client's language. Monster, place and duty names read from English
/// community pages (Console Games Wiki, Garland Tools) are replaced by the game's own name when the game's English sheets know the
/// page's name; any other text (a name the game does not know, free text) stays as the page wrote it.
/// </summary>
public static class CommunityNames
{
    // Grammar markers some clients keep in their names (German « [a] », « [t] »…): not shown.
    private static readonly Regex Markers=new(@"\[[a-z]\]",RegexOptions.CultureInvariant);

    /// <summary>The game's name when known, capitalised like the page's (the game writes most monster names in lower case).</summary>
    public static string Name(string english,string? game)
    {
        var text=Markers.Replace(game ?? "","").Trim();
        if(text.Length==0)return english;
        return english.Length>0 && char.IsUpper(english[0]) && char.IsLower(text[0]) ? char.ToUpperInvariant(text[0])+text[1..]:text;
    }

    /// <summary>A wiki location: the place is translated, the coordinates that follow it are kept as written.</summary>
    public static string Location(string english,Func<string,string?> place)
    {
        var text=(english ?? "").Trim();
        if(text.Length==0)return text;
        var name=Monsters.BlueSpellSources.Parse(text).Place;
        if(name.Length==0 || !text.StartsWith(name,StringComparison.Ordinal))return text;
        return Name(name,place(name))+text[name.Length..];
    }
}
