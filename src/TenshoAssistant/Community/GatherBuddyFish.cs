using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Community;

/// <summary>
/// How to catch a fish according to GatherBuddy (Ottermandias/GatherBuddy, Apache-2.0, GatherBuddy.GameData/Data/Fish/Data*.cs), read as
/// data (0.227): bait items, the fish to mooch from, hookset and bite (« ! » weak, « !! » strong, « !!! » legendary), ET window in minutes,
/// weathers and previous weathers (Weather sheet ids), lure, predators to catch first (fish, count) and snagging.
/// R-15 (0.417): spearfishing size and speed (« .Spear(data, SpearfishSize.X, SpearfishSpeed.Y) »), as the game's values
/// (Gathering/Spearfishing.cs); null when not given or « Unknown ».
/// </summary>
public sealed record GatherBuddyFish(uint FishId, IReadOnlyList<uint> Baits, IReadOnlyList<uint> Mooches, string? HookSet, string? Bite,
    int? StartMinute, int? EndMinute, IReadOnlyList<uint> Weathers, IReadOnlyList<uint> Transitions, string? Lure,
    IReadOnlyList<(uint Fish, int Count)> Predators, bool Snag, int? SpearSize = null, int? SpearSpeed = null);

public sealed class GatherBuddyFishData
{
    private readonly Dictionary<uint, GatherBuddyFish> fish;
    public int Count => fish.Count;
    public GatherBuddyFishData(Dictionary<uint, GatherBuddyFish> fish) { this.fish = fish; }
    public GatherBuddyFish? Find(uint fishId) => fish.GetValueOrDefault(fishId);
    /// <summary>R-15 (0.417): every fish with a known spearfishing size and speed.</summary>
    public IEnumerable<GatherBuddyFish> Spearfish => fish.Values.Where(entry => entry.SpearSize is not null && entry.SpearSpeed is not null);

    /// <summary>
    /// Bait route before the fish: its bait, or its mooch chain. GatherBuddy writes a mooch either as the whole chain (« Mooch(data, bait,
    /// fish) », older files) or as the last fish only (« Mooch(data, fish) », newer files): the chain is completed from the first known fish
    /// back to a bait item (bounded, no loop).
    /// </summary>
    public IReadOnlyList<uint> Route(uint fishId)
    {
        if (Find(fishId) is not { } target) return [];
        if (target.Mooches.Count == 0) return target.Baits.Count > 0 ? [target.Baits[0]] : [];
        var route = new List<uint>(target.Mooches);
        for (var guard = 0; guard < 8 && Find(route[0]) is { } first; guard++)
        {
            var before = first.Mooches.Count > 0 ? first.Mooches : first.Baits.Count > 0 ? [first.Baits[0]] : [];
            if (before.Count == 0 || before.Any(route.Contains)) break;
            route.InsertRange(0, before);
        }
        return route;
    }

