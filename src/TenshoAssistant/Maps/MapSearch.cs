using System.Globalization;
using System.Numerics;

namespace TenshoAssistant.Maps;

public enum MapSearchKind {Place,Aetheryte,Vendor,Triad,Quest,Resource}
/// <summary>0.597 (P10): something to find on a map. Pixel: texture pixels, as the map view centres on.</summary>
public sealed record MapSearchHit(MapSearchKind Kind,string Name,string Zone,uint MapId,Vector2 Pixel);

/// <summary>Search over the hits built once from the game's data: every word in the name or the zone, accents and capitals ignored.</summary>
public static class MapSearch
{
    public const int MinimumLetters=2,MaximumShown=200;

    /// <summary>Without its duplicates: many points of one resource on one spot, a name repeated by a marker on the same place.</summary>
    public static MapSearchHit[] Distinct(IEnumerable<MapSearchHit> hits)
    {
        var seen=new HashSet<(MapSearchKind,string,uint,int,int)>();
        // Within 24 texture pixels the map shows the same place at the zoom a search opens on.
        return hits.Where(hit=>hit.Name.Length>0 && hit.MapId!=0 && float.IsFinite(hit.Pixel.X) && float.IsFinite(hit.Pixel.Y)
            && seen.Add((hit.Kind,hit.Name,hit.MapId,(int)MathF.Floor(hit.Pixel.X/24),(int)MathF.Floor(hit.Pixel.Y/24)))).ToArray();
    }

    /// <summary>Names that start with the search first, then by name. At most MaximumShown; Total says how many there were.</summary>
    public static (MapSearchHit[] Shown,int Total) Find(IReadOnlyList<MapSearchHit> hits,string? query,CultureInfo? culture=null)
    {
        var compare=(culture ?? CultureInfo.CurrentCulture).CompareInfo;
        var words=(query ?? "").Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        if(words.Sum(word=>word.Length)<MinimumLetters)return ([],0);
        const CompareOptions Loose=CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace;
        var found=hits.Where(hit=>words.All(word=>compare.IndexOf(hit.Name+" "+hit.Zone,word,Loose)>=0)).ToArray();
        var shown=found.OrderByDescending(hit=>compare.IsPrefix(hit.Name,words[0],Loose)).ThenBy(hit=>hit.Name,StringComparer.Create(culture ?? CultureInfo.CurrentCulture,true))
            .ThenBy(hit=>hit.Zone,StringComparer.Ordinal).Take(MaximumShown).ToArray();
        return (shown,found.Length);
    }
}
