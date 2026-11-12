using System.Text.RegularExpressions;

// 0.1189. X-177 C6: the « Nearby monsters » line of the log is built in developer mode only (it was built every second for every player).
// (X-01 C1.3: the emptying of old instances this version added to find the leak's root is gone since 0.1196, the root being fixed.)
internal static class NearbyLog1189Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Nearby log 1189: cliquet: every « Nearby monsters » line is built only in developer mode", () =>
        {
            var source = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.NearbyMonsters.cs"));
            var calls = Regex.Matches(source, @"(.{0,24})NearbyMonstersLog\(\$").Select(match => match.Groups[1].Value).ToArray();
            Check(calls.Length >= 3, "calls read: " + calls.Length);
            Check(calls.All(before => before.EndsWith("if(developerMode)", StringComparison.Ordinal)), "a line built without asking developerMode first");
            Check(Regex.Matches(source, @"NearbyMonstersLog\(").Count == calls.Length + 1, "every call builds its text behind the check (plus the declaration)");
        }),
    ];
}
