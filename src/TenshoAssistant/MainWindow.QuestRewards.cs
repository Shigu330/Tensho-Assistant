using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string QuestRewardConditionLabel(QuestRewardItem item)=>item.Rule switch
    {
        QuestRewardRule.Fixed=>localization.Get(item.Source=="catalyst" ? "QuestRewards.FixedExtra":"QuestRewards.Fixed"),
        QuestRewardRule.Choice=>localization.Get("QuestRewards.Choice"),
        QuestRewardRule.Masculine=>localization.Get("QuestRewards.Masculine"),
        QuestRewardRule.Feminine=>localization.Get("QuestRewards.Feminine"),
        QuestRewardRule.ConditionalChoice=>localization.Get("QuestRewards.Conditional"),
        QuestRewardRule.ClassJob=>localization.Format("QuestRewards.Job",item.ConditionId==0 || string.IsNullOrWhiteSpace(item.ConditionLabel) ? localization.Get("QuestRewardDetails.UnknownJob"):item.ConditionLabel),
        QuestRewardRule.TribeRankVariant=>localization.Format("QuestRewardDetails.RankVariant",item.Variant+1),
        QuestRewardRule.Currency=>localization.Get("QuestRewards.Currency"),
        _=>localization.Get("QuestRewards.Unknown"),
    };
    private void DrawQuestRewards(QuestEntry entry)
    {
        if(!ImGui.CollapsingHeader(localization.Get("QuestRewards.Title")+"###QuestRewards"))return;
        ImGui.TextWrapped(localization.Get("QuestRewards.Help"));
        if(entry.RewardsError is { } error){ImGui.TextWrapped(localization.Get("QuestRewards.Error"));if(developerMode)ImGui.TextWrapped(error);return;}
        if(entry.Rewards is not { } rewards){ImGui.TextWrapped(localization.Get("QuestRewards.Missing"));return;}
        if(rewards.Gil>0)ImGui.TextColored(Accent,localization.Format("QuestRewards.Gil",rewards.Gil));
        var index=0;
        foreach(var group in rewards.Items.GroupBy(item=>(item.Rule,item.Source,item.ConditionId,item.ConditionLabel,item.SourceId,item.Variant,item.ParentSlot)))
        {
            ImGui.PushID(index++);
            try
            {
                var key=group.Key;
                var heading=QuestRewardConditionLabel(group.First());
                if(!ImGui.CollapsingHeader(heading,key.Rule is QuestRewardRule.Fixed or QuestRewardRule.Choice or QuestRewardRule.Currency ? ImGuiTreeNodeFlags.DefaultOpen:ImGuiTreeNodeFlags.None))continue;
                foreach(var line in group)
                {
                    var item=itemCatalog.State.Catalog?.Find(line.ItemId);
                    DrawItemIcon(item,new Vector2(28));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                    ImGui.BeginDisabled(item is null);
                    try{clicked|=ImGui.SmallButton($"{item?.Name ?? localization.Get("QuestRewards.Item")}##Reward{line.Slot}");}
                    finally{ImGui.EndDisabled();}
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("QuestRewards.Quality",line.HighQuality is null ? localization.Get("QuestRewards.Unspecified"):line.HighQuality==true ? "HQ":"NQ")+"\n"+localization.Get(line.StainId==0 ? "QuestRewards.NoStain":"QuestRewards.Stain")+"\n"+localization.Get("QuestRewards.Click"));
                    ImGui.SameLine();ImGui.TextDisabled(line.Quantity is { } quantity ? $"× {quantity:N0}{(line.HighQuality==true ? " HQ":"")}":localization.Get("QuestRewards.Quantity"));
                    if(clicked && item is not null){SelectItem(item.Id);page=Page.Items;focusStocksTab=true;}
                }
            }
            finally{ImGui.PopID();}
        }
        foreach(var unresolved in rewards.Unresolved)
        {
            var key=unresolved.Reason switch
            {
                "Table de récompenses par métier absente."=>"QuestRewardDetails.JobTable",
                "Bonus de rang non résolu."=>"QuestRewardDetails.RankBonus",
                "Type de récompense non interprété ; ce numéro n’est pas converti en Item."=>"QuestRewardDetails.Type",
                "Monnaie non reliée à un Item ; quantité conservée."=>"QuestRewardDetails.Currency",
                "Type de mémoquartz absent ou ambigu."=>"QuestRewardDetails.Tomestone",
                _=>"QuestRewards.Unknown",
            };
            ImGui.TextWrapped(localization.Get(key)+(unresolved.Quantity is { } quantity ? localization.Format("UiFragments.UnresolvedQuantity",quantity.ToString("N0")):""));
            if(developerMode)ImGui.TextWrapped(unresolved.Reason);
        }
        if(rewards.HasAdditionalRewards)ImGui.TextWrapped(localization.Get("QuestRewards.Additional"));
        if(rewards.Items.Count==0 && rewards.Gil==0 && rewards.Unresolved.Count==0)ImGui.TextDisabled(localization.Get("QuestRewards.Empty"));
        ImGui.TextDisabled(localization.Get("QuestRewards.Source"));
    }
}
