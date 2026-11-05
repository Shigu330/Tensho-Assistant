using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Live craft › next action: projects one step ahead, under the observed condition, every action the client marks usable (cooldowns
// excluded), with ranges where combo or unique-action history is unknown (Craft/CraftLiveProjection.cs); sortable. Recomputed only for a
// new observed frame or action catalogue; no rotation and nothing executed.
internal sealed partial class MainWindow
{
    private CraftLiveFrame? projectionFrame;private CraftActionCatalog? projectionCatalog;private CraftProjectionResult? projectionResult;
    private uint projectionRecipe;private int projectionTarget,projectionSort;
    private CraftProjectionResult? sortedProjectionSource;
    private int sortedProjectionOrder=-1;
    private string sortedProjectionForbidden="";
    private CraftActionProjection[] sortedProjectionRows=[];
    private CraftStepCondition? loggedProjectionCondition;
    // A-04 (DEC-028, 0.509): a suggested move, worked out off the game's thread for each new state of the synthesis. Display only.
    private readonly object suggestGate=new();
    private string suggestKey="";
    private CancellationTokenSource? suggestCancel;
    private CraftSuggestion? suggestion;
    private bool suggestLoading;
    private void UpdateCraftSuggestion(CraftProjectionResult result,string forbidden)
    {
        if(!configuration.CraftLiveSuggest || result.Suggest is not { } input || windowDisposed)
        {
            lock(suggestGate){suggestCancel?.Cancel();suggestCancel?.Dispose();suggestCancel=null;suggestKey="";suggestion=null;suggestLoading=false;}
            return;
        }
        var target=projectionTarget;
        var key=$"{input.State}|{input.Condition}|{input.GoodPercent}|{target}|{forbidden}|{string.Join(',',input.Available)}";
        lock(suggestGate)
        {
            if(key==suggestKey)return;
            suggestCancel?.Cancel();suggestCancel?.Dispose();
            var source=new CancellationTokenSource();
            suggestCancel=source;suggestKey=key;suggestion=null;suggestLoading=true;
            var token=source.Token;
            _=Task.Run(()=>
            {
                CraftSuggestion? found=null;
                try{found=CraftSolver.Suggest(input,target,forbidden,token);}
                catch(OperationCanceledException){return;}
                catch(Exception error){log.Warning(error,"The next move could not be worked out.");}
                lock(suggestGate)if(suggestKey==key && !token.IsCancellationRequested){suggestion=found;suggestLoading=false;}
            },token);
        }
    }
    private void DrawCraftSuggestion(CraftProjectionResult result,CraftActionCatalog catalog,uint job)
    {
        if(!configuration.CraftLiveSuggest || result.Suggest is null)return;
        CraftSuggestion? found;bool loading;
        lock(suggestGate){found=suggestion;loading=suggestLoading;}
        if(loading){ImGui.TextDisabled(localization.Get("CraftProjectionUi.SuggestLoading"));return;}
        if(found is null){ImGui.TextDisabled(localization.Get("CraftProjectionUi.SuggestNone"));return;}
        string Name(CraftActionKind kind)=>catalog.Find(job,kind)?.Name ?? kind.ToString();
        ImGui.PushStyleColor(ImGuiCol.Text,found.TargetReached ? Success:Accent);
        try{ImGui.TextWrapped(localization.Format(found.TargetReached ? "CraftProjectionUi.SuggestReached":"CraftProjectionUi.SuggestBelow",Name(found.Move),found.FinalQuality,projectionTarget));}
        finally{ImGui.PopStyleColor();}
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("CraftProjectionUi.SuggestTip")+(found.Then.Count>0 ? "\n"+string.Join(" › ",found.Then.Take(8).Select(Name))+(found.Then.Count>8 ? " …":""):""));
    }
    // 0.497 (user report): the state was used in the numbers but never shown: the window now says it, what it does, and which actions it opens.
    private void DrawProjectionCondition(CraftProjectionResult result,string shownName)
    {
        if(result.Condition is not { } condition)return;
        var effect=condition switch
        {
            CraftStepCondition.Good=>result.GoodPercent is { } percent ? localization.Format("CraftProjectionUi.ConditionGood",(percent/100m).ToString("0.##")):localization.Get("CraftProjectionUi.ConditionGoodUnknown"),
            CraftStepCondition.Excellent=>localization.Get("CraftProjectionUi.ConditionExcellent"),
            CraftStepCondition.Poor=>localization.Get("CraftProjectionUi.ConditionPoor"),
            _=>localization.Get("CraftProjectionUi.ConditionNormal"),
        };
        var color=condition is CraftStepCondition.Good or CraftStepCondition.Excellent ? Success:condition==CraftStepCondition.Poor ? Accent:Muted;
        ImGui.PushStyleColor(ImGuiCol.Text,color);
        try{ImGui.TextWrapped(localization.Format("CraftProjectionUi.ConditionLine",shownName,effect));}
        finally{ImGui.PopStyleColor();}
    }
    private void DrawCraftProjection(CraftLiveState state)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftProjectionUi.Title"),ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextWrapped(localization.Get("CraftProjectionUi.Help"));
        DrawCraftProjectionBody(state);
    }

    /// <summary>
    /// 0.464 (user request, in-game campaign): the projection beside the game's Synthesis window, so it is seen while crafting; off with its setting. Nothing is executed.
    /// </summary>
    internal void DrawCraftLivePanel()
    {
        if(windowDisposed || !configuration.CraftLivePanel)return;
        var state=craftLive.State;
        if(state.Frame is null)return;
        var addon=gameGui.GetAddonByName("Synthesis");
        if(addon.IsNull || !addon.IsVisible)return;
        var viewport=ImGui.GetMainViewport();var width=Math.Min(560,viewport.WorkSize.X);var height=Math.Min(craftFollowSources.Length>0 && configuration.CraftLiveFollow ? 580:460,viewport.WorkSize.Y);
        var position=viewport.Pos+addon.Position+new Vector2(addon.ScaledWidth+6,0);
        if(position.X+width>viewport.WorkPos.X+viewport.WorkSize.X)position.X=viewport.Pos.X+addon.Position.X-width-6;
        position=Vector2.Clamp(position,viewport.WorkPos,Vector2.Max(viewport.WorkPos,viewport.WorkPos+viewport.WorkSize-new Vector2(width,height)));
        // 0.468 (user request, in-game campaign): its cross turns its setting off.
        Panels?.Show("TenshoCraftLivePanel",localization.Get("CraftProjectionUi.Title"),ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize,()=>{ImGui.SetNextWindowPos(position,ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(width,height),ImGuiCond.Always);},()=>DrawCraftProjectionBody(state),()=>{configuration.CraftLivePanel=false;craftLive.SetEnabled(CraftLiveWanted(configuration));SaveReminders();});
    }

    private void DrawCraftProjectionBody(CraftLiveState state)
    {
        if(state.Frame is not { } frame || craftActions.State.Catalog is not { } catalog)return;
        var now=DateTimeOffset.UtcNow;
        if(!ReferenceEquals(projectionFrame,frame) || !ReferenceEquals(projectionCatalog,catalog))
        {
            projectionFrame=frame;projectionCatalog=catalog;projectionResult=CraftLiveProjection.Build(state,catalog,now);
            // 0.497 (user report): what the game answers for the actions that depend on the state, once per state, for the evidence.
            if(DeveloperMode && projectionResult?.Condition is { } seen && seen!=loggedProjectionCondition)
            {
                loggedProjectionCondition=seen;
                var kinds=frame.Raw.ActionsNative?.Where(action=>CraftSimulation.NeedsFavourable(action.Kind)).Select(action=>$"{action.Kind}={action.Status}") ?? [];
                log.Information("Craft condition {0}: game status {1}; {2} actions offered.",seen,string.Join(", ",kinds),projectionResult.Actions.Count);
            }
        }
        if(state.ObservedAt is not { } date || now<date || now-date>TimeSpan.FromSeconds(2))
        {ImGui.TextWrapped(localization.Get("CraftProjectionUi.Old"));return;}
        DrawCraftFollow(frame,catalog);
        if(projectionResult is not { } result)return;
        if(frame.Recipe is { } recipe && projectionRecipe!=recipe.RecipeId)
        {projectionRecipe=recipe.RecipeId;projectionTarget=recipe.Simulation?.Quality ?? 0;}
        ImGui.SetNextItemWidth(110);ImGui.InputInt(localization.Get("CraftProjectionUi.Target"),ref projectionTarget,0);Tip("CraftProjectionUi.TargetTip");
        if(projectionTarget<0 || projectionTarget>(frame.Recipe?.Simulation?.Quality ?? 0)){ImGui.TextWrapped(localization.Get("CraftProjectionUi.Invalid"));return;}
        if(ImGui.RadioButton(localization.Get("CraftProjectionUi.Quality"),projectionSort==0))projectionSort=0;Tip("CraftProjectionUi.SortTip");ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("CraftProjectionUi.Progress"),projectionSort==1))projectionSort=1;Tip("CraftProjectionUi.SortTip");ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("CraftProjectionUi.Durability"),projectionSort==2))projectionSort=2;Tip("CraftProjectionUi.SortTip");
        DrawProjectionCondition(result,frame.Raw.ConditionText);
        UpdateCraftSuggestion(result,craftForbidden);
        DrawCraftSuggestion(result,catalog,frame.Raw.JobId);
        foreach(var issue in result.Issues)DrawCraftIssue(issue);
        if(result.Actions.Count==0){ImGui.TextDisabled(localization.Get("CraftProjectionUi.Empty"));return;}
        ImGui.TextDisabled(localization.Get("CraftProjectionUi.SortHelp"));
        if(ImGui.BeginTable("LiveCraftProjection",7,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("CraftProjectionUi.Action"),ImGuiTableColumnFlags.WidthStretch,2);
                foreach(var title in new[]{localization.Get("CraftProjectionUi.AfterProgress"),localization.Get("CraftProjectionUi.AfterQuality"),localization.Get("CraftProjectionUi.AfterDurability"),localization.Get("CraftProjectionUi.Cp"),localization.Get("CraftProjectionUi.Quiet"),localization.Get("CraftProjectionUi.Result")})ImGui.TableSetupColumn(title);
                ImGui.TableHeadersRow();
                // 0.438 (user request): forbidden actions are not shown.
                var forbiddenKey=craftForbidden;
                if(!ReferenceEquals(sortedProjectionSource,result) || sortedProjectionOrder!=projectionSort || sortedProjectionForbidden!=forbiddenKey)
                {
                    sortedProjectionSource=result;sortedProjectionOrder=projectionSort;sortedProjectionForbidden=forbiddenKey;
                    sortedProjectionRows=result.Actions.Where(row=>!CraftActionRules.Forbidden(forbiddenKey,row.Metadata.Spec.Kind)).OrderBy(row=>row.Broken).ThenByDescending(row=>row.OnlyInThisCondition).ThenByDescending(row=>projectionSort==0 ? row.Quality?.Min ?? -1:projectionSort==1 ? row.Progress.Min:row.Durability.Min).ThenBy(row=>row.CpCost.Max).ToArray();
                }
                var rows=sortedProjectionRows;
                foreach(var row in rows)
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();DrawItemIcon(new ItemDefinition(0,row.Metadata.Name,false,row.Metadata.Icon),new Vector2(24));ImGui.SameLine();ImGui.TextWrapped(row.Metadata.Name);
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("LiveDetails.Alternatives",row.Metadata.Description,row.Alternatives));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(row.Progress.ToString());ImGui.TableNextColumn();ImGui.TextUnformatted(row.Quality?.ToString() ?? localization.Get("CraftProjectionUi.Unknown"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(row.Durability.ToString());ImGui.TableNextColumn();ImGui.TextUnformatted(row.CpCost.ToString());
                    ImGui.TableNextColumn();ImGui.TextUnformatted(row.InnerQuiet.ToString());ImGui.TableNextColumn();
                    var text=row.Broken ? localization.Get("CraftProjectionUi.Broken"):row.Completed ? row.Quality is null ? localization.Get("CraftProjectionUi.UnknownEnd"):row.Quality.Value.Min>=projectionTarget ? localization.Get("CraftProjectionUi.Reached"):localization.Get("CraftProjectionUi.Below"):localization.Get("CraftProjectionUi.Continue");
                    if(row.OnlyInThisCondition && !row.Broken && !row.Completed)ImGui.TextColored(Success,localization.Get("CraftProjectionUi.OnlyNow"));
                    else ImGui.TextColored(row.Broken || row.Completed && (row.Quality is null || row.Quality.Value.Min<projectionTarget) ? Accent:row.Completed ? Success:Muted,text);
                }
            }
            finally{ImGui.EndTable();}
        }
    }
}
