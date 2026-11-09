using System.Text.Json;

namespace TenshoAssistant.Diagnostics;

// X-92 (0.972, user request « prévoir les contenus à venir »): after a game update, what is new in the game's sheets and what Tensho does not cover
// without code. A snapshot of the watched sheets (rows, columns, last row id) and of the sheet names is kept; each load compares with the one before.
// Most Tensho lists are drawn from the sheets and follow an update by themselves; the sheets of Tables back a table written in the code, so their
// new rows are named as « needs a look ».
public sealed record SheetShape(int Rows,int Columns,uint LastRow);
public sealed record GameDataSnapshot(string Version,IReadOnlyDictionary<string,SheetShape> Sheets,IReadOnlyList<string> SheetNames);
public sealed record SheetChange(string Sheet,int RowsAdded,int ColumnsAdded,uint LastRowBefore,uint LastRowNow,bool Gone);
public sealed record GameDataReport(string? VersionBefore,string VersionNow,IReadOnlyList<string> NewSheets,IReadOnlyList<SheetChange> Changes,IReadOnlyList<string> NeedsLook)
{
    public bool First=>VersionBefore is null;
    public bool Changed=>!First && (VersionBefore!=VersionNow || NewSheets.Count>0 || Changes.Count>0);
}

public static class GameDataChanges
{
    /// <summary>The sheets Tensho reads for its lists (zones, contents, items, quests, collections, gathering, voyages, island, hunts…).</summary>
    public static readonly string[] Watched=["TerritoryType","ContentFinderCondition","ContentType","ExVersion","Item","ItemUICategory","Quest","Fate","Map","Aetheryte",
        "Recipe","GatheringItem","FishParameter","SpearfishingItem","Achievement","Mount","Companion","Orchestrion","TripleTriadCard","Ornament","Emote","Festival",
        "Weather","ClassJob","Leve","SubmarineExploration","AirshipExplorationPoint","MJIAnimals","AozAction","NotoriousMonster","IKDRoute","DeepDungeon",
        "BNpcName","ENpcResident","Status","AdventureExPhase","RetainerTask"];

    /// <summary>Sheets whose new rows need a table of the code looked at (the rest is drawn from the sheets).</summary>
    public static readonly IReadOnlyDictionary<string,string> Tables=new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["ExVersion"]="a new expansion: the tables kept per expansion (S rank conditions, sightseeing log numbers) need a look",
        ["NotoriousMonster"]="new hunt marks: the S rank conditions (Hunting/SRankConditions.cs) need a look",
        ["IKDRoute"]="new ocean fishing routes: the voyage timetable (Gathering/OceanFishing.cs) needs a look",
        ["Festival"]="new seasonal events: the festival names (Events/Data/festival-names.csv) need a look",
        ["ContentType"]="a new kind of content: the content families (X-90) need a look",
    };

    public static GameDataReport Compare(GameDataSnapshot? before,GameDataSnapshot now)
    {
        if(before is null)return new(null,now.Version,[],[],[]);
        var newSheets=now.SheetNames.Except(before.SheetNames,StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var changes=new List<SheetChange>();
        foreach(var (name,was) in before.Sheets.OrderBy(pair=>pair.Key,StringComparer.Ordinal))
        {
            if(!now.Sheets.TryGetValue(name,out var shape))
            {
                // Unreadable now is not gone: only a sheet missing from the game's list is.
                if(!now.SheetNames.Contains(name,StringComparer.Ordinal))changes.Add(new(name,-was.Rows,-was.Columns,was.LastRow,0,true));
                continue;
            }
            if(shape!=was)changes.Add(new(name,shape.Rows-was.Rows,shape.Columns-was.Columns,was.LastRow,shape.LastRow,false));
        }
        var needs=changes.Where(change=>!change.Gone && (change.RowsAdded>0 || change.LastRowNow>change.LastRowBefore) && Tables.ContainsKey(change.Sheet))
            .Select(change=>$"{change.Sheet}: {Tables[change.Sheet]}").ToArray();
        return new(before.Version,now.Version,newSheets,changes,needs);
    }

    /// <summary>The report as English log lines (the log stays in English).</summary>
    public static IReadOnlyList<string> Lines(GameDataReport report)
    {
        if(report.First)return [$"game data {report.VersionNow}: first snapshot kept."];
        if(!report.Changed)return [$"game data {report.VersionNow}: no change in the watched sheets since the last load."];
        var lines=new List<string>{$"game data {report.VersionBefore} -> {report.VersionNow}: {report.Changes.Count} watched sheets changed, {report.NewSheets.Count} new sheets."};
        if(report.NewSheets.Count>0)lines.Add("new sheets: "+string.Join(", ",report.NewSheets.Take(40))+(report.NewSheets.Count>40 ? $" (+{report.NewSheets.Count-40})":""));
        foreach(var change in report.Changes)
            lines.Add(change.Gone ? $"{change.Sheet}: gone from the game's sheets."
                :$"{change.Sheet}: {Signed(change.RowsAdded)} rows (last row {change.LastRowBefore} -> {change.LastRowNow}), {Signed(change.ColumnsAdded)} columns.");
        lines.AddRange(report.NeedsLook.Select(look=>"needs a look: "+look));
        return lines;
    }
    private static string Signed(int value)=>value>0 ? "+"+value:value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>One line for the self-check.</summary>
    public static (bool NeedsLook,string Detail) Summary(GameDataReport report)
        =>report.First ? (false,$"{report.VersionNow}, first snapshot")
        :!report.Changed ? (false,$"{report.VersionNow}, unchanged")
        :(report.NeedsLook.Count>0,$"{report.VersionBefore} -> {report.VersionNow}: {report.Changes.Count} sheets changed, {report.NewSheets.Count} new"
            +(report.NeedsLook.Count>0 ? ", needs a look: "+string.Join(", ",report.NeedsLook.Select(look=>look.Split(':')[0])):""));

    public static string Write(GameDataSnapshot snapshot)=>JsonSerializer.Serialize(snapshot);
    /// <summary>Null when the file cannot be read (then the next snapshot is taken as the first).</summary>
    public static GameDataSnapshot? Read(string text)
    {
        try{return JsonSerializer.Deserialize<GameDataSnapshot>(text) is { Version:not null, Sheets:not null, SheetNames:not null } snapshot ? snapshot:null;}
        catch(JsonException){return null;}
    }
}
