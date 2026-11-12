using TenshoAssistant.Fields;

// 0.1179 (X-189, user's request of 12/11 on the Occult Crescent, docs/evidence/0.1179-occult-alerts.md): critical engagements that open their
// registration and FATEs that start are said once while they last, nothing during a fight (DEC-041), and the developer capture writes the container.
internal static class OccultAlerts1179Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static OccultEvent Event(int id, int state, uint left = 67) => new(id, "CE " + id, state, left, 0, 1083, 120, -340);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Occult alerts 1179: an engagement open for registration is said once; one in battle is not; one that closes and opens again is said again", () =>
        {
            var said = new HashSet<int>();
            var first = OccultAlerts.Engagements([Event(33, OccultAlerts.Register), Event(34, 3)], said);
            Check(first.Select(item => item.Id).SequenceEqual([33]), "the open one only");
            Check(OccultAlerts.Engagements([Event(33, OccultAlerts.Register, 50)], said).Count == 0, "the same one a second later: nothing");
            Check(OccultAlerts.Engagements([Event(33, 2)], said).Count == 0 && said.Count == 0, "warm-up: forgotten");
            Check(OccultAlerts.Engagements([Event(33, OccultAlerts.Register)], said).Count == 1, "open again later: said again");
            Check(OccultAlerts.Engagements([Event(0, OccultAlerts.Register)], said).Count == 0, "no id: nothing");
        }),
        ("Occult alerts 1179: a FATE is said once while it runs; the countdown as the game writes it", () =>
        {
            var said = new HashSet<uint>();
            Check(OccultAlerts.Fates(new uint[] { 5, 6 }, id => id, said).Count == 2, "two new FATEs");
            Check(OccultAlerts.Fates(new uint[] { 5, 6, 7 }, id => id, said).SequenceEqual([7u]), "only the new one");
            Check(OccultAlerts.Fates(new uint[] { 7 }, id => id, said).Count == 0 && !said.Contains(5), "ended ones forgotten");
            Check(OccultAlerts.Clock(67) == "1:07" && OccultAlerts.Clock(0) == "0:00" && OccultAlerts.Clock(-3) == "0:00" && OccultAlerts.Clock(600) == "10:00", "clock");
        }),
        ("Occult alerts 1179: wiring: read from the island's container, held in a fight, chat, sound and desktop, on by default, developer capture", () =>
        {
            string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
            var reader = Read("src/TenshoAssistant/Fields/OccultCrescentReader.Dalamud.cs");
            Check(reader.Contains("var container=DynamicEventContainer.GetInstance();", StringComparison.Ordinal) && reader.Contains("{Events=ReadEvents()}", StringComparison.Ordinal), "container read with the state");
            var page = Read("src/TenshoAssistant/MainWindow.FieldContent.cs");
            Check(page.Contains("if(live is { InCombat:true })return;", StringComparison.Ordinal) && page.Contains("if(mapLive?.State is { InCombat:true })return false;", StringComparison.Ordinal), "DEC-041: alerts held and panel hidden in a fight");
            Check(page.Contains("if(OccultOnIsland?.Invoke()!=true){occultEngagementsSaid.Clear();occultFatesSaid.Clear();return;}", StringComparison.Ordinal), "only on the island");
            Check(page.Contains("ChatNotice?.Invoke(alert);", StringComparison.Ordinal) && page.Contains("LinkNotice?.Invoke(alert);", StringComparison.Ordinal), "chat and desktop");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("(\"OccultAlerts\",mainWindow.ObserveOccultAlerts),", StringComparison.Ordinal), "observed every frame, failing alone");
            Check(new TenshoAssistant.Configuration().OccultAlerts, "on by default");
            var capture = Read("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs");
            Check(capture.Contains("Line(\"occultevents\",120,line.ToString());", StringComparison.Ordinal) && capture.Contains("if(all==occultEvents)return;", StringComparison.Ordinal), "developer capture, on change");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1179-occult-alerts.md")), "evidence");
        }),
    ];
}
