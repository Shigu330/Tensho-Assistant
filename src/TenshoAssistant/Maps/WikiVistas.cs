using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;

namespace TenshoAssistant.Maps;

/// <summary>
/// One row of the wiki's Sightseeing Log tables (ffxiv.consolegameswiki.com, raw wikitext). A Realm Reborn rows carry the
/// required weathers and Eorzea time, which the game data does not hold; later expansions give only emote and comment.
/// English text, community data: shown as such, never as game data.
/// </summary>
public sealed record WikiVista(int Section,int Number,string Name,string Zone,float X,float Y,IReadOnlyList<string> Weathers,string Time,string Emote,string Comment);
public sealed record WikiVistaDocument(DateTimeOffset FetchedAt,IReadOnlyList<WikiVista> Rows);

public static class WikiVistaParser
{
    public const string Url="https://ffxiv.consolegameswiki.com/mediawiki/index.php?title=Sightseeing_Log&action=raw";
    private static readonly TimeSpan Limit=TimeSpan.FromSeconds(1);
    private static string Clean(string text)
    {
        text=Regex.Replace(text,@"\[\[(?:[^\]|]*\|)?([^\]]*)\]\]","$1",RegexOptions.None,Limit);
        text=Regex.Replace(text,@"\{\{action icon\|([^}]*)\}\}","$1",RegexOptions.None,Limit);
        text=Regex.Replace(text,@"\{\{(?:i|item)\|([^}|]*)[^}]*\}\}","$1",RegexOptions.None,Limit);
        text=Regex.Replace(text,@"\{\{[^}]*\}\}","",RegexOptions.None,Limit);
        text=Regex.Replace(text,@"<[^>]+>"," ",RegexOptions.None,Limit).Replace("''","");
        return Regex.Replace(text,@"\s+"," ",RegexOptions.None,Limit).Trim();
    }
    // A cell may start with an attribute ("width=\"125px\"| …"): only the content after it counts.
    private static string Content(string cell)=>Regex.Match(cell,@"^\s*\w+=""[^""]*""\s*\|(?!\|)(.*)$",RegexOptions.Singleline,Limit) is { Success:true } match ? match.Groups[1].Value:cell;

