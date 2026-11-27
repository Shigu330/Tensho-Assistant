using System.Reflection;
using System.Text.RegularExpressions;
using TenshoAssistant.Community;

// 0.1908 (X-886): the wiki's acquisition page is read with the linear engine (as GardenJournal since X-428): its time is bounded by the
// page's length, so it needs no timeout, and a busy processor can no longer turn a page into a read error (the 1 s timeout ran out once
// on a page of a few lines under the whole suite's load). The same lines as before (WikiItemAcquisitionTests unchanged).
internal static class WikiLinear1908Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Wiki linear 1908: every expression of the wiki's acquisition reader is linear, without a timeout, and none is built at each call (X-886)", () =>
        {
            var members = typeof(WikiItemAcquisitionParser).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(member => member.FieldType == typeof(Regex)).ToList();
            Check(members.Count >= 11, "the reader's expressions held once: " + members.Count);
            foreach (var member in members)
            {
                var regex = (Regex)member.GetValue(null)!;
                Check(regex.Options.HasFlag(RegexOptions.NonBacktracking) && regex.MatchTimeout == Regex.InfiniteMatchTimeout, member.Name + ": linear, no timeout");
            }
            var source = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Community/WikiItemAcquisition.cs"));
            Check(!source.Contains("Regex.Replace(", StringComparison.Ordinal) && !source.Contains("Regex.Match(", StringComparison.Ordinal) && !source.Contains("Regex.IsMatch(", StringComparison.Ordinal), "no expression built at each call");
        }),
        ("Wiki linear 1908: behaviour: a long line of nested brackets is read at once, without a timeout (X-886)", () =>
        {
            var line = "* " + string.Concat(Enumerable.Repeat("[[a|{{b|[https://x ", 4000)) + "end";
            var page = WikiItemAcquisitionParser.Parse("==Acquisition==\n" + line + "\n", 1, "t", DateTimeOffset.UnixEpoch);
            Check(page.Lines.Count == 1 && page.Lines[0].Text.EndsWith("…", StringComparison.Ordinal), "one line, cut at 200 characters");
        }),
    ];
}
