using TenshoAssistant.Items;

namespace TenshoAssistant.Market;

public sealed record MarketHoverItem(uint ItemId, bool Hq);
public sealed class MarketHoverTracker
{
    private ulong owner;
    private uint world;
    private DateTimeOffset stableSince;
    private DateTimeOffset nextAttempt;
    public MarketHoverItem? Current { get; private set; }
    public static MarketHoverItem? Normalize(ulong raw, ItemCatalog? catalog)
        => Eligible(ItemHoverService.Normalize(raw,catalog),catalog);
    public static MarketHoverItem? Eligible(ItemHoverItem? hovered,ItemCatalog? catalog)
    {
        if(hovered is null || hovered.Kind is not (ItemHoverKind.Normal or ItemHoverKind.Hq) || catalog?.Find(hovered.ItemId) is not {IsMarketable:true} item
            || hovered.Kind==ItemHoverKind.Hq && !item.CanBeHq)return null;
        return new(item.Id,hovered.Kind==ItemHoverKind.Hq);
    }
    public bool Observe(bool enabled, ulong character, uint worldId, ulong raw, ItemCatalog? catalog, DateTimeOffset now)
        => Observe(enabled,character,worldId,ItemHoverService.Normalize(raw,catalog),catalog,now);
    public bool Observe(bool enabled,ulong character,uint worldId,ItemHoverItem? hovered,ItemCatalog? catalog,DateTimeOffset now)
    {
        var item = enabled && character != 0 && worldId != 0 ? Eligible(hovered,catalog) : null;
        var changed = owner != character || world != worldId || item != Current || now<stableSince;
        if (changed) { owner = character; world = worldId; Current = item; stableSince = now; nextAttempt = now; }
        return changed;
    }
    public bool TakeRequest(DateTimeOffset now)
    {
        if (Current is null || now - stableSince < TimeSpan.FromMilliseconds(600) || now < nextAttempt) return false;
        nextAttempt = now.AddSeconds(5); return true;
    }
}
