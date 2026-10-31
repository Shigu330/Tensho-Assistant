using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    internal void OpenItemContext(ItemMenuRequest request)
    {
        SelectItem(request.Token.Item.ItemId);
        // Reuse the same tabs, history, queries and future-tool directory as an ordinary item click.
        focusCraftTab=false;focusUsesTab=false;focusStocksTab=false;focusGatherTab=false;focusBankTab=false;
        switch(request.Route)
        {
            case ItemContextRoute.Usages: OpenContextTool("Item Use");break;
            case ItemContextRoute.Stocks:page=Page.Items;focusStocksTab=true;break;
            case ItemContextRoute.Craft:OpenContextTool("Craftimizer");break;
            case ItemContextRoute.Gathering:OpenContextTool("GatherBuddy");break;
            case ItemContextRoute.Tools:toolQuery=string.Empty;page=Page.Home;break;
            case ItemContextRoute.NpcVendors:page=Page.NpcVendors;break;
        }
        IsOpen=true;
    }
}
