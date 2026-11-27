using TenshoAssistant.Chat;

// X-715 (0.1718, D-202 b): the developer-mode probe of what the game's chat sends on a click on a player's name: its lines never hold a name
// nor a whole identifier, a menu is tied to the click just before, the lines stop at their maximum; the hook calls the game's function
// always and is on in developer mode only (docs/evidence/0.1718-chat-link-click-probe.md).
internal static class ChatLinkProbe1718Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat link probe 1718: small values as they are, large ones and identifiers masked", () =>
        {
            Check(ChatLinkProbe.Value(0) == "0" && ChatLinkProbe.Value(99_999) == "99999" && ChatLinkProbe.Value(-5) == "-5", "small values");
            Check(ChatLinkProbe.Value(0x1234_5678) == "large (…678)" && ChatLinkProbe.Value(100_000) == "large (…6a0)", "large values: the last three hexadecimal digits");
            Check(ChatLinkProbe.Masked(0) == "0" && ChatLinkProbe.Masked(0xE000_0000) == "0" && ChatLinkProbe.Masked(0x0040_0000_1234_5ABC) == "set (…abc)", "identifiers");
        }),
        ("Chat link probe 1718: the click's values matched with the menu's target, named only", () =>
        {
            const ulong content = 0x0040_0000_1234_5ABC;
            var matched = ChatLinkProbe.Matches(0x1234_5ABC, 0x0040_0000, 40, 0, content, 40, 0x1000_0042);
            Check(matched == "value 1 = content id (low half), value 2 = content id (high half), value 3 = home world", "content id halves and world: " + matched);
            Check(ChatLinkProbe.Matches(0x1000_0042, 0, 0, 7, content, 40, 0x1000_0042) == "value 1 = object id", "object id");
            Check(ChatLinkProbe.Matches(0, 0, 0, 0, 0, 0, 0xE000_0000) == "none" && ChatLinkProbe.Matches(5, 6, 7, 8, content, 40, 0xE000_0000) == "none", "nothing alike: none; 0 never matches");
        }),
        ("Chat link probe 1718: a click then its menu, a menu alone, never a name, at most forty lines", () =>
        {
            var probe = new ChatLinkProbe();
            var click = probe.Clicked(3, 1, 2, 0, 0x1234_5ABC, 17, 0, true, 40, 1000);
            Check(click == "Chat link probe, click 1: link type 3 (sent as 13), id 1, index 2, group 0, values large (…abc), 17, 0, text in the panel (end 40).", "the click: " + click);
            var menu = probe.MenuOpened("ChatLog", 0x0040_0000_1234_5ABC, 40, 0xE000_0000, false, 7, 1200);
            Check(menu == "Chat link probe, menu after click 1 (200 ms): content id set (…abc), home world 40, object none (id 0), name 7 characters; the click's values that match it: value 1 = content id (low half).", "its menu: " + menu);
            var alone = probe.MenuOpened("ChatLog", 0, 0, 0, false, 0, 5000);
            Check(alone is not null && alone.StartsWith("Chat link probe, the game's menu opened from ChatLog without a link click just before", StringComparison.Ordinal), "a menu with no click just before: " + alone);
            probe.Clicked(3, 1, 2, 0, 1, 2, 3, false, 0, 10_000);
            Check(probe.MenuOpened("ChatLog", 0, 0, 0, false, 0, 11_001)!.Contains("without a link click", StringComparison.Ordinal), "more than a second later: not that click's");
            for (var count = 0; count < 50; count++) probe.Clicked(1, 0, 0, 0, 0, 0, 0, false, 0, 20_000 + count);
            Check(probe.Full && probe.Clicked(1, 0, 0, 0, 0, 0, 0, false, 0, 30_000) is null && probe.MenuOpened("ChatLog", 0, 0, 0, false, 0, 30_001) is null, "forty lines at most");
            Check(!Read("Chat/ChatLinkProbe.cs").Contains("TargetName", StringComparison.Ordinal), "no name reaches the lines (its length only)");
        }),
        ("Chat link probe 1718: wiring: the game's function always called, developer mode only, the menu read, the plugin owns it", () =>
        {
            var watch = Read("Chat/ChatLinkWatch.Dalamud.cs");
            Check(watch.Contains("if((nint)LogViewer.MemberFunctionPointers.HandleLinkClick==0)throw new InvalidOperationException(", StringComparison.Ordinal), "the pointer checked first");
            var detour = watch[watch.IndexOf("private void OnLinkClick(", StringComparison.Ordinal)..watch.IndexOf("private void OnMenuOpened(", StringComparison.Ordinal)];
            Check(detour.Contains("catch(Exception error){Said(error);}\n        hook!.Original(viewer,link);", StringComparison.Ordinal), "the game's function called after the guarded copy, whatever happens");
            Check(detour.Contains("developerMode() && !TenshoAssistant.Competitive.CompetitiveGate.Cut", StringComparison.Ordinal) && !detour.Contains("link->Payload[", StringComparison.Ordinal) && !detour.Contains("link->IntValue1=", StringComparison.Ordinal), "read only, developer mode, never during a cut");
            Check(watch.Contains("var want=!failed && hook is not null && developerMode() && !probe.Full;", StringComparison.Ordinal), "the hook on in developer mode only");
            Check(watch.Contains("addon.StartsWith(\"ChatLog\",StringComparison.Ordinal)", StringComparison.Ordinal) && !watch.Contains("AddMenuItem", StringComparison.Ordinal), "the chat's menus read, nothing added");
            // X-153 (0.1894): owned by the chat family.
            Check(Read("Plugin.Chat.cs").Contains("new Chat.ChatLinkWatch(interop,framework,contextMenus,log,()=>configuration.DeveloperMode),()=>chatLinkWatch?.Dispose());", StringComparison.Ordinal), "owned and disposed by the plugin");
        }),
    ];
}
