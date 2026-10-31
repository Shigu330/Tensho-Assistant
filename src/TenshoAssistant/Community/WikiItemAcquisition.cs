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
    private static readonly TimeSpan Limit=TimeSpan.FromSeconds(1);
    public static string Url(string title)=>"https://ffxiv.consolegameswiki.com/mediawiki/index.php?title="+Uri.EscapeDataString(title.Replace(' ','_'))+"&action=raw";
    public static WikiItemAcquisition Parse(string wikitext,uint itemId,string title,DateTimeOffset fetchedAt)
    {
        ArgumentNullException.ThrowIfNull(wikitext);
        if(wikitext.Length>GarlandClient.MaximumBytes)throw new InvalidDataException("Wiki response too large.");
        var lines=new List<WikiAcquisitionLine>();var inside=false;var heading="";
        // Infobox « online-store-id » (0.148: Bennu Horn, Lunar Whale Identification Key) = sold on the online store.
        if(Regex.IsMatch(wikitext,@"^\|\s*online-store-id\s*=\s*\d+",RegexOptions.Multiline,Limit))lines.Add(new("","Online Store"));
        foreach(var raw in wikitext.Split('\n'))
        {
            var line=raw.TrimEnd('\r').Trim();
            if(Regex.Match(line,@"^==\s*([^=].*?)\s*==$",RegexOptions.None,Limit) is { Success:true } section){inside=section.Groups[1].Value.Equals("Acquisition",StringComparison.OrdinalIgnoreCase);heading="";continue;}
            if(!inside)continue;
            if(Regex.Match(line,@"^===+\s*(.*?)\s*===+$",RegexOptions.None,Limit) is { Success:true } sub){heading=Clean(sub.Groups[1].Value);continue;}
            // Bullets, and plain lines such as « {{onlinestore|24}} » (0.148); tables, files and categories are skipped.
            if(line.Length==0 || line.StartsWith('{') && !line.StartsWith("{{",StringComparison.Ordinal) || line.StartsWith('|') || line.StartsWith('!') || line.StartsWith("[[File:",StringComparison.OrdinalIgnoreCase) || line.StartsWith("[[Category:",StringComparison.OrdinalIgnoreCase))continue;
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
        // Layout-only templates (0.150: « clear » appeared as a source line).
        ["clear"]=_=>"",["-"]=_=>"",["clr"]=_=>"",["br"]=_=>"",
    };
    // [[a|b]] → b, [https://… text] → text, {{template|x|y}} → known wording or « template x y », markup and HTML removed.
    private static string Clean(string text)
    {
        text=Regex.Replace(text,@"\[https?://[^\s\]]+\s+([^\]]+)\]","$1",RegexOptions.None,Limit);
        text=Regex.Replace(text,@"\[https?://[^\]]+\]","",RegexOptions.None,Limit);
        text=Regex.Replace(text,@"\[\[(?:[^\]|]*\|)?([^\]]*)\]\]","$1",RegexOptions.None,Limit);
        text=Regex.Replace(text,@"\{\{([^{}]*)\}\}",match=>
        {
            var parts=match.Groups[1].Value.Split('|').Select(part=>part.Trim()).ToArray();
            return Templates.TryGetValue(parts[0],out var wording) ? wording(parts)
                :string.Join(" ",parts.Select(part=>part.Contains('=') ? part[(part.IndexOf('=')+1)..]:part).Where(part=>part.Length>0));
        },RegexOptions.None,Limit);
        text=Regex.Replace(text,@"<[^>]+>"," ",RegexOptions.None,Limit).Replace("'''","").Replace("''","");
        text=Regex.Replace(text,@"^\s*(clear|-)\s*$","",RegexOptions.IgnoreCase,Limit);
        return Regex.Replace(text,@"\s+"," ",RegexOptions.None,Limit).Trim();
    }
}

public sealed record WikiItemAcquisitionState(WikiItemAcquisition? Page,bool Loading,string? Error);

/// <summary>Reads item pages on demand, one at a time (3 s apart), kept in DataCache « wiki-items » for the sources lifetime.</summary>
public sealed class WikiItemAcquisitionService:IDisposable
{
    private const string Family="wiki-items",Version="v3";
    private readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(20)};
    private readonly Func<uint,string?> englishName;private readonly Func<TimeSpan> lifetime;private readonly DataCache? disk;
    private readonly object gate=new();
    private readonly Dictionary<uint,WikiItemAcquisitionState> states=[];
    private readonly Queue<uint> pending=new();
    private readonly CancellationTokenSource disposal=new();
    private Task? worker;private bool disposed;private DateTimeOffset lastRequest;
    public WikiItemAcquisitionService(Func<uint,string?> englishName,Func<TimeSpan> lifetime,DataCache? disk){this.englishName=englishName;this.lifetime=lifetime;this.disk=disk;}
    public WikiItemAcquisitionState Get(uint itemId)
    {
        if(itemId==0)return new(null,false,null);
        lock(gate)
        {
            if(states.TryGetValue(itemId,out var known))return known;
            if(states.Count>=512)states.Remove(states.Keys.First());
            var loading=new WikiItemAcquisitionState(null,true,null);states[itemId]=loading;
            if(!disposed){pending.Enqueue(itemId);if(worker is null || worker.IsCompleted)worker=Task.Run(DrainAsync);}
            return loading;
        }
    }
    private async Task DrainAsync()
    {
        while(true)
        {
            uint id;
            lock(gate){if(disposed || pending.Count==0)return;id=pending.Dequeue();}
            WikiItemAcquisitionState result;
            try{result=new(await LoadAsync(id).ConfigureAwait(false),false,null);}
            catch(OperationCanceledException) when(disposal.IsCancellationRequested){return;}
            catch(Exception error){result=new(null,false,error.Message);}
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
        var wait=lastRequest+TimeSpan.FromSeconds(3)-DateTimeOffset.UtcNow;
        if(wait>TimeSpan.Zero)await Task.Delay(wait,token).ConfigureAwait(false);
        lastRequest=DateTimeOffset.UtcNow;
        var text=await BoundedHttp.ReadAsync(http,new Uri(WikiItemAcquisitionParser.Url(title)),GarlandClient.MaximumBytes,token).ConfigureAwait(false);
        var page=WikiItemAcquisitionParser.Parse(text,id,title,DateTimeOffset.UtcNow);
        if(disk is not null)_=disk.WriteAsync(Family,id.ToString(),Version,page,page.FetchedAt);
        return page;
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;pending.Clear();}disposal.Cancel();http.Dispose();}
}
