using System.Collections.Frozen;

namespace TenshoAssistant.Quests;

public sealed record QuestItemRewardSource(uint QuestId,string Name,string Category,uint Icon,IReadOnlyList<QuestRewardItem> Variants);
public sealed record QuestRewardCoverage(int TotalQuests,int CoveredQuests,int UnresolvedReferences,int UnknownQuantityLines,int UnversionedQuests);

/// <summary>Static reward candidates only. Contains no player progress and performs no native reads.</summary>
public sealed class QuestRewardIndex
{
    private readonly FrozenDictionary<uint,IReadOnlyList<QuestItemRewardSource>> byItem;
    public QuestRewardCoverage Coverage {get;}
    public IReadOnlyList<string> DataVersions {get;}
    public int ItemCount=>byItem.Count;
    public int SourceCount=>byItem.Values.Sum(sources=>sources.Count);
    private QuestRewardIndex(Dictionary<uint,List<QuestItemRewardSource>> source,QuestRewardCoverage coverage,IEnumerable<string> versions)
    {
        byItem=source.ToFrozenDictionary(pair=>pair.Key,pair=>(IReadOnlyList<QuestItemRewardSource>)Array.AsReadOnly(pair.Value
            .OrderBy(row=>row.Name,StringComparer.OrdinalIgnoreCase).ThenBy(row=>row.QuestId).ToArray()));
        Coverage=coverage;DataVersions=Array.AsReadOnly(versions.Distinct(StringComparer.Ordinal).OrderBy(value=>value,StringComparer.Ordinal).ToArray());
    }
    public IReadOnlyList<QuestItemRewardSource> ForItem(uint item)=>byItem.GetValueOrDefault(item) ?? [];
    public IReadOnlyList<QuestItemRewardSource> Search(uint item,string? query)
    {
        var sources=ForItem(item);
        if(string.IsNullOrWhiteSpace(query))return sources;
        var tokens=query.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        return Array.AsReadOnly(sources.Where(source=>tokens.All(token=>
            source.Name.Contains(token,StringComparison.OrdinalIgnoreCase)
            || source.Category.Contains(token,StringComparison.OrdinalIgnoreCase)
            || source.QuestId.ToString(System.Globalization.CultureInfo.InvariantCulture)==token)).ToArray());
    }
    public static QuestRewardIndex Build(IEnumerable<QuestEntry> quests,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(quests);cancellation.ThrowIfCancellationRequested();
        var map=new Dictionary<uint,List<QuestItemRewardSource>>();var seen=new HashSet<uint>();var versions=new HashSet<string>(StringComparer.Ordinal);
        var total=0;var covered=0;var unresolved=0;var unknown=0;var unversioned=0;var scanned=0;
        foreach(var quest in quests)
        {
            cancellation.ThrowIfCancellationRequested();
            if(++total>50000)throw new InvalidDataException("Quest source catalogue exceeds bounded index size.");
            if(quest.Id==0 || string.IsNullOrWhiteSpace(quest.Name) || !seen.Add(quest.Id))throw new InvalidDataException("Invalid or duplicate quest source identity.");
            if(quest.Rewards is not { } rewards || quest.RewardsError is not null)continue;
            covered++;unresolved=checked(unresolved+rewards.Unresolved.Count);
            if(string.IsNullOrWhiteSpace(rewards.DataVersion))unversioned++;else versions.Add(rewards.DataVersion);
            var variants=new Dictionary<uint,List<QuestRewardItem>>();
            foreach(var item in rewards.Items)
            {
                cancellation.ThrowIfCancellationRequested();
                if(++scanned>200000)throw new InvalidDataException("Quest reward index exceeds bounded association count.");
                if(item.Quantity is null)unknown++;
                if(!variants.TryGetValue(item.ItemId,out var group))variants[item.ItemId]=group=[];
                group.Add(item);
            }
            foreach(var (item,lines) in variants)
            {
                if(!map.TryGetValue(item,out var sources))map[item]=sources=[];
                sources.Add(new(quest.Id,quest.Name,quest.Category,quest.Icon,Array.AsReadOnly(lines.ToArray())));
            }
        }
        cancellation.ThrowIfCancellationRequested();
        var result=new QuestRewardIndex(map,new(total,covered,unresolved,unknown,unversioned),versions);
        cancellation.ThrowIfCancellationRequested();return result;
    }
}
