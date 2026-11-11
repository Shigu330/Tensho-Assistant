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

}
