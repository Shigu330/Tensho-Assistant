using System.Net.Http;
using System.Text.RegularExpressions;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Community;

/// <summary>
/// « Acquisition » section of an item page on the FFXIV wiki (raw wikitext, English), used as the last source when neither the
/// game data nor Garland Tools know one (checked 2026-10-31: « Rocket Punch Identification Key » → ===PvP=== + a series reward).
/// Community data, shown as such; template names are kept readable rather than interpreted.
/// </summary>
public sealed record WikiAcquisitionLine(string Heading,string Text);
public sealed record WikiItemAcquisition(uint ItemId,string Title,IReadOnlyList<WikiAcquisitionLine> Lines,DateTimeOffset FetchedAt);

public static class WikiItemAcquisitionParser
{
    // X-886 (0.1908): the linear engine (as GardenJournal since X-428): its time is bounded by the input's length (itself bounded by
    // GarlandClient.MaximumBytes), never by going back over it, so it needs no timeout. The 1 s timeout ran out once on a page of a few lines,
    // a thread not scheduled under the whole suite's load: on a busy PC a page could fail to read for the same reason. Same matches and groups.
    private const RegexOptions Linear=RegexOptions.CultureInvariant|RegexOptions.NonBacktracking;
    private static readonly TimeSpan Limit=Regex.InfiniteMatchTimeout;
    private static readonly Regex Generator=new(@"^[*#:\s]*\{\{\s*(vendor list|contained in|quest list|deployed from|desynthesis list|retrieval list|achievement list|item list|recipe list|gathering list|drop list)\s*(\||\}\})",
        RegexOptions.IgnoreCase|Linear,Limit);
    private static readonly Regex OnlineStore=new(@"^\|\s*online-store-id\s*=\s*\d+",RegexOptions.Multiline|Linear,Limit);
    private static readonly Regex Section=new(@"^==\s*([^=].*?)\s*==$",Linear,Limit);
    private static readonly Regex SubSection=new(@"^===+\s*(.*?)\s*===+$",Linear,Limit);
    private static readonly Regex NamedLink=new(@"\[https?://[^\s\]]+\s+([^\]]+)\]",Linear,Limit);
    private static readonly Regex BareLink=new(@"\[https?://[^\]]+\]",Linear,Limit);
    private static readonly Regex WikiLink=new(@"\[\[(?:[^\]|]*\|)?([^\]]*)\]\]",Linear,Limit);
    private static readonly Regex Template=new(@"\{\{([^{}]*)\}\}",Linear,Limit);
    private static readonly Regex Markup=new(@"<[^>]+>",Linear,Limit);
    private static readonly Regex Layout=new(@"^\s*(clear|-)\s*$",RegexOptions.IgnoreCase|Linear,Limit);
    private static readonly Regex Spaces=new(@"\s+",Linear,Limit);
    public static string Url(string title)=>"https://ffxiv.consolegameswiki.com/mediawiki/index.php?title="+Uri.EscapeDataString(title.Replace(' ','_'))+"&action=raw";
    public static WikiItemAcquisition Parse(string wikitext,uint itemId,string title,DateTimeOffset fetchedAt)
    {
        ArgumentNullException.ThrowIfNull(wikitext);
        if(wikitext.Length>GarlandClient.MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        var lines=new List<WikiAcquisitionLine>();var inside=false;var heading="";
        // Infobox « online-store-id » (0.148: Bennu Horn, Lunar Whale Identification Key) = sold on the online store.
        if(OnlineStore.IsMatch(wikitext))lines.Add(new("","Online Store"));
        foreach(var raw in wikitext.Split('\n'))
        {
            var line=raw.TrimEnd('\r').Trim();
            if(Section.Match(line) is { Success:true } section){inside=section.Groups[1].Value.Equals("Acquisition",StringComparison.OrdinalIgnoreCase);heading="";continue;}
            if(!inside)continue;
            if(SubSection.Match(line) is { Success:true } sub){heading=Clean(sub.Groups[1].Value);continue;}
            // Bullets, and plain lines such as « {{onlinestore|24}} » (0.148); tables, files and categories are skipped.
            if(line.Length==0 || line.StartsWith('{') && !line.StartsWith("{{",StringComparison.Ordinal) || line.StartsWith('|') || line.StartsWith('!') || line.StartsWith("[[File:",StringComparison.OrdinalIgnoreCase) || line.StartsWith("[[Category:",StringComparison.OrdinalIgnoreCase))continue;
            // List generators filled by the wiki at display time (capture 2026-10-31, Alsvid Whistle: « vendor list y », « contained in »,
            // « {{quest list||…{{#titleparts:{{PAGENAME}}}}…}} », « deployed from Y »): their source text holds no acquisition, skip them.
            if(Generator.IsMatch(line) || line.Contains("{{#",StringComparison.Ordinal) || line.Contains("PAGENAME",StringComparison.Ordinal))continue;
            var text=Clean(line.TrimStart('*','#',':',' '));
            if(text.Length>0 && lines.Count<12 && !lines.Any(known=>known.Text==text))lines.Add(new(heading,text.Length>200 ? text[..200]+"…":text));
        }
        return new(itemId,title,lines.AsReadOnly(),fetchedAt.ToUniversalTime());
    }
    // Known templates in plain words; others « template x y ».
    private static readonly IReadOnlyDictionary<string,Func<string[],string>> Templates=new Dictionary<string,Func<string[],string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["onlinestore"]=parts=>parts.Length>1 ? $"Online Store ({parts[1]} USD)":"Online Store",
        ["seriesmalmstonesreward"]=parts=>parts.Length>1 ? $"PvP series reward (rank {parts[1]})":"PvP series reward",
        // Achievement tables (0.179, capture Aeturna: « achievement table header », « achievement table row Vintage Vogue »): the row names
        // the achievement, the header is layout only; the section heading (Achievement) already says what it is.
        ["achievement table header"]=_=>"",["achievement table row"]=parts=>parts.Length>1 ? parts[1]:"",
        // Layout-only templates (0.150: « clear » appeared as a source line).
        ["clear"]=_=>"",["-"]=_=>"",["clr"]=_=>"",["br"]=_=>"",
    };
    // [[a|b]] → b, [https://… text] → text, {{template|x|y}} → known wording or « template x y », markup and HTML removed.
    private static string Clean(string text)
    {
        text=NamedLink.Replace(text,"$1");
        text=BareLink.Replace(text,"");
        text=WikiLink.Replace(text,"$1");
        text=Template.Replace(text,match=>
        {
            var parts=match.Groups[1].Value.Split('|').Select(part=>part.Trim()).ToArray();
            return Templates.TryGetValue(parts[0],out var wording) ? wording(parts)
                :string.Join(" ",parts.Select(part=>part.Contains('=') ? part[(part.IndexOf('=')+1)..]:part).Where(part=>part.Length>0));
        });
        text=Markup.Replace(text," ").Replace("'''","").Replace("''","");
        text=Layout.Replace(text,"");
        return Spaces.Replace(text," ").Trim();
    }
}

