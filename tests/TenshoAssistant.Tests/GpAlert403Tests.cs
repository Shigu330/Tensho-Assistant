using TenshoAssistant.Gathering;

// 0.403 (g, R-08): GP alert — once when GP rises to the threshold (0 = full), armed again below it, never at the first read; off by default.
internal static class GpAlert403Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
