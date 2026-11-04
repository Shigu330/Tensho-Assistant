using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-06 (0.409): while the game's Collectable window is open, the gathering panel (setting GatheringPanel) gives way to this one,
/// beside it: the item's collectability thresholds and scrip rewards from the game's sheets, GP and integrity as shown. The current
/// collectability is not read yet (no named field; developer mode lists the window's raw values to find it in game), so no next
/// action is advised.
/// </summary>
internal sealed partial class MainWindow
{
    internal CollectableReader? Collectables { get; set; }
    private string? collectableRawLogged;
    // 0.479: an action's name and icon, from the game's sheet.
    internal Func<uint,(string Name,uint Icon,int Cost)?>? CollectableActionInfo{get;set;}
    // 0.480: Scrutiny is on the player now (status read on the game's thread, as the draw is).
    internal Func<bool>? ScrutinyActive{get;set;}
    private IReadOnlyList<string>? collectableTurnRaw;
    private CollectableTurn? collectableTurn;
    private (CollectableTurn Turn,int? Attempts,CollectableRule? Rule,CollectableBonuses Bonuses)? collectableAdviceKey;
    private CollectableAdvice? collectableAdvice;
    private readonly Dictionary<uint,int> collectableChanceBase=[];

    internal void DrawCollectablePanel()
    {
        if(windowDisposed || !configuration.GatheringPanel || Collectables?.Frame is not { } frame)return;
        using var theme=UI.FfxivTheme.Apply();
        ImGui.SetNextWindowPos(new Vector2(frame.X+frame.Width+6,frame.Y),ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(360,0),ImGuiCond.Always);
        var visible=ImGui.Begin(localization.Get("CollectablePanel.Title")+"###TenshoCollectablePanel",
            ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize);
        try
        {
            if(!visible)return;
            // 0.478: the game's window cuts long names; the rules hold the whole one.
            if(!ReferenceEquals(collectableTurnRaw,frame.RawValues)){collectableTurnRaw=frame.RawValues;collectableTurn=CollectableTurn.Parse(frame.RawValues);}
            var turn=collectableTurn;
            // 0.479: the window names its item by identifier; the name is only for when its values cannot be read.
            var named=turn is not null && Collectables.Catalog?.For(turn.ItemId) is {Count:>0} rows ? rows:Collectables.Catalog?.Named(frame.ItemName);
            ImGui.TextColored(Accent,named is { Count:>0 } ? named[0].Name:frame.ItemName);
            ImGui.TextDisabled(localization.Format("CollectablePanel.Resources",Shown(frame.Gp),Shown(frame.MaxGp),Shown(frame.Integrity),Shown(frame.MaxIntegrity)));
            if(Collectables.Catalog is not { } catalog)ImGui.TextDisabled(localization.Get(Collectables.CatalogFailed ? "CollectablePanel.Unavailable":"CollectablePanel.Loading"));
            else if(named is not { Count:>0 } rules)ImGui.TextWrapped(localization.Get("CollectablePanel.NoRule"));
            else
                foreach(var rule in rules.DistinctBy(rule=>(rule.Low,rule.Mid,rule.High,rule.RewardLow,rule.RewardMid,rule.RewardHigh,rule.Scrips)).Take(4))
                {
                    ImGui.Separator();
                    ImGui.TextUnformatted(localization.Format("CollectablePanel.Thresholds",rule.Low,rule.Mid,rule.High));
                    if(rule.Scrips && rule.RewardHigh>0)ImGui.TextUnformatted(localization.Format("CollectablePanel.Scrips",rule.RewardLow,rule.RewardMid,rule.RewardHigh));
                    if(rule.LevelMin>0)ImGui.TextDisabled(localization.Format("CollectablePanel.Levels",rule.LevelMin,rule.LevelMax));
                }
            ImGui.Separator();
            if(turn is not null)DrawCollectableAdvice(turn,frame.Integrity,frame.MaxIntegrity,frame.Gp,named?.FirstOrDefault(rule=>rule.Scrips) ?? named?.FirstOrDefault());
            else ImGui.TextWrapped(localization.Get("CollectablePanel.Unknown"));
            // Developer mode: what the window holds goes to the log at each change, to find which value follows the collectability.
            if(developerMode && string.Join(" | ",frame.RawValues) is var raw && raw!=collectableRawLogged){collectableRawLogged=raw;log.Information("Collectable window values: {Values}",raw);}
            if(developerMode && ImGui.CollapsingHeader(localization.Get("CollectablePanel.Raw")+"###CollectableRaw"))
                foreach(var value in frame.RawValues)ImGui.TextDisabled(value);
        }
        finally { ImGui.End(); }
        static string Shown(int? value)=>value?.ToString("N0") ?? "?";
    }

