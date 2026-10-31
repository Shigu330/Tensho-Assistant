using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Items;

namespace TenshoAssistant.Fashion;

public sealed record FashionHint(uint Id,string Name);
public sealed class FashionHintCatalog
{
    public IReadOnlyList<FashionHint> Hints { get; }
    public FashionHintCatalog(IEnumerable<FashionHint> hints)
    {
        var copied=hints.Where(hint=>hint.Id!=0 && !string.IsNullOrWhiteSpace(hint.Name)).ToArray();
        if(copied.Select(hint=>hint.Id).Distinct().Count()!=copied.Length) throw new ArgumentException("Duplicate native fashion hint.");
        Hints=Array.AsReadOnly(copied.OrderBy(hint=>hint.Name,StringComparer.CurrentCultureIgnoreCase).ThenBy(hint=>hint.Id).ToArray());
    }
}
public sealed record FashionHintState(FashionHintCatalog? Catalog,string? Error);
public sealed class FashionHintService
{
    private FashionHintState state=new(null,null);
    public FashionHintState State=>Volatile.Read(ref state);
    internal void Publish(FashionHintCatalog catalog)=>Interlocked.Exchange(ref state,new(catalog,null));
    internal void Fail(Exception error)=>Interlocked.Exchange(ref state,new(null,error.Message));
    internal void Clear()=>Interlocked.Exchange(ref state,new(null,null));
}
public sealed record FashionMatch(uint ItemId,long Reports);
public sealed record FashionDye(uint StainId,int Reports,double Share);
public sealed record FashionCandidate(FashionMatch Match,ItemDefinition? Item);
public enum FashionDateStatus { Missing, Parsed, Invalid, Future }
public sealed class FashionDataset
{
    public const int MaximumBytes=1024*1024;
    private readonly FrozenDictionary<uint,IReadOnlyList<FashionMatch>> byHint;
    public IReadOnlyList<uint> HintIds { get; }
    public DateTimeOffset FetchedAt { get; }
    public DateTimeOffset? ProviderUpdatedAt { get; }
    public FashionDateStatus DateStatus { get; }
    private FashionDataset(Dictionary<uint,IReadOnlyList<FashionMatch>> hints,DateTimeOffset fetchedAt,DateTimeOffset? providerDate,FashionDateStatus dateStatus)
    {
        byHint=hints.ToFrozenDictionary(); HintIds=Array.AsReadOnly(hints.Keys.Order().ToArray());
        FetchedAt=fetchedAt.ToUniversalTime(); ProviderUpdatedAt=providerDate; DateStatus=dateStatus;
    }
    public IReadOnlyList<FashionMatch> ForHint(uint hintId)=>byHint.GetValueOrDefault(hintId) ?? Array.Empty<FashionMatch>();
    // Dyes of the week (0.215): « WeeklyDyes » → week number → slot name (Weapon, Head, Body, Hands, Legs, Feet) → stain id, count, share.
    // Read leniently: a malformed dye section leaves dyes unknown without rejecting the gear data.
    public IReadOnlyDictionary<int,IReadOnlyDictionary<string,IReadOnlyList<FashionDye>>> WeeklyDyes { get; private set; }=new Dictionary<int,IReadOnlyDictionary<string,IReadOnlyList<FashionDye>>>();
    public IReadOnlyList<FashionDye> DyesFor(int week,string slot)=>WeeklyDyes.GetValueOrDefault(week)?.GetValueOrDefault(slot) ?? [];
    internal static IReadOnlyDictionary<int,IReadOnlyDictionary<string,IReadOnlyList<FashionDye>>> ParseDyes(JsonElement root)
    {
        var weeks=new Dictionary<int,IReadOnlyDictionary<string,IReadOnlyList<FashionDye>>>();
        try
        {
            if(!root.TryGetProperty("WeeklyDyes",out var section) || section.ValueKind!=JsonValueKind.Object)return weeks;
            foreach(var week in section.EnumerateObject().Take(256))
            {
                if(!int.TryParse(week.Name,NumberStyles.None,CultureInfo.InvariantCulture,out var number) || week.Value.ValueKind!=JsonValueKind.Array)continue;
                var slots=new Dictionary<string,IReadOnlyList<FashionDye>>(StringComparer.OrdinalIgnoreCase);
                foreach(var slot in week.Value.EnumerateArray().Take(16))
                {
                    if(!slot.TryGetProperty("Name",out var name) || name.ValueKind!=JsonValueKind.String || !slot.TryGetProperty("Dyes",out var dyes) || dyes.ValueKind!=JsonValueKind.Object)continue;
                    var list=new List<FashionDye>();
                    foreach(var dye in dyes.EnumerateObject().Take(128))
                        if(uint.TryParse(dye.Name,NumberStyles.None,CultureInfo.InvariantCulture,out var stain) && dye.Value.ValueKind==JsonValueKind.Object
                           && dye.Value.TryGetProperty("Count",out var count) && count.TryGetInt32(out var reports)
                           && dye.Value.TryGetProperty("Pct",out var pct) && pct.TryGetDouble(out var share))
                            list.Add(new(stain,reports,share));
                    slots[name.GetString() ?? ""]=list.OrderByDescending(entry=>entry.Share).ThenByDescending(entry=>entry.Reports).ToArray();
                }
                weeks[number]=slots;
            }
        }
        catch(InvalidOperationException){}
        return weeks;
    }
    public static FashionDataset Parse(string json,string? sourceDateJson,DateTimeOffset fetchedAt,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(json); cancellation.ThrowIfCancellationRequested();
        if(json.Length>MaximumBytes || Encoding.UTF8.GetByteCount(json)>MaximumBytes) throw new InvalidDataException("Fashion data too large.");
        using var document=JsonDocument.Parse(json,new JsonDocumentOptions { MaxDepth=32 });
        if(document.RootElement.ValueKind!=JsonValueKind.Object) throw new InvalidDataException("Invalid fashion root.");
        var sections=document.RootElement.EnumerateObject().Where(property=>property.NameEquals("Categories")).ToArray();
        if(sections.Length!=1 || sections[0].Value.ValueKind!=JsonValueKind.Object) throw new InvalidDataException("Missing or ambiguous fashion categories.");
        var result=new Dictionary<uint,IReadOnlyList<FashionMatch>>(); var total=0;
        foreach(var category in sections[0].Value.EnumerateObject())
        {
            cancellation.ThrowIfCancellationRequested();
            if(result.Count>=1024 || !uint.TryParse(category.Name,NumberStyles.None,CultureInfo.InvariantCulture,out var hintId) || hintId==0
                || result.ContainsKey(hintId) || category.Value.ValueKind!=JsonValueKind.Object) throw new InvalidDataException("Invalid fashion category.");
            var matches=new Dictionary<uint,FashionMatch>();
            foreach(var item in category.Value.EnumerateObject())
            {
                cancellation.ThrowIfCancellationRequested();
                if(++total>32768 || matches.Count>=4096 || !uint.TryParse(item.Name,NumberStyles.None,CultureInfo.InvariantCulture,out var itemId) || itemId==0
                    || item.Value.ValueKind!=JsonValueKind.Number || !item.Value.TryGetInt64(out var count) || count<=0
                    || !matches.TryAdd(itemId,new(itemId,count))) throw new InvalidDataException("Invalid fashion item observation.");
            }
            result.Add(hintId,Array.AsReadOnly(matches.Values.OrderByDescending(match=>match.Reports).ThenBy(match=>match.ItemId).ToArray()));
        }
        DateTimeOffset? sourceDate=null; var status=FashionDateStatus.Missing;
        if(sourceDateJson is not null)
        {
            status=FashionDateStatus.Invalid;
            try
            {
                if(sourceDateJson.Length>4096) throw new JsonException();
                using var date=JsonDocument.Parse(sourceDateJson);
                if(date.RootElement.ValueKind==JsonValueKind.String && DateTimeOffset.TryParseExact(date.RootElement.GetString(),
                    new[]{"r","O","yyyy-MM-dd'T'HH:mm:ss'Z'"},CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out var parsed)
                    && parsed>=new DateTimeOffset(2018,1,1,0,0,0,TimeSpan.Zero))
                { sourceDate=parsed; status=parsed>fetchedAt.AddMinutes(5) ? FashionDateStatus.Future:FashionDateStatus.Parsed; }
            }
            catch(JsonException) { }
        }
        var dataset=new FashionDataset(result,fetchedAt,sourceDate,status);
        dataset.WeeklyDyes=ParseDyes(document.RootElement);
        return dataset;
    }
    public IReadOnlyList<FashionCandidate> Filter(uint hintId,EquipmentSlots slot,ItemCatalog? items,string query)
    {
        if(slot!=EquipmentSlots.None && (slot&~EquipmentSlots.Wearable)!=0) throw new ArgumentOutOfRangeException(nameof(slot));
        query=query.Trim();
        return Array.AsReadOnly(ForHint(hintId).Select(match=>new FashionCandidate(match,items?.Find(match.ItemId)))
            .Where(candidate=>(slot==EquipmentSlots.None || candidate.Item is { } item && (item.Equipment&slot)!=0)
                && (query.Length==0 || candidate.Match.ItemId.ToString(CultureInfo.InvariantCulture)==query
                    || CultureInfo.CurrentCulture.CompareInfo.IndexOf(candidate.Item?.Name ?? "",query,CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace)>=0)).ToArray());
    }
}
