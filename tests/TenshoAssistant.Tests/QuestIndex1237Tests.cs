using TenshoAssistant.Quests;

// 0.1237 (X-01 C5, rest): the quest journal looked up by id in an index the service builds off the frame with the reward index (QuestState.ById),
// not a dictionary built in the frame by each page when the journal changes (Events, shop requirements). Behaviour tests on the service.
internal static class QuestIndex1237Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static QuestEntry Entry(uint id,string name,QuestProgress progress)=>new(id,name,"",0,progress,null);

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Quest index 1237: built with the reward index, off the caller's thread; null before and after a logout",() =>
        {
            var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var builtOn=0;
            var service=new QuestService((entries,token)=>Task.Run(async()=>{await release.Task;builtOn=Environment.CurrentManagedThreadId;return QuestRewardIndex.Build(entries,token);},token));
            service.Publish(7,[Entry(10,"B",QuestProgress.Complete),Entry(11,"A",QuestProgress.Active)]);
            Check(service.State.ById is null && service.State.RewardIndexLoading,"not built yet: null, not a frame dictionary");
            release.SetResult();
            service.IndexWork.Wait(5000);
            var byId=service.State.ById;
            Check(byId is not null && byId[10].Progress==QuestProgress.Complete && byId[11].Name=="A" && byId.Count==2,"index of the published entries");
            Check(builtOn!=Environment.CurrentManagedThreadId,"built on another thread");
            service.Invalidate();
            Check(service.State.ById is null,"after a logout: no index (not read)");
            service.Dispose();
        }),
        ("Quest index 1237: a repeated id keeps the first entry, never throws",() =>
        {
            var index=QuestState.Index([Entry(5,"first",QuestProgress.Complete),Entry(5,"second",QuestProgress.Active)]);
            Check(index.Count==1 && index[5].Name=="first","first kept");
        }),
        ("Quest index 1237: the pages use the service's index, no dictionary of their own",() =>
        {
            var events=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Events.cs"));
            var shops=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ShopRequirements.cs"));
            Check(events.Contains("var byId=quests.State.ById;") && !events.Contains("journal.ToDictionary(entry=>entry.Id)"),"Events");
            Check(shops.Contains("var byId=quests.State.ById;") && !shops.Contains("shopQuests"),"shop requirements");
        }),
    ];
}
