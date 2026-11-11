using System.Text.Json;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Caching;

/// <summary>X-117 (0.1046): one array of a catalogue copy compared between two game versions; keys are capped at <see cref="PatchCatalogDiff.MaximumKeys"/>.</summary>
public sealed record PatchArrayDiff(string Name,int Added,int Removed,int Changed,int Same,
    IReadOnlyList<string> AddedKeys,IReadOnlyList<string> RemovedKeys,IReadOnlyList<string> ChangedKeys);

/// <summary>X-117 (0.1046): what a game patch changed in one catalogue copy (family + scope), or why it could not be compared.</summary>
public sealed record PatchCatalogReport(string Family,string Scope,string? OldGame,string? NewGame,DateTimeOffset At,string? Skipped,
    IReadOnlyList<PatchArrayDiff> Arrays)
{
    public int Added=>Arrays.Sum(array=>array.Added);
    public int Removed=>Arrays.Sum(array=>array.Removed);
    public int Changed=>Arrays.Sum(array=>array.Changed);
}

/// <summary>X-117 (0.1046): compares the catalogue copy kept from before a game patch with the one built from the new client.
/// Pure: works on the cached JSON, so every catalogue family is compared the same way, without its loader.</summary>
public static class PatchCatalogDiff
{
    public const int MaximumKeys=2000;
    private static readonly Regex GameVersionPattern=new(@"\bffxiv=(\d{4}\.\d{2}\.\d{2}\.\d{4}\.\d{4})",RegexOptions.CultureInvariant);
    private static readonly Regex DottedNumber=new(@"\d+(?:\.\d+)+",RegexOptions.CultureInvariant);
    private static readonly string[] KeyNames=["Id","RowId","ItemId","Key"];

    /// <summary>The base game version written in a cache version string (« ffxiv=2026.11.03.0000.0000 »), or null.</summary>
    public static string? GameVersion(string? cacheVersion)=>cacheVersion is null ? null
        : GameVersionPattern.Match(cacheVersion) is {Success:true} match ? match.Groups[1].Value:null;

    /// <summary>Both copies hold the same record shape, language and copy rules: only version numbers (game, plugin, Dalamud, Lumina) differ.</summary>
    public static bool Comparable(string oldVersion,string newVersion)=>
        DottedNumber.Replace(oldVersion,"#")==DottedNumber.Replace(newVersion,"#");

    public static PatchCatalogReport Compare(string family,string scope,string oldVersion,JsonElement oldValue,string newVersion,JsonElement newValue,DateTimeOffset at)
    {
        string? oldGame=GameVersion(oldVersion),newGame=GameVersion(newVersion);
        if(oldGame is null || newGame is null)return new(family,scope,oldGame,newGame,at,"no game version",[]);
        if(oldGame==newGame)return new(family,scope,oldGame,newGame,at,"same game version",[]);
        if(!Comparable(oldVersion,newVersion))return new(family,scope,oldGame,newGame,at,"record shape changed",[]);
        if(oldValue.ValueKind!=JsonValueKind.Object || newValue.ValueKind!=JsonValueKind.Object)return new(family,scope,oldGame,newGame,at,"not an object",[]);
        var arrays=new List<PatchArrayDiff>();
        foreach(var property in newValue.EnumerateObject())
            if(property.Value.ValueKind==JsonValueKind.Array && oldValue.TryGetProperty(property.Name,out var before) && before.ValueKind==JsonValueKind.Array)
                arrays.Add(CompareArray(property.Name,before,property.Value));
        return new(family,scope,oldGame,newGame,at,arrays.Count==0 ? "no list to compare":null,arrays);
    }

    public static PatchArrayDiff CompareArray(string name,JsonElement before,JsonElement after)
    {
        var old=Index(before);var current=Index(after);
        if(old is null || current is null){old=ByText(before);current=ByText(after);}
        List<string> added=[],removed=[],changed=[];int addedCount=0,removedCount=0,changedCount=0,same=0;
        foreach(var (key,text) in current)
        {
            if(!old.TryGetValue(key,out var previous)){addedCount++;if(added.Count<MaximumKeys)added.Add(key);}
            else if(previous!=text){changedCount++;if(changed.Count<MaximumKeys)changed.Add(key);}
            else same++;
        }
        foreach(var key in old.Keys)
            if(!current.ContainsKey(key)){removedCount++;if(removed.Count<MaximumKeys)removed.Add(key);}
        return new(name,addedCount,removedCount,changedCount,same,added,removed,changed);
    }

    // Rows keyed by their identifier (Id, RowId, ItemId or Key); null when a row has none or two rows share one.
    private static Dictionary<string,string>? Index(JsonElement array)
    {
        var rows=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var row in array.EnumerateArray())
        {
            if(row.ValueKind!=JsonValueKind.Object)return null;
            string? key=null;
            foreach(var name in KeyNames)
                if(row.TryGetProperty(name,out var value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.String){key=value.ToString();break;}
            if(key is null || !rows.TryAdd(key,row.GetRawText()))return null;
        }
        return rows;
    }
    // Rows without an identifier: the row text is its own key (then a change shows as one row removed and one added).
    private static Dictionary<string,string> ByText(JsonElement array)
    {
        var rows=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var row in array.EnumerateArray()){var text=row.GetRawText();rows.TryAdd(text,text);}
        return rows;
    }

    /// <summary>One English line per report, for the log.</summary>
    public static string Describe(PatchCatalogReport report)=>report.Skipped is { } reason
        ? $"{report.Family}: {report.OldGame ?? "?"} -> {report.NewGame ?? "?"} not compared ({reason})."
        : $"{report.Family}: {report.OldGame} -> {report.NewGame}, {report.Added} added, {report.Removed} removed, {report.Changed} changed ("
          +string.Join(", ",report.Arrays.Select(array=>$"{array.Name} +{array.Added} -{array.Removed} ~{array.Changed}"))+").";
}
