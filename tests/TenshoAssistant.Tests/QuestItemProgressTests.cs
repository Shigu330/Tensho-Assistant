using TenshoAssistant.Quests;

internal static class QuestItemProgressTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static readonly DateTimeOffset Seen=new(2026,10,30,17,0,0,TimeSpan.Zero);
    private static QuestEntry Q(uint id,string name,QuestProgress progress,byte? sequence=null,uint item=4551)=>new(id,name,"Genre",42,progress,sequence)
        {Rewards=new(1,0,[new(item,2,QuestRewardRule.Choice,"optional",0),new(item,5,QuestRewardRule.Fixed,"fixed",0,true)],[],version:"v")};
    private static QuestState State(ulong owner,IReadOnlyList<QuestEntry> entries,DateTimeOffset? seen,IEnumerable<QuestEntry>? indexed=null,
        bool loading=false,string? error=null,bool cached=false)
        =>new(owner,seen,entries,loading,error,QuestRewardIndex.Build(indexed ?? entries),FromCache:cached);
    private static IReadOnlyList<QuestEntry> Sample=>
        [Q(65537,"Bravo",QuestProgress.Active,3),Q(1,"Alpha",QuestProgress.Complete) with {IsRepeatable=true},Q(65538,"Charlie",QuestProgress.Unconfirmed),Q(2,"Other",QuestProgress.Active,1,12892)];
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Quest Item progress joins reward candidates to the same snapshot by full quest ID",()=>
        {
            var projection=QuestItemProgress.Project(State(10,Sample,Seen),4551,null,QuestSourceProgressFilter.All)!;
            Check(projection.ProgressConfirmed && projection.CharacterId==10 && projection.ObservedAt==Seen && !projection.FromCache,"owner and date of the observation kept");
            Check(projection.Rows.Select(row=>row.Source.QuestId).SequenceEqual(new uint[]{1,65537,65538}),"index order kept; 1 and 65537 stay distinct full identities");
            Check(projection.Rows[0].Progress==QuestProgress.Complete && projection.Rows[0].Sequence is null && projection.Rows[0].IsRepeatable,"completion confirmed without sequence");
            Check(projection.Rows[1].Progress==QuestProgress.Active && projection.Rows[1].Sequence==3,"active sequence carried");
            Check(projection.Rows[2].Progress==QuestProgress.Unconfirmed,"unconfirmed is never turned into not started");
            Check((projection.Matches,projection.Active,projection.Complete,projection.Unconfirmed)==(3,1,1,1),"counts over matching sources");
            Check(projection.Rows.All(row=>row.Source.Variants.Count==2),"variants are neither summed nor selected");
        }),
        ("Offline undated loading or failed quest snapshots cannot assert progress",()=>
        {
            foreach(var state in new[]{State(0,Sample,Seen),State(10,Sample,null),State(10,Sample,Seen,loading:true),State(10,Sample,Seen,error:"bad")})
            {
                var projection=QuestItemProgress.Project(state,4551,null,QuestSourceProgressFilter.All)!;
                Check(!projection.ProgressConfirmed && projection.CharacterId==0 && projection.ObservedAt is null && !projection.FromCache,"no scope claimed without a settled observation");
                Check(projection.Rows.Count==3 && projection.Rows.All(row=>row.Progress==QuestProgress.Unconfirmed && row.Sequence is null),"all sources stay listed as unconfirmed");
                Check((projection.Active,projection.Complete,projection.Unconfirmed)==(0,0,3),"no confirmed counts");
            }
        }),
        ("Quest Item progress keeps sources missing from the snapshot as unconfirmed",()=>
        {
            var observed=Sample.Where(entry=>entry.Id!=65537).ToArray();
            var projection=QuestItemProgress.Project(State(10,observed,Seen,Sample),4551,null,QuestSourceProgressFilter.All)!;
            var missing=projection.Rows.Single(row=>row.Source.QuestId==65537);
            Check(missing.Progress==QuestProgress.Unconfirmed && missing.Sequence is null && !missing.IsRepeatable,"absent line is not inferred");
            Check(projection.Matches==3 && projection.Unconfirmed==2,"absence kept in counts");
        }),
        ("Quest Item progress filter applies after counts and composes with the text query",()=>
        {
            var state=State(10,Sample,Seen);
            var all=QuestItemProgress.Project(state,4551,null,QuestSourceProgressFilter.All)!;
            foreach(var (filter,expected) in new[]{(QuestSourceProgressFilter.Active,65537u),(QuestSourceProgressFilter.Complete,1u),(QuestSourceProgressFilter.Unconfirmed,65538u)})
            {
                var filtered=QuestItemProgress.Project(state,4551,null,filter)!;
                Check(filtered.Rows.Single().Source.QuestId==expected,"filter keeps one confirmed state");
                Check((filtered.Matches,filtered.Active,filtered.Complete,filtered.Unconfirmed)==(all.Matches,all.Active,all.Complete,all.Unconfirmed),"counts independent of the filter");
                Check(ReferenceEquals(filtered.Rows.Single().Source,all.Rows.Single(row=>row.Source.QuestId==expected).Source),"same index source and variants");
            }
            var query=QuestItemProgress.Project(state,4551,"brav",QuestSourceProgressFilter.Complete)!;
            Check(query.Matches==1 && query.Active==1 && query.Rows.Count==0,"query narrows matches; progress filter can empty the rows");
            Check(QuestItemProgress.Project(state,3,null,QuestSourceProgressFilter.All)!.Matches==0,"Item without source");
        }),
        ("Quest Item progress follows service observations invalidation character change cache origin and clear",()=>
        {
            using var service=new QuestService();
            service.Publish(10,Sample,Seen);service.IndexWork.GetAwaiter().GetResult();
            var first=QuestItemProgress.Project(service.State,4551,null,QuestSourceProgressFilter.All)!;
            Check(first.ProgressConfirmed && first.CharacterId==10 && first.Active==1,"settled observation projected");
            service.Invalidate();
            Check(QuestItemProgress.Project(service.State,4551,null,QuestSourceProgressFilter.All) is null,"invalidated snapshot has no index to join");
            service.Publish(20,[Q(65537,"Bravo",QuestProgress.Complete) with {IsRepeatable=true}],Seen.AddHours(1),true);service.IndexWork.GetAwaiter().GetResult();
            var second=QuestItemProgress.Project(service.State,4551,null,QuestSourceProgressFilter.All)!;
            Check(second.CharacterId==20 && second.FromCache && second.ObservedAt==Seen.AddHours(1),"new owner, cached origin and date");
            Check(second.Rows.Single().Progress==QuestProgress.Complete && second.Rows.Single().IsRepeatable,"repeatable completion stays a confirmed completion only");
            service.Publish(0,Sample);service.IndexWork.GetAwaiter().GetResult();
            Check(!QuestItemProgress.Project(service.State,4551,null,QuestSourceProgressFilter.All)!.ProgressConfirmed,"offline publication cannot confirm");
            service.Clear();
            Check(QuestItemProgress.Project(service.State,4551,null,QuestSourceProgressFilter.All) is null,"cleared state projects nothing");
        }),
        ("Quest Item progress projection is readonly detached and rejects a missing state",()=>
        {
            var entries=Sample.ToList();var state=State(10,entries,Seen);
            var projection=QuestItemProgress.Project(state,4551,null,QuestSourceProgressFilter.All)!;
            Throws<NotSupportedException>(()=>((IList<QuestItemSourceProgress>)projection.Rows).Clear());
            Throws<NotSupportedException>(()=>((IList<QuestItemSourceProgress>)QuestItemProgress.Project(state,4551,null,QuestSourceProgressFilter.Active)!.Rows).Clear());
            Check(state.Entries.Single(entry=>entry.Id==65537).Progress==QuestProgress.Active,"snapshot not modified by the projection");
            Check(QuestItemProgress.Project(state with {RewardIndex=null},4551,null,QuestSourceProgressFilter.All) is null,"no index, no projection");
            Throws<ArgumentNullException>(()=>QuestItemProgress.Project(null!,4551,null,QuestSourceProgressFilter.All));
        }),
    };
}
