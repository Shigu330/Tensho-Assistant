using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Market;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private readonly PreparationPriceSession preparationPrices=new();
    private int costBasisIndex;
    private int costQualityIndex;
    private string? preparationCostError;
    private readonly record struct ChainCostViewKey(CraftChainResult Result,ItemCatalog? Items,uint World,int Quality,long Second,TimeSpan Lifetime);
    private ChainCostViewKey? chainCostViewKey;
    private PreparationCostEstimate? chainCostView;
    private PreparationCostEstimate? preparationListSource;
    private ItemCatalog? preparationListCatalog;
    private uint[] preparationListIds=[];
    private void DrawPreparationCost(RecipeDefinition recipe,CraftPlan plan)
    {
        if (!ImGui.CollapsingHeader(localization.Get("CostUi.Title"))) return;
        var world=market.State;
        ImGui.SetNextItemWidth(230);
        var changed=ImGui.Combo(localization.Get("CostUi.Quantities")+"##Cost",ref costBasisIndex,new[] {localization.Get("CostUi.Shortfall"),localization.Get("CostUi.AllRequired")},2);
        ImGui.SetNextItemWidth(230);
        changed |= ImGui.Combo(localization.Get("CostUi.Quality")+"##Cost",ref costQualityIndex,new[] {"NQ","HQ",localization.Get("CostUi.Cheapest")},3);
        if(changed) {preparationPrices.Cancel(); preparationCostError=null;}
        PreparationCostEstimate estimate;
        try
        {
            estimate=PreparationCost.Build(plan,world.WorldId,(PreparationCostBasis)costBasisIndex,(PreparationPriceQuality)costQualityIndex,
                id=>itemCatalog.State.Catalog?.Find(id),id=>market.GetCached(world.WorldId,id),DateTimeOffset.UtcNow,market.CacheLifetime);
        }
        catch(Exception error) {ImGui.TextWrapped(localization.Get("CostUi.EstimateError")); if(developerMode)ImGui.TextWrapped(error.Message); return;}
        DrawPreparationCostDetails(recipe,plan.Requested,estimate);
    }
    private void DrawChainCost(RecipeDefinition recipe,CraftChainState chain,bool stale)
    {
        if(chain.Result is not { } result || !ImGui.CollapsingHeader(localization.Get("CostUi.Chain"))) return;
        if(chain.Input?.ChoiceRevision!=craftChainChoiceRevision)
        {ImGui.TextWrapped(localization.Get("CostUi.ChoicesChanged"));return;}
        ImGui.PushID("ChainCost");
        try
        {
            ImGui.TextWrapped(localization.Format("CostUi.Calculation",chain.CalculatedAt?.ToLocalTime().ToString("T") ?? "—"));
            if(stale) ImGui.TextColored(Accent,localization.Get("CostUi.StaleStock"));
            if(result.HasUnresolved) ImGui.TextWrapped(localization.Get("CostUi.Unresolved"));
            ImGui.SetNextItemWidth(230);
            if(ImGui.Combo(localization.Get("CostUi.Quality")+"##Cost",ref costQualityIndex,new[] {"NQ","HQ",localization.Get("CostUi.Cheapest")},3)) preparationPrices.Cancel();
            try
            {
                var world=market.State;
                var now=DateTimeOffset.UtcNow;
                var items=itemCatalog.State.Catalog;
                var key=new ChainCostViewKey(result,items,world.WorldId,costQualityIndex,now.ToUnixTimeSeconds(),market.CacheLifetime);
                if(chainCostView is null || chainCostViewKey!=key)
                {
                    chainCostView=PreparationCost.FromChain(result,world.WorldId,(PreparationPriceQuality)costQualityIndex,
                        id=>items?.Find(id),id=>market.GetCached(world.WorldId,id),now,key.Lifetime);
                    chainCostViewKey=key;
                }
                var estimate=chainCostView;
                DrawPreparationCostDetails(recipe,result.Requested,estimate,true,chain.Input.ChoiceRevision,chain.CalculatedAt,stale);
            }
            catch(Exception error) {ImGui.TextWrapped(localization.Get("CostUi.EstimateError")); if(developerMode)ImGui.TextWrapped(error.Message);}
        }
        finally {ImGui.PopID();}
    }
    private void DrawPreparationCostDetails(RecipeDefinition recipe,long requested,PreparationCostEstimate estimate,bool chain=false,int chainRevision=0,
        DateTimeOffset? quantityAt=null,bool oldQuantities=false)
    {
        var world=market.State;
        if(world.WorldId!=estimate.WorldId) {ImGui.TextWrapped(localization.Get("CostUi.WorldChanged"));return;}
        ImGui.TextColored(Accent,world.WorldId==0 ? localization.Get("CostUi.UnknownWorld") : localization.Format("CostUi.Market",world.WorldName));
        var items=itemCatalog.State.Catalog;
        if(!ReferenceEquals(preparationListSource,estimate) || !ReferenceEquals(preparationListCatalog,items))
        {
            preparationListIds=estimate.Lines.Where(line=>line.Quantity!=0 && line.Status!=PreparationCostStatus.UnresolvedChain).Select(line=>items?.Find(line.ItemId))
                .Where(item=>item is {IsMarketable:true} && (estimate.Quality!=PreparationPriceQuality.Hq || item.CanBeHq)).Select(item=>item!.Id).Distinct().ToArray();
            preparationListSource=estimate;preparationListCatalog=items;
        }
        var ids=preparationListIds;
        var batch=preparationPrices.State;
        if(ids.Length>32) ImGui.TextWrapped(localization.Get("CostUi.BatchLimit"));
        ImGui.BeginDisabled(world.WorldId==0 || bankBridge.State.CharacterId==0 || ids.Length==0 || ids.Length>32 || batch.Loading);
        try
        {
            if(ImGui.Button(localization.Get("CostUi.Fetch")))
            {
                try
                {
                    _=preparationPrices.StartAsync(market,new(bankBridge.State.CharacterId,world.WorldId,(uint)itemId,recipe.RecipeId,craftPlanTarget,costBasisIndex,costQualityIndex,chain,chainRevision),ids);
                    preparationCostError=null;
                }
                catch(Exception error) {preparationCostError=error.Message;}
            }
        }
        finally {ImGui.EndDisabled();}
        if(batch.Loading)
        {
            ImGui.SameLine(); if(ImGui.SmallButton(localization.Get("CostUi.Stop"))) preparationPrices.Cancel();
        }
        if(batch.Context?.RecipeId==recipe.RecipeId && batch.Context.IsChain==chain)
        {
            ImGui.TextDisabled(localization.Format("CostUi.Progress",batch.Completed,batch.Items.Count)+(batch.Loading ? localization.Get("CostUi.InProgress") : ""));
            if(batch.Error is { } error) DrawDataError(error,"ServiceErrors.Price");
        }
        if(preparationCostError is { } failed) DrawDataError(failed,"ServiceErrors.Price");
        ImGui.TextWrapped(localization.Format("CostUi.Privacy",market.CacheLifetime.ToString("g")));
        var costed=estimate.Lines.Count(line=>line.Cost is not null);
        if(estimate.KnownSubtotal is not { } subtotal) ImGui.TextColored(Accent,localization.Get("CostUi.Overflow"));
        else if(estimate.Lines.Count==0) ImGui.TextColored(Success,localization.Get("CostUi.NoPurchases"));
        else if(costed==0) ImGui.TextColored(Accent,localization.Get("CostUi.NoPrices"));
        else ImGui.TextColored(estimate.AllCosted ? Success : Accent,
            localization.Format("CostUi.Summary",localization.Get(estimate.AllCosted ? "CostUi.Total":"CostUi.PartialTotal"),subtotal.ToString("N0"),costed,estimate.Lines.Count));
        if(estimate.StockUncertain) ImGui.TextWrapped(localization.Get("CostUi.Uncertain"));
        if(estimate.CachedStock) ImGui.TextDisabled(localization.Get("CostUi.Imported"));
        if(estimate.StalePrices) ImGui.TextColored(Accent,localization.Get("CostUi.OldPrices"));
        if(ImGui.BeginTable("PreparationPrices",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("CostUi.Ingredient"),ImGuiTableColumnFlags.WidthStretch,2);
                ImGui.TableSetupColumn(localization.Get("CostUi.Quantity")); ImGui.TableSetupColumn(localization.Get("CostUi.UnitPrice")); ImGui.TableSetupColumn(localization.Get("CostUi.Cost")); ImGui.TableSetupColumn(localization.Get("CostUi.State"));
                ImGui.TableHeadersRow();
                var clipper=ImGui.ImGuiListClipper();
                try
                {
                    clipper.Begin(estimate.Lines.Count);
                    while(clipper.Step())
                    for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                    {
                    var line=estimate.Lines[index];
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.PushID((int)line.ItemId);
                    try
                    {
                        var item=itemCatalog.State.Catalog?.Find(line.ItemId);
                        DrawItemIcon(item,new Vector2(22)); var clicked=ImGui.IsItemClicked(); ImGui.SameLine();
                        if(ImGui.SmallButton(item?.Name ?? $"ID {line.ItemId}") || clicked) SelectItem(line.ItemId);
                        ImGui.TableNextColumn(); ImGui.TextUnformatted(line.Quantity?.ToString("N0") ?? localization.Get("CostUi.Verify"));
                        ImGui.TableNextColumn(); ImGui.TextUnformatted(line.UnitPrice is { } unit ? $"{unit:N0} {(line.HighQuality==true ? "HQ" : "NQ")}" : "—");
                        if(ImGui.IsItemHovered() && line.Quote is { } quote)
                            ImGui.SetTooltip(localization.Format("CostUi.Quote",quote.WorldId,quote.FetchedAt.ToLocalTime().ToString("G"),quote.UploadedAt?.ToLocalTime().ToString("G") ?? localization.Get("CostUi.Unknown")));
                        ImGui.TableNextColumn(); ImGui.TextUnformatted(line.Cost?.ToString("N0") ?? localization.Get("CostUi.Unknown"));
                        ImGui.TableNextColumn(); ImGui.TextUnformatted(PreparationPriceStatus(line));
                        if(ImGui.IsItemHovered()) ImGui.SetTooltip(PreparationPriceStatus(line));
                    }
                    finally {ImGui.PopID();}
                    }
                }
                finally {clipper.End();clipper.Destroy();}
            }
            finally {ImGui.EndTable();}
        }
        ImGui.TextWrapped(localization.Get("CostUi.Help"));
        if(ImGui.SmallButton(localization.Get("CostUi.Copy")))
        {
            var basis=localization.Get("CostCopy."+estimate.Basis);
            var quality=estimate.Quality switch {PreparationPriceQuality.Nq=>"NQ",PreparationPriceQuality.Hq=>"HQ",_=>localization.Get("CostCopy.Cheapest")};
            var text=new StringBuilder().AppendLine(localization.Format("CostCopy.Header",recipe.ResultItemId,requested,world.WorldName,world.WorldId,basis,quality));
            if(chain)text.AppendLine(localization.Format("CostCopy.Chain",quantityAt?.ToLocalTime().ToString("G") ?? "—")+(oldQuantities ? localization.Get("CostCopy.Changed"):""));
            foreach(var line in estimate.Lines)
                text.AppendLine(localization.Format("CostCopy.Line",
                    itemCatalog.State.Catalog?.Find(line.ItemId)?.Name ?? localization.Format("BankUi.Item",line.ItemId),
                    line.Quantity?.ToString() ?? localization.Get("CostUi.UnknownLower"),
                    line.UnitPrice?.ToString() ?? localization.Get("CostUi.UnknownLower"),
                    line.HighQuality is { } high ? high ? "HQ":"NQ" : localization.Get("CostCopy.UnknownQuality"),
                    line.Cost?.ToString() ?? localization.Get("CostUi.UnknownLower"),
                    PreparationPriceStatus(line),
                    line.Quote?.UploadedAt?.ToLocalTime().ToString("G") ?? localization.Get("CostUi.UnknownLower"))
                    +(line.StockUncertain ? localization.Get("CostCopy.Uncertain"):"")+(line.CachedStock ? localization.Get("CostCopy.Imported"):""));
            text.AppendLine(localization.Format("CostCopy.Summary",localization.Get(estimate.AllCosted ? "CostUi.Total":"CostUi.PartialTotal"),estimate.KnownSubtotal?.ToString() ?? localization.Get("CostCopy.Overflow")));
            ImGui.SetClipboardText(text.ToString());
        }
    }
    private string PreparationPriceStatus(PreparationCostLine line) => line.Status switch
    {
        PreparationCostStatus.Covered=>localization.Get("CostUi.Covered"),
        PreparationCostStatus.Priced=>line.StalePrice ? localization.Get("CostUi.OldPrice") : localization.Get("CostUi.Minimum"),
        PreparationCostStatus.UnknownStock=>localization.Get("CostUi.UnknownStock"),
        PreparationCostStatus.UnknownWorld=>localization.Get("CostUi.UnknownWorld"),
        PreparationCostStatus.UnknownItem=>localization.Get("CostUi.UnknownItem"),
        PreparationCostStatus.NotMarketable=>localization.Get("CostUi.NotMarketable"),
        PreparationCostStatus.UnsupportedQuality=>localization.Get("CostUi.UnsupportedHq"),
        PreparationCostStatus.MissingQuote=>localization.Get("CostUi.NoQuote"),
        PreparationCostStatus.WrongScope=>localization.Get("CostUi.WrongScope"),
        PreparationCostStatus.MissingPrice=>localization.Get("CostUi.NoMinimum"),
        PreparationCostStatus.UnresolvedChain=>localization.Get("CostUi.UnresolvedStatus"),
        _=>localization.Get("CostUi.OverflowStatus"),
    };
}
