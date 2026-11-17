using TenshoAssistant.Chat;

// 0.1472 (X-482, review of 0.1467, minor findings of the Tensho chat window): M1 a link without text of its own kept « [link] » for good when
// the message came before the lists of names (login): the layout key now says which lists are loaded; M2 the « back to the bottom » button
// sat over the lines, and a click on it also opened the link or the menu under it: it is placed before the lines, which do not answer the
// mouse under it; M3 every small wheel value (a fine wheel, a touchpad) moved a whole line: the values add up to whole notches; M4 « 1 new »
// in the singular. M5 was done with X-480 in 0.1469 (the three loaders of floors alike).
internal static class ChatReview1472Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Window() => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1472 M1: a message laid out before the lists of names is laid out again once they are loaded", () =>
        {
            var options = new ChatSettings();
            var before = ChatFormat.LayoutKey(options, ChatFormat.NamesKey(false, false, false));
            Check(before != ChatFormat.LayoutKey(options, ChatFormat.NamesKey(true, false, false)) && ChatFormat.NamesKey(true, true, true) == "iqm", "items loaded: a new key");
            var view = new ChatTabView();
            view.Relayout(500, before);
            Check(view.LaidOutFor(500, before) && !view.LaidOutFor(500, ChatFormat.LayoutKey(options, ChatFormat.NamesKey(true, true, true))), "laid out again");
            Check(Window().Contains("ChatFormat.LayoutKey(options,ChatFormat.NamesKey(itemCatalog.State.Catalog is not null,quests.State.ById is not null,maps.State.Catalog is not null))", StringComparison.Ordinal), "the window's key");
        }),
        ("Chat review 1472 M2: the button's place is known before the lines; the lines and links under it do not answer the mouse", () =>
        {
            var corner = ChatScroll.ButtonCorner(500, 400, 200, 80, 24);
            Check(corner == (416f, 572f), "bottom right of the visible lines: " + corner);
            Check(ChatScroll.OnButton(450, 580, corner, 80, 24) && !ChatScroll.OnButton(400, 580, corner, 80, 24) && !ChatScroll.OnButton(450, 560, corner, 80, 24), "on it, beside it, above it");
            var window = Window();
            var placed = window.IndexOf("var button=atBottom ? null:ChatBottomButton(view,scroll,shown);", StringComparison.Ordinal);
            var lines = window.IndexOf("DrawChatRecord(view.RecordAt(index),view.LinesAt(index)!,top+row*height,height,options,view.RepeatsAt(index));", StringComparison.Ordinal);
            Check(placed > 0 && placed < lines, "placed before the lines are drawn");
            Check(window.Contains("if(!chatLinesBlocked && ImGui.IsItemClicked(ImGuiMouseButton.Right))", StringComparison.Ordinal)
                  && window.Contains("if(chatLinesBlocked){if(link.Kind==ChatSegmentKind.ItemLink", StringComparison.Ordinal), "a line's menu and a link: not under the button");
        }),
        ("Chat review 1472 M3: a fine wheel's small values add up to whole notches (ten of 0.1: one line), a notch is still one line", () =>
        {
            var rest = 0f;
            var scroll = 400f;
            var moved = 0;
            for (var frame = 0; frame < 10; frame++)
            {
                var next = ChatScroll.Wheel(scroll, 0.1f, 20, 600, ref rest);
                if (next != scroll) moved++;
                scroll = next;
            }
            Check(moved == 1 && scroll == 380, $"one line for ten small values: {moved}, {scroll}");
            rest = 0;
            Check(ChatScroll.Wheel(400, 1, 20, 600, ref rest) == 380 && rest == 0, "a notch: one line, nothing left");
            rest = 0;
            Check(ChatScroll.Wheel(400, -2.5f, 20, 600, ref rest) == 440 && Math.Abs(rest + 0.5f) < 0.001f, "two and a half notches: two lines, half a notch kept");
        }),
        ("Chat review 1472 M4: « 1 new » in the singular, « N new » above", () =>
        {
            var window = Window();
            Check(window.Contains("view.Unread switch{1=>localization.Get(\"ChatWindow.NewBelowOne\"),>1=>localization.Format(\"ChatWindow.NewBelow\",view.Unread),_=>localization.Get(\"ChatWindow.ToBottom\")}", StringComparison.Ordinal), "the label");
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            Check(fr.Contains("\"NewBelowOne\": \"1 nouveau\"", StringComparison.Ordinal) && en.Contains("\"NewBelowOne\": \"1 new\"", StringComparison.Ordinal), "both languages");
        }),
    ];
}
