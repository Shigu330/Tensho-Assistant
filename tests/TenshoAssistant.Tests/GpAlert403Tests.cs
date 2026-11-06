using TenshoAssistant.Gathering;

// 0.403 (g, R-08): GP alert — once when GP rises to the threshold (0 = full), armed again below it, never at the first read; off by default.
internal static class GpAlert403Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GP alert (0.657): the fall at a change of zone does not ring when GP climbs back, a real spending does",() =>
        {
            var tracker=new GpAlertTracker();
            Check(!tracker.Evaluate(809,809,0),"full before the teleport");
            tracker.Away();
            Check(!tracker.Evaluate(772,809,0),"the reading: 772 after it");
            Check(!tracker.Evaluate(780,809,0) && !tracker.Evaluate(804,809,0),"climbing back");
            Check(!tracker.Evaluate(809,809,0),"full again: nothing");
            Check(!tracker.Evaluate(400,809,0) && tracker.Evaluate(809,809,0),"then spent: it rings as usual");
            var spent=new GpAlertTracker();
            Check(!spent.Evaluate(809,809,0),"full");
            spent.Away();
            Check(!spent.Evaluate(772,809,0) && !spent.Evaluate(780,809,0),"climbing back");
            Check(!spent.Evaluate(300,809,0),"spent during the climb");
            Check(!spent.Evaluate(500,809,0) && spent.Evaluate(809,809,0),"this rise rings");
            var low=new GpAlertTracker();
            Check(!low.Evaluate(100,809,0),"low before the teleport");
            low.Away();
            Check(!low.Evaluate(772,809,0),"still below");
            Check(low.Evaluate(809,809,0),"a rise from low rings");
            var threshold=new GpAlertTracker();
            Check(!threshold.Evaluate(809,809,600),"above a threshold");
            threshold.Away();
            Check(!threshold.Evaluate(772,809,600),"the fall stays above it");
            Check(!threshold.Evaluate(500,809,600) && threshold.Evaluate(600,809,600),"a threshold works as usual");
        }),
        ("GP alert: once per rise to the threshold, full when 0, not at the first read, off by default",() =>
        {
            var tracker = new GpAlertTracker();
            Check(!tracker.Evaluate(700,700,0),"first read: starting point only");
            Check(!tracker.Evaluate(400,700,0) && !tracker.Evaluate(690,700,0) && tracker.Evaluate(700,700,0) && !tracker.Evaluate(700,700,0),"once at full");
            Check(!tracker.Evaluate(300,700,500) && tracker.Evaluate(505,700,500) && !tracker.Evaluate(600,700,500),"threshold 500, once");
            Check(GpAlertTracker.Target(700,900)==700,"threshold above the maximum = full");
            Check(!new TenshoAssistant.Configuration().GpAlert && TenshoAssistant.ConfigurationReset.Reset.Contains("GpAlertThreshold"),"off by default");
        }),
    ];
}