    public static WikiVistaDocument Parse(string wikitext,DateTimeOffset fetchedAt)
    {
        ArgumentNullException.ThrowIfNull(wikitext);
        if(wikitext.Length>GarlandClient.MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        var rows=new List<WikiVista>();var section=-1;
        foreach(var raw in wikitext.Split('\n'))
        {
            var line=raw.TrimEnd('\r');
            // « ==A Realm Reborn Vistas== », « ==Heavensward Vistas== »…: numbering restarts in each expansion section.
            if(Regex.IsMatch(line,@"^==[^=].*Vistas\s*==\s*$",RegexOptions.None,Limit)){section++;continue;}
            if(section<0)continue;
            var row=Regex.Match(line,@"^\|\s*(\d+)\s*\|\|(.*)$",RegexOptions.None,Limit);
            if(!row.Success)continue;
            var cells=row.Groups[2].Value.Split("||").Select(Content).ToArray();
            // ARR: name, zone, coords, weather, time, emote, comment. Later: name, zone, coords, emote, comment.
            if(cells.Length is not (5 or 7))continue;
            var coords=Regex.Match(cells[2],@"\{\{coords\|\s*([0-9.]+)\s*,\s*([0-9.]+)",RegexOptions.None,Limit);
            if(!coords.Success)continue;
            var weathers=cells.Length==7 ? Regex.Matches(cells[3],@"\[\[([^\]|]+)(?:\|[^\]]*)?\]\]",RegexOptions.None,Limit).Select(match=>match.Groups[1].Value.Trim()).Distinct().ToArray():[];
            rows.Add(new(section,int.Parse(row.Groups[1].Value,CultureInfo.InvariantCulture),Clean(cells[0]),Clean(cells[1]),
                float.Parse(coords.Groups[1].Value,CultureInfo.InvariantCulture),float.Parse(coords.Groups[2].Value,CultureInfo.InvariantCulture),
                weathers,cells.Length==7 ? Clean(cells[4]):"",Clean(cells[^2]),Clean(cells[^1])));
            if(rows.Count>2000)throw new InvalidDataException("Too many wiki vista rows.");
        }
        if(rows.Count==0)throw new InvalidDataException("No vista table found on the wiki page.");
        return new(fetchedAt.ToUniversalTime(),rows.AsReadOnly());
    }

    /// <summary>
    /// Links wiki rows to game vistas (id, row index, map coordinates): same coordinates within 0.3 on the vista's map; A Realm
    /// Reborn rows must also have number = index + 1 (verified for #001–#003). Unmatched rows are dropped, never guessed.
    /// </summary>
    public static IReadOnlyDictionary<uint,WikiVista> Match(IEnumerable<WikiVista> rows,IEnumerable<(uint Id,int Index,float X,float Y)> vistas)
    {
        var games=vistas.ToArray();var result=new Dictionary<uint,WikiVista>();
        foreach(var row in rows)
        {
            var candidates=games.Where(game=>!result.ContainsKey(game.Id) && Math.Abs(game.X-row.X)<=0.3f && Math.Abs(game.Y-row.Y)<=0.3f
                && (row.Section!=0 || game.Index==row.Number-1)).ToArray();
            if(candidates.Length==1)result[candidates[0].Id]=row;
        }
        return result;
    }
}

public sealed record WikiVistaState(bool Loading,WikiVistaDocument? Document,string? Error);

/// <summary>Reads the wiki page once (bounded HTTP, 25 s), kept in DataCache « wiki-vistas » for the sources lifetime.</summary>
public sealed class WikiVistaSource:IDisposable
{
    private const string Family="wiki-vistas",Version="v1";
    private readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(25)};
    private readonly Func<TimeSpan> lifetime;private readonly DataCache? disk;
    private readonly CancellationTokenSource disposal=new();
    private WikiVistaState state=new(false,null,null);private bool started;
    public WikiVistaSource(Func<TimeSpan> lifetime,DataCache? disk){this.lifetime=lifetime;this.disk=disk;}
    public WikiVistaState State=>Volatile.Read(ref state);
    /// <summary>Starts the first load (disk cache, else network); later calls do nothing unless forced after an error.</summary>
    public void Request(bool retry=false)
    {
        lock(disposal)
        {
            if(disposal.IsCancellationRequested || started && !(retry && State.Error is not null))return;
            started=true;Volatile.Write(ref state,new(true,State.Document,null));
        }
        _=Task.Run(LoadAsync);
    }
    private async Task LoadAsync()
    {
        try
        {
            var token=disposal.Token;
            if(disk is not null && await disk.ReadAsync<WikiVistaDocument>(Family,"en",Version,token).ConfigureAwait(false) is { } saved
               && CacheAge.Fresh(saved.RecordedAt,DateTimeOffset.UtcNow,lifetime()) && saved.Value.Rows is { Count:>0 })
            {Volatile.Write(ref state,new(false,saved.Value,null));return;}
            var text=await BoundedHttp.ReadAsync(http,new Uri(WikiVistaParser.Url),GarlandClient.MaximumBytes,token).ConfigureAwait(false);
            var document=WikiVistaParser.Parse(text,DateTimeOffset.UtcNow);
            Volatile.Write(ref state,new(false,document,null));
            if(disk is not null)_=disk.WriteAsync(Family,"en",Version,document,document.FetchedAt);
        }
        catch(OperationCanceledException) when(disposal.IsCancellationRequested){}
        catch(Exception error){Volatile.Write(ref state,new(false,State.Document,error.Message));}
    }
    public void Dispose(){lock(disposal){if(disposal.IsCancellationRequested)return;disposal.Cancel();}http.Dispose();}
}
