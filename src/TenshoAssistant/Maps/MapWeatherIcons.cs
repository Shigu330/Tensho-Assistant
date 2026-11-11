using System.Numerics;
using TenshoAssistant.Gathering;

namespace TenshoAssistant.Maps;

/// <summary>X-114 (0.1049): the current weather of one zone on Tensho's map; <see cref="At"/> is the zone's place on a region map (map
/// pixels), null on the zone's own map (drawn in a corner of the canvas).</summary>
public sealed record MapWeatherMark(uint TerritoryId,uint Icon,string Name,Vector2? At);

/// <summary>
/// X-114 (0.1049, user request of 2026-11-10): an icon of the current weather of each zone, on Tensho's map only (the game's compass already
/// shows it), drawn only while the mouse is over the map. Zone map: one icon, its own zone. Region map: one icon per zone shown (the linked
/// zone pictures), once per zone. The weather is the game's forecast (WeatherCatalog.Forecast, like the vista tooltip): a zone without a known
/// weather (individual or forced weather, X-96, no table) gets no icon, never a guess. The list is built once per map and weather period
/// (1400 s, 8 Eorzean hours: one weather), off the per-frame path.
/// </summary>
public sealed class MapWeatherIcons
{
    public const long PeriodSeconds=1400;
    private (uint Map,long Period)? key;
    private IReadOnlyList<MapWeatherMark> marks=[];

    /// <summary>The marks of a map: its own zone when <paramref name="ownTerritory"/> is not 0, otherwise the zones its links lead to.</summary>
    public static IReadOnlyList<MapWeatherMark> Build(uint ownTerritory,IEnumerable<(Vector2 At,uint LinkMapId)> links,Func<uint,uint> territoryOf,Func<uint,WeatherDefinition?> weatherOf)
    {
        var result=new List<MapWeatherMark>();
        if(ownTerritory!=0)
        {
            if(weatherOf(ownTerritory) is { Icon:not 0 } own)result.Add(new(ownTerritory,own.Icon,own.Name,null));
            return result;
        }
        var seen=new HashSet<uint>();
        foreach(var (at,link) in links)
        {
            if(link==0)continue;
            var territory=territoryOf(link);
            if(territory==0 || !seen.Add(territory))continue;
            if(weatherOf(territory) is { Icon:not 0 } weather)result.Add(new(territory,weather.Icon,weather.Name,at));
        }
        return result;
    }

    /// <summary>The marks for this map and period, built again only when either changes; nothing is kept while the catalogues are not
    /// <paramref name="ready"/> (asked again on the next frame).</summary>
    public IReadOnlyList<MapWeatherMark> For(uint mapId,long unixSeconds,bool ready,Func<IReadOnlyList<MapWeatherMark>> build)
    {
        if(!ready){key=null;marks=[];return marks;}
        var period=unixSeconds<0 ? -1:unixSeconds/PeriodSeconds;
        if(key is { } known && known.Map==mapId && known.Period==period)return marks;
        marks=build();key=(mapId,period);
        return marks;
    }
}
