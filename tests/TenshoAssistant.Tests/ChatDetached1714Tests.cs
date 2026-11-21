using TenshoAssistant.Chat;

// X-447 B11 and B12 (0.1714, the user's choices of 15/11): a chat tab detached into a window of its own (numbered, each with its own place), the
// chat window always keeping one tab; every chat window hidden for a moment by « /tensho chat hide » or a button, shown back by the same or
// « /tensho chat »; the detached window drawn by the chat window's own code with its state swapped in.
internal static class ChatDetached1714Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static ChatTabSettings Tab(string name, int window = 0) => new() { Name = name, Window = window };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat detached 1714: a tab goes into the first free window; the chat window keeps one tab; at most four", () =>
        {
            var tabs = new List<ChatTabSettings> { Tab("General"), Tab("Battle"), Tab("Event"), Tab("Social"), Tab("Party"), Tab("Tells") };
            Check(ChatWindows.Detach(tabs, tabs[1]) && tabs[1].Window == 1, "the first free window: 1");
            Check(ChatWindows.Detach(tabs, tabs[5]) && tabs[5].Window == 2, "then 2");
            Check(!ChatWindows.Detach(tabs, tabs[1]) && tabs[1].Window == 1, "already detached: nothing changes");
            Check(ChatWindows.Detach(tabs, tabs[2]) && ChatWindows.Detach(tabs, tabs[3]) && tabs[2].Window == 3 && tabs[3].Window == 4, "3 and 4");
            Check(ChatWindows.Free(tabs, tabs[4]) == 0 && !ChatWindows.Detach(tabs, tabs[4]) && tabs[4].Window == 0, "no fifth window");
            Check(ChatWindows.Attach(tabs[2]) && tabs[2].Window == 0 && !ChatWindows.Attach(tabs[2]), "put back once");
            Check(ChatWindows.Detach(tabs, tabs[4]) && tabs[4].Window == 3, "the window let go is taken again");
            var main = new List<ChatTabSettings>();
            ChatWindows.InChatWindow(tabs, tabs, main);
            Check(main.Count == 2 && main[0] == tabs[0] && main[1] == tabs[2], "the chat window keeps the others, in their order");
            Check(ChatWindows.Free(tabs, tabs[0]) == 0 || main.Count > 1, "never the last");
            var two = new List<ChatTabSettings> { Tab("A"), Tab("B") };
            Check(ChatWindows.Detach(two, two[0]) && !ChatWindows.Detach(two, two[1]) && two[1].Window == 0, "the last tab of the chat window stays");
            Check(ChatWindows.Free(two, Tab("Elsewhere")) == 0, "a tab not in the list: nothing");
        }),
        ("Chat detached 1714: a number shared or out of range: the first owns it, the other stays in the chat window", () =>
        {
            var tabs = new List<ChatTabSettings> { Tab("General"), Tab("One", 2), Tab("Two", 2), Tab("Far", 9), Tab("Below", -1) };
            Check(ChatWindows.In(tabs, 2) == tabs[1] && ChatWindows.Detached(tabs, tabs[1]) && !ChatWindows.Detached(tabs, tabs[2]), "the first owns window 2");
            Check(ChatWindows.Of(tabs[3]) == 0 && ChatWindows.Of(tabs[4]) == 0 && ChatWindows.In(tabs, 9) is null && ChatWindows.In(tabs, 0) is null, "out of range: the chat window");
            var main = new List<ChatTabSettings>();
            ChatWindows.InChatWindow(tabs, tabs, main);
            Check(main.Count == 4 && !main.Contains(tabs[1]) && main.Contains(tabs[2]), "the second asking for 2 is drawn in the chat window, never lost");
            Check(ChatWindows.Any(tabs) && !ChatWindows.Any([Tab("A"), Tab("B", 7)]), "any detached");
            var shown = new List<ChatTabSettings> { tabs[0], tabs[1] };
            ChatWindows.InChatWindow(shown, tabs, main);
            Check(main.Count == 1 && main[0] == tabs[0], "the context's tabs, the detached ones out");
        }),
        ("Chat detached 1714: wiring: the panels, the bar without the detached tabs, the menus, the swap, the command", () =>
        {
            var window = Read("MainWindow.ChatWindow.cs");
            var detached = Read("MainWindow.ChatDetached.cs");
            Check(window.Contains("        ChatWindows.InChatWindow(tabs,ChatAllTabs(options),chatMainTabs);\n        tabs=chatMainTabs;", StringComparison.Ordinal), "the bar's tabs");
            Check(window.Contains("DrawChatDetachEntry(tab);", StringComparison.Ordinal), "the tab's menu detaches it");
            Check(window.Contains("if(!ChatWindowShown() || chatAllHidden)return false;", StringComparison.Ordinal), "hidden with the others");
            Check(window.Contains("if(chatAllHidden){chatAllHidden=false;if(ChatOptions.WindowOpen)return;}", StringComparison.Ordinal), "« /tensho chat » shows them back first");
            Check(detached.Contains("if(ImGui.Selectable(localization.Get(\"ChatWindow.HideAll\")))chatAllHidden=true;", StringComparison.Ordinal), "hide all, from a tab's menu and the detached window's");
            Check(window.Contains("ChatWindows.Of(tab)!=0 ||", StringComparison.Ordinal), "a detached conversation is never replaced");
            Check(detached.Contains("SwapChatPane(pane);\n        try{DrawChatPins(tab,options);DrawChatLines(view,options,0,view.Serial);}\n        finally{SwapChatPane(pane);}", StringComparison.Ordinal), "its lines by the chat window's code, its state swapped");
            Check(detached.Contains("()=>ChatOptions.WindowOpen && !inDesktopApp && ChatDetachedTab(window) is not null,()=>ChatWindowShown() && !chatAllHidden,", StringComparison.Ordinal), "wanted and shown");
            Check(detached.Contains("()=>AttachChatWindow(window)", StringComparison.Ordinal), "its cross puts the tab back");
            foreach (var name in new[] { "chatLineHeight", "chatWheelView", "chatWheelRest", "chatShownTab", "chatWindowLine", "chatWindowPlace", "chatWindowWeb", "chatWindowWebSite", "chatWindowQuest", "chatSelectText", "chatSelectOpen", "chatScrollTo", "chatScrollWatch", "chatZonesNext", "chatSize" })
                Check(detached.Contains("(" + name + ",pane.", StringComparison.Ordinal), "swapped: " + name);
            Check(Read("MainWindow.TrackerWindow.cs").Contains("..ChatDetachedPanels().Select(panel=>new PanelWindow(this,panel)),new(this,OccultPanelDefinition),", StringComparison.Ordinal), "the panels created");
            Check(Read("Plugin.cs").Contains("is \"chat hide\") { mainWindow?.ToggleChatWindowsHidden(); return; }", StringComparison.Ordinal), "the command");
            Check(Read("Chat/ChatSettings.cs").Contains("    public int Window{get;set;}", StringComparison.Ordinal), "saved with the tab");
            var en = Read("Localization/Languages/en.json"); var fr = Read("Localization/Languages/fr.json");
            foreach (var key in new[] { "Detach", "DetachTip", "DetachNoneTip", "Attach", "AttachTip", "HideAll", "HideAllTip" })
                Check(en.Contains($"    \"{key}\": ", StringComparison.Ordinal) && fr.Contains($"    \"{key}\": ", StringComparison.Ordinal), "text: " + key);
            Check(en.Contains("« chat hide »", StringComparison.Ordinal) && fr.Contains("« chat hide »", StringComparison.Ordinal), "the command's help");
        }),
    ];
}
