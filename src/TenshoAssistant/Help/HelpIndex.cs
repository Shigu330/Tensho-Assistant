using TenshoAssistant.Diagnostics;

namespace TenshoAssistant.Help;

/// <summary>Help of one page (ONBOARD-001, lot 1): what it is for, how to start, what to do when it stays empty.</summary>
public sealed record HelpTopic(string Key, string Title, string Purpose, string Start, string Empty);

/// <summary>The state of a page on this character, from the self-check: the worst of the lines it depends on.</summary>
public enum HelpPageState { Unknown, Ready, Waiting, Unusual, Failed }

public static class HelpIndex
{
    private static readonly IReadOnlyDictionary<string, string[]> Domains = new Dictionary<string, string[]>
    {
        ["Items"] = ["items", "inventory"], ["Storage"] = ["inventory", "bank"], ["Market"] = ["items"], ["CraftSimulation"] = ["recipes", "character"],
        ["CraftLive"] = ["synthesis"], ["Gathering"] = ["gathering"], ["GatheringGp"] = ["character"], ["Maps"] = ["maps", "map"], ["Reminders"] = ["reminders"],
        ["FreeCompany"] = ["submarine data", "airship data"], ["NpcVendors"] = ["vendors"], ["DutyLoot"] = ["duties", "duty progress"], ["Quests"] = ["quests"],
        ["Collections"] = ["collections", "extra collections"], ["Gear"] = ["gear data", "gear sets", "worn gear"], ["Chocobo"] = ["chocobo"], ["Leves"] = ["leves"],
        ["HuntingLog"] = ["hunting log"], ["Beastmaster"] = ["beastmaster"], ["TripleTriad"] = ["triad"], ["MiniCactpot"] = ["cactpot ticket"],
    };
    /// <summary>Self-check domains a page depends on; none for a page that reads nothing of the character.</summary>
    public static IReadOnlyList<string> DomainsOf(string page) => Domains.GetValueOrDefault(page) ?? [];
    public static IEnumerable<string> AllDomains => Domains.Values.SelectMany(domains => domains).Distinct();

    public static HelpPageState StateOf(IEnumerable<SelfCheckLine> lines)
    {
        var state = HelpPageState.Unknown;
        foreach (var line in lines)
        {
            var one = line.Status switch
            {
                SelfCheckStatus.Failed => HelpPageState.Failed,
                SelfCheckStatus.Suspect => HelpPageState.Unusual,
                SelfCheckStatus.NotRead => HelpPageState.Waiting,
                _ => HelpPageState.Ready,
            };
            if (one > state) state = one;
        }
        return state;
    }

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
