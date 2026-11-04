using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Panel beside the game's Crafting Log (setting CraftLogHelperEnabled, also turned on by the plan's « Simulate » button, which asks the
// game to open the log at the recipe): the selected recipe, its saved sequence and a suggested macro searched on a worker by
// Craft/CraftLogSession (350 ms after the selection settles; 0.327: only once asked when CraftLogAutoSolve is off; cached on disk for the
// craft calculations duration), text copy, copy into
// the game's macros and « Edit » in the simulator. The log is read by Craft/CraftLogReader on the framework thread.
internal sealed partial class MainWindow
{
    private readonly CraftLogObservationService craftLog=new();
    internal CraftLogObservationService CraftLog=>craftLog;
    // 0.467 (user request, in-game campaign): the crafting log follows the solver chosen in the settings.
    // 0.468: Raphael's sequences are kept (RaphaelSequenceCache), shared with the simulator's search.
    private static readonly RaphaelSequenceCache RaphaelSequences=new(RaphaelCraftSolver.Solve);
    private readonly CraftLogSession craftLogSession=new((input,token)=>CraftLogSession.Build(input,token,RaphaelSequences.Solve));
    private string? loggedCraftLogRaphaelError;
    private CraftLogInput? craftLogInput;
    private DateTimeOffset craftLogStableSince;
    private bool craftLogStarted;
    private bool craftLogForceRefresh;
    // 0.327 (CraftLogAutoSolve off): the character and recipe the player asked a search for; (0, 0) = none (a real one is never 0).
    private (ulong Owner,uint RecipeId) craftLogRequested;
    private string? craftLogOpenError;
    private readonly Dictionary<uint,bool> craftLogManipulation=[];
    private CraftDetected? craftLogDetected;
    private DateTimeOffset craftLogDetectedAt;
    private uint craftLogDetectedJob;
    private int craftLogTargetDraft;
    private bool craftLogTargetEditing;
    private string? craftLogSaveError;

