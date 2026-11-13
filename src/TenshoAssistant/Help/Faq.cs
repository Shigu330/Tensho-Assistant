namespace TenshoAssistant.Help;

/// <summary>One question of the FAQ (system plan 4.19): its texts are Help.Question{Key} and Help.Answer{Key}; Page is the page its answer
/// names, opened from the answer.</summary>
public sealed record FaqEntry(string Key, string? Page = null);

/// <summary>
/// The FAQ's one list (system plan 4.19, plan F3). The Help page shows it and searches it with the pages; tools/faq.py writes docs/FAQ.md from
/// it and the English and French files (English first, then French). A test (FaqTests) refuses a question without its two texts in English
/// and French, a page that does not exist, or a docs/FAQ.md out of date. A question asked again and again in GitHub Discussions › Q&amp;A joins it.
/// </summary>
public static class Faq
{
    public static readonly IReadOnlyList<FaqEntry> Entries =
    [
        // 0.1222 (X-256, first-player review G12): where to start first; the install and the desktop app last (a player in the game
        // has installed it already, and the app is optional).
        new("WhereStart", "Help"),
        new("EmptyPage"),
        new("WhyOpen"),
        new("Marks"),
        new("Language", "Settings"),
        new("Internet", "Settings"),
        new("Offline", "Settings"),
        new("Safe"),
        new("Saved"),
        new("Files", "Storage"),
        new("Actions"),
        // X-253 (0.1183): M opens Tensho's map by default.
        new("Map", "Settings"),
        new("Panels", "Settings"),
        new("Pause"),
        new("Update"),
        new("Reset", "Settings"),
        new("Official"),
        new("Ask"),
        new("Report"),
        new("Translate"),
        new("Wishlist", "Wishlist"),
        // X-178 (0.1257): the garden in icons, and the way back to the text.
        new("GardenText", "Garden"),
        new("Install"),
        new("Desktop"),
        new("DesktopUpdate"),
        new("DesktopLink", "Settings"),
        new("DesktopInstall", "Settings"),
    ];

    /// <summary>The page an answer names, or null.</summary>
    public static string? PageOf(string key) => Entries.FirstOrDefault(entry => entry.Key == key)?.Page;
}
