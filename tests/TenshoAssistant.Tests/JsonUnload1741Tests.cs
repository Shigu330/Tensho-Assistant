using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Chat;

// X-685 (0.1741): an unloaded copy of Tensho is no longer kept by System.Text.Json (shared by the game process): every options of the plugin
// has a resolver of its own (TenshoJson). A copy of this test assembly is loaded in a context that can unload, as Dalamud loads the plugin.
internal static class JsonUnload1741Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public sealed class Sample { public string Name { get; set; } = ""; public List<int> Values { get; set; } = []; public Dictionary<uint, string> Named { get; set; } = []; }

    /// <summary>Run in the copy, by reflection: Tensho's options (plain, made in place, the chat history's lines), or the default ones.</summary>
    public static void InCopy(string mode)
    {
        var sample = new Sample { Name = "a", Values = [1, 2], Named = { [7] = "seven" } };
        if (mode == "default")
        {
            JsonSerializer.Deserialize<Sample>(JsonSerializer.Serialize(sample));
            return;
        }
        Check(JsonSerializer.Deserialize<Sample>(JsonSerializer.Serialize(sample, TenshoJson.Plain), TenshoJson.Plain) is { Name: "a", Values.Count: 2 }, "round trip in the copy");
        JsonSerializer.Serialize(sample, TenshoJson.Own(new JsonSerializerOptions { WriteIndented = true }));
        var record = new ChatRecord(1, DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000), 10, ChatChannels.From(10), "Ann Doe", [new ChatSegment("hello")]);
        Check(ChatHistory.Decode(ChatHistory.Encode(record), 7) is { } back && back.Body[0].Text == "hello", "the chat history's line in the copy");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Load(string mode)
    {
        var context = new AssemblyLoadContext("TenshoAssistant.Test1741." + mode + "." + Guid.NewGuid().ToString("N"), isCollectible: true);
        using (var stream = File.OpenRead(typeof(JsonUnload1741Tests).Assembly.Location))
            context.LoadFromStream(stream).GetType(nameof(JsonUnload1741Tests), true)!.GetMethod(nameof(InCopy), BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [mode]);
        context.Unload();
        return new WeakReference(context);
    }

    // Collected within the time given; meanwhile others go on using System.Text.Json (as the game's other plugins and the next copy of
    // Tensho do): the accessors the runtime made by reflection for the unloaded copy expire then.
    private static bool Collected(WeakReference context, TimeSpan within)
    {
        var watch = Stopwatch.StartNew();
        for (var round = 0; ; round++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            if (!context.IsAlive) return true;
            if (watch.Elapsed > within) return false;
            JsonSerializer.Serialize(new Sample { Name = "host " + round }, TenshoJson.Own(new JsonSerializerOptions()));
            Thread.Sleep(100);
        }
    }

    // The arguments of a call (at its opening parenthesis), strings and characters skipped.
    private static List<string> Arguments(string text, int open)
    {
        var args = new List<string>();
        var depth = 0; var start = open + 1;
        for (var at = open; at < text.Length; at++)
        {
            var c = text[at];
            if (c == '"')
            {
                var verbatim = at > 0 && (text[at - 1] == '@' || (text[at - 1] == '$' && at > 1 && text[at - 2] == '@'));
                for (at++; at < text.Length; at++)
                {
                    if (!verbatim && text[at] == '\\') { at++; continue; }
                    if (text[at] == '"') { if (verbatim && at + 1 < text.Length && text[at + 1] == '"') { at++; continue; } break; }
                }
                continue;
            }
            if (c == '/' && at + 1 < text.Length && text[at + 1] == '/') { at = text.IndexOf('\n', at); continue; }
            if (c == '\'') { at += text[at + 1] == '\\' ? 3 : 2; while (text[at] != '\'') at++; continue; }
            if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}') { depth--; if (depth == 0) { args.Add(text[start..at].Trim()); return args; } }
            else if (c == ',' && depth == 1) { args.Add(text[start..at].Trim()); start = at + 1; }
        }
        throw new Exception("call not closed");
    }

    private static readonly Regex Call = new(@"(?<![\w.])(?:System\.Text\.Json\.)?JsonSerializer\.(?:Serialize|Deserialize|SerializeToUtf8Bytes|SerializeAsync|DeserializeAsync|SerializeToElement|SerializeToNode|SerializeToDocument)\s*(?:<[^()]*?>)?\s*\(|(?<!JsonSerializer)\.Deserialize\s*<[^()]*?>\s*\(", RegexOptions.CultureInvariant);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Json unload 1741: behaviour: a copy of Tensho that used its own options is collected after its unload; one that used the default options stays (the leak X-685 found)", () =>
        {
            var own = Load("tensho");
            Check(Collected(own, TimeSpan.FromSeconds(10)), "a copy that used Tensho's options is collected after its unload");
            var shared = Load("default");
            Check(!Collected(shared, TimeSpan.FromSeconds(2)), "the default options keep an unloaded copy alive (the control: the test sees the leak)");
        }),
        ("Json unload 1741: behaviour: TenshoJson gives every options a resolver of its own, keeps one given, and reads as the default options do", () =>
        {
            var one = TenshoJson.Own(new JsonSerializerOptions { WriteIndented = true });
            var two = TenshoJson.Own(new JsonSerializerOptions { WriteIndented = true });
            Check(one.TypeInfoResolver is not null && !ReferenceEquals(one.TypeInfoResolver, two.TypeInfoResolver) && TenshoJson.Plain.TypeInfoResolver is not null, "never shared");
            var given = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
            Check(ReferenceEquals(TenshoJson.Own(new JsonSerializerOptions { TypeInfoResolver = given }).TypeInfoResolver, given), "a resolver given is kept");
            var sample = new Sample { Name = "b", Values = [3], Named = { [1] = "one" } };
            Check(JsonSerializer.Serialize(sample, TenshoJson.Plain) == JsonSerializer.Serialize(sample), "the same text as the default options");
        }),
        ("Json unload 1741: cliquet: every System.Text.Json call of the plugin passes options made by TenshoJson; no options made elsewhere", () =>
        {
            var folder = Path.GetFullPath("src/TenshoAssistant");
            var calls = 0; var wrong = new List<string>();
            foreach (var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
                var name = Path.GetRelativePath(folder, file);
                var text = File.ReadAllText(file);
                if (name == "TenshoJson.cs") continue;
                var own = Regex.Matches(text, @"JsonSerializerOptions\s+(\w+)\s*=\s*TenshoJson\.Own\(").Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
                foreach (Match bare in Regex.Matches(text, @"JsonSerializerOptions\s+(\w+)\s*=\s*new\b")) wrong.Add($"{name}: options {bare.Groups[1].Value} made without TenshoJson.Own");
                foreach (Match made in Regex.Matches(text, @"new\s+JsonSerializerOptions\b"))
                    if (!text[Math.Max(0, made.Index - 15)..made.Index].EndsWith("TenshoJson.Own(", StringComparison.Ordinal)) wrong.Add($"{name}: options made in place without TenshoJson.Own");
                foreach (Match call in Call.Matches(text))
                {
                    calls++;
                    var args = Arguments(text, call.Index + call.Length - 1);
                    if (!args.Any(arg => own.Contains(arg) || arg.StartsWith("TenshoJson.", StringComparison.Ordinal) || arg == "options"))
                        wrong.Add($"{name}: line {text[..call.Index].Count(c => c == '\n') + 1}: no options of Tensho");
                }
            }
            Check(calls >= 80, "the calls found: " + calls);
            Check(wrong.Count == 0, string.Join("; ", wrong));
        }),
    ];
}
