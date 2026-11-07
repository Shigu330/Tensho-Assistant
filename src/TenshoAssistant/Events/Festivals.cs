namespace TenshoAssistant.Events;

/// <summary>A festival the game says is running now (GameMain.ActiveFestivals: Festival row and phase).</summary>
public readonly record struct ActiveFestival(uint Id,int Phase);

/// <summary>What the page shows of the running festivals: read at, festivals, or why they are not known (null = not read yet).</summary>
public sealed record FestivalState(DateTimeOffset ReadAt,IReadOnlyList<ActiveFestival> Active);

/// <summary>
/// 0.342 (user request: event shops and the right names): the NPCs each festival places (layers of planevent.lgb with FestivalID, with
/// the territory), and its guide NPC's name in the client's language (the NPC whose CustomTalk is « Fes…Guide »).
/// </summary>
public sealed record FestivalLayouts(IReadOnlyDictionary<uint,IReadOnlyList<(uint Npc,uint Territory)>> Npcs,IReadOnlyDictionary<uint,string> Guides);

/// <summary>
/// R-09 (0.342, DEC-023 point 7, user request « follow the seasonal events »): the game holds the running festivals (GameMain) and the
/// quests of each (Quest.Festival); it holds neither their names (the Festival sheet has none) nor their dates. Names: LuminaSupplemental
/// FestivalName (GPL-3.0, English only, embedded with its notice). No upcoming dates: no reliable source (docs/evidence/0.342-festivals.md).
/// </summary>
public static class Festivals
{
    /// <summary>
    /// 0.810 (X-45, user report: event vendors on the map while absent in game): the NPCs a festival places (by territory) that no running festival places.
    /// Running festivals not read yet: none running, so every festival NPC is absent (they are there only a few weeks a year).
    /// </summary>
    public static IReadOnlySet<(uint Npc,uint Territory)> Absent(FestivalLayouts layouts,IReadOnlySet<uint> running)
    {
        var present=layouts.Npcs.Where(pair=>running.Contains(pair.Key)).SelectMany(pair=>pair.Value).ToHashSet();
        return layouts.Npcs.Where(pair=>!running.Contains(pair.Key)).SelectMany(pair=>pair.Value).Where(place=>!present.Contains(place)).ToHashSet();
    }

    /// <summary>FestivalName rows (id, English name); « None » and « Unknown » are no name.</summary>
    public static IReadOnlyDictionary<uint,string> ReadNames(TextReader reader)
    {
        var names=new Dictionary<uint,string>();
        string? line;
        while((line=reader.ReadLine()) is not null)
        {
            if(line.Length==0 || line.StartsWith('#'))continue;
            var comma=line.IndexOf(',');
            if(comma<=0 || !uint.TryParse(line.AsSpan(0,comma),out var id))continue;
            var name=line[(comma+1)..].Trim();
            if(name.Length>0 && name is not ("None" or "Unknown"))names[id]=name;
        }
        return names;
    }

    /// <summary>
    /// French client: the festival's own name taken from its guide NPC's name, after the first « de la », « de l' », « du », « des » or
    /// « de » (« démarcheur de la Veillée des saints » → « Veillée des saints »); null when the name has none of them.
    /// </summary>
    public static string? FrenchNameFromGuide(string guide)
    {
        // The earliest separator wins (« démarcheuse des Feux de la Mort » → « Feux de la Mort », not « Mort »).
        (int At,int Length)? first=null;
        foreach(var separator in new[]{" de la "," de l'"," du "," des "," de "})
        {
            var at=guide.IndexOf(separator,StringComparison.Ordinal);
            if(at>=0 && (first is not { } known || at<known.At || at==known.At && separator.Length>known.Length))first=(at,separator.Length);
        }
        if(first is not { } found)return null;
        var rest=guide[(found.At+found.Length)..].Trim();
        return rest.Length==0 ? null:char.ToUpper(rest[0],System.Globalization.CultureInfo.GetCultureInfo("fr-FR"))+rest[1..];
    }

    /// <summary>A festival the page leaves out: a game flag with no quest and no NPC of its own (« Special Event Flag » rows).</summary>
    public static bool Technical(uint id,IReadOnlyDictionary<uint,string> englishNames,IReadOnlyDictionary<uint,IReadOnlyList<uint>>? quests,FestivalLayouts? layouts)
        =>englishNames.GetValueOrDefault(id)=="Special Event Flag"
          || quests is not null && layouts is not null && !quests.ContainsKey(id) && !layouts.Npcs.ContainsKey(id);

    /// <summary>Running festivals, without the empty slots (id 0) and duplicates, in the game's order.</summary>
    public static IReadOnlyList<ActiveFestival> Running(IEnumerable<(ushort Id,ushort Phase)> slots)
        =>slots.Where(slot=>slot.Id!=0).Select(slot=>new ActiveFestival(slot.Id,slot.Phase)).DistinctBy(festival=>festival.Id).ToArray();
}
