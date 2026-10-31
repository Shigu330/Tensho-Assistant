using TenshoAssistant.Quests;
namespace TenshoAssistant.Caching;

public static class CacheSnapshots
{
    public static bool Valid(QuestCacheData snapshot,ulong owner)=>owner!=0 && snapshot.Owner==owner && snapshot.Entries is {Count:>0 and <=50000}
        && snapshot.Entries.All(row=>row is not null && row.Id>0 && !string.IsNullOrWhiteSpace(row.Name) && row.Name.Length<=1024
            && row.Category is not null && row.Zone is not null && row.PreviousQuestIds is {Count:<=64} && Enum.IsDefined(row.Progress))
        && snapshot.Entries.Select(row=>row.Id).Distinct().Count()==snapshot.Entries.Count;
    public static bool Valid(CollectionCacheData snapshot,ulong owner)=>owner!=0 && snapshot.Owner==owner && snapshot.Entries is {Count:>0 and <=30000}
        && snapshot.Entries.All(row=>row is not null && row.Id>0 && !string.IsNullOrWhiteSpace(row.Name) && row.Name.Length<=1024
            && row.Description is not null && row.Location is not null && Enum.IsDefined(row.Kind) && !(row.ConfirmedUnlocked && row.NegativeConfirmed))
        && snapshot.Entries.Select(row=>(row.Kind,row.Id)).Distinct().Count()==snapshot.Entries.Count
        && (snapshot.Issues is null || snapshot.Issues.Count<=64 && snapshot.Issues.All(issue=>issue is not null && Enum.IsDefined(issue.Kind) && issue.Reason is {Length:<=4096}));
}
