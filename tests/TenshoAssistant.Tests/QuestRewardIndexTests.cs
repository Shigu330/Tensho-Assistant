using TenshoAssistant.Quests;

internal static class QuestRewardIndexTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static QuestEntry Q(uint id,uint item,string name="Quête")=>new(id,name,"Genre",42,QuestProgress.Active,3)
        {Rewards=new(1,0,[new(item,2,QuestRewardRule.Choice,"optional",0)],[],version:"v")};
    private static TaskCompletionSource<QuestRewardIndex> Pending()=>new(TaskCreationOptions.RunContinuationsAsynchronously);
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Quest Item index keeps all conditional variants and full quest identities without summing or progress",()=>
        {
            var rewards=new QuestRewardSnapshot(7,0,[new(4551,1,QuestRewardRule.Fixed,"fixed",0),new(4551,5,QuestRewardRule.Choice,"optional",0,true),new(4551,0,QuestRewardRule.TribeRankVariant,"rank",0,Variant:0)],[]);
            var index=QuestRewardIndex.Build([Q(65537,4551,"B") with {Rewards=rewards},Q(1,4551,"A"),Q(65538,12892)]);
            var sources=index.ForItem(4551);
            Check(sources.Select(source=>source.QuestId).SequenceEqual(new uint[]{1,65537}),"full row identities and stable quest order");
            Check(sources[1].Variants.Select(line=>line.Quantity).SequenceEqual(new uint?[]{1,5,0}) && sources[1].Variants[1].HighQuality==true,"fixed choice rank and HQ retained without selecting a variant");
            Check(index.ForItem(12892).Single().QuestId==65538 && index.ForItem(3).Count==0 && index.SourceCount==3,"only typed reward Items indexed, one source per quest and Item");
        }),
        ("Quest Item index coverage keeps missing failed unresolved and unversioned reward data explicit",()=>
        {
            var first=Q(65537,4551) with {Rewards=new(1,0,[new(4551,null,QuestRewardRule.Fixed,"fixed",0)],[new("currency",0,0,"missing",50)],version:"v")};
            var index=QuestRewardIndex.Build([first,Q(1,1) with {Rewards=new(1,0,[],[])},Q(2,1) with {Rewards=null},Q(3,1) with {RewardsError="bad"}]);
            Check(index.Coverage==new QuestRewardCoverage(4,2,1,1,1) && index.DataVersions.Single()=="v","coverage counts cannot turn missing or failed data into an exhaustive empty set");
            Check(index.ForItem(1).Count==0 && index.ForItem(4551).Single().Variants.Single().Quantity is null,"failed snapshot ignored, unknown quantity retained");
        }),
        ("Quest Item index detaches producer collections and exposes readonly candidate and variant lists",()=>
        {
            var rows=new List<QuestEntry>{Q(65537,4551)};var index=QuestRewardIndex.Build(rows);rows.Clear();
            Check(index.ForItem(4551).Count==1,"index owns source associations");
            Throws<NotSupportedException>(()=>((IList<QuestItemRewardSource>)index.ForItem(4551)).Clear());
            Throws<NotSupportedException>(()=>((IList<QuestRewardItem>)index.ForItem(4551)[0].Variants).Clear());
            Throws<NotSupportedException>(()=>((IList<string>)index.DataVersions).Clear());
        }),
        ("Quest Item index rejects invalid duplicate or unbounded sources and cancels during enumeration",()=>
        {
            Throws<InvalidDataException>(()=>QuestRewardIndex.Build([Q(1,1),Q(1,2)]));
            Throws<InvalidDataException>(()=>QuestRewardIndex.Build([Q(0,1)]));
            Throws<InvalidDataException>(()=>QuestRewardIndex.Build(Enumerable.Range(1,50001).Select(id=>Q((uint)id,1) with {Rewards=null})));
            var many=new QuestRewardSnapshot(1,0,Enumerable.Repeat(new QuestRewardItem(1,1,QuestRewardRule.Fixed,"fixed",0),2048),[]);
            Throws<InvalidDataException>(()=>QuestRewardIndex.Build(Enumerable.Range(1,100).Select(id=>Q((uint)id,1) with {Rewards=many})));
            using var cancellation=new CancellationTokenSource();
            IEnumerable<QuestEntry> Cancelled(){yield return Q(1,1);cancellation.Cancel();yield return Q(2,2);}
            Throws<OperationCanceledException>(()=>QuestRewardIndex.Build(Cancelled(),cancellation.Token));
        }),
        ("Quest service default worker uses copied managed snapshots without changing native progression",()=>
        {
            using var service=new QuestService();var rows=new List<QuestEntry>{Q(65537,4551)};
            service.Publish(10,rows);rows.Clear();service.IndexWork.GetAwaiter().GetResult();
            Check(service.State.CharacterId==10 && service.State.Entries.Single().Progress==QuestProgress.Active && service.State.Entries.Single().Sequence==3,"index completion preserves observation and scope");
            Check(!service.State.RewardIndexLoading && service.State.RewardIndex?.ForItem(4551).Single().QuestId==65537,"index ready from immutable source despite producer mutation");
        }),
        ("Late quest index completion cannot replace a newer character or same-character observation",()=>
        {
            foreach(var nextOwner in new ulong[]{10,20})
            {
                var requests=new List<(IReadOnlyList<QuestEntry> Rows,CancellationToken Token,TaskCompletionSource<QuestRewardIndex> Reply)>();
                using var service=new QuestService((rows,token)=>{var reply=Pending();requests.Add((rows,token,reply));return reply.Task;});
                service.Publish(10,[Q(65537,4551)]);var oldWork=service.IndexWork;
                service.Publish(nextOwner,[Q(65538,12892)]);var newWork=service.IndexWork;
                Check(requests[0].Token.IsCancellationRequested,"superseded worker cancelled");
                requests[1].Reply.SetResult(QuestRewardIndex.Build(requests[1].Rows));newWork.GetAwaiter().GetResult();var expected=service.State;
                requests[0].Reply.SetResult(QuestRewardIndex.Build(requests[0].Rows));oldWork.GetAwaiter().GetResult();
                Check(ReferenceEquals(expected,service.State) && service.State.CharacterId==nextOwner && service.State.RewardIndex!.ForItem(4551).Count==0,"generation gate rejects late reply even if worker ignores cancellation");
            }
        }),
        ("Quest index invalidation clear failure and disposal cannot resurrect owner or pending results",()=>
        {
            foreach(var boundary in new[]{"invalidate","clear","fail","dispose"})
            {
                var reply=Pending();CancellationToken received=default;IReadOnlyList<QuestEntry>? rows=null;
                using var service=new QuestService((source,token)=>{rows=source;received=token;return reply.Task;});
                service.Publish(10,[Q(65537,4551)]);var work=service.IndexWork;
                switch(boundary){case "invalidate":service.Invalidate();break;case "clear":service.Clear();break;case "fail":service.Fail(new Exception("failed"));break;default:service.Dispose();break;}
                var expected=service.State;Check(received.IsCancellationRequested && expected.CharacterId==0 && expected.RewardIndex is null,"boundary clears scope/index and cancels worker");
                Check(service.IndexWork.IsCompleted,"current-state work handle released even when obsolete producer ignores cancellation");
                reply.SetResult(QuestRewardIndex.Build(rows!));work.GetAwaiter().GetResult();
                Check(ReferenceEquals(expected,service.State),"late result cannot resurrect boundary state");
                if(boundary=="dispose"){service.Publish(20,[Q(1,1)]);service.RequestRefresh();Check(ReferenceEquals(expected,service.State) && !service.ConsumeRefresh(),"disposed service is inert");}
            }
        }),
        ("Quest source search combines category name and full identity without losing variants or other items",()=>
        {
            var index=QuestRewardIndex.Build([Q(65537,4551,"Potion royale") with {Category="Artisanat"},Q(65538,4551,"Autre potion"),Q(65539,12892,"Potion royale")]);
            Check(index.Search(4551," ROYALE\tartisanat ").Single().QuestId==65537,"all tokens may span name and native category, ignoring case and whitespace");
            Check(index.Search(4551,"65537").Single().Variants.SequenceEqual(index.ForItem(4551)[1].Variants),"full quest ID and reward variants retained");
            Check(index.Search(4551,"655").Count==0 && index.Search(0,"potion").Count==0,"no partial identity or cross-item source leak");
            Check(ReferenceEquals(index.Search(4551,null),index.ForItem(4551)) && index.Search(4551,"   ").Count==2,"empty query restores original candidates");
            var filtered=index.Search(4551,"potion");Throws<NotSupportedException>(()=>((IList<QuestItemRewardSource>)filtered).Clear());
            Check(index.ForItem(4551).Count==2,"search cannot mutate shared sources");
        }),
        ("Quest index failures and unexpected cancellations stay isolated from quest observation errors",()=>
        {
            foreach(var cancelled in new[]{false,true})
            {
                using var service=new QuestService((_,_)=>cancelled ? Task.FromCanceled<QuestRewardIndex>(new CancellationToken(true)):Task.FromException<QuestRewardIndex>(new InvalidDataException("index failed")));
                service.Publish(10,[Q(65537,4551)]);service.IndexWork.GetAwaiter().GetResult();
                Check(service.State.Error is null && service.State.RewardIndexError is not null && !service.State.RewardIndexLoading && service.State.Entries.Single().Progress==QuestProgress.Active,"failed index never erases native quest state or stays loading forever");
            }
        }),
    };
}
