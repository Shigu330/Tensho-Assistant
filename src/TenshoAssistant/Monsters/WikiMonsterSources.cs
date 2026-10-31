using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;

namespace TenshoAssistant.Monsters;

public sealed record WikiMonsterRow(string Name,string Level,string Location);
public sealed record WikiMonsterDocument(uint ItemId,string Title,string Url,long Revision,DateTimeOffset FetchedAt,IReadOnlyList<WikiMonsterRow> Monsters);
public sealed record WikiMonsterState(uint ItemId,bool Loading,WikiMonsterDocument? Document,string? Error);

public static class WikiMonsterParser
{
    private static MatchCollection Matches(string text,string pattern)=>Regex.Matches(text,pattern,RegexOptions.Singleline|RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
    private static string Text(string html)=>WebUtility.HtmlDecode(Regex.Replace(html,"<[^>]+>"," ",RegexOptions.Singleline,TimeSpan.FromSeconds(1))).Trim();
    public static string Url(string title)=>"https://ffxiv.consolegameswiki.com/wiki/"+Uri.EscapeDataString(title.Replace(' ','_'));
    public static WikiMonsterDocument Parse(string json,uint itemId,string expectedTitle,DateTimeOffset fetchedAt)
    {
        if(json.Length>GarlandClient.MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});
        var root=doc.RootElement;
        // A missing page is an answer, not a failure: the wiki has no drop table for this title.
        if(root.TryGetProperty("error",out var error) && error.TryGetProperty("code",out var code) && code.GetString()=="missingtitle")
            return new(itemId,expectedTitle,Url(expectedTitle),0,fetchedAt.ToUniversalTime(),Array.Empty<WikiMonsterRow>());
        if(!root.TryGetProperty("parse",out var page) || page.GetProperty("title").GetString() is not { Length:>0 } title)
            throw new InvalidDataException("Wiki page missing.");
        // The requested title must be the page itself or the source of a redirect the wiki followed.
        var redirected=page.TryGetProperty("redirects",out var redirects) && redirects.ValueKind==JsonValueKind.Array
            && redirects.EnumerateArray().Any(entry=>entry.TryGetProperty("from",out var from) && from.GetString()==expectedTitle
                && entry.TryGetProperty("to",out var to) && to.GetString()==title);
        if(title!=expectedTitle && !redirected)throw new InvalidDataException("Wiki title mismatch.");
        expectedTitle=title;
        var html=page.GetProperty("text").GetProperty("*").GetString() ?? "";
        var rows=new List<WikiMonsterRow>();
        foreach(Match table in Matches(html,"<table\\b[^>]*class=\"[^\"]*\\bitem\\b[^\"]*\"[^>]*>.*?</table>"))
        {
            var headers=Matches(table.Value,"<th\\b[^>]*>(.*?)</th>").Select(match=>Text(match.Groups[1].Value)).ToArray();
            if(headers.Length!=3 || headers[0]!="Enemy" || headers[1]!="Level" || headers[2]!="Location")continue;
            foreach(Match row in Matches(table.Value,"<tr\\b[^>]*>(.*?)</tr>"))
            {
                var cells=Matches(row.Value,"<td\\b[^>]*>(.*?)</td>");
                if(cells.Count==0)continue;
                if(cells.Count!=3 || rows.Count>=512)throw new InvalidDataException("Unsupported wiki monster table.");
                var values=cells.Select(cell=>Text(cell.Groups[1].Value)).ToArray();
                if(values.Any(value=>value.Length>1024) || values[0].Length==0)throw new InvalidDataException("Invalid wiki monster row.");
                rows.Add(new(values[0],values[1],values[2]));
            }
        }
        return new(itemId,expectedTitle,Url(expectedTitle),page.GetProperty("revid").GetInt64(),fetchedAt.ToUniversalTime(),Array.AsReadOnly(rows.ToArray()));
    }
}

/// <summary>Item categories that can hold monster drops (ItemUICategory, verified against the local catalogue copy on 2026-10-30).</summary>
public static class MonsterDropCandidates
{
    // 45 ingredient, 48 stone, 49 metal, 50 lumber, 51 cloth, 52 leather, 53 bone, 54 reagent.
    public static readonly IReadOnlySet<uint> Categories=new HashSet<uint>{45,48,49,50,51,52,53,54};
    public static bool Eligible(Items.ItemDefinition? item,bool craftable,bool gathered)
        =>item is { Equipment:Items.EquipmentSlots.None } && item.EnglishName.Length>0 && Categories.Contains(item.UiCategoryId) && !craftable && !gathered;
}

