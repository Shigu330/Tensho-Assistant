namespace TenshoAssistant;

/// <summary>
/// X-76 P4 (0.927): whether an item teaches something (emote, hairstyle, barding, orchestrion roll, card, mount, minion…) and whether this
/// character already learned it, asked to the game (Dalamud IUnlockState.IsItemUnlockable / IsItemUnlocked; never called before, only mounts and
/// minions were known through a two-action table). Null: not an unlocking item, not logged in, or the game could not say. Kept 5 s per item.
/// </summary>
internal sealed partial class MainWindow
{
    internal Func<uint,bool?>? ItemUnlockState { get; set; }
    private readonly Dictionary<uint,(bool? Value,long At)> itemUnlockCache=[];

    private bool? GameItemUnlocked(uint itemId)
    {
        if(itemId==0 || ItemUnlockState is not { } ask)return null;
        var now=Environment.TickCount64;
        if(itemUnlockCache.TryGetValue(itemId,out var known) && now-known.At<5000)return known.Value;
        bool? value;
        try{value=ask(itemId);}catch(Exception){value=null;}
        if(itemUnlockCache.Count>4096)itemUnlockCache.Clear();
        itemUnlockCache[itemId]=(value,now);
        return value;
    }

    /// <summary>The collection state when the item belongs to a tracked collection, else the game's own unlock state.</summary>
    private Collections.CollectionAcquisition ItemAcquisition(Items.ItemDefinition? item)
    {
        if(item is null)return Collections.CollectionAcquisition.Unknown;
        var tracked=CollectionStateOf(CollectionOf(item));
        if(tracked!=Collections.CollectionAcquisition.Unknown)return tracked;
        return GameItemUnlocked(item.Id) switch{true=>Collections.CollectionAcquisition.Acquired,false=>Collections.CollectionAcquisition.NotAcquired,_=>Collections.CollectionAcquisition.Unknown};
    }
}
