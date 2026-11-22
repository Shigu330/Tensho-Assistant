using TenshoAssistant.Collections;

namespace TenshoAssistant.Monsters;

/// <summary>
/// X-742 (idea I-035 chosen by the user on 21/11, slices 1 and 2): the blue magic spells the player wants, checked on the Blue magic page and
/// kept in the settings (spell ids, the AozAction rows), copied as a list grouped by place, so several can be learned on one outing; the list
/// sorted by place; the place linked to the Tensho map or to the duty only when the game's sheets name one map or one duty (never guessed).
/// The places are the game's own (AozActionTransient.Location: a place name, a territory or a duty); the subjects only came from a list the
/// user pasted (R05: no data taken from it). Pure (tests).
/// </summary>
public static class BlueSpellPlan
{
    /// <summary>The spell checked or unchecked; true when it is now wanted.</summary>
    public static bool Toggle(List<uint> wanted,uint spell)
    {
        if(wanted.Remove(spell))return false;
        wanted.Add(spell);
        return true;
    }

    /// <summary>The spells by place (the game's name, in the client's language; a spell without a place last), then by number.</summary>
    public static CollectionEntry[] ByPlace(IEnumerable<CollectionEntry> spells)=>
        [.. spells.OrderBy(spell=>spell.Location.Length==0).ThenBy(spell=>spell.Location,StringComparer.CurrentCultureIgnoreCase).ThenBy(spell=>spell.Number)];

    /// <summary>
    /// The text copied: the spells wanted and not learned yet, one line per place (« Place: 12 Name, 15 Name »), the places in order; empty
    /// when none. A spell without a place goes under <paramref name="noPlace"/>.
    /// </summary>
    public static string CopyText(IEnumerable<CollectionEntry> spells,IReadOnlyCollection<uint> wanted,string noPlace)
    {
        var chosen=spells.Where(spell=>spell.Kind==CollectionKind.BlueMagic && wanted.Contains(spell.Id) && !spell.ConfirmedUnlocked);
        return string.Join("\n",ByPlace(chosen).GroupBy(spell=>spell.Location,StringComparer.CurrentCultureIgnoreCase)
            .Select(place=>(place.Key.Length==0 ? noPlace:place.Key)+": "+string.Join(", ",place.Select(spell=>$"{spell.Number} {spell.Name}"))));
    }

    /// <summary>
    /// The map of a place name, only when every territory of the game with that place name and a map has the same one (else 0: a place shared
    /// by several maps is not linked, never guessed).
    /// </summary>
    public static uint MapOfPlace(IEnumerable<(uint PlaceName,uint Map)> territories,uint placeName)
    {
        var maps=territories.Where(territory=>territory.PlaceName==placeName && territory.Map!=0).Select(territory=>territory.Map).Distinct().Take(2).ToArray();
        return maps.Length==1 ? maps[0]:0;
    }
}
