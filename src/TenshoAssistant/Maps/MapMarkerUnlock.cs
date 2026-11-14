namespace TenshoAssistant.Maps;

/// <summary>
/// X-322 (0.1320, user report of 2026-11-14 at Mor Dhona: an icon « Exchange » alone at X 21.4 Y 5.5, absent from the game's map). A fixed
/// map icon of the MapMarker sheet with a MapMarkerUnlockedBit (row 108.46 there: icon 60935, bit 44; 446 rows in the game) is drawn by the
/// game only once that unlock is gained; Tensho drew it always. docs/evidence/0.1320-map-marker-unlock.md.
/// </summary>
public static class MapMarkerUnlock
{
    /// <summary>
    /// Whether a fixed icon is drawn: no unlock bit, or its bit read unlocked in the game. Unknown (not read: outside the game, before the
    /// first read) hides it: a marker the game may not show is never shown as there (R06).
    /// </summary>
    public static bool Visible(ushort bit,IReadOnlySet<ushort>? unlocked)=>bit==0 || unlocked?.Contains(bit)==true;

    /// <summary>
    /// 0.1340 (X-351): the bits are read from QuestManager.UnlockedMapMarkers (the game's own array for these icons), no longer from
    /// UIState.IsUnlockLinkUnlocked (unlock links, another array): see MapLiveReader.ReadMarkerUnlocks.
    /// 0.1321 (X-322, dalamud.log of 11/14 11:39:48, Mor Dhona): the bit read by IsUnlockLinkUnlocked agreed with the game on 8 icons of 9;
    /// the icon of bit 83 (60467 at 1375, 653) is drawn by the game while that reading says locked. So on the map the game has built, its
    /// own list decides (an icon with a bit is drawn when the game lists it: same icon at the same pixel, GameList.Listed); on another
    /// map, the bit's reading (an error there only hides).
    /// </summary>
    /// <remarks>
    /// X-351 (0.1335, review of the block 0.1318 to 0.1329, I3): on a map the game has not built, the bit's reading alone hid what the game
    /// draws (bit 83). There, an icon is also drawn when the game listed it on that map earlier in the session (<see cref="GameList.Remembered"/>)
    /// or when its bit was seen drawn by the game while read locked (<see cref="GameList.DrawnWhileLocked"/>): what the game was seen to draw
    /// is never hidden again. The rule that needs no memory waits for the probe in the game (R06; candidates in the log).
    /// 0.1337 (probe of 11/14 15:41, map 13: bit 2, icon 60412 at 1062,813, read unlocked, not listed by the game): a map the game built earlier
    /// keeps its list as the rule (what it did not draw stays hidden too), except a bit unlocked since (read locked then, unlocked now).
    /// </remarks>
    public static bool Visible(ushort bit,uint icon,float pixelX,float pixelY,uint mapId,GameList? game,IReadOnlySet<ushort>? unlocked)
    {
        if(bit==0)return true;
        if(game is { } built && built.MapId==mapId)return built.Listed.Contains((icon,pixelX,pixelY));
        if(game?.Remembered.TryGetValue(mapId,out var seen)==true)
            return seen.Listed.Contains((icon,pixelX,pixelY)) || Visible(bit,unlocked) && !seen.UnlockedThen.Contains(bit);
        return Visible(bit,unlocked) || game?.DrawnWhileLocked.Contains(bit)==true;
    }

    /// <summary>0.1337 (X-351): a map the game built earlier in the session: the icons it listed, and the bits read unlocked then.</summary>
    public sealed record SeenMap(IReadOnlySet<(uint Icon,float PixelX,float PixelY)> Listed,IReadOnlySet<ushort> UnlockedThen);

    /// <summary>
    /// 0.1321: the icons with an unlock bit that the game lists on the map it has built (AgentMap.CurrentMapId), by icon and sheet pixel.
    /// X-351 (0.1335): with what the game listed on the maps it built earlier in the session, and the bits it drew while their reading said locked.
    /// </summary>
    public sealed record GameList(uint MapId,IReadOnlySet<(uint Icon,float PixelX,float PixelY)> Listed)
    {
        public IReadOnlyDictionary<uint,SeenMap> Remembered{get;init;}=new Dictionary<uint,SeenMap>();
        public IReadOnlySet<ushort> DrawnWhileLocked{get;init;}=new HashSet<ushort>();
    }

    /// <summary>At most this many maps remembered (the game has about 500 maps with icons).</summary>
    public const int MaximumRemembered=1024;

    /// <summary>
    /// X-351 (0.1335): the list after the game has built <paramref name="mapId"/> with these checks: its listed icons, the earlier maps kept,
    /// the bits drawn while read locked added. The same object when nothing changed (the map's filter is kept by reference).
    /// </summary>
    public static GameList Remember(GameList? before,uint mapId,IReadOnlyList<Check> checks)
    {
        var listed=checks.Where(check=>check.Listed).Select(check=>(check.Icon,check.PixelX,check.PixelY)).ToHashSet();
        var drawn=checks.Where(check=>check.Listed && !check.Unlocked).Select(check=>check.Bit).ToHashSet();
        if(before is { } same && same.MapId==mapId && same.Listed.SetEquals(listed) && drawn.IsSubsetOf(same.DrawnWhileLocked))return same;
        var remembered=new Dictionary<uint,SeenMap>();
        if(before is not null)foreach(var (map,seen) in before.Remembered)if(remembered.Count<MaximumRemembered-1)remembered[map]=seen;
        remembered[mapId]=new(listed,checks.Where(check=>check.Unlocked).Select(check=>check.Bit).ToHashSet());
        if(before is not null)drawn.UnionWith(before.DrawnWhileLocked);
        return new(mapId,listed){Remembered=remembered,DrawnWhileLocked=drawn};
    }

