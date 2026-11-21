using TenshoAssistant;

// X-765 M4 (review of 0.1751, there before the block): Settings.json unreadable and its copy Settings.json.bak unreadable too: only
// Settings.json was kept aside; the unreadable copy stayed in place and the next save wrote the backup over it. A copy cut short, still
// mended by hand (« [1, 2, 3 »), was lost. Now the unreadable backup is kept aside under a name of its own at load, and said in the chat.
internal static class SettingsBackupAside1754Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-settings-1754-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "TenshoAssistant"));
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SettingsFile<Configuration> Settings(string root, List<string>? said = null) =>
        new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), Path.Combine(root, "TenshoAssistant.json"), (error, message) => said?.Add(message));

    private static string[] Aside(string root, string mark) => Directory.GetFiles(Path.Combine(root, "TenshoAssistant"), "Settings" + mark + "*.json");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings backup aside 1754: behaviour: the file and its backup unreadable: both kept aside under their own names, the backup's copy kept after the next save writes the backup", () => InTemp(root =>
        {
            var said = new List<string>();
            var file = Settings(root, said);
            File.WriteAllText(file.FilePath, "{\"Version\":1,\"FollowedQuests\":[1, 2");
            File.WriteAllText(file.BackupPath, "{\"Version\":1,\"FollowedQuests\":[1, 2, 3");
            var value = file.Load(out var origin);
            Check(origin == SettingsOrigin.NewAfterUnreadable, "nothing readable: new settings, said as such: " + origin);
            Check(file.KeptAside is { } own && own.StartsWith("Settings.unreadable-", StringComparison.Ordinal), "the file's copy: " + file.KeptAside);
            Check(file.BackupKeptAside is { } copy && copy.StartsWith("Settings.backup-unreadable-", StringComparison.Ordinal), "the backup's copy, a name of its own: " + file.BackupKeptAside);
            var backupCopy = Path.Combine(root, "TenshoAssistant", file.BackupKeptAside!);
            Check(File.ReadAllText(backupCopy) == "{\"Version\":1,\"FollowedQuests\":[1, 2, 3", "the backup's text kept as it was");
            Check(File.ReadAllText(Path.Combine(root, "TenshoAssistant", file.KeptAside!)) == "{\"Version\":1,\"FollowedQuests\":[1, 2", "the file's text kept as it was");
            value.DeveloperMode = !value.DeveloperMode;
            Check(file.Save(value) && file.Save(value), "saved twice: the backup is written over by now");
            Check(File.ReadAllText(file.BackupPath).Contains("\"DeveloperMode\"", StringComparison.Ordinal), "the backup is a copy of the new file");
            Check(File.ReadAllText(backupCopy) == "{\"Version\":1,\"FollowedQuests\":[1, 2, 3", "the unreadable backup's copy is still there, as it was");
            Check(said.Count(line => line.Contains("kept aside as Settings.backup-unreadable-", StringComparison.Ordinal)) == 1, "said once in the log: " + string.Join(" | ", said));
        })),
        ("Settings backup aside 1754: behaviour: a readable backup is read and never kept aside; the same unreadable text in both is kept once", () => InTemp(root =>
        {
            var file = Settings(root);
            File.WriteAllText(file.FilePath, "not json");
            File.WriteAllText(file.BackupPath, SettingsJson.Write(new Configuration { FollowedQuests = [7] }));
            Check(file.Load(out var origin).FollowedQuests.SequenceEqual([7u]) && origin == SettingsOrigin.Backup, "the backup read: " + origin);
            Check(file.BackupKeptAside is null && Aside(root, ".backup-unreadable-").Length == 0, "a readable backup is not kept aside");
            InTemp(other =>
            {
                var same = Settings(other);
                File.WriteAllText(same.FilePath, "[1, 2, 3");
                File.WriteAllText(same.BackupPath, "[1, 2, 3");
                same.Load(out _);
                Check(same.KeptAside is not null && same.BackupKeptAside is null && Aside(other, ".").Length == 1, "the same text kept once: " + string.Join(", ", Aside(other, ".").Select(Path.GetFileName)));
            });
        })),
        ("Settings backup aside 1754: wiring: the plugin says the backup's copy in the chat, in English and French", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("if(settingsFile.BackupKeptAside is { } backupAside)SettingsNotice(\"Plugin.SettingsBackupKeptAside\",backupAside);", StringComparison.Ordinal), "the notice");
            foreach (var language in new[] { "en", "fr" })
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"SettingsBackupKeptAside\": \"", StringComparison.Ordinal) , language);
        }),
    ];
}
