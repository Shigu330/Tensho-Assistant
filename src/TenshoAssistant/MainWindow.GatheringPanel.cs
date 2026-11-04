using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// In-game gathering help (0.225): a panel beside the game's gathering window (setting GatheringPanel, needs a fresh node observation),
/// headed by the job (miner or botanist, own icon and colour), listing the node's items (ItemCell, 0.252) with success, gatherer's boon,
/// stock and the Bountiful II estimate, and the GP plan for the chosen item with that job's own actions (GpOptimizer, same as the GP
/// tool), solved from the node as it opened (0.330) and recomputed only when those values change.
/// </summary>
internal sealed partial class MainWindow
{
    private (uint Job,int Level,int Slot,uint Item,int Gp,int? Attempts,bool Late,int? Success,int? Boon,int? Bountiful,GpActionCatalog Catalog)? gatheringPanelKey;
    private GpOptimization? gatheringPanelPlan;private uint gatheringPanelItem;private string[] gatheringPanelMissing=[];private bool gatheringPanelLate,gatheringPanelBountifulFloor;
    private GatheringNodeStart? gatheringPanelStart;

    // 0.330 (audit 8): the node as it opened, before any GP spent at it (TrackGatheringNode).
    private sealed class GatheringNodeStart(ulong owner,GatheringObservedFrame frame)
    {
        public ulong Owner {get;}=owner;public uint JobId {get;}=frame.JobId;public int Level {get;}=frame.Level;public int? TotalAttempts {get;}=frame.TotalAttempts;
        public uint Gp {get;set;}=frame.Gp;public int? Attempts {get;set;}=frame.Attempts;public bool Frozen {get;set;}
        // Success and boon of each slot (index, raw item) as first read; Late: first read after GP was spent at the node.
        public Dictionary<(int Index,uint RawItemId),(int? Success,int? Boon,bool Late,int? Quantity)> Slots {get;}=[];
    }

    internal void DrawGatheringPanel()
    {
        var observed=gatheringObservation.State;
        // 0.330: the node's opening values are forgotten when its window closes; a gather or an action only makes the reading « not ready ».
        if(windowDisposed || !configuration.GatheringPanel || observed.Status is not (GatheringObservationStatus.Observed or GatheringObservationStatus.NotReady))gatheringPanelStart=null;
        if(windowDisposed || !configuration.GatheringPanel || gatheringObservation.Window is not { } window
           || observed.Frame is not { } frame || !observed.Fresh(DateTimeOffset.UtcNow))return;
        TrackGatheringNode(observed.Owner,frame);
        using var theme=UI.FfxivTheme.Apply();
        ImGui.SetNextWindowPos(new Vector2(window.X+window.Width+6,window.Y),ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(360,0),ImGuiCond.Always);
        var miner=frame.JobId==16;
        var visible=ImGui.Begin(localization.Get(miner ? "GatheringPanel.TitleMiner":"GatheringPanel.TitleBotanist")+"###TenshoGatheringPanel",
            ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize);
        try
        {
            if(!visible)return;
            // Job header: its own icon (ClassJob 62100 + job) and colour, so miner and botanist help never look alike.
            var colour=miner ? new Vector4(0.95f,0.65f,0.30f,1):new Vector4(0.45f,0.85f,0.45f,1);
            DrawItemIcon(new ItemDefinition(0,"",false,62100+frame.JobId),new Vector2(28));ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.TextColored(colour,localization.Get(miner ? "GatheringPanel.Miner":"GatheringPanel.Botanist")+$" · {localization.Format("GatheringPanel.Level",frame.Level)}");
            ImGui.TextDisabled(localization.Format("GatheringPanel.Resources",frame.Gp,frame.MaxGp,frame.Attempts?.ToString() ?? "?",frame.TotalAttempts?.ToString() ?? "?"));
            ImGui.EndGroup();
            ImGui.Separator();
            var stats=characterStats.State;
            var rating=stats.JobId==frame.JobId && stats.Values.TryGetValue("Gathering",out var gathering) ? gathering:0;
            RefreshRecipeHoldingsCache();
            var items=itemCatalog.State.Catalog;
            var chosen=frame.Slots.FirstOrDefault(slot=>slot.ItemId!=0 && slot.ItemId==(uint)Math.Max(0,itemId)) ?? frame.Slots.FirstOrDefault(slot=>slot.ItemId!=0);
            foreach(var slot in frame.Slots.Where(slot=>slot.ItemId!=0))
            {
                var item=items?.Find(slot.ItemId);
                ImGui.PushID(slot.Index);
                try
                {
                    DrawItemIcon(item,new Vector2(24));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                    var bountiful=bountifulBase?.Invoke(slot.ItemId) is { } baseValue ? BountifulEstimate.Bonus(rating,baseValue):null;
                    var stock=GetRecipeHoldings(slot.ItemId);
                    // 0.478 (user report: lines cut at the panel's width): the name, then its rates under it.
                    var rates=$"{slot.SuccessPercent?.ToString() ?? "?"} % · {localization.Format("GatheringPanel.Boon",slot.BoonPercent?.ToString() ?? "?")}"
                        +(bountiful is { } bonus ? " · "+localization.Format("GatheringPanel.Bountiful",bonus):"")
                        +(stock.HasObservation ? " · ×"+stock.Total.ToString("N0"):"");
                    ImGui.BeginGroup();
                    if(ImGui.Selectable((item?.Name ?? slot.ItemId.ToString())+"##Slot",ReferenceEquals(slot,chosen)) || clicked)SelectItem(slot.ItemId);
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(slot.ItemId);
                    ItemLinksMenu($"GatheringSlot{slot.ItemId}",item);
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(slot.ItemId);
                    ImGui.TextDisabled(rates);
                    ImGui.EndGroup();
                }
                finally{ImGui.PopID();}
            }
            ImGui.Separator();
            ImGui.PushTextWrapPos(0);
            try
            {
                DrawGatheringPanelPlan(chosen,rating,items,(int)frame.MaxGp);
                ImGui.TextDisabled(localization.Get("GatheringPanel.Footer"));
            }
            finally{ImGui.PopTextWrapPos();}
        }
        finally{ImGui.End();}
    }