/// <summary>X-619 M3 (review of 0.1557): FailedAt = when the read failed (null: not failed); the item is asked again <see cref="WikiItemAcquisitionService.RetryAfter"/> later.</summary>
public sealed record WikiItemAcquisitionState(WikiItemAcquisition? Page,bool Loading,string? Error,DateTimeOffset? FailedAt=null);

/// <summary>Reads item pages on demand, one at a time (3 s apart), kept in DataCache « wiki-items » for the sources lifetime.</summary>
public sealed class WikiItemAcquisitionService:IDisposable
{
    private const string Family="wiki-items",Version="v5"; // v4 (0.177): list generators dropped; v5 (0.179): achievement tables.
    private readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(20)};
    private readonly Func<uint,string?> englishName;private readonly Func<TimeSpan> lifetime;private readonly DataCache? disk;
    private readonly object gate=new();
    private readonly Dictionary<uint,WikiItemAcquisitionState> states=[];
    private readonly Queue<uint> pending=new();
    private readonly CancellationTokenSource disposal=new();
    // X-589 (external audit A10 of 18/11): « draining » is changed under the same lock as the queue. Before, the drainer left the lock with
    // the queue empty just before its task was completed; an item asked in between saw a task still running, woke nobody, and stayed
    // « loading » (asked again, the state kept was returned).
    private bool disposed,draining;private DateTimeOffset lastRequest;
    public WikiItemAcquisitionService(Func<uint,string?> englishName,Func<TimeSpan> lifetime,DataCache? disk){this.englishName=englishName;this.lifetime=lifetime;this.disk=disk;}
    /// <summary>X-589 (A10): a simulated network for the managed tests (the service's own client otherwise).</summary>
    internal WikiItemAcquisitionService(Func<uint,string?> englishName,Func<TimeSpan> lifetime,DataCache? disk,HttpMessageHandler handler):this(englishName,lifetime,disk)
    {http.Dispose();http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(20)};}
    /// <summary>X-589 (A10): the pause between two requests (3 s; shorter in the managed tests).</summary>
    internal TimeSpan Gap { get; set; }=TimeSpan.FromSeconds(3);
    /// <summary>X-589 (A10): called by the drainer once it found the queue empty, just before its task ends (the managed tests ask there).</summary>
    internal Action? Idle { get; set; }
    /// <summary>X-619 M3 (review of 0.1557): how long a failed item waits before it is asked again (network cut, the 20 s delay): as
    /// Teamcraft's failedAt and GatherBuddy's retryAfter. Before, the error was kept for the whole session.</summary>
    internal TimeSpan RetryAfter { get; set; }=TimeSpan.FromMinutes(5);
    /// <summary>X-619 M3: the clock (moved forward in the managed tests).</summary>
    internal Func<DateTimeOffset> Clock { get; set; }=()=>DateTimeOffset.UtcNow;
    // Settings › Advanced (0.206): when off, nothing is requested and the source is simply absent.
    public Func<bool>? Enabled { get; set; }
    /// <summary>What is already known for the item, without requesting anything (null = nothing yet).</summary>
    public WikiItemAcquisitionState? Peek(uint itemId)
    {
        if(itemId==0 || Enabled?.Invoke()==false)return null;
        lock(gate)return states.TryGetValue(itemId,out var known) ? known:null;
    }
    public WikiItemAcquisitionState Get(uint itemId)
    {
        if(itemId==0 || Enabled?.Invoke()==false)return new(null,false,null);
        lock(gate)
        {
            if(states.TryGetValue(itemId,out var known))
            {
                if(known.FailedAt is not { } failed || Clock()-failed<RetryAfter)return known;
                states.Remove(itemId);
            }
            if(states.Count>=512)states.Remove(states.Keys.First());
            var loading=new WikiItemAcquisitionState(null,true,null);states[itemId]=loading;
            if(!disposed){pending.Enqueue(itemId);if(!draining){draining=true;_=Task.Run(DrainAsync);}}
            return loading;
        }
    }
    private async Task DrainAsync()
    {
        while(true)
        {
            uint id=0;var idle=false;
            lock(gate){if(disposed || pending.Count==0){draining=false;idle=true;}else id=pending.Dequeue();}
            if(idle){Idle?.Invoke();return;}
            WikiItemAcquisitionState result;
            try{result=new(await LoadAsync(id).ConfigureAwait(false),false,null);}
            catch(OperationCanceledException) when(disposal.IsCancellationRequested){return;}
            // 0.326: the wiki was turned off while this item waited: forget it, so it is asked again once the switch is back on.
            catch(SourceDisabledException){lock(gate){if(!disposed)states.Remove(id);}continue;}
            catch(Exception error){result=new(null,false,error.Message,Clock());}
            lock(gate){if(!disposed)states[id]=result;}
        }
    }
    private async Task<WikiItemAcquisition> LoadAsync(uint id)
    {
        var token=disposal.Token;
        if(disk is not null && await disk.ReadAsync<WikiItemAcquisition>(Family,id.ToString(),Version,token).ConfigureAwait(false) is { } saved
           && CacheAge.Fresh(saved.RecordedAt,DateTimeOffset.UtcNow,lifetime()) && saved.Value.Lines is not null)return saved.Value;
        var title=englishName(id);
        if(string.IsNullOrWhiteSpace(title) || title.Length>256)throw new InvalidDataException("English item name unavailable.");
        var wait=lastRequest+Gap-DateTimeOffset.UtcNow;
        if(wait>TimeSpan.Zero)await Task.Delay(wait,token).ConfigureAwait(false);
        if(Enabled?.Invoke()==false)throw new SourceDisabledException("FFXIV Console Games Wiki");
        lastRequest=DateTimeOffset.UtcNow;
        var text=await BoundedHttp.ReadAsync(http,new Uri(WikiItemAcquisitionParser.Url(title)),GarlandClient.MaximumBytes,token).ConfigureAwait(false);
        var page=WikiItemAcquisitionParser.Parse(text,id,title,DateTimeOffset.UtcNow);
        if(disk is not null)_=disk.WriteAsync(Family,id.ToString(),Version,page,page.FetchedAt);
        return page;
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;pending.Clear();}disposal.Cancel();http.Dispose();}
}
