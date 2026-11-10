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
        new("Install"),
        new("EmptyPage"),
        new("WhyOpen"),
        new("Marks"),
        new("Language", "Settings"),
        new("Internet", "Settings"),
        new("Offline", "Settings"),
        new("Saved"),
        new("Actions"),
        new("Pause"),
        new("Reset", "Settings"),
        new("Official"),
        new("Ask"),
        new("Report"),
        new("Translate"),
        new("Desktop"),
    ];

    /// <summary>The page an answer names, or null.</summary>
    public static string? PageOf(string key) => Entries.FirstOrDefault(entry => entry.Key == key)?.Page;
}