    // 0.330 (audit 8): the game's rates already include the actions bought at the node (Gift, Tidings…) and the optimizer, told of no
    // active effect, would offer them again. The plan is therefore solved from the node as it opened: GP follows the reading while none is
    // spent and no attempt is used (regeneration), each slot keeps its first read rates, then everything stays until the window closes.
    // A node sharing no slot with them, another job or another character starts again.
    private void TrackGatheringNode(ulong owner,GatheringObservedFrame frame)
    {
        if(gatheringPanelStart is not { } start || start.Owner!=owner || start.JobId!=frame.JobId || start.TotalAttempts!=frame.TotalAttempts
           || frame.Slots.Count>0 && !frame.Slots.Any(slot=>start.Slots.ContainsKey((slot.Index,slot.RawItemId))))
            gatheringPanelStart=start=new(owner,frame);
        // 0.478: the GP that comes back at each tick, as the game gives it: a small rise while nothing is spent. The smallest seen counts (two ticks can fall between two reads).
        var gained=(int)frame.Gp-(int)start.Gp;
        if(!start.Frozen && gained is >0 and <=50 && (configuration.GpRegenObserved==0 || gained<configuration.GpRegenObserved)){configuration.GpRegenObserved=gained;SaveReminders();}
        start.Frozen|=frame.Gp<start.Gp || frame.Attempts<start.Attempts;
        if(!start.Frozen){start.Gp=frame.Gp;start.Attempts??=frame.Attempts;}
        foreach(var slot in frame.Slots)
        {
            var at=(slot.Index,slot.RawItemId);
            if(!start.Slots.TryGetValue(at,out var first))start.Slots[at]=(slot.SuccessPercent,slot.BoonPercent,start.Frozen,slot.DisplayedQuantity);
            else if(!start.Frozen && (first.Success is null || first.Boon is null))start.Slots[at]=(first.Success ?? slot.SuccessPercent,first.Boon ?? slot.BoonPercent,false,first.Quantity ?? slot.DisplayedQuantity);
        }
    }

