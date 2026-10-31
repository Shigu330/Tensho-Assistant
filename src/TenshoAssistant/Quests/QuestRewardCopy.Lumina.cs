using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Quests;

public static class QuestRewardCopy
{
    public static QuestRewardSnapshot FromRow(Quest row,SubrowExcelSheet<QuestClassJobReward> jobs,
        ExcelSheet<BeastRankBonus> bonuses,IReadOnlyDictionary<uint,uint?> tomestones,string version="")
    {
        var items=new List<QuestRewardItem>();var unresolved=new List<QuestUnresolvedReward>();var mode=row.ItemRewardType;
        void Missing(string source,uint id,int slot,string reason,uint? count=null)=>unresolved.Add(new(source,id,slot,reason,count));
        for(var slot=0;slot<row.Reward.Count;slot++)
        {
            var reference=row.Reward[slot];if(reference.RowId==0)continue;
            if(reference.RowType==typeof(Item))
            {
                var rule=mode switch {1=>QuestRewardRule.Fixed,3 when slot==0=>QuestRewardRule.Masculine,3 when slot==1=>QuestRewardRule.Feminine,5=>QuestRewardRule.ConditionalChoice,_=>QuestRewardRule.Unknown};
                uint count=slot<row.ItemCountReward.Count ? row.ItemCountReward[slot]:0u;
                items.Add(new(reference.RowId,count==0 ? null:count,rule,"fixed",slot,StainId:slot<row.RewardStain.Count ? row.RewardStain[slot].RowId:0));
            }
            else if(mode==6 && reference.RowType==typeof(QuestClassJobReward))
            {
                if(!jobs.TryGetRow(reference.RowId,out var variants)){Missing("job",reference.RowId,slot,"Table de récompenses par métier absente.");continue;}
                if(variants.Count>128)throw new InvalidDataException("Too many quest job reward variants.");
                foreach(var variant in variants)
                for(var index=0;index<variant.RewardItem.Count;index++)
                {
                    var id=variant.RewardItem[index].RowId;if(id==0)continue;
                    uint amount=index<variant.RewardAmount.Count ? variant.RewardAmount[index]:0u;
                    var category=variant.ClassJobCategory.RowId;
                    var label=category==0 ? "Métier non renseigné":variant.ClassJobCategory.Value.Name.ExtractText();
                    items.Add(new(id,amount==0 ? null:amount,QuestRewardRule.ClassJob,"job",index,
                        ConditionId:category,ConditionLabel:label,SourceId:reference.RowId,Variant:variant.SubrowId,ParentSlot:slot));
                }
            }
            else if(mode==7 && reference.RowType==typeof(BeastRankBonus))
            {
                if(!bonuses.TryGetRow(reference.RowId,out var bonus) || bonus.Item.RowId==0){Missing("rank",reference.RowId,slot,"Bonus de rang non résolu.");continue;}
                for(var index=0;index<bonus.ItemQuantity.Count;index++)
                    items.Add(new(bonus.Item.RowId,bonus.ItemQuantity[index],QuestRewardRule.TribeRankVariant,"rank",slot,
                        ConditionLabel:$"Variante de rang {index+1}",SourceId:reference.RowId,Variant:index));
            }
            else Missing("fixed",reference.RowId,slot,"Type de récompense non interprété ; ce numéro n’est pas converti en Item.");
        }
        for(var index=0;index<row.OptionalItemReward.Count;index++)
        {
            var id=row.OptionalItemReward[index].RowId;if(id==0)continue;
            uint count=index<row.OptionalItemCountReward.Count ? row.OptionalItemCountReward[index]:0u;
            var rule=mode==1 ? QuestRewardRule.Choice:mode==5 ? QuestRewardRule.ConditionalChoice:QuestRewardRule.Unknown;
            items.Add(new(id,count==0 ? null:count,rule,"optional",index,
                index<row.OptionalItemIsHQReward.Count ? row.OptionalItemIsHQReward[index]:null,
                index<row.OptionalItemStainReward.Count ? row.OptionalItemStainReward[index].RowId:0));
        }
        for(var index=0;index<row.ItemCatalyst.Count;index++)
        {
            var id=row.ItemCatalyst[index].RowId;if(id==0)continue;
            uint count=index<row.ItemCountCatalyst.Count ? row.ItemCountCatalyst[index]:0u;
            items.Add(new(id,count==0 ? null:count,QuestRewardRule.Fixed,"catalyst",index));
        }
        if(row.CurrencyRewardCount>0)
        {
            if(row.CurrencyReward.RowId==0)Missing("currency",0,0,"Monnaie non reliée à un Item ; quantité conservée.",row.CurrencyRewardCount);
            else items.Add(new(row.CurrencyReward.RowId,row.CurrencyRewardCount,QuestRewardRule.Currency,"currency",0));
        }
        if(row.TomestoneCountReward>0)
        {
            if(!tomestones.TryGetValue(row.TomestoneReward,out var id) || id is null)Missing("tomestone",row.TomestoneReward,0,"Type de mémoquartz absent ou ambigu.",row.TomestoneCountReward);
            else items.Add(new(id.Value,row.TomestoneCountReward,QuestRewardRule.Currency,"tomestone",0));
        }
        return new(mode,row.GilReward,items,unresolved,row.OtherReward.RowId!=0 || row.EmoteReward.RowId!=0 || row.ActionReward.RowId!=0
            || row.GeneralActionReward.Any(action=>action.RowId!=0) || row.SystemReward.Any(value=>value!=0) || row.ExpFactor!=0 || row.ReputationReward!=0,version);
    }

}
