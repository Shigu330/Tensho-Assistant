using TenshoAssistant.Market;

// 0.1581 (X-639, finding of the reviewer bench X-631 on the review of 0.1560, M3): at start, a damaged prices-seen file that could not be set
// aside is told apart from a file held by another program: only a held file is read again later (the « held » path of LoadDiscoveries).
internal static class SeenStuck1581Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Seen stuck 1581: a damaged file that cannot be moved is « stuck », never « left » (held)", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "tensho-stuck-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var path = Path.Combine(root, "MarketSeen.json");
                File.WriteAllText(path, "{ damaged");
                bool left = false, stuck = false, aside = false;
                // Read allowed, delete and move refused (Windows): the read fails as damaged, the move fails. Elsewhere the move may succeed.
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Check(SeenPriceStore.ReadAtStart(path, DateTimeOffset.UtcNow, (_, _) => aside = true, _ => left = true, tries: 1, waitMs: 0, stuck: _ => stuck = true) is null, "nothing read");
                Check(!left, "a damaged file is never taken for a held one");
                Check(stuck ^ aside, $"either set aside or said stuck: stuck {stuck}, aside {aside}");
                var oldWay = false;
                File.WriteAllText(path, "{ damaged");
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    SeenPriceStore.ReadAtStart(path, DateTimeOffset.UtcNow, (_, _) => { }, _ => oldWay = true, tries: 1, waitMs: 0);
                Check(oldWay || !File.Exists(path), "without « stuck »: « left », as before");
            }
            finally { try { Directory.Delete(root, true); } catch (IOException) { } }
        }),
        ("Seen stuck 1581: the start reads a held file again later, not a stuck one (wiring)", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs"));
            var start = plugin.IndexOf("Market.SeenPriceStore.ReadAtStart(", StringComparison.Ordinal);
            var call = plugin[start..plugin.IndexOf("if(held)_=ReadSeenPricesLater(", start, StringComparison.Ordinal)];
            Check(call.Contains("stuck:error=>log.Warning(") && !call[call.IndexOf("stuck:", StringComparison.Ordinal)..].Contains("held=true"), "stuck does not set held");
        }),
    ];
}
