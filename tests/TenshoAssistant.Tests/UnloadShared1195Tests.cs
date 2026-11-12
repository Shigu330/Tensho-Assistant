using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1195 (X-01 C1.3, the first reload after 0.1194): the unloaded instance's plugin, window and closures were collected, and the line still
// said « 225 of 1689 parts alive »: fields initialised with « [] » (one empty array per type, shared by every instance) or with a static
// value. Such a value lives as long as the load context and tells nothing about a leak: it is no longer noted, and a note made before
// counts it apart from the parts a root holds.
internal static class UnloadShared1195Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed record Filter(int Level = 0) { public static readonly Filter None = new(); }
    private sealed class Leaf { public int Number = 1; }
    private sealed class Service { public Leaf Leaf = new(); public Leaf[] Rows = []; public Filter Filter = Filter.None; }
    private class HubBase { protected static readonly Dictionary<uint, Leaf> NoNotes = []; }
    private sealed class Hub : HubBase
    {
        private static readonly Leaf Default = new();
        public Leaf[] Empty = [];
        public IReadOnlyList<Leaf> EmptyList = [];
        public Leaf[] Rows = [new()];
        public Filter Filter = Filter.None;
        public Filter Own = new(3);
        public Leaf Fallback = Default;
        public Dictionary<uint, Leaf> Notes = NoNotes;
        public Dictionary<uint, Leaf> OwnNotes = [];
        public Service Service = new();
    }

    private static readonly List<object> OutsideRoot = [];

    private static void Collect(Func<bool> alive)
    {
        for (var round = 0; round < 10 && alive(); round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
    }

    // An instance noted as the plugin notes itself at unload (its own fields); nothing of it is kept unless « keep » says so.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, WeakReference> Noted(bool keep)
    {
        var hub = new Hub();
        if (keep) OutsideRoot.Add(hub.Service);
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        UnloadWatch.Note(parts, "plugin", hub, typeof(UnloadShared1195Tests).Assembly);
        return parts;
    }

    // What an instance unloaded before 0.1195 noted: its shared values too.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, WeakReference> NotedBefore(bool keep)
    {
        var hub = new Hub();
        if (keep) OutsideRoot.Add(hub.Service);
        return new Dictionary<string, WeakReference>(StringComparer.Ordinal)
        {
            ["plugin"] = new(hub), ["plugin.Empty"] = new(hub.Empty), ["plugin.EmptyList"] = new(hub.EmptyList), ["plugin.Filter"] = new(hub.Filter),
            ["plugin.Service"] = new(hub.Service), ["plugin.Service.Rows"] = new(hub.Service.Rows),
        };
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unload shared 1195: an empty array, the value a static field of its own type or of its owner's type holds are shared; a value of the instance's own is not", () =>
        {
            var hub = new Hub();
            Check(UnloadWatch.Shared(hub.Empty) && UnloadWatch.Shared(hub.EmptyList) && UnloadWatch.Shared(new Leaf[hub.Rows.Length - 1]), "an empty array holds nothing");
            Check(!UnloadWatch.Shared(hub.Rows), "an array with a row is the instance's own");
            Check(UnloadWatch.Shared(hub.Filter) && UnloadWatch.Shared(Filter.None), "the static value of its own type");
            // A record equal to the static value is still another object: by reference only.
            Check(!UnloadWatch.Shared(new Filter()) && !UnloadWatch.Shared(hub.Own), "an equal value of the instance's own is not the static one");
            Check(!UnloadWatch.Shared(hub.Notes) && UnloadWatch.Shared(hub.Notes, typeof(Hub)), "a static value of the owner's base type, once the owner is known");
            Check(!UnloadWatch.Shared(hub.Fallback) && UnloadWatch.Shared(hub.Fallback, typeof(Hub)), "a static value of the owner's type");
            Check(!UnloadWatch.Shared(hub.OwnNotes, typeof(Hub)) && !UnloadWatch.Shared(hub.Service, typeof(Hub)), "the instance's own");
            var cache = new Dictionary<Type, HashSet<object>>();
            Check(UnloadWatch.Shared(hub.Fallback, typeof(Hub), cache) && cache.ContainsKey(typeof(Hub)) && UnloadWatch.Shared(hub.Notes, typeof(Hub), cache), "the static values of a type are read once per pass");
        }),
        ("Unload shared 1195: the parts noted at unload leave out the shared values", () =>
        {
            OutsideRoot.Clear();
            try
            {
                var parts = Noted(true);
                var names = string.Join(" | ", parts.Keys.Order(StringComparer.Ordinal));
                Check(names == "plugin | plugin.Own | plugin.OwnNotes | plugin.Rows | plugin.Service", names);
            }
            finally { OutsideRoot.Clear(); }
        }),
        ("Unload shared 1195: behaviour: an instance nothing holds is collected whole: no part alive, where its shared values used to read as alive", () =>
        {
            OutsideRoot.Clear();
            try
            {
                var parts = Noted(false);
                Collect(() => parts.Values.Any(part => part.IsAlive));
                Check(UnloadWatch.Describe("i", parts) == "i: context unknown, 0 of 5 parts alive", UnloadWatch.Describe("i", parts));
                // The same instance as noted before 0.1195: collected as well, and its shared values are counted apart, not as held.
                var before = NotedBefore(false);
                Collect(() => before["plugin"].IsAlive || before["plugin.Service"].IsAlive);
                Check(UnloadWatch.Describe("o", before) == "o: context unknown, 4 of 6 parts alive, 4 shared (empty arrays: 3, static values: 1)", UnloadWatch.Describe("o", before));
            }
            finally { OutsideRoot.Clear(); }
        }),
        ("Unload shared 1195: behaviour: a root that holds one service: the line names it as held, apart from the shared values", () =>
        {
            OutsideRoot.Clear();
            try
            {
                var parts = Noted(true);
                Collect(() => parts["plugin"].IsAlive);
                Check(UnloadWatch.Describe("i", parts) == "i: context unknown, 1 of 5 parts alive: plugin.Service", UnloadWatch.Describe("i", parts));
                var before = NotedBefore(true);
                Collect(() => before["plugin"].IsAlive);
                Check(UnloadWatch.Describe("o", before) == "o: context unknown, 5 of 6 parts alive, 4 shared (empty arrays: 3, static values: 1), 1 held: plugin.Service", UnloadWatch.Describe("o", before));
            }
            finally { OutsideRoot.Clear(); }
        }),
    ];
}
