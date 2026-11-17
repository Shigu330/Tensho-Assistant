using System.Numerics;
using TenshoAssistant.Chat;
using TenshoAssistant.UI;

// 0.1476: the chat window's tabs. X-466 (the user's request of 15/11): « 100+ » past 100 unread messages. X-477 (user report of 15/11): the
// frame and lock icons centred in tab-high buttons. X-478 (the user's request of 15/11): a colour for each tab's name, always legible.
internal static class ChatTabs1476Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Window() => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat tabs 1476: the unread count up to 100, then « 100+ » (X-466)", () =>
        {
            Check(ChatFormat.TabLabel("General", true, 99) == "General (99)", "99");
            Check(ChatFormat.TabLabel("General", true, 100) == "General (100)", "100");
            Check(ChatFormat.TabLabel("General", true, 101) == "General (100+)", "101");
            Check(ChatFormat.TabLabel("General", true, 5000) == "General (100+)", "5000");
            Check(ChatFormat.TabLabel("General", true, 101).Length == ChatFormat.TabLabel("General", true, 5000).Length, "one width past 100");
            Check(ChatFormat.TabLabel("General", false, 5000) == "General" && ChatFormat.TabLabel("General", true, 0) == "General", "hidden or none");
        }),
        ("Chat tabs 1476: a tab's name colour stays legible on the inactive and the active tab, in every theme (X-478)", () =>
        {
            Vector4[] chosen = [new(1, 1, 0, 1), new(0, 0, 0, 1), new(1, 1, 1, 1), new(1, 0, 0, 1), new(0.2f, 0.2f, 0.2f, 1), new(0.9f, 0.9f, 0.85f, 1)];
            foreach (var (theme, palette) in ThemePalettes.All)
                foreach (var colour in chosen)
                {
                    var shown = ChatTabColors.Readable(colour, palette.Text, palette.Panel, palette.Selected);   // the two focused backgrounds (X-495: ChatReview1478Tests for the five)
                    var legible = ThemePalettes.Contrast(shown, palette.Panel) >= ChatTabColors.MinimumContrast
                                  && ThemePalettes.Contrast(shown, palette.Selected) >= ChatTabColors.MinimumContrast;
                    Check(legible || shown == palette.Text, $"{theme} {colour}: {shown}");
                }
            var dark = ThemePalettes.All[PluginTheme.Dark];
            var gold = new Vector4(0.94f, 0.77f, 0.46f, 1);
            Check(ChatTabColors.Readable(gold, dark.Text, dark.Panel, dark.Selected) == gold, "a legible colour is kept as chosen");
            var light = ThemePalettes.All[PluginTheme.Light];
            Check(ChatTabColors.Readable(new(1, 1, 1, 1), light.Text, light.Panel, light.Selected) != new Vector4(1, 1, 1, 1), "white on the light theme is darkened");
        }),
        ("Chat tabs 1476: the setting is saved per tab, null by default (the theme's text colour), and drawn on the tab (X-478)", () =>
        {
            Check(new ChatTabSettings().NameColor is null, "default: none");
            var window = Window();
            Check(window.Contains("var coloured=PushChatTabColor(tab);", StringComparison.Ordinal)
                  && window.Contains("finally{if(coloured)ImGui.PopStyleColor();}", StringComparison.Ordinal), "the name's colour pushed around the tab only");
            Check(window.Contains("Tip(\"ChatWindow.TabColorTip\");", StringComparison.Ordinal)
                  && window.Contains("if(DefaultButton(\"ChatTabColor\",tab.NameColor is not null)){tab.NameColor=null;chatSettingsPending=true;}", StringComparison.Ordinal), "tooltip and reset");
        }),
        ("Chat tabs 1476: the frame and lock buttons go through the common icon component, no icon font label (X-477)", () =>
        {
            var window = Window();
            Check(window.Split("GlyphTabButton(").Length == 7, "six icon tab buttons (X-476: « + » and the gear; X-467: both chats and reduce)");
            Check(!window.Contains("ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);", StringComparison.Ordinal), "no icon font pushed by hand in the chat window");
            var component = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/ToolButton.cs")).Replace("\r\n", "\n");
            Check(component.Contains("var clicked=ImGui.TabItemButton(new string(' ',", StringComparison.Ordinal)
                  && component.Contains("DrawGlyphAt(icon,ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),", StringComparison.Ordinal), "blank label in the text font, glyph centred");
        }),
    ];
}
