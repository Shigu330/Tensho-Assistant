using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TenshoAssistant;
using TenshoAssistant.Alarms;

// 0.1328 (X-334, review of 0.1318 I6, R28.14): the « save then read back » of X-277 (GearExactOutsideCaps renamed GearExactAtCaps) and X-280
// (an alarm moved in place) went through reflection or System.Text.Json, while the plugin saves its settings with Newtonsoft and
// TypeNameHandling.Objects (Plugin.cs, saveText; Dalamud reads them the same way). Here the same serializer and settings, on a file written
// before the rename.
internal static class ConfigRoundTrip1328Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // The plugin's settings (Plugin.cs: saveText).
    private static readonly JsonSerializerSettings Settings = new() { TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple, TypeNameHandling = TypeNameHandling.Objects };
    private static string Save(Configuration configuration) => JsonConvert.SerializeObject(configuration, Settings);
    private static Configuration Load(string text) => JsonConvert.DeserializeObject<Configuration>(text, Settings) ?? throw new Exception("not read back");

    // A settings file as written before 0.1290: the old name, no new one.
    private static string BeforeRename(bool? exactOutsideCaps)
    {
        var file = JObject.Parse(Save(new Configuration()));
        Check(file.Remove("GearExactAtCaps"), "the new name was saved");
        if (exactOutsideCaps is { } value) file["GearExactOutsideCaps"] = value;
        return file.ToString(Formatting.None);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Config round trip 1328: the plugin's serializer reads the old name, writes the new one only, and keeps the choice across two saves", () =>
        {
            Check(Plugin().Contains("TypeNameAssemblyFormatHandling=Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple,TypeNameHandling=Newtonsoft.Json.TypeNameHandling.Objects", StringComparison.Ordinal), "the same settings as the plugin");
            var off = Load(BeforeRename(false));
            Check(!off.GearExactAtCaps, "turned off before the rename: off");
            var saved = Save(off);
            Check(saved.Contains("\"GearExactAtCaps\":false", StringComparison.Ordinal) && !saved.Contains("GearExactOutsideCaps", StringComparison.Ordinal), "the new name saved, never the old one");
            Check(!Load(saved).GearExactAtCaps && !Load(Save(Load(saved))).GearExactAtCaps, "still off after two saves");
            Check(Load(BeforeRename(true)).GearExactAtCaps, "on before the rename: on");
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
