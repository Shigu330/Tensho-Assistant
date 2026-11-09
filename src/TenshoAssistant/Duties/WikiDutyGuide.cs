using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;

namespace TenshoAssistant.Duties;

/// <summary>One section of a duty's wiki page: its heading and its text (paragraphs, lists, table cells), plain.</summary>
public sealed record WikiGuideSection(string Title,string Text);
public sealed record WikiDutyGuide(uint DutyId,string Title,string Url,long Revision,DateTimeOffset FetchedAt,IReadOnlyList<WikiGuideSection> Sections);
public sealed record WikiDutyGuideState(uint DutyId,bool Loading,WikiDutyGuide? Document,string? Error);

/// <summary>
/// X-90 (0.994, DEC-045: « mechanics and strategies from the wiki, GFDL, on a click, credited »): a duty's page on the FFXIV Console Games Wiki (by its
/// English name), cut into its sections. Sections that are not about the fight are left out (loot, rewards, achievements, gallery, story, dialogue,
/// trivia, patch history, links). The text stays the wiki's, in English, credited where it is shown.
/// </summary>
public static class WikiDutyGuideParser
{
    public const int MaximumSections=30,MaximumText=4000;
    private static readonly string[] Skipped=["loot","reward","achievement","gallery","image","trivia","patch","lore","story","dialogue","reference",
        "external","navigation","treasure","chest","video","unlock","related","see also","music","sound","quest","currency"];
    private static string Strip(string html,string pattern)=>Regex.Replace(html,pattern,"",RegexOptions.Singleline|RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
    public static string Url(string title)=>"https://ffxiv.consolegameswiki.com/wiki/"+Uri.EscapeDataString(title.Replace(' ','_'));

    public static string Text(string html)
    {
        html=Strip(html,"<span[^>]*class=\"mw-editsection[^\"]*\"[^>]*>.*?</span>\\s*</span>|<span[^>]*class=\"mw-editsection[^\"]*\"[^>]*>.*?</span>");
        html=Strip(html,"<(script|style|sup)\\b[^>]*>.*?</\\1>");
        html=Regex.Replace(html,"</p>|<br\\s*/?>|</li>|</tr>|</dd>|</dt>|</h[1-6]>","\n",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
        html=Regex.Replace(html,"</td>|</th>"," · ",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
        var text=WebUtility.HtmlDecode(Regex.Replace(html,"<[^>]+>","",RegexOptions.Singleline,TimeSpan.FromSeconds(1)));
        var lines=text.Split('\n').Select(line=>Regex.Replace(line,"[ \\t\u00A0]+"," ",RegexOptions.None,TimeSpan.FromSeconds(1)).Trim(' ','·')).Where(line=>line.Length>0);
        return string.Join("\n",lines);
    }

    public static IReadOnlyList<WikiGuideSection> Sections(string html)
    {
        var headings=Regex.Matches(html,"<h([2-4])\\b[^>]*>(.*?)</h\\1>",RegexOptions.Singleline|RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
        var sections=new List<WikiGuideSection>();
        for(var index=0;index<headings.Count && sections.Count<MaximumSections;index++)
        {
            var title=Text(headings[index].Groups[2].Value).Replace("\n"," ").Trim();
            if(title.Length==0 || Skipped.Any(word=>title.Contains(word,StringComparison.OrdinalIgnoreCase)))continue;
            var start=headings[index].Index+headings[index].Length;
            var end=index+1<headings.Count ? headings[index+1].Index:html.Length;
            var text=Text(html[start..end]);
            if(text.Length==0)continue;
            sections.Add(new(title,text.Length<=MaximumText ? text:text[..MaximumText]+" …"));
        }
        return sections;
    }

    public static WikiDutyGuide Parse(string json,uint dutyId,string expectedTitle,DateTimeOffset fetchedAt)
    {
        if(json.Length>GarlandClient.MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});
        if(!doc.RootElement.TryGetProperty("parse",out var page))throw new InvalidDataException("Wiki page missing.");
        var title=page.TryGetProperty("title",out var named) ? named.GetString() ?? expectedTitle:expectedTitle;
        return new(dutyId,title,Url(title),page.TryGetProperty("revid",out var revision) ? revision.GetInt64():0,fetchedAt.ToUniversalTime(),
            Sections(page.GetProperty("text").GetProperty("*").GetString() ?? ""));
    }
}

/// <summary>X-90 (0.994): the duty guides, on a click only, three seconds apart, kept in memory and on disk; the wiki switch stops the requests.</summary>
public sealed class WikiDutyGuides(Func<uint,string?> englishName,Func<TimeSpan> lifetime,DataCache? disk=null,HttpClient? client=null) : IDisposable
{
    private const string Family="wiki-duty-guides",Version="wiki-guide-v1";
    private readonly object gate=new();
    private readonly HttpClient http=client ?? new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
    private readonly bool ownsHttp=client is null;
    private readonly CancellationTokenSource disposal=new();
    private readonly Dictionary<uint,WikiDutyGuide> cache=[];
    private WikiDutyGuideState state=new(0,false,null,null);
    private DateTimeOffset lastRequest;
    private long generation;
    private bool disposed;
    public Func<bool>? Enabled { get; set; }
    public WikiDutyGuideState State=>Volatile.Read(ref state);

    /// <summary>The duty shown; its copy in memory or on disk, never the network.</summary>
    public void Select(uint dutyId)
    {
        lock(gate){if(disposed || state.DutyId==dutyId)return;generation++;state=new(dutyId,false,cache.GetValueOrDefault(dutyId),null);}
        if(dutyId!=0 && disk is not null)_=ReadDiskAsync(dutyId);
    }
    private async Task ReadDiskAsync(uint dutyId)
    {
        try
        {
            var saved=await disk!.ReadAsync<WikiDutyGuide>(Family,dutyId.ToString(System.Globalization.CultureInfo.InvariantCulture),Version,disposal.Token).ConfigureAwait(false);
            if(saved?.Value is not { Sections:not null } document || document.DutyId!=dutyId)return;
            lock(gate){if(disposed)return;cache.TryAdd(dutyId,document);if(state.DutyId==dutyId && state.Document is null)state=state with{Document=document};}
        }
        catch(Exception){}
    }

    public async Task RequestAsync(bool force=false)
    {
        uint dutyId;long ticket;string? title;
        lock(gate)
        {
            if(disposed || state.Loading || state.DutyId==0)return;
            dutyId=state.DutyId;
            if(!force && cache.TryGetValue(dutyId,out var known) && CacheAge.Fresh(known.FetchedAt,DateTimeOffset.UtcNow,lifetime())){state=state with{Document=known,Error=null};return;}
            if(Enabled?.Invoke()==false){state=state with{Error=new SourceDisabledException("FFXIV Console Games Wiki").Message};return;}
            if(DateTimeOffset.UtcNow-lastRequest<TimeSpan.FromSeconds(3)){state=state with{Error="Please wait three seconds before another wiki request."};return;}
            title=englishName(dutyId);
            if(string.IsNullOrWhiteSpace(title) || title.Length>256){state=state with{Error="English duty name unavailable."};return;}
            lastRequest=DateTimeOffset.UtcNow;ticket=++generation;state=state with{Loading=true,Error=null};
        }
        try
        {
            using var linked=CancellationTokenSource.CreateLinkedTokenSource(disposal.Token);
            linked.CancelAfter(TimeSpan.FromSeconds(25));
            var uri=new Uri("https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=parse&format=json&redirects=1&prop=text%7Crevid&page="+Uri.EscapeDataString(title.Replace(' ','_')));
            var json=await BoundedHttp.ReadAsync(http,uri,GarlandClient.MaximumBytes,linked.Token).ConfigureAwait(false);
            var document=WikiDutyGuideParser.Parse(json,dutyId,title,DateTimeOffset.UtcNow);
            lock(gate)
            {
                if(disposed)return;
                if(!cache.ContainsKey(dutyId) && cache.Count>=64)cache.Remove(cache.MinBy(pair=>pair.Value.FetchedAt).Key);
                cache[dutyId]=document;
                if(ticket==generation)state=new(dutyId,false,document,null);
            }
            if(disk is not null)_=disk.WriteAsync(Family,dutyId.ToString(System.Globalization.CultureInfo.InvariantCulture),Version,document,document.FetchedAt);
        }
        catch(Exception error){lock(gate)if(!disposed && ticket==generation)state=state with{Loading=false,Error=error.Message};}
    }

    public void Dispose(){lock(gate){if(disposed)return;disposed=true;disposal.Cancel();cache.Clear();if(ownsHttp)http.Dispose();}}
}
