using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;

namespace TenshoAssistant.Monsters;

/// <summary>One beast of the wiki's « Master's Bestiary »: its number (the XBMPet row: docs/evidence/0.804-beast-wiki.md), the enemy it is tamed from (English) and where.</summary>
public sealed record BeastWikiRow(uint Number,string Beast,string Enemy,string Location);
public sealed record BeastWikiBook(IReadOnlyList<BeastWikiRow> Rows,long Revision,DateTimeOffset FetchedAt);

/// <summary>0.804 (X-39): reads the wiki's « Beasts » page (No., Name, Tamed from, Location, Min. Level…).</summary>
public static class BeastWikiParser
{
    public const string Title="Beasts";
    private static MatchCollection Matches(string text,string pattern)=>Regex.Matches(text,pattern,RegexOptions.Singleline|RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
    private static string Text(string html)=>Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html,"<[^>]+>"," ",RegexOptions.Singleline,TimeSpan.FromSeconds(1))),@"\s+"," ",RegexOptions.None,TimeSpan.FromSeconds(1)).Trim();

    public static BeastWikiBook Parse(string json,DateTimeOffset fetchedAt)
    {
        if(json.Length>GarlandClient.MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});
        if(!doc.RootElement.TryGetProperty("parse",out var page))throw new InvalidDataException("Wiki page missing.");
        return new(Rows(page.GetProperty("text").GetProperty("*").GetString() ?? ""),page.TryGetProperty("revid",out var revision) ? revision.GetInt64():0,fetchedAt.ToUniversalTime());
    }

    /// <summary>The rows of the table whose headers hold « No. », « Name », « Tamed from » and « Location »; the first of each number.</summary>
    public static IReadOnlyList<BeastWikiRow> Rows(string html)
    {
        var rows=new Dictionary<uint,BeastWikiRow>();
        foreach(Match table in Matches(html,"<table\\b[^>]*>.*?</table>"))
        {
            var headers=Matches(table.Value,"<th\\b[^>]*>(.*?)</th>").Select(match=>Text(match.Groups[1].Value)).ToArray();
            int Column(string name)=>Array.FindIndex(headers,header=>header.StartsWith(name,StringComparison.OrdinalIgnoreCase));
            int number=Column("No."),name=Column("Name"),tamed=Column("Tamed from"),location=Column("Location");
            if(number<0 || name<0 || tamed<0)continue;
            foreach(Match row in Matches(table.Value,"<tr\\b[^>]*>(.*?)</tr>"))
            {
                var cells=Matches(row.Value,"<td\\b[^>]*>(.*?)</td>").Select(cell=>Text(cell.Groups[1].Value)).ToArray();
                if(cells.Length<=Math.Max(Math.Max(number,name),tamed) || !uint.TryParse(cells[number],NumberStyles.None,CultureInfo.InvariantCulture,out var no) || no==0 || rows.Count>=200)continue;
                rows.TryAdd(no,new(no,cells[name],cells[tamed],location>=0 && location<cells.Length ? cells[location]:""));
            }
        }
        if(rows.Count==0)throw new InvalidDataException("Bestiary table not found.");
        return rows.Values.OrderBy(row=>row.Number).ToArray();
    }

    /// <summary>The beasts (XBMPet rows) a monster (BNpcName row) gives, by the wiki's English enemy name.</summary>
    public static IReadOnlyDictionary<uint,IReadOnlyList<uint>> Map(IReadOnlyList<BeastWikiRow> rows,Func<string,IReadOnlyList<uint>> nameIds)
    {
        var result=new Dictionary<uint,List<uint>>();
        foreach(var row in rows)
            foreach(var id in nameIds(row.Enemy.Trim()))
            {
                if(!result.TryGetValue(id,out var beasts))result[id]=beasts=[];
                if(!beasts.Contains(row.Number))beasts.Add(row.Number);
            }
        return result.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<uint>)pair.Value);
    }
}

/// <summary>0.804 (X-39, user request): the wiki's bestiary, its copy on disk read without network, the page asked only on a click.</summary>
public sealed class BeastWikiIndex(DataCache? disk,Func<bool> enabled,HttpClient? client=null) : IDisposable
{
    private const string Family="wiki-beasts",Version="v1";
    private readonly HttpClient http=client ?? new HttpClient{Timeout=TimeSpan.FromSeconds(20)};
    private readonly CancellationTokenSource disposal=new();
    private BeastWikiBook? book;private int running,loaded,revision;private string? error;
    public BeastWikiBook? Book=>Volatile.Read(ref book);
    public int Revision=>Volatile.Read(ref revision);
    public bool Running=>Volatile.Read(ref running)==1;
    public string? Error=>Volatile.Read(ref error);

    public void LoadCached()
    {
        if(disk is null || Interlocked.Exchange(ref loaded,1)==1)return;
        var token=disposal.Token;
        _=Task.Run(async()=>
        {
            try{if(Book is null && await disk.ReadAsync<BeastWikiBook>(Family,"all",Version,token).ConfigureAwait(false) is { Value:{ Rows.Count:>0 } saved })Publish(saved);}
            catch(Exception){}
        },token);
    }

    public void Fetch()
    {
        if(Interlocked.CompareExchange(ref running,1,0)!=0)return;
        var token=disposal.Token;
        _=Task.Run(async()=>
        {
            try
            {
                if(!enabled()){Volatile.Write(ref error,"disabled");return;}
                var uri=new Uri("https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=parse&format=json&redirects=1&prop=text%7Crevid&page="+BeastWikiParser.Title);
                var fetched=BeastWikiParser.Parse(await BoundedHttp.ReadAsync(http,uri,GarlandClient.MaximumBytes,token).ConfigureAwait(false),DateTimeOffset.UtcNow);
                Publish(fetched);Volatile.Write(ref error,null);
                if(disk is not null)_=disk.WriteAsync(Family,"all",Version,fetched,fetched.FetchedAt);
            }
            catch(OperationCanceledException){}
            catch(Exception failure){Volatile.Write(ref error,failure.Message);}
            finally{Volatile.Write(ref running,0);}
        },token);
    }

    private void Publish(BeastWikiBook value){Volatile.Write(ref book,value);Interlocked.Increment(ref revision);}
    public void Dispose(){disposal.Cancel();if(client is null)http.Dispose();}
}
