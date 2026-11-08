using TenshoAssistant.Collections;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// R-06 step 6 (0.290): shared look-ups for the draw code. Collection and quest lists hold thousands of rows; tooltips, icons and
// map labels asked for one row by scanning the whole list on every frame. The indexes are rebuilt only when the state object is
// replaced (a new read), never per frame.
internal sealed partial class MainWindow
{
    private object? collectionIndexState;
    private Dictionary<(CollectionKind Kind,uint Id),CollectionEntry> collectionIndex=[];
    private Dictionary<uint,List<CollectionEntry>> achievementsByReward=[];
    // X-76 (0.955): achievements by English name (wiki lines), the first of a name kept as FirstOrDefault did.
    private Dictionary<string,CollectionEntry> achievementsByEnglishName=new(StringComparer.OrdinalIgnoreCase);
    private CollectionEntry[] triadCardEntries=[];

    private void RefreshCollectionIndex()
    {
        var state=collections.State;
        if(ReferenceEquals(state,collectionIndexState))return;
        collectionIndexState=state;
        var index=new Dictionary<(CollectionKind,uint),CollectionEntry>();
        var rewards=new Dictionary<uint,List<CollectionEntry>>();
        var named=new Dictionary<string,CollectionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach(var row in state.Entries)
        {
            index.TryAdd((row.Kind,row.Id),row);
            if(row.Kind==CollectionKind.Achievement && row.Achievement is { RewardItemId:not 0 } metadata)
                (rewards.TryGetValue(metadata.RewardItemId,out var list) ? list:rewards[metadata.RewardItemId]=[]).Add(row);
            if(row.Kind==CollectionKind.Achievement && row.Achievement is { EnglishName.Length:>0 } english)named.TryAdd(english.EnglishName,row);
        }
        collectionIndex=index;achievementsByReward=rewards;achievementsByEnglishName=named;
        triadCardEntries=state.Entries.Where(row=>row.Kind==CollectionKind.TripleTriad).ToArray();
        triadById=triadCardEntries.GroupBy(row=>row.Id).ToDictionary(group=>group.Key,group=>group.First());
    }
    private CollectionEntry? CollectionEntryOf(CollectionKind kind,uint id){RefreshCollectionIndex();return collectionIndex.GetValueOrDefault((kind,id));}
    private IReadOnlyList<CollectionEntry> AchievementsRewardingItem(uint itemId){RefreshCollectionIndex();return achievementsByReward.GetValueOrDefault(itemId) ?? [];}
    private CollectionEntry? AchievementByEnglishName(string? name){RefreshCollectionIndex();return name is { Length:>0 } ? achievementsByEnglishName.GetValueOrDefault(name):null;}
    private Dictionary<uint,CollectionEntry> triadById=[];
    private Dictionary<uint,CollectionEntry> TriadEntriesById(){RefreshCollectionIndex();return triadById;}

    private object? questIndexState;
    private Dictionary<uint,QuestEntry> questById=[];
    private Dictionary<uint,QuestEntry> activeQuestByKey=[];

    private void RefreshQuestIndex()
    {
        var entries=quests.State.Entries;
        if(ReferenceEquals(entries,questIndexState))return;
        questIndexState=entries;
        var byId=new Dictionary<uint,QuestEntry>();var active=new Dictionary<uint,QuestEntry>();
        foreach(var entry in entries)
        {
            byId.TryAdd(entry.Id,entry);
            if(entry.Progress==QuestProgress.Active)active.TryAdd(entry.Id & 0xFFFF,entry);
        }
        questById=byId;activeQuestByKey=active;
    }
    private QuestEntry? QuestOf(uint id){RefreshQuestIndex();return questById.GetValueOrDefault(id);}
    /// <summary>Active quest by the low 16 bits of its id (map markers give that key).</summary>
    private QuestEntry? ActiveQuestByKey(uint key){RefreshQuestIndex();return activeQuestByKey.GetValueOrDefault(key);}
}
