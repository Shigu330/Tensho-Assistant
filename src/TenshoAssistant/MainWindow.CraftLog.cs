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
    internal const string RaphaelNotCalled="Raphael not called:";
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
        if(!windowDisposed && configuration.CraftLogHelperEnabled && craftLog.Error is not null)DrawReadFailedPanel("TenshoCraftLogFailed","RecipeNote","CraftLog");
        var now=DateTimeOffset.UtcNow;var observed=craftLog.Observation;
        if(windowDisposed || !configuration.CraftLogHelperEnabled || observed is null || observed.Revision!=craftLog.Revision || !observed.Fresh(now))
        {ClearCraftLogCalculation();return;}
        var job=observed.CurrentJobId;var manipulationDetected=ManipulationLearned?.Invoke(job)==true;
        if(now-craftLogDetectedAt>=TimeSpan.FromSeconds(1) || craftLogDetectedJob!=job){craftLogDetectedAt=now;craftLogDetectedJob=job;craftLogDetected=CraftDetectedNow?.Invoke(job);}
        var context=CraftLogContext.Resolve(observed,characterStats.State,recipeCatalog.State.Catalog,now,craftLogManipulation.TryGetValue(job,out var chosen) ? chosen:manipulationDetected,craftLogDetected?.Specialist==true,job=>RememberedCraftStats(observed.Owner,job));
        var actions=craftActions.State.Catalog;
        var presets=(IReadOnlyList<CraftPreset>?)configuration.CraftPresets ?? Array.Empty<CraftPreset>();
        CraftLogInput? input=context is {Recipe:{ } recipe,Stats:{ } stats} && actions is not null
            ? new(observed.Owner,recipe,stats,actions,presets,new(CraftLogTarget(recipe),craftSolveSteps,craftSolveWidth,craftSolveBudget,craftMacroExtraWait,craftMacroBuffWait,craftForbidden,CraftSearchOptions.Raphael),craftMacroExtraWait,craftMacroEcho,observed.Revision,context.StartingQuality,configuration.MacroCopy?.Chain==true,craftMacroBuffWait,MacroChainLoaded?.Invoke()==true,TryAllPresets:configuration.CraftLogTryAllPresets,EchoSound:CraftSearchOptions.BoundedEchoSound,EchoLastOnly:CraftSearchOptions.EchoLastOnly):null;
        if(input!=craftLogInput)
        {craftLogSession.Cancel();craftLogInput=input;craftLogStableSince=now;craftLogStarted=false;craftLogForceRefresh=false;}
        if(now<craftLogStableSince)craftLogStableSince=now;
        if(CraftLogSession.ShouldStart(input,craftLogStarted,now-craftLogStableSince,configuration.CraftLogAutoSolve,craftLogRequested))
        {craftLogStarted=true;_=craftLogSession.StartAsync(input,craftLogForceRefresh);craftLogForceRefresh=false;}
        var viewport=ImGui.GetMainViewport();var width=Math.Min(380,viewport.WorkSize.X);var height=Math.Min(720,viewport.WorkSize.Y);
        var logAt=observed.Position+WindowMoved("RecipeNote",observed.Position.X,observed.Position.Y);
        var position=viewport.Pos+logAt+new Vector2(observed.Size.X+6,0);
        if(position.X+width>viewport.WorkPos.X+viewport.WorkSize.X)position.X=viewport.Pos.X+logAt.X-width-6;
        position=Vector2.Clamp(position,viewport.WorkPos,Vector2.Max(viewport.WorkPos,viewport.WorkPos+viewport.WorkSize-new Vector2(width,height)));
        Panels?.Show("TenshoCraftLog",localization.Get("CraftLogUi.Title"),ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoResize,()=>{ImGui.SetNextWindowPos(position,ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(width,height),ImGuiCond.Always);},()=>
        {
            // 0.469 (user request, in-game campaign): the panel in sections (recipe, your state, target, result), figures in a table, explanations in the tooltips of « (?) ».
            if(context.Recipe is { } selected){DrawCraftLogTitle(selected);DrawRecipeFacts(selected);}
            DrawToolSettingsLink("CraftLog");
            if(context.Recipe is { } forSimulator)
            {
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("CraftLogUi.OpenSimulator"))){OpenCraftSimulation(forSimulator);IsOpen=true;}
            }
            DrawCraftLogFigures(context.Recipe,context.Stats);
            // 0.788 (X-29, user report: as Carpenter on a Blacksmith recipe, « not detected »: the note was cut off and in the information colour): both jobs named,
            // in the warning colour, wrapped, with the button that wears the recipe's job.
            if(context.RememberedAt is { } rememberedAt)
            {
                ImGui.PushStyleColor(ImGuiCol.Text,UI.FfxivTheme.Warning);
                try{ImGui.TextWrapped(localization.Format("CraftLogUi.Remembered",JobName(job),context.Recipe?.CraftType ?? "",rememberedAt.ToLocalTime().ToString("g")));}
                finally{ImGui.PopStyleColor();}
                if(context.Recipe is { } worn)DrawEquipRecipeJob(worn,"CraftLogEquipRemembered");
            }
            if(context.Stats is { } current)
            {
                CraftLogSection(localization.Get("CraftLogUi.SectionState"));
                DrawCraftLogState(job,manipulationDetected,current.ManipulationAllowed);
            }
            if(context.Issue is { } issue)
            {
                var wrongJob=context.Recipe is { } linked && issue==$"Equip the {linked.CraftType} job to calculate with its attributes.";
                ImGui.Spacing();
                ImGui.TextWrapped(wrongJob ? localization.Format("CraftMessages.Equip",context.Recipe!.CraftType) : CraftMessage(issue));
                // 0.438 (user request): the recipe's job, worn from here on click.
                if(wrongJob)DrawEquipRecipeJob(context.Recipe!,"CraftLogEquip");
                return;
            }
            if(actions is null){if(craftActions.State.Error is { } failure)DrawDataError(failure,"ServiceErrors.Craft");else ImGui.TextWrapped(localization.Get("CraftLogUi.ActionsLoading"));return;}
            if(context.Recipe is {Simulation.Quality:>0} aimed)
            {
                CraftLogSection(localization.Get("CraftLogUi.SectionTarget"));
                DrawCraftLogTarget(aimed);
            }
            CraftLogSection(localization.Get("CraftLogUi.SectionResult"),localization.Format("CraftLogUi.Assumptions",context.StartingQuality.ToString("N0"))
                +(context.Recipe?.Simulation?.Collectable==true ? localization.Get("CraftLogUi.CollectableHelp"):localization.Get("CraftLogUi.NoHq"))+"\n"+localization.Get("CraftLogUi.Objective"));
            var state=craftLogSession.State;
            if(state.Result is { } solved)
            {
                // 0.783 (X-25): Raphael not called on purpose (nothing it can take, such as a recipe with no quality to seek) is not a failure.
                if(solved.RaphaelError is { } skipped && skipped.StartsWith(RaphaelNotCalled,StringComparison.Ordinal))
                    ImGui.TextDisabled(localization.Get("CraftLogUi.RaphaelNotNeeded"));
                else if(solved.RaphaelError is { } raphaelError)
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
            if(state.CalculatedAt is { } calculated)
            {
                ImGui.PushTextWrapPos(0);
                try{DrawCacheStamp(TenshoAssistant.Caching.CacheArea.CraftCalculations,calculated,state.FromCache);}
                finally{ImGui.PopTextWrapPos();}
            }
            if(!waitsForRequest && ImGui.SmallButton(localization.Get("CraftLogUi.Recalculate"))){craftLogSession.Cancel();craftLogStarted=false;craftLogStableSince=now;craftLogForceRefresh=true;if(input is not null)craftLogRequested=(input.Owner,input.Recipe.RecipeId);}
            if(state.Result is not { } result)return;
            if(result.Saved is not null)DrawCraftLogMacro(localization.Get("CraftLogUi.Saved"),result.Saved,actions);
            if(!result.SavedIsEnough)DrawCraftLogMacro(localization.Get("CraftLogUi.Suggested"),result.Suggested,actions,true);
            if(result.SearchIssue is { } searchIssue)ImGui.TextWrapped(CraftMessage(searchIssue));
        });
    }
    private CraftJobStats? RememberedCraftStats(ulong owner,uint job)=>configuration.LastCraftStats?.GetValueOrDefault(owner)?.GetValueOrDefault(job);
    /// <summary>0.554 (C3): each frame; only what changed is written, so a food or a piece of gear makes one save.</summary>
    internal void KeepCraftStats()
    {
        var state=characterStats.State;
        if(state.CharacterId==0 || state.JobId is <8 or >15 || state.Level<1 || state.ObservedAt is not { } seen
            || !state.Values.TryGetValue("Craftsmanship",out var craftsmanship) || !state.Values.TryGetValue("Control",out var control) || !state.Values.TryGetValue("CraftingPoints",out var cp))return;
        var all=configuration.LastCraftStats??=[];
        if(!all.TryGetValue(state.CharacterId,out var jobs))all[state.CharacterId]=jobs=[];
        if(jobs.TryGetValue(state.JobId,out var known) && known.Level==state.Level && known.Craftsmanship==craftsmanship && known.Control==control && known.Cp==cp)return;
        jobs[state.JobId]=new(state.Level,craftsmanship,control,cp,seen);
        try{save();}
        catch(Exception error){log.Warning(error,"Could not save the crafting attributes.");}
    }
    private void DrawCraftLogMacro(string title,CraftLogMacro? macro,CraftActionCatalog catalog,bool canSave=false)
    {
        if(macro is {SavedForItem:not 0})ImGui.TextDisabled(localization.Format("CraftLogUi.SavedFor",itemCatalog.State.Catalog?.Find(macro.SavedForItem)?.Name ?? macro.SavedForItem.ToString()));
        ImGui.PushID(title);
        try
        {
            CraftLogSection(title);
            if(macro is null){ImGui.TextDisabled(localization.Get("CraftLogUi.NoSequence"));return;}
            if(macro.Name!="Suggested macro")ImGui.TextWrapped(macro.Name);
            var simulation=macro.Simulation;var maximum=craftLogInput!.Recipe.Simulation!.Quality;var target=craftLogInput.Options.TargetQuality;var quality=simulation.Final.Quality;
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram,quality>=target ? UI.FfxivTheme.Success:UI.FfxivTheme.Warning);
            try{ImGui.ProgressBar(maximum>0 ? Math.Clamp((float)quality/maximum,0,1):1,new Vector2(-1,ImGui.GetFrameHeight()),$"{quality:N0} / {maximum:N0}");}
            finally{ImGui.PopStyleColor();}
            ImGui.TextUnformatted(localization.Format("CraftLogUi.ResultLine",macro.Export.Actions,macro.Export.WaitSeconds)+(target<maximum ? localization.Format("CraftLogUi.ResultTarget",target.ToString("N0")):""));
            var job=craftLogInput.Recipe.CraftTypeId+8;var steps=simulation.Steps;
            const float iconSize=28,iconGap=3;
            var perRow=Math.Max(1,(int)((ImGui.GetContentRegionAvail().X+iconGap)/(iconSize+iconGap)));
            for(var index=0;index<Math.Min(30,steps.Count);index++)
            {
                if(index%perRow!=0)ImGui.SameLine(0,iconGap);
                var metadata=catalog.Find(job,steps[index].Action);
                DrawItemIcon(metadata is null ? null:new ItemDefinition(0,metadata.Name,false,metadata.Icon),new Vector2(iconSize));
                if(ImGui.IsItemHovered())WrappedTooltip($"{index+1}. {metadata?.Name ?? steps[index].Action.ToString()}");
            }
            if(steps.Count>30)ImGui.TextDisabled(localization.Format("CraftLogUi.More",steps.Count-30));
            ImGui.Spacing();
            for(var page=0;page<macro.Export.Pages.Count;page++)
            {
                var block=macro.Export.Pages[page];
                if(page%3!=0)ImGui.SameLine();
                if(ImGui.SmallButton(localization.Format("CraftLogUi.Copy",block.Number,macro.Export.Pages.Count)))ImGui.SetClipboardText(block.Text);
            }
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
    private void CraftLogSection(string title,string? help=null)
    {
        ImGui.Spacing();
        using(TenshoAssistant.UI.UiComponents.PushSectionFont())ImGui.TextColored(Accent,title);
        if(help is not null)
        {
            ImGui.SameLine();HelpMarkerText(help);
        }
        ImGui.Separator();
    }
    private void DrawCraftLogTitle(RecipeDefinition recipe)
    {
        var item=itemCatalog.State.Catalog?.Find(recipe.ResultItemId);
        DrawItemIcon(item,new Vector2(40));
        if(ImGui.IsItemHovered())DrawItemInfoTooltip(recipe.ResultItemId);
        ImGui.SameLine();
        ImGui.BeginGroup();
        try
        {
            using(TenshoAssistant.UI.UiComponents.PushSectionFont())ImGui.TextWrapped(item?.Name ?? localization.Format("CraftLogUi.Item",recipe.ResultItemId));
            ImGui.TextColored(Accent,recipe.Simulation?.Collectable==true ? $"{recipe.CraftType} · {localization.Get("CraftLogUi.Collectable")}":recipe.CraftType);
        }
        finally{ImGui.EndGroup();}
    }
    private void DrawCraftLogFigures(RecipeDefinition? recipe,CraftStats? stats)
    {
        if(recipe?.Simulation is null && stats is null)return;
        ImGui.Spacing();
        if(!ImGui.BeginTable("###CraftLogFigures",4,ImGuiTableFlags.SizingStretchSame))return;
        try
        {
            void Cell(string key,int value){ImGui.TableNextColumn();ImGui.TextDisabled(localization.Get(key));ImGui.TextUnformatted(value.ToString("N0"));}
            if(recipe?.Simulation is { } parameters)
            {
                Cell("CraftLogUi.FigureProgress",parameters.Progress);Cell("CraftLogUi.FigureQuality",parameters.Quality);
                Cell("CraftLogUi.FigureDurability",parameters.Durability);Cell("CraftLogUi.FigureRecipeLevel",recipe.Level);
            }
            if(stats is not null)
            {
                Cell("CraftLogUi.FigureCraftsmanship",stats.Craftsmanship);Cell("CraftLogUi.FigureControl",stats.Control);
                Cell("CraftLogUi.FigureCp",stats.Cp);Cell("CraftLogUi.FigureLevel",stats.Level);
            }
        }
        finally{ImGui.EndTable();}
    }
    // 0.468 (user request, in-game campaign): what the attributes of the panel's calculation already hold (food, medicine, specialist, read from the game) and whether Manipulation is used;
    // the box corrects it for this session if the detection is wrong.
    private void DrawCraftLogState(uint job,bool detected,bool used)
    {
        DrawDetectedConsumables(craftLogDetected);
        ImGui.SameLine();ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(localization.Format("CraftLogUi.Specialist",CraftYesNo(craftLogDetected?.Specialist)));
        var manipulation=used;
        ImGui.AlignTextToFramePadding();
        if(ImGui.Checkbox("###CraftLogManipulation",ref manipulation))
        {
            if(manipulation==detected)craftLogManipulation.Remove(job);
            else craftLogManipulation[job]=manipulation;
        }
        Tip("CraftLogUi.ManipulationTip");
        // 0.469 (user request): the skill's icon instead of its name; what was detected is in its tooltip, and beside it only when the box says otherwise.
        ImGui.SameLine();
        var skill=craftActions.State.Catalog?.Find(job,CraftActionKind.Manipulation);
        DrawItemIcon(skill is null ? null:new ItemDefinition(0,skill.Name,false,skill.Icon),new Vector2(28));
        if(ImGui.IsItemHovered())WrappedTooltip((skill?.Name ?? CraftActionKind.Manipulation.ToString())+"\n"+localization.Format("CraftLogUi.ManipulationDetected",CraftYesNo(detected))+"\n"+localization.Get("CraftLogUi.ManipulationTip"));
        if(used!=detected){ImGui.SameLine();ImGui.TextDisabled(localization.Format("CraftLogUi.ManipulationDetected",CraftYesNo(detected)));}
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
