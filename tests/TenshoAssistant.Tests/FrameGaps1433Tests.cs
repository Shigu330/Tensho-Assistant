// 0.1433, X-433 measure 8: the gaps between two of the game's frames, whatever ran on its thread, the longest kept with their end, a note for
// each long one, to see whether the stops of every managed thread during a background collection (and the blocking collections about 10 s after
// a reload) freeze the game. The tracker; the plugin listens in developer mode for the two minutes of the other measures, and lets go.
using TenshoAssistant.Diagnostics;

internal static class FrameGaps1433Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset At = new(2026, 11, 15, 16, 53, 0, TimeSpan.FromHours(1));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Frame gaps 1433: a long gap gives a note with its end; the longest are kept, longest first; short ones only counted", () =>
        {
            var gaps = new FrameGaps(150, 2, 1);
            Check(gaps.Frame(0, At) is null, "the first frame: no gap yet");
            Check(gaps.Frame(16, At.AddMilliseconds(16)) is null && gaps.Frame(33, At.AddMilliseconds(33)) is null, "ordinary frames: no note");
            var note = gaps.Frame(473, At.AddMilliseconds(473));
            Check(note == $"Game frame gap: 440 ms without a frame, ended {At.AddMilliseconds(473).ToLocalTime():HH:mm:ss.fff}.", "the note: " + note);
            Check(gaps.Frame(1249, At.AddMilliseconds(1249)) is null && gaps.Long == 2, "past the notes' cap: counted, not said");
            Check(gaps.Frame(1249, At.AddMilliseconds(1249)) is null, "no time between two calls: nothing");
            var summary = gaps.Summary(TimeSpan.FromMinutes(2));
            Check(summary == $"Game frames in the 120 s after the load: 6, 2 gaps of 150 ms or more; the longest: 776 ms (ended {At.AddMilliseconds(1249).ToLocalTime():HH:mm:ss.fff}), 440 ms (ended {At.AddMilliseconds(473).ToLocalTime():HH:mm:ss.fff}).", summary);
            Check(new FrameGaps().Summary(TimeSpan.FromMinutes(2)).EndsWith("the longest: none.", StringComparison.Ordinal), "none yet");
        }),
        ("Frame gaps 1433: the plugin counts the game's frames in developer mode, two minutes, and lets go at the end and at the unload", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            var start = plugin.IndexOf("var suspensions=Diagnostics.RuntimeSuspensionListener.Open(note=>log.Information(note));", StringComparison.Ordinal);
            var subscribe = plugin.IndexOf("pluginInterface.UiBuilder.Draw+=Gap;", StringComparison.Ordinal);
            Check(start > 0 && subscribe > start && plugin.IndexOf("if(configuration.DeveloperMode)", start - 200, StringComparison.Ordinal) > 0, "with the other measures, developer mode");
            Check(plugin.Contains("registrations.Register(()=>{},()=>{pluginInterface.UiBuilder.Draw-=Gap;pluginInterface.UiBuilder.HideUi-=GapHidden;});", StringComparison.Ordinal), "let go at the unload (0.1437: HideUi too)");
            var end = plugin.IndexOf("pluginInterface.UiBuilder.Draw-=Gap;\n                    pluginInterface.UiBuilder.HideUi-=GapHidden;\n                    if(Diagnostics.RuntimeSuspensionWatch.End(suspensions))", StringComparison.Ordinal);
            Check(end > subscribe && plugin.IndexOf("log.Information(gaps.Summary(Diagnostics.RuntimeSuspensionWatch.Length));", end, StringComparison.Ordinal) > end, "let go at the watch's end, then the summary");
        }),
    ];
}
