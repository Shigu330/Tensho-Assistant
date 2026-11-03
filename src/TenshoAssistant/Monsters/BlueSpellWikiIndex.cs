using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;

namespace TenshoAssistant.Monsters;

/// <summary>One spell of the wiki's « Blue Magic Spellbook » table: number, kind, player's minimum level, world monster and duty levels.</summary>
public sealed record BlueSpellbookRow(int Number,string Spell,string Type,int? MinLevel,int? WorldLevel,int? DutyLevel)
{
    /// <summary>Level of the monster to learn it from: the lower of the world monster's and the duty's level; null when neither is given.</summary>
    public int? MonsterLevel=>WorldLevel is { } world ? DutyLevel is { } duty ? Math.Min(world,duty):world:DutyLevel;
}

/// <summary>The spellbook page as read: rows by spell number, revision and when it was read.</summary>
public sealed record BlueSpellbook(IReadOnlyDictionary<int,BlueSpellbookRow> Rows,long Revision,DateTimeOffset FetchedAt);

/// <summary>Reads the wiki's « Blue Magic Spellbook » table (Spell, No., Rank, Type, Min Lv, World Mob L, Duty L, Common Acquisition Methods).</summary>
public static class BlueSpellbookParser
{
    public const string Title="Blue Magic Spellbook";
    private static MatchCollection Matches(string text,string pattern)=>Regex.Matches(text,pattern,RegexOptions.Singleline|RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
    private static string Text(string html)=>Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html,"<[^>]+>"," ",RegexOptions.Singleline,TimeSpan.FromSeconds(1))),@"\s+"," ",RegexOptions.None,TimeSpan.FromSeconds(1)).Trim();
    private static int? Number(string text)=>Regex.Match(text,@"\d{1,3}",RegexOptions.None,TimeSpan.FromSeconds(1)) is { Success:true } match ? int.Parse(match.Value,CultureInfo.InvariantCulture):null;

    public static BlueSpellbook Parse(string json,DateTimeOffset fetchedAt)
    {
        if(json.Length>GarlandClient.MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});
        if(!doc.RootElement.TryGetProperty("parse",out var page))throw new InvalidDataException("Wiki page missing.");
        var html=page.GetProperty("text").GetProperty("*").GetString() ?? "";
        var rows=new Dictionary<int,BlueSpellbookRow>();
        foreach(Match table in Matches(html,"<table\\b[^>]*>.*?</table>"))
        {
            var headers=Matches(table.Value,"<th\\b[^>]*>(.*?)</th>").Select(match=>Text(match.Groups[1].Value)).ToArray();
            int Column(string name)=>Array.FindIndex(headers,header=>header.StartsWith(name,StringComparison.OrdinalIgnoreCase));
            int spell=Column("Spell"),number=Column("No."),type=Column("Type"),min=Column("Min Lv"),world=Column("World Mob"),duty=Column("Duty");
            if(spell<0 || number<0 || world<0 || duty<0)continue;
            foreach(Match row in Matches(table.Value,"<tr\\b[^>]*>(.*?)</tr>"))
            {
                var cells=Matches(row.Value,"<td\\b[^>]*>(.*?)</td>").Select(cell=>Text(cell.Groups[1].Value)).ToArray();
                if(cells.Length<=Math.Max(Math.Max(spell,number),Math.Max(world,duty)) || Number(cells[number]) is not { } no || rows.Count>=400)continue;
                rows[no]=new(no,cells[spell],type>=0 && type<cells.Length ? cells[type]:"",min>=0 && min<cells.Length ? Number(cells[min]):null,Number(cells[world]),Number(cells[duty]));
            }
        }
        if(rows.Count==0)throw new InvalidDataException("Spellbook table not found.");
        return new(rows,page.TryGetProperty("revid",out var revision) ? revision.GetInt64():0,fetchedAt.ToUniversalTime());
    }
}

/// <summary>
/// 0.395 (user request: « the sort must be for every spell, cached when the page opens »): the wiki's spellbook table — one page for
/// every spell — read once when the Blue magic page opens: the saved copy (as long as the cache settings keep it), else one wiki
/// request. Honours the wiki switch. The monsters of one spell stay on its window (WikiMonsterSources, on click).
/// </summary>
public sealed class BlueSpellWikiIndex(Func<TimeSpan> lifetime,DataCache? disk,Func<bool> enabled,HttpClient? client=null) : IDisposable
{
    private const string Family="wiki-blue-spellbook",Version="v1";
    private readonly HttpClient http=client ?? new HttpClient{Timeout=TimeSpan.FromSeconds(20)};
    private readonly CancellationTokenSource disposal=new();
    private BlueSpellbook? book;
    private int running,revision;
    private string? error;
    private DateTimeOffset nextTry;

    public int Revision=>Volatile.Read(ref revision);
    public BlueSpellbook? Book=>Volatile.Read(ref book);
    public bool Running=>Volatile.Read(ref running)==1;
    public string? Error=>Volatile.Read(ref error);
    public BlueSpellbookRow? Get(int number)=>Book?.Rows.GetValueOrDefault(number);

    /// <summary>Called by the page each time it is drawn: reads the copy, or the wiki when the copy is missing or old (a failure waits a minute).</summary>
    public void Ensure()
    {
        if(disposal.IsCancellationRequested || DateTimeOffset.UtcNow<nextTry)return;
        if(Book is { } known && CacheAge.Fresh(known.FetchedAt,DateTimeOffset.UtcNow,lifetime()))return;
        if(Interlocked.CompareExchange(ref running,1,0)!=0)return;
        _=Task.Run(()=>Read(disposal.Token));
    }

    private async Task Read(CancellationToken token)
    {
        try
        {
            if(Book is null && disk is not null && await disk.ReadAsync<BlueSpellbook>(Family,"all",Version,token).ConfigureAwait(false) is { Value:{ Rows.Count:>0 } saved })
                Publish(saved);
            if(Book is { } copy && CacheAge.Fresh(copy.FetchedAt,DateTimeOffset.UtcNow,lifetime()))return;
            if(!enabled()){Volatile.Write(ref error,"disabled");nextTry=DateTimeOffset.UtcNow.AddMinutes(1);return;}
            var uri=new Uri("https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=parse&format=json&redirects=1&prop=text%7Crevid&page="+Uri.EscapeDataString(BlueSpellbookParser.Title.Replace(' ','_')));
            var fetched=BlueSpellbookParser.Parse(await BoundedHttp.ReadAsync(http,uri,GarlandClient.MaximumBytes,token).ConfigureAwait(false),DateTimeOffset.UtcNow);
            Publish(fetched);Volatile.Write(ref error,null);
            if(disk is not null)_=disk.WriteAsync(Family,"all",Version,fetched,fetched.FetchedAt);
        }
        catch(OperationCanceledException){}
        catch(Exception failure){Volatile.Write(ref error,failure.Message);nextTry=DateTimeOffset.UtcNow.AddMinutes(1);}
        finally{Volatile.Write(ref running,0);}
    }

    private void Publish(BlueSpellbook value){Volatile.Write(ref book,value);Interlocked.Increment(ref revision);}

    public void Dispose(){disposal.Cancel();if(client is null)http.Dispose();}
}
