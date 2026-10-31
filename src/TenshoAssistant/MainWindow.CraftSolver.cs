using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private readonly CraftSolveSession craftSolve=new();
    private string? loggedCraftSearchError;
    private CraftSolveInput? craftSolveSignature;
    private readonly record struct CraftSearchViewKey(ulong Owner,RecipeDefinition Recipe,int Level,int Craftsmanship,int Control,int Cp,bool Manipulation,
        int StartingQuality,int Target,int Steps,int Width,int Budget,int ExtraWait,int ContextItem);
    private CraftSearchViewKey? craftSearchViewKey;
    private int craftSolveRevision,craftSolveTarget,craftSolveSteps=40,craftSolveWidth=128,craftSolveBudget=100000;
    private void ValidateCraftSearch()
    {
        if(simulationRecipe is not { } recipe)
        {
            if(craftSolveSignature is not null || craftSolve.State.Input is not null)craftSolve.Cancel();
            craftSolveSignature=null;craftSearchViewKey=null;return;
        }
        var key=new CraftSearchViewKey(bankBridge.State.CharacterId,recipe,simulationLevel,simulationCs,simulationControl,simulationCp,simulationManipulation,
            simulationQuality,craftSolveTarget,craftSolveSteps,craftSolveWidth,craftSolveBudget,craftMacroExtraWait,itemId);
        if(craftSearchViewKey!=key || craftSolveSignature is null)
        {
            craftSolveSignature=new(key.Owner,0,recipe,new(key.Level,key.Craftsmanship,key.Control,key.Cp,key.Manipulation),key.StartingQuality,
                new(key.Target,key.Steps,key.Width,key.Budget,key.ExtraWait));
            craftSearchViewKey=key;craftSolveRevision++;craftSolve.Cancel();
        }
        craftSolve.ValidateContext(key.Owner,craftSolveRevision,recipe.RecipeId);
    }
    private void DrawCraftSearch()
    {
        ValidateCraftSearch();
        if(!ImGui.CollapsingHeader(localization.Get("CraftSearch.Title")+"###CraftSearch")) return;
        ImGui.TextWrapped(localization.Format("CraftSearchResults.Description",CraftActionCatalog.Specs.Count));
        ImGui.SetNextItemWidth(120); ImGui.InputInt(localization.Get("CraftSearch.Target")+"###CraftTarget",ref craftSolveTarget,0); ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("CraftSearch.Maximum"))) craftSolveTarget=simulationRecipe!.Simulation!.Quality;
        ImGui.TextDisabled(localization.Get("CraftSearch.TargetHelp"));
        ImGui.TextWrapped(localization.Get("CraftSearch.Priority"));
        DrawToolSettingsLink("CraftSearch");
        ImGui.TextDisabled(localization.Format("CraftSearch.Limits",craftSolveSteps,craftSolveWidth,craftSolveBudget));
        ValidateCraftSearch();
        var state=craftSolve.State;
        ImGui.BeginDisabled(state.Loading);
        try
        {
            if(ImGui.Button(localization.Get("CraftSearch.Start")) && craftSolveSignature is { } input)
                _=craftSolve.StartAsync(input with {Revision=craftSolveRevision});
        }
        finally {ImGui.EndDisabled();}
        if(state.Loading) {ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("CraftSearch.Cancel"))) craftSolve.Cancel(); ImGui.TextDisabled(localization.Get("CraftSearch.Loading"));}
        if(state.Error is { } error)
        {
            if(loggedCraftSearchError != error)
            {
                log.Warning("Craft search failed: {Reason}",error);
                loggedCraftSearchError = error;
            }
            ImGui.TextWrapped(localization.Get("CraftSearchResults.Error"));
            if(developerMode) ImGui.TextWrapped(error);
        }
        else loggedCraftSearchError = null;
        if(state.Result is not { } result) return;
        if(result.WaitSeconds is { } waits)ImGui.TextDisabled(localization.Format("CraftSearchResults.Waits",waits));
        ImGui.TextDisabled(localization.Format("CraftSearchResults.Statistics",result.Expanded,result.Depth,state.CalculatedAt?.ToLocalTime().ToString("T") ?? "—"));
        if(result.BeamPruned) ImGui.TextDisabled(localization.Get("CraftSearch.Pruned"));
        if(result.NodeLimitReached || result.DepthLimitReached) ImGui.TextDisabled(localization.Get("CraftSearch.LimitReached"));
        if(result.Simulation is not { } simulation) {ImGui.TextWrapped(localization.Get("CraftSearch.NoResult"));return;}
        ImGui.TextColored(result.TargetReached ? Success:Accent,localization.Format("CraftSearchResults.Summary",localization.Get(result.TargetReached ? "CraftSearchResults.Reached":"CraftSearchResults.Below"),simulation.Final.Quality,result.TargetQuality,simulation.Steps.Count,simulation.Final.Cp));
        if(ImGui.Button(localization.Get(result.TargetReached ? "CraftSearch.Use":"CraftSearch.UseBelow")))
        {
            simulationActions.Clear(); simulationActions.AddRange(simulation.Steps.Select(step=>step.Action)); simulationResult=simulation; simulationError=null;
        }
        ImGui.TextWrapped(localization.Get("CraftSearch.UseHelp"));
    }
}
