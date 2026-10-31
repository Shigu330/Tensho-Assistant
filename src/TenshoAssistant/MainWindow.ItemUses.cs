using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private bool focusUsesTab;
    private void DrawItemUses()
    {
        Heading(localization.Get("ItemUsesUi.Title"),localization.Get("ItemUsesUi.Description"));
        if(itemId<=0){ImGui.TextWrapped(localization.Get("ItemUsesUi.Select"));return;}
        DrawRecipeResultHeader();var id=(uint)itemId;
        if(SelectedItem is {OrchestrionId:>0} roll)
        {
            var music=collections.State.Entries.FirstOrDefault(entry=>entry.Kind==TenshoAssistant.Collections.CollectionKind.Orchestrion && entry.Id==roll.OrchestrionId);
            ImGui.TextColored(Accent,localization.Get("ItemUsesUi.Roll"));
            ImGui.TextWrapped(music is {ConfirmedUnlocked:true} ? localization.Get("ItemUsesUi.Acquired"):localization.Get("ItemUsesUi.UnknownMusic"));
            ImGui.BeginDisabled(music is null);
            try{if(ImGui.SmallButton(localization.Get("ItemUsesUi.MusicDetails")) && music is not null)OpenOrchestrionFromItem(music);}
            finally{ImGui.EndDisabled();}
            if(music is null)ImGui.TextDisabled(localization.Get("ItemUsesUi.MissingMusic"));
        }
        if(recipeCatalog.State.Catalog is { } recipes)
        {
            var count=recipes.ForIngredient(id).Count;
            if(ImGui.SmallButton(localization.Format("ItemUseCounts.Recipes",count)))focusCraftTab=true;
        }
        else ImGui.TextDisabled(localization.Get("ItemUsesUi.LoadingRecipes"));
        DrawQuestItemSources(id);
        var state=itemUses.State;
        if(state.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(state.Error is null ? localization.Format("ItemUseCounts.Loading",state.Copied,state.Total):localization.Get("ItemUsesUi.Unavailable"));return;
        }
        ImGui.Separator();
        if(catalog.IsSupplyCandidate(id))
        {ImGui.TextColored(Accent,localization.Get("ItemUsesUi.Gc"));ImGui.TextWrapped(localization.Get("ItemUsesUi.GcHelp"));}
        if(catalog.IsAquariumFish(id))
        {ImGui.TextColored(Accent,localization.Get("ItemUsesUi.Aquarium"));ImGui.TextWrapped(localization.Get("ItemUsesUi.AquariumHelp"));}
        if(!catalog.IsSupplyCandidate(id) && !catalog.IsAquariumFish(id))ImGui.TextDisabled(localization.Get("ItemUsesUi.NoOther"));
        var leves=catalog.LevesFor(id);ImGui.Spacing();ImGui.TextColored(Accent,localization.Format("ItemUseCounts.Leves",leves.Count));
        ImGui.TextWrapped(localization.Get("ItemUsesUi.LeveHelp"));
        if(leves.Count==0)ImGui.TextDisabled(localization.Get("ItemUsesUi.NoLeve"));
        foreach(var leve in leves)
        {
            ImGui.PushID((int)leve.RowId);
            try
            {
                DrawItemIcon(new ItemDefinition(0,leve.Name,false,leve.IconId),new Vector2(24));ImGui.SameLine();
                var job=string.IsNullOrWhiteSpace(leve.Job) ? localization.Get("ItemUsesUi.UnknownJob"):leve.Job;
                var level=leve.Level>0 ? localization.Format("QuestDetails.Level",leve.Level):localization.Get("ItemUsesUi.UnknownLevel");
                if(!ImGui.CollapsingHeader($"{leve.Name} · {job} · {level}"))continue;
                if(!string.IsNullOrWhiteSpace(leve.Place))ImGui.TextWrapped(localization.Format("ItemUseCounts.Place",leve.Place));
                ImGui.TextDisabled(localization.Get("ItemUsesUi.Requests"));
                for(var index=0;index<leve.Requests.Count;index++)
                {
                    var request=leve.Requests[index];var item=itemCatalog.State.Catalog?.Find(request.ItemId);ImGui.PushID(index);
                    try
                    {
                        DrawItemIcon(item,new Vector2(28));var click=ImGui.IsItemClicked();ImGui.SameLine();
                        click|=ImGui.SmallButton(item?.Name ?? localization.Format("CraftPresetMessages.Item",request.ItemId));
                        ImGui.SameLine();ImGui.TextUnformatted(request.Quantity is { } quantity ? $"× {quantity}":localization.Get("ItemUsesUi.Quantity"));
                        if(click){SelectItem(request.ItemId);focusUsesTab=true;}
                    }
                    finally{ImGui.PopID();}
                }
            }
            finally{ImGui.PopID();}
        }
        if(catalog.UnresolvedLeveRows>0)ImGui.TextWrapped(localization.Format("ItemUseCounts.Partial",catalog.UnresolvedLeveRows));
        ImGui.TextDisabled(localization.Format("ItemUseCounts.Source",catalog.Language));
    }
}
