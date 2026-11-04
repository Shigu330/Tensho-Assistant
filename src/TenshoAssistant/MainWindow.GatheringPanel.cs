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
        var miner=frame.JobId==16;
        Panels?.Show("TenshoGatheringPanel",localization.Get(miner ? "GatheringPanel.TitleMiner":"GatheringPanel.TitleBotanist"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(window.X+window.Width+6,window.Y),ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(360,0),ImGuiCond.Always);},()=>
        {
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
                DrawGatheringPanelPlan(chosen,rating,items,frame);
                DrawGatheringNodeEffects(frame,rating);
                ImGui.TextDisabled(localization.Get("GatheringPanel.Footer"));
            }
            finally{ImGui.PopTextWrapPos();}
        });
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
            if(!start.Slots.TryGetValue(at,out var first))start.Slots[at]=(slot.SuccessPercent,slot.BoonPercent,start.Frozen,slot.DisplayedQuantity);
            else if(!start.Frozen && (first.Success is null || first.Boon is null))start.Slots[at]=(first.Success ?? slot.SuccessPercent,first.Boon ?? slot.BoonPercent,false,first.Quantity ?? slot.DisplayedQuantity);
        }
    }

    // GP plan for the chosen item from the node as it opened (TrackGatheringNode); recomputed only when those values change.
    private Func<uint,IReadOnlyList<GatherNodeEffect>>? nodeEffectsOf;
    internal void ConfigureNodeEffects(Func<uint,IReadOnlyList<GatherNodeEffect>> read)=>nodeEffectsOf=read;
    private readonly Dictionary<GatherNodeEffect,decimal> gatheringPanelEffectWith=[];
    private string? gatheringPanelExtraKey;
    private void DrawGatheringPanelPlan(GatheringObservedSlot? slot,int rating,ItemCatalog? items,GatheringObservedFrame frame)
    {
        // 0.478 (user report): a collectable's GP goes in the game's Collectable window, not in the actions of this plan; its rates here are not those of an ordinary gather.
        if(slot is not null && Collectables?.Catalog?.For(slot.ItemId) is {Count:>0}){ImGui.TextDisabled(localization.Get("GatheringPanel.CollectablePlan"));return;}
        // 0.519 (user report): shards, crystals and clusters have their own actions, with no figure to compute: the actions, without an amount.
        if(slot is not null && gatheringPanelStart is { } opened && items?.Find(slot.ItemId)?.UiCategoryId==GpCrystals.UiCategory){DrawGatheringCrystalPlan(slot,opened,items);return;}
        if(slot is null || gatheringPanelStart is not { } start || gpActions.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("GatheringPanel.NoPlan"));return;}
        var first=start.Slots.GetValueOrDefault((slot.Index,slot.RawItemId));
        var bountiful=bountifulBase?.Invoke(slot.ItemId) is { } baseValue ? BountifulEstimate.Bonus(rating,baseValue):null;
        var key=(start.JobId,start.Level,slot.Index,slot.ItemId,(int)start.Gp,start.Attempts,first.Late,first.Success,first.Boon,bountiful,catalog);
        // 0.515: the node's effects, and what else can change the plan without changing the node.
        var perception=GatheringPerception(start.JobId);
        var extraKey=$"{GpForbiddenFor(start.JobId)}|{first.Quantity}|{frame.NodeId}|{frame.MaxGp}|{rating}|{perception}";
        if(extraKey!=gatheringPanelExtraKey){gatheringPanelExtraKey=extraKey;gatheringPanelKey=null;}
        if(key!=gatheringPanelKey)
        {
            gatheringPanelEffectWith.Clear();
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
                    var scenario=new GpScenario(start.JobId,start.Level,(int)start.Gp,attempts,quantity,success,boon,bountiful ?? 1,MoreActions:true,Forbidden:GpForbiddenFor(start.JobId));
                    gatheringPanelPlan=GpOptimizer.Solve(scenario,catalog);
                    foreach(var effect in NodeEffects(frame))
                        if(GatherNodeEffects.Missing(effect,(int)frame.MaxGp,rating,perception)>0 && GatherNodeEffects.With(scenario,effect) is { } better)
                            gatheringPanelEffectWith[effect]=GpOptimizer.Solve(better,catalog).Best.ExpectedYield;
                }
                catch(ArgumentException){gatheringPanelPlan=null;gatheringPanelEffectWith.Clear();}
            }
        }
        ImGui.TextColored(Accent,localization.Format("GatheringPanel.PlanFor",items?.Find(gatheringPanelItem)?.Name ?? gatheringPanelItem.ToString()));
        var need=GatherListNeed(slot.ItemId);
        if(gatheringPanelPlan is not { } plan)
        {
            if(need>0)ImGui.TextColored(Accent,localization.Format("GatheringPanel.ListNeed",need.ToString("N0")));
            ImGui.TextDisabled(gatheringPanelLate ? localization.Get("GatheringPanel.AfterStart")
                :gatheringPanelMissing.Length>0 ? localization.Format("GatheringPanel.Missing",string.Join(", ",gatheringPanelMissing.Select(localization.Get)))
                :localization.Get("GatheringPanel.NoPlan"));
            return;
        }
        var shown=plan.Best;
        // 0.515 (user request): the crafting list only says what it still needs; the plan is for this node.
        if(need>0)ImGui.TextColored(Accent,shown.ExpectedYield>0 ? localization.Format("GatheringPanel.ListNeedNodes",need.ToString("N0"),Math.Ceiling(need/shown.ExpectedYield).ToString("N0")):localization.Format("GatheringPanel.ListNeed",need.ToString("N0")));
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

    /// <summary>0.519: an action's name, icon and description from the game.</summary>
    internal Func<uint,(string Name,uint Icon,string Description)?>? GatheringActionRow { get; set; }
    /// <summary>0.522: the seconds left on an action's recast; null: unknown.</summary>
    internal Func<uint,float?>? GatheringRecastLeft { get; set; }
    private void DrawGatheringCrystalPlan(GatheringObservedSlot slot,GatheringNodeStart start,ItemCatalog items)
    {
        ImGui.TextColored(Accent,localization.Format("GatheringPanel.PlanFor",items.Find(slot.ItemId)?.Name ?? slot.ItemId.ToString()));
        var need=GatherListNeed(slot.ItemId);
        if(need>0)ImGui.TextColored(Accent,localization.Format("GatheringPanel.ListNeed",need.ToString("N0")));
        // 0.522 (user report): an action still recasting is not advised, and its GP stays for the other; it says when it comes back.
        var cooling=GpCrystals.WithRecast(start.JobId).Select(id=>(Id:id,Left:GatheringRecastLeft?.Invoke(id))).Where(row=>row.Left>0).ToArray();
        var actions=GpCrystals.For(start.JobId,start.Level,(int)start.Gp,cooling.Select(row=>row.Id).ToArray());
        if(actions.Count==0 && cooling.Length==0){ImGui.TextDisabled(localization.Get("GatheringPanel.CrystalNone"));return;}
        foreach(var action in actions)
        {
            if(GatheringActionRow?.Invoke(action.Id) is not { } row)continue;
            DrawItemIcon(new ItemDefinition(0,row.Name,false,row.Icon),new Vector2(22));ImGui.SameLine();
            ImGui.TextUnformatted($"{row.Name} · {action.Cost} {localization.Get("GatheringPanel.CrystalGp")}");
            if(ImGui.IsItemHovered())WrappedTooltip(row.Description);
        }
        foreach(var (id,left) in cooling)
            if(GatheringActionRow?.Invoke(id) is { } row)
            {
                DrawItemIcon(new ItemDefinition(0,row.Name,false,row.Icon),new Vector2(22));ImGui.SameLine();
                ImGui.TextDisabled(localization.Format("GatheringPanel.CrystalWait",row.Name,Math.Ceiling(left ?? 0).ToString("0")));
                if(ImGui.IsItemHovered())WrappedTooltip(row.Description);
            }
        ImGui.TextDisabled(localization.Get("GatheringPanel.CrystalNote"));
    }

    private IReadOnlyList<GatherNodeEffect> NodeEffects(GatheringObservedFrame frame)=>frame.NodeId!=0 && nodeEffectsOf is { } read ? read(frame.NodeId):[];
    private int GatheringPerception(uint job)=>characterStats.State is var stats && stats.JobId==job && stats.Values.TryGetValue("Perception",out var known) ? (int)known:0;

    // 0.515 (user request): every effect of the node, in the game's words. Five attempts from the node are not an action that adds one: the plan already counts them, and the line says so.
    private void DrawGatheringNodeEffects(GatheringObservedFrame frame,int rating)
    {
        var perception=GatheringPerception(frame.JobId);
        foreach(var effect in NodeEffects(frame))
        {
            var missing=GatherNodeEffects.Missing(effect,(int)frame.MaxGp,rating,perception);
            if(missing is null)ImGui.TextDisabled(localization.Format("GatheringPanel.EffectUnknown",effect.Condition,effect.Text));
            else if(missing>0)
            {
                ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("GatheringPanel.EffectMissing",effect.Condition,effect.Text,missing));
                if(gatheringPanelEffectWith.TryGetValue(effect,out var with))ImGui.TextDisabled(localization.Format("GatheringPanel.EffectWith",with.ToString("0.0")));
            }
            else if(effect.Kind==GatherEffectKind.Attempts && frame.TotalAttempts is { } total)ImGui.TextColored(Success,localization.Format("GatheringPanel.EffectOnAttempts",effect.Text,total,total-effect.Value));
            else ImGui.TextColored(Success,localization.Format("GatheringPanel.EffectOn",effect.Text));
        }
    }
}
