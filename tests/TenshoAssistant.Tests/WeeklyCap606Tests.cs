using TenshoAssistant.Inventory;

// 0.606: the weekly limit of the limited tomestones.
internal static class WeeklyCap606Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Weekly cap: once when reached, again the week after",() =>
        {
            var tracker=new WeeklyCapTracker();
            Check(!tracker.Reached(1,400,450),"under");
            Check(tracker.Reached(1,450,450),"reached");
            Check(!tracker.Reached(1,450,450),"not twice");
            Check(!tracker.Reached(1,0,450),"the reset");
            Check(!tracker.Reached(1,440,450) && tracker.Reached(1,450,450),"next week");
        }),
        ("Weekly cap: already there at the first reading, another character, unknown or no limit",() =>
        {
            var tracker=new WeeklyCapTracker();
            Check(!tracker.Reached(1,450,450),"nothing just happened");
            Check(!tracker.Reached(1,null,450) && !tracker.Reached(1,450,null) && !tracker.Reached(1,0,0),"unknown");
            tracker.Reached(1,400,450);
            Check(!tracker.Reached(2,450,450),"another character starts over");
            Check(new TenshoAssistant.Configuration().CurrencyWeeklyCapAlert,"on, with the alerts");
        }),
    ];
}
