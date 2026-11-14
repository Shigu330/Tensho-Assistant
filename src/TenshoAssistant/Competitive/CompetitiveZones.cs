namespace TenshoAssistant.Competitive;

/// <summary>
/// DEC-038: where the whole plugin stops. From the game's data where it says so: PvP zones, content flagged PvP (Triple Triad Battlehall, Lord of
/// Verminion). Listed by hand where nothing flags it: Triple Triad tournaments, Mahjong
/// (docs/evidence/0.600-competitive-inventory.md). No setting avoids it, and in doubt it stays stopped.
/// X-137 (0.1115, user's choice of 2026-11-11, DEC-038 completed): the Beastmaster labyrinth is no longer cut (no ranking); its territories stay
/// known (<see cref="IsLabyrinth"/>) for the board panel and the developer reading.
/// X-262 (0.1308, user's choice of 2026-11-13 after the Palace of the Dead 1-10, DEC-038 completed): deep dungeons (content type 21) are no
/// longer cut; Tensho works there as in a normal dungeon, but its map gives way to the game's (<see cref="OwnMapInstances"/>).
/// </summary>
public static class CompetitiveZones
{
    public const uint DeepDungeonContentType=21;
    public static readonly IReadOnlySet<uint> Listed=new HashSet<uint>{940,941,831};
    /// <summary>X-109 (0.979, DEC-047): the Beastmaster labyrinth's territories (one per board; 1339 read in game on 2026-11-09 at the 1st board's entry).</summary>
    public static readonly IReadOnlySet<uint> Labyrinth=new HashSet<uint>{1339,1340,1341,1342,1343};
    /// <summary>
    /// X-92 (0.1001, « plan for future content »): the labyrinth's territories found in the game's sheets (XBMContent column 0 → ContentFinderCondition →
    /// TerritoryType), so a board added by a game update is known too; the list above stays. Not cut since X-137 (0.1115).
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
        // X-137 (0.1115): the labyrinth's boards are remembered (IsLabyrinth), not cut.
        LabyrinthFound=(labyrinth ?? []).Where(territory=>territory!=0).ToHashSet();
        foreach(var (territory,pvpZone) in territories)if(pvpZone)zones.Add(territory);
        // X-262 (0.1308): content flagged PvP only; deep dungeons (DeepDungeonContentType) are no longer cut.
        foreach(var (territory,pvp,_) in content)if(territory!=0 && pvp)zones.Add(territory);
        return zones;
    }

    /// <summary>
    /// X-262 (0.1308): the instances whose map the game draws itself: the territory of a duty (ContentFinderCondition) that no map of the sheets
    /// covers (TerritoryType.Map 0 and no Map row naming it). Read on 2026-11-14 (docs/evidence/0.1308-own-map-instances.md): the 50 deep
    /// dungeon duties, the Gold Saucer's games, the Diadem's trial, the Final Verse and one extreme trial. Tensho's map has nothing to show there.
    /// </summary>
    public static HashSet<uint> OwnMapInstances(IEnumerable<uint> dutyTerritories,IEnumerable<(uint Territory,uint Map)> territories,IEnumerable<uint> mappedTerritories)
    {
        var withMap=mappedTerritories.Where(territory=>territory!=0).ToHashSet();
        foreach(var (territory,map) in territories)if(map!=0)withMap.Add(territory);
        return dutyTerritories.Where(territory=>territory!=0 && !withMap.Contains(territory)).ToHashSet();
    }

    /// <summary>Zones null: the data could not be read, so stopped. Territory 0: a loading screen, the state stays. Logged out: nothing to protect.</summary>
    public static bool IsCut(bool loggedIn,uint territory,bool isPvP,IReadOnlySet<uint>? zones,bool previous,bool inInstance=false)
        =>IsCut(loggedIn,territory,isPvP,zones,previous,()=>inInstance);

    /// <summary>
    /// X-169 (0.1141, GPT audit R06): the decision rests only on the reads it needs. The arena's instance is read only in its own place (never
    /// elsewhere, where a failing read could stop the decision), and a read that fails there counts as « in the arena »: in doubt, stopped.
    /// </summary>
    public static bool IsCut(bool loggedIn,uint territory,bool isPvP,IReadOnlySet<uint>? zones,bool previous,Func<bool> inInstance)
    {
        if(!loggedIn)return false;
        if(isPvP || zones is null)return true;
        if(territory==0)return previous;
        if(!InstanceOnly.Contains(territory))return zones.Contains(territory);
        try{return inInstance();}
        catch(Exception){return true;}
    }
}
