using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>
/// X-516 (0.1711, the user's request of 16/11): the icon of a present gathering point on Tensho's map (a point the game streams around the
/// player), one per job and timing, from the game's own GatheringType map icons (probe of 21/11, docs/evidence/0.1711-gathering-node-icons.md):
/// miner 60438 (Mining's IconMain, a pickaxe), botanist 60432 (Harvesting's IconMain, a leaf), timed miner 60464 and timed botanist 60461 (their
/// IconOff: the same with the sparkles of the game's special points); the timed ones also get Tensho's clock badge. Pure.
/// </summary>
public static class GatheringNodeIcons
{
    public const uint Miner=60438,Botanist=60432,TimedMiner=60464,TimedBotanist=60461;

    /// <summary>GatheringType rows: 0 Mining and 1 Quarrying (miner), 2 Logging and 3 Harvesting (botanist); any other (spearfishing): none.</summary>
    public static uint For(uint gatheringType,bool timed)=>gatheringType switch
    {
        0 or 1=>timed ? TimedMiner:Miner,
        2 or 3=>timed ? TimedBotanist:Botanist,
        _=>0,
    };

    public static bool Timed(uint icon)=>icon is TimedMiner or TimedBotanist;

    /// <summary>
    /// A physical point is timed when its GatheringPointTransient row has a rare pop table or an ephemeral window (65535: none), as the
    /// gathering catalogue reads its schedule; the probe counts these exactly on the points of types 2, 4 and 5 (285 of 5601).
    /// </summary>
    public static bool TimedPoint(uint rarePopTable,ushort ephemeralStart,ushort ephemeralEnd)=>rarePopTable!=0 || ephemeralStart!=65535 && ephemeralEnd!=65535;

    /// <summary>The colour of a gathering point's area: its own for a timed point.</summary>
    public static Vector4 AreaColour(bool timed,Vector4 normal,Vector4 timedColour)=>timed ? timedColour:normal;
}
