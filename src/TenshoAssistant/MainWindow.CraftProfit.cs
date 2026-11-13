using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// A-06 (0.431): profit under the preparation cost — the crafted item's lowest listing (world, or data center as an option) × the
// quantity, minus the known purchases; the last sale beside it. Prices from the same Universalis request as the ingredients.
internal sealed partial class MainWindow
{
    private bool profitHq,profitDataCenter;

    private void DrawCraftProfit(RecipeDefinition recipe,long requested,PreparationCostEstimate estimate,uint worldId,bool chain)
    {
        if(itemCatalog.State.Catalog?.Find(recipe.ResultItemId) is not { IsMarketable:true } product)return;
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("ProfitUi.Title"));
        if(product.CanBeHq)
        {
            ImGui.Checkbox(localization.Get("ProfitUi.Hq")+$"###ProfitHq{(chain ? "Chain":"")}",ref profitHq);
            Tip("ProfitUi.HqTip");
            ImGui.SameLine();
        }
        ImGui.Checkbox(localization.Get("ProfitUi.DataCenter")+$"###ProfitDc{(chain ? "Chain":"")}",ref profitDataCenter);
        Tip("ProfitUi.DataCenterTip");
        var profit=CraftProfits.Compute(market.GetCached(worldId,recipe.ResultItemId),profitHq && product.CanBeHq,requested,estimate.KnownSubtotal,estimate.AllCosted,profitDataCenter);
        if(profit.UnitPrice is not { } unit){ImGui.TextDisabled(localization.Get("ProfitUi.NoPrice"));return;}
        var where=profit.PriceWorld is { } world && market.State.NameOf(world) is { Length:>0 } name ? name:"";
        ImGui.TextUnformatted(localization.Format("ProfitUi.Price",unit.ToString("N0",Numbers),where,requested.ToString("N0",Numbers),(profit.Revenue ?? 0).ToString("N0",Numbers)));
        if(profit.LastSale is { } sale)ImGui.TextDisabled(localization.Format("ProfitUi.LastSale",sale.ToString("N0",Numbers)));
        if(profit.Profit is { } gain)ImGui.TextColored(gain>=0 ? Success:UI.FfxivTheme.Error,localization.Format("ProfitUi.Profit",gain.ToString("N0",Numbers)));
        else ImGui.TextColored(Accent,localization.Get("ProfitUi.CostIncomplete"));
        ImGui.TextDisabled(localization.Get("ProfitUi.Help"));
    }
}
