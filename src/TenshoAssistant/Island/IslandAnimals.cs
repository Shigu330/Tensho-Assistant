using System.Globalization;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Island;

/// <summary>W-04 (0.685): one row of the wiki's animal tables. English text, community data: shown as such, never as game data. docs/evidence/0.685-island-animals.md.</summary>
public sealed record IslandAnimal(string Name,float X,float Y,string Size,string Restraint,string Common,string Bonus,string Time,string Weather);
public sealed record IslandAnimalDocument(DateTimeOffset FetchedAt,IReadOnlyList<IslandAnimal> Rows);

/// <summary>An animal of the player's pasture, copied from the game. Mood and food are as the game holds them: their scale is not established.</summary>
public sealed record PastureAnimal(int Slot,string Nickname,string Name,int Food,int Mood,bool Leavings,bool UnderCare);
public enum PastureReadStatus { Loaded,NotOnIsland,NotLoaded }
public sealed record PastureSnapshot(PastureReadStatus Status,IReadOnlyList<PastureAnimal> Animals);

public static class IslandAnimalBook
{
    public const string Url="https://ffxiv.consolegameswiki.com/mediawiki/index.php?title=Island_Pasture&action=render";
    public const int MaximumBytes=1024*1024;
    private static readonly TimeSpan Limit=TimeSpan.FromSeconds(1);
    private const string Table="<table class=\"npc sortable";

    /// <summary>The page's four tables, with 7 cells a row. A row that does not read is left out; a page with no row at all is an error, not an empty list.</summary>
    public static IslandAnimalDocument Parse(string html,DateTimeOffset fetchedAt)
    {
        ArgumentNullException.ThrowIfNull(html);
        if(html.Length>MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        var rows=new List<IslandAnimal>();
        for(var start=html.IndexOf(Table,StringComparison.Ordinal);start>=0;start=html.IndexOf(Table,start+Table.Length,StringComparison.Ordinal))
        {
            var end=html.IndexOf("</table>",start,StringComparison.Ordinal);
            if(end<0)break;
            foreach(var row in html[start..end].Split("<tr>").Skip(1))
                if(Row(row) is { } animal)rows.Add(animal);
            if(rows.Count>500)throw new InvalidDataException("Too many wiki animal rows.");
        }
        if(rows.Count==0)throw new InvalidDataException("No animal table found on the wiki page.");
        return new(fetchedAt.ToUniversalTime(),rows.AsReadOnly());
    }

    private static IslandAnimal? Row(string row)
    {
        var cells=row.Split("<td").Skip(1).ToArray();
        if(cells.Length!=7)return null;
        var names=Regex.Matches(cells[0],@"<a [^>]*>([^<]+)</a>",RegexOptions.None,Limit);
        var place=Regex.Match(cells[1],@"X:</small></b>\s*(\d+(?:\.\d+)?)\s*,\s*<b><small>Y:</small></b>\s*(\d+(?:\.\d+)?)",RegexOptions.None,Limit);
        var size=Regex.Match(cells[2],@"\[([SML])\]",RegexOptions.None,Limit);
        if(names.Count==0 || !place.Success || !size.Success)return null;
        if(!float.TryParse(place.Groups[1].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out var x) || !float.TryParse(place.Groups[2].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out var y))return null;
        var name=System.Net.WebUtility.HtmlDecode(names[^1].Groups[1].Value).Trim();
        return new(name,x,y,size.Groups[1].Value,Title(cells[2]),Title(cells[3]),Title(cells[4]),Text(cells[5]),Title(cells[6]));
    }
    private static string Title(string cell)=>Regex.Match(cell,@"title=""([^""]+)""",RegexOptions.None,Limit) is { Success:true } match ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim():"";
    // A cell starts after « <td », so its first « > » closes that tag.
    private static string Text(string cell)
    {
        var open=cell.IndexOf('>');
        var inner=open<0 ? cell:cell[(open+1)..];
        return Regex.Replace(inner,@"<[^>]+>","",RegexOptions.None,Limit).Trim();
    }

    /// <summary>« 12PM-3PM » as Eorzea hours, 12:00 and 15:00; empty or unreadable text stays out: an empty cell is no time condition.</summary>
    public static string EorzeaHours(string time)
    {
        var match=Regex.Match(time,@"^\s*(\d{1,2})\s*(AM|PM)\s*[-–]\s*(\d{1,2})\s*(AM|PM)\s*$",RegexOptions.None,Limit);
        if(!match.Success)return time.Trim();
        static int Hour(string hour,string half)=>int.Parse(hour,CultureInfo.InvariantCulture)%12+(half=="PM" ? 12:0);
        return $"{Hour(match.Groups[1].Value,match.Groups[2].Value):00}:00 – {Hour(match.Groups[3].Value,match.Groups[4].Value):00}:00";
    }

    /// <summary>The words the game knows, in its own language: names, items and weathers; a word it does not know stays in the wiki's English.</summary>
    public static IReadOnlyList<IslandAnimal> Localized(IReadOnlyList<IslandAnimal> rows,Func<string,string?> npc,Func<string,string?> item,Func<string,string?> weather)=>
        rows.Select(row=>row with
        {
            Name=npc(row.Name) ?? row.Name,Restraint=item(row.Restraint) ?? row.Restraint,Common=item(row.Common) ?? row.Common,Bonus=item(row.Bonus) ?? row.Bonus,
            Time=EorzeaHours(row.Time),Weather=weather(row.Weather) ?? row.Weather,
        }).ToArray();
}
