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
    /// </remarks>
    public static bool Visible(ushort bit,uint icon,float pixelX,float pixelY,uint mapId,GameList? game,IReadOnlySet<ushort>? unlocked)
        =>bit==0 || (game is { } built && built.MapId==mapId ? built.Listed.Contains((icon,pixelX,pixelY))
            :Visible(bit,unlocked) || game?.DrawnWhileLocked.Contains(bit)==true
             || game?.Remembered.TryGetValue(mapId,out var seen)==true && seen.Contains((icon,pixelX,pixelY)));

    /// <summary>
    /// 0.1321: the icons with an unlock bit that the game lists on the map it has built (AgentMap.CurrentMapId), by icon and sheet pixel.
    /// X-351 (0.1335): with what the game listed on the maps it built earlier in the session, and the bits it drew while their reading said locked.
    /// </summary>
    public sealed record GameList(uint MapId,IReadOnlySet<(uint Icon,float PixelX,float PixelY)> Listed)
    {
        public IReadOnlyDictionary<uint,IReadOnlySet<(uint Icon,float PixelX,float PixelY)>> Remembered{get;init;}=new Dictionary<uint,IReadOnlySet<(uint,float,float)>>();
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
        var remembered=new Dictionary<uint,IReadOnlySet<(uint,float,float)>>();
        if(before is not null)foreach(var (map,icons) in before.Remembered)if(remembered.Count<MaximumRemembered-1)remembered[map]=icons;
        remembered[mapId]=listed;
        if(before is not null)drawn.UnionWith(before.DrawnWhileLocked);
        return new(mapId,listed){Remembered=remembered,DrawnWhileLocked=drawn};
    }

    /// <summary>X-351 (0.1335): the self-check's verdict: the bit's reading agrees with the game on every icon (the review saw it always « Ok »).</summary>
    public static bool Agrees(IReadOnlyList<Check> checks)=>checks.All(check=>check.Listed==check.Unlocked);

    /// <summary>
    /// Where the game's own map list (AgentMap.MapMarkers) puts a sheet marker: world position × 16 (Mor Dhona's aetheryte, sheet 108.3 at
    /// pixel 1064, 356, is listed at 640, −10688), the world position from the texture pixel as MapProjection.PixelFromWorld reverses it.
    /// </summary>
    public static (float X,float Y) GamePosition(float pixelX,float pixelY,int sizeFactor,int offsetX,int offsetY)
    {
        var scale=Math.Max(1,sizeFactor)/100f;
        return (((pixelX-1024)/scale-offsetX)*16,((pixelY-1024)/scale-offsetY)*16);
    }

    /// <summary>One fixed icon with an unlock bit, compared with the game's own list: listed there (same icon within two world units), its flags (−1: not listed), and its bit read unlocked.</summary>
    public sealed record Check(ushort Bit,uint Icon,float PixelX,float PixelY,bool Listed,int Flags,bool Unlocked);

    /// <summary>For the self-check in the game: each sheet icon with an unlock bit of the map the game has open, against the game's list and the unlocked bits.</summary>
    public static IReadOnlyList<Check> Compare(IEnumerable<(ushort Bit,uint Icon,float PixelX,float PixelY)> sheet,int sizeFactor,int offsetX,int offsetY,
        IReadOnlyList<(uint Icon,int X,int Y,int Flags)> game,IReadOnlySet<ushort> unlocked)
    {
        var result=new List<Check>();
        foreach(var (bit,icon,pixelX,pixelY) in sheet)
        {
            if(bit==0)continue;
            var (x,y)=GamePosition(pixelX,pixelY,sizeFactor,offsetX,offsetY);
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
