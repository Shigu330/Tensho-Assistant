using TenshoAssistant.Chat;

// X-716 (0.1716, the user's request of 21/11 around 11:35): the chat's lists (players hidden or put forward, alert words, words to keep or
// hide) in an area of their own height with a scroll bar; about two hundred and fifty names for the players and the alert words; a full
// list said, never an addition lost silently.
internal static class ChatLists1716Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat lists 1716: two hundred and fifty names fit; past the limit the addition is refused and said full, the list unchanged", () =>
        {
            string? list = null;
            for (var index = 0; index < 250; index++)
            {
                var (next, full) = ChatTextFilter.TryAdd(list, $"Player{index:000} Name@World", ChatTextFilter.ListMaximum);
                Check(next is not null && !full, $"name {index} fits");
                list = next;
            }
            Check(ChatTextFilter.Words(list).Count == 250 && list!.Length <= ChatTextFilter.ListMaximum, "two hundred and fifty names kept: " + list!.Length);
            var current = list;
            for (var index = 0; ChatTextFilter.TryAdd(current, $"x{index}", ChatTextFilter.ListMaximum) is ({ } more, false); index++) current = more;
            var (refused, isFull) = ChatTextFilter.TryAdd(current, "One More Player", ChatTextFilter.ListMaximum);
            Check(refused is null && isFull, "full: refused and said");
            Check(ChatTextFilter.Add(current, "One More Player", ChatTextFilter.ListMaximum) is null, "Add says nothing added, as before");
            Check(ChatTextFilter.TryAdd(current, "Player000 Name@World", ChatTextFilter.ListMaximum) == (null, false), "already there: nothing new, not full");
            Check(ChatTextFilter.TryAdd(current, "  ", ChatTextFilter.ListMaximum) == (null, false), "empty: nothing new, not full");
            var removed = ChatTextFilter.Remove(current, 0);
            Check(ChatTextFilter.TryAdd(removed, "One More", ChatTextFilter.ListMaximum).List is not null, "a line removed: room again");
            Check(ChatTextFilter.ListMaximum >= 6000, "at least six thousand characters");
        }),
        ("Chat lists 1716: wiring: the scroll area, the full line under the box, the menu's entries greyed, the limit used everywhere", () =>
        {
            var row = Read("UI/Components/SettingRow.cs");
            Check(row.Contains("if(scrolled)ImGui.BeginChild(\"###ListLines\",new Vector2(0,ListRowsShown*ImGui.GetFrameHeightWithSpacing()),true);", StringComparison.Ordinal)
                && row.Contains("finally{if(scrolled)ImGui.EndChild();}", StringComparison.Ordinal), "the scroll area");
            Check(row.Contains("else{listSettingDrafts[draftId]=draft;if(Chat.ChatTextFilter.TryAdd(read(),draft,maximum,matchCase?.Invoke()??false).Full)listSettingFull[draftId]=(draft,read()??\"\");}", StringComparison.Ordinal)
                && row.Contains("if(Chat.ChatTextFilter.StillRefused(refused,listSettingDrafts.GetValueOrDefault(draftId)??\"\",read()))ImGui.TextColored(Warning,localization.Format(\"ChatWindow.ListFull\",maximum));", StringComparison.Ordinal), "full: the text kept, said");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(!window.Contains(",600)", StringComparison.Ordinal), "no old limit left for the players and the alert words");
            Check(window.Split("ChatTextFilter.ListMaximum").Length - 1 >= 7, "the one limit");
            Check(window.Contains("ImGui.BeginDisabled(hideFull);", StringComparison.Ordinal) && window.Contains("ImGui.BeginDisabled(forwardFull);", StringComparison.Ordinal), "the menu's entries greyed when full");
            Check(Read("Localization/Languages/en.json").Contains("    \"ListFull\": ", StringComparison.Ordinal) && Read("Localization/Languages/fr.json").Contains("    \"ListFull\": ", StringComparison.Ordinal), "text");
        }),
    ];
}
