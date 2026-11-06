using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Craft simulator page: recipe chosen from the item sheet (expert, level-scaled, specialist and required-quality recipes refused),
// attributes typed or copied from the current job, the action sequence (Action / CraftAction sheets), « Simulate » on click
// (Craft/CraftSimulation.cs, 100 actions at most), step table, condition range, macro export as text pages and the copy into the game's
// macros (0.191: Macros/MacroWriter writes on the framework thread on the player's click; chained pages become the Macro chain plan,
// 0.196), presets and the bounded search (MainWindow.CraftSolver.cs).
internal sealed partial class MainWindow
{
    private RecipeDefinition? simulationRecipe;
    private readonly List<CraftActionKind> simulationActions=[];
    private int simulationLevel=100,simulationCs,simulationControl,simulationCp,simulationQuality;
    private bool simulationManipulation;
    private ulong simulationCopiedOwner;
    private DateTimeOffset? simulationCopiedAt;
    private CraftSimulationResult? simulationResult;
    // 0.553 (C4): the attributes a finished sequence needs, off the frame; only shown for the result they were computed from.
    private (CraftSimulationResult Result,Task<CraftThresholds?> Task)? simulationThresholds;
    private CancellationTokenSource? simulationThresholdsCancel;
    private void StartSimulationThresholds(RecipeDefinition recipe,CraftStats stats,int quality,CraftSimulationResult result)
    {
        simulationThresholdsCancel?.Cancel();simulationThresholdsCancel?.Dispose();
        var cancel=simulationThresholdsCancel=new CancellationTokenSource();var token=cancel.Token;var actions=simulationActions.ToArray();
        simulationThresholds=(result,Task.Run(()=>
        {
            try{return CraftStatThresholds.Find(recipe,stats,quality,actions,token);}
            catch(OperationCanceledException){return null;}
            catch(Exception error){log.Warning(error,"The sequence's thresholds could not be computed.");return null;}
        },token));
    }
    private string? simulationError;
    private CraftSimulationResult? macroExportSimulation;
    private CraftMacroExport? craftMacroExport;
    private TenshoAssistant.Macros.MacroWriter? macroWriter;
    internal void ConfigureMacroWriter(TenshoAssistant.Macros.MacroWriter writer)=>macroWriter=writer;
    private string? macroChainApplied;
    // 0.330: what « Start the macro chain » may run. The click remembers the character and the page texts it sends; only a « Done »
    // result whose plan is saved turns them into the written chain, any other result (Writing, NoRoom, Failed, failed save) clears it.
    private (ulong Owner,string[] Texts)? requestedChain;
    private (ulong Owner,string[] Texts,List<TenshoAssistant.Macros.MacroSlot> Plan)? writtenChain;
    // After a chained copy (0.196): the written slots become the Macro Chain plan once, then a button starts the chain.
    private void ApplyWrittenChain()
    {
        if(macroWriter?.Result is not { } written || written==macroChainApplied)return;
        macroChainApplied=written;
        // 0.330 review: a copy made with « Chain pages » off is consumed here and never becomes the saved chain plan later.
        if(configuration.MacroCopy is not { Chain:true }){writtenChain=null;return;}
        var parts=written.Split(':');
        if(parts is not ["Done",_,_,var bookText,var list] || !uint.TryParse(bookText,out var book)){writtenChain=null;return;}
        var slots=list.Split(',').Select(uint.Parse).ToArray();
        var plan=slots.Select((index,page)=>new TenshoAssistant.Macros.MacroSlot(book,index,TenshoAssistant.Macros.MacroSlotPlan.Name(configuration.MacroCopy.Prefix,page+1,slots.Length))).ToList();
        var previous=configuration.MacroChainPlan;
        configuration.MacroChainPlan=plan;
        try
        {
            save();macroPlanLoaded=false;macroChain.RequestRefresh();
            writtenChain=requestedChain is { } request && request.Texts.Length==slots.Length ? (request.Owner,request.Texts,plan):null;
        }
        catch(Exception error){configuration.MacroChainPlan=previous;writtenChain=null;log.Warning(error,"Macro chain plan could not be saved.");}
    }
    // 0.191: write the pages into the game's macros, inside the range set in Settings (as Craftimizer does); 0.192: also in the craft log panel.
    private void DrawMacroToGame(IReadOnlyList<CraftMacroPage> pages,bool small)
    {
        var copy=configuration.MacroCopy ?? new();
        var where=localization.Format("CraftMacro.Range",localization.Get(copy.Book==1 ? "CraftMacro.Shared":"CraftMacro.Individual"),copy.Start,copy.End);
        // Narrow craft log panel (0.195): short label, the range in the tooltip; the prediction wraps.
        var label=(small ? localization.Get("CraftMacro.ToGameShort"):localization.Get("CraftMacro.ToGame")+$" ({where})")+"##MacroToGame";
        if((small ? ImGui.SmallButton(label):ImGui.Button(label)) && macroWriter is not null)
        {
            requestedChain=(macroChain.State.CharacterId,pages.Select(page=>page.Text).ToArray());writtenChain=null;macroChainApplied=null;
            macroWriter.Request(pages.Select(page=>page.Lines).ToArray(),copy);
        }
        if(ImGui.IsItemHovered())WrappedTooltip(where+"\n"+localization.Get(copy.OverwriteAny ? "CraftMacro.ToGameAny":"CraftMacro.ToGameSafe"));
        // 0.194: prediction before the click, same rule as the copy, from the slot states copied each second.
        ApplyWrittenChain();
        // 0.330: only under the very pages of the last chained copy, for the character it wrote to, while the saved plan is still the one
        // that copy wrote (another recipe, a later failed copy or a plan saved on the Macro sequences page hides it).
        if(copy.Chain && writtenChain is { } chain && chain.Owner!=0 && chain.Owner==macroChain.State.CharacterId
            && ReferenceEquals(configuration.MacroChainPlan,chain.Plan) && pages.Select(page=>page.Text).SequenceEqual(chain.Texts))
        {
            if(ImGui.SmallButton(localization.Get("CraftMacro.StartChain")+"##MacroChainStart"))macroChain.RequestStart(chain.Plan);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("CraftMacro.StartChainHelp"));
        }
        if(macroWriter?.SlotsOf(copy.Book) is { } states)
        {
            var plan=TenshoAssistant.Macros.MacroSlotPlan.Plan(pages.Count,copy,index=>states[index]);
            ImGui.PushStyleColor(ImGuiCol.Text,plan.Problem is null ? Success:Accent);
            try
            {
                ImGui.TextWrapped(plan.Problem is null
                    ? localization.Format("CraftMacro.WillUse",plan.Slots[0],plan.Slots[^1],plan.Slots.Count,string.Join(", ",plan.Slots))
                    :localization.Format("CraftMacro.Will"+plan.Problem,pages.Count));
            }
            finally{ImGui.PopStyleColor();}
        }
        else ImGui.TextDisabled(localization.Get("CraftMacro.WillUnknown"));
    }
    private string? craftMacroError;
    // 0.572 (C15): once per change, never at each frame; an error stays until something changes.
    private string simulationAutoKey="";
    private bool SimulationAutoDue()
    {
        if(!CraftSearchOptions.AutoSimulate || simulationResult is not null || simulationActions.Count==0)return false;
        var key=$"{simulationRecipe?.RecipeId}|{EffectiveCraftStats()}|{simulationQuality}|{string.Join(',',simulationActions)}";
        if(key==simulationAutoKey)return false;
        simulationAutoKey=key;
        return true;
    }
    // 0.572 (C15): a recipe chosen from this page; the list is rebuilt only when the text changes.
    private string simulationRecipeQuery="",simulationRecipeShown="";private RecipeDefinition[] simulationRecipeHits=[];
    private void DrawSimulationRecipeSearch()
    {
        ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("##SimulationRecipeSearch",localization.Get("CraftSimulation.RecipeSearch"),ref simulationRecipeQuery,60);
        Tip("CraftSimulation.RecipeSearchTip");
        if(simulationRecipeQuery!=simulationRecipeShown)
        {
            simulationRecipeShown=simulationRecipeQuery;
            var items=itemCatalog.State.Catalog;var recipes=recipeCatalog.State.Catalog;
            simulationRecipeHits=simulationRecipeQuery.Trim().Length<2 || items is null || recipes is null ? []
                :items.Search(simulationRecipeQuery,100).SelectMany(item=>recipes.ForResult(item.Id)).Take(12).ToArray();
        }
        foreach(var hit in simulationRecipeHits)
            if(ImGui.Selectable($"{itemCatalog.State.Catalog?.Find(hit.ResultItemId)?.Name ?? hit.ResultItemId.ToString()} · {hit.CraftType}##SimulationRecipe{hit.RecipeId}",hit.RecipeId==simulationRecipe?.RecipeId))
            {OpenCraftSimulation(hit);simulationRecipeQuery="";simulationRecipeShown="";simulationRecipeHits=[];}
    }
    private void OpenCraftSimulation(RecipeDefinition recipe)
    {
        if(simulationRecipe?.RecipeId!=recipe.RecipeId)
        {
            simulationActions.Clear(); simulationResult=null; simulationError=null; simulationQuality=0;
            simulationCopiedAt=null; simulationCopiedOwner=0; simulationCs=simulationControl=simulationCp=0;
            simulationLevel=100; simulationManipulation=false;
            craftSolveTarget=recipe.Simulation?.Quality ?? 0;
        }
        simulationRecipe=recipe; SelectItem(recipe.ResultItemId); page=Page.CraftSimulation; focusCraftPage=CraftPageTab.Simulation;
    }
    private void ValidateCraftCopy()
    {
        if(simulationCopiedOwner==0 || simulationCopiedOwner==characterStats.State.CharacterId) return;
        simulationCopiedOwner=0; simulationCopiedAt=null; simulationCs=simulationControl=simulationCp=0;
        simulationLevel=1; simulationResult=null; simulationManipulation=false;
        simulationError=localization.Get("SimulationHelp.Changed");
    }
    private enum CraftPageTab { None, Simulation, Preparation }
    private CraftPageTab focusCraftPage;
    private void DrawCraftSimulation()
    {
        Heading(localization.Get("CraftSimulation.Title"),localization.Get("CraftSimulation.Description"));
        if(ImGui.SmallButton(localization.Get("CraftSimulation.Live"))) page=Page.CraftLive;
        // 0.424 (user report): « Back to the item » — the recipe's item sheet, Craft tab.
        if(ImGui.SmallButton(localization.Get("CraftSimulation.Notebook"))) { if(simulationRecipe is { } shown)SelectItem(shown.ResultItemId); page=Page.Items; focusCraftTab=true; }
        // 0.423 (user report): with no recipe chosen, the item in context's own recipe (its first one) is taken, on Preparation.
        if(simulationRecipe is null && itemId>0 && recipeCatalog.State.Catalog?.ForResult((uint)itemId) is { Count:>0 } own)OpenCraftPreparation(PreferredRecipe(own));
        // 0.408 (user request): the full preparation (quantities, cost, sub-recipe chain) is a tab of this page; the item sheet keeps
        // a short view with a link here.
        // 0.464 (user request, in-game campaign): the crafting list is a tab too, with or without a recipe shown.
        var preparation=false;var listsTab=false;var stockTab=false;
        if(ImGui.BeginTabBar("CraftPageTabs"))
        {
            try
            {
                if(ImGui.BeginTabItem(localization.Get("CraftSimulation.SimulationTab")+"###CraftSimulationTab",focusCraftPage==CraftPageTab.Simulation ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None))
                    ImGui.EndTabItem();
                if(ImGui.BeginTabItem(localization.Get("CraftSimulation.PreparationTab")+"###CraftPreparationTab",focusCraftPage==CraftPageTab.Preparation ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None))
                { preparation=true; ImGui.EndTabItem(); }
                if(ImGui.BeginTabItem(localization.Get("CraftSimulation.ListsTab")+"###CraftListsTab"))
                { listsTab=true; ImGui.EndTabItem(); }
                if(ImGui.BeginTabItem(localization.Get("CraftSimulation.StockTab")+"###CraftStockTab"))
                { stockTab=true; ImGui.EndTabItem(); }
            }
            finally { ImGui.EndTabBar(); }
        }
        focusCraftPage=CraftPageTab.None;
        if(listsTab) { DrawCraftLists(); return; }
        if(stockTab) { DrawCraftFromStock(); return; }
        DrawSimulationRecipeSearch();
        if(simulationRecipe is not { } recipe) { ImGui.TextWrapped(localization.Get("CraftSimulation.ChooseRecipe")); return; }
        var resultItem=itemCatalog.State.Catalog?.Find(recipe.ResultItemId);
        DrawItemIcon(resultItem,new Vector2(36)); ImGui.SameLine();
        ImGui.TextColored(Accent,localization.Format("SimulationHelp.Recipe",resultItem?.Name ?? localization.Format("BankUi.Item",recipe.ResultItemId),recipe.CraftType,recipe.Level));
        if(developerMode)ImGui.TextDisabled($"ID {recipe.RecipeId}");
        // 0.405 (user request): this recipe in the game's crafting log.
        if(ImGui.SmallButton(localization.Get("GameLogs.Recipe")))OpenGameRecipe(recipe);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("GameLogs.RecipeTip"));
        // A-12 (0.427): level, Craftsmanship and Control against the recipe; the job's gear set on click.
        DrawCraftReadiness(recipe);
        if(preparation) { DrawCraftPlan(recipe); return; }
        if(CraftSimulation.Unsupported(recipe) is { } unsupported) { ImGui.TextWrapped(CraftMessage(unsupported)); return; }
        if(craftActions.State.Catalog is not { } catalog) { if(craftActions.State.Error is { } failure) DrawDataError(failure,"ServiceErrors.Craft"); else ImGui.TextWrapped(localization.Get("SimulationHelp.Loading")); return; }
        var p=recipe.Simulation!; var job=recipe.CraftTypeId+8;
        if(p.Collectable)ImGui.TextColored(Accent,localization.Get("SimulationHelp.Collectable"));
        // A-07 (0.435): expert recipes simulated in the normal condition, said so.
        if(p.Expert)ImGui.TextColored(Accent,localization.Get("CraftSimulation.ExpertNormal"));
        // C6 (0.666): what these recipes ask, as the data gives it.
        if(p.LevelScaled)ImGui.TextColored(Accent,localization.Get("CraftSimulation.LevelScaled"));
        if(p.RequiredQuality>0)ImGui.TextColored(Accent,localization.Format("CraftSimulation.RequiredQuality",p.RequiredQuality.ToString("N0")));
        if(p.MissionHigh>0)ImGui.TextColored(Accent,localization.Format("CraftSimulation.Mission",string.Join(" / ",new[]{p.MissionLow,p.MissionMid,p.MissionHigh}.Where(threshold=>threshold>0).Select(threshold=>threshold.ToString("N0")))));
        ImGui.TextDisabled(localization.Format("SimulationHelp.Parameters",p.Progress,p.Quality,p.Durability,p.RequiredCraftsmanship,p.RequiredControl));
        ImGui.TextWrapped(localization.Get("SimulationHelp.Model"));
        var stats=characterStats.State;
        var canCopy=stats.CharacterId!=0 && stats.JobId==job && TenshoAssistant.Caching.CacheAge.Fresh(stats.ObservedAt,DateTimeOffset.UtcNow,TimeSpan.FromSeconds(5))
            && stats.Values.ContainsKey("Craftsmanship") && stats.Values.ContainsKey("Control") && stats.Values.ContainsKey("CraftingPoints");
        void CopyStats()
        {
            simulationLevel=stats.Level; simulationCs=stats.Values["Craftsmanship"]; simulationControl=stats.Values["Control"]; simulationCp=stats.Values["CraftingPoints"];
            simulationCopiedOwner=stats.CharacterId; simulationCopiedAt=stats.ObservedAt; simulationResult=null; simulationError=null;
        }
        DetectCraftState(recipe,job,canCopy,CopyStats);
        // 0.554 (C3): the job is not worn: its attributes as last read.
        if(!canCopy && stats.CharacterId!=0 && RememberedCraftStats(stats.CharacterId,job) is { } seenStats)
        {
            if(ImGui.SmallButton(localization.Format("CraftSimulation.UseRemembered",seenStats.SeenAt.ToLocalTime().ToString("g"))))
            {
                simulationLevel=seenStats.Level;simulationCs=seenStats.Craftsmanship;simulationControl=seenStats.Control;simulationCp=seenStats.Cp;
                simulationCopiedOwner=0;simulationCopiedAt=null;simulationResult=null;simulationError=null;
            }
            Tip("CraftSimulation.UseRememberedTip");
        }
        ImGui.BeginDisabled(!canCopy);
        try
        {
            if(ImGui.SmallButton(localization.Get("CraftSimulation.CopyStats")))CopyStats();
        }
        finally { ImGui.EndDisabled(); }
        DrawCraftDetection(canCopy);
        if(simulationCopiedAt is { } copiedAt) ImGui.TextDisabled(localization.Format("SimulationHelp.Copied",copiedAt.ToLocalTime().ToString("T")));
        var changed=false;
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftSimulation.Attributes")+"###SimulationAttributes",ImGuiTreeNodeFlags.DefaultOpen))
        {
            // 0.464 (user request, in-game campaign): green or red against the recipe.
            changed|=CraftInput(localization.Get("CraftSimulation.Level")+"###SimLevel",ref simulationLevel,simulationLevel>=recipe.Level,"CraftSimulation.LevelTip");
            changed|=CraftInput(localization.Get("CraftSimulation.Craftsmanship")+"###SimCraftsmanship",ref simulationCs,p.RequiredCraftsmanship>0 ? simulationCs>=p.RequiredCraftsmanship:null,"CraftSimulation.CraftsmanshipTip");
            changed|=CraftInput(localization.Get("CraftSimulation.Control")+"###SimControl",ref simulationControl,p.RequiredControl>0 ? simulationControl>=p.RequiredControl:null,"CraftSimulation.ControlTip");
            ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Get("CraftSimulation.Cp")+"###SimCp",ref simulationCp,0); Tip("CraftSimulation.CpTip");
            ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Format("CraftSimulation.StartingQuality",p.MaxStartingQuality)+"###SimQuality",ref simulationQuality,0); Tip("CraftSimulation.StartingQualityTip");
            changed|=ImGui.Checkbox(localization.Get("CraftSimulation.Manipulation")+"###SimManipulation",ref simulationManipulation); Tip("CraftSimulation.ManipulationTip");
            // A-13 (0.434): specialist job.
            changed|=ImGui.Checkbox(localization.Get("CraftSimulation.Specialist")+"###SimSpecialist",ref simulationSpecialist); Tip("CraftSimulation.SpecialistTip");
            // A-02 (0.420): food and medicine added to the attributes.
            if(DrawCraftConsumables()){simulationResult=null;simulationError=null;}
        }
        if(changed) { simulationCopiedOwner=0; simulationCopiedAt=null; simulationResult=null; simulationError=null; }
        DrawCraftSearch();
        // A-08 (0.425): a pasted macro as the sequence.
        DrawCraftMacroImport(catalog,job);
        // A-14 (0.436): a rotation played by hand, recorded.
        DrawCraftRecorder(catalog,job);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftSimulation.AddAction")+"###SimAddAction",ImGuiTreeNodeFlags.DefaultOpen))
        {
            var columns=Math.Max(1,(int)(ImGui.GetContentRegionAvail().X/175));
            if(ImGui.BeginTable("CraftActionPalette",columns,ImGuiTableFlags.SizingStretchSame))
            {
                try
                {
                    foreach(var spec in CraftActionCatalog.Specs)
                    {
                        var action=catalog.Find(job,spec.Kind); if(action is null) continue;
                        ImGui.TableNextColumn(); ImGui.PushID((int)spec.Kind);
                        try
                        {
                            // 0.438 (user request): forbidden actions greyed out (Settings › Crafting).
                            var forbidden=CraftActionRules.Forbidden(craftForbidden,spec.Kind);
                            ImGui.BeginDisabled(forbidden || simulationActions.Count>=100 || simulationLevel<spec.Level || spec.Kind==CraftActionKind.Manipulation && !simulationManipulation);
                            try
                            {
                                DrawItemIcon(new ItemDefinition(0,action.Name,false,action.Icon),new Vector2(26)); var click=ImGui.IsItemClicked();
                                if(ImGui.IsItemHovered()) WrappedTooltip(action.Description);
                                ImGui.SameLine(); click|=ImGui.SmallButton(action.Name);
                                if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) WrappedTooltip(forbidden ? localization.Get("CraftRules.ForbiddenTip"):localization.Format("SimulationHelp.Action",action.Description,spec.Level,spec.Cp));
                                if(click && !forbidden && simulationActions.Count<100 && simulationLevel>=spec.Level && (spec.Kind!=CraftActionKind.Manipulation || simulationManipulation))
                                { simulationActions.Add(spec.Kind); simulationResult=null; }
                            }
                            finally { ImGui.EndDisabled(); }
                        }
                        finally { ImGui.PopID(); }
                    }
                }
                finally { ImGui.EndTable(); }
            }
        }
        // 0.464 (user request, in-game campaign): saving the sequence comes after adding its actions.
        DrawCraftPresetSave();
        ImGui.TextColored(Accent,localization.Format("CraftSequence.Count",simulationActions.Count));
        if(ImGui.SmallButton(localization.Get("CraftSequence.Clear"))) { simulationActions.Clear(); simulationResult=null; simulationError=null; }
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("CraftSequence.Simulate")) || SimulationAutoDue())
        {
            try { simulationResult=CraftSimulation.Run(recipe,EffectiveCraftStats(),simulationQuality,simulationActions); StartSimulationThresholds(recipe,EffectiveCraftStats(),simulationQuality,simulationResult); simulationError=null; }
            catch(ArgumentException error)
            {
                simulationResult=null;
                // 0.434: a known reason (specialist recipe…) is shown as such; the others keep the generic message.
                simulationError=CraftMessageKey(error.Message) is not null ? error.Message
                    : localization.Get(error.ParamName=="stats" ? "SimulationHelp.InvalidStats" : error.ParamName=="sequence" ? "CraftMessages.ActionLimit" : "ServiceErrors.Craft");
                log.Warning(error,"Craft simulation rejected its input.");
            }
        }
        if(simulationError is { } errorText) ImGui.TextWrapped(CraftMessage(errorText));
        if(simulationResult is { } result)
        {
            var state=result.Final;
            ImGui.TextColored(state.Finish==CraftFinish.Completed ? Success:Accent,localization.Format("CraftSequence.Result",localization.Get(state.Finish switch {CraftFinish.Completed=>"CraftSequence.Completed",CraftFinish.Broken=>"CraftSequence.Broken",_=>"CraftSequence.Incomplete"}),state.Progress,p.Progress,state.Quality,p.Quality,state.Durability,state.Cp));
            if(result.InvalidAction is { } invalid) ImGui.TextWrapped(localization.Format("SimulationHelp.Invalid",invalid+1,CraftMessage(result.Error ?? "—")));
            // A-13 (0.434): success assumed for the actions left to chance.
            if(result.Steps.Any(step=>CraftSimulation.Random(step.Action))) ImGui.TextColored(Accent,localization.Get("CraftSimulation.RandomAssumed"));
            // 0.438 (user request): a sequence (imported, recorded, saved) with a forbidden action is said so.
            if(CraftActionRules.UsesForbidden(simulationActions,craftForbidden)) ImGui.TextColored(Accent,localization.Get("CraftRules.SequenceUsesForbidden"));
            if(result.Steps.Count<result.PlannedActions) ImGui.TextDisabled(localization.Format("CraftSequence.Skipped",result.PlannedActions-result.Steps.Count));
            DrawCraftConditionEnvelope(result);
            DrawCraftMacroExport(result,job,catalog);
        }
        if(ImGui.BeginTable("CraftSequence",6,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                foreach(var title in new[]{"Action","Progress","Quality","Durability","Cost","InnerQuiet"}) ImGui.TableSetupColumn(localization.Get("CraftSequence."+title));
                ImGui.TableHeadersRow();
                for(var i=0;i<simulationActions.Count;i++)
                {
                    var kind=simulationActions[i]; var metadata=catalog.Find(job,kind); var step=simulationResult?.Steps.ElementAtOrDefault(i);
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.PushID(i);
                    try
                    {
                        DrawItemIcon(metadata is null ? null:new ItemDefinition(0,metadata.Name,false,metadata.Icon),new Vector2(24)); ImGui.SameLine();
                        ImGui.TextWrapped($"{i+1}. {metadata?.Name ?? kind.ToString()}");
                        if(ImGui.IsItemHovered() && step is not null) WrappedTooltip(localization.Format("SimulationHelp.Effects",step.After.Veneration,step.After.Innovation,step.After.GreatStrides,step.After.WasteNot,step.After.Manipulation,step.After.MuscleMemory,localization.Get(step.After.TrainedPerfection ? "SimulationHelp.Protected":step.After.UsedTrainedPerfection ? "SimulationHelp.Used":"SimulationHelp.Available")));
                        if(ImGui.SmallButton(localization.Get("CraftPresets.Remove"))) { simulationActions.RemoveAt(i); simulationResult=null; break; }
                        if(i>0) { ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("CraftSequence.Up"))) { (simulationActions[i-1],simulationActions[i])=(simulationActions[i],simulationActions[i-1]); simulationResult=null; break; } }
                        if(i+1<simulationActions.Count) { ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("CraftSequence.Down"))) { (simulationActions[i+1],simulationActions[i])=(simulationActions[i],simulationActions[i+1]); simulationResult=null; break; } }
                    }
                    finally { ImGui.PopID(); }
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step is null ? "—":$"{step.After.Progress} (+{step.ProgressGain})");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step is null ? "—":$"{step.After.Quality} (+{step.QualityGain})");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step?.After.Durability.ToString() ?? "—");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step is null ? "—":$"{step.After.Cp} (-{step.CpCost})");
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(step?.After.InnerQuiet.ToString() ?? "—");
                }
            }
            finally { ImGui.EndTable(); }
        }
    }

    private void DrawCraftMacroExport(CraftSimulationResult result,uint job,CraftActionCatalog catalog)
    {
        if(!ReferenceEquals(macroExportSimulation,result)) { craftMacroExport=null; craftMacroError=null; macroExportSimulation=result; }
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftMacro.Title")+"###CraftMacroExport")) return;
        ImGui.TextWrapped(localization.Get("CraftMacro.Help"));
        DrawToolSettingsLink("CraftMacroExport");
        ImGui.TextDisabled(localization.Format("CraftMacro.Options",craftMacroExtraWait,localization.Get(craftMacroEcho ? "CraftMacro.Yes":"CraftMacro.No")));
        // A-10 (0.422): Macro Chain loaded and pages chained: they end with its /nextmacro.
        if(configuration.MacroCopy?.Chain==true && MacroChainLoaded?.Invoke()==true)ImGui.TextDisabled(localization.Get("CraftMacro.MacroChain"));
        if(simulationThresholds is { } thresholds && ReferenceEquals(thresholds.Result,result) && thresholds.Task.IsCompletedSuccessfully && thresholds.Task.Result is { } needed)
            ImGui.TextWrapped(localization.Format("CraftSimulation.Thresholds",needed.Craftsmanship,needed.Control,needed.Cp,needed.CraftsmanshipMargin,needed.ControlMargin,needed.CpMargin));
        ImGui.BeginDisabled(result.Final.Finish!=CraftFinish.Completed || result.Error is not null);
        try
        {
            if(ImGui.Button(localization.Get("CraftMacro.Prepare")))
            {
                try { craftMacroExport=CraftMacroExporter.Build(result,job,catalog,craftMacroExtraWait,craftMacroEcho,configuration.MacroCopy?.Chain==true,craftMacroBuffWait,MacroChainLoaded?.Invoke()==true,CraftSearchOptions.BoundedEchoSound,CraftSearchOptions.EchoLastOnly); craftMacroError=null; }
                catch(ArgumentException error) { craftMacroExport=null; craftMacroError="CraftMacro.Failed"; log.Warning(error,"Could not export craft macros."); }
            }
        }
        finally { ImGui.EndDisabled(); }
        if(result.Final.Finish!=CraftFinish.Completed || result.Error is not null) ImGui.TextDisabled(localization.Get("CraftMacro.Required"));
        if(craftMacroError is { } errorText) ImGui.TextWrapped(localization.Get(errorText));
        if(craftMacroExport is not { } export) return;
        ImGui.TextDisabled(localization.Format("CraftMacro.Summary",export.Actions,export.Pages.Count,export.WaitSeconds));
        if(export.OmittedActions>0) ImGui.TextWrapped(localization.Format("CraftMacro.Omitted",export.OmittedActions));
        DrawMacroToGame(export.Pages,false);
        foreach(var block in export.Pages)
        {
            ImGui.PushID(block.Number);
            try
            {
                if(TenshoAssistant.UI.UiComponents.Header(localization.Format("CraftMacro.Block",block.Number,export.Pages.Count,block.Lines.Count)+"###MacroBlock",ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(ImGui.SmallButton(localization.Get("CraftMacro.Copy"))) ImGui.SetClipboardText(block.Text);
                    foreach(var line in block.Lines) ImGui.TextUnformatted(line);
                }
            }
            finally { ImGui.PopID(); }
        }
    }
}
