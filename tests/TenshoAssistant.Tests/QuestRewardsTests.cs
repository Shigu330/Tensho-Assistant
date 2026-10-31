using TenshoAssistant.Quests;

internal static class QuestRewardsTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception(typeof(T).Name);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Quest reward alternatives retain quantities quality native slots and condition identities without summing",()=>
        {
            var source=new[]{new QuestRewardItem(4551,2,QuestRewardRule.Fixed,"fixed",0),new(4551,5,QuestRewardRule.Choice,"optional",0,true),
                new(4551,0,QuestRewardRule.TribeRankVariant,"rank",0,SourceId:2,Variant:0),new(4551,3,QuestRewardRule.TribeRankVariant,"rank",0,SourceId:2,Variant:7),
                new(13611,1,QuestRewardRule.ClassJob,"job",0,ConditionId:20,ConditionLabel:"PLD",SourceId:3,Variant:0,ParentSlot:1),
                new(13611,1,QuestRewardRule.ClassJob,"job",0,ConditionId:20,ConditionLabel:"PLD",SourceId:3,Variant:0,ParentSlot:2)};
            var snapshot=new QuestRewardSnapshot(7,200,source,[]);
            Check(snapshot.Items.Count==6 && snapshot.Items.Select(item=>item.Quantity).SequenceEqual(new uint?[]{2,5,0,3,1,1}),"no collapsing same Item IDs or rank variants into a maximum/sum");
            Check(snapshot.Items[1].HighQuality==true && snapshot.Items[4].ParentSlot!=snapshot.Items[5].ParentSlot,"optional HQ and parent/subrow identities retained");
        }),
        ("Unknown reward references and direct zero quantities never become guessed Items or quantity one",()=>
        {
            var snapshot=new QuestRewardSnapshot(99,0,[new(4551,null,QuestRewardRule.Unknown,"fixed",0)],
                [new("job",3,0,"Unresolved table"),new("currency",0,0,"Company dependent currency",300)],true);
            Check(snapshot.Items.Single().ItemId==4551 && snapshot.Items.Single().Quantity is null,"explicit unknown amount not replaced");
            Check(snapshot.Unresolved[0].ReferenceId==3 && snapshot.Items.All(item=>item.ItemId!=3),"non-Item source identity never substituted as Item ID");
            Check(snapshot.Unresolved[1].Quantity==300 && snapshot.HasAdditionalRewards,"unresolved currency quantity and other rewards not erased");
        }),
        ("Tomestone type mapping preserves foreign keys rejects ambiguity and owns its producers",()=>
        {
            var source=new List<(uint Type,uint Item)>{(1,28),(3,49),(3,49),(0,24),(4,47),(4,48)};
            var map=QuestRewardCurrencyMap.Build(source);source.Clear();
            Check(map[1]==28 && map[3]==49 && !map.ContainsKey(0),"type1 is poetic Item28, type3 Item49, never positional old Item24");
            Check(map[4] is null && !map.ContainsKey(99),"ambiguous/missing identities unresolved without first-row fallback");
        }),
        ("Quest reward snapshots remain static owned facts across progression and character invalidation",()=>
        {
            var items=new List<QuestRewardItem>{new(4551,2,QuestRewardRule.Fixed,"fixed",0)};
            var unresolved=new List<QuestUnresolvedReward>{new("currency",0,0,"Unknown",10)};
            var snapshot=new QuestRewardSnapshot(1,100,items,unresolved,version:"fixture");items.Clear();unresolved.Clear();
            Throws<NotSupportedException>(()=>((IList<QuestRewardItem>)snapshot.Items).Clear());
            Throws<NotSupportedException>(()=>((IList<QuestUnresolvedReward>)snapshot.Unresolved).Clear());
            var service=new QuestService();service.Publish(10,[new(65537,"Quête","Genre",1,QuestProgress.Complete,null){Rewards=snapshot}]);
            service.Invalidate();Check(service.State.CharacterId==0 && service.State.Entries.Single().Progress==QuestProgress.Unconfirmed && ReferenceEquals(service.State.Entries.Single().Rewards,snapshot),"owner/progression cleared independently of immutable catalogue reward facts");
            Check(snapshot.DataVersion=="fixture" && snapshot.Source=="Lumina.Excel/Quest" && snapshot.Items.Count==1,"provenance preserved after producer mutation");
        }),
        ("Malformed or oversized reward snapshots reject publication inputs",()=>
        {
            Throws<ArgumentException>(()=>new QuestRewardSnapshot(1,0,[new(0,1,QuestRewardRule.Fixed,"fixed",0)],[]));
            Throws<ArgumentException>(()=>new QuestRewardSnapshot(1,0,[new(1,1,QuestRewardRule.Fixed,"fixed",-1)],[]));
            Throws<ArgumentException>(()=>new QuestRewardSnapshot(1,0,Enumerable.Repeat(new QuestRewardItem(1,1,QuestRewardRule.Fixed,"fixed",0),2049),[]));
            Throws<ArgumentException>(()=>new QuestRewardSnapshot(1,0,[],Enumerable.Repeat(new QuestUnresolvedReward("x",0,0,"missing"),129)));
        }),
    };
}
