using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Npcs;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// g, I-11 (0.352): the unlock condition of a shop article (GilShopItem.QuestRequired / AchievementRequired, SpecialShop Quest /
// AchievementUnlock), shown after its price with « done », « to do » or « not read » (quest journal, achievements read by Tensho).
// Lookups rebuilt only when the journal or the collections change.
internal sealed partial class MainWindow
{
    internal Func<uint,string?>? QuestNameOf { get; set; }
    internal Func<uint,string?>? AchievementNameOf { get; set; }
    private (IReadOnlyList<QuestEntry> Source,Dictionary<uint,QuestProgress> Progress)? shopQuests;
    private (IReadOnlyList<CollectionEntry> Source,Dictionary<uint,bool> Done)? shopAchievements;

    private void DrawShopRequirement(NpcShopSale sale)
    {
        var journal=quests.State.Entries;
        if(!ReferenceEquals(shopQuests?.Source,journal))shopQuests=(journal,journal.GroupBy(entry=>entry.Id).ToDictionary(group=>group.Key,group=>group.First().Progress));
        var collected=collections.State.Entries;
        if(!ReferenceEquals(shopAchievements?.Source,collected))
            shopAchievements=(collected,collected.Where(entry=>entry.Kind==CollectionKind.Achievement).GroupBy(entry=>entry.Id).ToDictionary(group=>group.Key,group=>group.First().ConfirmedUnlocked));
        var parts=new List<(string Text,bool? Done)>();
        if(sale.QuestRequired!=0)
            parts.Add((localization.Format("ShopRequirement.Quest",QuestNameOf?.Invoke(sale.QuestRequired) ?? $"#{sale.QuestRequired}"),
                shopQuests.Value.Progress.TryGetValue(sale.QuestRequired,out var progress) ? progress==QuestProgress.Complete ? true:quests.State.CharacterId==0 ? null:false:null));
        if(sale.AchievementRequired!=0)
            parts.Add((localization.Format("ShopRequirement.Achievement",AchievementNameOf?.Invoke(sale.AchievementRequired) ?? $"#{sale.AchievementRequired}"),
                shopAchievements.Value.Done.TryGetValue(sale.AchievementRequired,out var done) ? done ? true:null:null));
        for(var index=0;index<parts.Count;index++)
        {
            if(index>0)ImGui.SameLine(0,6);
            var (text,state)=parts[index];
            var suffix=localization.Get(state==true ? "ShopRequirement.Done":state==false ? "ShopRequirement.ToDo":"ShopRequirement.Unknown");
            if(state==true)ImGui.TextColored(Success,$"{text} {suffix}");else ImGui.TextColored(Accent,$"{text} {suffix}");
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("ShopRequirement.Tip"));
        }
    }
}
