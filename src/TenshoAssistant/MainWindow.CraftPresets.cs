using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string craftPresetName="";private string? craftPresetError;private bool craftPresetSaved;private CraftPreset? lastSavedCraftPreset;
    private void SaveCraftPreset()
    {
        try
        {
            var preset=CraftPresets.Create(simulationRecipe!,craftPresetName,simulationActions);
            CraftPresets.Commit(configuration,save,rows=>CraftPresets.Add(rows,preset));craftPresetError=null;craftPresetSaved=true;lastSavedCraftPreset=preset;
        }
        catch(Exception error){craftPresetError="CraftPresetMessages.SaveFailed";craftPresetSaved=false;log.Warning(error,"Could not save craft sequence.");}
    }
    private void DrawCraftPresetSave()
    {
        if(ImGui.SmallButton(localization.Get("CraftPresets.Browse")))page=Page.CraftPresets;
        if(!ImGui.CollapsingHeader(localization.Get("CraftPresets.SaveSection")+"###SaveCraftPreset"))return;
        ImGui.SetNextItemWidth(260);if(ImGui.InputText(localization.Get("CraftPresets.Name")+"###CraftPresetName",ref craftPresetName,160))craftPresetSaved=false;
        if(ImGui.Button(localization.Get("CraftPresets.Save")))SaveCraftPreset();
        ImGui.TextDisabled(localization.Get("CraftPresets.Help"));
        if(craftPresetSaved && lastSavedCraftPreset is { } saved && saved.RecipeId==simulationRecipe?.RecipeId && saved.Actions.SequenceEqual(simulationActions))ImGui.TextColored(Success,localization.Get("CraftPresets.Saved"));
        if(craftPresetError is { } error)ImGui.TextWrapped(localization.Get(error));
    }
    private void DrawCraftPresets()
    {
        Heading(localization.Get("CraftPresets.Title"),localization.Get("CraftPresets.Description"));
        if(ImGui.SmallButton(localization.Get("CraftPresets.Back")))page=Page.CraftSimulation;
        var rows=configuration.CraftPresets ?? [];var catalog=recipeCatalog.State.Catalog;
        ImGui.TextDisabled(localization.Format("CraftPresetMessages.Count", rows.Count, CraftPresets.Capacity));
        if(craftPresetError is { } error)ImGui.TextWrapped(localization.Get(error));
        var remove=-1;
        for(var index=0;index<Math.Min(rows.Count,1000);index++)
        {
            var preset=rows[index];var recipe=CraftPresets.Resolve(preset,catalog);ImGui.PushID(index);
            try
            {
                if(preset is not null)
                {
                    var item=itemCatalog.State.Catalog?.Find(preset.ResultItemId);DrawItemIcon(item,new Vector2(28));ImGui.SameLine();
                    ImGui.TextWrapped($"{preset.Name} · {item?.Name ?? localization.Format("CraftPresetMessages.Item", preset.ResultItemId)}");
                    ImGui.TextDisabled(localization.Format("CraftPresetMessages.Details",recipe is null ? localization.Get("CraftPresetMessages.Unresolved") : $"{recipe.CraftType} {recipe.Level}",preset.Actions?.Count ?? 0,preset.SavedAt.ToLocalTime().ToString("g")));
                    if(developerMode)ImGui.TextDisabled($"Recipe ID {preset.RecipeId}");
                }
                else ImGui.TextWrapped(localization.Get("CraftPresetMessages.Unreadable"));
                ImGui.BeginDisabled(recipe is null);
                try
                {
                    if(ImGui.SmallButton(localization.Get("CraftPresets.Load")) && recipe is not null && preset?.Actions is { } actions)
                    {
                        OpenCraftSimulation(recipe);simulationActions.Clear();simulationActions.AddRange(actions);simulationResult=null;simulationError=null;
                        macroExportSimulation=null;craftMacroExport=null;craftSolve.Cancel();craftPresetName=preset.Name;craftPresetSaved=false;craftPresetError=null;
                    }
                }
                finally{ImGui.EndDisabled();}
                ImGui.SameLine();if(ImGui.SmallButton(localization.Get("CraftPresets.Remove")))remove=index;
                if(recipe is null)ImGui.TextDisabled(localization.Get(catalog is null ? "CraftPresetMessages.Loading":"CraftPresetMessages.Unresolved"));
                ImGui.Separator();
            }
            finally{ImGui.PopID();}
        }
        if(rows.Count>1000)ImGui.TextWrapped(localization.Get("CraftPresetMessages.Limit"));
        if(remove>=0)
        {
            try{CraftPresets.Commit(configuration,save,source=>CraftPresets.Remove(source,remove));craftPresetError=null;}
            catch(Exception saveError){craftPresetError="CraftPresetMessages.RemoveFailed";log.Warning(saveError,"Could not remove saved craft sequence.");}
        }
    }
}
