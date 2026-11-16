namespace TenshoAssistant.Maps;

/// <summary>X-462 (0.1464): a box of a territory's layout (planmap.lgb, LayerEntryType.MapRange) that names the map the game opens there: its
/// centre, half extents and turn about the vertical axis, in world units (docs/evidence/0.1464-map-floors.md).</summary>
public sealed record MapFloorRange(uint MapId,float X,float Y,float Z,float HalfX,float HalfY,float HalfZ,float TurnY);

/// <summary>X-480 (0.1469): a map as the floors compare it: the territory it draws, its scale and offset (its frame) and its marker range.</summary>
public readonly record struct MapFloorFrame(uint MapId,uint TerritoryId,int Size,int OffsetX,int OffsetY,uint Markers);

/// <summary>
/// X-462 (0.1464): the floors of a territory drawn on several maps of one frame (same territory, scale and offset), each with its own marker range:
/// Eulmore's The Root, The Trunk and The Canopy (maps 498, 554, 555). An icon of a floor shows on that floor only; versions of one land (Auxesia's
/// stages, the Island Sanctuary's) share a marker range, or the default one has none, and stay together.
/// X-480 (0.1469, review of 0.1467 I2): a layout's placement is recorded on the territory's map; it moves to another map only when the territory's
/// layout splits it into floors: the boxes kept are those whose map is the territory's map or another floor of its frame (Floors). planmap.lgb
/// names two maps or more in 316 territories (probe PROBE_FLOORS=check, docs/evidence/0.1469-map-floors-check.md), mostly not floors: the maps
/// of another territory sharing the layout (instanced copies), a dungeon's areas drawn at their own offset (another frame: coordinates computed
/// on the territory's map would be wrong there), versions of one land. A placement in no kept box stays on the territory's map (0.1464 dropped
/// one in no box); in boxes of two floors, the smallest box wins (<see cref="At"/>; X-492 M2: this line said it stayed on the territory's map,
/// the code and the test of 0.1464 « the smallest box wins » never did). Pure.
/// </summary>
public static class MapFloors
{
    /// <summary>Another floor of the same frame: both maps have a marker range, and they differ.</summary>
    public static bool OtherFloor(uint shown,uint shownMarkers,uint other,uint otherMarkers)
        =>other!=shown && shownMarkers!=0 && otherMarkers!=0 && otherMarkers!=shownMarkers;

    /// <summary>Same frame: same territory, scale and offset (a coordinate reads the same on both maps).</summary>
    public static bool SameFrame(MapFloorFrame one,MapFloorFrame other)
        =>one.TerritoryId==other.TerritoryId && one.Size==other.Size && one.OffsetX==other.OffsetX && one.OffsetY==other.OffsetY;

    /// <summary>The boxes that split the territory's map into floors: their map is the territory's map, or another floor of its frame (frameOf: null for a map that does not exist).</summary>
    public static IReadOnlyList<MapFloorRange> Floors(IReadOnlyList<MapFloorRange> ranges,MapFloorFrame home,Func<uint,MapFloorFrame?> frameOf)
        =>[.. ranges.Where(range=>range.MapId==home.MapId
                                  || frameOf(range.MapId) is { } frame && SameFrame(frame,home) && OtherFloor(home.MapId,home.Markers,frame.MapId,frame.Markers))];

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

    /// <summary>A territory drawn on floors: its kept boxes (Floors) name two maps or more.</summary>
    public static bool Layered(IReadOnlyList<MapFloorRange> floors)=>floors.Select(range=>range.MapId).Distinct().Skip(1).Any();

    /// <summary>The map of a layout's placement, from the territory's floors (Floors): the floor of the smallest box holding it; the territory's map
    /// when the territory has no floors or the point is in no box. Never 0.</summary>
    public static uint MapOf(IReadOnlyList<MapFloorRange> floors,uint territoryMap,float x,float y,float z)
        =>Layered(floors) && At(floors,x,y,z) is var found and not 0 ? found:territoryMap;
}
