using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using TenshoAssistant.Diagnostics;

// X-685 (0.1752): an unloaded copy of Tensho that opened a SQLite file is collected once the copy's own Microsoft.Data.Sqlite factory is
// released (its prune timer and its two handlers of the process events held the whole load context for good). As in the game, the copy has
// private copies of Microsoft.Data.Sqlite and SQLitePCLRaw, loaded in its own context that can unload.
internal static class SqliteUnload1752Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly string[] Private = ["Microsoft.Data.Sqlite", "SQLitePCLRaw.core", "SQLitePCLRaw.provider.e_sqlite3", "SQLitePCLRaw.batteries_v2"];

    // A plugin's context as Dalamud makes one: its own copies of the plugin's dependencies, the runtime's assemblies shared.
    private sealed class PluginContext(string name) : AssemblyLoadContext(name, isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (!Private.Contains(assemblyName.Name, StringComparer.Ordinal)) return null;
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll"));
            return LoadFromStream(stream);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            foreach (var path in new[] { Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "e_sqlite3.dll"), Path.Combine(AppContext.BaseDirectory, "e_sqlite3.dll") })
                if (unmanagedDllName.Contains("e_sqlite3", StringComparison.OrdinalIgnoreCase) && File.Exists(path)) return LoadUnmanagedDllFromPath(path);
            return IntPtr.Zero;
        }
    }

    /// <summary>Run in the copy, by reflection: opens a file as Tensho does (not pooled), then releases the factory or not. Returns runtime
    /// types only (the counts read), so nothing of the copy comes back: the copy's own Microsoft.Data.Sqlite (not the host's), the process
    /// events' handlers from the copy before and after, and what the release said.</summary>
    public static int[] InCopy(string folder, bool release)
    {
        var ownCopy = AssemblyLoadContext.GetLoadContext(typeof(SqliteConnection).Assembly) == AssemblyLoadContext.GetLoadContext(typeof(SqliteUnload1752Tests).Assembly) ? 1 : 0;
        using (var open = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder, "copy.db"), Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString()))
        {
            open.Open();
            using var command = open.CreateCommand();
            command.CommandText = "create table if not exists kept(value integer); insert into kept values (1);";
            command.ExecuteNonQuery();
        }
        var context = AssemblyLoadContext.GetLoadContext(typeof(SqliteConnection).Assembly);
        var before = UnloadRelease.ProcessEventsFrom(context)?.Count ?? -1;
        var said = release ? UnloadRelease.Sqlite() : default;
        var after = UnloadRelease.ProcessEventsFrom(context)?.Count ?? -1;
        // A connection opened after the release still works (a late write of the unload).
        if (release)
            using (var again = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder, "copy.db"), Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString()))
            {
                again.Open();
                using var count = again.CreateCommand();
                count.CommandText = "select count(*) from kept";
                Check(Convert.ToInt64(count.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1, "the file read after the release");
            }
        return [ownCopy, before, after, said.Found ? 1 : 0, said.Timers, said.Handlers];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Context, int[] Counts) Load(string folder, bool release)
    {
        var context = new PluginContext("TenshoAssistant.Test1752." + (release ? "released" : "kept") + "." + Guid.NewGuid().ToString("N"));
        int[] counts;
        using (var stream = File.OpenRead(typeof(SqliteUnload1752Tests).Assembly.Location))
            counts = (int[])context.LoadFromStream(stream).GetType(nameof(SqliteUnload1752Tests), true)!.GetMethod(nameof(InCopy), BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [folder, release])!;
        context.Unload();
        return (new WeakReference(context), counts);
    }

    private static bool Collected(WeakReference context, TimeSpan within)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            if (!context.IsAlive) return true;
            if (watch.Elapsed > within) return false;
            Thread.Sleep(100);
        }
    }

    private static string Folder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "tensho-1752-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void Remove(string folder) { try { Directory.Delete(folder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Sqlite unload 1752: behaviour: a copy that opened a SQLite file is collected after its unload once its factory is released; without the release it stays (the leak X-685 found in the game)", () =>
        {
            var folder = Folder();
            try
            {
                var (released, counts) = Load(folder, release: true);
                Check(counts[0] == 1, "the copy uses its own Microsoft.Data.Sqlite, as a plugin does");
                Check(counts[3] == 1 && counts[4] == 1 && counts[5] == 2, "the release found the timer and the two handlers: " + string.Join(",", counts));
                Check(Collected(released, TimeSpan.FromSeconds(15)), "a copy whose factory was released is collected after its unload");
                var (kept, _) = Load(folder, release: false);
                Check(!Collected(kept, TimeSpan.FromSeconds(3)), "without the release the copy stays (the control: the test sees the leak)");
            }
            finally { Remove(folder); }
        }),
        ("Sqlite unload 1752: behaviour: the process events' handlers from the copy are read: the factory's two before the release, none after", () =>
        {
            var folder = Folder();
            try
            {
                var (_, counts) = Load(folder, release: true);
                Check(counts[1] == 2, "two handlers from the copy before the release (DomainUnload, ProcessExit): " + counts[1]);
                Check(counts[2] == 0, "none after: " + counts[2]);
                Check(UnloadRelease.ProcessEventsFrom(null) is null, "no context: nothing read");
                var host = UnloadRelease.ProcessEventsFrom(AssemblyLoadContext.Default);
                Check(host is not null, "the runtime's fields are read on this runtime (the game's line would say « unknown » otherwise)");
            }
            finally { Remove(folder); }
        }),
        ("Sqlite unload 1752: behaviour: the release's line says what was done, and says so when nothing was found", () =>
        {
            Check(new UnloadRelease.Released(true, 1, 2).Text.Contains("1 timer(s) stopped, 2 process event handler(s) taken off", StringComparison.Ordinal), "found");
            Check(new UnloadRelease.Released(false, 0, 0).Text.Contains("not found", StringComparison.Ordinal), "not found");
        }),
        ("Sqlite unload 1752: wiring: Plugin.Dispose releases the factory after the registrations (the data files closed), says it at unload, and the plugin and the tests use the same Microsoft.Data.Sqlite", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var dispose = plugin[plugin.IndexOf("public void Dispose()", StringComparison.Ordinal)..];
            var registrations = dispose.IndexOf("registrations.Dispose();", StringComparison.Ordinal);
            var release = dispose.IndexOf("Diagnostics.UnloadRelease.Sqlite()", StringComparison.Ordinal);
            Check(registrations > 0 && release > registrations, "the release comes after the registrations' cleanup");
            Check(dispose.Contains("Diagnostics.UnloadRelease.ProcessEventsFrom(", StringComparison.Ordinal), "the developer mode's line names what still holds the copy among the process's events");
            string Version(string project) => Regex.Match(File.ReadAllText(Path.GetFullPath(project)), "Include=\"Microsoft.Data.Sqlite\" Version=\"([^\"]+)\"").Groups[1].Value;
            Check(Version("src/TenshoAssistant/TenshoAssistant.csproj") is { Length: > 0 } version && version == Version("tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj"),
                "the test runs the Microsoft.Data.Sqlite the plugin ships (a later version is checked here first)");
        }),
    ];
}
