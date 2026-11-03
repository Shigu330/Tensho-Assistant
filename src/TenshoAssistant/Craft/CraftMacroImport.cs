using System.Text.RegularExpressions;

namespace TenshoAssistant.Craft;

/// <summary>
/// A-08 (0.425): a pasted crafting macro read back into simulator actions. Lines « /ac Name », « /action Name », with or without quotes
/// and « &lt;wait.N&gt; »-style tags; the name is matched (case ignored) to the job's actions in the game's language. Other commands
/// (« /echo », « /mlock », « /nextmacro »…) are ignored and counted; names the catalogue does not know are listed, never guessed.
/// </summary>
public sealed record CraftMacroImportResult(IReadOnlyList<CraftActionKind> Actions,IReadOnlyList<string> Unknown,int Ignored);

public static class CraftMacroImport
{
    public const int MaxText=8000,MaxActions=100;
    private static readonly Regex ActionLine=new(@"^/(?:ac|action)\s+(?:""(?<name>[^""]+)""|(?<name>[^<]+?))\s*(?:<[^>]*>\s*)*$",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));

    public static CraftMacroImportResult Parse(string? text,Func<string,CraftActionKind?> byName)
    {
        var actions=new List<CraftActionKind>();var unknown=new List<string>();var ignored=0;
        foreach(var raw in (text ?? "").Length>MaxText ? [] : (text ?? "").Split('\n'))
        {
            var line=raw.Trim();
            if(line.Length==0)continue;
            var match=ActionLine.Match(line);
            if(!match.Success){ignored++;continue;}
            var name=match.Groups["name"].Value.Trim();
            if(byName(name) is { } kind){if(actions.Count<MaxActions)actions.Add(kind);}
            else if(!unknown.Contains(name,StringComparer.OrdinalIgnoreCase))unknown.Add(name);
        }
        return new(actions,unknown,ignored);
    }
}
