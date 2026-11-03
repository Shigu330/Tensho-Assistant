using TenshoAssistant.Alarms;

// 0.366 (g, C-10): duty ready alert — off by default, alarm silence rules, Discord in its own category, unsubscribed on dispose.
internal static class DutyPop366Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Duty ready: off by default, own Discord category, alarm silence rules, event released",() =>
        {
            Check(!new AlarmSettings().DutyPopAlert && new DiscordSettings().DutyPop,"alert off by default; Discord category on once the alert is on");
            var settings = new DiscordSettings { Enabled = true, WebhookUrl = "https://discord.com/api/webhooks/12345678901234567/" + new string('a',40), DutyPop = false };
            Check(!DiscordWebhook.ShouldSend(settings,AlarmSource.DutyPop) && DiscordWebhook.ShouldSend(settings,AlarmSource.Currency),"category switch");
            var plugin = Read("Plugin.cs");
            Check(plugin.Contains("if(!options.DutyPopAlert)return;",StringComparison.Ordinal) && plugin.Contains("options.ChatAllowed(inDuty)",StringComparison.Ordinal)
                && plugin.Contains("()=>client.CfPop+=OnDutyPop,()=>client.CfPop-=OnDutyPop",StringComparison.Ordinal),"opt-in, silence rules, released");
            Check(Read("MainWindow.Alarms.cs").Contains("DutyPopAlert=previous.DutyPopAlert",StringComparison.Ordinal),"kept when the other alarm options are saved");
        }),
    ];
}
