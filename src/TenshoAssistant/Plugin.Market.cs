using Dalamud.Plugin.Services;
using TenshoAssistant.Community;
using TenshoAssistant.Inventory;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// X-153 (0.1887, GPT review of 11/11, accepted by the user): the retainer market family out of Plugin.cs, after the Beastmaster family
// (0.1886, Plugin.Beastmaster.cs). Each service is created in one install path and released in one release path of the plugin's
// registrations (RegistrationScope: released in the reverse order; a failure does not stop the others), all in this file; Plugin.cs calls
// the family where these services were made, so the order of the registrations is the one of before. MarketFamily1887Tests counts those
// paths over every Plugin*.cs file. 0.1893: the rest of the family (the world reader, the prices seen reader, the undercut watcher), each
// block moved as it was and called where it stood (MarketFamily1893Tests).
public sealed partial class Plugin
{
    /// <summary>The market's world reader: the player's world and the worlds of the data centre, for the prices asked of Universalis.</summary>
    private void RegisterMarketWorld(IFramework framework,IPlayerState player,IClientState client,IDataManager data)
    {
        MarketWorldReader? marketReader = null;
        registrations.Register(() => marketReader = new MarketWorldReader(market, framework, player, client, log, data), () => marketReader?.Dispose());
    }

    /// <summary>The prices seen on the market board, kept as discoveries when the player allows it; the player's own retainers left out.</summary>
    private void RegisterSeenPrices(Configuration configuration,IMarketBoard marketBoardEvents,Market.SeenPriceStore seenPrices,BankInventoryBridge bankBridge)
    {
        Market.SeenPriceReader? seenReader=null;
        registrations.Register(()=>seenReader=new Market.SeenPriceReader(marketBoardEvents,seenPrices,()=>market.State.WorldId,()=>configuration.Discovery.Enabled && configuration.Discovery.MarketPrices,log)
            {OwnRetainers=()=>bankBridge.State.KnownRetainers?.Select(known=>known.Id).ToHashSet()},()=>seenReader?.Dispose());
    }

    /// <summary>0.346 (g, I-07): lowest offer minus 1 copied to the clipboard when the player opens an item's offers (off by default).</summary>
    private void RegisterUndercut(Configuration configuration,IMarketBoard marketBoardEvents,IFramework framework,IChatGui chat,IDataManager data)
    {
        registrations.Register(()=>undercut=new Market.UndercutWatcher(marketBoardEvents,framework,configuration,(item,lowest,paste,hq)=>
            chat.Print("[Tensho] "+localization.Format("Undercut.Copied",data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(item)?.Name.ExtractText() ?? $"#{item}",
                lowest.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),paste.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),hq ? " HQ":"")),log),()=>undercut?.Dispose());
    }

    /// <summary>The retainer market: the game's retainer sale list read while it is open (0.508, I-05; Universalis on the panel's button
    /// only), the panel's and the sale list's own comparisons (X-698), and the market tax rates the game sends. Returns the sale list's
    /// reader, which the character status reads too.</summary>
    private Market.RetainerSalesReader? RegisterRetainerMarket(Configuration configuration,IFramework framework,IGameGui gameGui,IClientState client,
        IPlayerState player,IMarketBoard marketBoardEvents)
    {
        Market.RetainerSalesReader? retainerSales=null;
        registrations.Register(()=>retainerSales=new Market.RetainerSalesReader(framework,gameGui,client,player,log,()=>configuration.RetainerSalesPanel,()=>configuration.DeveloperMode),()=>retainerSales?.Dispose());
        mainWindow.RetainerSalesSource=()=>retainerSales?.State ?? new(Market.RetainerSalesStatus.Closed,[],new HashSet<string>(),"",false);
        Market.RetainerCompareService? retainerCompare=null;
        registrations.Register(()=>mainWindow.RetainerCompare=retainerCompare=new Market.RetainerCompareService{Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.UniversalisRetainerCompare)},()=>retainerCompare?.Dispose());
        // X-698 M1 (0.1671): the sale list's own requests (its answers added up), apart from the panel's; M4: the market tax rates the game sends.
        Market.RetainerCompareService? retainerSaleListCompare=null;
        registrations.Register(()=>mainWindow.RetainerSaleListCompare=retainerSaleListCompare=new Market.RetainerCompareService{Accumulate=true,Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.UniversalisRetainerCompare)},()=>retainerSaleListCompare?.Dispose());
        Market.MarketTaxReader? marketTaxes=null;
        registrations.Register(()=>marketTaxes=new Market.MarketTaxReader(marketBoardEvents,log),()=>marketTaxes?.Dispose());
        mainWindow.MarketTaxRatesSource=()=>marketTaxes?.Rates;
        return retainerSales;
    }
}
