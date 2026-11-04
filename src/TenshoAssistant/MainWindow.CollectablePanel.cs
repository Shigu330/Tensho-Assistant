using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
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
    // 0.483 (user report: Solid Reason not available when advised): the game's own answer for an action, 0 = usable now; null = unknown. Read only.
    internal Func<uint,uint?>? CollectableActionStatus{get;set;}
    private string? collectableRestoreLogged;
    private IReadOnlyList<string>? collectableTurnRaw;
    private CollectableTurn? collectableTurn;
    private (CollectableTurn Turn,int? Attempts,CollectableRule? Rule,CollectableBonuses Bonuses)? collectableAdviceKey;
    private volatile CollectableAdvice? collectableAdvice;
    private volatile bool collectableAdvicePending;
    private Task? collectableAdviceRun;
    private readonly Dictionary<uint,int> collectableChanceBase=[];

    internal void DrawCollectablePanel()
    {
        if(windowDisposed || !configuration.GatheringPanel || Collectables?.Frame is not { } frame)return;
        Panels?.Show("TenshoCollectablePanel",localization.Get("CollectablePanel.Title"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(frame.X+frame.Width+6,frame.Y),ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(360,0),ImGuiCond.Always);},()=>
        {
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
                    ImGui.TextWrapped(localization.Format("CollectablePanel.Thresholds",rule.Low,rule.Mid,rule.High));
                    if(rule.Scrips && rule.RewardHigh>0)ImGui.TextWrapped(localization.Format("CollectablePanel.Scrips",rule.RewardLow,rule.RewardMid,rule.RewardHigh));
                    if(rule.LevelMin>0)ImGui.TextDisabled(localization.Format("CollectablePanel.Levels",rule.LevelMin,rule.LevelMax));
                }
            ImGui.Separator();
            if(turn is not null)DrawCollectableAdvice(turn,frame.Integrity,frame.MaxIntegrity,frame.Gp,named?.FirstOrDefault(rule=>rule.Scrips) ?? named?.FirstOrDefault());
            else ImGui.TextWrapped(localization.Get("CollectablePanel.Unknown"));
            // Developer mode: what the window holds goes to the log at each change, to find which value follows the collectability.
            if(developerMode && string.Join(" | ",frame.RawValues) is var raw && raw!=collectableRawLogged){collectableRawLogged=raw;log.Information("Collectable window values: {Values}",raw);}
            if(developerMode && ImGui.CollapsingHeader(localization.Get("CollectablePanel.Raw")+"###CollectableRaw"))
                foreach(var value in frame.RawValues)ImGui.TextDisabled(value);
        });
        static string Shown(int? value)=>value?.ToString("N0") ?? "?";
    }

    // 0.484 (user request): the risk accepted, set here. Applied as the slider moves; saved when it is let go.
    // 0.529 (user request): three steps, each with what it does, shared with the settings.
    private void DrawCollectableRisk(CollectableAdvice? advice=null,CollectableRule? rule=null)
    {
        var risk=CollectableRisk.Snap(configuration.CollectableRiskTolerance);
        ImGui.TextDisabled(localization.Get("CollectablePanel.Risk"));
        foreach(var cap in CollectableRisk.Caps)
        {
            ImGui.SameLine();
            // 0.530 (user request): icons, the name in the tooltip: a hand holding an item, a scale, a die.
            var icon=cap==0 ? FontAwesomeIcon.HandHolding:cap==50 ? FontAwesomeIcon.BalanceScale:FontAwesomeIcon.Dice;
            var chosen=risk==cap;
            if(chosen)ImGui.PushStyleColor(ImGuiCol.Button,Accent with {W=0.55f});
            bool clicked;
            ImGui.PushFont(UiBuilder.IconFont);
            try{clicked=ImGui.Button(icon.ToIconString()+$"###CollectableRisk{cap}");}
            finally{ImGui.PopFont();if(chosen)ImGui.PopStyleColor();}
            if(clicked && !chosen){configuration.CollectableRiskTolerance=cap;risk=cap;SaveReminders();}
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get($"CollectablePanel.Cap{cap}")+Environment.NewLine+localization.Get($"CollectablePanel.Cap{cap}Tip"));
        }
        ImGui.PushTextWrapPos(0);
        // 0.531 (user report: the description did not follow the change): with a plan in hand, what this step gives on this node, in figures.
        try{ImGui.TextDisabled(advice is {Impossible:false} && rule is {Scrips:true,RewardHigh:>0} ? localization.Format($"CollectablePanel.Cap{risk}Now",advice.Worst.ToString("0.#"),advice.Expected.ToString("0.0")):localization.Get($"CollectablePanel.Cap{risk}Tip"));}
        finally{ImGui.PopTextWrapPos();}
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
    private readonly Dictionary<uint,int> collectableMeticulousBase=[];
    /// <summary>0.520: from the job's trait, Solid Reason's chance of a free attempt; and whether that free attempt is ready now.</summary>
    internal Func<uint,decimal>? EurekaChanceFor { get; set; }
    internal Func<bool>? EurekaReady { get; set; }
    private void DrawCollectableAdvice(CollectableTurn turn,int? attempts,int? maximum,int? gp,CollectableRule? rule)
    {
        var active=ScrutinyActive?.Invoke()==true;
        LearnCollectableBonuses(turn,active);
        var job=turn.ScourAction==22182 ? 16u:17u;
        var restore=configuration.CollectableUseRestore ? gpActions.State.Catalog?.Actions.FirstOrDefault(row=>row.Spec.JobId==job && row.Spec.Effect==GpEffect.Attempt && !GpActionRules.Forbidden(GpForbiddenFor(job),row.Spec)):null;
        if(turn.IntuitionChance>0 && (!collectableChanceBase.TryGetValue(turn.ItemId,out var lowest) || turn.IntuitionChance<lowest))collectableChanceBase[turn.ItemId]=turn.IntuitionChance;
        if(turn.MeticulousChance>0 && (!collectableMeticulousBase.TryGetValue(turn.ItemId,out var lowestMeticulous) || turn.MeticulousChance<lowestMeticulous))collectableMeticulousBase[turn.ItemId]=turn.MeticulousChance;
        // 0.488 (in-game log: status 579, then 0, then 579, within seconds): the game's answer changes with each action's lock. It no longer changes the plan, which made the panel blink; it only says to wait.
        uint? restoreStatus=null;
        if(restore is not null && attempts is { } attemptsNow && maximum is { } most && attemptsNow<most && CollectableActionStatus?.Invoke(restore.Spec.Id) is { } status)
        {
            var seen=$"{status}|{attemptsNow}/{most}";
            if(developerMode && seen!=collectableRestoreLogged){collectableRestoreLogged=seen;log.Information("Collectable restore action {Action}: status {Status} at {Attempts}/{Maximum}",restore.Spec.Id,status,attemptsNow,most);}
            restoreStatus=status;
        }
        var bonuses=new CollectableBonuses(configuration.CollectableScrutinyGain,configuration.CollectableIntuitionGain,gp ?? 0,
            CollectableActionInfo?.Invoke(turn.ScrutinyAction)?.Cost ?? 0,configuration.CollectableUseFocus ? CollectableActionInfo?.Invoke(turn.FocusAction)?.Cost ?? 0:0,active,configuration.CollectableUseScrutiny,restore?.Spec.Cost ?? 0,maximum ?? 0,
            collectableChanceBase.GetValueOrDefault(turn.ItemId),CollectableRisk.Snap(configuration.CollectableRiskTolerance),restore is null ? 0:EurekaChanceFor?.Invoke(job) ?? 0,EurekaReady?.Invoke()==true,
            configuration.CollectableUsePriming && turn.PrimingAction!=0 ? CollectableActionInfo?.Invoke(turn.PrimingAction)?.Cost ?? 0:0,collectableMeticulousBase.GetValueOrDefault(turn.ItemId));
        var tier=rule is null ? CollectableTier.Below:Gathering.Collectables.Tier(rule,turn.Current);
        ImGui.ProgressBar(Math.Clamp((float)turn.Current/turn.Maximum,0,1),new Vector2(-1,ImGui.GetFrameHeight()),localization.Format("CollectablePanel.Current",turn.Current.ToString("N0"),turn.Maximum.ToString("N0"),localization.Get($"CollectablePanel.Tier{tier}")));
        // Worked out when something changes, not at each frame.
        var adviceKey=(turn,attempts,rule,bonuses);
        // Off the draw: a hard case takes most of a second (0.484), and a stale result is not shown.
        if(collectableAdviceKey!=adviceKey && collectableAdviceRun is null or {IsCompleted:true})
        {
            collectableAdviceKey=adviceKey;collectableAdvicePending=true;
            var count=attempts ?? 0;
            collectableAdviceRun=Task.Run(()=>
            {
                try{collectableAdvice=count>0 ? CollectableAdvisor.Advise(turn,count,rule,bonuses):null;}
                catch(Exception error){collectableAdvice=null;log.Warning(error,"The collectable plan could not be worked out.");}
                finally{collectableAdvicePending=false;}
            });
        }
        // While a new plan is worked out the last one stays (a shorter panel for a moment was a blink), unless it is another item's.
        if(collectableAdvice is null && (collectableAdvicePending || collectableAdviceKey!=adviceKey))
        {
            ImGui.TextDisabled(localization.Get("CollectablePanel.Working"));
            DrawCollectableRisk();
            return;
        }
        if(collectableAdvice is not { } advice)
        {
            ImGui.TextDisabled(localization.Get(rule is null ? "CollectablePanel.NoAdviceRule":"CollectablePanel.NoAdvice"));
            return;
        }
        var (action,gain)=advice.Move switch
        {
            CollectableMove.Scour=>(turn.ScourAction,$"+{advice.GainLow}"),
            CollectableMove.Brazen=>(turn.BrazenAction,localization.Format("CollectablePanel.Range",advice.GainLow,advice.GainHigh)),
            CollectableMove.Scrutiny=>(turn.ScrutinyAction,localization.Format("CollectablePanel.ScrutinyGain",bonuses.ScrutinyBonus,bonuses.ScrutinyCost)),
            CollectableMove.Restore=>(restore?.Spec.Id ?? 0,localization.Format(restoreStatus is null or 0 ? "CollectablePanel.RestoreGain":"CollectablePanel.RestoreWait",bonuses.RestoreCost)),
            CollectableMove.Eureka=>(job==16 ? 26521u:26522u,localization.Get("CollectablePanel.EurekaGain")),
            CollectableMove.Priming=>(turn.PrimingAction,localization.Format("CollectablePanel.PrimingGain",bonuses.PrimingCost,Math.Min(100,Math.Max(1,bonuses.MeticulousBase)*2))),
            CollectableMove.Focus=>(turn.FocusAction,localization.Format("CollectablePanel.FocusGain",bonuses.FocusCost,bonuses.IntuitionBonus)),
            CollectableMove.Meticulous=>(turn.MeticulousAction,localization.Format("CollectablePanel.MeticulousGain",advice.GainLow,turn.MeticulousChance)),
            _=>(turn.CollectAction,""),
        };
        var info=CollectableActionInfo?.Invoke(action);
        DrawCollectableRisk(advice,rule);
        ImGui.TextColored(Accent,localization.Get("CollectablePanel.Next"));
        DrawItemIcon(info is { } shown ? new TenshoAssistant.Items.ItemDefinition(0,shown.Name,false,shown.Icon):null,new Vector2(32));
        ImGui.SameLine();
        // 0.528 (user capture): this line was cut at the panel's width.
        ImGui.TextWrapped(gain.Length>0 ? $"{info?.Name ?? advice.Move.ToString()} ({gain})":info?.Name ?? advice.Move.ToString());
        ImGui.PushTextWrapPos(0);
        try
        {
            if(advice.Impossible)ImGui.TextWrapped(localization.Get("CollectablePanel.Impossible"));
            else
            {
                if(rule is {Scrips:true,RewardHigh:>0})
                {
                    ImGui.TextColored(Accent,advice.HighScrips is { } highest ? localization.Format("CollectablePanel.Reference",rule.RewardHigh,highest):localization.Format("CollectablePanel.ReferenceFar",rule.RewardHigh,advice.HighMissing));
                }
                // 0.529 (user request): one plan on screen, the one of the chosen step. Only « Safe » follows the plan without luck, so only it lists that plan's actions and GP.
                var safeStep=CollectableRisk.Snap(configuration.CollectableRiskTolerance)==CollectableRisk.Safe;
                if(safeStep)
                {
                    ImGui.TextDisabled(advice.Target==CollectableTier.Below ? localization.Get("CollectablePanel.OutOfReach")
                        :advice.Scrips>0 ? localization.Format("CollectablePanel.Plan",localization.Get($"CollectablePanel.Tier{advice.Target}"),advice.Selections,advice.Collections,advice.Scrips)
                        :localization.Format("CollectablePanel.PlanItems",localization.Get($"CollectablePanel.Tier{advice.Target}"),advice.Selections,advice.Collections));
                    // 0.482 (user report: « 5 actions » for 4 attempts): the actions that cost no attempt, and where an extra attempt comes from, are said under the plan.
                    if(advice.Scrutinies>0)ImGui.TextDisabled(localization.Format("CollectablePanel.PlanScrutinies",advice.Scrutinies,CollectableActionInfo?.Invoke(turn.ScrutinyAction)?.Name ?? "",advice.Scrutinies*bonuses.ScrutinyCost));
                    if(advice.Restores>0)ImGui.TextDisabled(localization.Format("CollectablePanel.PlanRestores",advice.Restores,restore?.Name ?? "",advice.Restores*bonuses.RestoreCost,attempts ?? 0,(attempts ?? 0)+advice.Restores));
                }
                else if(advice.Target!=CollectableTier.Below && advice.Scrips>0)ImGui.TextDisabled(localization.Format("CollectablePanel.SafeWould",advice.Scrips));
                if(advice.Restores>0 && bonuses.EurekaChance>0)ImGui.TextDisabled(localization.Format("CollectablePanel.PlanEureka",(bonuses.EurekaChance*100).ToString("0"),CollectableActionInfo?.Invoke(job==16 ? 26521u:26522u)?.Name ?? ""));
                // 0.527 (user report): why Collector's Focus or Priming Touch may not be the move.
                if(advice.Move is not (CollectableMove.Focus or CollectableMove.Priming) && (turn.FocusOpen && bonuses.FocusCost>0 || turn.PrimingOpen && bonuses.PrimingCost>0))
                    ImGui.TextDisabled(localization.Format("CollectablePanel.BonusWhenWorth",CollectableActionInfo?.Invoke(turn.FocusAction)?.Name ?? "",turn.PrimingAction!=0 ? CollectableActionInfo?.Invoke(turn.PrimingAction)?.Name ?? "":""));
                // 0.482 (user report: GP must count): what this plan spends, against the GP there is.
                if(safeStep && advice.Scrutinies*bonuses.ScrutinyCost+advice.Restores*bonuses.RestoreCost is > 0 and var planned)ImGui.TextDisabled(localization.Format("CollectablePanel.PlanGp",planned,bonuses.Gp,Math.Max(0,bonuses.Gp-planned)));
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
