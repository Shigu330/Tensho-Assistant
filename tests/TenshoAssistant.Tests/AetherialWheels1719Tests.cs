using TenshoAssistant.Alarms;
using TenshoAssistant.FreeCompany;

// X-225 (0.1719): the aetherial wheels of the company's stand: the window's layout read on 21/11 (evidence 0.1234), when each wheel is primed
// (seen being set: exact; else bounds from its charge that narrow), a wheel taken off, the other houses kept, the automatic alarms.
internal static class AetherialWheels1719Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static readonly AetherialWheelRow Pilgrimage = new(9668, 9700, 2, 45);
    private static readonly AetherialWheelRow Confrontation = new(8844, 8860, 1, 20);
    private static AetherialWheelRow? Wheel(uint item) => item is 9668 or 9700 ? Pilgrimage : item is 8844 or 8860 ? Confrontation : null;
    private static string Name(uint item) => item == 9668 ? "Grade 2 Wheel of Pilgrimage" : item == 9700 ? "Primed Grade 2 Wheel of Pilgrimage" : "";
    private static bool Near(DateTimeOffset? value, DateTimeOffset expected) => value is { } at && Math.Abs((at - expected).TotalSeconds) < 1;

    // A line of « Reading addons » as dalamud.log wrote it on 21/11: the numbers it shows, the others (texts) left empty.
    private static int?[] Values(string line)
    {
        var values = new int?[51];
        foreach (var pair in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=');
            values[int.Parse(parts[0])] = int.Parse(parts[1]);
        }
        return values;
    }

    private const string Empty = "0=2 1=-1 2=0 3=0 4=1 5=100 10=0 11=0 12=2 13=100 18=0 19=0 20=0 21=100 26=0 27=0 28=0 29=100 34=0 35=0 36=0 37=100 42=0 43=0 44=0 45=100 50=0";
    private const string Choosing = "0=2 1=1 2=1 3=0 4=1 5=100 10=0 11=0 12=2 13=100 18=0 19=0 20=0 21=100 26=0 27=0 28=0 29=100 34=0 35=0 36=0 37=100 42=0 43=0 44=0 45=100 50=0";
    private const string Set = "0=2 1=-1 2=0 3=0 4=1 5=100 10=0 11=9668 12=2 13=1 18=27682 19=0 20=0 21=100 26=0 27=0 28=0 29=100 34=0 35=0 36=0 37=100 42=0 43=0 44=0 45=100 50=0";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Aetherial wheels 1719: the readings of 21/11 give the stand's slots; a window not so reads nothing", () =>
        {
            var empty = AetherialWheels.Parse(Values(Empty), Wheel)!;
            Check(empty.Count == 2 && empty.All(slot => slot.Item == 0) && empty[1].Slot == 2, "two empty slots");
            var set = AetherialWheels.Parse(Values(Set), Wheel)!;
            Check(set.Count == 2 && set[0].Item == 0 && set[1] == new AetherialWheelRead(2, 9668, 1), "slot 2: the Wheel of Pilgrimage at 1 % (the user's screenshot)");
            Check(AetherialWheels.Parse(Values(Choosing), Wheel)!.All(slot => slot.Item == 0), "while choosing: still empty");
            var seven = Values(Empty); seven[0] = 7;
            Check(AetherialWheels.Parse(seven, Wheel) is null, "more slots than a stand has: nothing read");
            var unknown = Values(Set); unknown[11] = 12345;
            Check(AetherialWheels.Parse(unknown, Wheel) is null, "an item that is no wheel: nothing read");
            var over = Values(Set); over[13] = 101;
            Check(AetherialWheels.Parse(over, Wheel) is null, "a charge over 100: nothing read");
            var text = Values(Set); text[11] = null;
            Check(AetherialWheels.Parse(text, Wheel) is null && AetherialWheels.Parse([], Wheel) is null, "a text where a number is, or nothing: nothing read");
        }),
        ("Aetherial wheels 1719: a wheel seen being set is primed at the sheet's hours; read again, nothing changes", () =>
        {
            var at = new DateTimeOffset(2026, 11, 21, 11, 34, 7, TimeSpan.Zero);
            var before = (AetherialWheels.Parse(Values(Choosing), Wheel)!, at.AddSeconds(-1));
            var read = AetherialWheels.Parse(Values(Set), Wheel)!;
            var wheels = AetherialWheels.Update(null, 77, read, before, at, Wheel, Name);
            var wheel = wheels.Single();
            Check(wheel.SeenSet && wheel.FullFrom == at.AddHours(45) && wheel.FullBy == at.AddHours(45) && wheel.Name == "Grade 2 Wheel of Pilgrimage" && wheel.Slot == 2 && wheel.HouseId == 77, "exact: set time plus 45 hours");
            var again = AetherialWheels.Update(wheels, 77, read, (read, at), at.AddSeconds(1), Wheel, Name);
            Check(AetherialWheels.SameAs(wheels, again), "read again at the same charge: nothing to save");
            var later = AetherialWheels.Update(wheels, 77, [new(1, 0, 100), new(2, 9668, 2)], null, at.AddMinutes(40), Wheel, Name).Single();
            Check(later.SeenSet && later.FullFrom == at.AddHours(45) && later.FullBy == at.AddHours(45) && later.Percent == 2, "a later percent that agrees keeps the exact time");
            var stale = AetherialWheels.Update(null, 77, read, (read, at.AddSeconds(-1)), at, Wheel, Name).Single();
            Check(!stale.SeenSet && stale.FullFrom != stale.FullBy, "not seen empty just before: an estimate, not the exact time");
            var old = AetherialWheels.Update(null, 77, read, (AetherialWheels.Parse(Values(Empty), Wheel)!, at.AddMinutes(-5)), at, Wheel, Name).Single();
            Check(!old.SeenSet, "empty five minutes before: not seen being set");
        }),
        ("Aetherial wheels 1719: from its charge, primed between two times that narrow; readings that disagree start again", () =>
        {
            var now = new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero);
            var first = AetherialWheels.Update(null, 77, [new(2, 9668, 50)], null, now, Wheel, Name).Single();
            Check(Near(first.FullFrom, now.AddHours(45 * 0.49)) && Near(first.FullBy, now.AddHours(45 * 0.51)) && !first.SeenSet, "50 %: between 49 % and 51 % of 45 hours");
            var second = AetherialWheels.Update([first], 77, [new(2, 9668, 51)], null, now.AddMinutes(30), Wheel, Name).Single();
            Check(Near(second.FullFrom, now.AddMinutes(30).AddHours(45 * 0.48)) && second.FullBy == first.FullBy, "51 % half an hour later: the earliest moves up, the latest stays");
            var jump = AetherialWheels.Update([second], 77, [new(2, 9668, 90)], null, now.AddHours(1), Wheel, Name).Single();
            Check(Near(jump.FullFrom, now.AddHours(1).AddHours(45 * 0.09)) && Near(jump.FullBy, now.AddHours(1).AddHours(45 * 0.11)), "90 % an hour later (another wheel): the new reading alone");
            var (from, by) = AetherialWheels.Bounds(0, TimeSpan.FromHours(20), now);
            Check(Near(from, now.AddHours(20 * 0.99)) && Near(by, now.AddHours(20 * 1.01)), "0 %: about the whole time");
        }),
        ("Aetherial wheels 1719: primed, taken off, other houses kept, the alarms follow", () =>
        {
            var now = new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);
            var charging = new AetherialWheelSlot { HouseId = 77, Slot = 2, Item = 9668, Name = "Pilgrimage", Percent = 98, Hours = 45, ReadAt = now.AddHours(-1), FullFrom = now.AddMinutes(10), FullBy = now.AddHours(1) };
            var other = new AetherialWheelSlot { HouseId = 12, Slot = 1, Item = 8844, Name = "Confrontation", Percent = 10, Hours = 20, ReadAt = now, FullFrom = now.AddHours(17), FullBy = now.AddHours(18) };
            var primed = AetherialWheels.Update([charging, other], 77, [new(1, 0, 100), new(2, 9700, 100)], null, now, Wheel, Name);
            var wheel = primed.Single(slot => slot.HouseId == 77);
            Check(wheel.Primed && wheel.FullBy == now && AetherialWheels.IsPrimed(wheel, now) && primed.Contains(other), "the primed item: primed, by now at the latest; the other house kept");
            Check(AetherialWheels.Update([charging], 77, [new(2, 9668, 100)], null, now, Wheel, Name).Single().Primed, "100 %: primed too");
            var taken = AetherialWheels.Update([charging, other], 77, [new(1, 0, 100), new(2, 0, 100)], null, now, Wheel, Name);
            Check(taken.Count == 1 && taken[0] == other, "an empty slot: the wheel is gone, the other house stays");
            Check(AetherialWheels.IsPrimed(charging, now.AddHours(2)) && !AetherialWheels.IsPrimed(charging, now) && AetherialWheels.NextPrimed([charging, other], now) == now.AddHours(1), "primed past its latest time; the next one");
            var due = AetherialWheels.Due([charging, other, wheel], now, name => name + " primed", "Aetherial wheels");
            Check(due.Count == 2 && due[0].Key == "wheel:77/2" && due[0].At == now.AddHours(1) && due[0].Label == "Pilgrimage primed" && due[1].Key == "wheel:12/1", "an alarm per wheel still charging, at its latest time");
            var alarms = new List<TenshoAlarm>();
            Check(AutoAlarms.Sync(alarms, due, now, 0) && alarms.Count == 2 && alarms[0].AutoKey == "wheel:77/2" && alarms[0].At == now.AddHours(1), "the automatic alarms made");
            var gone = AetherialWheels.Gone(alarms, taken);
            Check(gone.SequenceEqual(["wheel:77/2"]), "the wheel taken off: its alarm is gone");
            Check(AutoAlarms.Sync(alarms, AetherialWheels.Due(taken, now, name => name, "g"), now, 0, gone) && alarms.Single().AutoKey == "wheel:12/1", "and taken back");
            Check(DiscordWebhook.ReminderRowOf(new TenshoAlarm { AutoKey = "wheel:77/2" }) == "AetherialWheels", "its Discord switch: the Reminders row");
        }),
        ("Aetherial wheels 1719: wiring: the window read, the house checked, the reminder row, the alarms, the saved list, the game's words", () =>
        {
            var reader = Read("FreeCompany/AetherialWheelReader.Dalamud.cs");
            Check(reader.Contains("gui.GetAddonByName(\"AetherialWheel\")", StringComparison.Ordinal) && reader.Contains("if((nint)HousingManager.MemberFunctionPointers.GetCurrentHouseId==0)return 0;", StringComparison.Ordinal)
                && reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);", StringComparison.Ordinal), "the reader");
            Check(Read("Alarms/AutoAlarmWatcher.Dalamud.cs").Contains("events.AddRange(TenshoAssistant.FreeCompany.AetherialWheels.Due(configuration.AetherialWheels,now,", StringComparison.Ordinal), "the automatic alarms");
            Check(Read("MainWindow.Reminders.cs").Contains("Row(\"AetherialWheels\",FontAwesomeIcon.Cog,", StringComparison.Ordinal), "the Reminders row");
            var configuration = Read("Configuration.cs");
            Check(configuration.Contains("public List<TenshoAssistant.FreeCompany.AetherialWheelSlot>? AetherialWheels { get; set; }", StringComparison.Ordinal) && configuration.Contains("\"AetherialWheels\",", StringComparison.Ordinal), "saved, kept as player data");
            Check(Read("Plugin.cs").Contains("new FreeCompany.AetherialWheelReader(framework,gameGui,data,log,configuration,()=>SaveTimed(\"aetherial wheels\"),()=>configuration.DeveloperMode),()=>wheelReader?.Dispose());", StringComparison.Ordinal), "owned by the plugin");
            var fr = Read("Localization/Languages/fr.json");
            Check(fr.Contains("\"AetherialWheels\": \"Tours éthéréens\"", StringComparison.Ordinal) && fr.Contains("base de tour éthéréen", StringComparison.Ordinal) && fr.Contains(": chargé\"", StringComparison.Ordinal)
                && !fr.Contains("tours éthérées", StringComparison.Ordinal) && !fr.Contains("roue éthérée", StringComparison.Ordinal), "the game's own words (Addon 8458, 8630, 8640)");
            Check(Read("Localization/Languages/en.json").Contains("\"AetherialWheelsHelp\": ", StringComparison.Ordinal), "help");
        }),
    ];
}
