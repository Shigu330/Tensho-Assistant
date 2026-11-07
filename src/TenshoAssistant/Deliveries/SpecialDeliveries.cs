using TenshoAssistant.Quests;

namespace TenshoAssistant.Deliveries;

/// <summary>W-12 (0.715): the crafters' and gatherers' delivery quest lines, by their journal genre: Crystalline Mean, Studium, Wachumeqimeqi. docs/evidence/0.715-special-deliveries.md.</summary>
public static class SpecialDeliveries
{
    public static readonly IReadOnlyList<uint> Genres=[204,210,216];

    /// <summary>The genre's quests by level, then in the game's order; done ones can be left out.</summary>
    public static IReadOnlyList<QuestEntry> Of(IEnumerable<QuestEntry> entries,uint genre,bool onlyLeft)=>
        entries.Where(entry=>entry.GenreId==genre && (!onlyLeft || entry.Progress!=QuestProgress.Complete)).OrderBy(entry=>entry.Level).ThenBy(entry=>entry.Id).ToArray();

    public static (int Done,int Total) Count(IEnumerable<QuestEntry> entries,uint genre)
    {
        var rows=entries.Where(entry=>entry.GenreId==genre).ToArray();
        return (rows.Count(entry=>entry.Progress==QuestProgress.Complete),rows.Length);
    }
}
