using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private RecipeDefinition? simulationRecipe;
    private readonly List<CraftActionKind> simulationActions=[];
    private int simulationLevel=100,simulationCs,simulationControl,simulationCp,simulationQuality;
    private bool simulationManipulation;
    private ulong simulationCopiedOwner;
    private DateTimeOffset? simulationCopiedAt;
    private CraftSimulationResult? simulationResult;
    private string? simulationError;
    private CraftSimulationResult? macroExportSimulation;
    private CraftMacroExport? craftMacroExport;
    private int craftMacroExtraWait;
    private bool craftMacroEcho=true;
    private string? craftMacroError;
    private void OpenCraftSimulation(RecipeDefinition recipe)
    {
        if(simulationRecipe?.RecipeId!=recipe.RecipeId)
        {
            simulationActions.Clear(); simulationResult=null; simulationError=null; simulationQuality=0;
            simulationCopiedAt=null; simulationCopiedOwner=0; simulationCs=simulationControl=simulationCp=0;
            simulationLevel=100; simulationManipulation=false;
            craftSolveTarget=recipe.Simulation?.Quality ?? 0;
        }
        simulationRecipe=recipe; SelectItem(recipe.ResultItemId); page=Page.CraftSimulation;
    }
    private void ValidateCraftCopy()
    {
        if(simulationCopiedOwner==0 || simulationCopiedOwner==characterStats.State.CharacterId) return;
        simulationCopiedOwner=0; simulationCopiedAt=null; simulationCs=simulationControl=simulationCp=0;
        simulationLevel=1; simulationResult=null; simulationManipulation=false;
        simulationError=localization.Get("SimulationHelp.Changed");
    }
    private void DrawCraftSimulation()
    {
        Heading(localization.Get("CraftSimulation.Title"),localization.Get("CraftSimulation.Description"));
        if(ImGui.SmallButton(localization.Get("CraftSimulation.Live"))) page=Page.CraftLive;
        if(ImGui.SmallButton(localization.Get("CraftSimulation.Notebook"))) { page=Page.Items; focusCraftTab=true; }
        if(simulationRecipe is not { } recipe) { ImGui.TextWrapped(localization.Get("CraftSimulation.ChooseRecipe")); return; }
        var resultItem=itemCatalog.State.Catalog?.Find(recipe.ResultItemId);
        DrawItemIcon(resultItem,new Vector2(36)); ImGui.SameLine();
        ImGui.TextColored(Accent,localization.Format("SimulationHelp.Recipe",resultItem?.Name ?? localization.Format("BankUi.Item",recipe.ResultItemId),recipe.CraftType,recipe.Level));
        if(developerMode)ImGui.TextDisabled($"ID {recipe.RecipeId}");
        if(CraftSimulation.Unsupported(recipe) is { } unsupported) { ImGui.TextWrapped(CraftMessage(unsupported)); return; }
        if(craftActions.State.Catalog is not { } catalog) { ImGui.TextWrapped(craftActions.State.Error ?? localization.Get("SimulationHelp.Loading")); return; }
        DrawCraftPresetSave();
        var p=recipe.Simulation!; var job=recipe.CraftTypeId+8;
        if(p.Collectable)ImGui.TextColored(Accent,localization.Get("SimulationHelp.Collectable"));
        ImGui.TextDisabled(localization.Format("SimulationHelp.Parameters",p.Progress,p.Quality,p.Durability,p.RequiredCraftsmanship,p.RequiredControl));
        ImGui.TextWrapped(localization.Get("SimulationHelp.Model"));
        var stats=characterStats.State;
        var canCopy=stats.CharacterId!=0 && stats.JobId==job && TenshoAssistant.Caching.CacheAge.Fresh(stats.ObservedAt,DateTimeOffset.UtcNow,TimeSpan.FromSeconds(5))
            && stats.Values.ContainsKey("Craftsmanship") && stats.Values.ContainsKey("Control") && stats.Values.ContainsKey("CraftingPoints");
        ImGui.BeginDisabled(!canCopy);
        try
        {
            if(ImGui.SmallButton(localization.Get("CraftSimulation.CopyStats")))
            {
                simulationLevel=stats.Level; simulationCs=stats.Values["Craftsmanship"]; simulationControl=stats.Values["Control"]; simulationCp=stats.Values["CraftingPoints"];
                simulationCopiedOwner=stats.CharacterId; simulationCopiedAt=stats.ObservedAt; simulationResult=null; simulationError=null;
            }
        }
        finally { ImGui.EndDisabled(); }
        if(simulationCopiedAt is { } copiedAt) ImGui.TextDisabled(localization.Format("SimulationHelp.Copied",copiedAt.ToLocalTime().ToString("T")));
        var changed=false;
        if(ImGui.CollapsingHeader(localization.Get("CraftSimulation.Attributes")+"###SimulationAttributes",ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Get("CraftSimulation.Level")+"###SimLevel",ref simulationLevel,0);
            ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Get("CraftSimulation.Craftsmanship")+"###SimCraftsmanship",ref simulationCs,0);
            ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Get("CraftSimulation.Control")+"###SimControl",ref simulationControl,0);
            ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Get("CraftSimulation.Cp")+"###SimCp",ref simulationCp,0);
            ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Format("CraftSimulation.StartingQuality",p.MaxStartingQuality)+"###SimQuality",ref simulationQuality,0);
            changed|=ImGui.Checkbox(localization.Get("CraftSimulation.Manipulation")+"###SimManipulation",ref simulationManipulation);
        }
        if(changed) { simulationCopiedOwner=0; simulationCopiedAt=null; simulationResult=null; simulationError=null; }
        DrawCraftSearch();
        if(ImGui.CollapsingHeader(localization.Get("CraftSimulation.AddAction")+"###SimAddAction",ImGuiTreeNodeFlags.DefaultOpen))
        {
            var columns=Math.Max(1,(int)(ImGui.GetContentRegionAvail().X/175));
            if(ImGui.BeginTable("CraftActionPalette",columns,ImGuiTableFlags.SizingStretchSame))
            {
                try
                {
                    foreach(var spec in CraftActionCatalog.Specs)
                    {
                        var action=catalog.Find(job,spec.Kind); if(action is null) continue;
                        ImGui.TableNextColumn(); ImGui.PushID((int)spec.Kind);
                        try
                        {
                            ImGui.BeginDisabled(simulationActions.Count>=100 || simulationLevel<spec.Level || spec.Kind==CraftActionKind.Manipulation && !simulationManipulation);
                            try
                            {
                                DrawItemIcon(new ItemDefinition(0,action.Name,false,action.Icon),new Vector2(26)); var click=ImGui.IsItemClicked();
                                if(ImGui.IsItemHovered()) ImGui.SetTooltip(action.Description);
                                ImGui.SameLine(); click|=ImGui.SmallButton(action.Name);
                                if(ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Format("SimulationHelp.Action",action.Description,spec.Level,spec.Cp));
                                if(click && simulationActions.Count<100 && simulationLevel>=spec.Level && (spec.Kind!=CraftActionKind.Manipulation || simulationManipulation))
                                { simulationActions.Add(spec.Kind); simulationResult=null; }
                            }
                            finally { ImGui.EndDisabled(); }
                        }
                        finally { ImGui.PopID(); }
                    }
                }
                finally { ImGui.EndTable(); }
            }
        }
        ImGui.TextColored(Accent,localization.Format("CraftSequence.Count",simulationActions.Count));
        if(ImGui.SmallButton(localization.Get("CraftSequence.Clear"))) { simulationActions.Clear(); simulationResult=null; simulationError=null; }
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("CraftSequence.Simulate")))
        {
            try { simulationResult=CraftSimulation.Run(recipe,new(simulationLevel,simulationCs,simulationControl,simulationCp,simulationManipulation),simulationQuality,simulationActions); simulationError=null; }
            catch(ArgumentException error)
            {
                simulationResult=null;
                simulationError=localization.Get(error.ParamName=="stats" ? "SimulationHelp.InvalidStats" : error.ParamName=="sequence" ? "CraftMessages.ActionLimit" : "ServiceErrors.Craft");
                log.Warning(error,"Craft simulation rejected its input.");
            }
        }
        if(simulationError is { } errorText) ImGui.TextWrapped(CraftMessage(errorText));
        if(simulationResult is { } result)
        {
            var state=result.Final;
            ImGui.TextColored(state.Finish==CraftFinish.Completed ? Success:Accent,localization.Format("CraftSequence.Result",localization.Get(state.Finish switch {CraftFinish.Completed=>"CraftSequence.Completed",CraftFinish.Broken=>"CraftSequence.Broken",_=>"CraftSequence.Incomplete"}),state.Progress,p.Progress,state.Quality,p.Quality,state.Durability,state.Cp));
            if(result.InvalidAction is { } invalid) ImGui.TextWrapped(localization.Format("SimulationHelp.Invalid",invalid+1,CraftMessage(result.Error ?? "—")));
            if(result.Steps.Count<result.PlannedActions) ImGui.TextDisabled(localization.Format("CraftSequence.Skipped",result.PlannedActions-result.Steps.Count));
            DrawCraftConditionEnvelope(result);
            DrawCraftMacroExport(result,job,catalog);
        }
        if(ImGui.BeginTable("CraftSequence",6,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                foreach(var title in new[]{"Action","Progress","Quality","Durability","Cost","InnerQuiet"}) ImGui.TableSetupColumn(localization.Get("CraftSequence."+title));
                ImGui.TableHeadersRow();
                for(var i=0;i<simulationActions.Count;i++)
                {
                    var kind=simulationActions[i]; var metadata=catalog.Find(job,kind); var step=simulationResult?.Steps.ElementAtOrDefault(i);
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.PushID(i);
                    try
                    {
                        DrawItemIcon(metadata is null ? null:new ItemDefinition(0,metadata.Name,false,metadata.Icon),new Vector2(24)); ImGui.SameLine();
                        ImGui.TextWrapped($"{i+1}. {metadata?.Name ?? kind.ToString()}");
                        if(ImGui.IsItemHovered() && step is not null) ImGui.SetTooltip(localization.Format("SimulationHelp.Effects",step.After.Veneration,step.After.Innovation,step.After.GreatStrides,step.After.WasteNot,step.After.Manipulation,step.After.MuscleMemory,localization.Get(step.After.TrainedPerfection ? "SimulationHelp.Protected":step.After.UsedTrainedPerfection ? "SimulationHelp.Used":"SimulationHelp.Available")));
                        if(ImGui.SmallButton(localization.Get("CraftPresets.Remove"))) { simulationActions.RemoveAt(i); simulationResult=null; break; }
                        if(i>0) { ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("CraftSequence.Up"))) { (simulationActions[i-1],simulationActions[i])=(simulationActions[i],simulationActions[i-1]); simulationResult=null; break; } }
                        if(i+1<simulationActions.Count) { ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("CraftSequence.Down"))) { (simulationActions[i+1],simulationActions[i])=(simulationActions[i],simulationActions[i+1]); simulationResult=null; break; } }
                    }
                    finally { ImGui.PopID(); }
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step is null ? "—":$"{step.After.Progress} (+{step.ProgressGain})");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step is null ? "—":$"{step.After.Quality} (+{step.QualityGain})");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step?.After.Durability.ToString() ?? "—");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step is null ? "—":$"{step.After.Cp} (−{step.CpCost})");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step?.After.InnerQuiet.ToString() ?? "—");
                }
            }
            finally { ImGui.EndTable(); }
        }
    }

    private void DrawCraftMacroExport(CraftSimulationResult result,uint job,CraftActionCatalog catalog)
    {
        if(!ReferenceEquals(macroExportSimulation,result)) { craftMacroExport=null; craftMacroError=null; macroExportSimulation=result; }
        if(!ImGui.CollapsingHeader(localization.Get("CraftMacro.Title")+"###CraftMacroExport")) return;
        ImGui.TextWrapped(localization.Get("CraftMacro.Help"));
        DrawToolSettingsLink("CraftMacroExport");
        ImGui.TextDisabled(localization.Format("CraftMacro.Options",craftMacroExtraWait,localization.Get(craftMacroEcho ? "CraftMacro.Yes":"CraftMacro.No")));
        ImGui.BeginDisabled(result.Final.Finish!=CraftFinish.Completed || result.Error is not null);
        try
        {
            if(ImGui.Button(localization.Get("CraftMacro.Prepare")))
            {
                try { craftMacroExport=CraftMacroExporter.Build(result,job,catalog,craftMacroExtraWait,craftMacroEcho); craftMacroError=null; }
                catch(ArgumentException error) { craftMacroExport=null; craftMacroError="CraftMacro.Failed"; log.Warning(error,"Could not export craft macros."); }
            }
        }
        finally { ImGui.EndDisabled(); }
        if(result.Final.Finish!=CraftFinish.Completed || result.Error is not null) ImGui.TextDisabled(localization.Get("CraftMacro.Required"));
        if(craftMacroError is { } errorText) ImGui.TextWrapped(localization.Get(errorText));
        if(craftMacroExport is not { } export) return;
        ImGui.TextDisabled(localization.Format("CraftMacro.Summary",export.Actions,export.Pages.Count,export.WaitSeconds));
        if(export.OmittedActions>0) ImGui.TextWrapped(localization.Format("CraftMacro.Omitted",export.OmittedActions));
        foreach(var block in export.Pages)
        {
            ImGui.PushID(block.Number);
            try
            {
                if(ImGui.CollapsingHeader(localization.Format("CraftMacro.Block",block.Number,export.Pages.Count,block.Lines.Count)+"###MacroBlock",ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(ImGui.SmallButton(localization.Get("CraftMacro.Copy"))) ImGui.SetClipboardText(block.Text);
                    foreach(var line in block.Lines) ImGui.TextUnformatted(line);
                }
            }
            finally { ImGui.PopID(); }
        }
    }
}
