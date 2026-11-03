using Dalamud.Game.Network.Structures;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Market;

/// <summary>
/// g, I-07 (0.346): when the player opens an item's offers on the market board (Dalamud IMarketBoard.OfferingsReceived: data the game
/// receives for the player's own request, nothing is asked), the first page of offers gives the lowest price; « lowest minus 1 » is
/// kept for the clipboard, written from the plugin's draw (ImGui), and announced in the chat. Off by default (it replaces the clipboard).
/// The player's retainers are read on the framework thread every 5 s (RetainerManager, guarded). Unsubscribed on dispose.
/// </summary>
public sealed unsafe class UndercutWatcher : IDisposable
{
    private readonly IMarketBoard board;
    private readonly IFramework framework;
    private readonly Configuration configuration;
    private readonly Action<uint,uint,uint,bool> announce;
    private readonly IPluginLog log;
    private readonly object gate=new();
    private IReadOnlySet<ulong> retainers=new HashSet<ulong>();
    private DateTimeOffset nextRetainers;
    private uint lastItem;
    private int lastRequest=-1;
    private string? pending;
    private bool disposed,warned;

    /// <param name="announce">Chat line (item, lowest, paste, HQ), framework thread.</param>
    public UndercutWatcher(IMarketBoard board,IFramework framework,Configuration configuration,Action<uint,uint,uint,bool> announce,IPluginLog log)
    {
        this.board=board;this.framework=framework;this.configuration=configuration;this.announce=announce;this.log=log;
        board.OfferingsReceived+=OnOfferings;
        framework.Update+=OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRetainers || !configuration.UndercutClipboard)return;
        nextRetainers=now.AddSeconds(5);
        try
        {
            var manager=RetainerManager.Instance();
            if(manager==null || !manager->IsReady)return;
            var ids=new HashSet<ulong>();
            foreach(ref var retainer in manager->Retainers)if(retainer.RetainerId!=0)ids.Add(retainer.RetainerId);
            lock(gate)retainers=ids;
        }
        catch(Exception error){if(!warned){warned=true;log.Warning(error,"Retainers could not be read for the undercut price.");}}
    }

    private void OnOfferings(IMarketBoardCurrentOfferings offerings)
    {
        try
        {
            if(disposed || !configuration.UndercutClipboard || offerings.ItemListings.Count==0)return;
            var item=offerings.ItemListings[0].ItemId;
            IReadOnlySet<ulong> own;
            lock(gate)
            {
                // The first page of a new request is the cheapest one: later pages of the same item are left alone.
                if(item==lastItem && offerings.RequestId==lastRequest)return;
                own=retainers;
            }
            if(Undercut.Price(offerings.ItemListings.Select(listing=>new UndercutListing(listing.PricePerUnit,listing.IsHq,listing.RetainerId)),own) is not { } price)return;
            lock(gate){lastItem=item;lastRequest=offerings.RequestId;pending=price.Paste.ToString(System.Globalization.CultureInfo.InvariantCulture);}
            announce(item,price.Lowest,price.Paste,price.Hq);
        }
        catch(Exception error){log.Warning(error,"Undercut price could not be computed.");}
    }

    /// <summary>Writes the pending price to the clipboard; called from the plugin's draw (ImGui thread).</summary>
    public void FlushClipboard()
    {
        string? text;
        lock(gate){text=pending;pending=null;}
        if(text is not null)Dalamud.Bindings.ImGui.ImGui.SetClipboardText(text);
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        board.OfferingsReceived-=OnOfferings;
        framework.Update-=OnUpdate;
    }
}
