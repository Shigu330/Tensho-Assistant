using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private readonly CraftLogObservationService craftLog=new();
    internal CraftLogObservationService CraftLog=>craftLog;
    private readonly CraftLogSession craftLogSession=new();
    private CraftLogInput? craftLogInput;
    private DateTimeOffset craftLogStableSince;
    private bool craftLogStarted;
    private bool craftLogForceRefresh;
    private string? craftLogOpenError;

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
            craftLog.RequestOpen(new(owner,recipe.RecipeId,recipe.ResultItemId));craftLogOpenError=null;
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
        var context=CraftLogContext.Resolve(observed,characterStats.State,recipeCatalog.State.Catalog,now);
        var actions=craftActions.State.Catalog;
        var presets=(IReadOnlyList<CraftPreset>?)configuration.CraftPresets ?? Array.Empty<CraftPreset>();
        CraftLogInput? input=context is {Recipe:{ } recipe,Stats:{ } stats} && actions is not null
            ? new(observed.Owner,recipe,stats,actions,presets,new(recipe.Simulation!.Quality,craftSolveSteps,craftSolveWidth,craftSolveBudget,craftMacroExtraWait),craftMacroExtraWait,craftMacroEcho,observed.Revision,context.StartingQuality,configuration.MacroCopy?.Chain==true):null;
        if(input!=craftLogInput)
        {craftLogSession.Cancel();craftLogInput=input;craftLogStableSince=now;craftLogStarted=false;craftLogForceRefresh=false;}
        if(now<craftLogStableSince)craftLogStableSince=now;
        if(input is not null && !craftLogStarted && now-craftLogStableSince>=TimeSpan.FromMilliseconds(350))
        {craftLogStarted=true;_=craftLogSession.StartAsync(input,craftLogForceRefresh);craftLogForceRefresh=false;}
        using var theme=TenshoAssistant.UI.FfxivTheme.Apply();
        var viewport=ImGui.GetMainViewport();var width=Math.Min(350,viewport.WorkSize.X);var height=Math.Min(590,viewport.WorkSize.Y);
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
            if(context.Stats is { } current)ImGui.TextUnformatted(localization.Format("CraftLogUi.Stats",current.Craftsmanship,current.Control,current.Cp,current.Level));
            if(context.Issue is { } issue){ImGui.TextWrapped(context.Recipe is { } linked && issue==$"Équipez le métier {linked.CraftType} pour calculer avec ses attributs." ? localization.Format("CraftMessages.Equip",linked.CraftType) : CraftMessage(issue));return;}
            if(actions is null){ImGui.TextWrapped(craftActions.State.Error ?? localization.Get("CraftLogUi.ActionsLoading"));return;}
            ImGui.TextWrapped(localization.Format("CraftLogUi.Assumptions",context.StartingQuality.ToString("N0"))
                +(context.Recipe?.Simulation?.Collectable==true ? localization.Get("CraftLogUi.CollectableHelp"):localization.Get("CraftLogUi.NoHq")));
            if(context.Recipe is { } forSimulator && ImGui.SmallButton(localization.Get("CraftLogUi.OpenSimulator"))){OpenCraftSimulation(forSimulator);IsOpen=true;}
            var state=craftLogSession.State;
            if(state.Loading || !craftLogStarted)ImGui.TextDisabled(localization.Get("CraftLogUi.Calculating"));
            if(state.Error is { } error)DrawDataError(error,"ServiceErrors.Craft");
            if(state.CalculatedAt is { } calculated)DrawCacheStamp(TenshoAssistant.Caching.CacheArea.CraftCalculations,calculated,state.FromCache);
            if(ImGui.SmallButton(localization.Get("CraftLogUi.Recalculate"))){craftLogSession.Cancel();craftLogStarted=false;craftLogStableSince=now;craftLogForceRefresh=true;}
            if(state.Result is not { } result)return;
            DrawCraftLogMacro(localization.Get("CraftLogUi.Saved"),result.Saved,actions);
            DrawCraftLogMacro(localization.Get("CraftLogUi.Suggested"),result.Suggested,actions);
            if(result.SearchIssue is { } searchIssue)ImGui.TextWrapped(CraftMessage(searchIssue));
            ImGui.TextWrapped(localization.Get("CraftLogUi.Objective"));
        }
        finally{ImGui.End();}
    }
    private void DrawCraftLogMacro(string title,CraftLogMacro? macro,CraftActionCatalog catalog)
    {
        ImGui.PushID(title);
        try
        {
            ImGui.Separator();ImGui.TextColored(Accent,title);
            if(macro is null){ImGui.TextDisabled(localization.Get("CraftLogUi.NoSequence"));return;}
            ImGui.TextWrapped(macro.Name=="Macro proposée" ? localization.Get("CraftLogUi.Suggested"):macro.Name);
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
            if(ImGui.TreeNode(localization.Get("CraftLogUi.Text")))
            {
                try{foreach(var block in macro.Export.Pages)foreach(var line in block.Lines)ImGui.TextUnformatted(line);}
                finally{ImGui.TreePop();}
            }
        }
        finally{ImGui.PopID();}
    }
}
