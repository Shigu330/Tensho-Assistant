using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// Saved craft sequences: « Save » in the simulator and the presets page (search, sort, load into the simulator, rename, duplicate, copy as
// text for the simulator's import, remove; the first 1 000 entries shown),
// stored in the configuration (Craft/CraftPresets.cs; recipe and actions only, no attributes or results). The Crafting Log panel also
// offers the saved sequence of its recipe.
internal sealed partial class MainWindow
{
    private string craftPresetSearch="",craftPresetRenameText="";private int craftPresetSort;private Guid? craftPresetRenaming;
    private (IReadOnlyList<CraftPreset> Rows,string Search,int Sort,object? Recipes,object? Items,int[] View)? craftPresetView;
    private int[] CraftPresetRows(IReadOnlyList<CraftPreset> rows,RecipeCatalog? catalog)
    {
        var items=itemCatalog.State.Catalog;
        if(craftPresetView is { } known && ReferenceEquals(known.Rows,rows) && known.Search==craftPresetSearch && known.Sort==craftPresetSort && ReferenceEquals(known.Recipes,catalog) && ReferenceEquals(known.Items,items))return known.View;
        var view=CraftPresets.View(rows,craftPresetSearch,(CraftPresetSort)craftPresetSort,row=>items?.Find(row.ResultItemId)?.Name ?? "",row=>CraftPresets.Resolve(row,catalog)?.CraftType ?? "");
        craftPresetView=(rows,craftPresetSearch,craftPresetSort,catalog,items,view);
        return view;
    }
    private void EditCraftPresets(Func<IReadOnlyList<CraftPreset>,IReadOnlyList<CraftPreset>> change,string failure)
    {
        try{CraftPresets.Commit(configuration,save,change);craftPresetError=null;craftPresetRenaming=null;}
        catch(Exception error){craftPresetError=failure;log.Warning(error,"Could not change the saved craft sequences.");}
    }
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
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftPresets.SaveSection")+"###SaveCraftPreset"))return;
        ImGui.SetNextItemWidth(260);if(ImGui.InputText(localization.Get("CraftPresets.Name")+"###CraftPresetName",ref craftPresetName,160))craftPresetSaved=false;Tip("CraftPresets.NameTip");
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
        ImGui.SetNextItemWidth(260);ImGui.InputTextWithHint("###CraftPresetSearch",localization.Get("CraftPresets.Search"),ref craftPresetSearch,80);Tip("CraftPresets.SearchTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(170);
        ImGui.Combo(localization.Get("CraftPresets.Sort")+"###CraftPresetSort",ref craftPresetSort,[localization.Get("CraftPresets.SortSaved"),localization.Get("CraftPresets.SortName"),localization.Get("CraftPresets.SortItem"),localization.Get("CraftPresets.SortJob"),localization.Get("CraftPresets.SortNewest")]);Tip("CraftPresets.SortTip");
        var view=CraftPresetRows(rows,catalog);
        if(view.Length==0 && rows.Count>0)ImGui.TextDisabled(localization.Get("CraftPresets.NoMatch"));
        var remove=-1;var rename=-1;var duplicate=-1;
        foreach(var index in view.Take(1000))
        {
            var preset=rows[index];var recipe=CraftPresets.Resolve(preset,catalog);ImGui.PushID(index);
            try
            {
                if(preset is not null)
                {
                    var item=itemCatalog.State.Catalog?.Find(preset.ResultItemId);DrawItemIcon(item,new Vector2(28));ImGui.SameLine();
                    ImGui.TextWrapped($"{preset.Name} · {item?.Name ?? localization.Format("BankUi.Item", preset.ResultItemId)}");
                    ImGui.TextDisabled(localization.Format("CraftPresetMessages.Details",recipe is null ? localization.Get("CraftPresetMessages.Unresolved") : $"{recipe.CraftType} {recipe.Level}",preset.Actions?.Count ?? 0,preset.SavedAt.ToLocalTime().ToString("g")));
                    if(developerMode)ImGui.TextDisabled(localization.Format("DevMode.RecipeId",preset.RecipeId));
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
                if(preset is not null)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("CraftPresets.Rename"))){craftPresetRenaming=preset.Id;craftPresetRenameText=preset.Name;}
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("CraftPresets.Duplicate")))duplicate=index;
                    Tip("CraftPresets.DuplicateTip");
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("CraftPresets.CopyText")))
                    {
                        var job=preset.CraftTypeId+8;var actionCatalog=craftActions.State.Catalog;
                        if(CraftMacroExporter.Text(preset.Actions ?? [],kind=>actionCatalog?.Find(job,kind)?.Name) is { } text){ImGui.SetClipboardText(text);craftPresetError=null;}
                        else craftPresetError="CraftPresetMessages.CopyFailed";
                    }
                    Tip("CraftPresets.CopyTextTip");
                }
                ImGui.SameLine();if(ImGui.SmallButton(localization.Get("CraftPresets.Remove")))remove=index;Tip("CraftPresets.RemoveTip");
                if(preset is not null && craftPresetRenaming==preset.Id)
                {
                    ImGui.SetNextItemWidth(260);
                    var confirmed=ImGui.InputText("###CraftPresetRename",ref craftPresetRenameText,80,ImGuiInputTextFlags.EnterReturnsTrue);Tip("CraftPresets.NameTip");
                    ImGui.SameLine();if(ImGui.SmallButton(localization.Get("CraftPresets.RenameOk")) || confirmed)rename=index;
                    ImGui.SameLine();if(ImGui.SmallButton(localization.Get("CraftPresets.RenameCancel")))craftPresetRenaming=null;
                }
                if(recipe is null)ImGui.TextDisabled(localization.Get(catalog is null ? "CraftPresetMessages.Loading":"CraftPresetMessages.Unresolved"));
                ImGui.Separator();
            }
            finally{ImGui.PopID();}
        }
        if(rows.Count>1000)ImGui.TextWrapped(localization.Get("CraftPresetMessages.Limit"));
        if(rename>=0){var name=craftPresetRenameText;var position=rename;EditCraftPresets(source=>CraftPresets.Rename(source,position,name),"CraftPresetMessages.RenameFailed");}
        if(duplicate>=0){var position=duplicate;EditCraftPresets(source=>CraftPresets.Duplicate(source,position),"CraftPresetMessages.DuplicateFailed");}
        if(remove>=0)
        {
            try{CraftPresets.Commit(configuration,save,source=>CraftPresets.Remove(source,remove));craftPresetError=null;}
            catch(Exception saveError){craftPresetError="CraftPresetMessages.RemoveFailed";log.Warning(saveError,"Could not remove saved craft sequence.");}
        }
    }
}
