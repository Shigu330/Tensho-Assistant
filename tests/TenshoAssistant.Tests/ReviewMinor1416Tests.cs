using TenshoAssistant.Alarms;
using TenshoAssistant.Inventory;

// 0.1416, X-431 (review of block 0.1405-0.1412, minor findings).
// M1: a retainer read with nothing left on sale takes its « sales end » alarm back; a retainer not read keeps it; a venture is never touched.
// M2: one reading of a retainer's gil (RetainerPurse): the open retainer's own purse, else the list's figure; GilWatcher and the table share it.
// M3: set aside (no timeout left since X-428): checked here that the garden code has none to catch.
internal static class ReviewMinor1416Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 15, 0, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minor 1416 M1: the sales emptied before their end: the alarm still to come is taken back, and only that one", () =>
        {
            var own = new TenshoAlarm { Label = "own", At = Now.AddHours(5) };
            var alarms = new List<TenshoAlarm> { own };
            DueEvent Market(int id, double hours) => new($"market:{id}", "Sales " + id, "Retainers", Now.AddHours(hours), AlarmSource.Reminder);
            DueEvent Venture(int id, double hours) => new($"venture:{id}", "Venture " + id, "Ventures", Now.AddHours(hours), AlarmSource.Reminder);
            Check(AutoAlarms.Sync(alarms, [Market(1, 20), Market(2, 30), Venture(1, 1)], Now, 5) && alarms.Count == 4, "three automatic alarms made");
            // Retainer 1 read again with nothing on sale; retainer 2 not read in this pass (no key): its alarm stays; the venture is not gone.
            Check(AutoAlarms.Sync(alarms, [Venture(1, 1)], Now.AddMinutes(1), 5, ["market:1"]), "a change to save");
            Check(!alarms.Any(alarm => alarm.AutoKey == "market:1"), "the emptied sales' alarm is gone");
            Check(alarms.Any(alarm => alarm.AutoKey == "market:2") && alarms.Any(alarm => alarm.AutoKey == "venture:1") && alarms.Contains(own), "the others stay");
            Check(!AutoAlarms.Sync(alarms, [Venture(1, 1)], Now.AddMinutes(2), 5, ["market:1"]), "nothing more to take back");
            // Read again on sale with a new end: made again.
            Check(AutoAlarms.Sync(alarms, [Market(1, 40), Venture(1, 1)], Now.AddMinutes(3), 5, []) && alarms.Any(alarm => alarm.AutoKey == "market:1" && alarm.At == Now.AddHours(40)), "back on sale: a new alarm");
            // A key both gone and read (never happens): the reading wins.
            Check(!AutoAlarms.Sync(alarms, [Market(1, 40), Venture(1, 1)], Now.AddMinutes(4), 5, ["market:1"]) && alarms.Any(alarm => alarm.AutoKey == "market:1"), "a reading is never taken back");
            var watcher = Read("Alarms/AutoAlarmWatcher.Dalamud.cs");
            Check(watcher.Contains("else gone.Add($\"market:{retainer->RetainerId}\");", StringComparison.Ordinal) && watcher.Contains("AutoAlarms.Sync(configuration.Alarms,events,now,options.LeadMinutes,gone)", StringComparison.Ordinal),
                "the watcher names the retainers read with nothing on sale");
            Check(watcher.IndexOf("var gone=new List<string>();", StringComparison.Ordinal) < watcher.IndexOf("if(retainers!=null && retainers->IsReady)", StringComparison.Ordinal),
                "gone only from retainers read in this pass (the list ready)");
        }),
        ("Review minor 1416 M2: one reading of a retainer's gil: the open one's purse, else the list's figure, for the journal and the table", () =>
        {
            Check(RetainerPurse.Gil(7, 100, (7, 250)) == 250, "the open retainer: its purse, read at once");
            Check(RetainerPurse.Gil(8, 100, (7, 250)) == 100 && RetainerPurse.Gil(8, 100, null) == 100, "another one, or none open: the list");
            Check(RetainerPurse.Gil(0, 100, (0, 250)) == 100, "no retainer id: never the purse");
            Check(Read("Inventory/GilWatcher.Dalamud.cs").Contains("var livePurse=RetainerPurse.Open(manager);", StringComparison.Ordinal), "GilWatcher");
            var reader = Read("Character/RetainerReader.Dalamud.cs");
            Check(reader.Contains("var open=RetainerPurse.Open(manager);", StringComparison.Ordinal) && reader.Contains("RetainerPurse.Gil(id,retainer->Gil,open)", StringComparison.Ordinal), "the table");
            Check(!reader.Contains("retainer->Gil,retainer->ItemCount", StringComparison.Ordinal), "the list's figure is no longer taken alone");
            var purse = Read("Inventory/RetainerPurse.Dalamud.cs");
            Check(purse.Contains("InventoryType.RetainerGil", StringComparison.Ordinal) && purse.Contains("item->ItemId==1", StringComparison.Ordinal) && purse.Contains("MemberFunctionPointers.GetInventoryContainer==0", StringComparison.Ordinal), "the purse read once, guarded (R08)");
        }),
        ("Review minor 1416 M3: set aside: no regular expression timeout left in the garden to provoke (X-428)", () =>
        {
            foreach (var file in new[] { "Reminders/GardenJournal.cs", "Reminders/GardenChat.cs" })
                Check(!Read(file).Contains("RegexMatchTimeoutException", StringComparison.Ordinal), file);
        }),
    ];
}
