using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Items;

internal static class ItemContextMenuTests
{
    private static ItemCatalog Catalog()=>ItemCatalog.Build([new(42,"Bound HQ-capable",true),new(43,"NQ only",false)],new("fixture","French","v",1));
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Inventory menu copies the chosen quality and dispatches each route once without hover state",()=>
        {
            using var session=new ItemContextMenuSession();var catalog=Catalog();var now=DateTimeOffset.UtcNow;
            foreach(var raw in new ulong[]{42,1000042,500043})
            foreach(var route in Enum.GetValues<ItemContextRoute>())
            {
                var token=session.Begin(true,7,raw,catalog,now)!;
                Check(token.Item==ItemHoverService.Normalize(raw,catalog),"shared standard identity and quality");
                Check(session.Queue(token,route,true,7,catalog,now.AddSeconds(1)),"explicit route accepted without world or market eligibility");
                Check(!session.Queue(token,route,true,7,catalog,now.AddSeconds(1)),"duplicate callback rejected");
                var result=session.Take(true,7,catalog,now.AddSeconds(2));
                Check(result is not null && result.Token==token && result.Route==route,"queued identity never replaced by current hover");
                Check(session.Take(true,7,catalog,now.AddSeconds(2)) is null && !session.Queue(token,route,true,7,catalog,now.AddSeconds(2)),"exactly one navigation");
            }
        }),
        ("Replacement menus invalidate older closures and pending navigation even for the same item",()=>
        {
            using var session=new ItemContextMenuSession();var catalog=Catalog();var now=DateTimeOffset.UtcNow;
            var old=session.Begin(true,7,42,catalog,now)!;
            Check(session.Queue(old,ItemContextRoute.Usages,true,7,catalog,now),"old queued");
            var current=session.Begin(true,7,42,catalog,now)!;
            Check(old.Revision!=current.Revision && !session.IsCurrent(old,true,7,catalog,now),"revision distinguishes identical menu items");
            Check(session.Take(true,7,catalog,now) is null && !session.Queue(old,ItemContextRoute.Craft,true,7,catalog,now),"old pending and callback discarded");
            Check(session.IsCurrent(current,true,7,catalog,now),"replacement usable");
            session.Begin(true,7,0,catalog,now);
            Check(!session.IsCurrent(current,true,7,catalog,now),"unsupported native menu also invalidates closure");
        }),
        ("Menu handoff rechecks owner opt in catalogue and age after the explicit click",()=>
        {
            var catalog=Catalog();var now=DateTimeOffset.UtcNow;
            foreach(var cause in new[]{"logout","other owner","disabled","catalogue","expired","clock"})
            {
                using var session=new ItemContextMenuSession();var token=session.Begin(true,7,42,catalog,now)!;
                Check(session.Queue(token,ItemContextRoute.Stocks,true,7,catalog,now),"queue valid click");
                var owner=cause=="logout" ? 0UL:cause=="other owner" ? 8UL:7UL;
                var at=cause=="expired" ? now.AddMinutes(2).AddTicks(1):cause=="clock" ? now.AddTicks(-1):now;
                Check(session.Take(cause!="disabled",owner,cause=="catalogue" ? null:catalog,at) is null,"stale dispatch rejected: "+cause);
                Check(session.Take(true,7,catalog,now) is null,"rejected click cannot be replayed");
            }
        }),
        ("Menu admission rejects special unknown impossible HQ and disabled or unloaded contexts",()=>
        {
            using var session=new ItemContextMenuSession();var catalog=Catalog();var now=DateTimeOffset.UtcNow;
            foreach(var raw in new ulong[]{0,99,1000043,2000042,ulong.MaxValue})Check(session.Begin(true,7,raw,catalog,now) is null,"invalid raw");
            Check(session.Begin(false,7,42,catalog,now) is null && session.Begin(true,0,42,catalog,now) is null && session.Begin(true,7,42,null,now) is null,"preference login catalogue gates");
            var token=session.Begin(true,7,42,catalog,now)!;
            Check(session.IsCurrent(token,true,7,catalog,now.AddMinutes(2)) && !session.IsCurrent(token,true,7,catalog,now.AddMinutes(2).AddTicks(1)),"two-minute boundary");
            Check(!session.Queue(token,(ItemContextRoute)999,true,7,catalog,now),"unsupported route cannot navigate");
        }),
        ("Logout invalidation survives reconnect to the same character and disposal is terminal",()=>
        {
            var session=new ItemContextMenuSession();var catalog=Catalog();var now=DateTimeOffset.UtcNow;
            var token=session.Begin(true,7,42,catalog,now)!;session.Clear();
            Check(!session.Queue(token,ItemContextRoute.Tools,true,7,catalog,now),"same owner after logout does not restore old token");
            var beforeDisable=session.Begin(true,7,42,catalog,now)!;
            session.Take(false,7,catalog,now);
            Check(!session.IsCurrent(beforeDisable,true,7,catalog,now),"disabling before a click invalidates the menu even after re-enabling");
            var current=session.Begin(true,7,43,catalog,now)!;
            Check(session.Queue(current,ItemContextRoute.Tools,true,7,catalog,now),"new login menu accepted");
            session.Dispose();session.Dispose();
            Check(session.Take(true,7,catalog,now) is null && session.Begin(true,7,42,catalog,now) is null && !session.IsCurrent(current,true,7,catalog,now),"disposed callback and pending navigation invalidated");
        }),
        ("Inventory menu option preserves old v1 defaults and remains independent of hover prices",()=>
        {
            var old=JsonSerializer.Deserialize<Configuration>("{\"Version\":1,\"ItemUsagesOnHover\":true}")!;
            Check(!old.ItemContextMenuEnabled && old.ItemUsagesOnHover && !old.MarketPricesOnHover,"legacy configuration stays opt out");
            var settings=new Configuration{ItemContextMenuEnabled=true};
            var copy=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(settings))!;
            Check(copy.ItemContextMenuEnabled && !copy.ItemUsagesOnHover && !copy.MarketPricesOnHover && copy.Version==1,"menu alone roundtrips without activating hover or network");
        }),
    };
}
