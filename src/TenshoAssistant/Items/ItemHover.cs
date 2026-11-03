namespace TenshoAssistant.Items;

// Public game ID markers, verified against Dalamud ItemUtil. Event/key IDs stay outside the standard Item context.
public enum ItemHoverKind:uint {Normal=0,Collectible=500000,Hq=1000000}
public sealed record ItemHoverItem(uint ItemId,ItemHoverKind Kind);
// Screen rectangle of the game's own item tooltip (ItemDetail), copied on the framework thread.
public readonly record struct ItemHoverAnchor(float X,float Y,float Width,float Height)
{
    public bool Valid=>float.IsFinite(X) && float.IsFinite(Y) && Width is >=40 and <=4000 && Height is >=20 and <=4000;
}
// 0.343 (I-01): Stack = quantity of the hovered inventory slot (null when the tooltip is not for one of the player's slots).
public sealed record ItemHoverState(bool Enabled,ulong Owner,ItemHoverItem? Item,DateTimeOffset? ObservedAt,DateTimeOffset? StableSince,ItemHoverAnchor? Anchor=null,uint? Stack=null)
{
    public bool Fresh(DateTimeOffset now)=>Enabled && Owner!=0 && Item is not null && ObservedAt is { } date && now>=date && now-date<=TimeSpan.FromSeconds(2);
    public bool Stable(DateTimeOffset now)=>Fresh(now) && StableSince is { } since && now>=since && now-since>=TimeSpan.FromMilliseconds(600);
}

/// <summary>One normalized, owner-scoped context for all hover consumers; independent of world and market eligibility.</summary>
public sealed class ItemHoverService : IDisposable
{
    private readonly object gate=new();private bool disposed;
    private ItemHoverState state=new(false,0,null,null,null);
    public ItemHoverState State=>Volatile.Read(ref state);
    public static ItemHoverItem? Normalize(ulong raw,ItemCatalog? catalog)
    {
        if(raw==0 || raw>=2000000 || catalog is null)return null;
        var kind=raw>=1000000 ? ItemHoverKind.Hq:raw>=500000 ? ItemHoverKind.Collectible:ItemHoverKind.Normal;
        var id=(uint)(raw-(uint)kind);
        if(id==0 || catalog.Find(id) is not { } item || kind==ItemHoverKind.Hq && !item.CanBeHq)return null;
        return new(id,kind);
    }
    public void Observe(bool enabled,ulong owner,ulong raw,ItemCatalog? catalog,DateTimeOffset now)
    {
        lock(gate)
        {
            if(disposed)return;
            var item=enabled && owner!=0 ? Normalize(raw,catalog):null;
            var changed=state.Enabled!=enabled || state.Owner!=owner || state.Item!=item || state.ObservedAt>now;
            state=new(enabled,enabled ? owner:0,item,item is null ? null:now,item is null ? null:changed ? now:state.StableSince,item is null ? null:state.Anchor,
                item is null || changed ? null:state.Stack);
        }
    }
    /// <summary>Records where the game's tooltip is drawn; an invalid or hidden tooltip removes the anchor.</summary>
    public void SetAnchor(ItemHoverAnchor? anchor)
    {
        lock(gate)
        {
            if(disposed)return;
            var value=anchor is { Valid:true } ? anchor:null;
            if(state.Anchor!=value)state=state with{Anchor=value};
        }
    }
    /// <summary>0.343: quantity of the hovered slot, read on the framework thread; null when not one of the player's slots.</summary>
    public void SetStack(uint? quantity)
    {
        lock(gate)
        {
            if(disposed)return;
            var value=quantity is > 0 and <= 999999 ? quantity:null;
            if(state.Stack!=value)state=state with{Stack=value};
        }
    }
    public void Clear(){lock(gate)if(!disposed)state=new(false,0,null,null,null);}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;state=new(false,0,null,null,null);}}
}
