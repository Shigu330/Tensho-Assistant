namespace TenshoAssistant.Beastmaster;

/// <summary>
/// X-745 (0.1767, the user's idea I-027 of 18/11): the beasts to capture, for the Beastmaster page's « To capture » tab. Where each is
/// caught is the game's (XBMPet columns 6 and 7: 1 a zone, 2 a duty; 0 the beast the job starts with, never captured:
/// docs/evidence/0.688-beast-places.md); the coordinates are the places seen in game (yours or shared, X-110), never guessed; the
/// techniques are the game's (XBMPet columns 9 and 10). Pure (tests).
/// </summary>
public static class BeastCapture
{
    /// <summary>XBMPet column 6: where a beast is caught.</summary>
    public const int InZone = 1, InDuty = 2;

    /// <summary>
    /// The beasts to list: those caught in a zone first, then those caught in a duty, each group by its place then by name; the beast the
    /// job starts with (and any of another kind) left out. With <paramref name="onlyMissing"/> and the list of beasts obtained known, those
    /// obtained are left out too; an unknown list hides nothing.
    /// </summary>
    public static IReadOnlyList<Beast> Rows(IEnumerable<Beast> beasts, IReadOnlySet<uint>? obtained, bool onlyMissing) =>
        beasts.Where(beast => beast.CatchKind is InZone or InDuty && !(onlyMissing && obtained is not null && obtained.Contains(beast.Id)))
            .OrderBy(beast => beast.CatchKind).ThenBy(beast => beast.CatchPlace, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(beast => beast.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(beast => beast.Id).ToArray();

    /// <summary>How many of the beasts that can be captured are not obtained yet; null when the list of beasts obtained is not known.</summary>
    public static int? Missing(IEnumerable<Beast> beasts, IReadOnlySet<uint>? obtained) =>
        obtained is null ? null : beasts.Count(beast => beast.CatchKind is InZone or InDuty && !obtained.Contains(beast.Id));
}
