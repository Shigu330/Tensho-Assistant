using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>X-684 (0.1672): what a place's tooltip says: zone and sub-zone, region, coordinates, the map's nearest aetheryte with its
/// teleport cost when the game's teleport list was read (not attuned when it is not in that list), the zone's weather now. Worked out once
/// for the place under the mouse (MainWindow.PlaceTooltip.cs keeps it until the place, the minute or the teleport list changes).</summary>
public sealed record PlaceTipFacts(string Area,string Region,string? Coordinates,string? Aetheryte,uint? AetheryteCost,bool? Attuned,string? Weather,uint WeatherIcon);

public static class PlaceTips
{
    /// <summary>The zone, then the sub-zone when it says more.</summary>
    public static string Area(MapDefinition map)=>map.Subzone.Length>0 && map.Subzone!=map.Zone ? $"{map.Zone} · {map.Subzone}":map.Zone;

    /// <summary>The map's aetheryte nearest to the point (texture pixels: the distance follows the world's); null without one.</summary>
    public static (uint Id,string Name)? Nearest(IEnumerable<(uint Id,float X,float Y,string Name)> aetherytes,Vector2 pixel)
    {
        (uint Id,string Name)? best=null;var bestDistance=float.MaxValue;
        foreach(var (id,x,y,name) in aetherytes)
        {
            if(id==0)continue;
            var distance=Vector2.DistanceSquared(new Vector2(x,y),pixel);
            if(distance<bestDistance){bestDistance=distance;best=(id,name);}
        }
        return best;
    }

    /// <summary>The facts of a place: <paramref name="coordinate"/> null for a whole zone (a residential district); <paramref name="costs"/>
    /// the game's teleport list (aetheryte → gil), null when not read; <paramref name="weather"/> the zone's weather now, null when not known.</summary>
    public static PlaceTipFacts Build(MapDefinition map,Vector2? coordinate,IEnumerable<(uint Id,float X,float Y,string Name)> aetherytes,
        IReadOnlyDictionary<uint,uint>? costs,(string Name,uint Icon)? weather)
    {
        string? text=null;(uint Id,string Name)? nearest=null;
        if(coordinate is { } point && MapCatalog.CoordinatesValid(point.X,point.Y))
        {
            text=MapProjection.CoordinateText(point);
            nearest=Nearest(aetherytes,MapProjection.PixelFromCoordinate(point,map));
        }
        uint? cost=nearest is { } found && costs is not null && costs.TryGetValue(found.Id,out var gil) ? gil:null;
        bool? attuned=nearest is { } aetheryte && costs is not null ? costs.ContainsKey(aetheryte.Id):null;
        return new(Area(map),map.Region,text,nearest?.Name,cost,attuned,weather?.Name,weather?.Icon ?? 0);
    }
}
