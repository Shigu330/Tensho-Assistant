using TenshoAssistant.Character;

// X-76 P9 (0.936): leves, deliveries, allied quests and squadron kept per character; merged like the rest; daily and weekly counts only since their reset.
internal static class CharacterTimers936Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now=new(2026,11,8,20,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Character timers 936: merge keeps the last known, counts shown only since their reset, change detected",() =>
        {
            var old=new CharacterStatus{ContentId=1,LeveAllowances=40,DeliveriesLeft=6,AlliedQuestsLeft=3,TimersAt=Now.AddHours(-1),SquadronMissionDone=Now.AddHours(2)};
            var fresh=new CharacterStatus{ContentId=1,SeenAt=Now};
            var merged=CharacterStatuses.Merge(old,fresh);
            Check(merged.LeveAllowances==40 && merged.DeliveriesLeft==6 && merged.AlliedQuestsLeft==3 && merged.TimersAt==old.TimersAt && merged.SquadronMissionDone==old.SquadronMissionDone,"kept");
            var newer=CharacterStatuses.Merge(old,fresh with{LeveAllowances=41,AlliedQuestsLeft=2,TimersAt=Now});
            Check(newer.LeveAllowances==41 && newer.AlliedQuestsLeft==2 && newer.DeliveriesLeft==6 && newer.TimersAt==Now,"fresh wins");
            Check(CharacterStatuses.Changed(old,old with{LeveAllowances=41}) && !CharacterStatuses.Changed(old,old with{TimersAt=Now}),"change");
            Check(CharacterStatuses.DailyWeekly(old,Now.AddHours(-2),Now.AddDays(-2))==(3,6),"since reset");
            Check(CharacterStatuses.DailyWeekly(old,Now.AddMinutes(-10),Now.AddDays(-2))==(null,6),"daily reset passed");
            Check(CharacterStatuses.DailyWeekly(new CharacterStatus{ContentId=1},Now,Now)==(null,null),"never read");
        }),
    ];
}
