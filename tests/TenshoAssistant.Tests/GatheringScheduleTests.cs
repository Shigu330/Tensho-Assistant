using TenshoAssistant.Gathering;

internal static class GatheringScheduleTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gathering rare HHmm and special 160 duration match verified native encoding", () =>
        {
            var threeHours = GatheringSchedule.FromRare([(900, 300), (65535, 0)]);
            Check(threeHours.Windows.SequenceEqual([new GatheringWindow(540, 720)]), "09:00 to 12:00");
            var twoHours = GatheringSchedule.FromRare([(1300, 160)]);
            Check(twoHours.Windows.Single() == new GatheringWindow(780, 900), "160 encodes two hours, not 160 minutes");
            Check(threeHours.Label == "09:00–12:00 ET", "ET explicit in UI");
        }),
        ("Gathering midnight windows merge overlapping restrictions and retain exact minutes", () =>
        {
            var rare = GatheringSchedule.FromRare([(2300, 200), (30, 200), (2300, 200)]);
            Check(rare.Windows.SequenceEqual([new GatheringWindow(0, 150), new GatheringWindow(1380, 1440)]), "split and union through midnight");
            var ephemeral = GatheringSchedule.FromEphemeral(2000, 0);
            Check(ephemeral.Windows.Single() == new GatheringWindow(1200, 1440), "20:00 to midnight without empty segment");
            Check(GatheringSchedule.FromEphemeral(400, 800).Windows.Single() == new GatheringWindow(240, 480), "ephemeral encoded in HHmm");
        }),
        ("Gathering unknown empty unrestricted and invalid never become equivalent", () =>
        {
            Check(GatheringSchedule.Unknown.Kind == GatheringScheduleKind.Unknown, "missing timing is unknown");
            Check(GatheringSchedule.FromRare([(65535, 0)]).Kind == GatheringScheduleKind.NoWindow, "empty table is not unrestricted");
            Check(GatheringSchedule.FromEphemeral(65535, 65535).Kind == GatheringScheduleKind.Unrestricted, "native no restriction sentinel");
            Check(GatheringSchedule.FromEphemeral(0, 2400).Kind == GatheringScheduleKind.Unrestricted, "whole day");
            Check(GatheringSchedule.FromEphemeral(400, 400).Kind == GatheringScheduleKind.Unrestricted, "equal valid bounds");
            foreach (var pair in new (ushort, ushort)[] { (65535, 0), (1260, 1300), (0, 2500) })
                Check(GatheringSchedule.FromEphemeral(pair.Item1, pair.Item2).Kind == GatheringScheduleKind.Invalid, "unexpected bounds remain invalid");
            Check(GatheringSchedule.FromRare([(0, 2400), (1260, 200)]).Kind == GatheringScheduleKind.Invalid, "invalid active slot not hidden by full-day slot");
            Check(GatheringSchedule.FromRare([(100, 260)]).Kind == GatheringScheduleKind.Invalid, "invalid duration minutes");
        }),
        ("Gathering immutable canonical schedules preserve distinct physical point windows", () =>
        {
            var input = new List<(ushort, ushort)> { (100, 200), (500, 200) };
            var first = GatheringSchedule.FromRare(input); input.Clear();
            Check(first.Windows.Count == 2, "copied windows");
            try { ((IList<GatheringWindow>)first.Windows).Clear(); throw new Exception("mutable"); }
            catch (NotSupportedException) { }
            Check(first.Key == GatheringSchedule.FromRare([(500, 200), (100, 200)]).Key, "order independent key");
            var row = new GatheringSource(42, 100, 10, 0, "Type", 1, 30, 20, "Zone", "Place", Schedule: first);
            var catalog = new GatheringCatalog([row, row with { PointId = 101 },
                row with { PointId = 102, Schedule = GatheringSchedule.FromRare([(900, 300)]) },
                row with { PointId = 103, Schedule = null }], "test");
            Check(catalog.ForItem(42).Count == 3, "equivalent windows coalesced, differing windows and unknown retained");
        }),
    ];
}
