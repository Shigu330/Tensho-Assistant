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
    private (uint Job,int Level,int Slot,uint Item,int Gp,int? Attempts,bool Late,int? Success,int? Boon,int? Bountiful,GpActionCatalog Catalog,bool Auto)? gatheringPanelKey;
    private GpOptimization? gatheringPanelPlan;private uint gatheringPanelItem;private string[] gatheringPanelMissing=[];private bool gatheringPanelLate,gatheringPanelBountifulFloor;
    private GatheringNodeStart? gatheringPanelStart;

    // 0.330 (audit 8): the node as it opened, before any GP spent at it (TrackGatheringNode).
    private sealed class GatheringNodeStart(ulong owner,GatheringObservedFrame frame)
    {
        public ulong Owner {get;}=owner;public uint JobId {get;}=frame.JobId;public int Level {get;}=frame.Level;public int? TotalAttempts {get;}=frame.TotalAttempts;
        public uint NodeId {get;}=frame.NodeId;
        // 0.831 (X-61): the attempts last read, to see a new node of the same kind open (they go up again).
        public int? LastAttempts {get;set;}=frame.Attempts;
        public uint Gp {get;set;}=frame.Gp;public int? Attempts {get;set;}=frame.Attempts;public bool Frozen {get;set;}
        // Success and boon of each slot (index, raw item) as first read; Late: first read after GP was spent at the node.
        public Dictionary<(int Index,uint RawItemId),(int? Success,int? Boon,bool Late,int? Quantity)> Slots {get;}=[];
    }

    /// <summary>0.560 (C24): what the last crafting chain still needs of this item; from the chain's state, nothing is computed again.</summary>
    private string? ChainNeed(uint item,Inventory.ItemHoldingsSummary stock)
    {
        if(!stock.HasObservation || craftChain.State is not { Result:{ } result,Input:{ } input } || result.Remaining(item,stock.Total) is not { } left || left<=0)return null;
        return localization.Format("GatheringPanel.ChainNeed",left.ToString("N0"),itemCatalog.State.Catalog?.Find(input.Recipe.ResultItemId)?.Name ?? input.Recipe.ResultItemId.ToString());
    }
    internal void DrawGatheringPanel()
    {
        DrawGatheringPanelCore(gatheringObservation.State);
    }

    /// <summary>0.751 (X-04): the rows of the game's gathering window, from the reader.</summary>
    internal Func<IReadOnlyList<ActionBarRect?>>? GatheringSlotRects { get; set; }
    /// <summary>0.753 (X-06): the game's « gather automatically » box; null when not shown.</summary>
    internal Func<bool?>? GatheringAutoGather { get; set; }
    private bool gatheringTrackedPicked;
    private static void DrawGatheringSlotFrame(ActionBarRect rect)
    {
        // 0.764 (X-17): under Tensho's windows, over the game.
        var draw=ImGui.GetBackgroundDrawList();
        var min=new Vector2(rect.X,rect.Y);var max=min+new Vector2(rect.Width,rect.Height);
        draw.AddRect(min,max,ImGui.GetColorU32(UI.FfxivTheme.Success),6,ImDrawFlags.None,3);
    }
    /// <summary>
    /// 0.751 (X-04, user request): what the tracking lists still need of an item: the crafting list chosen for the gathering plan, and the lists shown on screen (crafting list,
    /// workshop, preparation) as they were last worked out, within five seconds. The crafting list is counted once, the larger of its two readings.
    /// </summary>
    private long TrackedNeed(uint item)
    {
        var now=Environment.TickCount64;
        long Panel(string key)
        {
            // 0.764 (X-17): a folded panel still counts; a closed one does not.
            if(ListPanelLinesFor(key) is not { } known)return 0;
            var needed=known.Lines.Where(line=>line.Item==item).Sum(line=>line.Needed);
            if(needed<=0)return 0;
            var held=GetRecipeHoldings(item);
            return Math.Max(0,needed-(held.HasObservation ? held.Total:0));
        }
        return Math.Max(GatherListNeed(item),Panel("CraftList"))+Panel("Workshop")+Panel("Preparation");
    }

    // 0.728 (user request): a frame over the slot of the next action, as on the Mini Cactpot board. Drawn only: the player uses the action.
    internal Gathering.ActionBarLocator? BarLocator { get; set; }
    private (GpPlan Plan,int Gp,int? Attempts,int? Boon,GpNextStep Step)? gatheringNextCache;
    // 0.816: one golden frame over a part of a game window, under Tensho's windows.
    private static void DrawGameFrame(float x,float y,float width,float height)
    {
        var draw=ImGui.GetBackgroundDrawList();
        var pulse=0.65f+0.35f*MathF.Sin((float)(DateTime.UtcNow.TimeOfDay.TotalSeconds*5));
        var min=new Vector2(x,y);var max=min+new Vector2(width,height);
        draw.AddRectFilled(min,max,ImGui.GetColorU32(UI.FfxivTheme.Gold with {W=0.18f*pulse}),6);
        draw.AddRect(min,max,ImGui.GetColorU32(UI.FfxivTheme.Gold with {W=pulse}),6,ImDrawFlags.None,3);
    }
    private static void DrawGatheringBarHighlight(Gathering.ActionBarLocator located)
    {
        if(located.Rects is not { Count:>0 } rects)return;
        // 0.764 (X-17): under Tensho's windows, over the game.
        var draw=ImGui.GetBackgroundDrawList();
        var pulse=0.65f+0.35f*MathF.Sin((float)(DateTime.UtcNow.TimeOfDay.TotalSeconds*5));
        var border=ImGui.GetColorU32(UI.FfxivTheme.Gold with {W=pulse});
        var fill=ImGui.GetColorU32(UI.FfxivTheme.Gold with {W=0.18f*pulse});
        foreach(var rect in rects)
        {
            var min=new Vector2(rect.X,rect.Y);var max=min+new Vector2(rect.Width,rect.Height);
            draw.AddRectFilled(min,max,fill,6);draw.AddRect(min,max,border,6,ImDrawFlags.None,3);
        }
    }

    private void DrawGatheringNext(GpOptimization optimization,GatheringObservedSlot slot,GatheringNodeStart start,GatheringObservedFrame frame,string itemName,out GpNextStep? step)
    {
        step=null;
        if(start.Attempts is not { } startAttempts || frame.Attempts is not { } attempts)return;
        var plan=optimization.Best;
        if(gatheringNextCache is not { } known || !ReferenceEquals(known.Plan,plan) || known.Gp!=(int)frame.Gp || known.Attempts!=attempts || known.Boon!=slot.BoonPercent)
        {
            var found=GpNextAction.Next(plan,(int)start.Gp,(int)frame.Gp,startAttempts,attempts);
            gatheringNextCache=known=(plan,(int)frame.Gp,attempts,slot.BoonPercent,found);
            // 0.731 (user report): the end of the plan, with what is left; 0.747: solved off the frame, the plain step shown meanwhile.
            if(found is { Kind:GpNextKind.Gather,Finished:true } && !optimization.Scenario.AutoGather && slot.SuccessPercent is { } successNow && slot.BoonPercent is { } boonNow && gpActions.State.Catalog is { } catalog)
            {
                var opened=optimization.Scenario;var gp=(int)frame.Gp;
                gatheringLeftoverSolve=(known,Task.Run(()=>GpNextAction.Leftover(found,opened,plan,catalog,gp,attempts,successNow,boonNow)));
            }
        }
        if(gatheringLeftoverSolve is { Task.IsCompleted:true } leftover)
        {
            gatheringLeftoverSolve=null;
            if(leftover.Task.IsCompletedSuccessfully && Equals(leftover.Key,known))gatheringNextCache=known=known with {Step=leftover.Task.Result};
        }
        step=known.Step;
        switch(step.Kind)
        {
            case GpNextKind.Action when step.Action is { } action:
                DrawItemIcon(new ItemDefinition(0,action.Name,false,action.Icon),new Vector2(28));ImGui.SameLine();
                ImGui.TextColored(Accent,localization.Format(step.Leftover ? "GatheringPanel.NextLeftover":"GatheringPanel.NextAction",action.Name));
                if(ImGui.IsItemHovered())WrappedTooltip(action.Description);
                // 0.733 (user report): asked from here, inside the panel's own window, which is drawn after the overlays: the frame of 0.728 was cleared before it was looked for.
                if(configuration.GatheringNextHighlight && BarLocator is { } located)
                {
                    located.Want(action.Spec.Id);
                    if(located.ReadFor==action.Spec.Id)
                    {
                        if(located.Rects.Count==0)ImGui.TextDisabled(localization.Get("GatheringPanel.NextNotOnBars"));
                        else DrawGatheringBarHighlight(located);
                    }
                }
                break;
            case GpNextKind.Gather:ImGui.TextColored(Accent,localization.Format("GatheringPanel.NextGather",itemName));break;
            case GpNextKind.OffPlan:ImGui.TextDisabled(localization.Get("GatheringPanel.NextOffPlan"));break;
        }
    }

    private void DrawGatheringPanelCore(GatheringObservationState observed)
    {
        if(!windowDisposed && configuration.GatheringPanel && observed.Status==GatheringObservationStatus.Error)DrawReadFailedPanel("TenshoGatheringFailed","Gathering","Gathering");
        // 0.330: the node's opening values are forgotten when its window closes; a gather or an action only makes the reading « not ready ».
        if(windowDisposed || !configuration.GatheringPanel || observed.Status is not (GatheringObservationStatus.Observed or GatheringObservationStatus.NotReady))gatheringPanelStart=null;
        if(windowDisposed || !configuration.GatheringPanel || gatheringObservation.Window is not { } window
           || observed.Frame is not { } frame || !observed.Fresh(DateTimeOffset.UtcNow))return;
        TrackGatheringNode(observed.Owner,frame);
        var miner=frame.JobId==16;
        Panels?.Show("TenshoGatheringPanel",localization.Get(miner ? "GatheringPanel.TitleMiner":"GatheringPanel.TitleBotanist"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(window.X+window.Width+6,window.Y)+WindowMoved("Gathering",window.X,window.Y),ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(360,0),ImGuiCond.Always);},()=>
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
            // 0.756 (X-10, user request): once per node, as soon as its items are read, an item the tracking lists need is put in context first; a click still chooses another.
            if(!gatheringTrackedPicked && frame.Slots.Any(slot=>slot.ItemId!=0))
            {
                gatheringTrackedPicked=true;
                if(frame.Slots.Where(slot=>slot.ItemId!=0).OrderByDescending(slot=>TrackedNeed(slot.ItemId)).FirstOrDefault() is { } wanted && TrackedNeed(wanted.ItemId)>0)SelectItem(wanted.ItemId);
            }
            var chosen=frame.Slots.FirstOrDefault(slot=>slot.ItemId!=0 && slot.ItemId==(uint)Math.Max(0,itemId)) ?? frame.Slots.FirstOrDefault(slot=>slot.ItemId!=0);
            // 0.523 (user report): the scrips of a node's collectables, to choose between them.
            var scripRules=frame.Slots.Where(slot=>slot.ItemId!=0).Select(slot=>Collectables?.Catalog?.For(slot.ItemId).FirstOrDefault(rule=>rule.Scrips && rule.RewardHigh>0)).OfType<CollectableRule>().ToArray();
            var bestScrips=CollectableChoice.Best(scripRules);
            foreach(var slot in frame.Slots.Where(slot=>slot.ItemId!=0))
            {
                var item=items?.Find(slot.ItemId);
                ImGui.PushID(slot.Index);
                try
                {
                    DrawItemIcon(item,new Vector2(24));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                    var bountiful=bountifulBase?.Invoke(slot.ItemId) is { } baseValue ? BountifulEstimate.Bonus(rating,baseValue):null;
                    var stock=GetRecipeHoldings(slot.ItemId);
                    var needed=ChainNeed(slot.ItemId,stock);
                    // 0.478 (user report: lines cut at the panel's width): the name, then its rates under it.
                    var rates=$"{slot.SuccessPercent?.ToString() ?? "?"} % · {localization.Format("GatheringPanel.Boon",slot.BoonPercent?.ToString() ?? "?")}"
                        +(bountiful is { } bonus ? " · "+localization.Format("GatheringPanel.Bountiful",bonus):"")
                        +(stock.HasObservation ? " · ×"+stock.Total.ToString("N0"):"");
                    ImGui.BeginGroup();
                    if(ImGui.Selectable((item?.Name ?? slot.ItemId.ToString())+"##Slot",ReferenceEquals(slot,chosen)) || clicked)SelectItem(slot.ItemId);
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(slot.ItemId);
                    ItemLinksMenu($"GatheringSlot{slot.ItemId}",item);
                    if(needed is { } need)ImGui.TextColored(Accent,need);
                    // 0.751 (X-04, user request): what the tracking lists (crafting list, workshop, preparation) still need of this item; framed on the game's window too.
                    if(TrackedNeed(slot.ItemId) is > 0 and var tracked)
                    {
                        ImGui.TextColored(Accent,localization.Format("GatheringPanel.TrackedNeed",tracked.ToString("N0")));
                        if(configuration.GatheringListHighlight && GatheringSlotRects?.Invoke() is { } rects && slot.Index<rects.Count && rects[slot.Index] is { } rect)
                            DrawGatheringSlotFrame(rect);
                    }
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(slot.ItemId);
                    ImGui.TextDisabled(rates);
                    if(scripRules.FirstOrDefault(rule=>rule.ItemId==slot.ItemId) is { } paid)
                    {
                        var scrips=localization.Format("CollectablePanel.Scrips",paid.RewardLow,paid.RewardMid,paid.RewardHigh);
                        if(bestScrips==slot.ItemId)ImGui.TextColored(Accent,localization.Format("GatheringPanel.ScripsBest",scrips));
                        else ImGui.TextUnformatted(scrips);
                    }
                    ImGui.EndGroup();
                }
                finally{ImGui.PopID();}
            }
            ImGui.Separator();
            ImGui.PushTextWrapPos(0);
            try
            {
                if(CollectableChoice.MixedKinds(scripRules))ImGui.TextDisabled(localization.Get("GatheringPanel.ScripsKinds"));
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
           || frame.Slots.Count>0 && !frame.Slots.Any(slot=>start.Slots.ContainsKey((slot.Index,slot.RawItemId)))
           // 0.831 (X-61, user report: « read after GP was spent » with 4/4 attempts): the next node of the same kind shares its slots, and its window can open
           // before a closed one is read; another node row, or attempts going up again, is a new node, and nothing spent at the last one freezes it.
           || GatheringNodes.IsNew(start.NodeId,start.LastAttempts,frame.NodeId,frame.Attempts))
        {
            gatheringPanelStart=start=new(owner,frame);
            gatheringTrackedPicked=false;
        }
        start.LastAttempts=frame.Attempts ?? start.LastAttempts;
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
    private (object Key,Task<(GpOptimization Plan,Dictionary<GatherNodeEffect,decimal> With)> Task)? gatheringPanelSolve;
    private (object Key,Task<GpNextStep> Task)? gatheringLeftoverSolve;
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
        var auto=GatheringAutoGather?.Invoke()==true;
        var key=(start.JobId,start.Level,slot.Index,slot.ItemId,(int)start.Gp,start.Attempts,first.Late,first.Success,first.Boon,bountiful,catalog,auto);
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
                // 0.479: the quantity the game shows per gather; a node where it was not read counts one.
                var quantity=Math.Clamp(first.Quantity ?? 1,1,20);
                var scenario=new GpScenario(start.JobId,start.Level,(int)start.Gp,attempts,quantity,success,boon,bountiful ?? 1,MoreActions:true,Forbidden:GpForbiddenFor(start.JobId),AutoGather:auto);
                var effects=NodeEffects(frame).Where(effect=>GatherNodeEffects.Missing(effect,(int)frame.MaxGp,rating,perception)>0)
                    .Select(effect=>(Effect:effect,Better:GatherNodeEffects.With(scenario,effect))).Where(pair=>pair.Better is not null).ToArray();
                // 0.747 (X-01): solved off the frame: the slow-frame log of 0.745 measured this panel at 97 ms on the frame a node opened.
                gatheringPanelSolve=(key,Task.Run(()=>
                {
                    var solved=GpOptimizer.Solve(scenario,catalog);
                    var with=effects.ToDictionary(pair=>pair.Effect,pair=>GpOptimizer.Solve(pair.Better!,catalog).Best.ExpectedYield);
                    return (solved,with);
                }));
            }
        }
        if(gatheringPanelSolve is { Task.IsCompleted:true } solving)
        {
            gatheringPanelSolve=null;
            if(Equals(solving.Key,gatheringPanelKey))
            {
                gatheringPanelEffectWith.Clear();
                if(solving.Task.IsCompletedSuccessfully)
                {
                    gatheringPanelPlan=solving.Task.Result.Plan;
                    foreach(var (effect,yieldWith) in solving.Task.Result.With)gatheringPanelEffectWith[effect]=yieldWith;
                }
                else gatheringPanelPlan=null;
            }
        }
        ImGui.TextColored(Accent,localization.Format("GatheringPanel.PlanFor",items?.Find(gatheringPanelItem)?.Name ?? gatheringPanelItem.ToString()));
        var need=GatherListNeed(slot.ItemId);
        if(gatheringPanelPlan is not { } plan)
        {
            if(need>0)ImGui.TextColored(Accent,localization.Format("GatheringPanel.ListNeed",need.ToString("N0")));
            ImGui.TextDisabled(gatheringPanelSolve is not null && Equals(gatheringPanelSolve.Value.Key,gatheringPanelKey) ? localization.Get("GatheringPanel.Solving")
                :gatheringPanelLate ? localization.Get("GatheringPanel.AfterStart")
                :gatheringPanelMissing.Length>0 ? localization.Format("GatheringPanel.Missing",string.Join(", ",gatheringPanelMissing.Select(localization.Get)))
                :localization.Get("GatheringPanel.NoPlan"));
            return;
        }
        var shown=plan.Best;
        // 0.515 (user request): the crafting list only says what it still needs; the plan is for this node.
        if(need>0)ImGui.TextColored(Accent,shown.ExpectedYield>0 ? localization.Format("GatheringPanel.ListNeedNodes",need.ToString("N0"),Math.Ceiling(need/shown.ExpectedYield).ToString("N0")):localization.Format("GatheringPanel.ListNeed",need.ToString("N0")));
        ImGui.TextDisabled(localization.Format("GatheringPanel.Expected",shown.ExpectedYield.ToString("0.0"),plan.Baseline.ToString("0.0"),shown.Cost));
        if(start.Frozen)ImGui.TextDisabled(localization.Format("GatheringPanel.PlanAtStart",plan.Scenario.Budget));
        if(plan.Scenario.AutoGather)ImGui.TextColored(Accent,localization.Get("GatheringPanel.AutoGatherPlan"));
        if(gatheringPanelBountifulFloor)ImGui.TextDisabled(localization.Get("GatheringPanel.BountifulFloor"));
        DrawGatheringNext(plan,slot,start,frame,items?.Find(gatheringPanelItem)?.Name ?? gatheringPanelItem.ToString(),out var next);
        // 0.729 (user request): the next action apart from the plan.
        if(next is { Kind:not GpNextKind.Done })ImGui.Separator();
        if(shown.Actions.Count==0)ImGui.TextUnformatted(localization.Get("GpUi.Keep"));
        foreach(var use in shown.Actions)
        {
            DrawItemIcon(new ItemDefinition(0,use.Action.Name,false,use.Action.Icon),new Vector2(22));ImGui.SameLine();
            var spent=next?.Used.GetValueOrDefault(use.Action.Spec.Id) ?? 0;
            var label=use.Count>1 ? $"{use.Action.Name} ×{use.Count}":use.Action.Name;
            if(spent>0 && use.Count>1 && spent<use.Count)label+=$" ({spent}/{use.Count})";
            if(spent>=use.Count)ImGui.TextDisabled(label);
            else if(next?.Action?.Spec.Id==use.Action.Spec.Id)ImGui.TextColored(Accent,label);
            else ImGui.TextUnformatted(label);
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
