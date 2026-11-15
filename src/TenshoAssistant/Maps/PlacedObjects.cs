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

    /// <summary>A map of the placement's territory: its projection (Map.OffsetX/OffsetY, Map.SizeFactor) and the bell and chest markers on it.</summary>
    public readonly record struct MapFrame(float OffsetX,float OffsetY,float SizeFactor,IReadOnlyList<(uint Icon,Vector2 At)> Marks);

    /// <summary>
    /// X-424 (0.1405): whether one of these maps (the other maps of the territory: another floor of the place) already has a marker of the
    /// same icon at this world spot. Eulmore's bell (world 7.2/31.5) is marked by the game on map 555, not on the territory's main map 498:
    /// it is not missing (probe: docs/evidence/0.1402-bells-and-chests.md, section 0.1405).
    /// </summary>
    public static bool MarkedOnAnyMap(IEnumerable<MapFrame> maps,uint icon,float x,float z)
    {
        foreach(var map in maps)
        {
            if(map.SizeFactor==0)continue;
            var pixel=Pixel(x,z,map.OffsetX,map.OffsetY,map.SizeFactor);
            if(map.Marks.Any(mark=>mark.Icon==icon && Vector2.Distance(mark.At,pixel)<=SameSpotPixels))return true;
        }
        return false;
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
