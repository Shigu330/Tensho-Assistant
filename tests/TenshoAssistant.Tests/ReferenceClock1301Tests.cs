// 0.1301 (X-282, review of block 0.1278, I2): the reference pictures read a clock that stands still (References.Clock), not the computer's:
// a page that follows the hour or the day (forecasts, countdowns, the trial garden, ocean fishing) is the same picture on every day, and
// every picture keeps the default tolerance. Behaviour checked by the run itself (two runs: 63 pictures alike, 370 clock sites rewritten).
internal static class ReferenceClock1301Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reference clock 1301: the plugin copy's reads of the computer's clock go to HostClock (every way of reading it)", () =>
        {
            var rewrite = Read("tools/PluginUiTest/src/PluginUiTest.Host/Rewrite.cs");
            foreach (var read in new[] { "System.DateTime::get_Now", "System.DateTime::get_UtcNow", "System.DateTime::get_Today",
                         "System.DateTimeOffset::get_Now", "System.DateTimeOffset::get_UtcNow", "System.TimeProvider::get_System" })
                Check(rewrite.Contains($"[\"{read}\"]=Clock(", StringComparison.Ordinal), read);
            Check(rewrite.Contains("DateTimeOffset? clock=null", StringComparison.Ordinal) && rewrite.Contains("HostClock.Fixed=clock;", StringComparison.Ordinal), "the clock given");
            Check(rewrite.Contains("+(clock is null ? \"\" : \"clock\")+", StringComparison.Ordinal), "a copy with the fixed clock has its own folder (never the application's copy)");
            Check(rewrite.Contains("public static class HostClock", StringComparison.Ordinal) && rewrite.Contains("Fixed is { } at ? at.ToUniversalTime() : DateTimeOffset.UtcNow", StringComparison.Ordinal),
                "the real clock when no instant is fixed (the application itself)");
        }),
        ("Reference clock 1301: only the reference pictures stand the clock still; the trial garden reads the same clock", () =>
        {
            var program = Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.Contains("stamp:References.Stamp,clock:References.Clock);", StringComparison.Ordinal), "the pictures' copy");
            Check(program.Contains("Rewrite.Plugin(plugin,Path.Combine(Own,\"plugin\"),Hooks,AppVersion.ToString(),PluginSiblings)", StringComparison.Ordinal), "the application's copy keeps the real clock");
            Check(Read("src/TenshoAssistant.Desktop/References.cs").Contains("public static readonly DateTimeOffset Clock=new(2026,11,11,10,0,0,TimeSpan.Zero);", StringComparison.Ordinal), "a fixed instant in UTC");
            var headless = Read("src/TenshoAssistant.Desktop/Headless.cs");
            var garden = headless[headless.IndexOf("public static bool GardenTrial", StringComparison.Ordinal)..];
            garden = garden[..garden.IndexOf("return host.SetSetting(\"GardenBeds\"", StringComparison.Ordinal)];
            Check(garden.Contains("var now=HostClock.UtcNow;", StringComparison.Ordinal) && !garden.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal), "the trial garden's planting dates");
        }),
    ];
}
