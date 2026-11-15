namespace TenshoAssistant.Maps;

/// <summary>X-462 (0.1464): a box of a territory's layout (planmap.lgb, LayerEntryType.MapRange) that names the map the game opens there: its
/// centre, half extents and turn about the vertical axis, in world units (docs/evidence/0.1464-map-floors.md).</summary>
public sealed record MapFloorRange(uint MapId,float X,float Y,float Z,float HalfX,float HalfY,float HalfZ,float TurnY);

/// <summary>
/// X-462 (0.1464): the floors of a territory drawn on several maps of one frame (same territory, scale and offset), each with its own marker range:
/// Eulmore's The Root, The Trunk and The Canopy (maps 498, 554, 555), 63 frames in 62 territories (probe PROBE_FLOORS, 11/15). An icon of a floor
/// shows on that floor only; versions of one land (Auxesia's stages, the Island Sanctuary's) share a marker range, or the default one has none,
/// and stay together. A layout's placement is recorded on the territory's default map: in a territory with floors, its map is the floor of the
/// box holding it (the 117 Level NPCs of Eulmore all fall in their own map's box), and one in no box is on no floor (never on one by chance).
/// Pure.
/// </summary>
public static class MapFloors
{
    /// <summary>Another floor of the same frame: both maps have a marker range, and they differ.</summary>
    public static bool OtherFloor(MapDefinition shown,MapDefinition other)
        =>other.MapId!=shown.MapId && shown.MarkerRange!=0 && other.MarkerRange!=0 && other.MarkerRange!=shown.MarkerRange;

    /// <summary>The box holds the point (its turn about the vertical axis undone first).</summary>
    public static bool Contains(MapFloorRange range,float x,float y,float z)
    {
        var dx=x-range.X;var dz=z-range.Z;
        var cos=MathF.Cos(-range.TurnY);var sin=MathF.Sin(-range.TurnY);
        var localX=dx*cos-dz*sin;var localZ=dx*sin+dz*cos;
        return MathF.Abs(localX)<=range.HalfX && MathF.Abs(y-range.Y)<=range.HalfY && MathF.Abs(localZ)<=range.HalfZ;
    }

    /// <summary>The map of the smallest box holding the point; 0 when none does.</summary>
    public static uint At(IReadOnlyList<MapFloorRange> ranges,float x,float y,float z)
    {
        uint found=0;var volume=float.MaxValue;
        foreach(var range in ranges)
        {
            var size=range.HalfX*range.HalfY*range.HalfZ;
            if(size<volume && Contains(range,x,y,z)){found=range.MapId;volume=size;}
        }
        return found;
    }

    /// <summary>A territory drawn on floors: its boxes name two maps or more.</summary>
    public static bool Layered(IReadOnlyList<MapFloorRange> ranges)=>ranges.Select(range=>range.MapId).Distinct().Skip(1).Any();

    /// <summary>The map of a layout's placement: the territory's map when it has no floors, else the floor holding it (0: on none).</summary>
    public static uint MapOf(IReadOnlyList<MapFloorRange> ranges,uint territoryMap,float x,float y,float z)=>Layered(ranges) ? At(ranges,x,y,z):territoryMap;
}
