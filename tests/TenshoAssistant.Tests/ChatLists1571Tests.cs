using TenshoAssistant.Chat;

// 0.1571 (X-626, review of 0.1568, M1 to M3): the « Add » drafts kept by list and by tab, the cursor back in the box after Enter,
// « no longer put forward » that removes a line saved with its world, and a tab's words added with their capitals when « Match case » is on.
internal static class ChatLists1571Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat lists 1571 M3: « no longer put forward » removes the line with or without its world", () =>
        {
            var list = "Ann Doe@Moogle, Bob Roe, ann doe";
            Check(ChatTextFilter.IsPlayer("Ann Doe@Moogle", ChatTextFilter.Words("Ann Doe@Moogle")), "the menu shows the choice for a line with its world");
            Check(ChatTextFilter.Remove(list, ChatTextFilter.PlayerName("Ann Doe@Moogle")) != "Bob Roe", "the former removal by name left the line with its world");
            Check(ChatTextFilter.RemovePlayer(list, "Ann Doe@Moogle") == "Bob Roe", "every line naming the sender is removed: " + ChatTextFilter.RemovePlayer(list, "Ann Doe@Moogle"));
            Check(!ChatTextFilter.IsPlayer("Ann Doe@Moogle", ChatTextFilter.Words(ChatTextFilter.RemovePlayer(list, "Ann Doe@Moogle"))), "the sender is no longer put forward");
            Check(ChatTextFilter.RemovePlayer("Ann Doe@Moogle, Bob Roe", "Cid Moe@Moogle") == "Ann Doe@Moogle, Bob Roe", "another sender removes nothing");
        }),
        ("Chat lists 1571 M3: with « Match case », « Foo » is added beside « foo »; without, it is a double", () =>
        {
            Check(ChatTextFilter.Add("foo", "Foo", 300, matchCase: true) == "foo, Foo", "match case: a new line");
            Check(ChatTextFilter.Add("foo", "foo", 300, matchCase: true) is null, "match case: the same word is still a double");
            Check(ChatTextFilter.Add("foo", "Foo", 300) is null, "capitals ignored by default");
        }),
        ("Chat lists 1571 M1 M2 M3: drafts by box id, focus on the next frame, the wiring of the tab's case and of the menu", () =>
        {
            var row = Read("src/TenshoAssistant/UI/Components/SettingRow.cs");
            Check(row.Contains("Dictionary<uint,string> listSettingDrafts") && row.Contains("var draftId=ImGui.GetID(\"###ListDraft\");"), "M1: the draft is kept by the box's id");
            Check(!row.Contains("listSettingDrafts[labelKey]") && !row.Contains("listSettingDrafts.TryGetValue(labelKey"), "M1: no draft by label key left");
            Check(!row.Contains("SetKeyboardFocusHere(-1)"), "M2: no focus aimed at the last item (the button)");
            var focus = row.IndexOf("if(listSettingFocus.Remove(draftId))ImGui.SetKeyboardFocusHere();");
            Check(focus > 0 && focus < row.IndexOf("ImGui.InputTextWithHint(\"###ListDraft\""), "M2: the focus is asked just before the box");
            Check(row.Contains("if(enter)listSettingFocus.Add(draftId);"), "M2: Enter asks for the focus");
            Check(row.Contains("Chat.ChatTextFilter.Add(read(),draft,maximum,matchCase?.Invoke()??false)"), "M3: the list's case is passed to Add");
            var chat = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(chat.Contains("ListSetting(\"ChatWindow.KeepWords\",\"ChatWindow.KeepWordsTip\",()=>tab.KeepWords,value=>{tab.KeepWords=value;ChatTabChanged(tab);},300,()=>tab.WordsMatchCase);"), "M3: Only with follows the tab's case");
            Check(chat.Contains("ListSetting(\"ChatWindow.HideWords\",\"ChatWindow.HideWordsTip\",()=>tab.HideWords,value=>{tab.HideWords=value;ChatTabChanged(tab);},300,()=>tab.WordsMatchCase);"), "M3: Hide with follows the tab's case");
            Check(chat.Contains("ChatTextFilter.RemovePlayer(options.HighlightedPlayers,said.Sender)"), "M3: the menu removes by sender");
        }),
    ];
}