    // 0.481 (user report; the boxes were misread in 0.480): what each bonus adds, as the game shows it when its box is ticked or when Scrutiny is on. Both boxes: the sum, split if one is known.
    private void LearnCollectableBonuses(CollectableTurn turn,bool active)
    {
        if(turn.Extra is not { } extra || extra<=0)return;
        var scrutiny=configuration.CollectableScrutinyGain;var intuition=configuration.CollectableIntuitionGain;
        if(turn.EstimateScrutiny && turn.EstimateIntuition)
        {
            if(scrutiny>0 && extra>scrutiny)intuition=extra-scrutiny;
            else if(intuition>0 && extra>intuition)scrutiny=extra-intuition;
        }
        else if(turn.EstimateScrutiny || active && !turn.EstimateIntuition)scrutiny=extra;
        else if(turn.EstimateIntuition && !active)intuition=extra;
        if(scrutiny==configuration.CollectableScrutinyGain && intuition==configuration.CollectableIntuitionGain)return;
        configuration.CollectableScrutinyGain=scrutiny;configuration.CollectableIntuitionGain=intuition;SaveReminders();
    }

    // 0.479 (user request, in-game campaign): the current collectability and what to do next, from the values the window gives this turn. Nothing is executed.
    private void DrawCollectableAdvice(CollectableTurn turn,int? attempts,int? maximum,int? gp,CollectableRule? rule)
    {
        var active=ScrutinyActive?.Invoke()==true;
        LearnCollectableBonuses(turn,active);
        var job=turn.ScourAction==22182 ? 16u:17u;
        var restore=configuration.CollectableUseRestore ? gpActions.State.Catalog?.Actions.FirstOrDefault(row=>row.Spec.JobId==job && row.Spec.Effect==GpEffect.Attempt && !GpActionRules.Forbidden(GpForbidden,row.Spec)):null;
        if(turn.IntuitionChance>0 && (!collectableChanceBase.TryGetValue(turn.ItemId,out var lowest) || turn.IntuitionChance<lowest))collectableChanceBase[turn.ItemId]=turn.IntuitionChance;
        var bonuses=new CollectableBonuses(configuration.CollectableScrutinyGain,configuration.CollectableIntuitionGain,gp ?? 0,
            CollectableActionInfo?.Invoke(turn.ScrutinyAction)?.Cost ?? 0,configuration.CollectableUseFocus ? CollectableActionInfo?.Invoke(turn.FocusAction)?.Cost ?? 0:0,active,configuration.CollectableUseScrutiny,restore?.Spec.Cost ?? 0,maximum ?? 0,
            collectableChanceBase.GetValueOrDefault(turn.ItemId));
        var tier=rule is null ? CollectableTier.Below:Gathering.Collectables.Tier(rule,turn.Current);
        ImGui.ProgressBar(Math.Clamp((float)turn.Current/turn.Maximum,0,1),new Vector2(-1,ImGui.GetFrameHeight()),localization.Format("CollectablePanel.Current",turn.Current.ToString("N0"),turn.Maximum.ToString("N0"),localization.Get($"CollectablePanel.Tier{tier}")));
        // Worked out when something changes, not at each frame.
        var adviceKey=(turn,attempts,rule,bonuses);
        if(collectableAdviceKey!=adviceKey){collectableAdviceKey=adviceKey;collectableAdvice=attempts is { } count ? CollectableAdvisor.Advise(turn,count,rule,bonuses):null;}
        if(collectableAdvice is not { } advice)
        {
            ImGui.TextDisabled(localization.Get(rule is null ? "CollectablePanel.NoAdviceRule":"CollectablePanel.NoAdvice"));
            return;
        }
        var (action,gain)=advice.Move switch
        {
            CollectableMove.Scour=>(turn.ScourAction,$"+{turn.Scour}"),
            CollectableMove.Brazen=>(turn.BrazenAction,localization.Format("CollectablePanel.Range",turn.BrazenMin,turn.BrazenMax)),
            CollectableMove.Scrutiny=>(turn.ScrutinyAction,localization.Format("CollectablePanel.ScrutinyGain",bonuses.ScrutinyBonus,bonuses.ScrutinyCost)),
            CollectableMove.Restore=>(restore?.Spec.Id ?? 0,localization.Format("CollectablePanel.RestoreGain",bonuses.RestoreCost)),
            CollectableMove.Focus=>(turn.FocusAction,localization.Format("CollectablePanel.FocusGain",bonuses.FocusCost,bonuses.IntuitionBonus)),
            CollectableMove.Meticulous=>(turn.MeticulousAction,localization.Format("CollectablePanel.MeticulousGain",turn.Meticulous,turn.MeticulousChance)),
            _=>(turn.CollectAction,""),
        };
        var info=CollectableActionInfo?.Invoke(action);
        ImGui.TextColored(Accent,localization.Get("CollectablePanel.Next"));
        DrawItemIcon(info is { } shown ? new TenshoAssistant.Items.ItemDefinition(0,shown.Name,false,shown.Icon):null,new Vector2(32));
        ImGui.SameLine();
        ImGui.TextUnformatted(gain.Length>0 ? $"{info?.Name ?? advice.Move.ToString()} ({gain})":info?.Name ?? advice.Move.ToString());
        ImGui.PushTextWrapPos(0);
        try
        {
            if(advice.Impossible)ImGui.TextWrapped(localization.Get("CollectablePanel.Impossible"));
            else
            {
                if(rule is {Scrips:true,RewardHigh:>0})
                {
                    ImGui.TextColored(Accent,advice.HighScrips is { } highest ? localization.Format("CollectablePanel.Reference",rule.RewardHigh,highest):localization.Format("CollectablePanel.ReferenceFar",rule.RewardHigh,advice.HighMissing));
                    ImGui.TextWrapped(localization.Format("CollectablePanel.Expected",advice.Expected.ToString("0.0")));
                }
                if(advice.Restores>0)ImGui.TextDisabled(localization.Format("CollectablePanel.PlanRestores",advice.Restores,restore?.Name ?? "",advice.Restores*bonuses.RestoreCost));
                ImGui.TextDisabled(advice.Target==CollectableTier.Below ? localization.Get("CollectablePanel.OutOfReach")
                    :advice.Scrips>0 ? localization.Format("CollectablePanel.Plan",localization.Get($"CollectablePanel.Tier{advice.Target}"),advice.Selections,advice.Collections,advice.Scrips)
                    :localization.Format("CollectablePanel.PlanItems",localization.Get($"CollectablePanel.Tier{advice.Target}"),advice.Selections,advice.Collections));
            }
        }
        finally{ImGui.PopTextWrapPos();}
        ImGui.PushTextWrapPos(0);
        try
        {
            if(configuration.CollectableUseScrutiny && configuration.CollectableScrutinyGain<=0)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("CollectablePanel.LearnScrutiny"));
            if(configuration.CollectableIntuitionGain<=0)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("CollectablePanel.LearnIntuition"));
        }
        finally{ImGui.PopTextWrapPos();}
        // 0.481 (user screenshot: the panel was too long): the method in a tooltip.
        ImGui.TextDisabled(localization.Get("CollectablePanel.HowLabel"));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("CollectablePanel.AdviceNote"));
    }
}
