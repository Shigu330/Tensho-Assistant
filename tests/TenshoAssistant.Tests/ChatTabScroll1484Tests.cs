using TenshoAssistant.Chat;

// 0.1484 (X-501: review of 0.1482): the lines are drawn outside the tab item since 0.1481; their area is named after its tab, so that each tab
// keeps its own scroll (one area for all tabs: a tab opened at the scroll of the one before, with « back to the bottom », its lines not read).
internal static class ChatTabScroll1484Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat tab scroll 1484: each tab has its own lines' area, so its own scroll (X-501)", () =>
        {
            var ids = Enumerable.Range(0, 12).Select(ChatFormat.LinesId).ToList();
            Check(ids.Distinct().Count() == ids.Count, "one area per tab: switching tabs never reuses the scroll of another");
            Check(ChatFormat.LinesId(3) == ChatFormat.LinesId(3), "the same tab finds its area, and its scroll, again");
            Check(ids.All(id => id.StartsWith("###", StringComparison.Ordinal)), "an ID only, nothing written");
            Check(ChatFormat.LinesId(1) != ChatFormat.LinesId(11) && ChatFormat.LinesId(11) == "###TenshoChatLines11", "no culture in the number");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("DrawChatLines(view,options,height,view.Serial);", StringComparison.Ordinal), "the tab shown names its area (0.1489, X-504 M2: by its view's own number)");
            Check(window.Contains("ImGui.BeginChild(ChatFormat.LinesId(tab),", StringComparison.Ordinal) && !window.Contains("BeginChild(\"###TenshoChatLines\"", StringComparison.Ordinal),
                  "never one area for all tabs again");
        }),
    ];
}
