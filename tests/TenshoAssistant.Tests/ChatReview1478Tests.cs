using System.Numerics;
using TenshoAssistant.Chat;
using TenshoAssistant.UI;

// 0.1478: the review of block 0.1475-0.1477. X-495: a tab's name colour readable on every background a tab is drawn on, the window focused
// or not (the chat window has NoFocusOnAppearing: its usual state is « unfocused »). X-496 M1: a link of Dalamud passes its own string to its
// handler, as Dalamud does; M2: a right click on it opens the message's menu; M4: HelpTextProbe find names the sheets it could not read.
internal static class ChatReview1478Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1478: a dark name chosen on the light theme stays legible on a dark background of an unfocused tab (X-495)", () =>
        {
            var light = ThemePalettes.All[PluginTheme.Light];
            var darkUnfocused = new Vector4(0.1f, 0.1f, 0.1f, 1);
            var black = new Vector4(0, 0, 0, 1);
            // Before 0.1478 only the focused backgrounds were checked: black passed on the light theme's tabs.
            Check(ChatTabColors.Readable(black, light.Text, light.Panel, light.Selected) == black, "the light focused tabs alone accept black");
            var shown = ChatTabColors.Readable(black, light.Text, light.Panel, light.Selected, darkUnfocused);
            Check(shown != black, "black is moved off when an unfocused tab is dark");
            Check(ThemePalettes.Contrast(shown, darkUnfocused) >= ChatTabColors.MinimumContrast || shown == light.Text, "legible on it, or the theme's text");
        }),
        ("Chat review 1478: every theme, every colour: legible on the five tab backgrounds the theme pushes (X-495)", () =>
        {
            Vector4[] chosen = [new(1, 1, 0, 1), new(0, 0, 0, 1), new(1, 1, 1, 1), new(1, 0, 0, 1), new(0.2f, 0.2f, 0.2f, 1), new(0.9f, 0.9f, 0.85f, 1)];
            foreach (var (theme, p) in ThemePalettes.All)
            {
                // Tab, TabActive, TabHovered, TabUnfocused, TabUnfocusedActive, as FfxivTheme.Push sets them.
                Vector4[] backgrounds = [p.Panel, p.Selected, p.Hovered, p.Panel, p.Selected];
                foreach (var colour in chosen)
                {
                    var shown = ChatTabColors.Readable(colour, p.Text, backgrounds);
                    Check(backgrounds.All(b => ThemePalettes.Contrast(shown, b) >= ChatTabColors.MinimumContrast) || shown == p.Text, $"{theme} {colour}: {shown}");
                }
            }
        }),
        ("Chat review 1478: the theme pushes the unfocused tabs' colours; the window checks the five backgrounds (X-495)", () =>
        {
            var theme = Read("src/TenshoAssistant/UI/FfxivTheme.cs");
            Check(theme.Contains("ImGui.PushStyleColor(ImGuiCol.TabUnfocused, c.Panel);", StringComparison.Ordinal)
                  && theme.Contains("ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, c.Selected);", StringComparison.Ordinal), "theme");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            foreach (var colour in new[] { "Tab", "TabActive", "TabHovered", "TabUnfocused", "TabUnfocusedActive" })
                Check(window.Contains($"Style(ImGuiCol.{colour})", StringComparison.Ordinal), colour);
        }),
        ("Chat review 1478: a Dalamud link's own string runs from the link to its end, the terminator included (X-496 M1)", () =>
        {
            var link = new ChatLink(ChatSegmentKind.DalamudLink, Plugin: "Dalamud", CommandId: 7);
            ChatPayload[] payloads =
            [
                new(ChatPayloadKind.Text, "TextPayload", Text: "Developer Mode is enabled. "),
                new(ChatPayloadKind.Dalamud, "DalamudLink", Link: link),
                new(ChatPayloadKind.Other, "UIForegroundPayload"),
                new(ChatPayloadKind.Text, "TextPayload", Text: "Learn more here"),
                new(ChatPayloadKind.Other, "UIForegroundPayload"),
                new(ChatPayloadKind.LinkEnd, "LinkEnd"),
                new(ChatPayloadKind.Text, "TextPayload", Text: "."),
                new(ChatPayloadKind.LinkEnd, "LinkEnd"),
            ];
            Check(ChatPayloads.LinkLast(payloads, 1) == 5, "the first terminator after the link: " + ChatPayloads.LinkLast(payloads, 1));
            Check(ChatPayloads.LinkLast(payloads[..5], 1) == 4, "not closed: the message's last payload");
            var withData = link with { Data = [1, 2, 3] };
            Check(ChatPayloads.Segments([payloads[0], payloads[1] with { Link = withData }, .. payloads[2..]])[1].Link?.Data?.Length == 3, "the data stays on the segment");
            var reader = Read("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("var last=ChatPayloads.LinkLast(payloads,index);", StringComparison.Ordinal)
                  && reader.Contains("Data=new SeString(message.Message.Payloads.Skip(index).Take(last-index+1).ToList()).Encode()", StringComparison.Ordinal), "reader keeps it");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("var message=data is { Length:>0 } ? Dalamud.Game.Text.SeStringHandling.SeString.Parse(data)", StringComparison.Ordinal)
                  && plugin.Contains("handler(command,message);", StringComparison.Ordinal), "the handler gets it");
            Check(plugin.Contains("catch(Exception error){mainWindow.DalamudLinkFailed=plugin;", StringComparison.Ordinal), "a failure is said to the player");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1478-dalamud-link-handler.md")), "evidence (R06)");
        }),
        ("Chat review 1478: a right click on a Dalamud link opens the message's menu; a failure shows on its tooltip (X-496 M2)", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var branch = window[window.IndexOf("if(link.Kind==ChatSegmentKind.DalamudLink)", StringComparison.Ordinal)..window.IndexOf("if(link.Kind==ChatSegmentKind.WebLink)", StringComparison.Ordinal)];
            Check(branch.Contains("if(ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowLine=record;ImGui.OpenPopup(\"###TenshoChatLine\");}", StringComparison.Ordinal), "right click: the message's menu");
            Check(branch.Contains("OpenDalamudLink?.Invoke(link.Plugin,link.CommandId,link.Data,run.Text)", StringComparison.Ordinal), "the link's data passed");
            Check(branch.Contains("\"ChatWindow.DalamudLinkFailedTip\"", StringComparison.Ordinal), "failure tooltip");
        }),
        ("Chat review 1478: HelpTextProbe find names the sheets it could not read (X-496 M4)", () =>
        {
            var probe = Read("tools/HelpTextProbe/Program.cs");
            Check(!probe.Contains("catch(Exception){}", StringComparison.Ordinal), "no silent catch");
            Check(probe.Contains("NOT SEARCHED {unread.Count} sheet(s) unreadable", StringComparison.Ordinal), "named at the end");
        }),
    ];
}
