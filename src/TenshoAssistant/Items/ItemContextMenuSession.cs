namespace TenshoAssistant.Items;

public enum ItemContextRoute { Usages, Stocks, Craft, Gathering, Tools, NpcVendors }
public sealed record ItemMenuToken(long Revision,ulong Owner,ItemHoverItem Item,DateTimeOffset CreatedAt);
public sealed record ItemMenuRequest(ItemMenuToken Token,ItemContextRoute Route);

/// <summary>Copies identities only. A callback cannot reuse a replaced menu, another login, or a disabled preference.</summary>
public sealed class ItemContextMenuSession : IDisposable
{
    private readonly object gate=new();
    private long revision;
    private bool disposed;
    private ItemMenuToken? active;
    private ItemMenuRequest? pending;

    public ItemMenuToken? Begin(bool enabled,ulong owner,ulong raw,ItemCatalog? catalog,DateTimeOffset now)
    {
        lock(gate)
        {
            if(disposed)return null;
            active=null;pending=null;revision++;
            if(!enabled || owner==0 || ItemHoverService.Normalize(raw,catalog) is not { } item)return null;
            return active=new(revision,owner,item,now);
        }
    }

    private bool Valid(ItemMenuToken token,bool enabled,ulong owner,ItemCatalog? catalog,DateTimeOffset now)
        =>!disposed && enabled && owner!=0 && token.Owner==owner && active==token
          && now>=token.CreatedAt && now-token.CreatedAt<=TimeSpan.FromMinutes(2)
          && ItemHoverService.Normalize((ulong)token.Item.ItemId+(uint)token.Item.Kind,catalog)==token.Item;

    public bool IsCurrent(ItemMenuToken token,bool enabled,ulong owner,ItemCatalog? catalog,DateTimeOffset now)
    {lock(gate)return Valid(token,enabled,owner,catalog,now);}

    public bool Queue(ItemMenuToken token,ItemContextRoute route,bool enabled,ulong owner,ItemCatalog? catalog,DateTimeOffset now)
    {
        lock(gate)
        {
            if(pending is not null || !Enum.IsDefined(route) || !Valid(token,enabled,owner,catalog,now))return false;
            pending=new(token,route);return true;
        }
    }

    public ItemMenuRequest? Take(bool enabled,ulong owner,ItemCatalog? catalog,DateTimeOffset now)
    {
        lock(gate)
        {
            if(active is { } current && !Valid(current,enabled,owner,catalog,now))
            {active=null;pending=null;}
            var request=pending;pending=null;
            var valid=request is not null && Valid(request.Token,enabled,owner,catalog,now);
            if(request is not null)active=null; // one explicit click, at most one navigation
            return valid ? request:null;
        }
    }
    public void Clear(){lock(gate){active=null;pending=null;revision++;}}
    public void Dispose(){lock(gate){disposed=true;active=null;pending=null;revision++;}}
}
