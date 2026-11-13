namespace TenshoAssistant.Guide;

/// <summary>
/// 0.1222 (X-257, first-player review G6): what a new player sees first. The side bar's secondary groups start folded (the 46 pages
/// all unfolded were too much at once); the player's own choice, saved at the first click on a group, wins from then on.
/// </summary>
public static class FirstScreen
{
    /// <summary>The navigation groups folded while the player has never folded or unfolded one.</summary>
    public static readonly IReadOnlySet<string> FoldedGroups = new HashSet<string>(StringComparer.Ordinal) { "Progression", "Content", "GoldSaucer", "References" };

    /// <summary>The folded groups: the saved choice, else the defaults.</summary>
    public static IReadOnlySet<string> Folded(HashSet<string>? saved) => saved ?? FoldedGroups;

    /// <summary>The set to save at the first click: a copy of the defaults, so that what the player saw stays as it was.</summary>
    public static HashSet<string> ToSave(HashSet<string>? saved) => saved ?? new HashSet<string>(FoldedGroups, StringComparer.Ordinal);
}
