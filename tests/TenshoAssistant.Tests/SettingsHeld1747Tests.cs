using System.Text.Json.Nodes;
using TenshoAssistant;

// X-761 (review of 0.1746, I1, R28.14): Settings.json there but held by another program when Tensho loads. That session never writes it:
// no saved list replaced by the session's (taken whole), the file and its backup kept byte for byte, through the saver's unload too; the
// backup's settings shown when it can be read, new ones otherwise, never taken for a new install; said once, and once more when the file
// can be read again; the next load reads the file whole.
internal static class SettingsHeld1747Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-settings-1747-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SettingsFile<Configuration> Settings(string root, List<string>? said = null) =>
        new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), Path.Combine(root, "TenshoAssistant.json"), (error, message) => said?.Add(message));

    // Another program holding the file (an antivirus, a backup tool): nothing can open it, not even to read.
    private static FileStream Hold(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.None);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings held 1747: behaviour: a file held at load is never written by that session; the saved list, the file and its backup kept whole; read whole at the next load", () => InTemp(root =>
        {
            Check(Settings(root).Save(new Configuration { FollowedQuests = [101, 102, 103], DeveloperMode = true }), "first save");
            Check(Settings(root).Save(new Configuration { FollowedQuests = [101, 102, 103, 104], DeveloperMode = true }), "second save: the first kept as the backup");
            var said = new List<string>();
            var file = Settings(root, said);
            var fileBytes = System.IO.File.ReadAllBytes(file.FilePath);
            var backupBytes = System.IO.File.ReadAllBytes(file.BackupPath);
            var freeAgain = 0;
            file.FreeAgain = () => Interlocked.Increment(ref freeAgain);
            Configuration session;
            using (Hold(file.FilePath))
            {
                session = file.Load(out var origin);
                Check(origin == SettingsOrigin.HeldBackup && file.Unseen, "held: the backup's settings: " + origin);
                Check(session.FollowedQuests.SequenceEqual([101u, 102u, 103u]) && session.DeveloperMode, "the backup's list shown: " + string.Join(",", session.FollowedQuests));
                session.FollowedQuests.Add(555);
                session.DeveloperMode = false;
                Check(!file.Save(session), "not written while held");
                Check(freeAgain == 0, "not free yet");
            }
            session.FollowedQuests.Add(556);
            Check(!file.Save(session), "not written once free either: this session never read the file");
            Check(!file.Save(session), "nor at the next save");
            Check(System.IO.File.ReadAllBytes(file.FilePath).SequenceEqual(fileBytes), "the file as it was, byte for byte");
            Check(System.IO.File.ReadAllBytes(file.BackupPath).SequenceEqual(backupBytes), "its backup as it was, byte for byte");
            Check(freeAgain == 1, "told once that the file can be read again: " + freeAgain);
            Check(said.Count(line => line.StartsWith("The settings are not written", StringComparison.Ordinal)) == 1
                && said.Count(line => line.StartsWith("The settings file can be read again", StringComparison.Ordinal)) == 1
                && said.Count(line => line.StartsWith("The settings file could not be opened (held by another program); its backup is used", StringComparison.Ordinal)) == 1, "each said once: " + string.Join(" | ", said));
            var next = Settings(root).Load(out var nextOrigin);
            Check(nextOrigin == SettingsOrigin.Own && next.FollowedQuests.SequenceEqual([101u, 102u, 103u, 104u]) && next.DeveloperMode, "the next load reads the file whole: " + nextOrigin + " " + string.Join(",", next.FollowedQuests));
        })),
        ("Settings held 1747: behaviour: no backup: new settings for the session, not a new install, nothing written, the saver's unload included; the danger the test guards against is real", () => InTemp(root =>
        {
            var start = new Configuration { FollowedQuests = [7, 8, 9] };
            start.MapView.IconSizes[777] = 0.25f;
            Check(Settings(root).Save(start), "saved");
            var file = Settings(root);
            var fileBytes = System.IO.File.ReadAllBytes(file.FilePath);
            Check(!System.IO.File.Exists(file.BackupPath), "no backup");
            Configuration session;
            using (Hold(file.FilePath))
            {
                session = file.Load(out var origin);
                Check(origin == SettingsOrigin.Held && origin != SettingsOrigin.New && file.Unseen, "held, new settings, not a new install: " + origin);
                Check(session.FollowedQuests.Count == 0, "new settings");
            }
            session.FollowedQuests.Add(42);
            session.MapView.IconSizes[888] = 0.75f;
            var saver = new SettingsSaver<Configuration>(session, SettingsJson.Write, SettingsJson.Read<Configuration>, copy => file.Save(copy), action => action(), (_, _) => { });
            saver.Save();
            Check(saver.Flush(TimeSpan.FromSeconds(10)), "the saver's unload done");
            Check(System.IO.File.ReadAllBytes(file.FilePath).SequenceEqual(fileBytes), "the file as it was, byte for byte, after the saver's saves and its unload");
            Check(!System.IO.File.Exists(file.BackupPath), "no backup written");
            // The control: the merge a save would have made over the file never read (before = new settings) puts the session's list,
            // taken whole, over the saved one: the loss this session avoids by never writing.
            var merged = SettingsJson.Merge(SettingsJson.Node(SettingsJson.Write(new Configuration())), SettingsJson.Node(SettingsJson.Write(session)), SettingsJson.Node(System.IO.File.ReadAllText(file.FilePath)));
            Check(merged["FollowedQuests"]?.ToJsonString() == "[42]", "the control: such a merge loses the saved list: " + merged["FollowedQuests"]?.ToJsonString());
            var next = Settings(root).Load(out _);
            Check(next.FollowedQuests.SequenceEqual([7u, 8u, 9u]) && next.MapView.IconSizes.GetValueOrDefault(777u) == 0.25f && !next.MapView.IconSizes.ContainsKey(888u), "the next load reads the saved settings whole");
        })),
        ("Settings held 1747: wiring: a held file is no new install (no guide), said in the chat now or at the next login, and again when it is free; one wait at load, one attempt at a save", () =>
        {
            var plugin = System.IO.File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            Check(plugin.Contains("var newInstall=settingsOrigin==SettingsOrigin.New;", StringComparison.Ordinal), "only a true new install opens the guide");
            Check(plugin.Contains("if(settingsOrigin is SettingsOrigin.Held or SettingsOrigin.HeldBackup)SettingsNotice(settingsOrigin==SettingsOrigin.HeldBackup ? \"Plugin.SettingsHeldBackup\" : \"Plugin.SettingsHeld\");", StringComparison.Ordinal)
                && plugin.Contains("settingsFile.FreeAgain=()=>SettingsNotice(\"Plugin.SettingsFreeAgain\");", StringComparison.Ordinal)
                && plugin.Contains("registrations.Register(()=>client.Login+=settingsNoticeOnLogin,()=>client.Login-=settingsNoticeOnLogin);", StringComparison.Ordinal), "the chat notices");
            var code = System.IO.File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/SettingsFile.cs")).ReplaceLineEndings("\n");
            Check(code.Contains("try{own=ReadText(path,LoadAttempts);}", StringComparison.Ordinal) && code.Contains("try{return ReadText(file,1) is not null;}", StringComparison.Ordinal), "the waits");
            Check(!code.Contains("only what is changed in this session is written over it", StringComparison.Ordinal), "the old promise gone");
        }),
    ];
}
