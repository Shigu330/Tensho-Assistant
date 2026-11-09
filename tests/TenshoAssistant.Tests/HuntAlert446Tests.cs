// N-09 / X-13 (0.446): the A and S mark alert is a setting off by default, said once per mark; the conductor's call is only copied.
internal static class HuntAlert446Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunt marks: A/S alert off by default, reset, once per mark; the train call is copied with <flag> (also sent on click: Decisions447Tests)",() =>
        {
            Check(!new TenshoAssistant.Configuration().HuntMarkAlert && TenshoAssistant.ConfigurationReset.Reset.Contains("HuntMarkAlert"),"setting off by default and in the reset list");
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.HuntTrain.cs"));
            Check(ui.Contains("item.Kind==MapObjectKind.HuntMark && item.Level>=2") && ui.Contains("huntMarksAnnounced.Add("),"A and S only, once per mark");
            Check(ui.Contains("if(configuration.AlarmOptions.PlaySound)Alarms.AlarmService.PlaySound("),"sound follows the alarm options");
            // 0.447: the call is also sent on click (decided 2026-11-03, Decisions447Tests); the copy stays.
            Check(ui.Contains("localization.Format(\"HuntTrain.AnnounceText\"") && ui.Contains("CopyButton(localization.Get(\"HuntTrain.Announce\"),()=>announce+\" <flag>\")"),"copied with <flag>");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.ChatNotice=text=>chat.Print(\"[Tensho] \"+text);"),"local chat line only");
        }),
    ];
}
