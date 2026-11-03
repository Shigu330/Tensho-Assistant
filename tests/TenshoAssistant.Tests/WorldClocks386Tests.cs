using TenshoAssistant.Alarms;

// 0.386 (g, X-10): clocks of chosen time zones (6 at most, unknown ids left out) and a manual one-off alarm at a local date.
internal static class WorldClocks386Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("World clocks: known zones only, once each, 6 at most; local date parsed into a future moment within a year",() =>
        {
            var zones = new List<string>();
            Check(WorldClocks.Add(zones,"UTC") && !WorldClocks.Add(zones,"UTC") && !WorldClocks.Add(zones,"Not/AZone"),"known, once");
            var now = new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            Check(WorldClocks.Times(["UTC","Not/AZone"],now).Single().Time==now,"unknown left out");
            var utc = TimeZoneInfo.Utc;
            Check(WorldClocks.ParseLocal("2026-11-05 21:00",now,utc)==new DateTimeOffset(2026,11,5,21,0,0,TimeSpan.Zero),"parsed");
            Check(WorldClocks.ParseLocal("2026-11-01 21:00",now,utc) is null && WorldClocks.ParseLocal("2028-01-01 00:00",now,utc) is null
                && WorldClocks.ParseLocal("5 nov",now,utc) is null,"past, too far or not a date");
            Check(TenshoAssistant.ConfigurationReset.Reset.Contains("WorldClocks"),"reset list");
        }),
    ];
}
