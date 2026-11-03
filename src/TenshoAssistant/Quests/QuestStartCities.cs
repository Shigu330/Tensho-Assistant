namespace TenshoAssistant.Quests;

/// <summary>
/// ARR main scenario quests that belong to one starting city (0.285, docs/evidence/0.285-quest-start-cities.md). In journal genre 1
/// the roots are the quests without a previous quest of that genre (« Une visite s'impose », one per class group, in their city);
/// a quest whose previous-quest chain leads back only to roots of one city belongs to that city (city = place of its roots).
/// Quests reached from roots of several cities are the shared line. A character only plays its own city's line.
/// </summary>
public static class QuestStartCities
{
    public const uint ArrMainScenarioGenre = 1;

    /// <summary>Quest id → starting city (place name of its roots), for the city-specific quests only.</summary>
    public static IReadOnlyDictionary<uint, string> Of(IEnumerable<QuestEntry> entries)
    {
        var line = entries.Where(entry => entry.GenreId == ArrMainScenarioGenre).ToDictionary(entry => entry.Id);
        var roots = new Dictionary<uint, HashSet<uint>>();
        HashSet<uint> RootsOf(uint id)
        {
            if (roots.TryGetValue(id, out var known)) return known;
            roots[id] = []; // cycle guard
            var previous = line[id].PreviousQuestIds.Where(line.ContainsKey).ToArray();
            return roots[id] = previous.Length == 0 ? [id] : previous.SelectMany(RootsOf).ToHashSet();
        }
        var cities = new Dictionary<uint, string>();
        foreach (var id in line.Keys)
        {
            var places = RootsOf(id).Select(root => line[root].Place).Distinct().ToArray();
            if (places.Length == 1 && places[0].Length > 0) cities[id] = places[0];
        }
        return cities;
    }

    /// <summary>The character's city: the only city with a quest confirmed complete; null when none or several (then nothing is hidden).</summary>
    public static string? PlayerCity(IEnumerable<QuestEntry> entries, IReadOnlyDictionary<uint, string> cities)
    {
        var played = entries.Where(entry => entry.Progress == QuestProgress.Complete && cities.ContainsKey(entry.Id))
            .Select(entry => cities[entry.Id]).Distinct().ToArray();
        return played.Length == 1 ? played[0] : null;
    }
}
