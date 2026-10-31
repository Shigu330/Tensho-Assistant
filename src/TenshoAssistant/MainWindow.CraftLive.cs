using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private CraftLiveFrame? liveEffectsFrame;
    private CraftActionCatalog? liveEffectsCatalog;
    private CraftEffectObservation? liveEffectsObservation;
    private void DrawCraftLive()
    {
        Heading(localization.Get("CraftLiveUi.Title"),localization.Get("CraftLiveUi.Description"));
        var state=craftLive.State;
        DrawToolSettingsLink("CraftLive");
        ImGui.TextWrapped(localization.Get("CraftLiveUi.Help"));
        ImGui.TextUnformatted(state.Status switch {CraftLiveStatusKind.Disabled=>localization.Get("CraftLiveUi.Disabled"),CraftLiveStatusKind.Waiting=>localization.Get("CraftLiveUi.Waiting"),CraftLiveStatusKind.Offline=>localization.Get("CraftLiveUi.Offline"),CraftLiveStatusKind.AddonAbsent=>localization.Get("CraftLiveUi.Absent"),CraftLiveStatusKind.NotReady=>localization.Get("CraftLiveUi.NotReady"),CraftLiveStatusKind.Error=>localization.Get("CraftLiveUi.Error"),_=>localization.Get("CraftLiveUi.Partial")});
        if(developerMode && state.Error is { } error)ImGui.TextWrapped(error);
        if(state.Frame is not { } frame)return;
        var raw=frame.Raw;var fresh=TenshoAssistant.Caching.CacheAge.Fresh(state.ObservedAt,DateTimeOffset.UtcNow,TimeSpan.FromSeconds(2));
        ImGui.TextDisabled(localization.Format("CraftLiveCounts.Observed",state.ObservedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—",localization.Get(fresh ? "CraftLiveCounts.Fresh":"CraftLiveCounts.Old")));
        if(frame.Recipe is { } recipe)
        {
            var item=itemCatalog.State.Catalog?.Find(recipe.ResultItemId);DrawItemIcon(item,new Vector2(36));ImGui.SameLine();ImGui.TextColored(Accent,item?.Name ?? raw.ItemName);
            ImGui.TextUnformatted(localization.Format("CraftLiveCounts.Recipe",recipe.CraftType,recipe.Level));
            if(developerMode)ImGui.TextDisabled($"Recipe ID {recipe.RecipeId}");
            ImGui.BeginDisabled(!fresh || frame.Animating);
            try {if(ImGui.SmallButton(localization.Get("CraftLiveUi.Context"))){SelectItem(recipe.ResultItemId);page=Page.Items;focusCraftTab=true;}}
            finally {ImGui.EndDisabled();}
        }
        else { ImGui.TextWrapped(localization.Format("LiveDetails.Unresolved",raw.ItemName)); if(developerMode)ImGui.TextDisabled($"Recipe {raw.RecipeId} · item {raw.ResultItemId}"); }
        ImGui.TextUnformatted(localization.Format("CraftLiveCounts.Step",frame.UiStep?.ToString() ?? "—"));
        ImGui.TextWrapped(localization.Format("CraftLiveCounts.Condition",raw.ConditionText));
        if(raw.Tool is { } tool)ImGui.TextWrapped(localization.Format("LiveDetails.Tool",itemCatalog.State.Catalog?.Find(tool.ItemId)?.Name ?? localization.Format("BankUi.Item",tool.ItemId),tool.GoodQualityPercent is { } percent ? $"× {percent/100m:0.00}":localization.Get("LiveDetails.Unknown")));
        else ImGui.TextDisabled(localization.Get("CraftLiveUi.NoTool"));
        if(frame.Animating)ImGui.TextDisabled(localization.Get("CraftLiveUi.Animating"));
        ImGui.TextUnformatted(localization.Format("CraftLiveCounts.Progress",frame.Progress?.ToString("N0") ?? "—",raw.Numbers.MaxProgress));
        ImGui.TextUnformatted(localization.Format("CraftLiveCounts.Quality",frame.Quality?.ToString("N0") ?? "—",raw.Numbers.MaxQuality));
        ImGui.TextUnformatted(localization.Format("CraftLiveCounts.Durability",frame.Durability?.ToString() ?? "—",raw.Numbers.MaxDurability));
        ImGui.TextUnformatted(localization.Format("LiveDetails.Stats",raw.Cp,raw.MaxCp,raw.Stats.Craftsmanship,raw.Stats.Control,raw.Stats.Level));
        DrawCraftProjection(state);
        if(craftActions.State.Catalog is { } actionCatalog && ImGui.CollapsingHeader(localization.Get("CraftLiveUi.Effects"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(!ReferenceEquals(liveEffectsFrame,frame) || !ReferenceEquals(liveEffectsCatalog,actionCatalog) || liveEffectsObservation is null)
            {liveEffectsObservation=CraftObservedEffects.Decode(frame,actionCatalog);liveEffectsFrame=frame;liveEffectsCatalog=actionCatalog;}
            var observation=liveEffectsObservation;
            foreach(var effect in observation.Effects)
            {
                var icon=effect.Metadata.Icon+(effect.Metadata.MaxStacks>0 ? (uint)(Math.Clamp(effect.Counter ?? 1,1,(int)effect.Metadata.MaxStacks)-1):0);
                DrawItemIcon(new ItemDefinition(0,effect.Metadata.Name,false,icon),new Vector2(28));ImGui.SameLine();
                ImGui.TextUnformatted(effect.Metadata.Name+" · "+(effect.Indefinite && effect.Counter is null ? localization.Get("LiveDetails.Present"):effect.Counter?.ToString() ?? localization.Get("LiveDetails.UnknownCounter")));
                if(ImGui.IsItemHovered())WrappedTooltip(effect.Metadata.Description);
            }
            foreach(var issue in observation.Issues)DrawCraftIssue(issue);
            ImGui.TextWrapped(localization.Format("LiveDetails.Trained",localization.Get(observation.UsedTrainedPerfection is true ? "LiveDetails.Yes":observation.UsedTrainedPerfection is false ? "LiveDetails.No":"LiveDetails.UnknownTrained")));
            ImGui.TextDisabled(localization.Format("LiveDetails.Procs",observation.NativeProcs.Count>0 ? string.Join(", ",observation.NativeProcs.Select(proc=>proc switch
            {
                "StandardTouchProc"=>actionCatalog.Find(raw.JobId,CraftActionKind.StandardTouch)?.Name ?? localization.Get("CraftMessages.StandardProc"),
                "AdvancedTouchProc"=>actionCatalog.Find(raw.JobId,CraftActionKind.AdvancedTouch)?.Name ?? localization.Get("CraftMessages.AdvancedProc"),
                _=>localization.Get("CraftMessages.ObservedProc"),
            })):localization.Get("LiveDetails.NoProcs")));
            ImGui.TextWrapped(localization.Get("CraftLiveUi.CountersHelp"));
        }
        if(ImGui.CollapsingHeader(localization.Get("CraftLiveUi.VisibleEffects"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach(var effect in raw.Effects)ImGui.TextWrapped(localization.Format("LiveDetails.VisibleCounter",effect.Name,effect.Turns));
            if(raw.Effects.Count==0)ImGui.TextDisabled(localization.Get("CraftLiveUi.NoEffects"));
            if(raw.EffectsOverflow.Length>0)ImGui.TextWrapped(localization.Format("LiveDetails.Extra",raw.EffectsOverflow));
        }
        if(developerMode && ImGui.CollapsingHeader(localization.Get("CraftLiveUi.Debug")))
        {
            ImGui.TextWrapped(frame.Source);ImGui.TextUnformatted(localization.Format("CraftDebug.Flags",raw.StateFlags,raw.Flags));
            if(raw.NumbersNative is { } numbers)ImGui.TextUnformatted(localization.Format("CraftDebug.Numbers",numbers.Step,numbers.Condition,raw.Condition));
            foreach(var status in raw.Statuses)ImGui.TextUnformatted(localization.Format("CraftDebug.Status",status.Id,status.Param,status.RemainingTime));
            foreach(var action in raw.ActionsNative ?? [])ImGui.TextUnformatted(localization.Format("CraftDebug.Action",action.NativeId,action.Kind,action.Status));
            ImGui.TextWrapped(localization.Get("CraftDebug.Help"));
        }
    }
}
