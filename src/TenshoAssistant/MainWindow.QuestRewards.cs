using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// « Reported rewards » section of a quest in the Quests page: reward items copied from the game's Quest sheet with their rule
// (fixed, choice, by gender, conditional, by job, allied-society rank variant, currency); each name has the common item tooltip and
// tools menu. Static catalogue: variants and choices do not add up, and receipt or eligibility is not checked.
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
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("QuestRewards.Title")+"###QuestRewards"))return;
        ImGui.TextWrapped(localization.Get("QuestRewards.Help"));
        if(entry.RewardsError is { } error){ImGui.TextWrapped(localization.Get("QuestRewards.Error"));if(developerMode)ImGui.TextWrapped(RawMessage(error));return;}
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
                if(!TenshoAssistant.UI.UiComponents.Header(heading,key.Rule is QuestRewardRule.Fixed or QuestRewardRule.Choice or QuestRewardRule.Currency ? ImGuiTreeNodeFlags.DefaultOpen:ImGuiTreeNodeFlags.None))continue;
                foreach(var line in group)
                {
                    var item=itemCatalog.State.Catalog?.Find(line.ItemId);
                    DrawItemIcon(item,new Vector2(28));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                    ImGui.BeginDisabled(item is null);
                    try{clicked|=ImGui.SmallButton($"{item?.Name ?? localization.Get("QuestRewards.Item")}##Reward{line.Slot}");}
                    finally{ImGui.EndDisabled();}
                    // 0.255: the reward name gives the common item tooltip (quality and quantity follow it) and the tools menu.
                    if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && item is not null)DrawItemInfoTooltip(item.Id);
                    ItemLinksMenu($"RewardLinks{line.Slot}",item);
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
                "Job reward table missing."=>"QuestRewardDetails.JobTable",
                "Rank bonus not resolved."=>"QuestRewardDetails.RankBonus",
                "Reward type not interpreted; this number is not converted to an Item."=>"QuestRewardDetails.Type",
                "Currency not linked to an Item; quantity kept."=>"QuestRewardDetails.Currency",
                "Tomestone type missing or ambiguous."=>"QuestRewardDetails.Tomestone",
                _=>"QuestRewards.Unknown",
            };
            ImGui.TextWrapped(localization.Get(key)+(unresolved.Quantity is { } quantity ? localization.Format("UiFragments.UnresolvedQuantity",quantity.ToString("N0")):""));
            if(developerMode)ImGui.TextWrapped(RawMessage(unresolved.Reason));
        }
        if(rewards.HasAdditionalRewards)ImGui.TextWrapped(localization.Get("QuestRewards.Additional"));
        if(rewards.Items.Count==0 && rewards.Gil==0 && rewards.Unresolved.Count==0)ImGui.TextDisabled(localization.Get("QuestRewards.Empty"));
        ImGui.TextDisabled(localization.Get("QuestRewards.Source"));
    }
}
