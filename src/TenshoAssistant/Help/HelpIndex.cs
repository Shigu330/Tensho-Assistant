namespace TenshoAssistant.Help;

/// <summary>Help of one page (ONBOARD-001, lot 1): what it is for, how to start, what to do when it stays empty.</summary>
public sealed record HelpTopic(string Key, string Title, string Purpose, string Start, string Empty);

public static class HelpIndex
{
    /// <summary>Topics holding every word of the query, those whose title holds it first, in their order otherwise. An empty query keeps them all.</summary>
    public static IReadOnlyList<HelpTopic> Search(IEnumerable<HelpTopic> topics, string query)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return Array.AsReadOnly(topics.ToArray());
        static bool Holds(string text, string word) => text.Contains(word, StringComparison.CurrentCultureIgnoreCase);
        return Array.AsReadOnly(topics
            .Where(topic => words.All(word => Holds(topic.Title, word) || Holds(topic.Purpose, word) || Holds(topic.Start, word) || Holds(topic.Empty, word)))
            .OrderByDescending(topic => words.Count(word => Holds(topic.Title, word)))
            .ToArray());
    }
}
