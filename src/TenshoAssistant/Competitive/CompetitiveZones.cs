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
    /// <summary>X-109 (0.979, DEC-047): the Beastmaster labyrinth's territories (one per board; 1339 read in game on 2026-11-09 at the 1st board's entry).</summary>
    public static readonly IReadOnlySet<uint> Labyrinth=new HashSet<uint>{1339,1340,1341,1342,1343};
    /// <summary>
    /// X-92 (0.1001, « plan for future content »): the labyrinth's territories found in the game's sheets (XBMContent column 0 → ContentFinderCondition →
    /// TerritoryType), so a board added by a game update is cut too; the list above stays (in doubt, stopped).
    /// </summary>
    public static IReadOnlySet<uint> LabyrinthFound { get; private set; } = new HashSet<uint>();
    public static bool IsLabyrinth(uint territory)=>Labyrinth.Contains(territory) || LabyrinthFound.Contains(territory);
    /// <summary>
    /// 0.612 (user report, in game): territory 579 is also a public hall with PvE matches, so neither the place nor a match proves anything: stopped only in
    /// the arena's own instance. What marks it is measured in game (the watcher logs it): docs/project/ARCHITECTURE.md, « Competitive switch-off ».
    /// </summary>
    public static readonly IReadOnlySet<uint> InstanceOnly=new HashSet<uint>{579};

    public static HashSet<uint> Build(IEnumerable<(uint Territory,bool PvpZone)> territories,IEnumerable<(uint Territory,bool PvP,uint ContentType)> content,IEnumerable<uint>? labyrinth=null)
    {
        var zones=new HashSet<uint>(Listed);
        var found=(labyrinth ?? []).Where(territory=>territory!=0).ToHashSet();
        LabyrinthFound=found;
        zones.UnionWith(found);
        foreach(var (territory,pvpZone) in territories)if(pvpZone)zones.Add(territory);
        foreach(var (territory,pvp,type) in content)if(territory!=0 && (pvp || type==DeepDungeonContentType))zones.Add(territory);
        return zones;
    }

    /// <summary>Zones null: the data could not be read, so stopped. Territory 0: a loading screen, the state stays. Logged out: nothing to protect.</summary>
    public static bool IsCut(bool loggedIn,uint territory,bool isPvP,IReadOnlySet<uint>? zones,bool previous,bool inInstance=false)
    {
        if(!loggedIn)return false;
        if(isPvP || zones is null)return true;
        if(territory==0)return previous;
        return InstanceOnly.Contains(territory) ? inInstance:zones.Contains(territory);
    }
}
