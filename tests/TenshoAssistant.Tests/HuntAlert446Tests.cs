// N-09 / X-13 (0.446): the A and S mark alert is a setting off by default, said once per mark; the conductor's call is only copied.
internal static class HuntAlert446Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunt marks: A/S alert off by default, reset, once per mark; the train call is copied with <flag>, never sent",() =>
        {
            var configuration=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Configuration.cs")).Replace("\r\n","\n");
            Check(configuration.Contains("public bool HuntMarkAlert { get; set; }\n") && configuration.Contains("\"HuntMarkAlert\","),"setting off by default and in the reset list");
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.HuntTrain.cs"));
            Check(ui.Contains("item.Kind==MapObjectKind.HuntMark && item.Level>=2") && ui.Contains("huntMarksAnnounced.Add("),"A and S only, once per mark");
            Check(ui.Contains("if(configuration.AlarmOptions.PlaySound)Alarms.AlarmService.PlaySound("),"sound follows the alarm options");
            Check(ui.Contains("ImGui.SetClipboardText(localization.Format(\"HuntTrain.AnnounceText\"") && ui.Contains("+\" <flag>\"") && !ui.Contains("SendMessage"),"copied, never sent");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.ChatNotice=text=>chat.Print(\"[Tensho] \"+text);"),"local chat line only");
        }),
    ];
}
