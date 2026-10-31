using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private uint? orchestrionCategoryFilter;
    private uint selectedOrchestrion;
    private bool focusOrchestrionTab,openOrchestrionDetailsRequested;
    private CollectionState? orchestrionCategoryState;
    private CollectionEntry[] orchestrionCategories=[];

    private void OpenOrchestrionFromItem(CollectionEntry entry)
    {
        selectedOrchestrion=entry.Id;orchestrionCategoryFilter=entry.Music?.CategoryId;
        collectionQuery=entry.Name;collectionFilter=0;collectionKind=CollectionKind.Orchestrion;
        focusOrchestrionTab=true;openOrchestrionDetailsRequested=true;page=Page.Collections;
    }

    private void DrawOrchestrionCategories(CollectionState state)
    {
        if(!ReferenceEquals(state,orchestrionCategoryState))
        {
            orchestrionCategoryState=state;
            orchestrionCategories=state.Entries.Where(entry=>entry.Kind==CollectionKind.Orchestrion && entry.Music is { } music && !string.IsNullOrWhiteSpace(music.CategoryName))
                .GroupBy(entry=>entry.Music!.CategoryId).Select(group=>group.First()).OrderBy(entry=>entry.Music!.CategoryOrder).ThenBy(entry=>entry.Music!.CategoryName).ToArray();
            if(orchestrionCategoryFilter is { } chosen && !orchestrionCategories.Any(entry=>entry.Music!.CategoryId==chosen))orchestrionCategoryFilter=null;
        }
        var selected=orchestrionCategories.FirstOrDefault(entry=>entry.Music!.CategoryId==orchestrionCategoryFilter);
        ImGui.SetNextItemWidth(280);
        if(!ImGui.BeginCombo(localization.Get("OrchestrionDetails.Category")+"###OrchestrionCategory",selected?.Music?.CategoryName ?? localization.Get("OrchestrionDetails.AllCategories")))return;
        try
        {
            if(ImGui.Selectable(localization.Get("OrchestrionDetails.AllCategories"),orchestrionCategoryFilter is null))orchestrionCategoryFilter=null;
            foreach(var category in orchestrionCategories)
            {
                DrawItemIcon(new ItemDefinition(0,category.Music!.CategoryName,false,category.IconId),new Vector2(22));ImGui.SameLine();
                if(ImGui.Selectable($"{category.Music.CategoryName}##MusicCategory{category.Music.CategoryId}",category.Music.CategoryId==orchestrionCategoryFilter))orchestrionCategoryFilter=category.Music.CategoryId;
            }
        }
        finally{ImGui.EndCombo();}
    }

    private void DrawOrchestrionDetails(CollectionState state)
    {
        ImGui.SetNextWindowSize(new Vector2(540,0),ImGuiCond.Appearing);var opened=true;
        if(!ImGui.BeginPopupModal(localization.Get("OrchestrionDetails.Title")+"###OrchestrionDetails",ref opened,ImGuiWindowFlags.AlwaysAutoResize))return;
        try
        {
            var entry=state.Entries.FirstOrDefault(row=>row.Kind==CollectionKind.Orchestrion && row.Id==selectedOrchestrion);
            if(entry is null){ImGui.TextWrapped(localization.Get("OrchestrionDetails.Missing"));}
            else
            {
                ImGui.TextColored(Accent,entry.Name);
                ImGui.TextDisabled(entry.Music is {CategoryName.Length:>0} music ? music.CategoryName:localization.Get("OrchestrionDetails.UnknownCategory"));
                ImGui.TextColored(entry.ConfirmedUnlocked ? Success:Muted,localization.Get(entry.ConfirmedUnlocked ? "OrchestrionDetails.Acquired":"CollectionState.Unknown"));
                ImGui.TextWrapped(localization.Get("OrchestrionDetails.Help"));
                if(!string.IsNullOrWhiteSpace(entry.Description)){ImGui.Separator();ImGui.TextWrapped(entry.Description);}
                ImGui.Separator();
                if(itemCatalog.State.Catalog is not { } catalog)ImGui.TextWrapped(localization.Get("OrchestrionDetails.Loading"));
                else
                {
                    var items=catalog.ForOrchestrion(entry.Id);
                    if(items.Count==0)ImGui.TextWrapped(localization.Get("OrchestrionDetails.Empty"));
                    foreach(var item in items)
                    {
                        DrawItemIcon(item,new Vector2(32));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                        clicked|=ImGui.SmallButton($"{item.Name}##MusicItem{item.Id}");
                        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("OrchestrionDetails.ItemHelp"));
                        if(clicked){SelectItem(item.Id);page=Page.Items;focusStocksTab=true;ImGui.CloseCurrentPopup();}
                    }
                }
            }
            if(ImGui.Button(localization.Get("BlueSpell.Close")))ImGui.CloseCurrentPopup();
        }
        finally{ImGui.EndPopup();}
    }
}