    private void OpenCraftLog(RecipeDefinition recipe)
    {
        try
        {
            var owner=bankBridge.State.CharacterId;
            if(owner==0)throw new InvalidOperationException(localization.Get("CraftLogUi.Login"));
            if(!configuration.CraftLogHelperEnabled)
            {
                configuration.CraftLogHelperEnabled=true;
                try{save();}catch{configuration.CraftLogHelperEnabled=false;throw;}
            }
            craftLog.RequestOpen(new(owner,recipe.RecipeId,recipe.ResultItemId));craftLogRequested=(owner,recipe.RecipeId);craftLogOpenError=null;
        }
        catch(Exception error){craftLogOpenError=localization.Get("CraftLogUi.OpenError");log.Warning(error,"Could not request crafting log.");}
    }
    private void ClearCraftLogCalculation()
    {
        if(craftLogInput is not null || craftLogStarted)craftLogSession.Cancel();
        craftLogInput=null;craftLogStarted=false;
    }
    internal void DrawCraftLogHelper()
    {
        var now=DateTimeOffset.UtcNow;var observed=craftLog.Observation;
        if(windowDisposed || !configuration.CraftLogHelperEnabled || observed is null || observed.Revision!=craftLog.Revision || !observed.Fresh(now))
        {ClearCraftLogCalculation();return;}
        var job=observed.CurrentJobId;var manipulationDetected=ManipulationLearned?.Invoke(job)==true;
        if(now-craftLogDetectedAt>=TimeSpan.FromSeconds(1) || craftLogDetectedJob!=job){craftLogDetectedAt=now;craftLogDetectedJob=job;craftLogDetected=CraftDetectedNow?.Invoke(job);}
        var context=CraftLogContext.Resolve(observed,characterStats.State,recipeCatalog.State.Catalog,now,craftLogManipulation.TryGetValue(job,out var chosen) ? chosen:manipulationDetected,craftLogDetected?.Specialist==true);
        var actions=craftActions.State.Catalog;
        var presets=(IReadOnlyList<CraftPreset>?)configuration.CraftPresets ?? Array.Empty<CraftPreset>();
        CraftLogInput? input=context is {Recipe:{ } recipe,Stats:{ } stats} && actions is not null
            ? new(observed.Owner,recipe,stats,actions,presets,new(CraftLogTarget(recipe),craftSolveSteps,craftSolveWidth,craftSolveBudget,craftMacroExtraWait,craftMacroBuffWait,craftForbidden,CraftSearchOptions.Raphael),craftMacroExtraWait,craftMacroEcho,observed.Revision,context.StartingQuality,configuration.MacroCopy?.Chain==true,craftMacroBuffWait,MacroChainLoaded?.Invoke()==true):null;
        if(input!=craftLogInput)
        {craftLogSession.Cancel();craftLogInput=input;craftLogStableSince=now;craftLogStarted=false;craftLogForceRefresh=false;}
        if(now<craftLogStableSince)craftLogStableSince=now;
        if(CraftLogSession.ShouldStart(input,craftLogStarted,now-craftLogStableSince,configuration.CraftLogAutoSolve,craftLogRequested))
        {craftLogStarted=true;_=craftLogSession.StartAsync(input,craftLogForceRefresh);craftLogForceRefresh=false;}
        using var theme=TenshoAssistant.UI.FfxivTheme.Apply();
        var viewport=ImGui.GetMainViewport();var width=Math.Min(350,viewport.WorkSize.X);var height=Math.Min(680,viewport.WorkSize.Y);
        var position=viewport.Pos+observed.Position+new Vector2(observed.Size.X+6,0);
        if(position.X+width>viewport.WorkPos.X+viewport.WorkSize.X)position.X=viewport.Pos.X+observed.Position.X-width-6;
        position=Vector2.Clamp(position,viewport.WorkPos,Vector2.Max(viewport.WorkPos,viewport.WorkPos+viewport.WorkSize-new Vector2(width,height)));
        ImGui.SetNextWindowPos(position,ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(width,height),ImGuiCond.Always);
        var visible=ImGui.Begin(localization.Get("CraftLogUi.Title")+"###TenshoCraftLog",ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoResize);
        try
        {
            if(!visible)return;
            DrawToolSettingsLink("CraftLog");
            if(context.Recipe is { } selected)
            {
                var item=itemCatalog.State.Catalog?.Find(selected.ResultItemId);
                DrawItemIcon(item,new Vector2(36));ImGui.SameLine();ImGui.TextWrapped(item?.Name ?? localization.Format("CraftLogUi.Item",selected.ResultItemId));
                ImGui.TextColored(Accent,localization.Format("CraftLogUi.Recipe",selected.CraftType,selected.Level));
                if(selected.Simulation?.Collectable==true)ImGui.TextColored(Accent,localization.Get("CraftLogUi.Collectable"));
                if(selected.Simulation is { } parameters)ImGui.TextUnformatted(localization.Format("CraftLogUi.Parameters",parameters.Progress.ToString("N0"),parameters.Quality.ToString("N0"),parameters.Durability));
            }
            if(context.Stats is { } current)
            {
                ImGui.TextUnformatted(localization.Format("CraftLogUi.Stats",current.Craftsmanship,current.Control,current.Cp,current.Level));
                DrawCraftLogState(job,manipulationDetected,current.ManipulationAllowed);
            }
            if(context.Issue is { } issue)
            {
                var wrongJob=context.Recipe is { } linked && issue==$"Equip the {linked.CraftType} job to calculate with its attributes.";
                ImGui.TextWrapped(wrongJob ? localization.Format("CraftMessages.Equip",context.Recipe!.CraftType) : CraftMessage(issue));
                // 0.438 (user request): the recipe's job, worn from here on click.
                if(wrongJob)DrawEquipRecipeJob(context.Recipe!,"CraftLogEquip");
                return;
            }
            if(actions is null){if(craftActions.State.Error is { } failure)DrawDataError(failure,"ServiceErrors.Craft");else ImGui.TextWrapped(localization.Get("CraftLogUi.ActionsLoading"));return;}
            ImGui.TextWrapped(localization.Format("CraftLogUi.Assumptions",context.StartingQuality.ToString("N0"))
                +(context.Recipe?.Simulation?.Collectable==true ? localization.Get("CraftLogUi.CollectableHelp"):localization.Get("CraftLogUi.NoHq")));
            if(context.Recipe is { } forSimulator && ImGui.SmallButton(localization.Get("CraftLogUi.OpenSimulator"))){OpenCraftSimulation(forSimulator);IsOpen=true;}
            if(context.Recipe is {Simulation.Quality:>0} aimed)DrawCraftLogTarget(aimed);
            var state=craftLogSession.State;
            if(state.Result is { } solved)
            {
                if(solved.RaphaelError is { } raphaelError)
                {
                    if(loggedCraftLogRaphaelError!=raphaelError){loggedCraftLogRaphaelError=raphaelError;log.Warning("Raphael failed for the crafting log, Tensho's search was used: {Reason}",raphaelError);}
                    ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("CraftLogUi.RaphaelFailed"));
                }
                else if(solved.UsedRaphael)ImGui.TextDisabled(localization.Get(solved.RaphaelCached ? "CraftLogUi.ByRaphaelKept":"CraftLogUi.ByRaphael"));
                if(solved.SavedIsEnough)ImGui.TextWrapped(localization.Get("CraftLogUi.SavedIsEnough"));
            }
            // 0.327: with « compute automatically » off, the search waits for this button (« Recalculate » and the plan's « Simulate » also ask).
            var waitsForRequest=!craftLogStarted && CraftLogSession.WaitsForRequest(input,configuration.CraftLogAutoSolve,craftLogRequested);
            if(waitsForRequest)
            {
                if(ImGui.SmallButton(localization.Get("CraftLogUi.Compute")) && input is not null)craftLogRequested=(input.Owner,input.Recipe.RecipeId);
                Tip("CraftLogUi.ComputeTip");
            }
            else if(state.Loading || !craftLogStarted)ImGui.TextDisabled(localization.Get("CraftLogUi.Calculating"));
            if(state.Error is { } error)DrawDataError(error,"ServiceErrors.Craft");
            if(state.CalculatedAt is { } calculated)DrawCacheStamp(TenshoAssistant.Caching.CacheArea.CraftCalculations,calculated,state.FromCache);
            if(!waitsForRequest && ImGui.SmallButton(localization.Get("CraftLogUi.Recalculate"))){craftLogSession.Cancel();craftLogStarted=false;craftLogStableSince=now;craftLogForceRefresh=true;if(input is not null)craftLogRequested=(input.Owner,input.Recipe.RecipeId);}
            if(state.Result is not { } result)return;
            DrawCraftLogMacro(localization.Get("CraftLogUi.Saved"),result.Saved,actions);
            if(!result.SavedIsEnough)DrawCraftLogMacro(localization.Get("CraftLogUi.Suggested"),result.Suggested,actions,true);
            if(result.SearchIssue is { } searchIssue)ImGui.TextWrapped(CraftMessage(searchIssue));
            ImGui.TextWrapped(localization.Get("CraftLogUi.Objective"));
        }
        finally{ImGui.End();}
    }
    private void DrawCraftLogMacro(string title,CraftLogMacro? macro,CraftActionCatalog catalog,bool canSave=false)
    {
        ImGui.PushID(title);
        try
        {
            ImGui.Separator();ImGui.TextColored(Accent,title);
            if(macro is null){ImGui.TextDisabled(localization.Get("CraftLogUi.NoSequence"));return;}
            ImGui.TextWrapped(macro.Name=="Suggested macro" ? localization.Get("CraftLogUi.Suggested"):macro.Name);
            var simulation=macro.Simulation;var maximum=craftLogInput!.Recipe.Simulation!.Quality;
            ImGui.TextWrapped(localization.Format("CraftLogUi.Summary",simulation.Final.Quality.ToString("N0"),maximum.ToString("N0"),macro.Export.Actions,macro.Export.WaitSeconds));
            var job=craftLogInput.Recipe.CraftTypeId+8;var steps=simulation.Steps;
            for(var index=0;index<Math.Min(30,steps.Count);index++)
            {
                if(index%10!=0)ImGui.SameLine(0,3);
                var metadata=catalog.Find(job,steps[index].Action);
                DrawItemIcon(metadata is null ? null:new ItemDefinition(0,metadata.Name,false,metadata.Icon),new Vector2(25));
                if(ImGui.IsItemHovered())WrappedTooltip($"{index+1}. {metadata?.Name ?? steps[index].Action.ToString()}");
            }
            if(steps.Count>30)ImGui.TextDisabled(localization.Format("CraftLogUi.More",steps.Count-30));
            foreach(var block in macro.Export.Pages)if(ImGui.SmallButton(localization.Format("CraftLogUi.Copy",block.Number,macro.Export.Pages.Count)))ImGui.SetClipboardText(block.Text);
            DrawMacroToGame(macro.Export.Pages,true);
            if(ImGui.SmallButton(localization.Get("CraftLogUi.Edit")))
            {
                var source=craftLogInput;
                OpenCraftSimulation(source.Recipe);simulationActions.Clear();simulationActions.AddRange(simulation.Steps.Select(step=>step.Action));
                simulationLevel=source.Stats.Level;simulationCs=source.Stats.Craftsmanship;simulationControl=source.Stats.Control;
                simulationCp=source.Stats.Cp;simulationManipulation=source.Stats.ManipulationAllowed;simulationQuality=source.StartingQuality;
                simulationCopiedOwner=source.Owner;simulationCopiedAt=characterStats.State.ObservedAt;
                simulationResult=simulation;simulationError=null;macroExportSimulation=simulation;craftMacroExport=macro.Export;craftMacroError=null;
                craftSolve.Cancel();IsOpen=true;
            }
            if(canSave)DrawCraftLogSave(craftLogInput.Recipe,macro);
            if(ImGui.TreeNode(localization.Get("CraftLogUi.Text")))
            {
                try{foreach(var block in macro.Export.Pages)foreach(var line in block.Lines)ImGui.TextUnformatted(line);}
                finally{ImGui.TreePop();}
            }
        }
        finally{ImGui.PopID();}
    }
    // 0.468 (user request, in-game campaign): what the attributes of the panel's calculation already hold (food, medicine, specialist, read from the game) and whether Manipulation is used;
    // the box corrects it for this session if the detection is wrong.
    private void DrawCraftLogState(uint job,bool detected,bool used)
    {
        var list=CraftConsumableList?.Invoke();
        ImGui.TextWrapped(localization.Format("CraftLogUi.State",CraftConsumableName(CraftConsumables.FromStatus(list,craftLogDetected?.Food,false),craftLogDetected?.Food),
            CraftConsumableName(CraftConsumables.FromStatus(list,craftLogDetected?.Medicine,true),craftLogDetected?.Medicine),CraftYesNo(craftLogDetected?.Specialist)));
        Tip("CraftLogUi.StateTip");
        var manipulation=used;
        if(ImGui.Checkbox(localization.Get("CraftLogUi.Manipulation")+"###CraftLogManipulation",ref manipulation))
        {
            if(manipulation==detected)craftLogManipulation.Remove(job);
            else craftLogManipulation[job]=manipulation;
        }
        Tip("CraftLogUi.ManipulationTip");
        ImGui.SameLine();ImGui.TextDisabled(localization.Format("CraftLogUi.ManipulationDetected",CraftYesNo(detected)));
    }
    // 0.468 (user request, in-game campaign): the target of the panel's calculation, kept by recipe; applied once the field is left, so typing does not start a search for each digit.
    private int CraftLogTarget(RecipeDefinition recipe)
    {
        var maximum=recipe.Simulation!.Quality;
        return configuration.CraftLogTargets?.TryGetValue(recipe.RecipeId,out var target)==true ? Math.Clamp(target,0,maximum):maximum;
    }
    private void SetCraftLogTarget(RecipeDefinition recipe,int value)
    {
        var maximum=recipe.Simulation!.Quality;var target=Math.Clamp(value,0,maximum);
        configuration.CraftLogTargets??=[];
        if(target==maximum)configuration.CraftLogTargets.Remove(recipe.RecipeId);
        else configuration.CraftLogTargets[recipe.RecipeId]=target;
        craftLogTargetDraft=target;SaveReminders();
    }
    private void DrawCraftLogTarget(RecipeDefinition recipe)
    {
        var target=CraftLogTarget(recipe);var maximum=recipe.Simulation!.Quality;
        if(!craftLogTargetEditing)craftLogTargetDraft=target;
        ImGui.SetNextItemWidth(90);
        ImGui.InputInt(localization.Get("CraftSearch.Target")+"###CraftLogTarget",ref craftLogTargetDraft,0);
        craftLogTargetEditing=ImGui.IsItemActive();
        Tip("CraftLogUi.TargetTip");
        if(ImGui.IsItemDeactivatedAfterEdit())SetCraftLogTarget(recipe,craftLogTargetDraft);
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("CraftLogUi.TargetMaximum")))SetCraftLogTarget(recipe,maximum);
        if(Collectables?.Catalog?.For(recipe.ResultItemId) is not { Count:>0 } turnIns)return;
        var rule=turnIns[0];var first=true;
        foreach(var (labelKey,threshold) in new[]{("CraftSearch.CollectLow",rule.Low),("CraftSearch.CollectMid",rule.Mid),("CraftSearch.CollectHigh",rule.High)})
        {
            if(!first)ImGui.SameLine();
            first=false;
            var quality=Gathering.Collectables.QualityFor(threshold,maximum);
            if(ImGui.SmallButton(localization.Format(labelKey,threshold)+"###CraftLog"+labelKey))SetCraftLogTarget(recipe,quality);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("CraftSearch.CollectTip",threshold,quality));
        }
    }
    // 0.468 (user request, in-game campaign): the suggestion kept as the recipe's best sequence; from then on, while it reaches the target, the panel shows it and computes nothing.
    private void DrawCraftLogSave(RecipeDefinition recipe,CraftLogMacro macro)
    {
        var sequence=macro.Simulation.Steps.Select(step=>step.Action).ToArray();
        var saved=(IReadOnlyList<CraftPreset>?)configuration.CraftPresets ?? [];
        if(saved.Any(row=>row is not null && row.RecipeId==recipe.RecipeId && row.Actions.SequenceEqual(sequence))){ImGui.TextDisabled(localization.Get("CraftLogUi.AlreadySaved"));return;}
        if(ImGui.SmallButton(localization.Get("CraftLogUi.SaveBest")))
        {
            try
            {
                var name=CraftPresets.UniqueName(saved,recipe.RecipeId,localization.Format("CraftLogUi.BestName",macro.Simulation.Final.Quality));
                CraftPresets.Commit(configuration,save,source=>CraftPresets.Add(source,CraftPresets.Create(recipe,name,sequence)));
                craftLogSaveError=null;
            }
            catch(Exception error){craftLogSaveError=localization.Get("CraftLogUi.SaveBestError");log.Warning(error,"Could not save the crafting log's sequence.");}
        }
        Tip("CraftLogUi.SaveBestTip");
        if(craftLogSaveError is { } problem)ImGui.TextColored(UI.FfxivTheme.Warning,problem);
    }
}
