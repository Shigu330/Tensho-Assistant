using System.Collections.Frozen;
using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>
/// X-386 (0.1402): summoning bells and company chests placed by the territories' layouts (event objects of planlive, planevent and planmap),
/// for the places where the game's own map has no marker of them (company workshops). Ids and icons from the game: EObjName « summoning bell »
/// (2014985 left out: only on Sinus Ardorum's festival layers) and « company chest »; MapSymbol 60425 « Summoning Bell » and 60460
/// « Company Chest » (docs/evidence/0.139.0-symbols-probe.log). Probe: docs/evidence/0.1402-bells-and-chests.md.
/// </summary>
public static class PlacedObjects
{
    public const uint BellIcon=60425,CompanyChestIcon=60460,CompanyChest=2000470;
    public static readonly FrozenSet<uint> Bells=new uint[]{2000401,2000403,2000441,2006565,2010284}.ToFrozenSet();
    public static readonly string[] LayoutKinds=["planlive","planevent","planmap"];
    /// <summary>A placement this close to a game marker of the same icon is that marker (farthest match of the probe: 27.5 px).</summary>
    public const float SameSpotPixels=32;
    public const float TexturePixels=2048;

    /// <summary>The map icon of an event object: bell, company chest, or 0 (not one of them).</summary>
    public static uint IconOf(uint eventObject)=>eventObject==CompanyChest ? CompanyChestIcon:Bells.Contains(eventObject) ? BellIcon:0;

    /// <summary>World X/Z to the map's texture pixels (Map.OffsetX/OffsetY, Map.SizeFactor), as MapMarker X/Y are written.</summary>
    public static Vector2 Pixel(float x,float z,float offsetX,float offsetY,float sizeFactor)
    {
        var scale=sizeFactor/100f;
        return new((x+offsetX)*scale+1024f,(z+offsetY)*scale+1024f);
    }

    /// <summary>
    /// Whether a layout placement is to be added: inside the map's texture (a housing ward's subdivision has its own map), and no mark of the
    /// same icon (the game's markers, and the placements already added) within <see cref="SameSpotPixels"/>.
    /// </summary>
    public static bool Missing(IEnumerable<(uint Icon,Vector2 At)> marks,uint icon,Vector2 pixel)
    {
        if(icon==0 || !float.IsFinite(pixel.X) || !float.IsFinite(pixel.Y) || pixel.X<0 || pixel.Y<0 || pixel.X>TexturePixels || pixel.Y>TexturePixels)return false;
        return !marks.Any(mark=>mark.Icon==icon && Vector2.Distance(mark.At,pixel)<=SameSpotPixels);
    }
}
