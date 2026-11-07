using TenshoAssistant.Alarms;

// 0.598 (T24): a Discord channel per category. Addresses here are made up, of the right shape.
internal static class DiscordChannels598Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Url(char mark)=>"https://discord.com/api/webhooks/123456789012345678/"+new string(mark,40);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discord channels: a category's own address, else the main one",() =>
        {
            var settings=new DiscordSettings{Enabled=true,WebhookUrl=Url('a'),CategoryUrls=new(){[AlarmSource.Voyage]=Url('b')}};
            Check(DiscordWebhook.UrlFor(settings,AlarmSource.Voyage)==Url('b'),"its own");
            Check(DiscordWebhook.UrlFor(settings,AlarmSource.Gathering)==Url('a'),"the main one");
            Check(DiscordWebhook.UrlFor(new DiscordSettings{WebhookUrl=Url('a')},AlarmSource.Voyage)==Url('a'),"none set");
        }),
        ("Discord channels: a bad address never receives anything",() =>
        {
            var settings=new DiscordSettings{Enabled=true,WebhookUrl=Url('a'),CategoryUrls=new(){[AlarmSource.Voyage]="https://evil.net/api/webhooks/1/2"}};
            Check(DiscordWebhook.UrlFor(settings,AlarmSource.Voyage)==Url('a'),"falls back on the main one");
            var alone=new DiscordSettings{Enabled=true,CategoryUrls=new(){[AlarmSource.Voyage]="https://evil.net/api/webhooks/1/2"}};
            Check(!DiscordWebhook.ShouldSend(alone,AlarmSource.Voyage),"no valid address at all: nothing is sent");
        }),
        ("Discord channels: a category can send without a main address, the others cannot; switches still count",() =>
        {
            var settings=new DiscordSettings{Enabled=true,CategoryUrls=new(){[AlarmSource.Currency]=Url('b')}};
            Check(DiscordWebhook.ShouldSend(settings,AlarmSource.Currency) && !DiscordWebhook.ShouldSend(settings,AlarmSource.DutyPop),"only the one with an address");
            settings.Currencies=false;
            Check(!DiscordWebhook.ShouldSend(settings,AlarmSource.Currency),"its category off");
            settings.Currencies=true;settings.Enabled=false;
            Check(!DiscordWebhook.ShouldSend(settings,AlarmSource.Currency),"sending off");
            var fish=new TenshoAlarm{JobId=18,Discord=true};
            var fishing=new DiscordSettings{Enabled=true,CategoryUrls=new(){[AlarmSource.Fishing]=Url('c')}};
            Check(DiscordWebhook.ShouldSend(fishing,fish),"an alarm, through its category");
            fish.Discord=false;
            Check(!DiscordWebhook.ShouldSend(fishing,fish),"its own switch");
        }),
        ("Discord channels: every sender uses it, the reset keeps them, no report shows them",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(!plugin.Contains("discord.Enqueue(new(settings.WebhookUrl"),"no sender left on the main address alone");
            Check(plugin.Split("DiscordWebhook.UrlFor(settings,").Length==4,"alarms, currencies, duty ready");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/SettingRow.cs")).Contains("configuration.Discord.CategoryUrls=categoryWebhooks;"),"kept by a reset, as the main address");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discord.cs"));
            Check(view.Contains("ImGuiInputTextFlags.Password,reset:false);\n                    discordCategoryEdits".Replace("\n",view.Contains("\r\n") ? "\r\n":"\n")),"typed masked");
            Check(view.Contains("DiscordWebhook.Redact(Read())"),"shown redacted");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Advanced.cs")).Contains("CategoryUrls"),"not in the diagnostic report");
            Check(TenshoAssistant.Help.ProblemReport.Clean("x "+Url('b')+" y")=="x <webhook> y","and a report cleans any address");
        }),
    ];
}
