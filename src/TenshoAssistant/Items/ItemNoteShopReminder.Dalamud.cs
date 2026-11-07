using Dalamud.Plugin.Services;
using TenshoAssistant.Inventory;

namespace TenshoAssistant.Items;

/// <summary>
/// g, I-08 (0.347): when the player opens an NPC shop (the game's « Shop » window becomes visible), one chat line lists the items of the
/// bags labelled « to sell to an NPC », and another warns about the bags' items labelled « important » (with the alarm sound when the
/// player turned it on; off by default). Framework thread, once per opening; checked every half second. Removed on dispose.
/// </summary>
public sealed class ItemNoteShopReminder : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly Configuration configuration;
    private readonly Func<InventorySnapshot?> live;
    private readonly Func<ulong> character;
    private readonly Action<IReadOnlyList<(uint Item,long Quantity)>,IReadOnlyList<(uint Item,long Quantity)>> remind;
    private readonly IPluginLog log;
    private DateTimeOffset nextCheck;
    private bool shopOpen,disposed,warned;

    public ItemNoteShopReminder(IFramework framework,IGameGui gui,Configuration configuration,Func<InventorySnapshot?> live,Func<ulong> character,
        Action<IReadOnlyList<(uint Item,long Quantity)>,IReadOnlyList<(uint Item,long Quantity)>> remind,IPluginLog log)
    {
        this.framework=framework;this.gui=gui;this.configuration=configuration;this.live=live;this.character=character;this.remind=remind;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextCheck)return;
        nextCheck=now.AddMilliseconds(500);
        try
        {
            var notes=configuration.ItemNotes;
            if(!configuration.ItemNoteShopReminder || notes is null || notes.Count==0){shopOpen=false;return;}
            var addon=gui.GetAddonByName("Shop");
            var open=!addon.IsNull && addon.IsVisible;
            if(open && !shopOpen)
            {
                var (toSell,important)=ItemNotes.InBags(live(),character(),notes);
                if(toSell.Count>0 || important.Count>0)remind(toSell,important);
            }
            shopOpen=open;
        }
        catch(Exception error){if(!warned){warned=true;log.Warning(error,"Item note shop reminder failed.");}}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