    /// <summary>Reads every « data.Apply(fish, …) … ; » statement of the given source files; unknown calls are ignored.</summary>
    public static GatherBuddyFishData Parse(IEnumerable<string> sources)
    {
        var result = new Dictionary<uint, GatherBuddyFish>();
        foreach (var source in sources)
        {
            var index = 0;
            // « data.Apply(fish, …) », with or without spaces before the parenthesis (older files align their calls).
            while ((index = source.IndexOf("data.Apply", index, StringComparison.Ordinal)) >= 0)
            {
                var end = source.IndexOf(';', index);
                if (end < 0) break;
                var statement = source[index..end];
                index = end + 1;
                var calls = Calls(statement);
                if (calls.Count == 0 || calls[0].Name != "Apply" || Numbers(calls[0].Arguments) is not [var id, ..] || id == 0) continue;
                var baits = new List<uint>(); var mooches = new List<uint>(); var weathers = new List<uint>(); var transitions = new List<uint>();
                var predators = new List<(uint, int)>();
                string? hook = null, bite = null, lure = null; int? start = null, stop = null, spearSize = null, spearSpeed = null; var snag = false;
                foreach (var (name, arguments) in calls.Skip(1))
                    switch (name)
                    {
                        case "Bait": baits.AddRange(Numbers(arguments)); break;
                        case "Mooch": mooches.AddRange(Numbers(arguments)); break;
                        case "Weather": weathers.AddRange(Numbers(arguments)); break;
                        case "Transition": transitions.AddRange(Numbers(arguments)); break;
                        case "Bite":
                            // 0.330: only the known values are kept; GatherBuddy writes « Unknown » for fish not yet documented (unknown = null).
                            hook = (Word(arguments, "HookSet.") is ("Precise" or "Powerful") and var hookSet) ? hookSet : null;
                            bite = (Word(arguments, "BiteType.") is ("Weak" or "Strong" or "Legendary") and var biteType) ? biteType : null; break;
                        case "Time":
                            if (Numbers(arguments) is [var from, var to, ..]) { start = (int)from; stop = (int)to; }
                            break;
                        case "Lure": lure = Word(arguments, "Lure."); break;
                        case "Spear":
                            spearSize = Gathering.Spearfishing.Size(Word(arguments, "SpearfishSize."));
                            spearSpeed = Gathering.Spearfishing.Speed(Word(arguments, "SpearfishSpeed.")); break;
                        case "Snag": snag = arguments.Contains("Required", StringComparison.Ordinal); break;
                        case "Predators":
                            foreach (var pair in Tuples(arguments)) predators.Add(pair);
                            break;
                    }
                result[id] = new(id, baits, mooches, hook, bite, start, stop, weathers, transitions, lure, predators, snag, spearSize, spearSpeed);
            }
        }
        return new(result);
    }

    // « .Name(arguments) » calls of a statement, with balanced parentheses.
    private static List<(string Name, string Arguments)> Calls(string statement)
    {
        var calls = new List<(string, string)>();
        for (var position = 0; position < statement.Length; position++)
        {
            if (statement[position] != '.' && !(position == 4 && statement.StartsWith("data.", StringComparison.Ordinal))) continue;
            var nameStart = position + 1; var nameEnd = nameStart;
            while (nameEnd < statement.Length && char.IsLetter(statement[nameEnd])) nameEnd++;
            var open = nameEnd;
            while (open < statement.Length && statement[open] is ' ' or '	') open++;
            if (nameEnd == nameStart || open >= statement.Length || statement[open] != '(') continue;
            var depth = 0; var close = open;
            for (; close < statement.Length; close++)
            {
                if (statement[close] == '(') depth++;
                else if (statement[close] == ')' && --depth == 0) break;
            }
            if (close >= statement.Length) break;
            calls.Add((statement[nameStart..nameEnd], statement[(open + 1)..close]));
            position = close;
        }
        return calls;
    }
    // Plain numbers of an argument list (the leading « data » and enum words are skipped).
    private static List<uint> Numbers(string arguments) =>
        arguments.Split([',', '(', ')', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => uint.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : (uint?)null)
            .OfType<uint>().ToList();
    private static string? Word(string arguments, string prefix)
    {
        var at = arguments.IndexOf(prefix, StringComparison.Ordinal);
        if (at < 0) return null;
        var start = at + prefix.Length; var end = start;
        while (end < arguments.Length && char.IsLetter(arguments[end])) end++;
        return end > start ? arguments[start..end] : null;
    }
    private static IEnumerable<(uint, int)> Tuples(string arguments)
    {
        var at = 0;
        while ((at = arguments.IndexOf('(', at)) >= 0)
        {
            var close = arguments.IndexOf(')', at);
            if (close < 0) yield break;
            if (Numbers(arguments[at..close]) is [var fish, var count]) yield return (fish, (int)count);
            at = close + 1;
        }
    }
}

public sealed record GatherBuddyFishState(GatherBuddyFishData? Data, bool Loading, string? Error);
public sealed record GatherBuddyFishCache(string Text);

/// <summary>
/// Downloads GatherBuddy's fish data files once on first use (file list from the GitHub contents API, each file bounded to 512 KB), kept in
/// the DataCache « gatherbuddy-fish » for the sources lifetime. Settings › Advanced can turn the source off.
/// </summary>
public sealed class GatherBuddyFishService : IDisposable
{
    private const string Family = "gatherbuddy-fish", Version = "main-v1";
    private const string Listing = "https://api.github.com/repos/Ottermandias/GatherBuddy/contents/GatherBuddy.GameData/Data/Fish";
    private const string Raw = "https://raw.githubusercontent.com/Ottermandias/GatherBuddy/main/GatherBuddy.GameData/Data/Fish/";
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Func<TimeSpan> lifetime;
    private readonly DataCache? cache;
    private GatherBuddyFishState state = new(null, false, null);
    private int started;
    private long retryAfter;
    public Func<bool>? Enabled { get; set; }
    public GatherBuddyFishState State => Enabled?.Invoke() == false ? new(null, false, null) : Volatile.Read(ref state);

