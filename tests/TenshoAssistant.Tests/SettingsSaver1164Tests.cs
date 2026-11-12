using System.Collections.Concurrent;
using System.Text.Json;
using TenshoAssistant;

// 0.1164 (X-177 step 2): the settings saved off the game's thread (SettingsSaver). Behaviour tests (R28: concurrency, failed write,
// unload): the latest settings always end on disk, a write never lands over a newer one, any doubt goes back to the game's thread.
internal static class SettingsSaver1164Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public sealed class Settings { public int Version { get; set; } public List<string> Notes { get; set; } = []; }

    /// <summary>A fake disk: what was written, in order, with a slow write and an optional failure.</summary>
    private sealed class Disk
    {
        public readonly ConcurrentQueue<(int Version, int Thread)> Writes = new();
        public int SlowMs;
        public Func<Settings, bool>? Fail;
        public int Running;
        public int Overlaps;
        public void Write(Settings copy)
        {
            if (Interlocked.Increment(ref Running) > 1) Interlocked.Increment(ref Overlaps);
            try
            {
                if (SlowMs > 0) Thread.Sleep(SlowMs);
                if (Fail?.Invoke(copy) == true) throw new IOException("disk full");
                Writes.Enqueue((copy.Version, Environment.CurrentManagedThreadId));
            }
            finally { Interlocked.Decrement(ref Running); }
        }
        public int Last => Writes.LastOrDefault().Version;
    }

    private static (SettingsSaver<Settings> Saver, Settings Live, Disk Disk, List<string> Warnings, ConcurrentQueue<Action> Game) Make(Func<string, Settings?>? deserialize = null)
    {
        var live = new Settings();
        var disk = new Disk();
        var warnings = new List<string>();
        var game = new ConcurrentQueue<Action>();
        var saver = new SettingsSaver<Settings>(live, settings => JsonSerializer.Serialize(settings), deserialize ?? (text => JsonSerializer.Deserialize<Settings>(text)),
            disk.Write, game.Enqueue, (_, message) => { lock (warnings) warnings.Add(message); });
        return (saver, live, disk, warnings, game);
    }
    private static void Until(Func<bool> done) { var limit = Environment.TickCount64 + 10_000; while (!done() && Environment.TickCount64 < limit) Thread.Sleep(5); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings saver 1164: many saves while the disk is slow: one write at a time, in order, the latest last, off the caller's thread", () =>
        {
            var (saver, live, disk, warnings, _) = Make();
            disk.SlowMs = 20;
            for (var version = 1; version <= 50; version++) { live.Version = version; live.Notes.Add("n" + version); saver.Save(); }
            Check(saver.Flush(TimeSpan.FromSeconds(10)), "the worker ended");
            var versions = disk.Writes.Select(write => write.Version).ToArray();
            Check(versions.Length >= 1 && versions.Length < 50, "saves asked during a write are folded: " + versions.Length + " writes");
            Check(versions.Zip(versions.Skip(1)).All(pair => pair.First < pair.Second), "never an older one after a newer one: " + string.Join(",", versions));
            Check(disk.Last == 50, "the latest settings end on disk: " + disk.Last);
            Check(disk.Overlaps == 0, "one write at a time");
            Check(disk.Writes.All(write => write.Thread != Environment.CurrentManagedThreadId), "written off the caller's thread");
            Check(warnings.Count == 0 && !saver.Direct, "no doubt");
        }),
        ("Settings saver 1164: the copy written is the settings of the moment, not the live object changed afterwards", () =>
        {
            var (saver, live, disk, _, _) = Make();
            disk.SlowMs = 30;
            live.Version = 1; saver.Save();
            Until(() => disk.Running == 1);
            live.Version = 2; live.Notes.Add("later");
            live.Version = 3; saver.Save();
            saver.Flush(TimeSpan.FromSeconds(10));
            Check(disk.Writes.Select(write => write.Version).SequenceEqual(new[] { 1, 3 }), "1 then 3: " + string.Join(",", disk.Writes.Select(write => write.Version)));
        }),
        ("Settings saver 1164: saves from several threads at once (two components saving together): no lost save, no overlap", () =>
        {
            var (saver, live, disk, _, _) = Make();
            disk.SlowMs = 2;
            Parallel.For(0, 64, index => { lock (live) { live.Version = Math.Max(live.Version, index + 1); } saver.Save(); });
            live.Version = 100; saver.Save();
            saver.Flush(TimeSpan.FromSeconds(10));
            Check(disk.Last == 100 && disk.Overlaps == 0, "the last one written alone: " + disk.Last + ", overlaps " + disk.Overlaps);
        }),
        ("Settings saver 1164: a failed write goes back to the game's thread, with the live settings, said once; later saves are made there", () =>
        {
            var (saver, live, disk, warnings, game) = Make();
            disk.Fail = copy => copy.Version == 2;
            live.Version = 1; saver.Save(); Until(() => disk.Last == 1);
            live.Version = 2; saver.Save(); Until(() => saver.Direct);
            Check(saver.Direct && warnings.Count == 1, "said once: " + warnings.Count);
            disk.Fail = null;
            live.Version = 5;
            Until(() => !game.IsEmpty);
            while (game.TryDequeue(out var onGame)) onGame();
            Check(disk.Last == 5, "the live settings saved on the game's thread: " + disk.Last);
            live.Version = 6; saver.Save();
            Check(disk.Last == 6 && disk.Writes.Last().Thread == Environment.CurrentManagedThreadId, "then saved directly, as before");
            disk.Fail = _ => true;
            var thrown = false; try { saver.Save(); } catch (IOException) { thrown = true; }
            Check(thrown && warnings.Count == 1, "a direct save fails as before, to its caller; not said again");
        }),
        ("Settings saver 1164: a copy that does not give the same text is never written", () =>
        {
            var (saver, live, disk, warnings, game) = Make(text => JsonSerializer.Deserialize<Settings>(text) is { } copy ? new Settings { Version = copy.Version } : null);
            live.Version = 7; live.Notes.Add("kept");
            saver.Save(); Until(() => saver.Direct);
            Check(disk.Writes.IsEmpty, "the copy that lost a note is not written");
            Until(() => !game.IsEmpty);
            while (game.TryDequeue(out var onGame)) onGame();
            Check(disk.Last == 7 && warnings.Count == 1, "the live settings written on the game's thread instead");
        }),
        ("Settings saver 1164: unload writes what is left; a write still running after the wait does not stop the last save", () =>
        {
            var (saver, live, disk, _, _) = Make();
            disk.SlowMs = 50;
            live.Version = 1; saver.Save();
            live.Version = 2; saver.Save();
            Check(saver.Flush(TimeSpan.FromSeconds(10)) && disk.Last == 2, "flushed: the last one written");
            live.Version = 3; saver.Save();
            Check(disk.Last == 3 && disk.Writes.Last().Thread == Environment.CurrentManagedThreadId, "after unload, saved directly");
            var (slow, slowLive, slowDisk, _, _) = Make();
            slowDisk.SlowMs = 400;
            slowLive.Version = 1; slow.Save();
            Until(() => slowDisk.Running == 1);
            slowLive.Version = 2;
            Check(!slow.Flush(TimeSpan.FromMilliseconds(20)), "the wait ran out");
            Until(() => slowDisk.Writes.Count == 2);
            Check(slowDisk.Writes.Select(write => write.Version).SequenceEqual(new[] { 1, 2 }) && slowDisk.Overlaps == 0,
                "the live settings written anyway, after the write under way, never at the same time: " + string.Join(",", slowDisk.Writes.Select(write => write.Version)));
        }),
        ("Settings saver 1164: warm-up reads the saved text off the game's thread and writes nothing; a bad file is ignored", () =>
        {
            var (saver, _, disk, warnings, _) = Make();
            Check(saver.Warm(() => "{\"Version\":4,\"Notes\":[\"a\"]}").Wait(5000) && saver.Warm(() => "not json").Wait(5000) && saver.Warm(() => null).Wait(5000), "warm-ups ended");
            Check(disk.Writes.IsEmpty && warnings.Count == 0, "nothing written, nothing said");
        }),
        ("Settings saver 1164: wiring: every settings save of the plugin goes through it; the copy is checked; unload flushes", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Split("SavePluginConfig(").Length == 2 && plugin.Contains("copy=>pluginInterface.SavePluginConfig(copy),"), "one SavePluginConfig left: the saver's");
            Check(plugin.Contains("saver.Flush(TimeSpan.FromSeconds(5))") && plugin.Contains("saver.Warm("), "flushed at unload, warmed at load");
            Check(plugin.Contains("action=>_=framework.RunOnFrameworkThread(action)"), "the fallback runs on the game's thread");
            var saverText = Read("src/TenshoAssistant/SettingsSaver.cs");
            Check(saverText.Contains("if (copy is null || serialize(copy) != text)"), "the copy is checked against the text");
        }),
    ];
}
