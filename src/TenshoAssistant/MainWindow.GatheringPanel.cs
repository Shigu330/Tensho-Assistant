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
        public Dictionary<(int Index,uint RawItemId),(int? Success,int? Boon,bool Late)> Slots {get;}=[];
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
                    var line=$"{item?.Name ?? slot.ItemId.ToString()}  {slot.SuccessPercent?.ToString() ?? "?"} % · {localization.Format("GatheringPanel.Boon",slot.BoonPercent?.ToString() ?? "?")}"
                        +(bountiful is { } bonus ? " · "+localization.Format("GatheringPanel.Bountiful",bonus):"")
                        +(stock.HasObservation ? " · ×"+stock.Total.ToString("N0"):"");
                    if(ImGui.Selectable(line+"##Slot",ReferenceEquals(slot,chosen)) || clicked)SelectItem(slot.ItemId);
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(slot.ItemId);
                    ItemLinksMenu($"GatheringSlot{slot.ItemId}",item);
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(slot.ItemId);
                }
                finally{ImGui.PopID();}
            }
            ImGui.Separator();
            DrawGatheringPanelPlan(chosen,rating,items);
            ImGui.TextDisabled(localization.Get("GatheringPanel.Footer"));
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
        start.Frozen|=frame.Gp<start.Gp || frame.Attempts<start.Attempts;
        if(!start.Frozen){start.Gp=frame.Gp;start.Attempts??=frame.Attempts;}
        foreach(var slot in frame.Slots)
        {
            var at=(slot.Index,slot.RawItemId);
            if(!start.Slots.TryGetValue(at,out var first))start.Slots[at]=(slot.SuccessPercent,slot.BoonPercent,start.Frozen);
            else if(!start.Frozen && (first.Success is null || first.Boon is null))start.Slots[at]=(first.Success ?? slot.SuccessPercent,first.Boon ?? slot.BoonPercent,false);
        }
    }

    // GP plan for the chosen item from the node as it opened (TrackGatheringNode); recomputed only when those values change.
    private void DrawGatheringPanelPlan(GatheringObservedSlot? slot,int rating,ItemCatalog? items)
    {
        if(slot is null || gatheringPanelStart is not { } start || gpActions.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("GatheringPanel.NoPlan"));return;}
        var first=start.Slots.GetValueOrDefault((slot.Index,slot.RawItemId));
        var bountiful=bountifulBase?.Invoke(slot.ItemId) is { } baseValue ? BountifulEstimate.Bonus(rating,baseValue):null;
        var key=(start.JobId,start.Level,slot.Index,slot.ItemId,(int)start.Gp,start.Attempts,first.Late,first.Success,first.Boon,bountiful,catalog);
        if(key!=gatheringPanelKey)
        {
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
                try{gatheringPanelPlan=GpOptimizer.Solve(new(start.JobId,start.Level,(int)start.Gp,attempts,1,success,boon,bountiful ?? 1,MoreActions:true),catalog);}
                catch(ArgumentException){gatheringPanelPlan=null;}
            }
        }
        ImGui.TextColored(Accent,localization.Format("GatheringPanel.PlanFor",items?.Find(gatheringPanelItem)?.Name ?? gatheringPanelItem.ToString()));
        if(gatheringPanelPlan is not { } plan)
        {
            ImGui.TextDisabled(gatheringPanelLate ? localization.Get("GatheringPanel.AfterStart")
                :gatheringPanelMissing.Length>0 ? localization.Format("GatheringPanel.Missing",string.Join(", ",gatheringPanelMissing.Select(localization.Get)))
                :localization.Get("GatheringPanel.NoPlan"));
            return;
        }
        ImGui.TextDisabled(localization.Format("GatheringPanel.Expected",plan.Best.ExpectedYield.ToString("0.0"),plan.Baseline.ToString("0.0"),plan.Best.Cost));
        if(start.Frozen)ImGui.TextDisabled(localization.Format("GatheringPanel.PlanAtStart",plan.Scenario.Budget));
        if(gatheringPanelBountifulFloor)ImGui.TextDisabled(localization.Get("GatheringPanel.BountifulFloor"));
        if(plan.Best.Actions.Count==0)ImGui.TextUnformatted(localization.Get("GpUi.Keep"));
        foreach(var use in plan.Best.Actions)
        {
            DrawItemIcon(new ItemDefinition(0,use.Action.Name,false,use.Action.Icon),new Vector2(22));ImGui.SameLine();
            ImGui.TextUnformatted(use.Count>1 ? $"{use.Action.Name} ×{use.Count}":use.Action.Name);
            if(ImGui.IsItemHovered())WrappedTooltip(use.Action.Description);
        }
        if(plan.Best.Actions.Any(use=>use.Action.Spec.Effect==GpEffect.Attempt) && plan.Scenario.Level>=GpScenario.EurekaLevel)
            ImGui.TextDisabled(localization.Get("GpUi.Eureka"));
    }
}
