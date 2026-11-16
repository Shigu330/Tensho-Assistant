using TenshoAssistant.Chat;

// 0.1477 (X-474, user report of 15/11): « Learn more here » of Dalamud's « Developer Mode is enabled » was plain text in Tensho's chat window. A
// link of Dalamud or of a plugin (DalamudLinkPayload) is now a link of its own kind, with its plugin and command; a click runs the handler
// Dalamud keeps for it, as the game's chat does; without a handler it stays plain text with a tooltip.
internal static class ChatDalamudLink1477Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat Dalamud link 1477: « Learn more here » between the link and its end is a link of Dalamud with its command", () =>
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
            ];
            var body = ChatPayloads.Segments(payloads);
            Check(body.Count == 3, "three segments: " + body.Count);
            Check(body[1].Text == "Learn more here" && body[1].Kind == ChatSegmentKind.DalamudLink && body[1].Link == link, "the link's text and target");
            Check(body[0].Link is null && body[2].Link is null, "the texts around stay plain");
            Check(ChatPayloads.HasLink(payloads) && ChatPayloads.Describe(payloads).Contains("DalamudLink", StringComparison.Ordinal), "developer-mode line");
        }),
        ("Chat Dalamud link 1477: the reader keeps the plugin and command; the window opens it by Dalamud's handler, plain text without one", () =>
        {
            var reader = Read("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("DalamudLinkPayload dalamud=>new(ChatPayloadKind.Dalamud,\"DalamudLink\",Link:new ChatLink(ChatSegmentKind.DalamudLink,Plugin:dalamud.Plugin ?? \"\",CommandId:dalamud.CommandId)),", StringComparison.Ordinal), "reader");
            Check(reader.IndexOf("DalamudLinkPayload dalamud", StringComparison.Ordinal) < reader.IndexOf("ITextProvider text", StringComparison.Ordinal), "before the texts");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("mainWindow.DalamudLinkKnown=(plugin,command)=>chatGui.RegisteredLinkHandlers.ContainsKey((plugin,command));", StringComparison.Ordinal)
                  && plugin.Contains("if(!chatGui.RegisteredLinkHandlers.TryGetValue((plugin,command),out var handler))return false;", StringComparison.Ordinal)
                  && plugin.Contains("_=framework.RunOnFrameworkThread(()=>", StringComparison.Ordinal), "the handler Dalamud keeps, on the game's thread");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("if(run.Link is { } link && ChatLinkActive(link))DrawChatLink(", StringComparison.Ordinal)
                  && window.Contains("TipText(localization.Get(\"ChatWindow.DalamudLinkGameTip\"));", StringComparison.Ordinal), "plain text with a tooltip without a handler");
            Check(window.Contains("if(ImGui.IsItemClicked(ImGuiMouseButton.Left) && OpenDalamudLink?.Invoke(link.Plugin,link.CommandId,run.Text)!=true)", StringComparison.Ordinal), "a left click opens it");
        }),
    ];
}
