using System.Text.RegularExpressions;
using TenshoAssistant.Discoveries;

// X-406 (0.1383, relay 73: load test at a million facts): the merge tool bounds each kind of the common set to the plugin's limit before
// writing it, so that the index is never refused whole by ExchangeV3.ParseIndex (Count > DiscoveryStore.MaxFacts). The behaviour (the most
// seen kept, then the most recent; a kind over the bound refused by v3_build; the bounded set read back whole) is checked by the tool's
// selftest, run by check.sh; here: the same bound on both sides and the wiring of rebuild.
internal static class DiscoveryBound1383Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Discovery bound 1383: the merge tool's MAX_FACTS is the plugin's DiscoveryStore.MaxFacts", () =>
        {
            var tool = Read("tools/discoveries-merge.py");
            var match = Regex.Match(tool, @"MAX_FACTS=([0-9_]+);");
            Check(match.Success && int.Parse(match.Groups[1].Value.Replace("_", "")) == DiscoveryStore.MaxFacts, "MAX_FACTS differs from DiscoveryStore.MaxFacts");
        }),
        ("Discovery bound 1383: rebuild writes the bounded set, v3_build refuses a kind over the bound, the selftest holds the cases", () =>
        {
            var tool = Read("tools/discoveries-merge.py");
            Check(tool.Contains("return write_v3(bounded(common[\"Facts\"],say=say),V3_DIR,say)"), "rebuild writes bounded(...)");
            Check(tool.Contains("if over:raise ValueError(f\"kinds over the bound of {MAX_FACTS} facts"), "v3_build refuses a kind over the bound");
            Check(tool.Contains("key=lambda f:(-f[\"Imported\"],-when(f[\"Last\"]).timestamp(),sort_key(f))"), "the most seen, then the most recent");
            foreach (var name in new[] { "X-406: bounded keeps", "X-406: the bounded set's index reads whole", "X-406: a kind over the bound: v3_build refuses to write" })
                Check(tool.Contains(name), "selftest case missing: " + name);
        }),
    ];
}
