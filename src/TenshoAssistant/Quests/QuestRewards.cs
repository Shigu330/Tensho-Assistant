using System.Collections.Frozen;

namespace TenshoAssistant.Quests;

public static class QuestRewardCurrencyMap
{
    public static IReadOnlyDictionary<uint,uint?> Build(IEnumerable<(uint Type,uint Item)> rows)=>
        rows.Where(row=>row.Type!=0 && row.Item!=0).GroupBy(row=>row.Type)
            .ToFrozenDictionary(group=>group.Key,group=>group.Select(row=>row.Item).Distinct().Count()==1 ? (uint?)group.First().Item:null);
}

public enum QuestRewardRule { Fixed, Choice, Masculine, Feminine, ConditionalChoice, ClassJob, TribeRankVariant, Currency, Unknown }
public sealed record QuestRewardItem(uint ItemId,uint? Quantity,QuestRewardRule Rule,string Source,int Slot,
    bool? HighQuality=null,uint StainId=0,uint ConditionId=0,string ConditionLabel="",uint SourceId=0,int Variant=0,int ParentSlot=0);
public sealed record QuestUnresolvedReward(string Source,uint ReferenceId,int Slot,string Reason,uint? Quantity=null);

/// <summary>Static alternatives from one native quest. Never a statement of actual receipt or eligibility.</summary>
public sealed class QuestRewardSnapshot
{
    public byte Mode {get;}
    public uint Gil {get;}
    public IReadOnlyList<QuestRewardItem> Items {get;}
    public IReadOnlyList<QuestUnresolvedReward> Unresolved {get;}
    public bool HasAdditionalRewards {get;}
    public string DataVersion {get;}
    public string Source=>"Lumina.Excel/Quest";
    public QuestRewardSnapshot(byte mode,uint gil,IEnumerable<QuestRewardItem> items,IEnumerable<QuestUnresolvedReward> unresolved,bool additional=false,string version="")
    {
        var copied=items.ToArray();var missing=unresolved.ToArray();
        if(copied.Length>2048 || missing.Length>128 || copied.Any(item=>item.ItemId==0 || item.Slot<0 || item.Variant<0 || item.ParentSlot<0))throw new ArgumentException("Invalid or unbounded native quest rewards.");
        Mode=mode;Gil=gil;Items=Array.AsReadOnly(copied);Unresolved=Array.AsReadOnly(missing);HasAdditionalRewards=additional;DataVersion=version;
    }
}
