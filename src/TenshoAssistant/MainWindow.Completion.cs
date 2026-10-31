using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private bool focusTitleTab,focusAchievementTab,completionDetailsRequested;
    private uint selectedCompletionId;
    private CollectionKind selectedCompletionKind;
    private uint? achievementCategoryFilter;
    private CollectionState? achievementCategoryState,completionDetailState;
    private CollectionEntry[] achievementCategories=[],relatedCompletion=[];
    private CollectionEntry? completionDetail;
    private uint cachedCompletionId;
    private CollectionKind cachedCompletionKind;

    private string CollectionAcquisitionLabel(CollectionEntry entry)=>localization.Get(entry.Acquisition switch
    {
        CollectionAcquisition.Acquired=>entry.Kind==CollectionKind.Achievement ? "CollectionState.Completed":"CollectionState.Acquired",
        CollectionAcquisition.NotAcquired=>entry.Kind==CollectionKind.Achievement ? "CollectionState.NotCompleted":"CollectionState.NotAcquired",
        _=>"CollectionState.Unknown",
    });

    private void OpenCompletion(CollectionEntry entry)
    {
        selectedCompletionKind=entry.Kind;selectedCompletionId=entry.Id;completionDetailsRequested=true;
        focusTitleTab=entry.Kind==CollectionKind.Title;focusAchievementTab=entry.Kind==CollectionKind.Achievement;
        collectionKind=entry.Kind;collectionQuery=entry.Name;collectionFilter=0;achievementCategoryFilter=entry.Achievement?.CategoryId;
        page=Page.Collections;
    }

    private void DrawAchievementCategories(CollectionState state)
    {
        if(!ReferenceEquals(state,achievementCategoryState))
        {
            achievementCategoryState=state;
            achievementCategories=state.Entries.Where(entry=>entry.Kind==CollectionKind.Achievement && entry.Achievement is {CategoryName.Length:>0})
                .GroupBy(entry=>entry.Achievement!.CategoryId).Select(group=>group.First())
                .OrderBy(entry=>entry.Achievement!.KindOrder).ThenBy(entry=>entry.Achievement!.CategoryOrder).ToArray();
            if(achievementCategoryFilter is { } selected && !achievementCategories.Any(entry=>entry.Achievement!.CategoryId==selected))achievementCategoryFilter=null;
        }
        var current=achievementCategories.FirstOrDefault(entry=>entry.Achievement!.CategoryId==achievementCategoryFilter);
        ImGui.SetNextItemWidth(330);
        if(!ImGui.BeginCombo(localization.Get("CompletionDetails.Category")+"###AchievementCategory",current?.Achievement?.CategoryName ?? localization.Get("OrchestrionDetails.AllCategories")))return;
        try
        {
            if(ImGui.Selectable(localization.Get("OrchestrionDetails.AllCategories"),achievementCategoryFilter is null))achievementCategoryFilter=null;
            foreach(var entry in achievementCategories)
            {
                var meta=entry.Achievement!;
                if(ImGui.Selectable($"{meta.KindName} · {meta.CategoryName}##AchievementCategory{meta.CategoryId}",meta.CategoryId==achievementCategoryFilter))achievementCategoryFilter=meta.CategoryId;
            }
        }
        finally{ImGui.EndCombo();}
    }

    private void DrawCompletionDetails(CollectionState state)
    {
        ImGui.SetNextWindowSize(new Vector2(560,0),ImGuiCond.Appearing);var open=true;
        if(!ImGui.BeginPopupModal(localization.Get("CompletionDetails.Title")+"###CompletionDetails",ref open,ImGuiWindowFlags.AlwaysAutoResize))return;
        try
        {
            if(!ReferenceEquals(completionDetailState,state) || cachedCompletionId!=selectedCompletionId || cachedCompletionKind!=selectedCompletionKind)
            {
                completionDetailState=state;cachedCompletionId=selectedCompletionId;cachedCompletionKind=selectedCompletionKind;
                completionDetail=state.Entries.FirstOrDefault(entry=>entry.Kind==selectedCompletionKind && entry.Id==selectedCompletionId);
                relatedCompletion=completionDetail?.Title is not null
                    ? state.Entries.Where(entry=>entry.Achievement?.RewardTitleId==selectedCompletionId).ToArray()
                    : completionDetail?.Achievement is {RewardTitleId:>0} reward
                        ? state.Entries.Where(entry=>entry.Kind==CollectionKind.Title && entry.Id==reward.RewardTitleId).ToArray():[];
            }
            if(completionDetail is not { } entry)ImGui.TextWrapped(localization.Get("CompletionDetails.Missing"));
            else
            {
                ImGui.TextColored(Accent,entry.Name);ImGui.TextColored(entry.ConfirmedUnlocked ? Success:Muted,CollectionAcquisitionLabel(entry));
                if(entry.Title is { } title)
                {
                    ImGui.TextWrapped(localization.Format("CompletionDetails.Gender",title.Masculine,title.Feminine));
                    ImGui.TextDisabled(localization.Get(title.IsPrefix ? "CompletionDetails.Prefix":"CompletionDetails.Suffix"));
                    ImGui.TextColored(Accent,localization.Format("CompletionDetails.Related",relatedCompletion.Length));
                }
                if(entry.Achievement is { } achievement)
                {
                    ImGui.TextDisabled(localization.Format("CompletionDetails.Points",achievement.KindName,achievement.CategoryName,achievement.Points));
                    ImGui.TextWrapped(entry.Description);
                    if(achievement.RewardItemId!=0)
                    {
                        var item=itemCatalog.State.Catalog?.Find(achievement.RewardItemId);
                        if(item is null)ImGui.TextWrapped(localization.Get("CompletionDetails.MissingItem"));
                        else
                        {
                            DrawItemIcon(item,new Vector2(32));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                            clicked|=ImGui.SmallButton(localization.Format("CompletionDetails.Item",item.Name));
                            if(clicked){SelectItem(item.Id);page=Page.Items;focusStocksTab=true;ImGui.CloseCurrentPopup();}
                        }
                    }
                    if(achievement.RewardTitleId!=0 && relatedCompletion.Length==0)ImGui.TextWrapped(localization.Get("CompletionDetails.MissingTitle"));
                    ImGui.TextWrapped(localization.Get("CompletionDetails.RewardHelp"));
                }
                foreach(var related in relatedCompletion)
                {
                    if(ImGui.SmallButton($"{related.Name}##Related{related.Kind}{related.Id}"))
                    {OpenCompletion(related);ImGui.CloseCurrentPopup();}
                }
                ImGui.TextWrapped(localization.Get("CompletionDetails.Help"));
            }
            if(ImGui.Button(localization.Get("BlueSpell.Close")))ImGui.CloseCurrentPopup();
        }
        finally{ImGui.EndPopup();}
    }
}
