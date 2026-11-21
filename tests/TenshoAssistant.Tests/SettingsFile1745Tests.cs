using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using TenshoAssistant;

// X-685 part 2 (0.1745, D-206 a, R28.14): Tensho saves its settings itself (System.Text.Json, pluginConfigs\TenshoAssistant\Settings.json),
// reads Dalamud's file (Newtonsoft with type names) once, and loses nothing: every member of the settings at any depth read back as
// Newtonsoft read it, two game clients saving their own changes, a write that fails, a reload, names it does not know, a file it cannot read.
internal static class SettingsFile1745Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // Dalamud's way (PluginConfigurations: Newtonsoft, type names on objects), and a plain Newtonsoft text to compare two settings whole.
    private static readonly JsonSerializerSettings Dalamud = new() { TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple, TypeNameHandling = TypeNameHandling.Objects, Formatting = Formatting.Indented };
    private static string Legacy(Configuration value) => JsonConvert.SerializeObject(value, Dalamud);
    private static Configuration DalamudRead(string text) => JsonConvert.DeserializeObject<Configuration>(text, Dalamud) ?? throw new Exception("Dalamud's reading gave nothing");
    private static string Canon(object value) => JsonConvert.SerializeObject(value, Formatting.None);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-settings-1745-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SettingsFile<Configuration> Settings(string root, List<string>? said = null) =>
        new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), Path.Combine(root, "TenshoAssistant.json"), (error, message) => said?.Add(message + (error is null ? "" : " (" + error.GetType().Name + ")")));

    // ---- Every member given a value that is not its default, at any depth (one element added to each list, one entry to each table). ----
    private static int filled;
    private static readonly DateTimeOffset When = new(2026, 11, 21, 19, 30, 15, 123, TimeSpan.FromHours(1));

    private static Configuration Filled()
    {
        var value = new Configuration();
        filled = 0;
        Fill(value, 0);
        return value;
    }

    private static void Fill(object target, int depth)
    {
        if (depth > 6) return;
        var type = target.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true) continue;
            try { property.SetValue(target, Other(property.PropertyType, property.GetValue(target), depth)); filled++; }
            catch (TargetInvocationException) { }
            catch (ArgumentException) { }
        }
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.IsInitOnly) continue;
            field.SetValue(target, Other(field.FieldType, field.GetValue(target), depth)); filled++;
        }
    }

    private static object? Other(Type type, object? now, int depth)
    {
        if (Nullable.GetUnderlyingType(type) is { } under) return Other(under, now, depth);
        if (type == typeof(bool)) return now is not true;
        if (type == typeof(int)) return (now is int i ? i : 0) + 3;
        if (type == typeof(uint)) return (now is uint u ? u : 0) + 3;
        if (type == typeof(long)) return (now is long l ? l : 0) + 3;
        if (type == typeof(ulong)) return (now is ulong ul ? ul : 0) + 3;
        if (type == typeof(short)) return (short)((now is short s ? s : 0) + 3);
        if (type == typeof(ushort)) return (ushort)((now is ushort us ? us : 0) + 3);
        if (type == typeof(byte)) return (byte)((now is byte b ? b : 0) + 3);
        if (type == typeof(sbyte)) return (sbyte)((now is sbyte sb ? sb : 0) + 3);
        if (type == typeof(float)) return (now is float f ? f : 0f) + 0.375f;
        if (type == typeof(double)) return (now is double d ? d : 0d) + 0.375d;
        if (type == typeof(decimal)) return (now is decimal m ? m : 0m) + 0.375m;
        if (type == typeof(string)) return (now as string ?? "") + "é·x";
        if (type == typeof(Guid)) return new Guid("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");
        if (type == typeof(DateTimeOffset)) return When;
        if (type == typeof(DateTime)) return new DateTime(2026, 11, 21, 18, 30, 15, DateTimeKind.Utc);
        if (type == typeof(TimeSpan)) return TimeSpan.FromMinutes(97);
        if (type == typeof(Vector2)) return new Vector2(0.125f, 0.75f);
        if (type == typeof(Vector3)) return new Vector3(0.125f, 0.75f, 0.5f);
        if (type == typeof(Vector4)) return new Vector4(0.125f, 0.75f, 0.5f, 0.875f);
        if (type.IsEnum) return OtherEnum(type, now);
        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            var old = now as Array;
            var array = Array.CreateInstance(element, (old?.Length ?? 0) + 1);
            old?.CopyTo(array, 0);
            array.SetValue(Make(element, depth + 1), array.Length - 1);
            return array;
        }
        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
        {
            var definition = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();
            if (args.Length == 2 && (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>)))
            {
                var table = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args))!;
                if (now is IDictionary old) foreach (DictionaryEntry entry in old) table[entry.Key] = entry.Value;
                var key = NewKey(args[0], table);
                if (key is not null) table[key] = Make(args[1], depth + 1);
                return table;
            }
            if (args.Length == 1)
            {
                var concrete = definition == typeof(HashSet<>) || definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>) ? typeof(HashSet<>).MakeGenericType(args) : typeof(List<>).MakeGenericType(args);
                var list = Activator.CreateInstance(concrete)!;
                var add = concrete.GetMethod("Add")!;
                if (now is IEnumerable old) foreach (var item in old) add.Invoke(list, [item]);
                add.Invoke(list, [Make(args[0], depth + 1)]);
                return list;
            }
        }
        if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple", StringComparison.Ordinal))
            return Activator.CreateInstance(type, type.GetGenericArguments().Select(arg => Make(arg, depth + 1)).ToArray());
        if (type.IsClass && type.Assembly == typeof(Configuration).Assembly)
        {
            var value = now ?? Make(type, depth + 1);
            if (value is not null && depth < 6) Fill(value, depth + 1);
            return value;
        }
        throw new Exception("a member's type the test cannot fill: " + type.FullName + " (add it to the test, and check that both serializers write it)");
    }

    private static object? Make(Type type, int depth)
    {
        if (Nullable.GetUnderlyingType(type) is { } under) return Make(under, depth);
        if (type.IsClass && type != typeof(string) && !type.IsArray && type.Assembly == typeof(Configuration).Assembly)
        {
            if (type.GetConstructor(Type.EmptyTypes) is { } empty) { var made = empty.Invoke([]); if (depth < 6) Fill(made, depth); return made; }
            var constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            return constructor.Invoke(constructor.GetParameters().Select(p => Make(p.ParameterType, depth + 1)).ToArray());
        }
        if (type.IsInterface && type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            return Other(args.Length == 2 ? typeof(Dictionary<,>).MakeGenericType(args) : typeof(List<>).MakeGenericType(args), null, depth);
        }
        return Other(type, type.IsValueType ? Activator.CreateInstance(type) : null, depth);
    }

    private static object OtherEnum(Type type, object? now)
    {
        var values = Enum.GetValues(type).Cast<object>().Where(v => !Equals(v, now)).ToList();
        if (type.IsDefined(typeof(FlagsAttribute)))
        {
            var flags = values.Select(v => Convert.ToUInt64(v)).Where(v => v != 0 && (v & (v - 1)) == 0).Take(2).ToList();
            if (flags.Count == 2) return Enum.ToObject(type, flags[0] | flags[1]);
        }
        return values.FirstOrDefault(v => Convert.ToInt64(v) != 0) ?? values.First();
    }

    private static object? NewKey(Type type, IDictionary table)
    {
        if (type == typeof(string)) return "clé·" + table.Count;
        if (type.IsEnum) return Enum.GetValues(type).Cast<object>().FirstOrDefault(v => !table.Contains(v) && Convert.ToInt64(v) != 0);
        if (type == typeof(uint) || type == typeof(int) || type == typeof(ulong) || type == typeof(long) || type == typeof(ushort))
        {
            var next = table.Keys.Cast<object>().Select(Convert.ToInt64).DefaultIfEmpty(0).Max() + 11;
            return Convert.ChangeType(next, type, System.Globalization.CultureInfo.InvariantCulture);
        }
        throw new Exception("a table key the test cannot make: " + type.FullName);
    }

    // The top-level names the filling left at their default (computed members: never read back by either serializer).
    private static List<string> Unchanged(Configuration value)
    {
        var mine = JsonNode.Parse(Canon(value))!.AsObject();
        var plain = JsonNode.Parse(Canon(new Configuration()))!.AsObject();
        return mine.Where(pair => JsonNode.DeepEquals(pair.Value, plain[pair.Key])).Select(pair => pair.Key).ToList();
    }

    // ---- Unload (as JsonUnload1741Tests): a copy of the tests that read and wrote the settings is collected after its unload. ----
    public static void InCopy(string folder, string mode)
    {
        var value = Filled();
        if (mode == "newtonsoft")
        {
            Check(Canon(JsonConvert.DeserializeObject<Configuration>(Canon(value))!) == Canon(value), "Newtonsoft in the copy");
            return;
        }
        var file = Settings(folder);
        Check(file.Save(value), "saved in the copy");
        // Compared with System.Text.Json only: Newtonsoft in the copy would keep it alive (the leak itself).
        Check(SettingsJson.Write(Settings(folder).Load(out _)) == SettingsJson.Write(value), "read back in the copy");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadCopy(string folder, string mode)
    {
        var context = new AssemblyLoadContext("TenshoAssistant.Test1745." + Guid.NewGuid().ToString("N"), isCollectible: true);
        using (var stream = System.IO.File.OpenRead(typeof(SettingsFile1745Tests).Assembly.Location))
            context.LoadFromStream(stream).GetType(nameof(SettingsFile1745Tests), true)!.GetMethod(nameof(InCopy), BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [folder, mode]);
        context.Unload();
        return new WeakReference(context);
    }

    private static bool Collected(WeakReference context, TimeSpan within)
    {
        var watch = Stopwatch.StartNew();
        for (var round = 0; ; round++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            if (!context.IsAlive) return true;
            if (watch.Elapsed > within) return false;
            System.Text.Json.JsonSerializer.Serialize(new { Round = round }, TenshoJson.Own(new System.Text.Json.JsonSerializerOptions()));
            Thread.Sleep(100);
        }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings file 1745: behaviour: every member at any depth, written by Dalamud (Newtonsoft, type names), is read back as Newtonsoft read it", () =>
        {
            var value = Filled();
            Check(filled >= 300, "members filled: " + filled);
            var unchanged = Unchanged(value);
            Check(unchanged.Count == 0, "top-level settings the filling left at their default: " + string.Join(", ", unchanged));
            var text = Legacy(value);
            Check(text.Contains("\"$type\"", StringComparison.Ordinal), "Dalamud's text has type names");
            var reference = Canon(DalamudRead(text));
            Check(reference == Canon(value), "Newtonsoft reads its own text back (the reference)");
            var read = SettingsJson.ReadLegacy<Configuration>(text) ?? throw new Exception("not read");
            Check(Canon(read) == reference, "read as Newtonsoft read it: " + FirstDifference(Canon(read), reference));
        }),
        ("Settings file 1745: behaviour: Tensho's own text reads back to the same settings and the same text (the saver's check)", () =>
        {
            var value = Filled();
            var text = SettingsJson.Write(value);
            Check(!text.Contains("$type", StringComparison.Ordinal) && text.Contains("é·x", StringComparison.Ordinal), "no type names; accents written as they are");
            var read = SettingsJson.Read<Configuration>(text) ?? throw new Exception("not read");
            Check(Canon(read) == Canon(value), "the same settings: " + FirstDifference(Canon(read), Canon(value)));
            Check(SettingsJson.Write(read) == text, "the same text");
            var plain = new Configuration();
            Check(SettingsJson.Write(SettingsJson.Read<Configuration>(SettingsJson.Write(plain))!) == SettingsJson.Write(plain), "new settings too");
        }),
        ("Settings file 1745: behaviour: Dalamud's file read once: written to Settings.json, left as it was, never read again", () => InTemp(root =>
        {
            var value = Filled();
            var legacy = Path.Combine(root, "TenshoAssistant.json");
            System.IO.File.WriteAllText(legacy, Legacy(value));
            var before = System.IO.File.ReadAllBytes(legacy);
            var first = Settings(root).Load(out var origin);
            Check(origin == SettingsOrigin.Legacy && Canon(first) == Canon(DalamudRead(Legacy(value))), "moved: " + origin);
            Check(System.IO.File.Exists(Path.Combine(root, "TenshoAssistant", "Settings.json")), "Settings.json written at once");
            Check(System.IO.File.ReadAllBytes(legacy).SequenceEqual(before), "Dalamud's file left as it was");
            System.IO.File.WriteAllText(legacy, Legacy(new Configuration()));
            var again = Settings(root).Load(out var next);
            Check(next == SettingsOrigin.Own && Canon(again) == Canon(first), "read from Settings.json afterwards, never from Dalamud's file: " + next);
            Check(Settings(Path.Combine(root, "empty")).Load(out var none) is not null && none == SettingsOrigin.New, "no file at all: new settings");
        })),
        ("Settings file 1745: behaviour: two game clients, each saving its own changes, keep each other's; at once too, the file always whole", () => InTemp(root =>
        {
            Settings(root).Save(new Configuration());
            var oneFile = Settings(root); var one = oneFile.Load(out _);
            var twoFile = Settings(root); var two = twoFile.Load(out _);
            one.DeveloperMode = !one.DeveloperMode; one.MapView.IconSizes[777] = 0.25f; one.Chat.ShowSeconds = !one.Chat.ShowSeconds;
            Check(oneFile.Save(one), "the first saved");
            two.HidePanelsBehindGame = !two.HidePanelsBehindGame; two.MapView.IconSizes[888] = 0.75f; two.Chat.HideInCombat = !two.Chat.HideInCombat;
            Check(twoFile.Save(two), "the second saved");
            var both = Settings(root).Load(out _);
            Check(both.DeveloperMode == one.DeveloperMode && both.HidePanelsBehindGame == two.HidePanelsBehindGame, "each one's setting");
            Check(both.MapView.IconSizes.GetValueOrDefault(777u) == 0.25f && both.MapView.IconSizes.GetValueOrDefault(888u) == 0.75f, "each one's entry of one table");
            Check(both.Chat.ShowSeconds == one.Chat.ShowSeconds && both.Chat.HideInCombat == two.Chat.HideInCombat, "each one's member of one object");
            // At once: each client saves its own counter 25 times while a reader keeps reading the file.
            var path = oneFile.FilePath;
            var broken = 0;
            using var stop = new CancellationTokenSource();
            var reader = new Thread(() => { while (!stop.IsCancellationRequested) { try { SettingsJson.Node(System.IO.File.ReadAllText(path)); } catch (System.Text.Json.JsonException) { Interlocked.Increment(ref broken); } catch (IOException) { } } }) { IsBackground = true };
            reader.Start();
            var a = new Thread(() => { for (var i = 1; i <= 25; i++) { one.MapView.IconScale = 1f + i; oneFile.Save(one); } });
            var b = new Thread(() => { for (var i = 1; i <= 25; i++) { two.MapView.MapIconPriorities[5] = new() { [9] = i }; twoFile.Save(two); } });
            a.Start(); b.Start();
            Check(a.Join(TimeSpan.FromSeconds(60)) && b.Join(TimeSpan.FromSeconds(60)), "both ended");
            stop.Cancel(); reader.Join();
            var last = Settings(root).Load(out _);
            Check(last.MapView.IconScale == 26f && last.MapView.MapIconPriorities.GetValueOrDefault(5u)?.GetValueOrDefault(9u) == 25, $"each one's last value: {last.MapView.IconScale}, {last.MapView.MapIconPriorities.GetValueOrDefault(5u)?.GetValueOrDefault(9u)}");
            Check(last.DeveloperMode == one.DeveloperMode && last.HidePanelsBehindGame == two.HidePanelsBehindGame, "the earlier changes still there");
            Check(broken == 0, "the file read half written " + broken + " time(s)");
        })),
        ("Settings file 1745: behaviour: a write that fails changes nothing on disk, is said once, and the next save writes everything", () => InTemp(root =>
        {
            var said = new List<string>();
            var file = Settings(root, said);
            var value = file.Load(out _);
            Check(file.Save(value), "first save");
            var before = System.IO.File.ReadAllBytes(file.FilePath);
            value.DeveloperMode = !value.DeveloperMode;
            using (new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Check(!file.Save(value), "refused while another program holds the file");
                value.MapView.IconScale = 3.5f;
                Check(!file.Save(value), "refused again");
            }
            Check(System.IO.File.ReadAllBytes(file.FilePath).SequenceEqual(before), "the file as it was");
            Check(said.Count(line => line.StartsWith("The settings could not be written", StringComparison.Ordinal)) == 1, "said once: " + string.Join(" | ", said));
            value.HidePanelsBehindGame = !value.HidePanelsBehindGame;
            Check(file.Save(value), "written once free");
            var back = Settings(root).Load(out _);
            Check(back.DeveloperMode == value.DeveloperMode && back.MapView.IconScale == 3.5f && back.HidePanelsBehindGame == value.HidePanelsBehindGame, "every change written, those of the failed saves too");
            Check(said.Contains("The settings are written again."), "said when it works again");
            Check(Directory.GetFiles(Path.GetDirectoryName(file.FilePath)!, "*.tmp").Length == 0, "no temporary file left");
        })),
        ("Settings file 1745: behaviour: names this version does not know are kept, at the top and inside an object, through a load and a save", () => InTemp(root =>
        {
            var file = Settings(root);
            file.Save(new Configuration());
            var node = SettingsJson.Node(System.IO.File.ReadAllText(file.FilePath));
            node["FromANewerTensho"] = new JsonObject { ["Kept"] = 7 };
            node["Chat"]!.AsObject()["NewerChatSetting"] = "kept";
            System.IO.File.WriteAllText(file.FilePath, SettingsJson.Text(node));
            var reader = Settings(root);
            var value = reader.Load(out _);
            value.Chat.ShowSeconds = !value.Chat.ShowSeconds;
            value.DeveloperMode = !value.DeveloperMode;
            Check(reader.Save(value), "saved");
            var after = SettingsJson.Node(System.IO.File.ReadAllText(file.FilePath));
            Check(after["FromANewerTensho"]?["Kept"]?.GetValue<int>() == 7, "the unknown setting kept");
            Check(after["Chat"]?["NewerChatSetting"]?.GetValue<string>() == "kept", "the unknown member of a known object kept");
            Check(after["Chat"]?["ShowSeconds"]?.GetValue<bool>() == value.Chat.ShowSeconds && after["DeveloperMode"]?.GetValue<bool>() == value.DeveloperMode, "the changes written");
        })),
        ("Settings file 1745: behaviour: a removal is kept (an entry removed by this client stays removed; one removed by another too)", () => InTemp(root =>
        {
            var start = new Configuration();
            start.MapView.IconSizes[111] = 0.5f; start.MapView.IconSizes[222] = 0.5f;
            Settings(root).Save(start);
            var oneFile = Settings(root); var one = oneFile.Load(out _);
            var twoFile = Settings(root); var two = twoFile.Load(out _);
            one.MapView.IconSizes.Remove(111);
            Check(oneFile.Save(one), "saved");
            two.MapView.IconSizes.Remove(222); two.DeveloperMode = !two.DeveloperMode;
            Check(twoFile.Save(two), "saved");
            var node = SettingsJson.Node(System.IO.File.ReadAllText(oneFile.FilePath));
            var sizes = node["MapView"]!["IconSizes"]!.AsObject();
            Check(!sizes.ContainsKey("111") && !sizes.ContainsKey("222"), "both removals kept: " + sizes.ToJsonString());
        })),
        ("Settings file 1745: behaviour: a file that cannot be read is kept aside, never written over unseen; the backup, then Dalamud's file", () => InTemp(root =>
        {
            var said = new List<string>();
            var file = Settings(root, said);
            var value = Filled();
            Check(file.Save(value), "first save");
            value.DeveloperMode = !value.DeveloperMode;
            Check(file.Save(value), "second save: the first kept as the backup");
            System.IO.File.WriteAllText(file.FilePath, "{ \"DeveloperMode\": tru");
            var back = Settings(root, said).Load(out var origin);
            Check(origin == SettingsOrigin.Backup && back.DeveloperMode != value.DeveloperMode && Canon(back) != Canon(new Configuration()), "the backup read: " + origin);
            var aside = Directory.GetFiles(Path.GetDirectoryName(file.FilePath)!, "Settings.unreadable-*.json");
            Check(aside.Length == 1 && System.IO.File.ReadAllText(aside[0]) == "{ \"DeveloperMode\": tru", "the unreadable file kept aside, whole");
            Check(SettingsJson.Read<Configuration>(System.IO.File.ReadAllText(file.FilePath)) is not null, "Settings.json readable again");
            InTemp(other =>
            {
                System.IO.File.WriteAllText(Path.Combine(other, "TenshoAssistant.json"), Legacy(value));
                Directory.CreateDirectory(Path.Combine(other, "TenshoAssistant"));
                System.IO.File.WriteAllText(Path.Combine(other, "TenshoAssistant", "Settings.json"), "not json");
                Check(Settings(other).Load(out var fallback) is not null && fallback == SettingsOrigin.LegacyAfterUnreadable, "no backup: Dalamud's file: " + fallback);
            });
        })),
        ("Settings file 1745: behaviour: a copy of Tensho that read and wrote its settings is collected after its unload (the leak X-685 found)", () => InTemp(root =>
        {
            Check(Collected(LoadCopy(root, "tensho"), TimeSpan.FromSeconds(10)), "a copy that used the settings file is collected after its unload");
            Check(!Collected(LoadCopy(root, "newtonsoft"), TimeSpan.FromSeconds(2)), "a copy that went Dalamud's way stays (the control: the test sees the leak)");
        })),
        ("Settings file 1745: behaviour: the desktop app finds Tensho's own file beside Dalamud's and reads it through its guard: no Discord address, the rest whole", () =>
        {
            Check(TenshoAssistant.Desktop.DesktopPaths.OwnSettings(Path.Combine(@"C:\A", "pluginConfigs", "TenshoAssistant.json")) == SettingsFile<Configuration>.In(Path.Combine(@"C:\A", "pluginConfigs", "TenshoAssistant")), "the same file as the plugin's");
            var value = Filled();
            value.Discord.WebhookUrl = "https://discord.com/api/webhooks/1/secret";
            var seen = SettingsJson.Read(TenshoAssistant.Desktop.SettingsGuard.Project(SettingsJson.Write(value)), typeof(Configuration)) as Configuration ?? throw new Exception("not read");
            Check(seen.Discord.WebhookUrl == "" && (seen.Discord.CategoryUrls is null || seen.Discord.CategoryUrls.Count == 0), "the Discord addresses taken out");
            seen.Discord.WebhookUrl = value.Discord.WebhookUrl; seen.Discord.CategoryUrls = value.Discord.CategoryUrls;
            Check(Canon(seen) == Canon(value), "everything else read whole: " + FirstDifference(Canon(seen), Canon(value)));
            var host = System.IO.File.ReadAllText(Path.GetFullPath("src/TenshoAssistant.Desktop/Host.cs")).ReplaceLineEndings("\n");
            Check(host.Contains("if(File.Exists(own) && Plugin.GetType(\"TenshoAssistant.SettingsJson\")?.GetMethod(\"Read\",[typeof(string),typeof(Type)]) is { } read)", StringComparison.Ordinal)
                && host.Contains("try{return read.Invoke(null,[SettingsGuard.Project(SettingsGuard.ReadBounded(own)),configurationType]);}", StringComparison.Ordinal), "the app reads the own file through the guard, with the plugin's reader");
            Check(!host.Contains("File.Write", StringComparison.Ordinal), "the app never writes the settings");
        }),
        ("Settings file 1745: wiring: the plugin reads and writes its own file, Dalamud's only once; no Newtonsoft left in the plugin", () =>
        {
            var plugin = System.IO.File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            Check(!plugin.Contains("GetPluginConfig()", StringComparison.Ordinal) && !plugin.Contains("SavePluginConfig(", StringComparison.Ordinal), "Dalamud's settings calls gone");
            Check(plugin.Contains("new SettingsFile<Configuration>(SettingsFile<Configuration>.In(pluginInterface.GetPluginConfigDirectory()),pluginInterface.ConfigFile.FullName,", StringComparison.Ordinal)
                && plugin.Contains("var configuration=settingsFile.Load(out var settingsOrigin);", StringComparison.Ordinal)
                && plugin.Contains("copy=>settingsFile.Save(copy,framework.IsInFrameworkUpdateThread),", StringComparison.Ordinal), "the plugin's settings file");
            var desktop = System.IO.File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Desktop.cs"));
            Check(desktop.Contains("SettingsFile<Configuration>.In(pluginInterface.GetPluginConfigDirectory())", StringComparison.Ordinal) && !desktop.Contains("ConfigFile", StringComparison.Ordinal), "the link watches the new file");
            var folder = Path.GetFullPath("src/TenshoAssistant");
            var left = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !path.EndsWith("CreditCatalog.cs", StringComparison.Ordinal))
                .Where(path => System.IO.File.ReadAllText(path).Contains("Newtonsoft.Json", StringComparison.Ordinal)).Select(path => Path.GetRelativePath(folder, path)).ToList();
            Check(left.Count == 0, "Newtonsoft still in: " + string.Join(", ", left));
        }),
    ];

    private static string FirstDifference(string a, string b)
    {
        var at = 0;
        while (at < a.Length && at < b.Length && a[at] == b[at]) at++;
        return at >= a.Length && at >= b.Length ? "none" : $"at {at}: «{a.Substring(Math.Max(0, at - 80), Math.Min(200, a.Length - Math.Max(0, at - 80)))}» against «{b.Substring(Math.Max(0, at - 80), Math.Min(200, b.Length - Math.Max(0, at - 80)))}»";
    }
}
