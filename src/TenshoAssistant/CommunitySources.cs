namespace TenshoAssistant;

/// <summary>X-76 step 3 (0.945): « game and observed data only »: the community sources off at once (eight since PaissaDB, 0.949), their choices kept (one bit each) and given back.</summary>
public static class CommunitySources
{
    public static int Bits(Configuration c)=>(c.CommunityLumina ? 1:0)|(c.CommunityGarland ? 2:0)|(c.CommunityWiki ? 4:0)|(c.CommunityGatherBuddy ? 8:0)
        |(c.CommunityTeamcraftBites ? 16:0)|(c.CommunityFashion ? 32:0)|(c.CommunityUniversalis ? 64:0)|(c.CommunityPaissa ? 128:0);
    public static void Set(Configuration c,int bits)
    {
        c.CommunityLumina=(bits&1)!=0;c.CommunityGarland=(bits&2)!=0;c.CommunityWiki=(bits&4)!=0;c.CommunityGatherBuddy=(bits&8)!=0;
        c.CommunityTeamcraftBites=(bits&16)!=0;c.CommunityFashion=(bits&32)!=0;c.CommunityUniversalis=(bits&64)!=0;c.CommunityPaissa=(bits&128)!=0;
    }
    /// <summary>X-122, D-101: a use asks its source only while the source's switch and its own are on (« game data only » turns the sources off).</summary>
    public static bool Allowed(Configuration c,SourceUse use)=>use switch
    {
        SourceUse.GarlandDutyLoot or SourceUse.GarlandMonsterLoot or SourceUse.GarlandItemSources=>c.CommunityGarland,
        SourceUse.UniversalisRetainerCompare or SourceUse.UniversalisToolPrices=>c.CommunityUniversalis,
        _=>c.CommunityWiki,
    } && c.SourceUses.Get(use);
    /// <summary>On: the sources' choices kept, all off. Off: the kept choices back (all on when none was kept).</summary>
    public static void ApplyGameDataOnly(Configuration c,bool on)
    {
        if(on==c.GameDataOnly)return;
        if(on){c.CommunityBeforeGameOnly=Bits(c);Set(c,0);}
        else{Set(c,c.CommunityBeforeGameOnly is { } kept ? kept | (kept<128 ? 128:0):255);c.CommunityBeforeGameOnly=null;}
        c.GameDataOnly=on;
    }

    /// <summary>
    /// X-834 (security review I6): the switch of each network service the game and the desktop application both build, by its type's name (no
    /// compile-time link, so the desktop application reads the same table through the plugin): its source's switch and, for a use, its own.
    /// Before, only Plugin.cs set them; the desktop application built these services without, and a service reads « no switch » as allowed:
    /// with every source off in the game, a « Fetch » in the application still asked Garland Tools or the fashion data. Null: not in the table.
    /// </summary>
    public static Func<bool>? SwitchFor(string typeName,Configuration c)=>typeName switch
    {
        "TenshoAssistant.Community.GarlandClient"=>()=>c.CommunityGarland,
        "TenshoAssistant.Market.MarketService" or "TenshoAssistant.Market.MarketBoardService"=>()=>c.CommunityUniversalis,
        "TenshoAssistant.Duties.DutyLootService"=>()=>Allowed(c,SourceUse.GarlandDutyLoot),
        "TenshoAssistant.Monsters.MonsterLootService"=>()=>Allowed(c,SourceUse.GarlandMonsterLoot),
        "TenshoAssistant.Fashion.FashionDataService"=>()=>c.CommunityFashion,
        "TenshoAssistant.Community.GarlandItemSourceService"=>()=>Allowed(c,SourceUse.GarlandItemSources),
        "TenshoAssistant.Community.GatherBuddyFishService"=>()=>c.CommunityGatherBuddy,
        "TenshoAssistant.Community.FishBiteTimeService"=>()=>c.CommunityTeamcraftBites,
        _=>null,
    };

    /// <summary>
    /// X-834: the service's « Enabled » (a Func&lt;bool&gt; property) set from <see cref="SwitchFor"/>; false when the service has no such property
    /// or its type no row (the desktop application then turns it off: a network service it does not know never asks).
    /// </summary>
    public static bool Wire(object service,Configuration c)
    {
        var property=service.GetType().GetProperty("Enabled");
        if(property is null || property.PropertyType!=typeof(Func<bool>) || !property.CanWrite)return false;
        if(SwitchFor(service.GetType().FullName ?? "",c) is not { } on)return false;
        property.SetValue(service,on);
        return true;
    }

}
