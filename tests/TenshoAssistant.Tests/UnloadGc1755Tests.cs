using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

// X-685 (0.1755): the line about an earlier instance says how many full collections ran since its unload: « context alive » after fewer than
// two proves nothing (the reading of 0.1752 in the game: one background collection of generation 2 between its unload and the line).
internal static class UnloadGc1755Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unload gc 1755: behaviour: the count noted at unload gives the full collections since, in the line; the mark is no part", () =>
        {
            var context = new object();
            var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal) { [UnloadWatch.Context] = new WeakReference(context), ["plugin"] = new WeakReference(null) };
            Check(UnloadWatch.Describe("a", parts) == "a: context alive, 0 of 1 parts alive", "nothing noted: the line of before: " + UnloadWatch.Describe("a", parts));
            UnloadWatch.Unloaded(parts, GC.CollectionCount(2) - 3);
            var line = UnloadWatch.Describe("a", parts);
            var match = Regex.Match(line, @"^a: context alive after (\d+) full collection\(s\) since its unload, 0 of 1 parts alive$");
            Check(match.Success && int.Parse(match.Groups[1].Value) >= 3, "the count since the unload (others may collect meanwhile): " + line);
            Check(UnloadWatch.FullCollectionsSinceUnload(parts, 100) is { } since && since >= 0, "read back");
            Check(UnloadWatch.FullCollectionsSinceUnload(new Dictionary<string, WeakReference>(), 5) is null, "none noted: null");
            var only = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
            UnloadWatch.Unloaded(only, 0);
            Check(UnloadWatch.Describe("b", only).EndsWith(", parts not noted", StringComparison.Ordinal), "the mark alone is no part: " + UnloadWatch.Describe("b", only));
            GC.KeepAlive(context);
        }),
        ("Unload gc 1755: wiring: the unload notes the count at its end, outside the developer mode's notes", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var dispose = plugin[plugin.IndexOf("public void Dispose()", StringComparison.Ordinal)..];
            var noted = dispose.IndexOf("if(unloadParts is { } unloaded)Diagnostics.UnloadWatch.Unloaded(unloaded,GC.CollectionCount(2));", StringComparison.Ordinal);
            Check(noted > dispose.IndexOf("Diagnostics.UnloadRelease.Sqlite()", StringComparison.Ordinal), "after the release, at the end");
        }),
    ];
}
