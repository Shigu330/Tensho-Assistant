using System.Reflection;
using System.Text.RegularExpressions;

// 0.1298 (X-281, review of the 0.1278 block, I1, R36.2): ratchet. The test classes run in parallel (TestRunner.cs); a class whose code
// writes state shared by the whole process must carry [SerialTests("reason")] and run alone after the others. Written here: the culture
// of Tensho (LocalizationManager.SetLanguage, SelectInitialLanguage, TenshoCulture.Use), the static hooks and measures of
// CompetitiveGate, the culture of every new thread, WeatherCatalog.StoryWeatherConfirmed. Program.cs writes none of them: its group
// cannot be serial. A mention inside a string (a test reading the source) or after « // » does not count.
internal static class SharedState1298Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    internal static readonly (string What, Regex Pattern)[] Writers =
    [
        ("TenshoCulture", new(@"\.SetLanguage\(|\.SelectInitialLanguage\(|\bTenshoCulture\.Use\(")),
        ("CompetitiveGate's static state", new(@"\bCompetitiveGate\.(?:ForgetInstance|Compiler\b|CompilerState|CompilerBusy|CompilerCallees|HoldsInstance|Developer\s*=|SlowSubscription\s*=|Changed\s*[+-]=|ClearSubscribers|MeasureStart|MeasureOwn|GameCallEnded)")),
        ("the culture of every new thread", new(@"\bDefaultThreadCurrent(?:UI)?Culture\s*=")),
        ("WeatherCatalog.StoryWeatherConfirmed", new(@"\bStoryWeatherConfirmed\s*=")),
    ];

    /// <summary>Whether position <paramref name="at"/> of <paramref name="line"/> is code: not inside a string, not after « // ».</summary>
    internal static bool IsCode(string line, int at)
    {
        var before = Regex.Replace(line[..at], @"\\.", "");
        if (before.Count(c => c == '"') % 2 == 1) return false;
        return !Regex.Replace(before, "\"[^\"]*\"", "").Contains("//", StringComparison.Ordinal);
    }

    /// <summary>What the code of <paramref name="text"/> writes of the shared state, by name.</summary>
    internal static SortedSet<string> Written(string text)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
            foreach (var (what, pattern) in Writers)
                if (pattern.Matches(line).Any(match => IsCode(line, match.Index))) found.Add(what);
        return found;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Shared state 1298: cliquet: a test class that writes state shared by the whole process runs alone ([SerialTests]); Program.cs writes none", () =>
        {
            var folder = Path.GetFullPath("tests/TenshoAssistant.Tests");
            var classes = TestRegistry.Classes(typeof(SharedState1298Tests).Assembly).ToDictionary(type => type.Name, StringComparer.Ordinal);
            var missing = new List<string>();
            var seen = 0;
            foreach (var file in Directory.GetFiles(folder, "*.cs").OrderBy(path => path, StringComparer.Ordinal))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name is "SharedState1298Tests" or "TestRunner") continue;
                var text = File.ReadAllText(file);
                var written = Written(text);
                if (written.Count == 0) continue;
                seen++;
                if (name == "Program") { missing.Add($"Program.cs writes {string.Join(", ", written)}: move the test into a class marked [SerialTests]"); continue; }
                var declared = Regex.Matches(text, @"\bstatic class (\w+)").Select(match => match.Groups[1].Value).Where(classes.ContainsKey).ToArray();
                if (declared.Length == 0) missing.Add($"{name}.cs writes {string.Join(", ", written)} outside a test class");
                foreach (var type in declared.Select(cls => classes[cls]))
                    if (type.GetCustomAttribute<SerialTestsAttribute>() is null) missing.Add($"{type.Name} writes {string.Join(", ", written)} without [SerialTests(\"reason\")]");
            }
            Check(missing.Count == 0, "shared state written by a class that runs in parallel: " + string.Join("; ", missing));
            Check(seen >= 7, $"the scan found the known writers ({seen} files)");
        }),
        ("Shared state 1298: the scan tells code from strings and comments", () =>
        {
            Check(Written("        loc.SetLanguage(\"fr\");").SetEquals(["TenshoCulture"]), "a call");
            Check(Written("        Check(text.Contains(\"loc.SetLanguage(fr)\"), \"x\");").Count == 0, "inside a string");
            Check(Written("        // CompetitiveGate.ForgetInstance(); at unload").Count == 0, "after //");
            Check(Written("        var a = \"//\"; CompetitiveGate.ForgetInstance();").SetEquals(["CompetitiveGate's static state"]), "// inside a string, then code");
            Check(Written("        var s = \"a\\\"b\"; TenshoCulture.Use(\"en\");").SetEquals(["TenshoCulture"]), "an escaped quote");
            Check(Written("        CompetitiveGate.Add(framework,OnUpdate); CompetitiveGate.Cut").Count == 0, "the gate's stateless members");
            Check(Written("        WeatherCatalog.StoryWeatherConfirmed = null;").SetEquals(["WeatherCatalog.StoryWeatherConfirmed"]), "a static hook set");
        }),
        ("Shared state 1298: behaviour: the serial classes run alone, after every parallel class", () =>
        {
            var order = new System.Collections.Concurrent.ConcurrentQueue<string>();
            var running = 0;
            var overlap = false;
            Action Step(string name) => () =>
            {
                if (Interlocked.Increment(ref running) > 1 && name.StartsWith("serial", StringComparison.Ordinal)) overlap = true;
                Thread.Sleep(20);
                order.Enqueue(name);
                Interlocked.Decrement(ref running);
            };
            TestGroup Group(string name, bool serial) => new(name, [(name + " 1", Step(name)), (name + " 2", Step(name))], serial);
            var run = ParallelTests.Run([Group("p1", false), Group("serialA", true), Group("p2", false), Group("serialB", true), Group("p3", false)], 4, [], DateOnly.FromDateTime(DateTime.Today));
            var seenOrder = order.ToArray();
            var firstSerial = Array.FindIndex(seenOrder, name => name.StartsWith("serial", StringComparison.Ordinal));
            Check(run.Failed == 0 && run.Total == 10 && !overlap, "every test passed, no serial test ran beside another");
            Check(firstSerial == 6 && seenOrder[6..].SequenceEqual(new[] { "serialA", "serialA", "serialB", "serialB" }), "the serial classes after the parallel ones, one after the other");
        }),
    ];
}