/// <summary>
/// Primary monster source: Console Games Wiki drop tables, titled by the game's own English item name (no Garland lookup).
/// One shared cadence, memory and disk copy for the window and the hover.
/// </summary>
public sealed class WikiMonsterSources : IDisposable
{
    public static readonly TimeSpan RequestInterval=TimeSpan.FromSeconds(3);
    private const string Family="wiki-monsters",Version="wiki-parse-v1";
    private readonly object gate=new();
    private readonly Func<uint,string?> englishName;
    private readonly Func<TimeSpan> lifetime;
    private readonly HttpClient http;
    private readonly bool ownsHttp;
    private readonly DataCache? disk;
    private readonly CancellationTokenSource disposal=new();
    private readonly Dictionary<uint,WikiMonsterDocument> cache=[];
    private CancellationTokenSource? pending;
    private long generation;
    private bool disposed;
    private DateTimeOffset lastRequest;
    private WikiMonsterState state=new(0,false,null,null);
    public WikiMonsterState State=>Volatile.Read(ref state);
    public WikiMonsterSources(Func<uint,string?> englishName,Func<TimeSpan> lifetime,DataCache? disk=null,HttpClient? http=null)
    {this.englishName=englishName;this.lifetime=lifetime;this.disk=disk;this.http=http ?? new HttpClient{Timeout=TimeSpan.FromSeconds(15)};ownsHttp=http is null;}
    public bool IsFresh(WikiMonsterDocument document)=>CacheAge.Fresh(document.FetchedAt,DateTimeOffset.UtcNow,lifetime());

    /// <summary>Memory or disk copy, whatever its age; never touches the network.</summary>
    public async Task<WikiMonsterDocument?> ReadCachedAsync(uint itemId,CancellationToken token)
    {
        lock(gate){if(disposed)return null;if(cache.TryGetValue(itemId,out var known))return known;}
        if(disk is null)return null;
        var saved=await disk.ReadAsync<WikiMonsterDocument>(Family,itemId.ToString(),Version,token).ConfigureAwait(false);
        if(saved?.Value is not { } document || document.ItemId!=itemId || document.Monsters is null)return null;
        lock(gate){if(!disposed)Remember(document,false);}
        return document;
    }

    /// <summary>Network lookup under the shared cadence; throws InvalidOperationException when another request ran less than three seconds ago.</summary>
    public async Task<WikiMonsterDocument> FetchAsync(uint itemId,CancellationToken token)
    {
        var title=englishName(itemId);
        if(string.IsNullOrWhiteSpace(title) || title.Length>256)throw new InvalidDataException("English item name unavailable.");
        lock(gate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            if(DateTimeOffset.UtcNow-lastRequest<RequestInterval)throw new InvalidOperationException("Please wait three seconds before another wiki request.");
            lastRequest=DateTimeOffset.UtcNow;
        }
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,disposal.Token);
        linked.CancelAfter(TimeSpan.FromSeconds(25));
        var uri=new Uri("https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=parse&format=json&redirects=1&prop=text%7Crevid&page="+Uri.EscapeDataString(title.Replace(' ','_')));
        var json=await BoundedHttp.ReadAsync(http,uri,GarlandClient.MaximumBytes,linked.Token).ConfigureAwait(false);
        var result=WikiMonsterParser.Parse(json,itemId,title.Replace('_',' '),DateTimeOffset.UtcNow);
        lock(gate){if(!disposed)Remember(result,true);}
        return result;
    }

    private void Remember(WikiMonsterDocument document,bool persist)
    {
        if(!cache.ContainsKey(document.ItemId) && cache.Count>=128)cache.Remove(cache.MinBy(pair=>pair.Value.FetchedAt).Key);
        cache[document.ItemId]=document;
        if(state.ItemId==document.ItemId && !state.Loading)state=state with{Document=document};
        if(persist && disk is not null)_=disk.WriteAsync(Family,document.ItemId.ToString(),Version,document,document.FetchedAt);
    }

    /// <summary>Selects the item of the window and shows its cached copy at once, without network.</summary>
    public void Select(uint itemId)
    {
        lock(gate)
        {
            if(disposed || state.ItemId==itemId)return;
            generation++;pending?.Cancel();state=new(itemId,false,cache.GetValueOrDefault(itemId),null);
        }
        if(itemId!=0)_=ReadCachedAsync(itemId,disposal.Token);
    }
    public async Task RequestAsync(bool force=false)
    {
        CancellationTokenSource source;long ticket;uint itemId;
        lock(gate)
        {
            if(disposed || state.Loading || state.ItemId==0)return;
            itemId=state.ItemId;
            if(!force && cache.TryGetValue(itemId,out var saved) && IsFresh(saved)){state=state with{Document=saved,Error=null};return;}
            source=new CancellationTokenSource();pending=source;ticket=++generation;state=state with{Loading=true,Error=null};
        }
        try
        {
            var result=!force && await ReadCachedAsync(itemId,source.Token).ConfigureAwait(false) is { } saved && IsFresh(saved)
                ? saved:await FetchAsync(itemId,source.Token).ConfigureAwait(false);
            lock(gate){if(!disposed && ticket==generation)state=new(itemId,false,result,null);}
        }
        catch(Exception error)
        {lock(gate)if(!disposed && ticket==generation)state=state with{Loading=false,Error=error.Message};}
        finally
        {lock(gate){if(ReferenceEquals(pending,source))pending=null;source.Dispose();}}
    }
    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;generation++;pending?.Cancel();disposal.Cancel();cache.Clear();state=new(0,false,null,null);if(ownsHttp)http.Dispose();}
    }
}
