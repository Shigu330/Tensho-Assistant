using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// C5 (0.589): the chosen sequence followed during a synthesis, above the comparison of next actions: the plan's next action, its numbers
// against the game's, and a warning when they part. The plan is simulated with the attributes of the synthesis, again only when something
// it depends on changes. Information only: nothing is played.
internal sealed partial class MainWindow
{
    private sealed record CraftFollowSource(string Key,string Label,IReadOnlyList<CraftActionKind> Actions);
    private readonly CraftFollowTracker craftFollowTracker=new();
    private CraftLiveFrame? craftFollowFrame;
    private CraftFollowSource[] craftFollowSources=[];
    private string craftFollowChoice="";
    private string craftFollowPlanKey="";
    private CraftSimulationResult? craftFollowPlan;
    private CraftFollowView? craftFollowView;

    private CraftFollowSource[] CraftFollowSourcesFor(RecipeDefinition recipe)
    {
        var sources=new List<CraftFollowSource>();
        if(simulationRecipe?.RecipeId==recipe.RecipeId && simulationActions.Count>0)
            sources.Add(new("simulator",localization.Get("CraftProjectionUi.FollowSimulator"),simulationActions.ToArray()));
        foreach(var preset in configuration.CraftPresets ?? [])
            if(preset is {Actions.Count:>0} && preset.RecipeId==recipe.RecipeId)sources.Add(new(preset.Id.ToString(),preset.Name,preset.Actions));
        if(craftLogSession.State is {Input:{ } input,Result.Suggested:{ } suggested} && input.Recipe.RecipeId==recipe.RecipeId)
            sources.Add(new("suggested",localization.Get("CraftProjectionUi.FollowSuggested"),suggested.Simulation.Steps.Select(step=>step.Action).ToArray()));
        return sources.ToArray();
    }

    private void UpdateCraftFollow(CraftLiveFrame frame,RecipeDefinition recipe)
    {
        var raw=frame.Raw;
        craftFollowFrame=frame;
        craftFollowSources=CraftFollowSourcesFor(recipe);
        // The numbers of a frame still playing an action are not those of a step: the last view stays.
        if(frame.Animating || frame.UiStep is not { } step || frame.Progress is not { } progress || frame.Durability is not { } durability)return;
        var quality=frame.Quality ?? 0;
        craftFollowTracker.Observe(recipe.RecipeId,step,quality,raw.NumbersNative?.Condition is null or 0,raw.ConditionText);
        var source=craftFollowSources.FirstOrDefault(row=>row.Key==craftFollowChoice) ?? craftFollowSources.FirstOrDefault();
        if(source is null){craftFollowPlan=null;craftFollowView=null;craftFollowPlanKey="";return;}
        var stats=raw.Stats with {Cp=(int)raw.MaxCp,ManipulationAllowed=true};
        var start=craftFollowTracker.StartingQuality;
        var key=$"{recipe.RecipeId}|{source.Key}|{string.Join(',',source.Actions)}|{stats}|{start}";
        if(key!=craftFollowPlanKey)
        {
            craftFollowPlanKey=key;
            try{craftFollowPlan=CraftSimulation.Run(recipe,stats,start ?? 0,source.Actions);}
            catch(ArgumentException){craftFollowPlan=null;}
        }
        craftFollowView=craftFollowPlan is { } plan
            ? CraftFollow.Compare(plan,recipe.Simulation?.Durability ?? 0,step,progress,quality,durability,(int)raw.Cp,start is not null):null;
    }

    private void DrawCraftFollow(CraftLiveFrame frame,CraftActionCatalog catalog)
    {
        if(!configuration.CraftLiveFollow || frame.Recipe is not { } recipe)return;
        if(!ReferenceEquals(craftFollowFrame,frame))UpdateCraftFollow(frame,recipe);
        if(craftFollowSources.Length==0)return;
        var current=craftFollowSources.FirstOrDefault(row=>row.Key==craftFollowChoice) ?? craftFollowSources[0];
        ImGui.SetNextItemWidth(260);
        if(ImGui.BeginCombo(localization.Get("CraftProjectionUi.FollowPick")+"###CraftFollowPick",current.Label))
        {
            try
            {
                foreach(var source in craftFollowSources)
                    if(ImGui.Selectable(source.Label+"###"+source.Key,source.Key==current.Key)){craftFollowChoice=source.Key;craftFollowFrame=null;}
            }
            finally{ImGui.EndCombo();}
        }
        Tip("CraftProjectionUi.FollowPickTip");
        if(craftFollowPlan is not { } plan || craftFollowView is not { } view)
        {
            ImGui.TextDisabled(localization.Get("CraftProjectionUi.FollowCannot"));
            return;
        }
        if(view.Next is { } next)
        {
            var action=catalog.Find(frame.Raw.JobId,next);
            if(action is not null){DrawGameIcon(action.Icon,new Vector2(28));ImGui.SameLine();}
            ImGui.TextColored(Accent,localization.Format("CraftProjectionUi.FollowNext",view.Position+1,view.Total,action?.Name ?? next.ToString()));
            if(!frame.Animating && frame.Raw.ActionsNative?.FirstOrDefault(row=>row.Kind==next) is {Status:not 0})
                ImGui.TextColored(Accent,localization.Get("CraftProjectionUi.FollowBlocked"));
        }
        else ImGui.TextColored(Success,localization.Get("CraftProjectionUi.FollowDone"));
        var expected=view.Expected;
        ImGui.TextDisabled(localization.Format("CraftProjectionUi.FollowPlanned",expected.Progress.ToString("N0",Numbers),view.QualityCompared ? expected.Quality.ToString("N0",Numbers):"—",expected.Durability,expected.Cp));
        ImGui.TextUnformatted(localization.Format("CraftProjectionUi.FollowNow",(frame.Progress ?? 0).ToString("N0",Numbers),(frame.Quality ?? 0).ToString("N0",Numbers),frame.Durability ?? 0,frame.Raw.Cp));
        if(!view.QualityCompared)ImGui.TextDisabled(localization.Get("CraftProjectionUi.FollowNoStart"));
        if(view.Diverged)
        {
            var states=craftFollowTracker.Before(frame.UiStep ?? 0);
            ImGui.PushStyleColor(ImGuiCol.Text,UI.FfxivTheme.Warning);
            try
            {
                ImGui.TextWrapped(states.Count>0
                    ? localization.Format("CraftProjectionUi.FollowOffStates",string.Join(", ",states.Select(row=>localization.Format("CraftProjectionUi.FollowStateAt",row.Name,row.Step))))
                    : localization.Get("CraftProjectionUi.FollowOff"));
            }
            finally{ImGui.PopStyleColor();}
        }
        else ImGui.TextColored(Success,localization.Get("CraftProjectionUi.FollowOnPlan"));
        if(plan.InvalidAction is { } stopped)ImGui.TextDisabled(localization.Format("CraftProjectionUi.FollowStops",stopped+1,plan.PlannedActions));
        ImGui.Separator();
    }
}
