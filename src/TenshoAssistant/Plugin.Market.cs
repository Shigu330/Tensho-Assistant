using Dalamud.Plugin.Services;
using TenshoAssistant.Community;

namespace TenshoAssistant;

// X-153 (0.1887, GPT review of 11/11, accepted by the user): the retainer market family out of Plugin.cs, after the Beastmaster family
// (0.1886, Plugin.Beastmaster.cs). Each service is created in one install path and released in one release path of the plugin's
// registrations (RegistrationScope: released in the reverse order; a failure does not stop the others), all in this file; Plugin.cs calls
// the family where these services were made, so the order of the registrations is the one of before. MarketFamily1887Tests counts those
// paths over every Plugin*.cs file.
public sealed partial class Plugin
{
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
