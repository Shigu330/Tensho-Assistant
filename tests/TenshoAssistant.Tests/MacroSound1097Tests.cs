using TenshoAssistant.Alarms;

// 0.1097 (user request, 11/11): the macros' end-line sound can be tried, as the alarms' « Listen » button (same service, same texts).
internal static class MacroSound1097Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Macro sound 1097: <se.1> to <se.16>, 0 is none",() =>
        {
            Check(!MacroSound.Valid(0) && MacroSound.Valid(1) && MacroSound.Valid(16) && !MacroSound.Valid(17) && !MacroSound.Valid(-1),"range");
        }),
        ("Macro sound 1097: the « Listen » button beside the setting goes through the alarm service",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            var settings=Read("MainWindow.Settings.cs");
            var row=settings.IndexOf("IntSetting(\"ModuleSettings.EchoSound\"",StringComparison.Ordinal);
            var button=settings.IndexOf("alarmService?.PreviewChatSound(search.EchoSound)",StringComparison.Ordinal);
            Check(row>0 && button>row && button-row<600,"button right after the setting");
            Check(settings.Contains("localization.Get(\"Alarms.Try\")+\"##EchoSoundTry\"") && settings.Contains("Tip(\"Advanced.TryTip\");"),"same texts as the alarms' button");
            var service=Read("Alarms/AlarmService.Dalamud.cs");
            Check(service.Contains("public void PreviewChatSound(int sound)") && service.Contains("if (chatPlay.Pending) PlayChatSound(chatPlay.Sound, log);"),"played on the framework update, like the alarm preview");
            Check(service.Contains("UIGlobals.PlayChatSoundEffect((uint)sound)"),"the game's chat sound effect");
        }),
    ];
}
