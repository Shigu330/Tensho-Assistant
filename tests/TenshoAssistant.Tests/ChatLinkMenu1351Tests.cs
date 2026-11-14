using TenshoAssistant.Maps;

// X-365 (0.1351): a right click on a chat map link opens the Tensho place menu; the game's left click (flag) is untouched; read only, nothing
// during a cut. The hovered link's state (ChatLinkHover) and the wiring (Dalamud's addon lifecycle, the place entries, the cut).
internal static class ChatLinkMenu1351Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly HoveredMapLink Link = new(1185, 1052, 11.2f, 13.5f, "Tuliyollal");
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-365: the map link under the mouse is the one a right click is about", () =>
        {
            var hover = new ChatLinkHover();
            Check(hover.RightClick(1_000, cut: false, enabled: true) is null, "nothing hovered: no menu");
            hover.Over(Link);
            Check(hover.RightClick(1_010, cut: false, enabled: true) == Link, "hovered: the menu is about it");
            hover.Out(1_100);
            Check(hover.RightClick(1_100 + ChatLinkHover.Grace, cut: false, enabled: true) == Link, "mouse out as the button goes down: still it");
            Check(hover.RightClick(1_101 + ChatLinkHover.Grace, cut: false, enabled: true) is null, "mouse gone: no menu");
            hover.Over(Link);hover.Over(null);
            Check(hover.Current(2_000) is null, "another kind of link (item, player) under the mouse: no place menu");
        }),
        ("X-365: nothing during a cut or with the chat links off, and the hovered link is forgotten", () =>
        {
            var hover = new ChatLinkHover();
            hover.Over(Link);
            Check(hover.RightClick(1_000, cut: true, enabled: true) is null && hover.Current(1_000) is null, "cut: nothing, forgotten");
            hover.Over(Link);
            Check(hover.RightClick(1_000, cut: false, enabled: false) is null && hover.Current(1_000) is null, "chat links off: nothing, forgotten");
        }),
        ("X-365: the chat panels' link events through Dalamud's lifecycle, read only, nothing during a cut", () =>
        {
            var watcher = Read("src/TenshoAssistant/Maps/ChatLinkHoverWatcher.Dalamud.cs");
            Check(watcher.Contains("lifecycle.RegisterListener(AddonEvent.PreReceiveEvent,Panels,OnEvent)") && watcher.Contains("lifecycle.UnregisterListener(AddonEvent.PreReceiveEvent,Panels,OnEvent)"), "listener registered and released");
            Check(watcher.Contains("[\"ChatLogPanel_0\",\"ChatLogPanel_1\",\"ChatLogPanel_2\",\"ChatLogPanel_3\"]"), "the four chat panels");
            Check(watcher.Contains("LinkMouseOver=76,LinkMouseOut=77"), "the game's link event numbers (evidence)");
            Check(watcher.Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut || !enabled()){Hover.Clear();return;}"), "nothing during a cut");
            foreach (var call in new[] { "FireCallback", "SendEvent", "ReceiveEvent(", "Hook<", "ProcessChatBox", "UIModule" })
                Check(!watcher.Contains(call, StringComparison.Ordinal), "never acts on the game: " + call);
            Check(watcher.Contains("index<MaximumBytes && data->Payload[index]!=0"), "the copy is bounded");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1351-chat-map-link-menu.md")), "evidence");
        }),
        ("X-365: the right click opens Tensho's place menu, the game keeps its left click", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatMapLinks.cs");
            var menu = window[window.IndexOf("internal void DrawChatLinkMenu()", StringComparison.Ordinal)..];
            Check(menu.Contains("ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.GetIO().WantCaptureMouse"), "right click outside Tensho's windows");
            Check(menu.Contains("TenshoAssistant.Competitive.CompetitiveGate.Cut,configuration.MapView.ChatLinks"), "cut and setting");
            Check(menu.Contains("MenuHeader(localization.Get(\"ChatLinks.MenuTitle\"));PlaceEntries(menu.Map,menu.Coordinate,null,menu.Place);"), "the place entries of Tensho's menus");
            Check(!menu.Contains("ImGuiMouseButton.Left"), "the left click is the game's");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("IAddonLifecycle addonLifecycle)") && plugin.Contains("(\"ChatLinkMenu\",mainWindow.DrawChatLinkMenu),"), "wired");
            Check(plugin.Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut){DrawBeastBoard("), "Draw stops before the overlays during a cut");
            foreach (var language in new[] { "en", "fr" })
                Check(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Contains("\"MenuTitle\": \"Tensho — "), language + ": menu title");
        }),
    ];
}
