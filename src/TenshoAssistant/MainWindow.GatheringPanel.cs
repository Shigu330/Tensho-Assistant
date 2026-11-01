using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// In-game gathering help (0.225): a panel beside the game's gathering window, headed by the job (miner or botanist, own icon and colour),
/// listing the node's items with success, gatherer's boon, stock and the Bountiful II estimate, and the GP plan for the chosen item
/// with that job's own actions (GpOptimizer, same as the GP tool).
/// </summary>
internal sealed partial class MainWindow
{
    private (uint Job,uint Item,int Gp,int Attempts,int Success,int Boon,int Bountiful)? gatheringPanelKey;
    private GpOptimization? gatheringPanelPlan;private uint gatheringPanelItem;

    internal void DrawGatheringPanel()
    {
        if(windowDisposed || !configuration.GatheringPanel || gatheringObservation.Window is not { } window
           || gatheringObservation.State is not { Frame:{ } frame } state || !state.Fresh(DateTimeOffset.UtcNow))return;
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
                }
                finally{ImGui.PopID();}
            }
            ImGui.Separator();
            DrawGatheringPanelPlan(frame,chosen,rating,items);
            ImGui.TextDisabled(localization.Get("GatheringPanel.Footer"));
        }
        finally{ImGui.End();}
    }

    // GP plan for the chosen item with the observed values; recomputed only when they change.
    private void DrawGatheringPanelPlan(GatheringObservedFrame frame,GatheringObservedSlot? slot,int rating,ItemCatalog? items)
    {
        if(slot is null || gpActions.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("GatheringPanel.NoPlan"));return;}
        var bountiful=bountifulBase?.Invoke(slot.ItemId) is { } baseValue ? BountifulEstimate.Bonus(rating,baseValue) ?? 1:1;
        var key=(frame.JobId,slot.ItemId,(int)frame.Gp,frame.Attempts ?? 0,slot.SuccessPercent ?? 100,slot.BoonPercent ?? 0,bountiful);
        if(key!=gatheringPanelKey)
        {
            gatheringPanelKey=key;gatheringPanelItem=slot.ItemId;
            try{gatheringPanelPlan=frame.Attempts is >=1 ? GpOptimizer.Solve(new(frame.JobId,frame.Level,(int)frame.Gp,frame.Attempts.Value,1,slot.SuccessPercent ?? 100,slot.BoonPercent ?? 0,bountiful),catalog):null;}
            catch(ArgumentException){gatheringPanelPlan=null;}
        }
        ImGui.TextColored(Accent,localization.Format("GatheringPanel.PlanFor",items?.Find(gatheringPanelItem)?.Name ?? gatheringPanelItem.ToString()));
        if(gatheringPanelPlan is not { } plan){ImGui.TextDisabled(localization.Get("GatheringPanel.NoPlan"));return;}
        ImGui.TextDisabled(localization.Format("GatheringPanel.Expected",plan.Best.ExpectedYield.ToString("0.0"),plan.Baseline.ToString("0.0"),plan.Best.Cost));
        if(plan.Best.Actions.Count==0)ImGui.TextUnformatted(localization.Get("GpUi.Keep"));
        foreach(var use in plan.Best.Actions)
        {
            DrawItemIcon(new ItemDefinition(0,use.Action.Name,false,use.Action.Icon),new Vector2(22));ImGui.SameLine();
            ImGui.TextUnformatted(use.Count>1 ? $"{use.Action.Name} ×{use.Count}":use.Action.Name);
            if(ImGui.IsItemHovered())WrappedTooltip(use.Action.Description);
        }
    }
}
