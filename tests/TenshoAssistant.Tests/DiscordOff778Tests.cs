using TenshoAssistant.Alarms;

// 0.778 (X-23, user request): Discord off for a new alarm.
internal static class DiscordOff778Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discord off by default: a new alarm does not post; a saved one keeps its choice",() =>
        {
            Check(!new TenshoAlarm().Discord,"new");
            var saved=System.Text.Json.JsonSerializer.Deserialize<TenshoAlarm>("{\"Discord\":true}")!;
            Check(saved.Discord,"saved choice kept");
        }),
    ];
}
