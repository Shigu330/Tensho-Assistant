using TenshoAssistant.Desktop;

// 0.1537 (X-520, the chat's settings picture X-560 lacked): the pictures of the chat's tabs show the first tab's channels unfolded, the grid
// of X-560, wide and narrow; only when the desktop application opens that category, once.
internal static class ChatSettingsPicture1537Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat settings picture 1537: the chat's tabs pictures unfold the first tab's channels, once, from the desktop application only", () =>
        {
            var plan = ReferencePlan.Plan(DesktopPages.AsIs.Concat(DesktopPages.NeedsLink));
            Check(plan.Count(shot => ReferencePlan.SettingsSetup(shot.Setup) is { Category: "ChatTabs" }) == 2, "two pictures of the chat's tabs");
            var desktop = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(desktop.Contains("desktopChatChannelsOpen=category==\"ChatTabs\";"), "set when the application opens that category");
            var chat = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var unfold = chat.IndexOf("if(index==0 && desktopChatChannelsOpen){ImGui.SetNextItemOpen(true);desktopChatChannelsOpen=false;}", StringComparison.Ordinal);
            var node = chat.IndexOf("+\"###ChatTabChannels\"))", StringComparison.Ordinal);
            Check(unfold > 0 && node > unfold && node - unfold < 300, "just before the first tab's channels node, then forgotten");
            Check(chat.Split("desktopChatChannelsOpen").Length == 3 && desktop.Split("desktopChatChannelsOpen").Length == 3, "no other use");
        }),
        ("Chat settings picture 1537: a chat channel's name is not an error state (« ChatChannels.Error » is the game's error messages' channel)", () =>
        {
            Check(!ReferenceErrors.IsError("ChatChannels.Error") && ReferenceErrors.Shown(["ChatChannels.Error", "ChatChannels.Say"]).Length == 0, "the channels' names");
            Check(ReferenceErrors.IsError("Weather.Error") && ReferenceErrors.IsError("ChatWindow.Error") && ReferenceErrors.Shown(["ChatChannels.Error", "Allied.SheetsUnavailable"]).SequenceEqual(new[] { "Allied.SheetsUnavailable" }), "the other errors still seen");
        }),
    ];
}