    /// <summary>X-351 (0.1335): the self-check's verdict: the bit's reading agrees with the game on every icon (the review saw it always « Ok »).</summary>
    public static bool Agrees(IReadOnlyList<Check> checks)=>checks.All(check=>check.Listed==check.Unlocked);

    /// <summary>
    /// Where the game's own map list (AgentMap.MapMarkers) puts a sheet marker: (texture pixel − 1024) × 16. Mor Dhona's aetheryte (sheet 108.3
    /// at pixel 1064, 356, size factor 100) is listed at 640, −10688; 0.1340 (X-351, dalamud.log of 11/14 16:05:29, map 14, Ul'dah, size factor
    /// 200): 60441 at pixel 1209, 794 is listed at 2960, −3680, and the 50 other icons of the map the same way: the size factor is not applied
    /// (the 0.1320 rule « world × 16 » held at factor 100 only).
    /// </summary>
    public static (float X,float Y) GamePosition(float pixelX,float pixelY)=>((pixelX-1024)*16,(pixelY-1024)*16);

    /// <summary>
    /// 0.1338 (X-351): how many of a map's icons without an unlock bit (always drawn by the game) the game's list holds (same icon within two
    /// world units): the game's list belongs to the map that it holds best, the player's or the one its map window shows.
    /// </summary>
    public static (int Found,int Total) Coverage(IEnumerable<(uint Icon,float PixelX,float PixelY)> plain,IReadOnlyList<(uint Icon,int X,int Y,int Flags)> game)
    {
        int found=0,total=0;
        foreach(var (icon,pixelX,pixelY) in plain)
        {
            total++;
            var (x,y)=GamePosition(pixelX,pixelY);
            foreach(var marker in game)
                if(marker.Icon==icon && Math.Abs(marker.X-x)<=32 && Math.Abs(marker.Y-y)<=32){found++;break;}
        }
        return (found,total);
    }

    /// <summary>0.1338 (X-351): the game's list is taken as this map's when it holds at least half of its icons without a bit (and one at least).</summary>
    public static bool Holds((int Found,int Total) coverage)=>coverage.Total>0 && coverage.Found*2>=coverage.Total;

    /// <summary>
    /// 0.1342 (X-351, dalamud.log of 11/14 16:15:27, docs/evidence/0.1342-map-marker-bits.md): a bit of QuestManager.UnlockedMapMarkers, the
    /// most significant bit of each byte first (bit 0 = 0x80 of byte 0). Read through FFXIVClientStructs' BitArray (least significant first),
    /// map 14 showed bits 69 and 98 unlocked while the game draws neither; read this way, the ten icons checked against the game's own list
    /// (Mor Dhona and map 14) all agree. A bit past the array is unknown: not set (hidden, R06).
    /// </summary>
    public static bool IsSet(ReadOnlySpan<byte> array,ushort bit)=>bit/8<array.Length && (array[bit/8] & (0x80>>(bit%8)))!=0;

    /// <summary>One fixed icon with an unlock bit, compared with the game's own list: listed there (same icon within two world units), its flags (−1: not listed), and its bit read unlocked.</summary>
    public sealed record Check(ushort Bit,uint Icon,float PixelX,float PixelY,bool Listed,int Flags,bool Unlocked);

    /// <summary>For the self-check in the game: each sheet icon with an unlock bit of the map the game has open, against the game's list and the unlocked bits.</summary>
    public static IReadOnlyList<Check> Compare(IEnumerable<(ushort Bit,uint Icon,float PixelX,float PixelY)> sheet,
        IReadOnlyList<(uint Icon,int X,int Y,int Flags)> game,IReadOnlySet<ushort> unlocked)
    {
        var result=new List<Check>();
        foreach(var (bit,icon,pixelX,pixelY) in sheet)
        {
            if(bit==0)continue;
            var (x,y)=GamePosition(pixelX,pixelY);
            var flags=-1;
            foreach(var marker in game)
                if(marker.Icon==icon && Math.Abs(marker.X-x)<=32 && Math.Abs(marker.Y-y)<=32){flags=marker.Flags;break;}
            result.Add(new(bit,icon,pixelX,pixelY,flags>=0,flags,unlocked.Contains(bit)));
        }
        return result.AsReadOnly();
    }

    /// <summary>One line for the log: « bit 44 icon 60935 at 1019,225: listed no, unlocked no ».</summary>
    public static string Describe(Check check)=>$"bit {check.Bit} icon {check.Icon} at {check.PixelX:0},{check.PixelY:0}: listed {(check.Listed ? $"yes (flags {check.Flags:X4})":"no")}, unlocked {(check.Unlocked ? "yes":"no")}";
}
