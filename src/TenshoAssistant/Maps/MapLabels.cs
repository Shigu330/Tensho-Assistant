using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>
/// 0.596 (P12): where the name of a map icon goes, as on the game's map. Measured from the MapMarker sheet: 1 left, 2 right, 3 below, 4 above
/// (docs/evidence/0.596-map-label-orientation.md); the rare other values are not established and go to the right.
/// </summary>
/// <summary>P17 (0.652): a leve's marker on the map: the icon read in game, or a tooltip naming a row of the Leve sheet (docs/evidence/0.652-leve-markers.md).</summary>
public static class MapLeves
{
    public const uint AreaIcon=60492;
    /// <summary>The sheet writes « x : y », the tooltip « x: y » and may start with a glyph: only letters and digits are compared.</summary>
    public static string Key(string name)=>string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    /// <summary>0.655 (user report): a journal target on a leve goes with the leve. Not known (no list read, or not a leve): it stays.</summary>
    public static bool TargetGone(string? leve,IReadOnlySet<string>? accepted)=>leve is { Length:>0 } && accepted is not null && !accepted.Contains(leve);
    public static bool Is(uint icon,byte markerType,string tooltip,IReadOnlySet<string>? names)
        =>markerType!=3 && (icon==AreaIcon || tooltip.Length>0 && names?.Contains(Key(tooltip))==true);
}

/// <summary>
/// 0.653 (user report): while the journal has the game's map on a leve, the game empties its event markers, every one of them, and fills them again once that request ends
/// (docs/evidence/0.653-markers-during-journal-request.md). Those read just before stay until then, on the same map.
/// </summary>
public static class MapEventHold
{
    public static bool Holds(int read,bool journalRequest,uint currentMap,uint keptMap)=>read==0 && journalRequest && currentMap!=0 && currentMap==keptMap;
}

/// <summary>
/// 0.654 (user report): the game gives a leve's areas only for some reads of its map. Those seen stay remembered, map by map, as long as the game lists the leve as accepted.
/// A new read of the same leve on the same map replaces them. An absence says nothing: the list is often missing them while the leve runs.
/// </summary>
public sealed class LeveAreaMemory<T>
{
    private const int MaxAreas=64;
    private readonly Dictionary<(uint Map,string Leve),List<T>> areas=[];
    /// <summary>0.657: an area from the journal's request, for a leve and a map with none known: what the game's list gave stays the better source.</summary>
    public void Seed(uint map,string leve,T area)
    {
        if(!areas.ContainsKey((map,leve)) && areas.Sum(pair=>pair.Value.Count)<MaxAreas)areas[(map,leve)]=[area];
    }
    /// <summary>Returns what to add to the markers read: the areas remembered that this read does not give.</summary>
    public IReadOnlyList<T> Update(IEnumerable<(uint Map,string Leve,T Area)> seen,IReadOnlySet<string> accepted)
    {
        foreach(var key in areas.Keys.Where(key=>!accepted.Contains(key.Leve)).ToArray())areas.Remove(key);
        var given=new HashSet<(uint Map,string Leve)>();
        foreach(var group in seen.Where(area=>accepted.Contains(area.Leve)).GroupBy(area=>(area.Map,area.Leve)))
        {
            given.Add(group.Key);
            if(areas.ContainsKey(group.Key) || areas.Sum(pair=>pair.Value.Count)<MaxAreas)areas[group.Key]=group.Select(area=>area.Area).Take(MaxAreas).ToList();
        }
        return areas.Where(pair=>!given.Contains(pair.Key)).SelectMany(pair=>pair.Value).ToArray();
    }
}

public static class MapLabels
{
    /// <summary>
    /// 0.623 (user report, with captures of the game's map): zoomed out, the game keeps only the names of map links; the others come back close up. The exact zoom of the
    /// game is not known: this one is to adjust in game. 0.660 (user report, too late; docs/evidence/0.660-map-name-sizes.md): the game still writes them at about 0.94 of it; by eye below that.
    /// </summary>
    public const float DetailZoom=0.75f;
    /// <summary>0.660: the size of the names against the setting, measured on the widths of the same names on both maps.</summary>
    public const float PlaceNameShare=0.65f,IconNameShare=0.41f;
    /// <summary>
    /// 0.631 (user capture, docs/evidence/0.631-region-zone-images.md): on a region map a link's icon is the picture of that whole zone (1024 px), drawn at its own size in the map's pixels.
    /// Normal map icons are far smaller.
    /// </summary>
    public static bool IsZoneImage(uint linkMap,int width)=>linkMap!=0 && width>=256;
    /// <summary>0.632: map pixels per zone pixel on a region map, measured on the game's crystals (docs/evidence/0.631-region-zone-images.md). The zone's picture covers its 2048 pixels.</summary>
    public const float ZoneScale=0.42f;
    public static Vector2 ZonePixel(Vector2 linkMarker,Vector2 zonePixel)=>linkMarker+(zonePixel-new Vector2(1024))*ZoneScale;
    /// <summary>
    /// 0.639 (user request): a marker with no link of its own that still opens the map it names, like a dungeon's entrance or Ville fantôme. 0.640 (user captures): every kind of door has its
    /// own icon, so the name decides: an icon named like a map (services have no name). Not plain place names (icon 0). See docs/evidence/0.639-instance-markers.md.
    /// </summary>
    public static bool OpensNamedMap(uint icon,uint dataType)=>dataType==0 && icon!=0;
    /// <summary>0.642 (user capture, too large once zoomed out): a place name's height on the map, read on the game's own map (about 18 px at zoom 0.39): it never exceeds this.</summary>
    public const float PlaceNameMapHeight=46f;
    public static float PlaceNameSize(float size,float zoom,float scale)=>Math.Max(6,Math.Min(size*PlaceNameShare,PlaceNameMapHeight*zoom*scale));
    public static float IconNameSize(float size)=>Math.Max(6,size*IconNameShare);
    /// <summary>0.645 (P7): an aetheryte missing from the game's teleport list. Unknown (list not read, or empty) says nothing.</summary>
    public static bool NotAttuned(uint aetheryte,IReadOnlyDictionary<uint,uint>? costs)=>aetheryte!=0 && costs is { Count:>0 } && !costs.ContainsKey(aetheryte);
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
