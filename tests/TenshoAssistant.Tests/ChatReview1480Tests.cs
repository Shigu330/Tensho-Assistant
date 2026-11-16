using TenshoAssistant.UI;

// 0.1480 (X-497, X-498: review of 0.1479): the game's map on screen covers the chat window (it was ignored by name); the faded chat window is
// faded whole; Tensho failing to re-read a Dalamud link is not said to be the plugin's error.
internal static class ChatReview1480Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1480: the game map on screen covers the chat window; off screen it does not; the other interfaces keep ignoring it (X-497)", () =>
        {
            var map = new GameWindow("AreaMap", 300, 200, 900, 700, 4990);
            Check(!GameWindows.Ignored("AreaMap"), "read, no longer ignored by name");
            Check(GameWindows.Covers([map], 100, 600, 560, 320, 1920, 1080, chat: true), "the game map over the chat: it covers it");
            Check(!GameWindows.Covers([map], 100, 600, 560, 320, 1920, 1080), "the panels, the bars and the map do not count it");
            var hosted = map with { X = -10000, Y = -10000 };
            Check(!GameWindows.Covers([hosted], 100, 600, 560, 320, 1920, 1080, chat: true), "hosted off screen by Tensho: nothing");
            var beside = map with { X = 1000, Y = 100 };
            Check(!GameWindows.Covers([beside], 100, 600, 560, 320, 1920, 1080, chat: true), "beside the chat: nothing");
            var enlarged = map with { X = 0, Y = 0, Width = 1900, Height = 1060 };
            Check(!GameWindows.Covers([enlarged], 100, 600, 560, 320, 1920, 1080, chat: true), "a screen layer stays a screen layer");
            Check(GameWindows.ChatOnlyWindow("AreaMap") && !GameWindows.ChatOnlyWindow("Inventory"), "only the map is the chat's own");
            var tracker = Read("src/TenshoAssistant/MainWindow.TrackerWindow.cs");
            Check(tracker.Contains("UI.GameWindows.Covers(GameWindowList,at.X,at.Y,size.X,size.Y,screen.X,screen.Y,chat:chat)", StringComparison.Ordinal), "passed through");
            Check(Read("src/TenshoAssistant/MainWindow.ChatWindow.cs").Contains("BehindGame(at,chatSize,chat:true)", StringComparison.Ordinal), "the chat asks with it");
        }),
        ("Chat review 1480: the faded chat window is faded whole, before Begin (X-498 M1)", () =>
        {
            var panel = Read("src/TenshoAssistant/PanelWindow.cs");
            var pre = panel[panel.IndexOf("public override void PreDraw()", StringComparison.Ordinal)..panel.IndexOf("public override void PostDraw()", StringComparison.Ordinal)];
            Check(pre.Contains("ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alphaFaded);", StringComparison.Ordinal), "pushed before Begin");
            Check(panel.Contains("if (fadePushed) { ImGui.PopStyleVar(); fadePushed = false; }", StringComparison.Ordinal), "popped after End");
            Check(!Read("src/TenshoAssistant/MainWindow.ChatWindow.cs").Contains("PushStyleVar(ImGuiStyleVar.Alpha,ChatCover.FadedAlpha)", StringComparison.Ordinal), "not twice");
        }),
        ("Chat review 1480: a link Tensho cannot re-read is Tensho's error; the handler still gets the link's text (X-498 M2)", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            var parse = plugin.IndexOf("try{message=Dalamud.Game.Text.SeStringHandling.SeString.Parse(data);}", StringComparison.Ordinal);
            var call = plugin.IndexOf("try{handler(command,message);}", StringComparison.Ordinal);
            Check(parse > 0 && call > parse, "two separate steps");
            var between = plugin[parse..call];
            Check(between.Contains("Tensho could not re-read a link", StringComparison.Ordinal) && !between.Contains("DalamudLinkFailed", StringComparison.Ordinal), "a re-read failure is Tensho's, not the plugin's");
            Check(plugin[call..(call + 300)].Contains("mainWindow.DalamudLinkFailed=plugin;", StringComparison.Ordinal), "the handler's failure is the plugin's");
        }),
    ];
}
