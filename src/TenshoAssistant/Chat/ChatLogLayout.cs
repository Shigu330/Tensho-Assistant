using System.Globalization;

namespace TenshoAssistant.Chat;

/// <summary>X-467 (0.1502, probe): a box on the screen, in pixels (the game's own windows, read by ChatLogLayoutProbe).</summary>
public readonly record struct ChatLogBox(float X,float Y,float Width,float Height)
{
    /// <summary>Whether this box lies inside <paramref name="outer"/>, within one pixel.</summary>
    public bool Inside(ChatLogBox outer)=>X>=outer.X-1 && Y>=outer.Y-1 && X+Width<=outer.X+outer.Width+1 && Y+Height<=outer.Y+outer.Height+1;

    public override string ToString()=>string.Create(CultureInfo.InvariantCulture,$"{X:0},{Y:0} {Width:0}x{Height:0}");
}

/// <summary>X-467 (0.1502, probe): one of the game's message panels (ChatLogPanel_0 to _3): shown or not, and where.</summary>
public readonly record struct ChatLogPanelBox(int Index,bool Visible,ChatLogBox Box);

/// <summary>
/// X-467 (0.1502, the master's choice of 16/11: the probe before the chat's look): where the game's chat window puts its parts, read without
/// changing anything. The game's chat is one window, ChatLog (the typing box, the channel menu, the tabs, the background, the resize button:
/// FFXIVClientStructs' AddonChatLog), and four windows of messages, ChatLogPanel_0 to _3 (AddonChatLogPanel), one per tab. This tells whether
/// the typing box can sit under Tensho's chat window while the messages are hidden: docs/evidence/0.1502-game-chat-input.md. Pure: the log
/// line holds boxes and flags only, never a word of a message nor of the typing box.
/// </summary>
/// <param name="Window">ChatLog itself (its position and its scaled size).</param>
/// <param name="Input">The typing box (the TextInput component's node); null when it could not be read.</param>
/// <param name="Channel">The channel menu before the typing box (ChannelSelectDropDown); null when it could not be read.</param>
/// <param name="Background">The window's background (BackgroundNode); null when it could not be read.</param>
/// <param name="Tabs">The first tab (ChatTabs[0]); null when it could not be read.</param>
/// <param name="InputFocused">Whether ChatLog's focus node is the typing box. X-525 M1 (0.1503): not a sign of typing: the reading of 0.1502
/// found it true while the player was not typing (ChatLog's focus node is the typing box even when it is closed; docs/evidence/0.1502-game-chat-input.md,
/// section 3), so the log line writes « focus node », never « focused » nor « typing ».</param>
public sealed record ChatLogLayout(bool Visible,float Scale,ChatLogBox Window,ChatLogBox? Input,ChatLogBox? Channel,ChatLogBox? Background,ChatLogBox? Tabs,bool InputFocused,IReadOnlyList<ChatLogPanelBox> Panels)
{
    /// <summary>The log line: boxes, flags, and where each part lies (inside ChatLog, above or below the input). No text.</summary>
    public string Line()
    {
        static string Box(ChatLogBox? box)=>box?.ToString() ?? "unread";
        var inputBelowPanels=Input is { } input && Panels.Where(panel=>panel.Visible).All(panel=>panel.Box.Y+panel.Box.Height<=input.Y+1);
        var panels=string.Join("; ",Panels.Select(panel=>$"panel{panel.Index} {(panel.Visible ? "shown" : "hidden")} {panel.Box}{(panel.Box.Inside(Window) ? " inside" : " outside")}"));
        return string.Create(CultureInfo.InvariantCulture,
            $"window {(Visible ? "shown" : "hidden")} {Window} scale {Scale:0.00}; input {Box(Input)}{(Input is { } i && i.Inside(Window) ? " inside" : "")}{(InputFocused ? " focus node" : "")}; "
            +$"channel {Box(Channel)}; tabs {Box(Tabs)}; background {Box(Background)}; input below the shown panels {inputBelowPanels}; {panels}");
    }
}
