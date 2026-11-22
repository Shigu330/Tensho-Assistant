using TenshoAssistant.Alarms;

// X-727 (0.1733, the user's request of 21/11): the tracker's rows: the alarm's icon, else its item's (an alarm made without one has 0); the
// common item tooltip with the alarm's place and next time; the common item menu with the alarm's gestures first.
internal static class TrackerRows1733Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Tracker rows 1733: an alarm without an icon shows its item's; its own icon is kept; no item icon known: none", () =>
        {
            var now = new DateTimeOffset(2026, 11, 21, 13, 0, 0, TimeSpan.Zero);
            var soil = new TenshoAlarm { Label = "Thanalan Topsoil III · Hammerlea", ItemId = 44000, IconId = 0, Repeat = AlarmRepeat.Once, At = now.AddMinutes(1) };
            var point = new TenshoAlarm { Label = "Point", ItemId = 44001, IconId = 60438, Repeat = AlarmRepeat.Once, At = now.AddMinutes(2) };
            static uint Sheet(uint id) => id == 44000 ? 25012u : id == 44001 ? 25013u : 0u;
            var rows = TrackedAlarms.Rows([soil, point], now, itemIcon: Sheet);
            Check(rows.Count == 2 && rows[0].IconId == 25012, "the item's icon when the alarm has none: " + (rows.Count > 0 ? rows[0].IconId : 0));
            Check(rows[1].IconId == 60438, "the alarm's own icon kept");
            Check(TrackedAlarms.Rows([soil], now)[0].IconId == 0 && TrackedAlarms.Icon(0, 44002, Sheet) == 0, "no item icon known: none (never a guessed icon)");
        }),
        ("Tracker rows 1733: wiring: the common tooltip with the place and time, the common menu with the alarm's gestures, en and fr", () =>
        {
            var tracker = Read("MainWindow.TrackerWindow.cs");
            Check(tracker.Contains("TrackedAlarms.Rows(configuration.Alarms,now,itemIcon:id=>itemCatalog.State.Catalog?.Find(id)?.IconId ?? 0)", StringComparison.Ordinal), "the Item sheet's icon");
            Check(tracker.Contains("TrackerRowHelp(\"TrackerIcon\",row,item,alarm,map,place,now);", StringComparison.Ordinal) && tracker.Contains("TrackerRowHelp(\"TrackerName\",row,item,alarm,map,place,now);", StringComparison.Ordinal), "icon and name both answer");
            Check(tracker.Contains("DrawItemInfoTooltip(row.ItemId,lines:lines);", StringComparison.Ordinal) && tracker.Contains("ItemLinksMenu(UniqueMenuId($\"{id}{row.Id}\"),item,first:", StringComparison.Ordinal), "the common tooltip and menu (R10)");
            Check(tracker.Contains("localization.Get(\"Alarms.Open\")", StringComparison.Ordinal) && tracker.Contains("localization.Get(\"Tracker.TurnOff\")", StringComparison.Ordinal)
                && tracker.Contains("alarm.Enabled=false;NoteGameCopy([alarm]);", StringComparison.Ordinal) && tracker.Contains("TrySave(\"Alarm turned off from the tracker\",()=>alarm.Enabled=true);", StringComparison.Ordinal), "the alarm's gestures; a failed save puts it back");
            Check(Read("MainWindow.DutyFinder.cs").Contains("if(first is not null){first();ImGui.Separator();}", StringComparison.Ordinal), "the caller's entries first");
            var fr = Read("Localization/Languages/fr.json"); var en = Read("Localization/Languages/en.json");
            Check(fr.Contains("\"TurnOff\": \"Couper cette alarme\"", StringComparison.Ordinal) && fr.Contains("\"Place\": \"Lieu : {0} · {1}\"", StringComparison.Ordinal) && fr.Contains("\"Next\": \"Prochaine alarme : {0} · {1}\"", StringComparison.Ordinal), "fr");
            Check(en.Contains("\"TurnOff\": \"Turn this alarm off\"", StringComparison.Ordinal) && en.Contains("\"Next\": \"Next alarm: {0} · {1}\"", StringComparison.Ordinal), "en");
        }),
    ];
}
