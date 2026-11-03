using System.Numerics;
using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// g, R-03 (0.397): where a gathering source is on its map (texture pixel), for « Go there »: a gathering point from its exported position
// (world X/Z), a fishing spot from its map pixels (docs/evidence/0.126.0-fishing-probe.log); null when not known.
internal sealed partial class MainWindow
{
    private (MapDefinition Map,Vector2 Pixel)? GatheringPointPixel(GatheringSource source)
    {
        if(source.MapId==0 || maps.State.Catalog?.Find(source.MapId) is not { } map || mapLocal?.State.Index is not { } index)return null;
        if(source.IsFishing)return index.Fishing(source.PointId) is { } spot ? (map,new Vector2(spot.X,spot.Z)):null;
        return index.Gathering(source.BaseId) is { } point ? (map,MapProjection.PixelFromWorld(new Vector3(point.X,0,point.Z),map)):null;
    }
}
