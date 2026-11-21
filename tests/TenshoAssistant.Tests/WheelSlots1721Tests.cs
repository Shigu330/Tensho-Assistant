using TenshoAssistant.Alarms;
using TenshoAssistant.FreeCompany;

// X-726 (0.1721, review of 0.1720, M1): a reading of the stand's window showing 0 slots is no reading (a stand holds 1 to 6: Item 8838 and
// 9462 to 9466): the house's wheels stay with their exact times and their alarms (behaviour test, R28.14: the reader's steps replayed).
internal static class WheelSlots1721Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static readonly AetherialWheelRow Pilgrimage = new(9668, 9700, 2, 45);
    private static AetherialWheelRow? Wheel(uint item) => item is 9668 or 9700 ? Pilgrimage : null;
    private static string Name(uint item) => item == 9668 ? "Grade 2 Wheel of Pilgrimage" : "";

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

    private const string Choosing = "0=2 1=1 2=1 3=0 4=1 5=100 10=0 11=0 12=2 13=100 18=0 19=0 20=0 21=100 26=0 27=0 28=0 29=100 34=0 35=0 36=0 37=100 42=0 43=0 44=0 45=100 50=0";
    private const string Set = "0=2 1=-1 2=0 3=0 4=1 5=100 10=0 11=9668 12=2 13=1 18=27682 19=0 20=0 21=100 26=0 27=0 28=0 29=100 34=0 35=0 36=0 37=100 42=0 43=0 44=0 45=100 50=0";
    private const string Later = "0=2 1=-1 2=0 3=0 4=1 5=100 10=0 11=9668 12=2 13=3 18=27682 19=0 20=0 21=100 26=0 27=0 28=0 29=100 34=0 35=0 36=0 37=100 42=0 43=0 44=0 45=100 50=0";
    private const string Zero = "0=0 1=0 2=0 3=0 4=0 5=0 10=0 11=0 12=0 13=0 18=0 19=0 20=0 21=0 26=0 27=0 28=0 29=0 34=0 35=0 36=0 37=0 42=0 43=0 44=0 45=0 50=0";

    // The reader's steps (AetherialWheelReader.OnUpdate): no slot yet or not as read, nothing; else the house's wheels updated.
    private static List<AetherialWheelSlot>? Step(List<AetherialWheelSlot>? saved, string line, ref (IReadOnlyList<AetherialWheelRead> Slots, DateTimeOffset At)? before, DateTimeOffset now)
    {
        var values = Values(line);
        if (AetherialWheels.NoSlotYet(values) || AetherialWheels.Parse(values, Wheel) is not { } read) return saved;
        var updated = AetherialWheels.Update(saved, 77, read, before, now, Wheel, Name);
        before = (read, now);
        return updated;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Wheel slots 1721: a window showing 0 slots reads nothing; a stand holds 1 to 6", () =>
        {
            var zero = Values(Zero);
            Check(AetherialWheels.NoSlotYet(zero) && AetherialWheels.Parse(zero, Wheel) is null, "0 slots: no reading");
            var one = Values(Set); one[0] = 1;
            Check(AetherialWheels.Parse(one, Wheel) is { Count: 1 } && !AetherialWheels.NoSlotYet(one), "1 slot: read (stand 8838)");
            var six = Values(Set); six[0] = 6;
            Check(AetherialWheels.Parse(six, Wheel) is { Count: 6 }, "6 slots: read (stand 9466)");
            var seven = Values(Set); seven[0] = 7;
            Check(AetherialWheels.Parse(seven, Wheel) is null && !AetherialWheels.NoSlotYet(seven), "7 slots: not as read, said as a game update");
            var negative = Values(Set); negative[0] = -1;
            Check(AetherialWheels.Parse(negative, Wheel) is null, "a negative count: nothing read");
        }),
        ("Wheel slots 1721: a wheel seen being set keeps its exact time and its alarm through a reading showing 0 slots", () =>
        {
            var at = new DateTimeOffset(2026, 11, 21, 11, 34, 7, TimeSpan.Zero);
            (IReadOnlyList<AetherialWheelRead> Slots, DateTimeOffset At)? before = null;
            var wheels = Step(null, Choosing, ref before, at.AddSeconds(-1));
            wheels = Step(wheels, Set, ref before, at);
            var set = wheels!.Single();
            Check(set.SeenSet && set.FullBy == at.AddHours(45), "seen being set: exact");
            var alarms = new List<TenshoAlarm>();
            Check(AutoAlarms.Sync(alarms, AetherialWheels.Due(wheels, at, name => name, "g"), at, 0) && alarms.Count == 1, "its automatic alarm");
            var afterZero = Step(wheels, Zero, ref before, at.AddHours(1));
            Check(ReferenceEquals(afterZero, wheels) && afterZero!.Single() == set, "0 slots: the saved wheels unchanged (nothing to save)");
            Check(AetherialWheels.Gone(alarms, afterZero).Count == 0 && !AutoAlarms.Sync(alarms, AetherialWheels.Due(afterZero, at.AddHours(1), name => name, "g"), at.AddHours(1), 0, AetherialWheels.Gone(alarms, afterZero)) && alarms.Count == 1, "its alarm stays");
            var next = Step(afterZero, Later, ref before, at.AddHours(1).AddSeconds(1))!.Single();
            Check(next.SeenSet && next.FullFrom == at.AddHours(45) && next.FullBy == at.AddHours(45) && next.Percent == 3, "read again later: still the exact time, not an estimate");
        }),
        ("Wheel slots 1721: wiring: the reader stops on no slot before Parse, in the developer's log only", () =>
        {
            var reader = Read("FreeCompany/AetherialWheelReader.Dalamud.cs");
            var noSlot = reader.IndexOf("if(AetherialWheels.NoSlotYet(values))", StringComparison.Ordinal);
            var parse = reader.IndexOf("if(AetherialWheels.Parse(values,Row) is not { } read)", StringComparison.Ordinal);
            var update = reader.IndexOf("AetherialWheels.Update(configuration.AetherialWheels,house,read,before,now,Row,Name)", StringComparison.Ordinal);
            Check(noSlot > 0 && noSlot < parse && parse < update, "no slot, then the layout, then the update");
            Check(reader.Contains("if(developer() && !noSlotSaid){noSlotSaid=true;log.Information(\"Aetherial wheels: the stand's window shows no slot yet: not read.\");}", StringComparison.Ordinal), "said in the developer's log once");
        }),
    ];
}
