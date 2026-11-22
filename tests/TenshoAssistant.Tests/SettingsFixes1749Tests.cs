using System.Diagnostics;
using TenshoAssistant;

// X-763 (review of 0.1746, minor findings), the settings file. M1: Settings.json and its backup unreadable: Dalamud's older file read as a
// step back, said so with the name of the copy kept aside, never as the first move. M2: a save on the game's thread waits a tenth of a
// second at most for another game client's lock (10 s before); the worker still waits. M3: the desktop app's note names the file read.
internal static class SettingsFixes1749Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-settings-1749-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SettingsFile<Configuration> Settings(string root, List<string>? said = null) =>
        new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), Path.Combine(root, "TenshoAssistant.json"), (error, message) => said?.Add(message));

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings fixes 1749: behaviour (M1): Settings.json and its backup unreadable: Dalamud's older file read as a step back, said so, the copy kept aside named; never the first move", () => InTemp(root =>
        {
            File.WriteAllText(Path.Combine(root, "TenshoAssistant.json"), SettingsJson.Write(new Configuration { FollowedQuests = [1] }));
            Check(Settings(root).Load(out var moved).FollowedQuests.SequenceEqual([1u]) && moved == SettingsOrigin.Legacy, "the first move: " + moved);
            var file = Settings(root);
            Check(file.Save(new Configuration { FollowedQuests = [1, 2] }) && file.Save(new Configuration { FollowedQuests = [1, 2, 3] }), "newer settings saved twice (a backup too)");
            File.WriteAllText(file.FilePath, "{ \"FollowedQuests\": [1, 2, 3");
            File.WriteAllText(file.BackupPath, "not json");
            var said = new List<string>();
            var reader = Settings(root, said);
            var back = reader.Load(out var origin);
            Check(origin == SettingsOrigin.LegacyAfterUnreadable && origin != SettingsOrigin.Legacy && back.FollowedQuests.SequenceEqual([1u]), "a step back, not the first move: " + origin);
            Check(reader.KeptAside is { } aside && aside.StartsWith("Settings.unreadable-", StringComparison.Ordinal)
                && File.ReadAllText(Path.Combine(Path.GetDirectoryName(reader.FilePath)!, aside)) == "{ \"FollowedQuests\": [1, 2, 3", "the unreadable file kept aside, whole, and named: " + reader.KeptAside);
            Check(said.Count(line => line.StartsWith("The settings file and its backup could not be read; Dalamud's older settings file of Tensho was read instead", StringComparison.Ordinal)) == 1, "said: " + string.Join(" | ", said));
            InTemp(other =>
            {
                Directory.CreateDirectory(Path.Combine(other, "TenshoAssistant"));
                File.WriteAllText(Path.Combine(other, "TenshoAssistant", "Settings.json"), "not json");
                var none = new List<string>();
                var alone = Settings(other, none);
                alone.Load(out var fresh);
                Check(fresh == SettingsOrigin.NewAfterUnreadable && fresh != SettingsOrigin.New && alone.KeptAside is not null, "nothing else: new settings, said, not a new install: " + fresh);
                Check(none.Contains("The settings file could not be read, nor any copy of it; new settings are used."), "said: " + string.Join(" | ", none));
                Check(Settings(Path.Combine(other, "empty")).Load(out var plain) is not null && plain == SettingsOrigin.New, "no file at all: still a new install");
            });
        })),
        ("Settings fixes 1749: behaviour (M2): on the game's thread a save waits a tenth of a second at most for another client's lock: refused, nothing changed, written at the next save; the worker waits", () => InTemp(root =>
        {
            var said = new List<string>();
            var file = Settings(root, said);
            var value = file.Load(out _);
            Check(file.Save(value), "first save");
            var before = File.ReadAllBytes(file.FilePath);
            value.DeveloperMode = !value.DeveloperMode;
            var watch = Stopwatch.StartNew();
            bool saved;
            using (FairFileLock.Hold(file.LockPath, 1_000, error => error))
                saved = file.Save(value, onGameThread: true);
            var spent = watch.ElapsedMilliseconds;
            Check(!saved && spent < 1_500, $"refused soon on the game's thread while another client holds the lock: {saved}, {spent} ms (was up to 10 s)");
            Check(File.ReadAllBytes(file.FilePath).SequenceEqual(before), "nothing changed");
            Check(said.Count(line => line.StartsWith("The settings could not be written", StringComparison.Ordinal)) == 1, "said once: " + string.Join(" | ", said));
            var other = FairFileLock.Hold(file.LockPath, 1_000, error => error);
            var release = Task.Run(() => { Thread.Sleep(600); other.Dispose(); });
            watch.Restart();
            Check(file.Save(value), "off the game's thread, the save waits for the other client");
            Check(watch.ElapsedMilliseconds >= 400, "it waited: " + watch.ElapsedMilliseconds + " ms");
            release.Wait();
            Check(Settings(root).Load(out _).DeveloperMode == value.DeveloperMode, "written once the lock was free");
            value.HidePanelsBehindGame = !value.HidePanelsBehindGame;
            Check(file.Save(value, onGameThread: true) && Settings(root).Load(out _).HidePanelsBehindGame == value.HidePanelsBehindGame, "on the game's thread, the lock free: written");
            Check(SettingsFile<Configuration>.GameThreadLockWaitMs == 100 && SettingsFile<Configuration>.LockWaitMs == 10_000, "a tenth of a second on the game's thread, 10 s off it");
        })),
        ("Settings fixes 1749: behaviour (M3): the desktop app's note names the file actually read; Dalamud's older one while Tensho's own is there is said as older", () =>
        {
            var dalamud = Path.Combine(@"C:\A", "pluginConfigs", "TenshoAssistant.json");
            var own = TenshoAssistant.Desktop.DesktopPaths.OwnSettings(dalamud);
            Check(TenshoAssistant.Desktop.DesktopPaths.ReadNote(own, own, true) == "read from " + own + " (read-only, without the Discord addresses)", "Tensho's own file");
            var older = TenshoAssistant.Desktop.DesktopPaths.ReadNote(dalamud, own, true);
            Check(older.StartsWith("read from " + dalamud + " ", StringComparison.Ordinal) && older.EndsWith("; older than " + own + ", which this copy of the plugin cannot read", StringComparison.Ordinal), "the older file said as older: " + older);
            Check(TenshoAssistant.Desktop.DesktopPaths.ReadNote(dalamud, own, false) == "read from " + dalamud + " (read-only, without the Discord addresses)", "no own file yet: Dalamud's, nothing more");
        }),
        ("Settings fixes 1749: wiring: the plugin says an unreadable file in the chat and saves on the game's thread with the short wait; the desktop app's note follows what Parse read", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("var keptAside=settingsFile.KeptAside;", StringComparison.Ordinal)
                && plugin.Contains("if(unreadableKey is not null)SettingsNotice(unreadableKey,keptAside ?? \"?\");", StringComparison.Ordinal)
                && plugin.Contains("if(settingsOrigin is SettingsOrigin.Legacy)log.Information(\"Settings moved", StringComparison.Ordinal), "the notices; the move said for the first move only");
            Check(plugin.Contains("copy=>settingsFile.Save(copy,framework.IsInFrameworkUpdateThread),", StringComparison.Ordinal), "the game's thread known to the save");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read("src/TenshoAssistant/Localization/Languages/" + language + ".json");
                foreach (var key in new[] { "SettingsUnreadableBackup", "SettingsUnreadableLegacy", "SettingsUnreadableNew" })
                    Check(text.Contains("\"" + key + "\": \"", StringComparison.Ordinal) && text[text.IndexOf("\"" + key + "\"", StringComparison.Ordinal)..].Split('\n')[0].Contains("{0}", StringComparison.Ordinal), language + ": " + key);
            }
            var host = Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("var value=Parse(path,out var from);", StringComparison.Ordinal) && host.Contains("DesktopPaths.ReadNote(from,own,File.Exists(own));", StringComparison.Ordinal)
                && host.Contains("            from=own;\n            try{return read.Invoke(", StringComparison.Ordinal) && host.Contains("        from=path;\n        var settings=new Newtonsoft.Json.JsonSerializerSettings", StringComparison.Ordinal), "the note follows the file Parse read");
        }),
    ];
}
