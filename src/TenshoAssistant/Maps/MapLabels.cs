using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>
/// 0.596 (P12): where the name of a map icon goes, as on the game's map. Measured from the MapMarker sheet: 1 left, 2 right, 3 below, 4 above
/// (docs/evidence/0.596-map-label-orientation.md); the rare other values are not established and go to the right.
/// </summary>
public static class MapLabels
{
    /// <summary>
    /// 0.623 (user report, with captures of the game's map): zoomed out, the game keeps only the names of map links; the others come back close up. The exact zoom of the
    /// game is not known: this one is to adjust in game.
    /// </summary>
    public const float DetailZoom=1f;
    public static bool Shown(bool mapLink,float zoom)=>mapLink || zoom>=DetailZoom;

    /// <summary>The names that fit, map links first, then in the order given: a name that would touch one already kept is left out.</summary>
    public static int[] Fitting(IReadOnlyList<(Vector2 At,Vector2 Size,bool MapLink)> names)
    {
        var kept=new List<int>();
        foreach(var index in Enumerable.Range(0,names.Count).OrderByDescending(index=>names[index].MapLink))
        {
            var (at,size,_)=names[index];
            if(kept.All(other=>at.X+size.X<=names[other].At.X || names[other].At.X+names[other].Size.X<=at.X || at.Y+size.Y<=names[other].At.Y || names[other].At.Y+names[other].Size.Y<=at.Y))kept.Add(index);
        }
        return kept.ToArray();
    }

    /// <summary>The top-left corner of the text, from the icon's centre.</summary>
    public static Vector2 Offset(byte orientation,float iconSize,Vector2 text)
    {
        var gap=iconSize/2+1;
        return orientation switch
        {
            1=>new Vector2(-gap-text.X,-text.Y/2),
            3=>new Vector2(-text.X/2,gap),
            4=>new Vector2(-text.X/2,-gap-text.Y),
            _=>new Vector2(gap,-text.Y/2),
        };
    }
}
