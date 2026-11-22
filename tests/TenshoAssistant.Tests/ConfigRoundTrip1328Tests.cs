using Newtonsoft.Json;
using TenshoAssistant;
using TenshoAssistant.Alarms;

// 0.1328 (X-334, review of 0.1318 I6, R28.14): the « save then read back » of X-277 (GearExactOutsideCaps renamed GearExactAtCaps) and X-280
// (an alarm moved in place) went through reflection or System.Text.Json, while the plugin saves its settings with Newtonsoft and
// TypeNameHandling.Objects (Plugin.cs, saveText; Dalamud reads them the same way). Here the same serializer and settings, on a file written
// before the rename. X-685 part 2 (0.1745): the plugin's serializer is now SettingsJson (System.Text.Json, Tensho's own file); a file
// written by Dalamud before (Newtonsoft, type names) is read once by SettingsJson.ReadLegacy: both are read here. X-574 (0.1793, the user's
// choice of 17/11): the name of before 0.1290 is no longer read; a file that still holds it loads all the same (the name ignored).
internal static class ConfigRoundTrip1328Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // The plugin's serializer (Plugin.cs: SettingsJson since 0.1745), and Dalamud's (Newtonsoft, type names) for a file written before.
    private static readonly JsonSerializerSettings Dalamud = new() { TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple, TypeNameHandling = TypeNameHandling.Objects };
    private static string Save(Configuration configuration) => SettingsJson.Write(configuration);
    private static Configuration Load(string text) => SettingsJson.Read<Configuration>(text) ?? throw new Exception("not read back");

    // A settings file as written before 0.1290: the old name, no new one (Tensho's own text, or Dalamud's).
    private static string BeforeRename(bool? exactOutsideCaps, bool dalamud = false)
    {
        var file = SettingsJson.Node(dalamud ? JsonConvert.SerializeObject(new Configuration(), Dalamud) : Save(new Configuration()));
        Check(file.Remove("GearExactAtCaps"), "the new name was saved");
        if (exactOutsideCaps is { } value) file["GearExactOutsideCaps"] = value;
        return SettingsJson.Text(file);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Config round trip 1328: the plugin's serializer keeps GearExactAtCaps across two saves; a file with the old name still loads (X-574: that name ignored)", () =>
        {
            Check(Plugin().Contains("live=>SettingsJson.Write(live),", StringComparison.Ordinal) && Plugin().Contains("text=>SettingsJson.Read<Configuration>(text),", StringComparison.Ordinal), "the same serializer as the plugin");
            var off = Load(Save(new Configuration { GearExactAtCaps = false, GearLevelsBelow = 7 }));
            Check(!off.GearExactAtCaps && off.GearLevelsBelow == 7, "turned off: off");
            var saved = Save(off);
            Check(SettingsJson.Node(saved)["GearExactAtCaps"]?.GetValue<bool>() == false && !saved.Contains("GearExactOutsideCaps", StringComparison.Ordinal), "the name saved, never the old one");
            Check(!Load(saved).GearExactAtCaps && !Load(Save(Load(saved))).GearExactAtCaps, "still off after two saves");
            var before = BeforeRename(false);
            Check(Load(before) is { GearExactAtCaps: true } && SettingsJson.ReadLegacy<Configuration>(BeforeRename(false, dalamud: true)) is { GearExactAtCaps: true }, "the old name ignored: the default, on (no error)");
            Check(Load(BeforeRename(null)).GearExactAtCaps, "never set: the default, on");
        }),
        ("Config round trip 1328: an alarm moved in place is read back by the plugin's serializer: same alarm, new time, nothing lost", () =>
        {
            var now = new DateTimeOffset(2026, 11, 13, 21, 55, 0, TimeSpan.Zero);
            var daily = new TenshoAlarm { Label = "Raid", Group = "Alarms at a date", Repeat = AlarmRepeat.RealDaily, At = now.AddDays(-3), LeadMinutes = 5,
                LastOccurrence = now.AddHours(-1), LastRung = now.AddHours(-1), Discord = true, DiscordMessage = "go" };
            var other = new TenshoAlarm { Label = "Other", Repeat = AlarmRepeat.Once, At = now.AddHours(5) };
            var configuration = new Configuration { Alarms = [daily, other] };
            Check(AlarmSchedule.Move(daily, new DateTimeOffset(2026, 11, 14, 19, 30, 0, TimeSpan.Zero)), "moved");
            var back = Load(Save(configuration)).Alarms;
            Check(back.Count == 2 && back.Count(alarm => alarm.Id == daily.Id) == 1, "same alarm, no copy");
            var moved = back.Single(alarm => alarm.Id == daily.Id);
            Check(moved.At == daily.At && moved.Repeat == AlarmRepeat.RealDaily && moved.Label == "Raid" && moved.Group == "Alarms at a date", "its time and kind");
            Check(moved.Discord && moved.DiscordMessage == "go" && moved.LeadMinutes == 5, "nothing else lost");
            Check(moved.LastOccurrence == default && moved.LastRung == default, "rings at the new time");
            Check(back.Single(alarm => alarm.Id == other.Id).At == other.At, "the other alarm untouched");
        }),
    ];

    private static string Plugin() => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
}
