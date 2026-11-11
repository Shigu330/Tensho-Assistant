// X-122, D-102 (0.1042): alarms copied into the game's list go to Discord only when « Also send to Discord » is ticked (off by default).
internal static class GameAlarmDiscord1042Tests
{
    internal static readonly (string Name, Action Run)[] Cases =
    [
        ("Game alarm Discord 1042: off by default, read by the ring, drawn and saved",() =>
        {
            var model=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Alarms/TenshoAlarm.cs"));
            Check(model.Contains("public bool GameAlarmsToDiscord { get; set; }\n") || model.Contains("public bool GameAlarmsToDiscord { get; set; }\r\n"),"field with no initializer (off by default)");
            var service=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs"));
            Check(service.Contains("var discord = toDiscord is not null && (!inGame || settings.GameAlarmsToDiscord) && (wantsDiscord?.Invoke(alarm) ?? false);"),"ring sends a game alarm to Discord only when ticked");
            Check(service.Contains("if (inGame && !discord) continue;"),"a game alarm is still not rung twice");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Alarms.cs"));
            Check(page.Contains("ImGui.Checkbox(localization.Get(\"Alarms.GameDiscord\"),ref gameDiscord);Tip(\"Alarms.GameDiscordTip\");"),"box drawn with its tip");
            Check(page.Contains("next.GameAlarmsToDiscord=gameDiscord;"),"saved with the other alarm options");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"GameDiscord\":") && text.Contains("\"GameDiscordTip\":"),$"{language}: texts present");
            }
        }),
    ];

    private static void Check(bool condition,string message)
    {
        if(!condition)throw new Exception(message);
    }
}
