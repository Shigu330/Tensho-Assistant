using System.Diagnostics;
using TenshoAssistant;

// X-764 M1 (review of 0.1751): the save made at unload (the saver in direct mode, or a save asked after its Flush) ran on the game's thread
// with a tenth of a second for another client's lock and two reads; refused, it said « written at the next save » (or nothing when a failure
// was said before) while no save followed, and the unload said nothing: the last changes were lost unsaid. After SettingsFile.Close every
// save waits as long as the worker's, and its failure is said each time as the loss it is.
internal static class SettingsClose1753Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-settings-1753-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SettingsFile<Configuration> Settings(string root, List<string>? said = null) =>
        new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), Path.Combine(root, "TenshoAssistant.json"), (error, message) => said?.Add(message));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings close 1753: behaviour: after Close, a save asked on the game's thread waits for another client's lock past the tenth of a second, and writes", () => InTemp(root =>
        {
            var file = Settings(root);
            var value = file.Load(out _);
            Check(file.Save(value), "first save");
            Check(file.ClosingSave is null, "no save at unload yet");
            file.Close();
            value.DeveloperMode = !value.DeveloperMode;
            var other = FairFileLock.Hold(file.LockPath, 1_000, error => error);
            var release = Task.Run(() => { Thread.Sleep(600); other.Dispose(); });
            var watch = Stopwatch.StartNew();
            Check(file.Save(value, onGameThread: true), "written once the other client let go");
            Check(watch.ElapsedMilliseconds >= 400, "it waited past the game's tenth of a second: " + watch.ElapsedMilliseconds + " ms");
            release.Wait();
            Check(file.ClosingSave == true, "the unload's save is known written");
            Check(Settings(root).Load(out _).DeveloperMode == value.DeveloperMode, "on disk");
        })),
        ("Settings close 1753: behaviour: after Close, a save that fails says the loss each time, even after a failure said before; nothing changes on disk", () => InTemp(root =>
        {
            var said = new List<string>();
            var file = Settings(root, said);
            var value = file.Load(out _);
            Check(file.Save(value), "first save");
            var before = File.ReadAllBytes(file.FilePath);
            value.DeveloperMode = !value.DeveloperMode;
            using (new FileStream(file.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Check(!file.Save(value), "held by another program: refused");
                Check(file.Failing, "the file is older than the settings in memory");
                Check(said.Count(line => line.StartsWith("The settings could not be written", StringComparison.Ordinal)) == 1, "the usual failure said once: " + string.Join(" | ", said));
                file.Close();
                Check(!file.Save(value, onGameThread: true), "the unload's save refused too");
                Check(file.ClosingSave == false, "known failed");
                Check(said.Count(line => line == SettingsFile<Configuration>.ClosingLost) == 1, "the loss said, though a failure was said before: " + string.Join(" | ", said));
                Check(!file.Save(value), "another save at unload refused");
                Check(said.Count(line => line == SettingsFile<Configuration>.ClosingLost) == 2, "said again: no later save to wait for");
                Check(!said.Skip(1).Any(line => line.Contains("written at the next save", StringComparison.Ordinal)), "never « written at the next save » at unload");
            }
            Check(File.ReadAllBytes(file.FilePath).SequenceEqual(before), "nothing changed on disk");
        })),
        ("Settings close 1753: behaviour: Failing is true after a failed write and false once one is written (the unload tries again then)", () => InTemp(root =>
        {
            var file = Settings(root);
            var value = file.Load(out _);
            Check(file.Save(value) && !file.Failing, "written: not failing");
            using (new FileStream(file.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Check(!file.Save(value) && file.Failing, "refused: failing");
            file.Close();
            value.HidePanelsBehindGame = !value.HidePanelsBehindGame;
            Check(file.Save(value) && !file.Failing && file.ClosingSave == true, "the unload's save written: failing no more");
            Check(Settings(root).Load(out _).HidePanelsBehindGame == value.HidePanelsBehindGame, "the last settings on disk");
        })),
        ("Settings close 1753: wiring: the unload closes the settings file before the saver's Flush, saves once more when the last write failed, and says the late save written only when it was", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var close = plugin.IndexOf("settingsFile.Close();", StringComparison.Ordinal);
            var flush = plugin.IndexOf("if(saver.Flush(TimeSpan.FromSeconds(5)))", StringComparison.Ordinal);
            Check(close > 0 && flush > close, "Close before Flush");
            Check(plugin.Contains("if(settingsFile.Failing && settingsFile.ClosingSave is null)saver.Save();", StringComparison.Ordinal), "the last write failed: once more");
            Check(plugin.Contains("if(saver.Settle(TimeSpan.FromSeconds(15)))log.Warning(settingsFile.ClosingSave==false ? \"The last settings save took more than 5 s at unload and failed (said above).\" : ", StringComparison.Ordinal), "« written once it ended » only when it was");
        }),
    ];
}
