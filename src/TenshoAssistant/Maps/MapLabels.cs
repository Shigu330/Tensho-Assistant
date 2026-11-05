using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>
/// 0.596 (P12): where the name of a map icon goes, as on the game's map. Measured from the MapMarker sheet: 1 left, 2 right, 3 below, 4 above
/// (docs/evidence/0.596-map-label-orientation.md); the rare other values are not established and go to the right.
/// </summary>
public static class MapLabels
{
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
