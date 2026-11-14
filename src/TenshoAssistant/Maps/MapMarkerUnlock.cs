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
