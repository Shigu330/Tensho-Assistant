using TenshoAssistant.UI;

// 0.1483 (X-500: review of 0.1482): the game map Tensho hosts off screen moves its root node only; the windows' reader skips it by the node.
internal static class ChatReview1483Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1483: the hosted game map is skipped by its root node, where Tensho puts it (X-500)", () =>
        {
            Check(GameWindows.HostedOffScreen(GameWindows.OffScreen, GameWindows.OffScreen), "where the replacer puts it: off screen");
            Check(!GameWindows.HostedOffScreen(300, 200), "on screen");
            Check(!GameWindows.HostedOffScreen(-60, -20), "partly past the screen's edge: still on screen");
            Check(!GameWindows.HostedOffScreen(-2000, 100), "far left by the player, on a second screen: still read");
            var replacer = Read("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs");
            Check(replacer.Contains("SetPositionFloat(UI.GameWindows.OffScreen,UI.GameWindows.OffScreen)", StringComparison.Ordinal), "the replacer moves it there");
            Check(!replacer.Contains("SetPositionFloat(-9000,-9000)", StringComparison.Ordinal), "one value, not two");
            var reader = Read("src/TenshoAssistant/UI/GameWindowsReader.Dalamud.cs");
            var skip = reader.IndexOf("if(GameWindows.HostedOffScreen(unit->RootNode->X,unit->RootNode->Y))continue;", StringComparison.Ordinal);
            Check(skip > 0 && skip < reader.IndexOf("found.Add(", StringComparison.Ordinal), "the reader skips it before listing it, by the node, not by unit->X");
        }),
    ];
}