    // GP plan for the chosen item from the node as it opened (TrackGatheringNode); recomputed only when those values change.
    private void DrawGatheringPanelPlan(GatheringObservedSlot? slot,int rating,ItemCatalog? items,int maxGp)
    {
        // 0.478 (user report): a collectable's GP goes in the game's Collectable window, not in the actions of this plan; its rates here are not those of an ordinary gather.
        if(slot is not null && Collectables?.Catalog?.For(slot.ItemId) is {Count:>0}){ImGui.TextDisabled(localization.Get("GatheringPanel.CollectablePlan"));return;}
        if(slot is null || gatheringPanelStart is not { } start || gpActions.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("GatheringPanel.NoPlan"));return;}
        var first=start.Slots.GetValueOrDefault((slot.Index,slot.RawItemId));
        var bountiful=bountifulBase?.Invoke(slot.ItemId) is { } baseValue ? BountifulEstimate.Bonus(rating,baseValue):null;
        var key=(start.JobId,start.Level,slot.Index,slot.ItemId,(int)start.Gp,start.Attempts,first.Late,first.Success,first.Boon,bountiful,catalog);
        // 0.478: the route also follows what the list needs, the GP that comes back and the forbidden actions.
        var need=GatherListNeed(slot.ItemId);var seconds=Math.Clamp(configuration.GatherSecondsBetweenNodes,5,600);
        var regenPerNode=GpRegenPerTick*seconds/3;
        var routeKey=$"{need}|{maxGp}|{regenPerNode}|{GpForbidden}|{first.Quantity}|{configuration.GpSaveForBonus}";
        if(routeKey!=gatheringPanelRouteKey){gatheringPanelRouteKey=routeKey;gatheringPanelKey=null;}
        if(key!=gatheringPanelKey)
        {
            gatheringPanelRoute=null;gatheringPanelBonus=false;
            gatheringPanelKey=key;gatheringPanelItem=slot.ItemId;gatheringPanelPlan=null;gatheringPanelLate=first.Late;
            // 0.330 (audit 7): an unread success, boon or attempt count is never replaced (it was 100 % success, 0 % boon): no plan, and the
            // panel names what is missing. An unknown Bountiful bonus uses the rule's floor, +1 (BountifulEstimate « else +1 »): a lower
            // bound, said under the plan, not a guess (review 0.330).
            var missing=new List<string>();
            if(start.Attempts is null)missing.Add("GatheringPanel.MissingAttempts");
            if(first.Success is null)missing.Add("GatheringPanel.MissingSuccess");
            if(first.Boon is null)missing.Add("GatheringPanel.MissingBoon");
            gatheringPanelMissing=[..missing];gatheringPanelBountifulFloor=start.Level>=68 && bountiful is null;
            if(missing.Count==0 && !first.Late && first.Success is { } success && first.Boon is { } boon && start.Attempts is { } attempts && attempts>=1)
            {
                try
                {
                    // 0.479: the quantity the game shows per gather; a node where it was not read counts one.
                    var quantity=Math.Clamp(first.Quantity ?? 1,1,20);
                    var scenario=new GpScenario(start.JobId,start.Level,(int)start.Gp,attempts,quantity,success,boon,bountiful ?? 1,MoreActions:true,Forbidden:GpForbidden);
                    gatheringPanelPlan=GpOptimizer.Solve(scenario,catalog);
                    var shape=new GatherNodeShape(attempts,quantity,boon);
                    var normal=gatherNodeNorms.Observe(slot.ItemId,shape);
                    gatheringPanelBonus=GatherNodeNorms.IsBonus(normal,shape);
                    if(need>0 && maxGp>0)
                    {
                        var whole=scenario with {Budget=maxGp};
                        var usual=gatheringPanelBonus ? GpOptimizer.Solve(whole with {Attempts=normal.Attempts,BaseYield=normal.Quantity,BoonPercent=normal.Boon},catalog):null;
                        gatheringPanelRoute=GpRoutes.Build(GpOptimizer.Solve(whole,catalog),new(need,(int)start.Gp,maxGp,regenPerNode,configuration.GpSaveForBonus,gatheringPanelBonus),usual);
                    }
                }
                catch(ArgumentException){gatheringPanelPlan=null;gatheringPanelRoute=null;}
            }
        }
        ImGui.TextColored(Accent,localization.Format("GatheringPanel.PlanFor",items?.Find(gatheringPanelItem)?.Name ?? gatheringPanelItem.ToString()));
        if(gatheringPanelRoute is { } route)
        {
            DrawGatheringListRoute(route,need,seconds);
            if(configuration.GpSaveForBonus)ImGui.TextDisabled(localization.Get(gatheringPanelBonus ? "GatheringPanel.NodeBonus":"GatheringPanel.NodeNormal"));
        }
        if(gatheringPanelPlan is not { } plan)
        {
            ImGui.TextDisabled(gatheringPanelLate ? localization.Get("GatheringPanel.AfterStart")
                :gatheringPanelMissing.Length>0 ? localization.Format("GatheringPanel.Missing",string.Join(", ",gatheringPanelMissing.Select(localization.Get)))
                :localization.Get("GatheringPanel.NoPlan"));
            return;
        }
        var shown=gatheringPanelRoute?.First ?? plan.Best;
        ImGui.TextDisabled(localization.Format("GatheringPanel.Expected",shown.ExpectedYield.ToString("0.0"),plan.Baseline.ToString("0.0"),shown.Cost));
        if(start.Frozen)ImGui.TextDisabled(localization.Format("GatheringPanel.PlanAtStart",plan.Scenario.Budget));
        if(gatheringPanelBountifulFloor)ImGui.TextDisabled(localization.Get("GatheringPanel.BountifulFloor"));
        if(shown.Actions.Count==0)ImGui.TextUnformatted(localization.Get("GpUi.Keep"));
        foreach(var use in shown.Actions)
        {
            DrawItemIcon(new ItemDefinition(0,use.Action.Name,false,use.Action.Icon),new Vector2(22));ImGui.SameLine();
            ImGui.TextUnformatted(use.Count>1 ? $"{use.Action.Name} ×{use.Count}":use.Action.Name);
            if(ImGui.IsItemHovered())WrappedTooltip(use.Action.Description);
        }
        if(shown.Actions.Any(use=>use.Action.Spec.Effect==GpEffect.Attempt) && plan.Scenario.Level>=GpScenario.EurekaLevel)
            ImGui.TextDisabled(localization.Get("GpUi.Eureka"));
    }
}