    // 0.330: a client can be given (managed tests, as FishBiteTimeService); the service's own keeps its 30 s timeout and User-Agent.
    public GatherBuddyFishService(Func<TimeSpan> lifetime, DataCache? cache, HttpClient? client = null)
    {
        this.lifetime = lifetime; this.cache = cache;
        ownsClient = client is null; this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        if (ownsClient) this.client.DefaultRequestHeaders.UserAgent.ParseAdd("TenshoAssistant");
    }

    public void Request()
    {
        // R-10 (0.246): called from Draw; after a failure, no new attempt for 5 minutes (it used to retry every frame).
        if (Enabled?.Invoke() == false || Environment.TickCount64 < Volatile.Read(ref retryAfter) || Interlocked.Exchange(ref started, 1) == 1) return;
        Volatile.Write(ref state, new(null, true, null));
        var token = cancellation.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                var cached = cache is null ? null : await cache.ReadAsync<GatherBuddyFishCache>(Family, "all", Version, token).ConfigureAwait(false);
                string text;
                if (cached is not null && DateTimeOffset.UtcNow - cached.RecordedAt <= lifetime()) text = cached.Value.Text;
                else try
                {
                    if (Abandoned()) return;
                    using var document = JsonDocument.Parse(await BoundedHttp.ReadAsync(client, new Uri(Listing), 256 * 1024, token).ConfigureAwait(false));
                    var names = document.RootElement.EnumerateArray().Select(entry => entry.GetProperty("name").GetString() ?? "")
                        .Where(name => name.StartsWith("Data", StringComparison.Ordinal) && name.EndsWith(".cs", StringComparison.Ordinal)).Take(80).ToArray();
                    var parts = new List<string>();
                    foreach (var name in names)
                    {
                        if (Abandoned()) return;
                        parts.Add(await BoundedHttp.ReadAsync(client, new Uri(Raw + Uri.EscapeDataString(name)), 512 * 1024, token).ConfigureAwait(false));
                    }
                    text = string.Join("\n", parts);
                    if (cache is not null) await cache.WriteAsync(Family, "all", Version, new GatherBuddyFishCache(text), DateTimeOffset.UtcNow).ConfigureAwait(false);
                }
                // Network down or GitHub refusing: the expired copy is better than nothing (kept until a download succeeds).
                catch (Exception) when (cached?.Value.Text is { Length: > 0 } && !token.IsCancellationRequested) { text = cached.Value.Text; }
                Volatile.Write(ref state, new(GatherBuddyFishData.Parse([text]), false, null));
            }
            // 0.330: only the plugin's own cancellation is silent; an HttpClient timeout (also an OperationCanceledException) is a failure: it
            // ends the loading state and is retried after the pause, instead of leaving « downloading… » until the plugin reloads.
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error)
            {
                Volatile.Write(ref state, new(null, false, error.Message));
                Volatile.Write(ref retryAfter, Environment.TickCount64 + 300_000); Interlocked.Exchange(ref started, 0);
            }
        }, token);
    }

    // 0.330: GatherBuddy turned off during the download: the next requests are not sent and nothing is written as an error, so the download
    // is asked again once the switch is back on (as the Garland, wiki and Teamcraft queues since 0.326).
    private bool Abandoned()
    {
        if (Enabled?.Invoke() != false) return false;
        Volatile.Write(ref state, new(null, false, null)); Interlocked.Exchange(ref started, 0);
        return true;
    }

    public void Dispose() { cancellation.Cancel(); cancellation.Dispose(); if (ownsClient) client.Dispose(); }
}
