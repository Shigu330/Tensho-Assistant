using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// Craft simulator › search: a deterministic bounded beam search (Craft/CraftSolver.cs) for a sequence reaching the target quality, started
// by the player and run on a worker (Craft/CraftSolveSession, cancellable); steps / width / node limits are edited in Settings › Modules
// and saved in the configuration (CraftSearch, 0.321). The input key is rebuilt when the recipe, stats or limits change
// and an outdated search is cancelled. No result does not prove that none exists.
internal sealed partial class MainWindow
{
    // A-03 (0.465, DEC-028): Raphael when the search asks for it, otherwise Tensho's own search.
    private readonly CraftSolveSession craftSolve=new((input,token)=>input.Options.Raphael
        ? RaphaelCraftSolver.Solve(input.Recipe,input.Stats,input.StartingQuality,input.Options,token)
        :CraftSolver.Solve(input.Recipe,input.Stats,input.StartingQuality,input.Options,token));
    private string? loggedCraftSearchError;
    private CraftSolveInput? craftSolveSignature;
    private readonly record struct CraftSearchViewKey(ulong Owner,RecipeDefinition Recipe,int Level,int Craftsmanship,int Control,int Cp,bool Manipulation,
        int StartingQuality,int Target,int Steps,int Width,int Budget,int ExtraWait,int ContextItem,int? BuffExtraWait=null,string Forbidden="",bool Specialist=false,bool Raphael=false);
    private CraftSearchViewKey? craftSearchViewKey;
    private int craftSolveRevision,craftSolveTarget;
    // 0.321: search limits and export options come from the saved settings (Settings › Modules › Crafting).
    private CraftSearchSettings CraftSearchOptions=>configuration.CraftSearch??=new();
    private int craftSolveSteps=>CraftSearchOptions.BoundedSteps;
    private int craftSolveWidth=>CraftSearchOptions.BoundedWidth;
    private int craftSolveBudget=>CraftSearchOptions.BoundedBudget;
    private int craftMacroExtraWait=>CraftSearchOptions.BoundedExtraWait;
    private int? craftMacroBuffWait=>CraftSearchOptions.BoundedBuffExtraWait;
    // 0.438 (user request): the forbidden actions' key.
    private string craftForbidden=>CraftActionRules.Key(CraftSearchOptions.ForbiddenActions);
    private bool craftMacroEcho=>CraftSearchOptions.Echo;
    private void ValidateCraftSearch()
    {
        if(simulationRecipe is not { } recipe)
        {
            if(craftSolveSignature is not null || craftSolve.State.Input is not null)craftSolve.Cancel();
            craftSolveSignature=null;craftSearchViewKey=null;return;
        }
        var effective=EffectiveCraftStats();
        var key=new CraftSearchViewKey(bankBridge.State.CharacterId,recipe,simulationLevel,effective.Craftsmanship,effective.Control,effective.Cp,simulationManipulation,
            simulationQuality,craftSolveTarget,craftSolveSteps,craftSolveWidth,craftSolveBudget,craftMacroExtraWait,itemId,craftMacroBuffWait,craftForbidden,effective.Specialist,CraftSearchOptions.Raphael);
        if(craftSearchViewKey!=key || craftSolveSignature is null)
        {
            craftSolveSignature=new(key.Owner,0,recipe,new(key.Level,key.Craftsmanship,key.Control,key.Cp,key.Manipulation,key.Specialist),key.StartingQuality,
                new(key.Target,key.Steps,key.Width,key.Budget,key.ExtraWait,key.BuffExtraWait,key.Forbidden,key.Raphael));
            craftSearchViewKey=key;craftSolveRevision++;craftSolve.Cancel();
        }
        craftSolve.ValidateContext(key.Owner,craftSolveRevision,recipe.RecipeId);
    }
    // X-11 (0.419): whether the job (ClassJob row) has learned Manipulation; set by the plugin from the game's unlock state.
    internal Func<uint,bool>? ManipulationLearned { get; set; }
    // A-10 (0.422): Caraxi's Macro Chain is loaded (its « /nextmacro » ends the exported pages).
    internal Func<bool>? MacroChainLoaded { get; set; }
    private void DrawCraftSearch()
    {
        ValidateCraftSearch();
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftSearch.Title")+"###CraftSearch")) return;
        ImGui.TextWrapped(localization.Format("CraftSearchResults.Description",CraftActionCatalog.Specs.Count));
        ImGui.SetNextItemWidth(120); ImGui.InputInt(localization.Get("CraftSearch.Target")+"###CraftTarget",ref craftSolveTarget,0);
        // 0.330: the only range the search accepts (outside it, it failed with a generic message).
        craftSolveTarget=Math.Clamp(craftSolveTarget,0,simulationRecipe!.Simulation!.Quality); Tip("CraftSearch.TargetHelp"); ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("CraftSearch.Maximum"))) craftSolveTarget=simulationRecipe!.Simulation!.Quality;
        // A-01 (0.419): a collectable's turn-in thresholds as quality targets (game sheets, 0.409).
        if(Collectables?.Catalog?.For(simulationRecipe!.ResultItemId) is { Count:>0 } turnIns)
        {
            var rule=turnIns[0];var maximum=simulationRecipe!.Simulation!.Quality;
            foreach(var (labelKey,threshold) in new[]{("CraftSearch.CollectLow",rule.Low),("CraftSearch.CollectMid",rule.Mid),("CraftSearch.CollectHigh",rule.High)})
            {
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Format(labelKey,threshold)+"###"+labelKey))craftSolveTarget=Gathering.Collectables.QualityFor(threshold,maximum);
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("CraftSearch.CollectTip",threshold,Gathering.Collectables.QualityFor(threshold,maximum)));
            }
        }
        ImGui.TextDisabled(localization.Get("CraftSearch.TargetHelp"));
        ImGui.TextWrapped(localization.Get("CraftSearch.Priority"));
        DrawToolSettingsLink("CraftSearch");
        // A-03 (0.465, DEC-028): which solver; also in Settings › Modules › Crafting.
        ImGui.TextUnformatted(localization.Get("CraftSearch.Solver"));
        ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("CraftSearch.SolverTensho"),!CraftSearchOptions.Raphael) && CraftSearchOptions.Raphael){CraftSearchOptions.Raphael=false;SaveReminders();}
        Tip("CraftSearch.SolverTenshoTip");
        ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("CraftSearch.SolverRaphael"),CraftSearchOptions.Raphael) && !CraftSearchOptions.Raphael){CraftSearchOptions.Raphael=true;SaveReminders();}
        Tip("CraftSearch.SolverRaphaelTip");
        if(!CraftSearchOptions.Raphael)ImGui.TextDisabled(localization.Format("CraftSearch.Limits",craftSolveSteps,craftSolveWidth,craftSolveBudget));
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
            if(developerMode) ImGui.TextWrapped(RawMessage(error));
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
