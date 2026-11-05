namespace TenshoAssistant.Competitive;

/// <summary>
/// DEC-038: where the whole plugin stops. From the game's data where it says so: PvP zones, content flagged PvP (Triple Triad Battlehall, Lord of
/// Verminion) and deep dungeons, ranked on the Lodestone. Listed by hand where nothing flags it: Triple Triad tournaments, Mahjong, the Beastmaster
/// labyrinth (docs/evidence/0.600-competitive-inventory.md). No setting avoids it, and in doubt it stays stopped.
/// </summary>
public static class CompetitiveZones
{
    public const uint DeepDungeonContentType=21;
    public static readonly IReadOnlySet<uint> Listed=new HashSet<uint>{940,941,831,1339,1340,1341,1342,1343};

    public static HashSet<uint> Build(IEnumerable<(uint Territory,bool PvpZone)> territories,IEnumerable<(uint Territory,bool PvP,uint ContentType)> content)
    {
        var zones=new HashSet<uint>(Listed);
        foreach(var (territory,pvpZone) in territories)if(pvpZone)zones.Add(territory);
        foreach(var (territory,pvp,type) in content)if(territory!=0 && (pvp || type==DeepDungeonContentType))zones.Add(territory);
        return zones;
    }

    /// <summary>Zones null: the data could not be read, so stopped. Territory 0: a loading screen, the state stays. Logged out: nothing to protect.</summary>
    public static bool IsCut(bool loggedIn,uint territory,bool isPvP,IReadOnlySet<uint>? zones,bool previous)
    {
        if(!loggedIn)return false;
        if(isPvP || zones is null)return true;
        return territory==0 ? previous:zones.Contains(territory);
    }
}
