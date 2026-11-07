using Dalamud.Game.Network.Structures;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Market;

/// <summary>
/// X-52 (0.817, user request): what the game's market board shows when the player opens an item (Dalamud IMarketBoard: offers by pages, the last 20 sales), kept for the
/// player's world. Only prices, qualities, quantities and dates: never a retainer's, seller's or buyer's name or id. Cut in PvP; off with its setting.
/// </summary>
public sealed class SeenPriceReader : IDisposable
{
    private readonly IMarketBoard board;private readonly SeenPriceStore store;private readonly Func<uint> world;private readonly Func<bool> enabled;private readonly IPluginLog log;
    private bool disposed,warned;

    public SeenPriceReader(IMarketBoard board,SeenPriceStore store,Func<uint> world,Func<bool> enabled,IPluginLog log)
    {
        this.board=board;this.store=store;this.world=world;this.enabled=enabled;this.log=log;
        board.OfferingsReceived+=OnOfferings;board.HistoryReceived+=OnHistory;
    }

    private bool Off=>disposed || TenshoAssistant.Competitive.CompetitiveGate.Cut || !enabled();

    private void OnOfferings(IMarketBoardCurrentOfferings offerings)
    {
        if(Off || offerings.ItemListings.Count==0)return;
        try{store.SeeListings(world(),offerings.ItemListings[0].ItemId,(uint)offerings.RequestId,offerings.ItemListings.Select(listing=>new SeenListing(listing.PricePerUnit,listing.IsHq,(int)listing.ItemQuantity)),DateTimeOffset.UtcNow);}
        catch(Exception error){if(!warned){warned=true;log.Warning(error,"The market board offers could not be noted.");}}
    }

    private void OnHistory(IMarketBoardHistory history)
    {
        if(Off || history.ItemId==0)return;
        try{store.SeeSales(world(),history.ItemId,history.HistoryListings.Select(sale=>new SeenSale(sale.SalePrice,sale.IsHq,(int)sale.Quantity,new DateTimeOffset(sale.PurchaseTime.ToUniversalTime(),TimeSpan.Zero))),DateTimeOffset.UtcNow);}
        catch(Exception error){if(!warned){warned=true;log.Warning(error,"The market board history could not be noted.");}}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;board.OfferingsReceived-=OnOfferings;board.HistoryReceived-=OnHistory;
    }
}
